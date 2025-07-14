using BPUtil.IO;
using BPUtil.SimpleHttp.Client;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

// This file has been modified continuously since Nov 10, 2012 by Brian Pearce.
// Based on http://www.codeproject.com/Articles/137979/Simple-HTTP-Server-in-C

// offered to the public domain for any use with no restriction
// and also with no warranty of any kind, please enjoy. - David Jeske. 

// simple HTTP explanation
// http://www.jmarshall.com/easy/http/


namespace BPUtil.SimpleHttp
{
	/// <summary>
	/// Implements the HTTP 1.1 protocol for a given <see cref="TcpClient"/> and <see cref="HttpServerBase"/>.
	/// </summary>
	public class HttpProcessor : IProcessor
	{
		public static UTF8Encoding Utf8NoBOM = new UTF8Encoding(false);
		internal readonly AllowedConnectionTypes allowedConnectionTypes;
		/// <summary>
		/// Read timeout for read attempts, in seconds.  Clients are given this value minus one in the "Keep-Alive: timeout=N" header.
		/// </summary>
		internal const byte readTimeoutSeconds = 5;

		#region Fields and Properties
		/// <summary>
		/// True if this HttpProcessor is using async/await. False if using classic blocking/synchronous APIs.
		/// </summary>
		internal bool IsAsync { get; private set; }
		/// <summary>
		/// The Http Request currently being processed.
		/// </summary>
		public SimpleHttpRequest Request { get; private set; }

		/// <summary>
		/// The Http Response that will be sent.
		/// </summary>
		public SimpleHttpResponse Response { get; private set; }

		/// <summary>
		/// The underlying tcpClient which handles the network connection.
		/// </summary>
		internal TcpClient tcpClient { get; private set; }

		/// <summary>
		/// The HttpServerBase instance that accepted this request.
		/// </summary>
		internal HttpServerBase srv { get; private set; }

		/// <summary>
		/// This stream is for reading and writing binary data.  It is for internal use by BPUtil.
		/// </summary>
		internal Stream tcpStream { get; set; }

		/// <summary>
		/// <para>The base Uri for this server, containing its host name and port.</para>
		/// <para>The host name is provided by the client via TLS Server Name Indication or the Host header.</para>
		/// <para>If the client does not provide a host name, the host name will be the IP address which received the TCP connection (which may be an internal IPv4 address).</para>
		/// </summary>
		public Uri base_uri_this_server { get; internal set; }

		/// <summary>
		/// The base Uri for this server constructed from the local IPEndpoint.  In many cases, this will not match what the user entered in their web browser.
		/// </summary>
		public Uri base_uri_this_server_via_local_endpoint { get; internal set; }

		/// <summary>
		/// <para>Gets true if the HTTP server is believed to be under high load conditions.</para>
		/// <para>In high load conditions, buffers may be smaller and "Connection: keep-alive" may not be allowed.</para>
		/// </summary>
		public bool ServerIsUnderHighLoad { get { return srv.IsServerUnderHighLoad(); } }
		/// <summary>
		/// True if this connection is from a trusted proxy server, meaning the <see cref="TrueRemoteIPAddress"/> is trusted by the web server's <see cref="HttpServerBase.IsTrustedProxyServer"/> method.
		/// </summary>
		public bool IsConnectionViaTrustedProxyServer { get; internal set; }
		/// <summary>
		/// True if the <see cref="secure_https"/> property was set based on the <c>X-Forwarded-Proto</c> header value.
		/// </summary>
		public bool Trusted_XForwardedProtoHeader { get; internal set; }

		#region Properties dealing with the IP Address of the remote host
		private int isLanConnection = -1;
		/// <summary>
		/// Returns true if the remote client's IP address is in the same subnet as any of the server's IP addresses.
		/// </summary>
		public bool IsLanConnection
		{
			get
			{
				if (isLanConnection == -1)
				{
					if (RemoteIPAddress == null)
						isLanConnection = 0;
					else
					{
						NetworkAddressInfo addressInfo = srv.GetAddressInfo();
						isLanConnection = addressInfo.IsSameLAN(RemoteIPAddress) ? 1 : 0;
					}
				}
				return isLanConnection == 1;
			}
		}
		private int isLocalConnection = -1;
		/// <summary>
		/// Returns true if the remote client's IP address is an exact match with any of the server's IP addresses.
		/// </summary>
		public bool IsLocalConnection
		{
			get
			{
				if (isLocalConnection == -1)
				{
					if (RemoteIPAddress == null)
						isLocalConnection = 0;
					else
					{
						NetworkAddressInfo addressInfo = srv.GetAddressInfo();
						isLocalConnection = addressInfo.IsSameMachine(RemoteIPAddress) ? 1 : 0;
					}
				}
				return isLocalConnection == 1;
			}
		}
		private byte[] remoteIPAddressBytes = null;
		private byte[] RemoteIPAddressBytes
		{
			get
			{
				if (remoteIPAddressBytes != null)
					return remoteIPAddressBytes;
				try
				{
					if (RemoteIPAddress != null)
					{
						if (RemoteIPAddress.AddressFamily == AddressFamily.InterNetwork || RemoteIPAddress.AddressFamily == AddressFamily.InterNetworkV6)
							remoteIPAddressBytes = RemoteIPAddress.GetAddressBytes();
					}
				}
				catch (ThreadAbortException) { throw; }
				catch (SocketException) { throw; }
				catch (Exception ex)
				{
					SimpleHttpLogger.Log(ex);
				}
				return remoteIPAddressBytes;
			}
		}
		protected string remoteIPAddressStr = null;
		/// <summary>
		/// Returns the remote client's IP address as a string, or null if the remote IP address is somehow not available.
		/// </summary>
		public string RemoteIPAddressStr
		{
			get
			{
				if (!string.IsNullOrEmpty(remoteIPAddressStr))
					return remoteIPAddressStr;
				try
				{
					if (RemoteIPAddress != null)
						remoteIPAddressStr = RemoteIPAddress.ToString();
				}
				catch (ThreadAbortException) { throw; }
				catch (SocketException) { throw; }
				catch (Exception ex)
				{
					SimpleHttpLogger.Log(ex);
				}
				return remoteIPAddressStr;
			}
		}
		/// <summary>
		/// Returns the remote client's IP address, or null if the remote IP address is somehow not available.
		/// </summary>
		public IPAddress RemoteIPAddress { get; private set; }

		protected uint remoteIPAddressInt = 0;
		/// <summary>
		/// Returns the remote client's IPv4 address as a 32 bit unsigned integer.
		/// </summary>
		[Obsolete("This method does not support IPv6 addresses")]
		public uint RemoteIPAddressInt
		{
			get
			{
				if (remoteIPAddressInt != 0)
					return remoteIPAddressInt;
				try
				{
					byte[] bytes = RemoteIPAddressBytes;
					if (bytes != null && bytes.Length == 4)
						remoteIPAddressInt = BitConverter.ToUInt32(bytes, 0);
				}
				catch (ThreadAbortException) { throw; }
				catch (SocketException) { throw; }
				catch (Exception ex)
				{
					SimpleHttpLogger.Log(ex);
				}
				return remoteIPAddressInt;
			}
		}
		/// <summary>
		/// Gets the true remote IP address of the client, directly from the TcpClient's socket, without regard for HTTP headers from proxy servers. If the address is somehow unavailable, an exception will be thrown.
		/// </summary>
		public IPAddress TrueRemoteIPAddress => RemoteEndPoint.Address;
		#endregion

		/// <summary>
		/// If true, the connection is secured with TLS.  May be overridden by the <c>X-Forwarded-Proto</c> header if the server is configured to trust that header.
		/// </summary>
		public bool secure_https { get; internal set; }

		/// <summary>
		/// If true, the actual network connection is secured with TLS.  See also <see cref="secure_https"/>.
		/// </summary>
		public bool TrueSecureConnection { get; internal set; }

		/// <summary>
		/// An object responsible for delivering TLS server certificates upon demand.
		/// </summary>
		internal ICertificateSelector certificateSelector;

		/// <summary>
		/// The hostname that was requested by the client.  This is populated from TLS Server Name Indication if available, otherwise from the Host header.  Null if not provided in either place.
		/// </summary>
		public string HostName { get; internal set; }
		/// <summary>
		/// Gets the remote endpoint.
		/// </summary>
		public IPEndPoint RemoteEndPoint { get; private set; }
		/// <summary>
		/// Gets the local endpoint.
		/// </summary>
		public IPEndPoint LocalEndPoint { get; private set; }
		private static long _connectionIdCounter = 0;
		/// <summary>
		/// A unique auto-incremented identifier of this HttpProcessor instance, guaranteed to be unique for the lifetime of the current process.
		/// </summary>
		public readonly long ConnectionID = Interlocked.Increment(ref _connectionIdCounter);
		/// <summary>
		/// Gets the number of requests that have been handled by this HttpProcessor
		/// </summary>
		public long RequestsHandled { get; private set; } = 0;
		/// <summary>
		/// An exception that was caught but not logged by the last request processing routine.  Null if no exception was caught by the last request processing routine or if the exception was already logged.
		/// </summary>
		private Exception _lastUnloggedProcessingException = null;
		/// <summary>
		/// Gets the proxyOptions being used by the active request, or null if this HttpProcessor is not currently handling a an async proxy request.
		/// </summary>
		public ProxyOptions proxyOptions { get; private set; } = null;
		#endregion

