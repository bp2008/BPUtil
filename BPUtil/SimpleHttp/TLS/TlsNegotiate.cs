using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp.TLS
{
	/// <summary>
	/// Static class which offers TLS negotiation capability to HttpProcessor.
	/// </summary>
	public static class TlsNegotiate
	{
		/// <summary>
		/// SslProtocols typed Tls13 that is available in earlier versions of .NET Framework.
		/// </summary>
		public const SslProtocols Tls13 = (SslProtocols)12288;

#if NET6_0
		private static ConcurrentDictionary<X509Certificate, SslStreamCertificateContext> tlsCertContexts = new ConcurrentDictionary<X509Certificate, SslStreamCertificateContext>();
		private static SslStreamCertificateContext CreateSslStreamCertificateContextFromCert(X509Certificate cert)
		{
			X509Certificate2 c2;
			if (cert is X509Certificate2)
				c2 = (X509Certificate2)cert;
			else
				c2 = new X509Certificate2(cert);
			return SslStreamCertificateContext.Create(c2, null);
		}
#endif
		/// <summary>
		/// <para>Handles TLS negotiation on the given HttpProcessor.</para>
		/// <para>Returns false if the connection should be closed immediately.</para>
		/// <para>It is possible for this method to return without performing TLS authentication, if the client did not request it and the HttpProcessor is configured to allow plain HTTP.  Check the HttpProcessor's secure_https flag to know what happened.</para>
		/// </summary>
		/// <param name="p">HttpProcessor</param>
		/// <returns>Returns false if the connection should be closed immediately.</returns>
		public static bool NegotiateSync(HttpProcessor p)
		{
			return TaskHelper.RunAsyncCodeSafely(() => NegotiateAsync(p));
		}
		/// <summary>
		/// <para>Handles TLS negotiation on the given HttpProcessor.</para>
		/// <para>Returns false if the connection should be closed immediately.</para>
		/// <para>It is possible for this method to return without performing TLS authentication, if the client did not request it and the HttpProcessor is configured to allow plain HTTP.  Check the HttpProcessor's secure_https flag to know what happened.</para>
		/// </summary>
		/// <param name="p">HttpProcessor</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns>Returns false if the connection should be closed immediately.</returns>
		public static async Task<bool> NegotiateAsync(HttpProcessor p, CancellationToken cancellationToken = default)
		{
			if (p.allowedConnectionTypes.HasFlag(AllowedConnectionTypes.https))
			{
				try
				{
					// Read the TLS Client Hello message to get the server name so we can select the correct certificate and populate the hostName property of this HttpProcessor.
					// Note it is possible that the client is not using TLS, in which case no certificate will be selected and the request will be processed as plain HTTP.
					TlsPeekData tlsData;
					tlsData = await TLS.TlsServerNameReader.TryGetTlsClientHelloServerNamesAsync(p.tcpClient.Client, cancellationToken).ConfigureAwait(false);
					if (tlsData != null)
					{
						X509Certificate cert = null;
						p.secure_https = true;
						p.HostName = tlsData.ServerName;
						if (tlsData.IsTlsAlpn01Validation)
						{
#if NET6_0
							cert = await p.certificateSelector.GetAcmeTls1Certificate(p, tlsData.ServerName).ConfigureAwait(false);
							if (cert == null)
							{
								SimpleHttpLogger.LogVerbose("\"acme-tls/1\" protocol negotiation failed because the certificate selector [" + p.certificateSelector.GetType() + "] returned null certificate for server name " + (tlsData.ServerName == null ? "null" : ("\"" + tlsData.ServerName + "\"")) + ". Client IP: " + p.RemoteIPAddressStr);
								return false;
							}
							SslServerAuthenticationOptions sslOptions = new SslServerAuthenticationOptions();
							sslOptions.ApplicationProtocols = new List<SslApplicationProtocol> { new SslApplicationProtocol("acme-tls/1") };
							sslOptions.ServerCertificate = cert;
							SslStream ssA = WrapSslStream(p);
							await TaskHelper.DoWithTimeout(ssA.AuthenticateAsServerAsync(sslOptions, cancellationToken), (HttpProcessor.readTimeoutSeconds * 2000).Clamp(1000, 30000)).ConfigureAwait(false);
							SimpleHttpLogger.LogVerbose("\"acme-tls/1\" client connected using SslProtocol." + (p.tcpStream as SslStream).SslProtocol + ". " + "Client IP: " + p.RemoteIPAddressStr);
							return false; // This connection is not allowed to be used for data transmission after TLS negotiation is complete.
#else
							SimpleHttpLogger.LogVerbose("\"acme-tls/1\" protocol negotiation failed because the current .NET version does not support the \"acme-tls/1\" protocol. Client IP: " + p.RemoteIPAddressStr);
							return false;
#endif
						}
						else
							cert = await p.certificateSelector.GetCertificate(p, tlsData.ServerName).ConfigureAwait(false);
						if (cert == null)
						{
							SimpleHttpLogger.LogVerbose("TLS negotiation failed because the certificate selector [" + p.certificateSelector.GetType() + "] returned null certificate for server name " + (tlsData.ServerName == null ? "null" : ("\"" + tlsData.ServerName + "\"")) + ". Client IP: " + p.RemoteIPAddressStr);
							return false;
						}
						SslStream ss = WrapSslStream(p);
#if NET6_0
						SslServerAuthenticationOptions sslServerOptions = new SslServerAuthenticationOptions();
						sslServerOptions.ServerCertificateContext = tlsCertContexts.GetOrAdd(cert, CreateSslStreamCertificateContextFromCert);
						sslServerOptions.EnabledSslProtocols = p.srv.ChooseSslProtocols(p.RemoteIPAddress, SslProtocols.Tls12 | SslProtocols.Tls13);
						sslServerOptions.AllowRenegotiation = false; // Client-side renegotiation is viewed as insecure by the industry and is not available in TLS 1.3.
						if (HttpServerBase.IsTlsCipherSuitesPolicySupported())
						{
							IEnumerable<TlsCipherSuite> suites = p.srv.GetAllowedCipherSuites(p);
							if (suites != null)
								sslServerOptions.CipherSuitesPolicy = new CipherSuitesPolicy(suites);
						}

						await TaskHelper.DoWithTimeout(ss.AuthenticateAsServerAsync(sslServerOptions, cancellationToken), HttpProcessor.readTimeoutSeconds * 1000).ConfigureAwait(false);
#else
						await TaskHelper.DoWithCancellation(ss.AuthenticateAsServerAsync(cert, false, p.srv.ChooseSslProtocols(p.RemoteIPAddress, Tls13 | SslProtocols.Tls12), false), HttpProcessor.readTimeoutSeconds * 1000, cancellationToken).ConfigureAwait(false);
#endif
						SimpleHttpLogger.LogVerbose("Client connected using SslProtocol." + (p.tcpStream as SslStream).SslProtocol + ". Client IP: " + p.RemoteIPAddressStr);
					}
					else
					{
						if (!p.allowedConnectionTypes.HasFlag(AllowedConnectionTypes.http))
						{
							SimpleHttpLogger.LogVerbose("Client requested plain HTTP from an IP endpoint (" + p.tcpClient.Client.LocalEndPoint.ToString() + ") that is not configured to support plain HTTP. Client IP: " + p.RemoteIPAddressStr);
							return false;
						}
					}
				}
				catch (OperationCanceledException) { return false; }
				catch (ThreadAbortException) { throw; }
				catch (SocketException) { throw; }
				catch (Exception ex)
				{
					if (ex is AuthenticationException)
					{
						AuthenticationException aex = (AuthenticationException)ex;
						if (ex.InnerException is Win32Exception)
						{
							Win32Exception wex = (Win32Exception)ex.InnerException;
							if (wex.NativeErrorCode == unchecked((int)0x80090327))
							{
								// Happens unpredictably to some self-signed certificates when used with some clients.
								SimpleHttpLogger.LogVerbose("SslStream.AuthenticateAsServer --> An unknown error occurred while processing the certificate.");
								return false;
							}
						}
					}
					SimpleHttpLogger.LogVerbose(ex, "Client IP: " + p.RemoteIPAddressStr);
					return false;
				}
			}
			p.base_uri_this_server = new Uri("http" + (p.secure_https ? "s" : "") + "://" + p.tcpClient.Client.LocalEndPoint.ToString(), UriKind.Absolute);
			return true;
		}
		private static SslStream WrapSslStream(HttpProcessor p)
		{
			SslStream sslStream = new SslStream(p.tcpStream, false, null, null, EncryptionPolicy.RequireEncryption);
			p.tcpStream = sslStream;
			return sslStream;
		}
	}
}
