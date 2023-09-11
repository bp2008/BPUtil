using BPUtil.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp.Client
{
	/// <summary>
	/// Provides advanced web proxying capability.
	/// </summary>
	public class ProxyClient : IDisposable
	{
		/// <summary>
		/// Web origin which this client is bound to, all lower case. e.g. "https://example.com"
		/// </summary>
		public readonly string Origin;
		/// <summary>
		/// The TcpClient representing the connection to the remote server.
		/// </summary>
		private TcpClient proxyClient;
		/// <summary>
		/// The bidirectional data stream from the connection to the remote server.
		/// </summary>
		private UnreadableStream proxyStream;
		/// <summary>
		/// When this is Finished, the ProxyClient is expired and should be disposed.
		/// </summary>
		private CountdownStopwatch expireTimer = CountdownStopwatch.StartNew(TimeSpan.FromMinutes(60));
		/// <summary>
		/// A string describing details of the last request handled by this ProxyClient, for debugging purposes.
		/// </summary>
		private string lastRequestDetails;

		/// <summary>
		/// Constructs a ProxyClient and binds it to the specified origin.
		/// </summary>
		/// <param name="origin">The ProxyClient will only be able to send requests to this origin.</param>
		private ProxyClient(string origin)
		{
			Origin = origin;
		}
		private static string GetOriginLower(Uri uri)
		{
			return (uri.Scheme + "://" + uri.DnsSafeHost + (uri.IsDefaultPort ? "" : (":" + uri.Port))).ToLower();
		}
		private static ConcurrentDictionary<string, ConcurrentQueue<ProxyClient>> poolsByOrigin = new ConcurrentDictionary<string, ConcurrentQueue<ProxyClient>>();
		private static ConcurrentQueue<ProxyClient> createNewPool(string origin)
		{
			return new ConcurrentQueue<ProxyClient>();
		}
		/// <summary>
		/// Serves as a reverse proxy, sending the request to the given uri. This static method utilizes connection pooling in order to be more efficient.
		/// </summary>
		/// <param name="p">HttpProcessor instance that is requesting the proxied connection.</param>
		/// <param name="uri">URI that should be requested.</param>
		/// <param name="options">Optional options to control the behavior of the proxy request.</param>
		/// <returns></returns>
		public static async Task ProxyRequest(HttpProcessor p, Uri uri, ProxyOptions options = null)
		{
			if (options == null)
				options = new ProxyOptions();
			try
			{
				options.bet?.Start("ProxyRequest Start");

				options.log.AppendLine("ProxyClient.ProxyRequest(\"" + uri + "\")");
				string origin = GetOriginLower(uri);
				ConcurrentQueue<ProxyClient> pool = poolsByOrigin.GetOrAdd(origin, createNewPool);

				int numTries = 0;
				while (true)
				{
					numTries++;
					ProxyClient client = null;
					try
					{
						if (!pool.TryDequeue(out client))
						{
							client = new ProxyClient(origin);
							options.log.AppendLine("Using New ProxyClient");
							options.bet?.Start("ProxyRequest BeginProxyRequestAsync #" + numTries + " with new client");
						}
						else
						{
							if (client.expireTimer.Finished)
							{
								options.log.AppendLine("Removing Expired ProxyClient");
								continue;
							}
							options.log.AppendLine("Using Existing ProxyClient");
							options.bet?.Start("ProxyRequest BeginProxyRequestAsync #" + numTries + " with existing client");
						}

						ProxyResult result = null;
						try
						{
							options.cancellationToken.ThrowIfCancellationRequested();
							result = await client.BeginProxyRequestAsync(p, uri, options).ConfigureAwait(false);
						}
						catch (OperationCanceledException)
						{
							break;
						}
						catch (Exception ex)
						{
							if (HttpProcessor.IsOrdinaryDisconnectException(ex))
								options.log.AppendLine("Unexpected disconnection!");
							else
								options.log.AppendLine("ERROR: Exception occurred: " + ex.FlattenMessages());
							ex.Rethrow();
						}
						options.bet?.Start("ProxyRequest analyze result #" + numTries + ": " + result.ErrorCode);

						if (result.IsProxyClientReusable && pool.Count < 128)
						{
							pool.Enqueue(client);
							client = null;
							options.log.AppendLine("Recycling ProxyClient for future use.");
						}
						if (result.Success)
							break;
						else
						{
							if (!result.ShouldTryAgainWithAnotherConnection)
							{
								options.log.AppendLine("ERROR: ProxyRequest to uri \"" + uri.ToString() + "\" failed with error: " + result.ErrorMessage);
								break;
							}
						}
					}
					finally
					{
						client?.Dispose();
					}
				}
			}
			finally
			{
				//Logger.Info("Request ID " + options.RequestId + ":" + Environment.NewLine
				//	+ options.log.ToString()
				//	+ (options.bet == null ? "" : (Environment.NewLine + Environment.NewLine + options.bet.ToString(Environment.NewLine))));
			}
		}
		/// <summary>
		/// Polls the socket to see if it has closed.
		/// </summary>
		/// <returns></returns>
		private bool CheckIfStillConnected()
		{
			if (proxyClient == null || !proxyClient.Connected)
				return false;
			bool readable = proxyClient.Client.Poll(0, SelectMode.SelectRead);
			if (readable)
			{
				// data is available for reading OR connection is closed.
				byte[] buffer = new byte[1];
				if (proxyClient.Client.Receive(buffer, SocketFlags.Peek) == 0)
					return false;
				// Data was available, connection may not be closed.
				bool writable = proxyClient.Client.Poll(0, SelectMode.SelectWrite);
				bool errored = proxyClient.Client.Poll(0, SelectMode.SelectError);
				return writable && !errored;
			}
			else
			{
				return true; // The read poll returned false, so the connection is supposedly open with no data available to read, which is the normal state.
			}
		}
		/// <summary>
		/// Serves as a reverse proxy, sending the request to the given uri.  This method is meant to be used internally by a wrapper that provides connection reuse.
		/// </summary>
		/// <param name="p">HttpProcessor instance that is requesting the proxied connection.</param>
		/// <param name="uri">The Uri which the request should be forwarded to.</param>
		/// <param name="options">Optional options to control the behavior of the proxy request.</param>
		/// <returns></returns>
		internal async Task<ProxyResult> BeginProxyRequestAsync(HttpProcessor p, Uri uri, ProxyOptions options = null)
		{
			string origin = GetOriginLower(uri);
			if (Origin != origin)
				throw new ApplicationException("This ProxyClient is bound to a different web origin than the requested Uri.");

			if (p.Response.ResponseHeaderWritten)
				throw new ApplicationException("This ProxyClient is unable to complete the current request because a response has already been written to the client making this request.");

			// This would show up occasionally in the admin console dashboard and be confusing:
			//p.Response.Simple("500 Internal Server Error", "This server had an unexpected failure when attempting to proxy the request.");

			if (options == null)
				options = new ProxyOptions();

			BasicEventTimer proxyTiming = null;
			if (options.includeServerTimingHeader)
				proxyTiming = new BasicEventTimer();

			string host = options.host;
			ProxyDataBuffer snoopy = options.snoopy;

			// Interally, we shall refer to the original client making the request to this application as "the client", while the destination URI points at "the server".

			if (string.IsNullOrWhiteSpace(host))
				host = uri.DnsSafeHost;

			p.tcpClient.NoDelay = true;

			///////////////////////////
			// PHASE 1: ANALYZE REQUEST //
			///////////////////////////
			// Send the original client's request to the remote server with any necessary modifications.

			// Collection of lower case header names that are "hop-by-hop" and should not be proxied.  Values in the "Connection" header are supposed to be added to this collection on a per-request basis, but this server does not currently recognize comma separated values in the Connection header, therefore that is not happening.
			HashSet<string> doNotProxyHeaders = new HashSet<string>(new string[] {
				"keep-alive", "transfer-encoding", "te", "connection", "trailer", "upgrade", "proxy-authorization", "proxy-authenticate", "host"
			});

			// Figure out the Connection header
			bool ourClientWantsConnectionUpgrade = false;
			if (p.Request.ConnectionHeaderValues != null)
			{
				if (p.Request.ConnectionHeaderValues.Contains("upgrade", true))
					ourClientWantsConnectionUpgrade = true;
			}

			string requestHeader_Connection;
			if (ourClientWantsConnectionUpgrade)
				requestHeader_Connection = "upgrade";
			else
			{
				if (options.allowConnectionKeepalive && !p.ServerIsUnderHighLoad)
					requestHeader_Connection = "keep-alive";
				else
					requestHeader_Connection = "close";
			}

			bool ourClientWantsWebsocket = ourClientWantsConnectionUpgrade && "websocket".IEquals(p.Request.Headers.Get("Upgrade"));

			string requestHeader_Upgrade = ourClientWantsWebsocket ? "websocket" : null;

			//////////////////////
			// PHASE 2: CONNECT //
			//////////////////////
			bool didConnect = false;
			if (proxyClient == null)
			{
				options.log.AppendLine("Constructing new TcpClient");
				try
				{
					await Connect(uri, host, options, proxyTiming).ConfigureAwait(false);
					didConnect = true;
				}
				catch (GatewayTimeoutException ex)
				{
					if (options.allowGatewayTimeoutResponse)
					{
						p.Response.Simple("504 Gateway Timeout");
						p.Response.SetServerTiming(proxyTiming);
						return new ProxyResult(ProxyResultErrorCode.GatewayTimeout, ex.ToHierarchicalString(), false, false);
					}
					ex.Rethrow();
				}
				catch (TLSNegotiationFailedException ex)
				{
					p.Response.Simple("502 Bad Gateway");
					p.Response.SetServerTiming(proxyTiming);
					return new ProxyResult(ProxyResultErrorCode.TLSNegotiationError, ex.ToHierarchicalString(), false, false);
				}
			}
			else
				options.log.AppendLine("Reusing old connection");

			// Connection to remote server is now established and ready for data transfer.
			///////////////////////////
			// PHASE 3: SEND REQUEST //
			///////////////////////////
			// Send the first line of our HTTP request.
			proxyTiming?.Start("Send Request");
			options.bet?.Start("Send Request");
			StringBuilder sbRequestText = new StringBuilder();
			string requestLine = p.Request.HttpMethod + ' ' + p.Request.Url.PathAndQuery + ' ' + p.Request.HttpProtocolVersionString;
			sbRequestText.AppendLineRN(requestLine);
			// After the first line comes the request headers.
			string outgoingHostHeader = host;
			if (!uri.IsDefaultPort)
				outgoingHostHeader += ":" + uri.Port;
			sbRequestText.AppendLineRN("Host: " + outgoingHostHeader);
			sbRequestText.AppendLineRN("Connection: " + requestHeader_Connection);
			if (requestHeader_Upgrade != null)
				sbRequestText.AppendLineRN("Upgrade: " + requestHeader_Upgrade);
			bool sendRequestChunked = p.Request.RequestBodyStream is ReadableChunkedTransferEncodingStream;
			if (sendRequestChunked)
				sbRequestText.AppendLineRN("Transfer-Encoding: chunked");
			ProcessProxyHeaders(p, options); // Manipulate X-Forwarded-For, etc.

			options.RaiseBeforeRequestHeadersSent(this, p);

			foreach (HttpHeader header in p.Request.Headers)
			{
				if (!doNotProxyHeaders.Contains(header.Key, true))
					sbRequestText.AppendLineRN(header.Key + ": " + header.Value);
			}
			// Now a blank line to indicate the end of the request headers.
			sbRequestText.AppendLineRN("");

			try
			{
				await _ProxyStringAsync(ProxyDataDirection.RequestToServer, proxyStream, sbRequestText.ToString(), snoopy, options.networkTimeoutMs, options.cancellationToken).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				if (!didConnect && ex.GetExceptionOfType<SocketException>() != null)
				{
					options.log.AppendLine("Recycled proxy stream EOF while sending request to remote server. Will retry.");
					return new ProxyResult(ProxyResultErrorCode.ConnectionLost, "ProxyClient encountered unexpected SocketException while sending the request to the remote server. Should retry with a new connection.", false, true);
				}
				ex.Rethrow();
			}
			// Write the original request body if there was one.
			if (p.Request.RequestBodyStream != null)
			{
				Stream streamToWrite = proxyStream;
				if (sendRequestChunked)
					streamToWrite = new WritableChunkedTransferEncodingStream(proxyStream);

				await CopyStreamUntilClosedAsync(ProxyDataDirection.RequestToServer, p.Request.RequestBodyStream, streamToWrite, snoopy, options.networkTimeoutMs, options.cancellationToken).ConfigureAwait(false);

				if (sendRequestChunked)
					await ((WritableChunkedTransferEncodingStream)streamToWrite).CloseAsync(options.cancellationToken).ConfigureAwait(false);
			}

			await proxyStream.FlushAsync(options.cancellationToken).ConfigureAwait(false);

			////////////////////////////
			// PHASE 4: READ RESPONSE //
			////////////////////////////
			// Read response from remote server
			proxyTiming?.Start("Read Response Header");
			options.bet?.Start("Read Response Header");
			HttpResponseStatusLine responseStatusLine = null;
			HttpHeaderCollection proxyHttpHeaders = new HttpHeaderCollection();
			try
			{
				List<string> responseHeaderLines = await SimpleHttpRequest.ReadHttpHeaderSectionAsync(proxyStream, options.networkTimeoutMs, options.cancellationToken).ConfigureAwait(false);
				if (responseHeaderLines == null || responseHeaderLines.Count == 0)
				{
					if (!didConnect)
						return new ProxyResult(ProxyResultErrorCode.ConnectionLost, "ProxyClient encountered end of stream reading response status line from the remote server. Should retry with a new connection.", false, true);
					p.Response.Simple("502 Bad Gateway");
					p.Response.SetServerTiming(proxyTiming);
					return new ProxyResult(ProxyResultErrorCode.TLSNegotiationError, "ProxyClient encountered end of stream reading response status line from the remote server (this was a new connection).", false, false);
				}

				try
				{
					responseStatusLine = new HttpResponseStatusLine(responseHeaderLines[0]);
				}
				catch (Exception ex)
				{
					string message = "Response status line was invalid.";
					if (lastRequestDetails == null)
						message += Environment.NewLine + "This ProxyClient has not been used for any previous requests.";
					else
						message += Environment.NewLine + "This probably happened because of the PREVIOUS REQUEST handled by this ProxyClient, which was: " + lastRequestDetails;
					throw new ApplicationException(message, ex);
				}

				// Parse response headers from remote server
				SimpleHttpRequest.ParseHeaders(proxyHttpHeaders, responseHeaderLines);
			}
			catch (HttpProcessor.EndOfStreamException)
			{
				if (!didConnect)
				{
					options.log.AppendLine("Recycled proxy stream EOF while reading response from remote server. Will retry.");
					return new ProxyResult(ProxyResultErrorCode.ConnectionLost, "ProxyClient encountered unexpected end of stream while reading the response from the remote server. Should retry with a new connection.", false, true);
				}
				p.Response.Simple("502 Bad Gateway");
				p.Response.SetServerTiming(proxyTiming);
				return new ProxyResult(ProxyResultErrorCode.TLSNegotiationError, "ProxyClient encountered unexpected end of stream while reading the response from the remote server (this was a new connection).", false, false);
			}
			catch (TimeoutException ex)
			{
				if (options.allowGatewayTimeoutResponse)
				{
					p.Response.Simple("504 Gateway Timeout");
					p.Response.SetServerTiming(proxyTiming);
					return new ProxyResult(ProxyResultErrorCode.GatewayTimeout, ex.ToHierarchicalString(), false, false);
				}
				ex.Rethrow();
			}
			finally
			{
				proxyTiming?.Stop();
				options.bet?.Stop();
			}

			if (proxyTiming != null)
				proxyHttpHeaders["Server-Timing"] = proxyTiming.ToServerTimingHeader();

			///////////////////////////////
			// PHASE 5: Process Response //
			///////////////////////////////

			// Rewrite redirect location to point at this server
			if (proxyHttpHeaders.ContainsKey("Location"))
			{
				if (!string.IsNullOrWhiteSpace(p.HostName)
					&& Uri.TryCreate(proxyHttpHeaders["Location"], UriKind.Absolute, out Uri redirectUri)
					&& redirectUri.Host.IEquals(host))
				{
					UriBuilder uriBuilder = new UriBuilder(redirectUri);
					uriBuilder.Scheme = p.secure_https ? "https" : "http";
					uriBuilder.Host = p.HostName;
					uriBuilder.Port = ((IPEndPoint)p.tcpClient.Client.LocalEndPoint).Port;
					proxyHttpHeaders["Location"] = uriBuilder.Uri.ToString();
				}
			}

			// Begin populating the Response object
			options.bet?.Start("Proxy Process Response Headers");
			p.Response.Reset(responseStatusLine.StatusString);
			foreach (HttpHeader header in proxyHttpHeaders)
			{
				if (!doNotProxyHeaders.Contains(header.Key, true))
					p.Response.Headers.Add(header);
			}

			// Decide how to respond
			Stream proxyResponseStream = proxyStream;
			bool remoteServerWantsKeepalive = false; // Do NOT set true for WebSocket connections.

			string proxyConnectionHeader;
			if (!proxyHttpHeaders.TryGetValue("Connection", out proxyConnectionHeader))
			{
				if (responseStatusLine.Version.StartsWith("1.0"))
					proxyConnectionHeader = "close";
				else
					proxyConnectionHeader = "keep-alive";
			}
			string[] proxyConnectionHeaderValues = proxyConnectionHeader
				.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(s => s.Trim())
				.ToArray();
			if (proxyConnectionHeaderValues.Contains("upgrade", true) && proxyHttpHeaders.TryGetValue("Upgrade", out string proxyUpgradeHeader) && proxyUpgradeHeader.IEquals("websocket"))
			{
				options.log.AppendLine("This is a websocket connection.");
				p.Response.Headers["Upgrade"] = "websocket";
			}
			else
			{
				remoteServerWantsKeepalive = proxyConnectionHeaderValues.Contains("keep-alive");

				if (!HttpServer.HttpStatusCodeCanHaveResponseBody(responseStatusLine.StatusCodeInt))
					proxyResponseStream = null;
				else if (proxyHttpHeaders.TryGetValue("Content-Length", out string proxyContentLengthStr) && long.TryParse(proxyContentLengthStr, out long proxyContentLength) && proxyContentLength > -1)
					proxyResponseStream = proxyResponseStream.Substream(proxyContentLength);
				else
				{
					if (proxyHttpHeaders.TryGetValue("Transfer-Encoding", out string proxyTransferEncoding))
					{
						options.log.AppendLine("Remote server specified Transfer-Encoding: " + proxyTransferEncoding);
						string[] proxyTransferEncodingValues = proxyTransferEncoding
							.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
							.Select(s => s.Trim())
							.ToArray();
						if (proxyTransferEncodingValues.Contains("chunked", true))
						{
							// Transfer-Encoding: chunked
							proxyResponseStream = new ReadableChunkedTransferEncodingStream(proxyResponseStream);
							if (proxyTransferEncodingValues.Length > 1)
								options.log.AppendLine("WARNING: Transfer-Encoding is not recognized fully: " + proxyTransferEncoding);
						}
						else
							options.log.AppendLine("WARNING: Transfer-Encoding is not recognized: " + proxyTransferEncoding);
					}
					if (proxyResponseStream == proxyStream && remoteServerWantsKeepalive)
					{
						proxyResponseStream = null;
						remoteServerWantsKeepalive = false;
					}
				}
			}
			if (p.Request.HttpMethod == "HEAD")
				proxyResponseStream = null;

			options.log.AppendLine("Response will be read as: " + proxyResponseStream?.GetType().Name ?? "[no response body]");

			options.RaiseBeforeResponseHeadersSent(this, p);

			/////////////////////////////
			// PHASE 6: WRITE RESPONSE //
			/////////////////////////////
			options.bet?.Start("Proxy Write Response: " + proxyResponseStream?.GetType().Name ?? "[no response body]");

			// Work with the response object to create the response header and create a response stream we can use to write a response body.
			byte[] responseHeaderBytes = p.Response.PrepareForProxy(out Stream outgoingStream);

			// Write the response header.
			await _ProxyDataAsync(ProxyDataDirection.ResponseFromServer, p.tcpStream, responseHeaderBytes, responseHeaderBytes.Length, snoopy, options.networkTimeoutMs, options.cancellationToken).ConfigureAwait(false);

			// Flush the tcpStream to ensure that future writes to outgoingStream are not out of order.
			if (outgoingStream != p.tcpStream)
				await p.tcpStream.FlushAsync(options.cancellationToken).ConfigureAwait(false);

			if (p.Response.Headers["Upgrade"] == "websocket")
			{
				// Asynchronously proxy additional incoming data to the remote server (do not await)
				_ = CopyStreamUntilClosedAsync(ProxyDataDirection.RequestToServer, p.tcpStream, proxyStream, snoopy, options.networkTimeoutMs, options.cancellationToken);
				// Later code will handle proxying the response from the remote server to our client.
			}
			if (proxyResponseStream != null)
				await CopyStreamUntilClosedAsync(ProxyDataDirection.ResponseFromServer, proxyResponseStream, outgoingStream, snoopy, options.networkTimeoutMs, options.cancellationToken).ConfigureAwait(false);

			await p.Response.FinishAsync(options.cancellationToken).ConfigureAwait(false);

			//////////////////////
			// PHASE 7: CLEANUP //
			//////////////////////
			options.bet?.Start("Proxy Finish");
			if (snoopy != null)
			{
				Directory.CreateDirectory(Globals.WritableDirectoryBase + "ProxyDebug");
				using (FileStream fs = new FileStream(Globals.WritableDirectoryBase + "ProxyDebug/" + options.RequestId + ".txt", FileMode.Create, FileAccess.Write, FileShare.Read))
				{
					byte[] buf;
					buf = ByteUtil.Utf8NoBOM.GetBytes("***** REQUEST *****\r\n");
					fs.Write(buf, 0, buf.Length);
					buf = snoopy.GetRequestBytes();
					fs.Write(buf, 0, buf.Length);
					buf = ByteUtil.Utf8NoBOM.GetBytes("\r\n***** RESPONSE *****\r\n");
					fs.Write(buf, 0, buf.Length);
					buf = snoopy.GetResponseBytes();
					fs.Write(buf, 0, buf.Length);
				}
			}

			if (remoteServerWantsKeepalive && options.allowConnectionKeepalive)
			{
				int seconds = 60;
				if (proxyHttpHeaders.TryGetValue("keep-alive", out string keepAliveStr))
				{
					KeepAliveHeader keepAliveHeader = new KeepAliveHeader(keepAliveStr);
					if (keepAliveHeader.Timeout != null)
					{
						seconds = keepAliveHeader.Timeout.Value.Clamp(0, 60);
						if (seconds < 1)
							remoteServerWantsKeepalive = false;
					}
				}
				expireTimer = CountdownStopwatch.StartNew(TimeSpan.FromSeconds(seconds));
			}
			lastRequestDetails = p.TrueRemoteIPAddress + " -> " + p.HostName + " " + requestLine;
			return new ProxyResult(ProxyResultErrorCode.Success, null, remoteServerWantsKeepalive && options.allowConnectionKeepalive && !p.ServerIsUnderHighLoad, false);
		}
		/// <summary>
		/// Connects to the given URI and sets the <see cref="proxyClient"/> and <see cref="proxyStream"/> fields of this ProxyClient.
		/// </summary>
		/// <param name="uri">URI to connect to.</param>
		/// <param name="host">Hostname for TLS Server Name Indication, should match the hostname used later in the Host header.</param>
		/// <param name="options">Options</param>
		/// <param name="proxyTiming">Possibly null, this BasicEventTimer is used to log timing of each step of the connection process.</param>
		/// <returns></returns>
		private async Task Connect(Uri uri, string host, ProxyOptions options, BasicEventTimer proxyTiming)
		{
			try
			{
				proxyTiming?.Start("DNS Lookup");
				options.bet?.Start("DNS Lookup");
				IPAddress ip = await DnsHelper.GetHostAddressAsync(uri.DnsSafeHost).ConfigureAwait(false);

				proxyClient = new TcpClient(ip.AddressFamily); // Create TcpClient with IPv4 or IPv6 support as needed. We are unable to construct one that can use both.
				proxyClient.NoDelay = true;
				Stream pStream = null;
				try
				{
					proxyTiming?.Start("Connect");
					options.bet?.Start("Connect");
#if NET6_0

					await TaskHelper.DoWithTimeout(proxyClient.ConnectAsync(ip, uri.Port, options.cancellationToken).AsTask(), options.connectTimeoutMs,
						() => throw new GatewayTimeoutException("ProxyClient outgoing connection attempt timed out.")).ConfigureAwait(false);
#else
					await TaskHelper.DoWithCancellation(proxyClient.ConnectAsync(ip, uri.Port), options.connectTimeoutMs, options.cancellationToken,
						(timeout) =>
						{
							if (timeout)
								throw new GatewayTimeoutException("ProxyClient outgoing connection attempt timed out.");
							throw new OperationCanceledException();
						}).ConfigureAwait(false);
#endif
					pStream = proxyClient.GetStream();
				}
				catch (Exception ex)
				{
					// This was a fresh connection attempt and it failed.
					proxyClient = null;
					proxyStream = null;
					throw new GatewayTimeoutException("ProxyClient failed to connect to the remote host: " + uri.DnsSafeHost + ":" + uri.Port, ex);
				}
				try
				{
					if (uri.Scheme.IEquals("https"))
					{
						proxyTiming?.Start("TLS Negotiate");
						options.bet?.Start("TLS Negotiate");
						RemoteCertificateValidationCallback certCallback = null;
						if (options.acceptAnyCert)
							certCallback = (sender, certificate, chain, sslPolicyErrors) => true;
						SslStream ss = new SslStream(pStream, false, certCallback, null);
						pStream = ss;
#if NET6_0
						SslClientAuthenticationOptions sslClientOptions = new SslClientAuthenticationOptions();
						sslClientOptions.AllowRenegotiation = false; // Client-side renegotiation is viewed as insecure by the industry and is not available in TLS 1.3.
						sslClientOptions.EnabledSslProtocols = TLS.TlsNegotiate.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls;
						sslClientOptions.EncryptionPolicy = EncryptionPolicy.RequireEncryption;
						if (certCallback != null)
						{
							sslClientOptions.CertificateRevocationCheckMode = System.Security.Cryptography.X509Certificates.X509RevocationMode.NoCheck;
							sslClientOptions.RemoteCertificateValidationCallback = certCallback;
						}
						sslClientOptions.TargetHost = host;

						await TaskHelper.DoWithTimeout(ss.AuthenticateAsClientAsync(sslClientOptions, options.cancellationToken), options.connectTimeoutMs,
						() => throw new GatewayTimeoutException("ProxyClient TLS Client Negotiation attempt timed out.")).ConfigureAwait(false);
#else
						Task authTask = ss.AuthenticateAsClientAsync(host, null, TLS.TlsNegotiate.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, false);
						await TaskHelper.DoWithCancellation(authTask, options.connectTimeoutMs, options.cancellationToken,
							timedOut =>
							{
								if (timedOut)
									throw new GatewayTimeoutException("ProxyClient TLS Client Negotiation attempt timed out.");
								throw new OperationCanceledException("ProxyClient TLS Client Negotiation attempt was cancelled.");
							}).ConfigureAwait(false);
#endif
					}
				}
				catch (Exception ex)
				{
					proxyClient = null;
					proxyStream = null;
					throw new TLSNegotiationFailedException("TLS Negotiation failed with remote host: " + uri.DnsSafeHost + ":" + uri.Port, ex);
				}
				proxyStream = new UnreadableStream(pStream, false);
			}
			finally
			{
				proxyTiming?.Stop();
				options.bet?.Stop();
			}
		}
		#region ProxyHeaders
		protected const string XFF = "X-Forwarded-For";
		protected const string XFH = "X-Forwarded-Host";
		protected const string XFP = "X-Forwarded-Proto";
		protected const string XRI = "X-Real-Ip";
		/// <summary>
		/// According to the given options, manipulates the headers: "X-Forwarded-For", "X-Forwarded-Proto", "X-Forwarded-Host", "X-Real-Ip".
		/// </summary>
		/// <param name="p">HttpProcessor</param>
		/// <param name="options">Options which determine how the headers are manipulated.</param>
		protected void ProcessProxyHeaders(HttpProcessor p, ProxyOptions options)
		{
			// Only "X-Forwarded-For" supports the concept of "Combine", so everything else will treated "Combine" as equivalent to "Create".

			// X-Forwarded-For
			switch (options.xForwardedFor)
			{
				case ProxyHeaderBehavior.Drop:
					p.Request.Headers.Remove(XFF);
					break;
				case ProxyHeaderBehavior.Create:
					p.Request.Headers[XFF] = p.TrueRemoteIPAddress.ToString();
					break;
				case ProxyHeaderBehavior.CombineUnsafe:
					p.Request.Headers.Add(XFF, p.TrueRemoteIPAddress.ToString());
					break;
				case ProxyHeaderBehavior.CombineIfTrustedElseCreate:
					if (IPAddressRange.WhitelistCheck(p.TrueRemoteIPAddress, options.proxyHeaderTrustedIpRanges))
						p.Request.Headers.Add(XFF, p.TrueRemoteIPAddress.ToString());
					else
						p.Request.Headers[XFF] = p.TrueRemoteIPAddress.ToString();
					break;
				case ProxyHeaderBehavior.PassthroughUnsafe:
					break;
				case ProxyHeaderBehavior.PassthroughIfTrustedElseDrop:
					if (!IPAddressRange.WhitelistCheck(p.TrueRemoteIPAddress, options.proxyHeaderTrustedIpRanges))
						p.Request.Headers.Remove(XFF);
					break;
				case ProxyHeaderBehavior.PassthroughIfTrustedElseCreate:
					if (!IPAddressRange.WhitelistCheck(p.TrueRemoteIPAddress, options.proxyHeaderTrustedIpRanges))
						p.Request.Headers[XFF] = p.TrueRemoteIPAddress.ToString();
					break;
				default:
					throw new Exception("Unhandled options.xForwardedFor: " + options.xForwardedFor);
			}

			// X-Forwarded-Proto
			switch (options.xForwardedProto)
			{
				case ProxyHeaderBehavior.Drop:
					p.Request.Headers.Remove(XFP);
					break;
				case ProxyHeaderBehavior.Create:
				case ProxyHeaderBehavior.CombineUnsafe:
				case ProxyHeaderBehavior.CombineIfTrustedElseCreate:
					p.Request.Headers[XFP] = p.secure_https ? "https" : "http";
					break;
				case ProxyHeaderBehavior.PassthroughUnsafe:
					break;
				case ProxyHeaderBehavior.PassthroughIfTrustedElseDrop:
					if (!IPAddressRange.WhitelistCheck(p.TrueRemoteIPAddress, options.proxyHeaderTrustedIpRanges))
						p.Request.Headers.Remove(XFP);
					break;
				case ProxyHeaderBehavior.PassthroughIfTrustedElseCreate:
					if (!IPAddressRange.WhitelistCheck(p.TrueRemoteIPAddress, options.proxyHeaderTrustedIpRanges))
						p.Request.Headers[XFP] = p.secure_https ? "https" : "http";
					break;
				default:
					throw new Exception("Unhandled options.xForwardedProto: " + options.xForwardedProto);
			}

			// X-Forwarded-Host
			switch (options.xForwardedHost)
			{
				case ProxyHeaderBehavior.Drop:
					p.Request.Headers.Remove(XFH);
					break;
				case ProxyHeaderBehavior.Create:
				case ProxyHeaderBehavior.CombineUnsafe:
				case ProxyHeaderBehavior.CombineIfTrustedElseCreate:
					p.Request.Headers.Set(XFH, p.Request.Headers.Get("Host") ?? "undefined");
					break;
				case ProxyHeaderBehavior.PassthroughUnsafe:
					break;
				case ProxyHeaderBehavior.PassthroughIfTrustedElseDrop:
					if (!IPAddressRange.WhitelistCheck(p.TrueRemoteIPAddress, options.proxyHeaderTrustedIpRanges))
						p.Request.Headers.Remove(XFH);
					break;
				case ProxyHeaderBehavior.PassthroughIfTrustedElseCreate:
					if (!IPAddressRange.WhitelistCheck(p.TrueRemoteIPAddress, options.proxyHeaderTrustedIpRanges))
						p.Request.Headers.Set(XFH, p.Request.Headers.Get("Host") ?? "undefined");
					break;
				default:
					throw new Exception("Unhandled options.xForwardedHost: " + options.xForwardedHost);
			}

			// X-Real-IP
			switch (options.xRealIp)
			{
				case ProxyHeaderBehavior.Drop:
					p.Request.Headers.Remove(XRI);
					break;
				case ProxyHeaderBehavior.Create:
				case ProxyHeaderBehavior.CombineUnsafe:
				case ProxyHeaderBehavior.CombineIfTrustedElseCreate:
					p.Request.Headers[XRI] = p.TrueRemoteIPAddress.ToString();
					break;
				case ProxyHeaderBehavior.PassthroughUnsafe:
					break;
				case ProxyHeaderBehavior.PassthroughIfTrustedElseDrop:
					if (!IPAddressRange.WhitelistCheck(p.TrueRemoteIPAddress, options.proxyHeaderTrustedIpRanges))
						p.Request.Headers.Remove(XRI);
					break;
				case ProxyHeaderBehavior.PassthroughIfTrustedElseCreate:
					if (!IPAddressRange.WhitelistCheck(p.TrueRemoteIPAddress, options.proxyHeaderTrustedIpRanges))
						p.Request.Headers[XRI] = p.TrueRemoteIPAddress.ToString();
					break;
				default:
					throw new Exception("Unhandled options.xRealIp: " + options.xRealIp);
			}
		}
		#endregion
		#region Helpers
		private enum ProxyResponseDecision
		{
			/// <summary>
			/// No decision has been made.
			/// </summary>
			Undefined,
			/// <summary>
			/// The request should be proxied for the number of bytes specified by the "Content-Length" header.
			/// </summary>
			ContentLength,
			/// <summary>
			/// The remote server is going to deliver the response using "Transfer-Encoding: chunked", so we can use that to identify when the response is complete.
			/// </summary>
			ReadChunked,
			/// <summary>
			/// The request should be proxied until the connection to the remote server is closed.
			/// </summary>
			UntilClosed,
			/// <summary>
			/// The request should be proxied as a WebSocket.
			/// </summary>
			Websocket,
			/// <summary>
			/// The request should be considered fully proxied after the response headers are written.
			/// </summary>
			NoBody
		}
		private static async Task _ProxyStringAsync(ProxyDataDirection Direction, Stream target, string str, ProxyDataBuffer snoopy, int ioTimeoutMilliseconds, CancellationToken cancellationToken = default)
		{
			if (snoopy != null)
				snoopy.AddItem(new ProxyDataItem(Direction, str));
			byte[] buf = ByteUtil.Utf8NoBOM.GetBytes(str);
			using (CancellationTokenSource ctsTimeout = new CancellationTokenSource(ioTimeoutMilliseconds))
			using (CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ctsTimeout.Token))
				await target.WriteAsync(buf, 0, buf.Length, cts.Token).ConfigureAwait(false);
		}
		private static async Task _ProxyDataAsync(ProxyDataDirection Direction, Stream target, byte[] buf, int length, ProxyDataBuffer snoopy, int ioTimeoutMilliseconds, CancellationToken cancellationToken = default)
		{
			if (snoopy != null)
			{
				ProxyDataItem item;
				if (buf.Length != length)
					item = new ProxyDataItem(Direction, ByteUtil.SubArray(buf, 0, length));
				else
					item = new ProxyDataItem(Direction, buf);
				snoopy?.AddItem(item);
			}
			using (CancellationTokenSource ctsTimeout = new CancellationTokenSource(ioTimeoutMilliseconds))
			using (CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ctsTimeout.Token))
				await target.WriteAsync(buf, 0, length, cts.Token).ConfigureAwait(false);
		}

		private static async Task CopyStreamUntilClosedAsync(ProxyDataDirection Direction, Stream source, Stream target, ProxyDataBuffer snoopy, int ioTimeoutMilliseconds, CancellationToken cancellationToken = default)
		{
			byte[] buf = ByteUtil.BufferGet();
			try
			{
				int read = 1;
				while (read > 0)
				{
					read = await source.ReadAsync(buf, 0, buf.Length, cancellationToken).ConfigureAwait(false);
					if (read > 0)
						await _ProxyDataAsync(Direction, target, buf, read, snoopy, ioTimeoutMilliseconds, cancellationToken).ConfigureAwait(false);
				}
			}
			finally
			{
				ByteUtil.BufferRecycle(buf);
			}
		}
		/// <summary>
		/// Thrown by ProxyClient in several situations where a timeout occurs attempting to contact the destination server.
		/// </summary>
		[Serializable]
		private class GatewayTimeoutException : Exception
		{
			/// <summary>
			/// Thrown by ProxyClient in several situations where a timeout occurs attempting to contact the destination server.
			/// </summary>
			public GatewayTimeoutException()
			{
			}

			/// <summary>
			/// Thrown by ProxyClient in several situations where a timeout occurs attempting to contact the destination server.
			/// </summary>
			public GatewayTimeoutException(string message) : base(message)
			{
			}

			/// <summary>
			/// Thrown by ProxyClient in several situations where a timeout occurs attempting to contact the destination server.
			/// </summary>
			public GatewayTimeoutException(string message, Exception innerException) : base(message, innerException)
			{
			}

			/// <summary>
			/// Thrown by ProxyClient in several situations where a timeout occurs attempting to contact the destination server.
			/// </summary>
			protected GatewayTimeoutException(SerializationInfo info, StreamingContext context) : base(info, context)
			{
			}
		}

		[Serializable]
		private class TLSNegotiationFailedException : Exception
		{
			public TLSNegotiationFailedException()
			{
			}

			public TLSNegotiationFailedException(string message) : base(message)
			{
			}

			public TLSNegotiationFailedException(string message, Exception innerException) : base(message, innerException)
			{
			}

			protected TLSNegotiationFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
			{
			}
		}
		/// <summary>
		/// Represents the status line from an HTTP response.
		/// </summary>
		private class HttpResponseStatusLine
		{
			/// <summary>
			/// HTTP version, e.g. "1.1"
			/// </summary>
			public string Version;
			/// <summary>
			/// Status code, e.g. "200" or "404".
			/// </summary>
			public string StatusCode;
			/// <summary>
			/// Status code, e.g. 200 or 404.
			/// </summary>
			public int StatusCodeInt;
			/// <summary>
			/// Status text, e.g. "OK" or "Not Found".
			/// </summary>
			public string StatusText;
			/// <summary>
			/// Original string sent into the constructor.
			/// </summary>
			public string OriginalStatusLine;
			/// <summary>
			/// Returns the full status string, e.g. "200 OK" or "404 Not Found".
			/// </summary>
			public string StatusString => StatusCode + (StatusText == null ? "" : " " + StatusText);
			/// <summary>
			/// Constructs an empty HttpResponseStatusLine.
			/// </summary>
			public HttpResponseStatusLine() { }
			/// <summary>
			/// Constructs an HttpResponseStatusLine from a string that was the first line of an HTTP response.
			/// </summary>
			/// <exception cref="ArgumentNullException">Throws if the given HTTP status line is null.</exception>
			/// <exception cref="ArgumentException">Throws if the given HTTP status line is invalid.</exception>
			public HttpResponseStatusLine(string line)
			{
				if (line == null)
					throw new ArgumentNullException(nameof(line));
				OriginalStatusLine = line;
				string[] parts = line.Split(' ');
				if (parts.Length < 3)
					throw new ArgumentException("Invalid HTTP response status line does not contain at least 2 spaces: " + line);
				if (parts[0].StartsWith("HTTP/"))
					Version = parts[0].Substring("HTTP/".Length);
				else
					throw new ArgumentException("Invalid HTTP response status line does begin with \"HTTP/\": " + line);
				StatusCode = parts[1];
				if (int.TryParse(StatusCode, out int statusCodeInt))
					StatusCodeInt = statusCodeInt;
				else
					throw new ArgumentException("Invalid HTTP response status line does not contain integer status code: " + line);
				StatusText = string.Join(" ", parts.Skip(2));
			}
			/// <summary>
			/// Builds the status line from the current <see cref="Version"/>, <see cref="StatusCode"/>, and <see cref="StatusText"/> fields.
			/// </summary>
			/// <returns></returns>
			public override string ToString()
			{
				return "HTTP/" + Version + " " + StatusCode + " " + StatusText;
			}
		}
		public class KeepAliveHeader
		{
			public int? Timeout;
			public int? Max;

			public KeepAliveHeader(string headerValue)
			{
				if (!string.IsNullOrEmpty(headerValue))
				{
					if (int.TryParse(headerValue.Trim(), out int num))
						Timeout = num;
					else
					{
						string[] parts = headerValue.Split(',');
						foreach (string part in parts)
						{
							string[] keyValue = part.Split('=');
							if (keyValue.Length == 2)
							{
								string value = keyValue[1].Trim();
								if (int.TryParse(value, out int v))
								{
									string key = keyValue[0].Trim().ToLower();
									if (key == "timeout")
										Timeout = v;
									else if (key == "max")
										Max = v;
								}
							}
						}
					}
				}
			}
		}

		private bool disposedValue;
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// dispose managed state (managed objects)
					try
					{
						proxyClient?.Close();
					}
					catch (Exception ex)
					{
						Logger.Debug(ex, "ProxyClient.Dispose");
					}
				}

				// free unmanaged resources (unmanaged objects) and override finalizer
				// set large fields to null
				disposedValue = true;
			}
		}
		// // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~ProxyClient()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