		/// <summary>
		/// Constructs an HttpProcessor to handle an HTTP or HTTPS request from a client.
		/// </summary>
		/// <param name="client">TcpClient which is managing the client connection.</param>
		/// <param name="srv">The HttpServerBase instance which accepted the client connection.</param>
		/// <param name="certificateSelector"> An object responsible for delivering TLS server certificates upon demand. May be null only if [allowedConnectionTypes] does not include https.</param>
		/// <param name="allowedConnectionTypes">Enumeration flags indicating which protocols are allowed to be used.</param>
		public HttpProcessor(TcpClient client, HttpServerBase srv, ICertificateSelector certificateSelector, AllowedConnectionTypes allowedConnectionTypes)
		{
			if (allowedConnectionTypes.HasFlag(AllowedConnectionTypes.https) && certificateSelector == null)
				throw new ArgumentException("HttpProcessor was instructed to accept https requests but was not provided a certificate selector.", "certificateSelector");

			this.certificateSelector = certificateSelector;
			this.tcpClient = client;
			this.srv = srv;
			this.allowedConnectionTypes = allowedConnectionTypes;
			LocalEndPoint = (IPEndPoint)tcpClient.Client.LocalEndPoint;
			RemoteEndPoint = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
			RemoteIPAddress = RemoteEndPoint.Address;
		}

		/// <summary>
		/// Processes the request synchronously.
		/// </summary>
		public void Process()
		{
			try
			{
				CommonRequestPreprocessing(false);

				if (!TLS.TlsNegotiate.NegotiateSync(this))
					return;

				do
				{
					RecycleConnection();
					try
					{
						Request = SimpleHttpRequest.FromStream(base_uri_this_server, tcpStream);
						if (Request == null)
							return;
						try
						{
							Response = new SimpleHttpResponse(this);

							CommonRequestProcessing();

							HttpServer s = srv as HttpServer;
							if (Request.HttpMethod == "GET")
								s.handleGETRequest(this);
							else if (Request.HttpMethod == "POST")
								s.handlePOSTRequest(this);
							else
								s.handleOtherRequest(this, Request.HttpMethod);
						}
						finally
						{
							Request.CleanupSync();
						}
					}
					catch (Exception e)
					{
						if (HandleCommonExceptionScenarios(e, "HttpProcessor.Process:"))
							return;
					}
					if (Response != null)
					{
						Response.FinishSync();
						srv.Notify_RequestHandled();
						RequestsHandled++;
					}
				}
				while (Response?.KeepaliveTimeSeconds > 0);
			}
			catch (Exception ex)
			{
				HandleCommonExceptionScenarios(ex, "HttpProcessor.Process:Outer:");
			}
			finally
			{
				try
				{
					tcpStream?.Dispose();
					tcpStream = null;
				}
				catch (Exception ex) { SimpleHttpLogger.LogVerbose(ex, "HttpProcessor.Process:Outer:Finally:" + GetDebugLogPrefix()); }
				FinalConnectionCleanup();
			}
		}

		/// <summary>
		/// Processes the request asynchronously.
		/// </summary>
		/// <param name="cancellationToken">Cancellation Token</param>
		public async Task ProcessAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				CommonRequestPreprocessing(true);

				if (!await TLS.TlsNegotiate.NegotiateAsync(this, cancellationToken).ConfigureAwait(false))
					return;

				tcpStream = new UnreadableStream(tcpStream, false);

				do
				{
					RecycleConnection();
					try
					{
						Request = await SimpleHttpRequest.FromStreamAsync(base_uri_this_server, (UnreadableStream)tcpStream, readTimeoutSeconds * 1000, cancellationToken).ConfigureAwait(false);
						if (Request == null)
							return;
						try
						{
							Response = new SimpleHttpResponse(this);

							CommonRequestProcessing();

							HttpServerAsync s = srv as HttpServerAsync;
							await s.handleRequest(this, Request.HttpMethod, cancellationToken).ConfigureAwait(false);
						}
						finally
						{
							await Request.CleanupAsync(readTimeoutSeconds * 1000, cancellationToken).ConfigureAwait(false);
						}
					}
					catch (Exception e)
					{
						if (HandleCommonExceptionScenarios(e, "HttpProcessor.ProcessAsync:"))
							return;
					}
					if (Response != null)
					{
						await Response.FinishAsync(cancellationToken).ConfigureAwait(false);
						srv.Notify_RequestHandled();
						RequestsHandled++;
					}
				}
				while (Response?.KeepaliveTimeSeconds > 0);
			}
			catch (Exception ex)
			{
				HandleCommonExceptionScenarios(ex, "HttpProcessor.ProcessAsync:Outer:");
			}
			finally
			{
				try
				{
					if (tcpStream != null)
					{
#if NET6_0
						await tcpStream.DisposeAsync().ConfigureAwait(false);
#else
						tcpStream.Dispose();
#endif
						tcpStream = null;
					}
				}
				catch (Exception ex) { SimpleHttpLogger.LogVerbose(ex, "HttpProcessor.ProcessAsync:Outer:Finally:" + GetDebugLogPrefix()); }
				FinalConnectionCleanup();
			}
		}

		private void CommonRequestPreprocessing(bool isAsync)
		{
			IsAsync = isAsync;
			srv.Notify_ConnectionOpen(this);
			if (!isAsync)
			{
				tcpClient.ReceiveTimeout = 5000; // Affects synchronous I/O only!
				tcpClient.SendTimeout = 5000; // Affects synchronous I/O only!
			}
			tcpClient.NoDelay = true;
			tcpStream = tcpClient.GetStream();
		}
		/// <summary>
		/// Perform actions that should happen each time we are about to read a request.
		/// </summary>
		private void RecycleConnection()
		{
			Request = null;
			Response = null;
			proxyOptions = null;
			_lastUnloggedProcessingException = null;

			// While the server is under low load, a larger buffer is allowed for better write performance.
			if (this.ServerIsUnderHighLoad)
			{
				if (tcpClient.SendBufferSize != 8192)
					tcpClient.SendBufferSize = 8192;
			}
			else
			{
				if (tcpClient.SendBufferSize != 65536)
					tcpClient.SendBufferSize = 65536;
			}
		}

		/// <summary>
		/// <para>Call after creating the Request and Response objects.</para>
		/// <para>Performs common request processing that is the same between Async and Sync processing methods.</para>
		/// </summary>
		private void CommonRequestProcessing()
		{
			if (string.IsNullOrWhiteSpace(HostName))
			{
				HostName = Request.Headers.Get("Host");
				if (HostName != null)
				{
					string portSuffix = ":" + LocalEndPoint.Port;
					if (HostName.EndsWith(portSuffix))
						HostName = HostName.Substring(0, HostName.Length - portSuffix.Length);
				}
			}
			if (string.IsNullOrWhiteSpace(HostName))
				HostName = null;

			IPAddress originalRemoteIp = RemoteIPAddress;
			if (srv.IsTrustedProxyServer(this, originalRemoteIp))
			{
				IsConnectionViaTrustedProxyServer = true;

				if (srv.XRealIPHeader)
				{
					string headerValue = Request.Headers.Get("X-Real-Ip");
					if (!string.IsNullOrWhiteSpace(headerValue))
					{
						headerValue = headerValue.Trim();
						if (IPAddress.TryParse(headerValue, out IPAddress addr))
							RemoteIPAddress = addr;
					}
				}
				if (srv.XForwardedForHeader)
				{
					string headerValue = Request.Headers.Get("X-Forwarded-For");
					if (!string.IsNullOrWhiteSpace(headerValue))
					{
						// Because we trust the source of the header, we must trust that they validated the chain of IP addresses all the way back to the root.
						// Therefore we should get the leftmost address; this is the true client IP.
						headerValue = headerValue.Split(',').FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
						if (headerValue != null)
						{
							headerValue = headerValue.Trim();
							if (IPAddress.TryParse(headerValue, out IPAddress addr))
								RemoteIPAddress = addr;
						}
					}
				}

				if (srv.XForwardedProtoHeader)
				{
					string headerValue = Request.Headers.Get("X-Forwarded-For");
					if (!string.IsNullOrWhiteSpace(headerValue))
					{
						if (headerValue.IEquals("https"))
							secure_https = true;
						else
							secure_https = false;
						Trusted_XForwardedProtoHeader = true;
						_SetBaseUriProperties();
					}
				}
			}

			if (shouldLogRequestsToFile())
				SimpleHttpLogger.LogRequest(DateTime.Now, this.RemoteIPAddressStr, Request.HttpMethod, Request.Url.OriginalString, HostName);
		}
		internal void _SetBaseUriProperties()
		{
			string scheme = "http" + (secure_https ? "s" : "");
			IPEndPoint ipEndpoint = (IPEndPoint)tcpClient.Client.LocalEndPoint;
			string ipEndpointHost = ipEndpoint.Address.AddressFamily == AddressFamily.InterNetworkV6 ? ("[" + ipEndpoint.Address.ToString() + "]") : ipEndpoint.Address.ToString();
			int defaultPort = secure_https ? 443 : 80;
			string strPort = ipEndpoint.Port == defaultPort ? "" : ":" + ipEndpoint.Port;
			base_uri_this_server = new Uri(scheme + "://" + (HostName ?? ipEndpointHost) + strPort, UriKind.Absolute);
			base_uri_this_server_via_local_endpoint = new Uri(scheme + "://" + ipEndpointHost + strPort, UriKind.Absolute);
		}
		/// <summary>
		/// <para>Handles exception scenarios that are common to both Async and Sync processing methods.</para>
		/// <para>Returns true if the processor should return immediately with no need to flush unwritten data.</para>
		/// </summary>
		/// <param name="e">Exception that occurred.</param>
		/// <param name="errorLogPrefix">A string which should be printed at the start of any error logs.</param>
		/// <returns></returns>
		private bool HandleCommonExceptionScenarios(Exception e, string errorLogPrefix)
		{
			try
			{
				if (e == null)
				{
					SimpleHttpLogger.Log(errorLogPrefix + GetDebugLogPrefix() + "Exception object was null.");
					SimpleHttpLogger.Log(new System.Diagnostics.StackTrace().ToString());
					return true;
				}

				if (IsOrdinaryDisconnectException(e))
					return true;

				if (shouldLogRequestsToFile())
					SimpleHttpLogger.LogRequest(DateTime.Now, this.RemoteIPAddressStr, Request?.HttpMethod + ":FAIL", Request?.Url.OriginalString, HostName);

				HttpProcessorException hpex = e.GetExceptionOfType<HttpProcessorException>();
				if (hpex != null)
				{
					_logVerbose(e, errorLogPrefix);
					ResponseCriticalFail(hpex.StatusString, hpex.ResponseBody, hpex.Headers);
				}
				else if (e.GetExceptionOfType<HttpProtocolException>() != null)
				{
					_logVerbose(e, errorLogPrefix);
					ResponseCriticalFail("400 Bad Request");
				}
				else if (e.GetExceptionOfType<AuthenticationException>() != null)
				{
					// Tls Authentication Failed
					_logVerbose(e, errorLogPrefix);
					return true;
				}
				else if (e.GetExceptionWhere(ex => ex.GetType().ToString() == "Interop+OpenSsl+SslException") != null)
				{
					// This exception occurs for some requests during https://www.ssllabs.com/ssltest/
					_logVerbose(e, errorLogPrefix);
					return true;
				}
				else if (e.GetExceptionOfType<HttpRequestBodyNotReadException>() != null)
				{
					_logNormal(e, errorLogPrefix);
					ResponseCriticalFail("500 Internal Server Error");
				}
				else
				{
					_logNormal(e, errorLogPrefix);
					ResponseCriticalFail("500 Internal Server Error");
				}
				return false;
			}
			catch (Exception exception)
			{
				SimpleHttpLogger.Log(exception, "HandleCommonExceptionScenarios Failed");
				ResponseCriticalFail("500 Internal Server Error");
				return false;
			}
		}
		/// <summary>
		/// Logs the request if verbose logging is enabled, otherwise saves the exception to be logged later if a more important exception occurs.
		/// </summary>
		/// <param name="e">Exception that occurred.</param>
		/// <param name="errorLogPrefix">A string which should be printed at the start of any error logs.</param>
		private void _logVerbose(Exception e, string errorLogPrefix)
		{
			if (SimpleHttpLogger.logVerbose)
				SimpleHttpLogger.LogVerbose(_logGetString(errorLogPrefix, e));
			else
				_lastUnloggedProcessingException = e;
		}
		/// <summary>
		/// Logs the request.
		/// </summary>
		/// <param name="e">Exception that occurred.</param>
		/// <param name="errorLogPrefix">A string which should be printed at the start of any error logs.</param>
		private void _logNormal(Exception e, string errorLogPrefix)
		{
			SimpleHttpLogger.Log(_logGetString(errorLogPrefix, e));
		}
		private string _logGetString(string errorLogPrefix, Exception e)
		{
			StringBuilder sb = new StringBuilder();
			if (_lastUnloggedProcessingException != null)
			{
				sb.Append("This exception was caught earlier during request processing, but not logged due to verbose logging being disabled: ");
				sb.AppendLine(_lastUnloggedProcessingException.ToHierarchicalString());
				_lastUnloggedProcessingException = null;
			}
			sb.Append(errorLogPrefix + GetDebugLogPrefix() + e.ToHierarchicalString());
			return sb.ToString();
		}
		/// <summary>
		/// Called by <see cref="HandleCommonExceptionScenarios"/> when we should configure the response to indicate that an error occurred, and ensure that the connection is not used for further request processing.  It will not be possible to change the response if the response header has already been written.
		/// </summary>
		/// <param name="statusString">HTTP Status String, beginning with the Status Code.</param>
		/// <param name="description">Optional response body text.</param>
		/// <param name="headers">Optional headers to assign to the response.</param>
		private void ResponseCriticalFail(string statusString, string description = null, HttpHeaderCollection headers = null)
		{
			try
			{
				if (Response != null)
				{
					Response.PreventKeepalive();
					if (!Response.ResponseHeaderWritten)
					{
						Response.Simple(statusString, description);
						if (headers != null)
							foreach (HttpHeader header in headers)
								Response.Headers.Add(header);
					}
				}
			}
			catch (Exception exception)
			{
				SimpleHttpLogger.Log(exception, "ResponseCriticalFail Failed");
			}
		}

		/// <summary>
		/// Called when the HttpProcessor's process method is exiting.
		/// </summary>
		private void FinalConnectionCleanup()
		{
			try
			{
				tcpClient.Close();
				tcpClient = null;
			}
			catch (Exception ex) { SimpleHttpLogger.LogVerbose(ex, "HttpProcessor.FinalConnectionCleanup:"); }
			srv.Notify_ConnectionClosed(this);
		}

		/// <summary>
		/// Returns true if the specified Exception is a SocketException or EndOfStreamException or if one of these exception types is contained within the InnerException tree.
		/// </summary>
		/// <param name="ex">The exception.</param>
		/// <returns></returns>
		public static bool IsOrdinaryDisconnectException(Exception ex)
		{
			try
			{
				if (ex is SocketException && (ex as SocketException).SocketErrorCode != SocketError.InvalidArgument)
					return true;
				if (ex is HttpProcessor.EndOfStreamException)
					return true;
				if (ex is System.IO.EndOfStreamException)
					return true;
				if (ex is OperationCanceledException)
					return true;
				if (ex is AggregateException)
				{
					AggregateException agg = ex as AggregateException;
					if (agg.InnerExceptions != null)
						foreach (Exception inner in agg.InnerExceptions)
							if (IsOrdinaryDisconnectException(inner))
								return true;
				}
				if (ex.InnerException != null)
					return IsOrdinaryDisconnectException(ex.InnerException);
				return false;
			}
			catch (Exception exception)
			{
				SimpleHttpLogger.Log(exception, "IsOrdinaryDisconnectException Failed");
				return false;
			}
		}
		private bool shouldLogRequestsToFile()
		{
			return srv.shouldLogRequestsToFile();
		}

		private string GetDebugLogPrefix()
		{
			try
			{
				if (this.Request != null)
				{
					return this.RemoteIPAddressStr + " " + this.Request.HttpMethod + " " + this.Request.Url + " - ";
				}
				else
				{
					return this.RemoteIPAddressStr + " [request not processed yet] - ";
				}
			}
			catch (Exception ex)
			{
				SimpleHttpLogger.Log(ex, "GetDebugLogPrefix Failed");
				return "GetDebugLogPrefix Failed - ";
			}
		}

		#region Request Proxy Http(s)
		/// <summary>
		/// A thread pool to be used when additional threads are needed for proxying data.
		/// </summary>
		protected static SimpleThreadPool proxyResponseThreadPool = new SimpleThreadPool("ProxyResponses", 0, 1024, 10000);
		/// <summary>
		/// <para>Primitive, mostly-synchronous proxy method.  Recommended to use instead <see cref="ProxyToAsync(string, ProxyOptions)"/> using <see cref="TaskHelper.RunAsyncCodeSafely(Func{Task})"/> if necessary.</para>
		/// <para>Acts as a proxy server, sending the request to a different URL.</para>
		/// <para>This method starts a new (and unpooled) thread to handle the response from the remote server.</para>
		/// <para>The "Host" header is rewritten (or added) and output as the first header.</para>
		/// </summary>
		/// <param name="newUrl">The URL to proxy the original request to.</param>
		/// <param name="networkTimeoutMs">The send and receive timeout to set for both TcpClients, in milliseconds.</param>
		/// <param name="acceptAnyCert">If true, certificate validation will be disabled for outgoing https connections.</param>
		/// <param name="snoopy">If non-null, proxied communication will be copied into this object so you can snoop on it.</param>
		/// <param name="host">The value of the host header, also used in SSL authentication. If null or whitespace, it is set from the [newUrl] parameter.</param>
		/// <param name="allowKeepalive">[DANGEROUS TO SET = true] If false, a Connection: close header will be added, and any existing Connection header will be dropped.  If true, the Connection header from the client will be preserved.  Assuming the client wanted keep-alive, this proxy request will remain active and the next request to come in on this connection will be proxied even if you didn't want it to be.  If your web server does ANYTHING ELSE besides proxy requests straight through to the same destination, this may result in client requests going to the wrong place.</param>
		public void ProxyTo(string newUrl, int networkTimeoutMs = 60000, bool acceptAnyCert = false, ProxyDataBuffer snoopy = null, string host = null, bool allowKeepalive = false)
		{
			if (Response.ResponseHeaderWritten)
				throw new Exception("A response has already been written to this stream.");
			//try
			//{
			// Connect to the server we're proxying to.
			Uri newUri = new Uri(newUrl);
			if (string.IsNullOrWhiteSpace(host))
				host = newUri.DnsSafeHost;
			IPAddress ip = TaskHelper.RunAsyncCodeSafely(() => DnsHelper.GetHostAddressAsync(newUri.DnsSafeHost));

			using (TcpClient proxyClient = new TcpClient(ip.AddressFamily))
			{
				proxyClient.ReceiveTimeout = this.tcpClient.ReceiveTimeout = networkTimeoutMs;
				proxyClient.SendTimeout = this.tcpClient.SendTimeout = networkTimeoutMs;
				proxyClient.Connect(ip, newUri.Port);
				Stream proxyStream = proxyClient.GetStream();
				Response.ResponseHeaderWritten = true;
				if (newUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
				{
					RemoteCertificateValidationCallback certCallback = null;
					if (acceptAnyCert)
						certCallback = (sender, certificate, chain, sslPolicyErrors) => true;
					proxyStream = new SslStream(proxyStream, false, certCallback, null);
					((SslStream)proxyStream).AuthenticateAsClient(host, null, TLS.TlsNegotiate.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, false);
				}

				// Begin proxying by sending what we've already read from this.inputStream.
				// The first line of our HTTP request will be different from the original.
				_ProxyString(ProxyDataDirection.RequestToServer, proxyStream, Request.HttpMethod + ' ' + newUri.PathAndQuery + ' ' + Request.HttpProtocolVersionString + "\r\n", snoopy);
				// After the first line come the headers.
				_ProxyString(ProxyDataDirection.RequestToServer, proxyStream, "Host: " + host + "\r\n", snoopy);
				if (!allowKeepalive)
					_ProxyString(ProxyDataDirection.RequestToServer, proxyStream, "Connection: close\r\n", snoopy);
				else
					_ProxyString(ProxyDataDirection.RequestToServer, proxyStream, "Connection: keep-alive\r\n", snoopy);
				foreach (HttpHeader header in Request.Headers)
				{
					if (!header.Key.IEquals("Host") && (allowKeepalive || !header.Key.IEquals("Connection")))
						_ProxyString(ProxyDataDirection.RequestToServer, proxyStream, header.Key + ": " + header.Value + "\r\n", snoopy);
				}
				_ProxyString(ProxyDataDirection.RequestToServer, proxyStream, "\r\n", snoopy);

				// Write the original request body if there was one.
				if (Request.RequestBodyStream != null)
				{
					if (Request.RequestBodyStream is MemoryStream)
					{
						MemoryStream ms = (MemoryStream)Request.RequestBodyStream;
						long remember_position = ms.Position;
						ms.Seek(0, SeekOrigin.Begin);
						byte[] buf = ms.ToArray();
						_ProxyData(ProxyDataDirection.RequestToServer, proxyStream, buf, buf.Length, snoopy);
						ms.Seek(remember_position, SeekOrigin.Begin);
					}
					else
						CopyStreamUntilClosed(ProxyDataDirection.RequestToServer, Request.RequestBodyStream, proxyStream, snoopy);
				}

				// Start a thread to proxy the response to our client.
				proxyResponseThreadPool.Enqueue(() =>
				{
					try
					{
						CopyStreamUntilClosed(ProxyDataDirection.ResponseFromServer, proxyStream, this.tcpStream, snoopy);
					}
					catch (ThreadAbortException) { }
					catch (Exception ex)
					{
						if (ex.InnerException is ThreadAbortException)
							return;
						if (IsOrdinaryDisconnectException(ex))
							return;
						SimpleHttpLogger.LogVerbose(ex, "HttpProcessor.ProxyTo:");
					}
				});

				// The current thread will handle any additional incoming data from our client and proxy it to newUrl.
				this.tcpClient.NoDelay = true;
				CopyStreamUntilClosed(ProxyDataDirection.RequestToServer, this.tcpStream, proxyStream, snoopy);
			}
			//}
			//catch (ThreadAbortException) { throw; }
			//catch (Exception ex)
			//{
			//	SimpleHttpLogger.LogVerbose(ex);
			//	return ex;
			//}
		}
		private void _ProxyString(ProxyDataDirection Direction, Stream target, string str, ProxyDataBuffer snoopy)
		{
			if (snoopy != null)
				snoopy.AddItem(new ProxyDataItem(Direction, str));
			byte[] buf = Utf8NoBOM.GetBytes(str);
			target.Write(buf, 0, buf.Length);
		}
		private void _ProxyData(ProxyDataDirection Direction, Stream target, byte[] buf, int length, ProxyDataBuffer snoopy)
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
			target.Write(buf, 0, length);
		}
		private void CopyStreamUntilClosed(ProxyDataDirection Direction, Stream source, Stream target, ProxyDataBuffer snoopy)
		{
			byte[] buf = new byte[16000];
			int read = 1;
			while (read > 0)
			{
				read = source.Read(buf, 0, buf.Length);
				if (read > 0)
					_ProxyData(Direction, target, buf, read, snoopy);
			}
		}
		/// <summary>
		/// <para>Acts as a proxy server, sending the request to a different URL.</para>
		/// <para>The "Host" header is rewritten (or added) and output as the first header.</para>
		/// <para>This method is fully asynchronous, so long-running proxy operations do not tie up a thread.</para>
		/// </summary>
		/// <param name="newUrl">The URL to proxy the original request to.</param>
		/// <param name="options">Optional options to control the behavior of the proxy request.</param>
		public async Task ProxyToAsync(string newUrl, Client.ProxyOptions options = null)
		{
			if (options == null)
				options = new ProxyOptions();
			proxyOptions = options;
			options?.bet?.Start("Entering ProxyToAsync");
			await Client.ProxyClient.ProxyRequest(this, new Uri(newUrl), options).ConfigureAwait(false);
			options?.bet?.Stop();
		}
		#endregion


		/// <summary>
		/// Polls the socket to see if it has closed.
		/// </summary>
		/// <returns></returns>
		public bool CheckIfStillConnected()
		{
			//outputStream.Write(" ");
			//outputStream.Flush(); // This will throw an exception if disconnected.
			//return true;
			if (!tcpClient.Connected)
				return false;
			bool readable = tcpClient.Client.Poll(0, SelectMode.SelectRead);
			if (readable)
			{
				// data is available for reading OR connection is closed.
				byte[] buffer = new byte[1];
				if (tcpClient.Client.Receive(buffer, SocketFlags.Peek) == 0)
					return false;
				// Data was available, connection may not be closed.
				bool writable = tcpClient.Client.Poll(0, SelectMode.SelectWrite);
				bool errored = tcpClient.Client.Poll(0, SelectMode.SelectError);
				return writable && !errored;
			}
			else
			{
				return true; // The read poll returned false, so the connection is supposedly open with no data available to read, which is the normal state.
			}
		}
		/// <summary>
		/// Tests the "Authorization" header and returns the NetworkCredential that the client authenticated with, or null if authentication was not successful. "Digest" and "Basic" authentication are supported.  SimpleHttpServer does not guard against replay attacks.
		/// </summary>
		/// <param name="realm">Name of the system that is requesting authentication.  This string is shown to the user to help them understand which credential is being requested e.g. "example.com".</param>
		/// <param name="validCredentials">A collection of valid credentials which must be tested one at a time against the client's Authorization header.</param>
		/// <returns></returns>
		public NetworkCredential ValidateDigestAuth(string realm, IEnumerable<NetworkCredential> validCredentials)
		{
			string Authorization = Request.Headers.Get("Authorization");
			if (string.IsNullOrEmpty(Authorization))
				return null;

			try
			{
				AuthenticationHeaderValue authHeaderValue = AuthenticationHeaderValue.Parse(Authorization);
				if (authHeaderValue.Scheme.IEquals("Digest"))
				{
					Dictionary<string, string> parameters = ParseAuthorizationHeader(authHeaderValue.Parameter);

					parameters.TryGetValue("uri", out string uri);
					parameters.TryGetValue("nonce", out string nonce);
					parameters.TryGetValue("nc", out string nc);
					parameters.TryGetValue("cnonce", out string cnonce);
					parameters.TryGetValue("qop", out string qop);
					parameters.TryGetValue("response", out string response);

					string ha2 = Hash.GetMD5Hex(Request.HttpMethod + ":" + uri);

					foreach (NetworkCredential cred in validCredentials)
					{
						// Calculate the expected response based on the provided credentials and challenge parameters
						string ha1 = Hash.GetMD5Hex(cred.UserName + ":" + realm + ":" + cred.Password);
						string expectedResponse = Hash.GetMD5Hex(ha1 + ":" + nonce + ":" + nc + ":" + cnonce + ":" + qop + ":" + ha2);

						if (response == expectedResponse)
							return cred;
					}
				}
				else if (authHeaderValue.Scheme.IEquals("Basic"))
				{
					string parameter = Utf8NoBOM.GetString(Convert.FromBase64String(authHeaderValue.Parameter));
					foreach (NetworkCredential cred in validCredentials)
					{
						string expected = cred.UserName + ":" + cred.Password;
						if (expected == parameter)
							return cred;
					}
				}
			}
			catch (Exception) { }
			return null;
		}
		private static Dictionary<string, string> ParseAuthorizationHeader(string authHeader)
		{
			var authParams = new Dictionary<string, string>();
			string pattern = @"(\w+)=(""([^""]+)""|(\w+))";
			var matches = Regex.Matches(authHeader, pattern);
			foreach (Match match in matches)
			{
				string value = match.Groups[3].Value != "" ? match.Groups[3].Value : match.Groups[4].Value;
				authParams.Add(match.Groups[1].Value, value);
			}
			return authParams;
		}
		/// <summary>
		/// Returns a string which can be set as the value of the "WWW-Authenticate" header accompanying a "401 Unauthorized" response.  The header will request HTTP Digest authentication using random "nonce" and "opaque" strings.  SimpleHttpServer does not guard against replay attacks.
		/// </summary>
		/// <param name="realm"></param>
		/// <returns></returns>
		public string GetDigestAuthWWWAuthenticateHeaderValue(string realm)
		{
			byte[] nonceData = new byte[32];
			SecureRandom.NextBytes(nonceData);
			string nonce = Hex.ToHex(nonceData);

			byte[] opaqueData = new byte[32];
			SecureRandom.NextBytes(opaqueData);
			string opaque = Hex.ToHex(opaqueData);

			return "Digest realm=\"" + realm + "\", nonce=\"" + nonce + "\", opaque=\"" + opaque + "\", algorithm=MD5, qop=\"auth\", userhash=true";
		}
		/// <summary>
		/// Returns a reference to this HttpProcessor's TcpClient.  Direct access to the TcpClient is dangerous as it can be easy to violate the HTTP protocol or cause other bugs.
		/// </summary>
		/// <returns></returns>
		public TcpClient GetTcpClient()
		{
			return tcpClient;
		}
		/// <inheritdoc/>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("[" + this.ConnectionID + "] [" + this.RemoteIPAddressStr + " -> " + this.HostName + "]");
			if (this.Request == null)
				sb.Append(" ... awaiting request");
			else
			{
				sb.Append(" Req: " + this.Request.ToString());
				if (this.Response != null)
					sb.Append(" Res: " + this.Response.ToString());
			}
			return sb.ToString();
		}
		/// <summary>
		/// Gets a dynamic object containing a snapshot of the status of this HttpProcessor, the Request, and the Response.
		/// </summary>
		/// <returns>A dynamic object containing a snapshot of the status of this HttpProcessor, the Request, and the Response.</returns>
		public object GetSummary()
		{
			return new
			{
				ID = ConnectionID,
				ClientIP = RemoteIPAddressStr,
				Host = HostName,
				LocalPort = LocalEndPoint.Port,
				Tls = secure_https,
				RequestsHandled = RequestsHandled,
				Request = Request?.GetSummary(),
				Response = Response?.GetSummary()
			};
		}

		/// <summary>
		/// An exception containing an HTTP response code and text.
		/// </summary>
		public class HttpProcessorException : Exception
		{
			/// <summary>
			/// Http response code and text, e.g. "413 Entity Too Large"
			/// </summary>
			public string StatusString;
			/// <summary>
			/// Optional response body (will typically be visible in the web browser if this was a request for an HTML page).  If omitted, the <see cref="StatusString"/> is printed as the response body.
			/// </summary>
			public string ResponseBody;
			/// <summary>
			/// Http headers to set in the response.
			/// </summary>
			public HttpHeaderCollection Headers;
			/// <summary>
			/// Constructs an exception containing an HTTP response code and text, an optional body, and optional response headers.
			/// </summary>
			/// <param name="StatusString">Http response code and text, e.g. "413 Entity Too Large"</param>
			/// <param name="ResponseBody">Optional response body (will typically be visible in the web browser if this was a request for an HTML page).  If omitted, the <paramref name="StatusString"/> is printed as the response body.</param>
			/// <param name="Headers">Http headers to set in the response.</param>
			public HttpProcessorException(string StatusString, string ResponseBody = null, HttpHeaderCollection Headers = null) : base(CreateMessage(StatusString, ResponseBody))
			{
				this.StatusString = StatusString;
				this.ResponseBody = ResponseBody;
				this.Headers = Headers;
			}

			private static string CreateMessage(string statusString, string responseBody)
			{
				if (responseBody == null)
					return statusString;
				return statusString + Environment.NewLine + responseBody;
			}
		}

		/// <summary>
		/// Occurs when the client committed a protocol violation.
		/// </summary>
		internal class HttpProtocolException : Exception
		{
			public HttpProtocolException(string message) : base(message) { }
			public HttpProtocolException(string message, Exception innerException) : base(message, innerException) { }
		}

		/// <summary>
		/// Occurs when the end of stream is found during request processing. Inherits from <see cref="HttpProtocolException"/>.
		/// </summary>
		internal class EndOfStreamException : HttpProtocolException
		{
			public EndOfStreamException() : base("End of stream") { }
		}
		/// <summary>
		/// Occurs when the HTTP request body was not read by the server and the HttpProcessor is refusing to read it fully; the connection should be closed.
		/// </summary>
		internal class HttpRequestBodyNotReadException : Exception
		{
			public HttpRequestBodyNotReadException(string message) : base(message) { }
			public HttpRequestBodyNotReadException(string message, Exception innerException) : base(message, innerException) { }
		}
	}
	/// <summary>
	/// Base class that provides comment Http Web Server functionality.
	/// </summary>
	public abstract class HttpServerBase : IDisposable
	{
		/// <summary>
		/// Defines a local socket binding for HttpServer.
		/// </summary>
		public class Binding
		{
			/// <summary>
			/// The local endpoint which the server listens on for this Binding.
			/// </summary>
			public readonly IPEndPoint Endpoint;
			/// <summary>
			/// Connection types that are allowed, where it is possible to allow plain unencrypted connections or TLS.
			/// </summary>
			public AllowedConnectionTypes AllowedConnectionTypes { get; internal set; }
			/// <summary>
			/// Constructs an Endpoint.
			/// </summary>
			/// <param name="allowedConnectionTypes">Connection types that are allowed for this binding, where it is possible to allow plain unencrypted connections or TLS.</param>
			/// <param name="endpoint">The local endpoint which the server listens on for this Binding.</param>
			public Binding(AllowedConnectionTypes allowedConnectionTypes, IPEndPoint endpoint)
			{
				AllowedConnectionTypes = allowedConnectionTypes;
				if (endpoint == null)
					throw new ArgumentNullException("endpoint");
				Endpoint = endpoint;
			}
			/// <summary>
			/// Constructs a Binding.
			/// </summary>
			/// <param name="allowedConnectionTypes">Connection types that are allowed for this binding, where it is possible to allow plain unencrypted connections or TLS.</param>
			/// <param name="port">TCP port number to listen on.</param>
			/// <param name="address">IP address to listen on.</param>
			public Binding(AllowedConnectionTypes allowedConnectionTypes, ushort port, IPAddress address = null) : this(allowedConnectionTypes, new IPEndPoint(address ?? IPAddress.Any, port)) { }

			/// <summary>
			/// Returns true if this Binding is equal to a specified object (another Binding instance).
			/// </summary>
			/// <param name="obj">Other binding to compare with.</param>
			/// <returns></returns>
			public override bool Equals(object obj)
			{
				if (obj != null && obj is Binding)
				{
					Binding other = obj as Binding;
					return AllowedConnectionTypes.Equals(other.AllowedConnectionTypes) && Endpoint.Equals(other.Endpoint);
				}
				return false;
			}
			/// <inheritdoc/>
			public override int GetHashCode()
			{
				return AllowedConnectionTypes.GetHashCode() ^ Endpoint.GetHashCode();
			}
			/// <summary>
			/// Returns true if this Binding's <see cref="Endpoint"/> is equal to the other's Endpoint.
			/// </summary>
			/// <param name="other">Other binding to compare with.</param>
			/// <returns></returns>
			public bool SocketEndpointsEqual(Binding other)
			{
				if (other != null)
					return Endpoint.Equals(other.Endpoint);
				return false;
			}
			/// <inheritdoc/>
			public override string ToString()
			{
				return Endpoint.ToString() + " (" + AllowedConnectionTypes + ")";
			}
		}
		public class ListenerData
		{
			/// <summary>
			/// <para>The binding which this ListenerData uses.</para>
			/// <para>If port 0 is specified during ListenerData construction, a port number will be automatically selected and this Binding will be replaced with an updated one after listening begins.</para>
			/// </summary>
			public Binding Binding { get; private set; }
			private TcpListener tcpListener = null;
			private HttpServerBase Server;
			private CancellationTokenSource cts;
			private CancellationToken cancellationToken;
			private volatile bool didCallRun = false;
			/// <summary>
			/// Constructs a ListenerData.
			/// </summary>
			/// <param name="server">Server which owns this ListenerData.</param>
			/// <param name="binding">Local service binding which this ListenerData will use.</param>
			public ListenerData(HttpServerBase server, Binding binding)
			{
				this.Server = server;
				this.Binding = binding;
				cts = new CancellationTokenSource();
				cancellationToken = cts.Token;
			}
			/// <summary>
			/// Robustly listens on the Binding.  The task does not complete until Stop() is called.  You can only Run a ListenerData one time.
			/// </summary>
			public async Task Run()
			{
				if (didCallRun)
					throw new ApplicationException("This ListenerData was already run before.  It can not be run again.");
				didCallRun = true;
				await Task.Run(async () =>
				{
					bool didAnnounceStart = false;
					try
					{
						CountdownStopwatch addressInUseCooldown = null;
						while (!cancellationToken.IsCancellationRequested)
						{
							StopListening();

							// Bind Socket Listener
							try
							{
								tcpListener = new TcpListener(Binding.Endpoint);
								tcpListener.Start();
								addressInUseCooldown = null;
							}
							catch (Exception ex)
							{
								if (ex is SocketException && ((SocketException)ex).SocketErrorCode == SocketError.AddressAlreadyInUse)
								{
									if (addressInUseCooldown == null || addressInUseCooldown.Finished)
									{
										SimpleHttpLogger.Log(ToString() + " - Address in use.  Will keep trying to bind.");
										addressInUseCooldown = CountdownStopwatch.StartNew(TimeSpan.FromSeconds(60));
									}
									StopListening();
									await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
								}
								else
								{
									SimpleHttpLogger.Log(ex, ToString() + " - Unable to bind TcpListener due to error.");
									StopListening();
									await Task.Delay(10000, cancellationToken).ConfigureAwait(false);
								}
								continue;
							}

							// Accept Connections
							try
							{
								Server.SocketLog((didAnnounceStart ? "Resumed " : "Started ") + ToString());
								didAnnounceStart = true;

								string trafficType;
								if (Binding.AllowedConnectionTypes == AllowedConnectionTypes.http)
									trafficType = "http";
								else if (Binding.AllowedConnectionTypes == AllowedConnectionTypes.https)
									trafficType = "https";
								else if (Binding.AllowedConnectionTypes == AllowedConnectionTypes.httpAndHttps)
									trafficType = "http and https";
								else
									trafficType = "unknown";

								if (Binding.Endpoint.Port != ((IPEndPoint)tcpListener.LocalEndpoint).Port)
									Binding = new Binding(Binding.AllowedConnectionTypes, (IPEndPoint)tcpListener.LocalEndpoint);

								Server.SocketBound.Invoke(this, "Listening on " + Binding.Endpoint.ToString() + " for " + trafficType);
								while (!cancellationToken.IsCancellationRequested && tcpListener.Server?.IsBound == true)
								{
									try
									{
#if NETFRAMEWORK
										TcpClient s = await tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
#else
										TcpClient s = await tcpListener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
#endif
										if (Server.IsServerTooBusyToProcessNewConnection(s))
										{
											_ = DismissTcpClientWithHttp503(s, cancellationToken).ConfigureAwait(false);
										}
										else
										{
											// TcpClient's timeouts are merely limits on Read() and Write() call blocking time.  If we try to read or write a chunk of data that legitimately takes longer than the timeout to finish, it will still time out even if data was being transferred steadily.  These timeouts do not apply to async I/O methods.
											if (s.ReceiveTimeout < 10000) // Timeout of 0 is infinite (and default), which is bad for resource consumption.
												s.ReceiveTimeout = 10000;
											if (s.SendTimeout < 10000) // Timeout of 0 is infinite (and default), which is bad for resource consumption.
												s.SendTimeout = 10000;
											int? rbuf = Server.ReceiveBufferSize;
											if (rbuf != null)
												s.ReceiveBufferSize = rbuf.Value;
											IProcessor processor = Server.MakeClientProcessor(s, Server, Server.certificateSelector, Binding.AllowedConnectionTypes);
											if (Server is HttpServer syncServer)
												syncServer.pool.Enqueue(processor.Process);
											else
												_ = processor.ProcessAsync(cancellationToken).ConfigureAwait(false);
										}
									}
									catch (OperationCanceledException)
									{
									}
									catch (ObjectDisposedException ex)
									{
										if (!cancellationToken.IsCancellationRequested)
											SimpleHttpLogger.Log(ex, ToString() + " - Error processing TcpClient.");
									}
									catch (Exception ex)
									{
										if (!HttpProcessor.IsOrdinaryDisconnectException(ex))
											SimpleHttpLogger.Log(ex, ToString() + " - Error processing TcpClient.");
									}
								}
							}
							catch (OperationCanceledException)
							{
							}
							catch (Exception ex)
							{
								SimpleHttpLogger.Log(ex, ToString() + " - Restarting due to an error.");
								StopListening();
								await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
							}
						}
					}
					catch (OperationCanceledException)
					{
					}
					finally
					{
						StopListening();
						if (didAnnounceStart)
							Server.SocketLog("Stopped " + ToString());
					}
				}, cancellationToken).ConfigureAwait(false);
			}
			private static byte[] http503ResponseData = ByteUtil.Utf8NoBOM.GetBytes("HTTP/1.1 503 Service Unavailable\r\nConnection: close\r\n\r\nServer too busy");
			private static async Task DismissTcpClientWithHttp503(TcpClient s, CancellationToken cancellationToken = default)
			{
				try
				{
					await s.GetStream().WriteAsync(http503ResponseData, 0, http503ResponseData.Length, cancellationToken).ConfigureAwait(false);
				}
				finally
				{
					s.Close();
				}
			}
			private void StopListening()
			{
				try
				{
					tcpListener?.Stop();
				}
				catch (Exception ex)
				{
					SimpleHttpLogger.Log(ex);
				}
				tcpListener = null;
			}

			public void Stop()
			{
				cts.Cancel();
#if NETFRAMEWORK
				// .NET Framework 4.5 does not support `AcceptTcpClientAsync(cancellationToken)`, therefore we must stop the TcpListener to make the pending client acceptance get canceled.
				StopListening();
#endif
			}
			public override string ToString()
			{
				return "Listener " + Binding.ToString();
			}
		}
		/// <summary>
		/// <para>If not null, each TCP connection will be assigned this receive buffer size in bytes (default if unassigned is 8192).</para>
		/// <para>If your application will be recieving large amounts of data on a single connection, it can improve transfer rate by assigning a larger receive buffer.</para>
		/// <para>HttpProcessor may be upgraded in the future to automatically tune the receive buffer size depending on the amount of data being received, in which case this field would likely be deleted.</para>
		/// <para>If you wish to set the size of the send buffer, you should do it on a per-connection basis via the HttpProcessor when you process each request.</para>
		/// </summary>
		public int? ReceiveBufferSize = null;
		/// <summary>
		/// If true, the IP address of remote hosts will be learned from the HTTP header named "X-Real-IP".  Also requires the method <see cref="IsTrustedProxyServer"/> to return true.
		/// </summary>
		public bool XRealIPHeader = false;
		/// <summary>
		/// If true, the IP address of remote hosts will be learned from the HTTP header named "X-Forwarded-For".  Also requires the method <see cref="IsTrustedProxyServer"/> to return true.
		/// </summary>
		public bool XForwardedForHeader = false;
		/// <summary>
		/// If true, the protocol reported by <see cref="HttpProcessor.secure_https"/> will be obtained from the HTTP header named "X-Forwarded-Proto".  Also requires the method <see cref="IsTrustedProxyServer"/> to return true.
		/// </summary>
		public bool XForwardedProtoHeader = false;
		protected volatile bool stopRequested = false;
		protected ICertificateSelector certificateSelector;
		/// <summary>
		/// Raised when a listening socket is bound to a port.  The Event Handler passes along a string which can be printed to the console, announcing this event.
		/// </summary>
		public event EventHandler<string> SocketBound = delegate { };

		private NetworkAddressInfo addressInfo = new NetworkAddressInfo();
		/// <summary>
		/// Gets information about the current network interfaces.
		/// You should work with a local reference to the returned object, because this method is not guaranteed to always return the same instance.
		/// </summary>
		/// <returns></returns>
		internal NetworkAddressInfo GetAddressInfo()
		{
			return addressInfo;
		}

		/// <summary>
		/// List of ListenerData instances responsible for asynchronously accepting TCP clients.
		/// </summary>
		private List<ListenerData> listeners = new List<ListenerData>();

		/// <summary>
		/// The current number of open connections.  Should be written only via the <see cref="Interlocked"/> API.
		/// </summary>
		private volatile int _currentNumberOfOpenConnections = 0;
		/// <summary>
		/// Total number of connections served by this server.  Should be written only via the <see cref="Interlocked"/> API.
		/// </summary>
		private long _totalConnectionsServed = 0;
		/// <summary>
		/// Total number of requests served by this server.  Should be written only via the <see cref="Interlocked"/> API.
		/// </summary>
		private long _totalRequestsServed = 0;
		/// <summary>
		/// Gets the current number of open connections.
		/// </summary>
		public int CurrentNumberOfOpenConnections => _currentNumberOfOpenConnections;
		/// <summary>
		/// Gets the total number of connections served by this server.
		/// </summary>
		public long TotalConnectionsServed => Interlocked.Read(ref _totalConnectionsServed);
		/// <summary>
		/// Gets the total number of requests served by this server.
		/// </summary>
		public long TotalRequestsServed => Interlocked.Read(ref _totalRequestsServed);
		/// <summary>
		/// Collection of all active HTTP processors, keyed on their remote endpoint.
		/// </summary>
		private ConcurrentDictionary<long, HttpProcessor> ActiveHttpProcessors = new ConcurrentDictionary<long, HttpProcessor>();
		/// <summary>
		/// Increments the <see cref="CurrentNumberOfOpenConnections"/> counter in a thread-safe manner.
		/// </summary>
		internal void Notify_ConnectionOpen(HttpProcessor p)
		{
			Interlocked.Increment(ref _totalConnectionsServed);
			Interlocked.Increment(ref _currentNumberOfOpenConnections);
			ActiveHttpProcessors[p.ConnectionID] = p;
		}
		/// <summary>
		/// Decrements the <see cref="CurrentNumberOfOpenConnections"/> counter in a thread-safe manner.
		/// </summary>
		internal void Notify_ConnectionClosed(HttpProcessor p)
		{
			Interlocked.Decrement(ref _currentNumberOfOpenConnections);
			ActiveHttpProcessors.TryRemove(p.ConnectionID, out HttpProcessor ignored);
		}
		/// <summary>
		/// Increments the <see cref="TotalRequestsServed"/> counter in a thread-safe manner.
		/// </summary>
		internal void Notify_RequestHandled()
		{
			Interlocked.Increment(ref _totalRequestsServed);
		}

#if NET6_0
		private static bool? _cipherSuitesPolicySupported = null;
#endif
		/// <summary>
		/// <para>Returns true if the SslServerAuthenticationOptions.CipherSuitesPolicy property can be used on this platform.</para>
		/// <para>Expected to be false on Windows, true on Linux.</para>
		/// </summary>
		/// <returns>Returns true if the SslServerAuthenticationOptions.CipherSuitesPolicy property can be used on this platform.</returns>
		public static bool IsTlsCipherSuitesPolicySupported()
		{
#if NET6_0
			bool? v = _cipherSuitesPolicySupported;
			if (v == null)
			{
				SslServerAuthenticationOptions sslServerOptions = new SslServerAuthenticationOptions();
				try
				{
					sslServerOptions.CipherSuitesPolicy = new CipherSuitesPolicy(new TlsCipherSuite[]
						{
						TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384
						});
					_cipherSuitesPolicySupported = v = true;
				}
				catch (PlatformNotSupportedException)
				{
					_cipherSuitesPolicySupported = v = false;
				}
				catch (Exception ex)
				{
					_cipherSuitesPolicySupported = v = false;
					SimpleHttpLogger.Log(ex);
				}
			}
			return v.Value;
#else
			return false;
#endif
		}
		/// <summary>
		/// Returns true if the HTTP status code is allowed to have a response body.
		/// </summary>
		/// <param name="statusCode">HTTP status code (e.g. 404)</param>
		/// <returns>True if the HTTP status code is allowed to have a response body.</returns>
		public static bool HttpStatusCodeCanHaveResponseBody(int statusCode)
		{
			if (statusCode.ToString().StartsWith("1"))
				return false;
			if (statusCode == 204 || statusCode == 205)
				return false;
			if (statusCode == 304)
				return false;
			return true;
		}
		#region Internal Logging
		private static HttpLogger httpLogger = new HttpLogger();
		/// <summary>
		/// If true, HTTP server logging will be enabled during construction for HttpServer instances constructed after this field is set.
		/// </summary>
		public static bool EnableLoggingByDefault = false;
		/// <summary>
		/// If true, HTTP server verbose logging will be enabled during construction for HttpServer instances constructed after this field is set.
		/// </summary>
		public static bool EnableVerboseLoggingByDefault = false;
		/// <summary>
		/// Enables HTTP server logging of socket binding operations and unexpected errors not likely to be related to something a remote client did.
		/// </summary>
		/// <param name="logVerbose">If true, additional error reporting will be enabled.  These errors include things that can occur frequently during normal operation and may be caused by remote client activity, so it may be spammy.</param>
		public void EnableLogging(bool logVerbose)
		{
			httpLogger.StartLoggingThreads();
			SimpleHttpLogger.RegisterLogger(httpLogger, logVerbose);
		}
		/// <summary>
		/// Disables HTTP server logging.
		/// </summary>
		public void DisableLogging()
		{
			SimpleHttpLogger.UnregisterLogger();
			httpLogger.StopLoggingThreads();
		}
		#endregion
		/// <summary>
		/// 
		/// </summary>
		/// <param name="certificateSelector">(Optional) Certificate selector to use for https connections.  If null and an https-compatible endpoint was specified, a certificate is automatically created if necessary and loaded from "SimpleHttpServer-SslCert.pfx" in the same directory that the current executable is located in.</param>
		public HttpServerBase(ICertificateSelector certificateSelector = null)
		{
			if (EnableLoggingByDefault)
				EnableLogging(EnableVerboseLoggingByDefault);

			this.certificateSelector = certificateSelector;
			if (this.certificateSelector == null)
				this.certificateSelector = new SelfSignedCertificateSelector();

			NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
			NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
			UpdateNetworkAddresses();
		}

		#region IDisposable Support
		private bool disposedValue = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// Dispose managed objects
					Stop();
				}

				// Dispose unmanaged objects
				// Set large fields = null

				disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
		}
		#endregion

		/// <summary>
		/// A function which produces an IProcessor instance, allowing this server to be used for protocols besides HTTP.  By default, this returns a standard HttpProcessor.
		/// </summary>
		/// <param name="s">The TcpClient which holds the connection from the client.</param>
		/// <param name="srv">The server instance which accepted the TCP connection from the client.</param>
		/// <param name="certSelector">An instance of a type deriving from ICertificateSelector, such as <see cref="SimpleCertificateSelector"/> or <see cref="ServerNameCertificateSelector"/>.</param>
		/// <param name="allowedConnectionTypes">Indicates whether the connection should support HTTP, HTTPS, or both.</param>
		/// <returns></returns>
		public virtual IProcessor MakeClientProcessor(TcpClient s, HttpServerBase srv, ICertificateSelector certSelector, AllowedConnectionTypes allowedConnectionTypes)
		{
			return new HttpProcessor(s, srv, certSelector, allowedConnectionTypes);
		}
		private void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
		{
			UpdateNetworkAddresses();
		}

		private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
		{
			UpdateNetworkAddresses();
		}

		private void UpdateNetworkAddresses()
		{
			addressInfo = new NetworkAddressInfo(NetworkInterface.GetAllNetworkInterfaces());
		}

		private static object certCreateLock = new object();
		public static X509Certificate2 GetSelfSignedCertificate()
		{
			lock (certCreateLock)
			{
				X509Certificate2 ssl_certificate;
				FileInfo fiExe;
				try
				{
					fiExe = new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location);
				}
				catch
				{
#if NETFRAMEWORK || NET6_0_WIN
					try
					{
						fiExe = new FileInfo(System.Windows.Forms.Application.ExecutablePath);
					}
					catch
					{
						fiExe = new FileInfo(Globals.ApplicationDirectoryBase + Globals.ExecutableNameWithExtension);
					}
#else
					fiExe = new FileInfo(Globals.ApplicationDirectoryBase + Globals.ExecutableNameWithExtension);
#endif
				}
				string autoCertPassword = "N0t_V3ry-S3cure#lol";
				FileInfo fiCert = new FileInfo(fiExe.Directory.FullName + "/SimpleHttpServer-SslCert.pfx");
				if (fiCert.Exists)
				{
					try
					{
						ssl_certificate = new X509Certificate2(fiCert.FullName, autoCertPassword);
					}
					catch (Exception ex1)
					{
						try
						{
							ssl_certificate = new X509Certificate2(fiCert.FullName);
						}
						catch
						{
							throw ex1;
						}
					}
				}
				else
				{
#if NETFRAMEWORK
					using (BPUtil.SimpleHttp.Crypto.CryptContext ctx = new BPUtil.SimpleHttp.Crypto.CryptContext())
					{
						ctx.Open();

						ssl_certificate = ctx.CreateSelfSignedCertificate(
							new BPUtil.SimpleHttp.Crypto.SelfSignedCertProperties
							{
								IsPrivateKeyExportable = true,
								KeyBitLength = 4096,
								Name = new X500DistinguishedName("cn=localhost"),
								ValidFrom = DateTime.Today.AddDays(-1),
								ValidTo = DateTime.Today.AddYears(100),
							});

						byte[] certData = ssl_certificate.Export(X509ContentType.Pfx, autoCertPassword);
						File.WriteAllBytes(fiCert.FullName, certData);
					}
#elif NET6_0
					// Native cert generator. .NET 4.7.2 required.
					using (System.Security.Cryptography.RSA key = System.Security.Cryptography.RSA.Create(2048))
					{
						CertificateRequest request = new CertificateRequest("cn=localhost", key, System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);

						SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
						sanBuilder.AddDnsName("localhost");
						request.CertificateExtensions.Add(sanBuilder.Build());

						ssl_certificate = request.CreateSelfSigned(DateTime.Today.AddDays(-1), DateTime.Today.AddYears(100));

						byte[] certData = ssl_certificate.Export(X509ContentType.Pfx, autoCertPassword);
						File.WriteAllBytes(fiCert.FullName, certData);
					}
#endif
				}
				return ssl_certificate;
			}
		}
		/// <summary>
		/// <para>Shorthand method to configure this server to listen on all interfaces on one http and/or one https port.</para>
		/// <para>If the same port is given for both protocols, only one socket will be bound and it will accept both protocols.</para>
		/// <para>This method will start or stop listeners as necessary to transition from the current set of bindings to the new set of bindings.</para>
		/// </summary>
		/// <param name="httpPort">Port number for the HTTP listener. If out of range [0-65535] (e.g. -1), the listener is disabled.</param>
		/// <param name="httpsPort">Port number for the HTTPS listener. If out of range [0-65535] (e.g. -1), the listener is disabled.</param>
		public void SetBindings(int httpPort = -1, int httpsPort = -1)
		{
			if (httpPort > 65535 || httpPort < -1) httpPort = -1;
			if (httpsPort > 65535 || httpsPort < -1) httpsPort = -1;
			List<Binding> bindings = new List<Binding>();
			if (httpPort != -1 && httpPort == httpsPort)
				bindings.Add(new HttpServerBase.Binding(AllowedConnectionTypes.httpAndHttps, new IPEndPoint(IPAddress.Any, httpPort)));
			else
			{
				if (httpPort != -1)
					bindings.Add(new HttpServerBase.Binding(AllowedConnectionTypes.http, new IPEndPoint(IPAddress.Any, httpPort)));
				if (httpsPort != -1)
					bindings.Add(new HttpServerBase.Binding(AllowedConnectionTypes.https, new IPEndPoint(IPAddress.Any, httpsPort)));
			}
			SetBindings(bindings.ToArray());
		}
		/// <summary>
		/// Sets the collection of bindings which this server should listen on.  This method will start or stop listeners as necessary to transition from the current set of bindings to the new set of bindings.
		/// </summary>
		/// <param name="newBindings">All bindings which this server should listen on.</param>
		public void SetBindings(params Binding[] newBindings)
		{
			if (stopRequested)
				return;

			if (newBindings == null)
				newBindings = new Binding[0];

			List<ListenerData> toStart = new List<ListenerData>();
			List<ListenerData> toStop = new List<ListenerData>();
			lock (listeners)
			{
				// Decide what to do with each of our current listeners.
				foreach (ListenerData l in listeners)
				{
					Binding perfectMatch = newBindings.FirstOrDefault(b => b.Equals(l.Binding));
					if (perfectMatch != null)
					{
						// This listener remains unmodified.
						continue;
					}
					Binding socketLevelMatch = newBindings.FirstOrDefault(b => b.SocketEndpointsEqual(l.Binding));
					if (socketLevelMatch != null)
					{
						// This listener needs to change its AllowedConnectionTypes
						l.Binding.AllowedConnectionTypes = socketLevelMatch.AllowedConnectionTypes;
						SocketLog("Modified AllowedConnectionTypes on " + l.ToString());
						continue;
					}
					// This listener needs to stop.
					toStop.Add(l);
				}

				// Remove listeners that need to be stopped from the collection.
				listeners.RemoveAll(toStop.Contains);

				// Create new listeners as needed
				foreach (Binding b in newBindings)
				{
					if (!listeners.Any(l => l.Binding.SocketEndpointsEqual(b)))
						toStart.Add(new ListenerData(this, b));
				}

				// Add the new listeners to the collection.
				listeners.AddRange(toStart);
			}

			// Stop dead listeners
			foreach (ListenerData listener in toStop)
			{
				listener.Stop();
			}

			// Start new listeners
			foreach (ListenerData listener in toStart)
			{
				_ = listener.Run().ConfigureAwait(false);
			}
		}
		internal void SocketLog(string str)
		{
			if (shouldLogSocketBind())
				SimpleHttpLogger.Log(str);
			else
				SimpleHttpLogger.LogVerbose(str);
		}

		/// <summary>
		/// Stops listening for connections.
		/// </summary>
		public virtual void Stop()
		{
			if (stopRequested)
				return;
			stopRequested = true;

			try
			{
				NetworkChange.NetworkAvailabilityChanged -= NetworkChange_NetworkAvailabilityChanged;
				NetworkChange.NetworkAddressChanged -= NetworkChange_NetworkAddressChanged;
			}
			catch (Exception ex) { SimpleHttpLogger.Log(ex); }

			lock (listeners)
			{
				foreach (ListenerData listener in listeners)
				{
					listener.Stop();
				}
			}

			try { stopServer(); } catch (Exception ex) { SimpleHttpLogger.Log(ex); }
			try { GlobalThrottledStream.ThrottlingManager.Shutdown(); } catch (Exception ex) { SimpleHttpLogger.Log(ex); }
		}
		/// <summary>
		/// Returns an array of bindings currently active in this server. Please do not modify the bindings that are returned; instead, send a new set of bindings to <see cref="SetBindings(Binding[])"/>.
		/// </summary>
		/// <returns></returns>
		public Binding[] GetBindings()
		{
			lock (listeners)
			{
				return listeners.Select(l => l.Binding).ToArray();
			}
		}

		/// <summary>
		/// This is called when the server is stopping.  Perform any cleanup work here.
		/// </summary>
		protected abstract void stopServer();

		/// <summary>
		/// If this method returns true, requests should be logged to a file.
		/// </summary>
		/// <returns></returns>
		public virtual bool shouldLogRequestsToFile()
		{
			return false;
		}

		/// <summary>
		/// This method must return true for the <see cref="XForwardedForHeader"/> and <see cref="XRealIPHeader"/> and <see cref="XForwardedProtoHeader"/> flags to be honored.  This method should only return true if the provided remote IP address is trusted to provide the related headers.
		/// </summary>
		/// <param name="p">HttpProcessor</param>
		/// <param name="remoteIpAddress">Remote IP address of the client (proxy-related HTTP headers have not been read yet).</param>
		/// <returns></returns>
		public virtual bool IsTrustedProxyServer(HttpProcessor p, IPAddress remoteIpAddress)
		{
			return false;
		}
		/// <summary>
		/// If this method returns true, socket bind events will be logged normally.  If false, they will use the LogVerbose call.
		/// </summary>
		/// <returns></returns>
		public virtual bool shouldLogSocketBind()
		{
			return false;
		}
		/// <summary>
		/// HashSet of file extensions (not case-sensitive) that are not allowed to be cached by the <c>Response.StaticFile()</c> method.
		/// </summary>
		protected HashSet<string> NoCacheStaticFileExtensions = new HashSet<string>(new string[] { ".htm", ".html" });
		/// <summary>
		/// Returns true if the given file extension is allowed to be cached by the <c>Response.StaticFile()</c> method.
		/// </summary>
		/// <param name="extension">File extension (e.g. ".txt")</param>
		/// <returns></returns>
		public virtual bool CanCacheFileExtension(string extension)
		{
			return !NoCacheStaticFileExtensions.Contains(extension, true);
		}
		/// <summary>
		/// <para>Gets true if the HTTP server is believed to be under high load conditions.</para>
		/// <para>In high load conditions, buffers may be smaller, "Connection: keep-alive" may not be allowed, and other resource-saving effects may be used.</para>
		/// </summary>
		public virtual bool IsServerUnderHighLoad()
		{
			return CurrentNumberOfOpenConnections > Math.Max(0, MaxConnections / 2);
		}
		/// <summary>
		/// Each incoming connection to the server is passed to this method.  If the method returns true, an HTTP "503 Service Unavailable" response will be written and the connection will not be processed by an <see cref="HttpProcessor"/>.
		/// </summary>
		/// <param name="tcpClient">The new connection from a client.</param>
		/// <returns>True if the connection should be dismissed with an HTTP 503 response.</returns>
		protected virtual bool IsServerTooBusyToProcessNewConnection(TcpClient tcpClient)
		{
			return CurrentNumberOfOpenConnections >= MaxConnections;
		}
		/// <summary>
		/// Allows the implementor to override which Ssl Protocols are allowed by this server.
		/// </summary>
		/// <param name="remoteIpAddress">Remote IP address of the client (proxy-related HTTP headers have not been read yet).</param>
		/// <param name="defaultProtocols">The SslProtocols which would be used if you hadn't overridden this method.</param>
		/// <returns></returns>
		public virtual SslProtocols ChooseSslProtocols(IPAddress remoteIpAddress, SslProtocols defaultProtocols)
		{
			return defaultProtocols;
		}

#if NET6_0
		/// <summary>
		/// Gets a collection of allowed TLS cipher suites for the given connection.  Returns null if the default set of cipher suites should be allowed (which varies by platform).
		/// </summary>
		/// <param name="p">HttpProcessor providing connection information so that the derived class can decide which cipher suites to allow.</param>
		/// <returns>A collection of allowed TLS cipher suites, or null.</returns>
		public virtual IEnumerable<TlsCipherSuite> GetAllowedCipherSuites(HttpProcessor p)
		{
			return null;
		}
#endif
		/// <summary>
		/// Gets or sets maximum number of connections that should be allowed simultaneously.
		/// </summary>
		public virtual int MaxConnections { get; set; }
		/// <summary>
		/// Returns an array of all active HttpProcessor instances being processed by this server, ordered in the order in which the TCP connections were accepted.
		/// </summary>
		/// <returns></returns>
		public HttpProcessor[] GetActiveHttpProcessors()
		{
			return ActiveHttpProcessors.Values.OrderBy(p => p.ConnectionID).ToArray();
		}
	}
	/// <summary>
	/// Base class for Http Web Servers that use a classic synchronous API with a thread pool for request handling.
	/// </summary>
	public abstract class HttpServer : HttpServerBase
	{
		/// <summary>
		/// The thread pool to use for processing client connections.
		/// </summary>
		public SimpleThreadPool pool = new SimpleThreadPool("SimpleHttp.HttpServer", 6, 48, 5000);
		/// <summary>
		/// Gets or sets maximum number of connections that should be allowed simultaneously. A minimum of 1 is required.
		/// </summary>
		public override int MaxConnections
		{
			get
			{
				return pool.MaxThreads;
			}
			set
			{
				if (value < 1)
					throw new ArgumentException("MaxConnections value must be greater than 0.  Value of " + value + " is invalid.");
				pool.SetThreadLimits(Math.Min(6, value), value);
			}
		}
		/// <summary>
		/// Constructs a new HttpServer.
		/// </summary>
		/// <param name="certificateSelector">(Optional) Certificate selector to use for https connections.  If null and an https-compatible endpoint was specified, a certificate is automatically created if necessary and loaded from "SimpleHttpServer-SslCert.pfx" in the same directory that the current executable is located in.</param>
		public HttpServer(ICertificateSelector certificateSelector = null) : base(certificateSelector) { }
		/// <summary>
		/// Handles an Http GET request.
		/// </summary>
		/// <param name="p">The HttpProcessor handling the request.</param>
		public abstract void handleGETRequest(HttpProcessor p);
		/// <summary>
		/// Handles an Http POST request.
		/// </summary>
		/// <param name="p">The HttpProcessor handling the request.</param>
		public abstract void handlePOSTRequest(HttpProcessor p);
		/// <summary>
		/// Handles requests using less common Http verbs such as "HEAD" or "PUT". See <see cref="HttpMethods"/>.
		/// </summary>
		/// <param name="p">The HttpProcessor handling the request.</param>
		/// <param name="method">The HTTP method string, e.g. "HEAD" or "PUT". See <see cref="HttpMethods"/>.</param>
		public virtual void handleOtherRequest(HttpProcessor p, string method)
		{
			if (method == HttpMethods.HEAD)
			{
				p.Response.Simple("405 Method Not Allowed");
				p.Response.Headers.Add("Allow", "GET");
			}
			else
				p.Response.Simple("501 Not Implemented");
		}
		/// <summary>
		/// Each incoming connection to the server is passed to this method.  If the method returns true, an HTTP "503 Service Unavailable" response will be written and the connection will not be processed by an <see cref="HttpProcessor"/>.
		/// </summary>
		/// <param name="tcpClient">The new connection from a client.</param>
		/// <returns>True if the connection should be dismissed with an HTTP 503 response.</returns>
		protected override bool IsServerTooBusyToProcessNewConnection(TcpClient tcpClient)
		{
			return pool.QueuedActionCount > pool.MaxThreads.Clamp(24, 256);
		}
		public override void Stop()
		{
			try { pool.Stop(); } catch (Exception ex) { SimpleHttpLogger.Log(ex); }

			base.Stop();
		}
	}
	/// <summary>
	/// Base class for Http Web Servers that use an async API.
	/// </summary>
	public abstract class HttpServerAsync : HttpServerBase
	{
		/// <summary>
		/// Constructs a new HttpServerAsync.
		/// </summary>
		/// <param name="certificateSelector">(Optional) Certificate selector to use for https connections.  If null and an https-compatible endpoint was specified, a certificate is automatically created if necessary and loaded from "SimpleHttpServer-SslCert.pfx" in the same directory that the current executable is located in.</param>
		public HttpServerAsync(ICertificateSelector certificateSelector = null) : base(certificateSelector) { }
		/// <summary>
		/// Asynchronously handles an HTTP request.
		/// </summary>
		/// <param name="p">The HttpProcessor handling the request.</param>
		/// <param name="method">The HTTP method string, e.g. "GET", "POST", HEAD", "PUT", etc. See <see cref="HttpMethods"/>.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns></returns>
		public abstract Task handleRequest(HttpProcessor p, string method, CancellationToken cancellationToken = default);
	}
}