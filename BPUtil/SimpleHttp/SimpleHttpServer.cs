using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
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
	/// Implements the HTTP 1.1 protocol for a given <see cref="TcpClient"/> and <see cref="HttpServer"/>.
	/// </summary>
	public class HttpProcessor : IProcessor
	{
		public static UTF8Encoding Utf8NoBOM = new UTF8Encoding(false);
		/// <summary>
		/// SslProtocols typed Tls13 that is available in earlier versions of .NET Framework.
		/// </summary>
		public const SslProtocols Tls13 = (SslProtocols)12288;
		private const int BUF_SIZE = 4096;
		private static int MAX_POST_SIZE = 10 * 1024 * 1024; // 10MB
		private readonly AllowedConnectionTypes allowedConnectionTypes;

		#region Fields and Properties
		/// <summary>
		/// The underlying tcpClient which handles the network connection.
		/// </summary>
		public TcpClient tcpClient { get; private set; }

		/// <summary>
		/// The HttpServer instance that accepted this request.
		/// </summary>
		public HttpServer srv { get; private set; }

		/// <summary>
		/// This stream is for reading and writing binary data.
		/// 
		/// Be careful to flush [tcpStream] or [outputStream] before switching between them!!
		/// 
		/// This stream is typically either a NetworkStream or a GzipStream.
		/// </summary>
		public Stream tcpStream { get; private set; }

		/// <summary>
		/// This stream is for writing text data.
		/// Be careful to flush [tcpStream] or [outputStream] before switching between them!!
		/// </summary>
		public StreamWriter outputStream { get; private set; }

		/// <summary>
		/// Be careful to flush each output stream before using a different one!!
		/// 
		/// This stream is for writing binary data.
		/// </summary>
		[Obsolete("This property is deprecated and may be removed in a future version of BPUtil. Use tcpStream instead.")]
		public Stream rawOutputStream { get { return tcpStream; } }

		/// <summary>
		/// The cookies sent by the remote client.
		/// </summary>
		public Cookies requestCookies { get; private set; }

		/// <summary>
		/// The cookies to send to the remote client.
		/// </summary>
		public Cookies responseCookies { get; private set; } = new Cookies();

		/// <summary>
		/// The Http method used.  i.e. "POST" or "GET"
		/// </summary>
		public string http_method { get; private set; }
		/// <summary>
		/// The base Uri for this server, containing its host name and port.
		/// </summary>
		public Uri base_uri_this_server { get; private set; }
		/// <summary>
		/// The requested url.
		/// </summary>
		public Uri request_url { get; private set; }
		/// <summary>
		/// The protocol version string sent by the client.  e.g. "HTTP/1.1"
		/// </summary>
		public string http_protocol_versionstring { get; private set; }
		/// <summary>
		/// <para>The path to and name of the requested page, not including the first '/'.</para>
		/// <para>For example, if the URL was "/articles/science/moon.html?date=2011-10-21", requestedPage would be "articles/science/moon.html".</para>
		/// <para>URL-encoded characters remain url-encoded. E.g. "File%20Name.jpg".</para>
		/// </summary>
		public string requestedPage { get; private set; }
		/// <summary>
		/// A map of HTTP header names and values. Header names are all normalized to standard capitalization, and header names are treated as case-insensitive.
		/// </summary>
		public HttpHeaderCollection httpHeaders { get; private set; } = new HttpHeaderCollection();

		/// <summary>
		/// A SortedList mapping lower-case keys to values of parameters.  This list is populated if and only if the request was a POST request with mimetype "application/x-www-form-urlencoded".
		/// </summary>
		public SortedList<string, string> PostParams { get; private set; } = new SortedList<string, string>();
		/// <summary>
		/// A SortedList mapping keys to values of parameters.  No character case conversion is applied in this list.  This list is populated if and only if the request was a POST request with mimetype "application/x-www-form-urlencoded".
		/// </summary>
		public SortedList<string, string> RawPostParams { get; private set; } = new SortedList<string, string>();
		/// <summary>
		/// A SortedList mapping lower-case keys to values of parameters.  This list is populated parameters that were appended to the url (the query string).  e.g. if the url is "mypage.html?arg1=value1&amp;arg2=value2", then there will be two parameters ("arg1" with value "value1" and "arg2" with value "value2")
		/// </summary>
		public SortedList<string, string> QueryString { get; private set; } = new SortedList<string, string>();
		/// <summary>
		/// A SortedList mapping keys to values of parameters.  No character case conversion is applied in this list.  This list is populated parameters that were appended to the url (the query string).  e.g. if the url is "mypage.html?arg1=value1&amp;arg2=value2", then there will be two parameters ("arg1" with value "value1" and "arg2" with value "value2")
		/// </summary>
		public SortedList<string, string> RawQueryString { get; private set; } = new SortedList<string, string>();

		/// <summary>
		/// The mimetype of the request body content.
		/// </summary>
		[Obsolete("This will go away in a future build.")]
		public string postContentType { get; private set; } = "";

		/// <summary>
		/// The raw posted content as a string, populated only if the mimetype was "application/x-www-form-urlencoded"
		/// </summary>
		[Obsolete("This will go away in a future build.")]
		public string postFormDataRaw { get; private set; } = "";

		/// <summary>
		/// A flag that is set when writeSuccess(), writeFailure(), or writeRedirect() is called.
		/// </summary>
		public bool responseWritten = false;

		/// <summary>
		/// True if a "Connection: keep-alive;" header was received from the client.
		/// </summary>
		public bool keepAliveRequested { get; private set; }

		/// <summary>
		/// <para>True if a "Connection: keep-alive;" header was sent to the client.</para>
		/// <para>This flag is reset to false at the start of each request.</para>
		/// <para>If true at the end of a request, the server will attempt to read another request from this connection.</para>
		/// </summary>
		public bool keepAlive { get; internal set; } = false;

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
		private IPAddress remoteIpAddress = null;
		/// <summary>
		/// Returns the remote client's IP address, or null if the remote IP address is somehow not available.
		/// </summary>
		public IPAddress RemoteIPAddress
		{
			get
			{
				if (remoteIpAddress != null)
					return remoteIpAddress;

				try
				{
					if (tcpClient != null && tcpClient.Client.RemoteEndPoint is IPEndPoint)
					{
						IPEndPoint ipep = (IPEndPoint)tcpClient.Client.RemoteEndPoint;
						remoteIpAddress = ipep.Address;
					}
				}
				catch (ThreadAbortException) { throw; }
				catch (SocketException) { throw; }
				catch (Exception ex)
				{
					SimpleHttpLogger.Log(ex);
				}
				return remoteIpAddress;
			}
		}

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
		public IPAddress TrueRemoteIPAddress
		{
			get
			{
				return ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address;
			}
		}
		#endregion

		/// <summary>
		/// If true, the connection is secured with TLS.
		/// </summary>
		public bool secure_https { get; private set; }
		/// <summary>
		/// An object responsible for delivering TLS server certificates upon demand.
		/// </summary>
		private ICertificateSelector certificateSelector;
		/// <summary>
		/// The type of compression that will be used for the response stream.
		/// </summary>
		public CompressionType compressionType { get; private set; } = CompressionType.None;

		/// <summary>
		/// Gets the Stream containing the request body. It will be seeked to the beginning before this HttpProcessor is sent to the HttpServer for handling. For requests that did not provide a body, this will be null.
		/// </summary>
		public Stream RequestBodyStream { get; private set; }

		/// <summary>
		/// Gets the MemoryStream containing the request body. It will be seeked to the beginning before this HttpProcessor is sent to the HttpServer for handling. For requests that did not provide a body, this will be null.
		/// </summary>
		[Obsolete("Use RequestBodyStream instead")]
		public MemoryStream PostBodyStream
		{
			get
			{
				if (RequestBodyStream != null && RequestBodyStream is MemoryStream)
					return (MemoryStream)RequestBodyStream;
				return null;
			}
		}

		/// <summary>
		/// The number of requests that have been read by the current TCP connection.
		/// </summary>
		public long keepAliveRequestCount { get; private set; } = 0;

		/// <summary>
		/// The hostname that was requested by the client.  This is populated from TLS Server Name Indication if available, otherwise from the Host header.  Null if not provided in either place.
		/// </summary>
		public string hostName { get; private set; }
		#endregion

		/// <summary>
		/// Constructs an HttpProcessor to handle an HTTP or HTTPS request from a client.
		/// </summary>
		/// <param name="client">TcpClient which is managing the client connection.</param>
		/// <param name="srv">The HttpServer instance which accepted the client connection.</param>
		/// <param name="certificateSelector"> An object responsible for delivering TLS server certificates upon demand. May be null only if [allowedConnectionTypes] does not include https.</param>
		/// <param name="allowedConnectionTypes">Enumeration flags indicating which protocols are allowed to be used.</param>
		public HttpProcessor(TcpClient client, HttpServer srv, ICertificateSelector certificateSelector, AllowedConnectionTypes allowedConnectionTypes)
		{
			if (allowedConnectionTypes.HasFlag(AllowedConnectionTypes.https) && certificateSelector == null)
				throw new ArgumentException("HttpProcessor was instructed to accept https requests but was not provided a certificate selector.", "certificateSelector");

			this.certificateSelector = certificateSelector;
			this.tcpClient = client;
			this.srv = srv;
			this.allowedConnectionTypes = allowedConnectionTypes;
		}

		public static string streamReadLine(Stream inputStream, int maxLength = 32768)
		{
			int next_char;
			bool endOfStream = false;
			bool didRead = false;
			StringBuilder data = new StringBuilder();
			while (true)
			{
				next_char = inputStream.ReadByte();
				if (next_char == -1)
				{
					endOfStream = true;
					break;
				};
				didRead = true;
				if (next_char == '\n') { break; }
				if (next_char == '\r') { continue; }
				if (data.Length >= maxLength)
					throw new HttpProcessorException("413 Entity Too Large");
				data.Append(Convert.ToChar(next_char));
			}
			if (endOfStream && !didRead)
				return null;
			return data.ToString();
		}

		/// <summary>
		/// Processes the request.
		/// </summary>
		public void Process()
		{
			try
			{
				tcpClient.SendBufferSize = 65536;
				tcpStream = tcpClient.GetStream();
				if (allowedConnectionTypes.HasFlag(AllowedConnectionTypes.https))
				{
					X509Certificate cert = null;
					try
					{
						// Read the TLS Client Hello message to get the server name so we can select the correct certificate and
						//   populate the hostName property of this HttpProcessor.
						// Note it is possible that the client is not using TLS, in which case no certificate will be selected and the
						//   request will be processed as plain HTTP.
						if (TLS.TlsServerNameReader.TryGetTlsClientHelloServerNames(tcpClient.Client, out string serverName, out bool isTlsAlpn01Validation))
						{
							hostName = serverName;
							if (isTlsAlpn01Validation)
							{
#if NET6_0
								cert = certificateSelector.GetAcmeTls1Certificate(this, serverName).Result;
								if (cert == null)
								{
									SimpleHttpLogger.LogVerbose("\"acme-tls/1\" protocol negotiation failed because the certificate selector [" + certificateSelector.GetType() + "] returned null certificate for server name " + (serverName == null ? "null" : ("\"" + serverName + "\"")) + ".");
									return;
								}
								SslServerAuthenticationOptions sslOptions = new SslServerAuthenticationOptions();
								sslOptions.ApplicationProtocols = new List<SslApplicationProtocol> { new SslApplicationProtocol("acme-tls/1") };
								sslOptions.ServerCertificate = cert;
								tcpStream = new SslStream(tcpStream, false, null, null, EncryptionPolicy.RequireEncryption);
								((SslStream)tcpStream).AuthenticateAsServer(sslOptions);
								SimpleHttpLogger.LogVerbose("\"acme-tls/1\" client connected using SslProtocol." + (tcpStream as SslStream).SslProtocol);
								return; // This connection is not allowed to be used for data transmission after TLS negotiation is complete.
#else
								SimpleHttpLogger.LogVerbose("\"acme-tls/1\" protocol negotiation failed because the current .NET version does not support the \"acme-tls/1\" protocol.");
								return;
#endif
							}
							else
								cert = certificateSelector.GetCertificate(this, serverName).Result;
							if (cert == null)
							{
								SimpleHttpLogger.LogVerbose("TLS negotiation failed because the certificate selector [" + certificateSelector.GetType() + "] returned null certificate for server name " + (serverName == null ? "null" : ("\"" + serverName + "\"")) + ".");
								return;
							}
							tcpStream = new SslStream(tcpStream, false, null, null, EncryptionPolicy.RequireEncryption);
							((SslStream)tcpStream).AuthenticateAsServer(cert, false, Tls13 | SslProtocols.Tls12, false);
							SimpleHttpLogger.LogVerbose("Client connected using SslProtocol." + (tcpStream as SslStream).SslProtocol);
							this.secure_https = true;
						}
						else
						{
							if (!allowedConnectionTypes.HasFlag(AllowedConnectionTypes.http))
							{
								SimpleHttpLogger.LogVerbose("Client " + this.RemoteIPAddressStr + " requested plain HTTP from an IP endpoint (" + tcpClient.Client.LocalEndPoint.ToString() + ") that is not configured to support plain HTTP.");
								return;
							}
							cert = null;
						}
					}
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
									// Happens unpredictably to some certificates when used with some clients.
									SimpleHttpLogger.LogVerbose("SslStream.AuthenticateAsServer --> An unknown error occurred while processing the certificate.");
									return;
								}
							}
						}
						SimpleHttpLogger.LogVerbose(ex);
						return;
					}
				}
				this.base_uri_this_server = new Uri("http" + (this.secure_https ? "s" : "") + "://" + tcpClient.Client.LocalEndPoint.ToString(), UriKind.Absolute);
				Stream originalTcpStream = tcpStream;
				do
				{
					if (keepAlive)
					{
						responseCookies = new Cookies();
						httpHeaders.Clear();
						PostParams.Clear();
						RawPostParams.Clear();
						QueryString.Clear();
						RawQueryString.Clear();
						http_method = "";
#pragma warning disable CS0618
						postContentType = postFormDataRaw = "";
#pragma warning restore
						responseWritten = false;
						keepAliveRequested = false;
						compressionType = CompressionType.None;
					}
					keepAlive = false;
					keepAliveRequestCount++;
					tcpStream = originalTcpStream;
					//int inputStreamThrottlingRuleset = 1;
					//int outputStreamThrottlingRuleset = 0;
					//if (IsLanConnection)
					//	inputStreamThrottlingRuleset = outputStreamThrottlingRuleset = 2;
					//inputStream =  new GlobalThrottledStream(tcpStream, inputStreamThrottlingRuleset, RemoteIPAddressInt);
					//rawOutputStream = new GlobalThrottledStream(tcpStream, outputStreamThrottlingRuleset, RemoteIPAddressInt);
					outputStream = new StreamWriter(tcpStream, Utf8NoBOM, tcpClient.SendBufferSize, true);
					try
					{
						tcpClient.ReceiveTimeout = keepAliveRequestCount <= 1 ? 10000 : 60000;
						if (!parseRequest())
							return; // End of stream was encountered. Very common with "Connection: keep-alive" when another request does not arrive.
						readHeaders(tcpStream, httpHeaders);
						if (string.IsNullOrWhiteSpace(hostName))
						{
							hostName = GetHeaderValue("host");
							if (hostName != null)
							{
								string portSuffix = ":" + ((IPEndPoint)tcpClient.Client.LocalEndPoint).Port;
								if (hostName.EndsWith(portSuffix))
									hostName = hostName.Substring(0, hostName.Length - portSuffix.Length);
							}
						}
						if (string.IsNullOrWhiteSpace(hostName))
							hostName = null;
						keepAliveRequested = "keep-alive".IEquals(GetHeaderValue("connection"));
						IPAddress originalRemoteIp = RemoteIPAddress;
						if (srv.XRealIPHeader)
						{
							string headerValue = GetHeaderValue("x-real-ip");
							if (!string.IsNullOrWhiteSpace(headerValue))
							{
								if (srv.IsTrustedProxyServer(this, originalRemoteIp))
								{
									headerValue = headerValue.Trim();
									if (IPAddress.TryParse(headerValue, out IPAddress addr))
										remoteIpAddress = addr;
								}
							}
						}
						if (srv.XForwardedForHeader)
						{
							string headerValue = GetHeaderValue("x-forwarded-for");
							if (!string.IsNullOrWhiteSpace(headerValue))
							{
								if (srv.IsTrustedProxyServer(this, originalRemoteIp))
								{
									// Because we trust the source of the header, we must trust that they validated the chain of IP addresses all the way back to the root.
									// Therefore we should get the leftmost address; this is the true client IP.
									headerValue = headerValue.Split(',').FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
									if (headerValue != null)
									{
										headerValue = headerValue.Trim();
										if (IPAddress.TryParse(headerValue, out IPAddress addr))
											remoteIpAddress = addr;
									}
								}
							}
						}
						RawQueryString = ParseQueryStringArguments(this.request_url.Query, preserveKeyCharacterCase: true);
						QueryString = ParseQueryStringArguments(this.request_url.Query);
						requestCookies = Cookies.FromString(GetHeaderValue("Cookie", ""));

						if (HttpMethods.IsValid(http_method))
						{
							if (shouldLogRequestsToFile())
								SimpleHttpLogger.LogRequest(DateTime.Now, this.RemoteIPAddressStr, http_method, request_url.OriginalString);
							handleRequest();
						}
						else
						{
							this.writeFailure("501 Not Implemented");
						}
					}
					catch (ThreadAbortException) { throw; }
					catch (Exception e)
					{
						if (shouldLogRequestsToFile())
							SimpleHttpLogger.LogRequest(DateTime.Now, this.RemoteIPAddressStr, "FAIL", request_url?.OriginalString);
						if (IsOrdinaryDisconnectException(e))
						{
							//if (keepAliveRequestCount <= 1) // Do not log if this is a kept-alive connection.
							//	SimpleHttpLogger.LogVerbose(e);
							if (!responseWritten)
								this.writeFailure("500 Internal Server Error", "An error occurred while processing this request."); // This response should probably fail because the client has disconnected.
							return;
						}
						else if (e.GetExceptionOfType<HttpProcessorException>() != null)
						{
							SimpleHttpLogger.LogVerbose(e);
							if (!responseWritten)
								this.writeFailure(e.Message);
						}
						else if (e.GetExceptionOfType<HttpProtocolException>() != null)
						{
							SimpleHttpLogger.LogVerbose(e);
							if (!responseWritten)
								this.writeFailure("400 Bad Request", "The request cannot be fulfilled due to bad syntax.");
							return;
						}
						else
						{
							SimpleHttpLogger.Log(e);
							if (!responseWritten)
								this.writeFailure("500 Internal Server Error", "An error occurred while processing this request.");
						}
					}
					finally
					{
						if (!responseWritten)
							this.writeFailure();
					}
					outputStream.Flush();
					if (tcpStream is ChunkedTransferEncodingStream)
						(tcpStream as ChunkedTransferEncodingStream).WriteFinalChunk();
					tcpStream.Flush();
					// For some reason, GZip compression only works if we dispose streams here, not in the finally block.
					try
					{
						outputStream.Dispose(); // Safe to dispose StreamWriter because it was created with the leaveOpen option.
					}
					catch (ThreadAbortException) { throw; }
					catch (Exception ex) { SimpleHttpLogger.LogVerbose(ex); }
					try
					{
						if (tcpStream is GZipStream)
							tcpStream.Dispose(); // Safe to dispose GZipStream because it was created with the leaveOpen option.
					}
					catch (ThreadAbortException) { throw; }
					catch (Exception ex) { SimpleHttpLogger.LogVerbose(ex); }
					outputStream = null;
					tcpStream = null;
				}
				while (keepAlive && CheckIfStillConnected());
			}
			catch (ThreadAbortException) { throw; }
			catch (Exception ex)
			{
				if (!IsOrdinaryDisconnectException(ex))
					SimpleHttpLogger.LogVerbose(ex);
			}
			finally
			{
				try
				{
					tcpClient?.Close();
				}
				catch (ThreadAbortException) { throw; }
				catch (Exception ex) { SimpleHttpLogger.LogVerbose(ex); }
			}
		}
		/// <summary>
		/// Returns true if the specified Exception is a SocketException or EndOfStreamException or if one of these exception types is contained within the InnerException tree.
		/// </summary>
		/// <param name="ex">The exception.</param>
		/// <returns></returns>
		public static bool IsOrdinaryDisconnectException(Exception ex)
		{
			//if (ex is IOException)
			//{
			//	if (ex.InnerException != null && ex.InnerException is SocketException)
			//	{
			//		//if (ex.InnerException.Message.Contains("An established connection was aborted by the software in your host machine")
			//		//	|| ex.InnerException.Message.Contains("An existing connection was forcibly closed by the remote host")
			//		//	|| ex.InnerException.Message.Contains("The socket has been shut down") /* Mono/Linux */
			//		//	|| ex.InnerException.Message.Contains("Connection reset by peer") /* Mono/Linux */
			//		//	|| ex.InnerException.Message.Contains("The socket is not connected") /* Mono/Linux */
			//		//	)
			//		return true; // Connection aborted.  This happens often enough that reporting it can be excessive.
			//	}
			//}
			if (ex is SocketException && (ex as SocketException).SocketErrorCode != SocketError.InvalidArgument)
				return true;
			if (ex is HttpProcessor.EndOfStreamException)
				return true;
			if (ex is System.IO.EndOfStreamException)
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
		private bool shouldLogRequestsToFile()
		{
			return srv.shouldLogRequestsToFile();
		}
		// The following function was the start of an attempt to support basic authentication, but I have since decided against it as basic authentication is very insecure.
		//private NetworkCredential ParseAuthorizationCredentials()
		//{
		//    string auth = this.httpHeaders["Authorization"].ToString();
		//    if (auth != null && auth.StartsWith("Basic "))
		//    {
		//        byte[] bytes =  System.Convert.FromBase64String(auth.Substring(6));
		//        string creds = ASCIIEncoding.ASCII.GetString(bytes);

		//    }
		//    return new NetworkCredential();
		//}

		/// <summary>
		/// Parses the first line of the http request to get the request method, url, and protocol version. Returns true if successful or false if we encountered the end of the stream.  May throw various exceptions.
		/// </summary>
		private bool parseRequest()
		{
			string request = streamReadLine(tcpStream);
			if (request == null)
				return false;
			string[] tokens = request.Split(' ');
			if (tokens.Length != 3)
				throw new HttpProtocolException("invalid http request line: " + request);
			http_method = tokens[0].ToUpper();

			if (tokens[1].StartsWith("http://") || tokens[1].StartsWith("https://"))
				request_url = new Uri(tokens[1]);
			else
				request_url = new Uri(base_uri_this_server, tokens[1]);

			requestedPage = request_url.AbsolutePath.StartsWith("/") ? request_url.AbsolutePath.Substring(1) : request_url.AbsolutePath;

			http_protocol_versionstring = tokens[2];
			return true;
		}

		/// <summary>
		/// Parses the http headers
		/// </summary>
		/// <param name="tcpStream">Input stream.</param>
		/// <param name="httpHeaders">Http header collection where parsed headers should be stored.</param>
		internal static void readHeaders(Stream tcpStream, HttpHeaderCollection httpHeaders)
		{
			string line;
			while ((line = streamReadLine(tcpStream)) != "")
			{
				if (line == null)
					throw new EndOfStreamException();
				int separator = line.IndexOf(':');
				if (separator == -1)
					throw new HttpProtocolException("invalid http header line: " + line);
				string name = line.Substring(0, separator);
				int pos = separator + 1;
				while (pos < line.Length && line[pos] == ' ')
					pos++; // strip any spaces

				string value = line.Substring(pos);
				httpHeaders.Add(name, value);
			}
		}

		internal static async Task<string> streamReadLineAsync(StreamReader reader, int maxLength = 32768)
		{
			string str = await reader.ReadLineAsync();
			if (str.Length >= maxLength)
				throw new HttpProcessorException("413 Entity Too Large");
			return str; // Null if end of stream.
		}

		/// <summary>
		/// <para>Handles all request methods.</para>
		/// <para>This method data processing just reads the entire request body into a memory stream.</para>
		/// <para>This is fine for smallish things, but for large stuff we should really hand an input stream to the request processor.  However, the input stream we hand to the user's code needs to see the "end of the stream" at the appropriate time!</para>
		/// <para>// TODO: Make this handle Transfer-Encoding: chunked if Content-Length is not provided.</para>
		/// </summary>
		private void handleRequest(bool requestBodyRequired = false)
		{
			using (MemoryStream ms = new MemoryStream())
			{
#pragma warning disable CS0618
				string contentType = postContentType = GetHeaderValue("Content-Type");
#pragma warning restore
				string content_length_str = GetHeaderValue("Content-Length");
				if (!string.IsNullOrWhiteSpace(content_length_str))
				{
					if (int.TryParse(content_length_str, out int content_len))
					{
						if (content_len > MAX_POST_SIZE)
						{
							SimpleHttpLogger.LogVerbose(this.http_method + " Content-Length(" + content_len + ") too big for this simple server.  Server can handle up to " + MAX_POST_SIZE);
							this.writeFailure("413 Request Entity Too Large", "Request Too Large");
							return;
						}
						RequestBodyStream = ms;
						byte[] buf = new byte[BUF_SIZE];
						int to_read = content_len;
						while (to_read > 0)
						{
							int numread = this.tcpStream.Read(buf, 0, Math.Min(BUF_SIZE, to_read));
							if (numread == 0)
							{
								if (to_read == 0)
									break;
								else
								{
									SimpleHttpLogger.LogVerbose("client disconnected during post");
									return;
								}
							}
							to_read -= numread;
							ms.Write(buf, 0, numread);
						}
						ms.Seek(0, SeekOrigin.Begin);
					}
				}
				else if (this.http_method == "POST" || this.http_method == "PUT" || this.http_method == "PATCH")
				{
					this.writeFailure("411 Length Required", "The request did not specify the length of its content.");
					SimpleHttpLogger.LogVerbose("The request did not specify the length of its content.  This server requires that all POST requests include a Content-Length header.");
					return;
				}

				if (contentType != null && contentType.Contains("application/x-www-form-urlencoded"))
				{
					StreamReader sr = new StreamReader(ms, Utf8NoBOM);
#pragma warning disable CS0618
					string formDataRaw = postFormDataRaw = sr.ReadToEnd();
#pragma warning restore
					ms.Seek(0, SeekOrigin.Begin);

					RawPostParams = ParseQueryStringArguments(formDataRaw, false, true, convertPlusToSpace: true);
					PostParams = ParseQueryStringArguments(formDataRaw, false, convertPlusToSpace: true);

					try
					{
						if (this.http_method == "GET")
							srv.handleGETRequest(this);
						else if (this.http_method == "POST")
							srv.handlePOSTRequest(this, null);
						else
							srv.handleOtherRequest(this, http_method);
					}
					finally
					{
						sr.Dispose(); // Disposes underlying MemoryStream because we did not construct the StreamReader with leaveOpen flag.
					}
				}
				else
				{
					if (this.http_method == "GET")
						srv.handleGETRequest(this);
					else if (this.http_method == "POST")
						srv.handlePOSTRequest(this, new StreamReader(ms, Utf8NoBOM));
					else
						srv.handleOtherRequest(this, http_method);
				}
				RequestBodyStream = null;
			}
		}
		#region Response Compression
		/// <summary>
		/// Automatically compresses the response body using gzip encoding, if the client requested it.
		/// Must be called BEFORE writeSuccess().
		/// Note that the Content-Length header, if provided, should be the COMPRESSED length, so you likely won't know what value to use.  Omit the header instead.
		/// Returns true if the response will be compressed, and sets this.compressionType.
		/// </summary>
		/// <returns></returns>
		public virtual bool CompressResponseIfCompatible()
		{
			if (responseWritten)
				return false;
			if (ClientRequestsGZipCompression)
			{
				compressionType = CompressionType.GZip;
				return true;
			}
			return false;
		}
		/// <summary>
		/// Called automatically by writeSuccess method; flushes the existing output streams and wraps them in a gzipstream if gzip compression is to be used.
		/// </summary>
		private void EnableCompressionIfSet()
		{
			if (!responseWritten)
				return;
			if (compressionType == CompressionType.GZip)
			{
				if (tcpStream is GZipStream)
					return;
				outputStream.Flush();
				tcpStream.Flush();
				tcpStream = new GZipStream(tcpStream, CompressionLevel.Optimal, true);
				outputStream = new StreamWriter(tcpStream, ByteUtil.Utf8NoBOM, tcpClient.SendBufferSize, true);
			}
		}
		/// <summary>
		/// Called automatically by writeSuccess method; flushes the existing output streams and wraps them in a ChunkedTransferEncodingStream which ensures that all future data written to tcpStream or outputStream will be have appropriate chunk headers and footers written.
		/// </summary>
		private void EnableTransferEncodingChunked()
		{
			if (!responseWritten)
				return;
			if (tcpStream is ChunkedTransferEncodingStream)
				return;
			outputStream.Flush();
			tcpStream.Flush();
			tcpStream = new ChunkedTransferEncodingStream(tcpStream);
			outputStream = new StreamWriter(tcpStream, Utf8NoBOM, tcpClient.SendBufferSize, true);
		}
		/// <summary>
		/// Returns true if the client has requested gzip compression.
		/// </summary>
		public bool ClientRequestsGZipCompression
		{
			get
			{
				string acceptEncoding = GetHeaderValue("Accept-Encoding", "");
				string[] types = acceptEncoding.Split(',');
				foreach (string type in types)
					if (type.Trim().ToLower() == "gzip")
					{
						return true;
					}
				return false;
			}
		}
		#endregion
		/// <summary>
		/// Writes the response headers for a successful response.  Call this one time before writing your response, after you have determined that the request is valid.
		/// </summary>
		/// <param name="contentType">The MIME type of your response.</param>
		/// <param name="contentLength">(OPTIONAL) The length of your response, in bytes, if you know it. If you don't know it, provide -1, and if keep-alive is being used, then "Transfer-Encoding: chunked" will be used automatically on the output stream and you don't need to know anything about it.</param>
		/// <param name="responseCode">(OPTIONAL) The response code and optional status string.</param>
		/// <param name="additionalHeaders">(OPTIONAL) Additional headers to include in the response.</param>
		public virtual void writeSuccess(string contentType = "text/html; charset=utf-8", long contentLength = -1, string responseCode = "200 OK", HttpHeaderCollection additionalHeaders = null)
		{
			if (responseWritten)
				throw new Exception("A response has already been written to this stream.");

			responseWritten = true;
			HashSet<string> reservedHeaderKeys = new HashSet<string>();
			reservedHeaderKeys.Add("Connection");

			outputStream.WriteLineRN("HTTP/1.1 " + responseCode);
			if (!string.IsNullOrEmpty(contentType))
				WriteReservedHeader(reservedHeaderKeys, "Content-Type", contentType);
			if (contentLength > -1)
				WriteReservedHeader(reservedHeaderKeys, "Content-Length", contentLength.ToString());
			bool chunkedTransferEncoding = this.keepAliveRequested && contentLength < 0;
			if (chunkedTransferEncoding)
				WriteReservedHeader(reservedHeaderKeys, "Transfer-Encoding", "chunked");
			if (compressionType == CompressionType.GZip)
				WriteReservedHeader(reservedHeaderKeys, "Content-Encoding", "gzip");
			string cookieStr = responseCookies.ToString();
			if (!string.IsNullOrEmpty(cookieStr))
				outputStream.WriteLineRN(cookieStr);
			if (additionalHeaders != null)
				foreach (KeyValuePair<string, string> header in additionalHeaders)
				{
					if (reservedHeaderKeys.Contains(header.Key, true))
						throw new ApplicationException("writeSuccess() additionalHeaders conflict: Header \"" + header.Key + "\" is already predetermined for this response.");
					outputStream.WriteLineRN(header.Key + ": " + header.Value);
				}
			if (this.keepAliveRequested)
				outputStream.WriteLineRN("Connection: keep-alive");
			else
				outputStream.WriteLineRN("Connection: close");
			outputStream.WriteLineRN("");

			this.keepAlive = this.keepAliveRequested;

			tcpClient.NoDelay = true;
			EnableCompressionIfSet();
			if (chunkedTransferEncoding)
				EnableTransferEncodingChunked();
		}
		private void WriteReservedHeader(HashSet<string> reservedHeaderKeys, string key, string value)
		{
			reservedHeaderKeys.Add(key);
			outputStream.WriteLineRN(key + ": " + value);
		}

		/// <summary>
		/// Writes a failure response header.  Call this one time to return an error response.
		/// </summary>
		/// <param name="code">(OPTIONAL) The http error code (including explanation entity).  For example: "404 Not Found" where 404 is the error code and "Not Found" is the explanation.</param>
		/// <param name="description">(OPTIONAL) A description string to send after the headers as the response.  This is typically shown to the remote user in his browser.  If null, the code string is sent here.  If "", no response body is sent by this function, and you may or may not want to write your own.</param>
		/// <param name="additionalHeaders">(OPTIONAL) Additional headers to include in the response.</param>
		public virtual void writeFailure(string code = "404 Not Found", string description = null, HttpHeaderCollection additionalHeaders = null)
		{
			if (responseWritten)
				throw new Exception("A response has already been written to this stream.");
			if (code == null)
				throw new ArgumentNullException("code", "HttpProcessor.writeFailure() requires a non-null code argument.");

			if (description == null)
				description = code;
			int contentLength = ByteUtil.Utf8NoBOM.GetByteCount(description);

			responseWritten = true;
			HashSet<string> reservedHeaderKeys = new HashSet<string>();
			reservedHeaderKeys.Add("Connection");
			outputStream.WriteLineRN("HTTP/1.1 " + code);
			WriteReservedHeader(reservedHeaderKeys, "Content-Type", "text/plain; charset=utf-8");
			WriteReservedHeader(reservedHeaderKeys, "Content-Length", contentLength.ToString());
			if (additionalHeaders != null)
				foreach (KeyValuePair<string, string> header in additionalHeaders)
				{
					if (reservedHeaderKeys.Contains(header.Key, true))
						throw new ApplicationException("writeFailure() additionalHeaders conflict: Header \"" + header.Key + "\" is already predetermined for this response.");
					outputStream.WriteLineRN(header.Key + ": " + header.Value);
				}
			if (this.keepAliveRequested)
				outputStream.WriteLineRN("Connection: keep-alive");
			else
				outputStream.WriteLineRN("Connection: close");
			outputStream.WriteLineRN("");
			if (description != "")
				outputStream.Write(description);
		}

		/// <summary>
		/// Writes a redirect header instructing the remote user's browser to load the URL you specify.  Call this one time and do not write any other data to the response stream.
		/// </summary>
		/// <param name="redirectToUrl">URL to redirect to.</param>
		public virtual void writeRedirect(string redirectToUrl)
		{
			if (responseWritten)
				throw new Exception("A response has already been written to this stream.");
			responseWritten = true;
			outputStream.WriteLineRN("HTTP/1.1 302 Found");
			outputStream.WriteLineRN("Location: " + redirectToUrl);
			outputStream.WriteLineRN("Connection: close");
			outputStream.WriteLineRN("");
		}

		/// <summary>
		/// Writes response headers to finish the WebSocket handshake with the client. No extensions are supported (such as compression) at this time.
		/// </summary>
		public virtual void writeWebSocketUpgrade()
		{
			if (responseWritten)
				throw new Exception("A response has already been written to this stream.");
			responseWritten = true;
			outputStream.WriteLineRN("HTTP/1.1 101 Switching Protocols");
			outputStream.WriteLineRN("Upgrade: websocket");
			outputStream.WriteLineRN("Connection: Upgrade");
			outputStream.WriteLineRN("Sec-WebSocket-Accept: " + WebSockets.WebSocket.CreateSecWebSocketAcceptValue(this.GetHeaderValue("sec-websocket-key")));
			outputStream.WriteLineRN("");
		}

		/// <summary>
		/// Writes the specified response with the Content-Length header set appropriately.
		/// </summary>
		/// <param name="body">Data to send in the response. This string will be encoded as UTF8.</param>
		/// <param name="contentType">Content-Type header value. e.g. "text/html; charset=utf-8"</param>
		/// <param name="responseCode">(OPTIONAL) The response code and optional status string.</param>
		/// <param name="additionalHeaders">(OPTIONAL) Additional headers to include in the response.</param>
		public virtual void writeFullResponseUTF8(string body, string contentType, string responseCode = "200 OK", HttpHeaderCollection additionalHeaders = null)
		{
			writeFullResponseBytes(ByteUtil.Utf8NoBOM.GetBytes(body), contentType, responseCode, additionalHeaders);
		}

		/// <summary>
		/// Writes the specified response with the Content-Length header set appropriately.
		/// </summary>
		/// <param name="body">Data to send in the response.</param>
		/// <param name="contentType">Content-Type header value. e.g. "application/octet-stream"</param>
		/// <param name="responseCode">(OPTIONAL) The response code and optional status string.</param>
		/// <param name="additionalHeaders">(OPTIONAL) Additional headers to include in the response.</param>
		public virtual void writeFullResponseBytes(byte[] body, string contentType, string responseCode = "200 OK", HttpHeaderCollection additionalHeaders = null)
		{
			writeSuccess(contentType, body.Length, responseCode, additionalHeaders);
			this.outputStream.Flush();
			this.tcpStream.Write(body, 0, body.Length);
		}

		/// <summary>
		/// Gets the value of the header, or null if the header does not exist.  The name is case insensitive.
		/// </summary>
		/// <param name="name">The case insensitive name of the header to get the value of.</param>
		/// <param name="defaultValue">The default value to return, in case the value did not exist.</param>
		/// <returns>The value of the header, or null if the header did not exist.</returns>
		public string GetHeaderValue(string name, string defaultValue = null)
		{
			name = name.ToLower();
			string value;
			if (!httpHeaders.TryGetValue(name, out value))
				value = defaultValue;
			return value;
		}

		#region Request Proxy Http(s)
		/// <summary>
		/// A thread pool to be used when additional threads are needed for proxying data.
		/// </summary>
		protected static SimpleThreadPool proxyResponseThreadPool = new SimpleThreadPool("ProxyResponses", 0, 1024, 10000);
		/// <summary>
		/// Acts as a proxy server, sending the request to a different URL.  This method starts a new (and unpooled) thread to handle the response from the remote server.
		/// The "Host" header is rewritten (or added) and output as the first header.
		/// </summary>
		/// <param name="newUrl">The URL to proxy the original request to.</param>
		/// <param name="networkTimeoutMs">The send and receive timeout to set for both TcpClients, in milliseconds.</param>
		/// <param name="acceptAnyCert">If true, certificate validation will be disabled for outgoing https connections.</param>
		/// <param name="snoopy">If non-null, proxied communication will be copied into this object so you can snoop on it.</param>
		/// <param name="host">The value of the host header, also used in SSL authentication. If null or whitespace, it is set from the [newUrl] parameter.</param>
		/// <param name="allowKeepalive">[DANGEROUS TO SET = true] If false, a Connection: close header will be added, and any existing Connection header will be dropped.  If true, the Connection header from the client will be preserved.  Assuming the client wanted keep-alive, this proxy request will remain active and the next request to come in on this connection will be proxied even if you didn't want it to be.  If your web server does ANYTHING ELSE besides proxy requests straight through to the same destination, this may result in client requests going to the wrong place.</param>
		public void ProxyTo(string newUrl, int networkTimeoutMs = 60000, bool acceptAnyCert = false, ProxyDataBuffer snoopy = null, string host = null, bool allowKeepalive = false)
		{
			if (responseWritten)
				throw new Exception("A response has already been written to this stream.");
			//try
			//{
			// Connect to the server we're proxying to.
			Uri newUri = new Uri(newUrl);
			if (string.IsNullOrWhiteSpace(host))
				host = newUri.DnsSafeHost;
			IPAddress ip = DnsHelper.GetHostAddressAsync(newUri.DnsSafeHost).Result;

			using (TcpClient proxyClient = new TcpClient(ip.AddressFamily))
			{
				proxyClient.ReceiveTimeout = this.tcpClient.ReceiveTimeout = networkTimeoutMs;
				proxyClient.SendTimeout = this.tcpClient.SendTimeout = networkTimeoutMs;
				proxyClient.Connect(ip, newUri.Port);
				Stream proxyStream = proxyClient.GetStream();
				responseWritten = true;
				if (newUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
				{
					//try
					//{
					RemoteCertificateValidationCallback certCallback = null;
					if (acceptAnyCert)
						certCallback = (sender, certificate, chain, sslPolicyErrors) => true;
					proxyStream = new SslStream(proxyStream, false, certCallback, null);
					((SslStream)proxyStream).AuthenticateAsClient(host, null, Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, false);
					//}
					//catch (ThreadAbortException) { throw; }
					//catch (SocketException) { throw; }
					//catch (Exception ex)
					//{
					//	SimpleHttpLogger.LogVerbose(ex);
					//	return ex;
					//}
				}

				// Begin proxying by sending what we've already read from this.inputStream.
				// The first line of our HTTP request will be different from the original.
				_ProxyString(ProxyDataDirection.RequestToServer, proxyStream, http_method + ' ' + newUri.PathAndQuery + ' ' + http_protocol_versionstring + "\r\n", snoopy);
				// After the first line come the headers.
				_ProxyString(ProxyDataDirection.RequestToServer, proxyStream, "Host: " + host + "\r\n", snoopy);
				if (!allowKeepalive)
					_ProxyString(ProxyDataDirection.RequestToServer, proxyStream, "Connection: close\r\n", snoopy);
				foreach (KeyValuePair<string, string> header in httpHeaders)
				{
					if (header.Key != "Host" && (allowKeepalive || header.Key != "Connection"))
						_ProxyString(ProxyDataDirection.RequestToServer, proxyStream, header.Key + ": " + header.Value + "\r\n", snoopy);
				}
				_ProxyString(ProxyDataDirection.RequestToServer, proxyStream, "\r\n", snoopy);

				// Write the original request body if there was one.
				if (RequestBodyStream != null)
				{
					if (RequestBodyStream is MemoryStream)
					{
						MemoryStream ms = (MemoryStream)RequestBodyStream;
						long remember_position = ms.Position;
						ms.Seek(0, SeekOrigin.Begin);
						byte[] buf = ms.ToArray();
						_ProxyData(ProxyDataDirection.RequestToServer, proxyStream, buf, buf.Length, snoopy);
						ms.Seek(remember_position, SeekOrigin.Begin);
					}
					else
						CopyStreamUntilClosed(ProxyDataDirection.RequestToServer, RequestBodyStream, proxyStream, snoopy);
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
						SimpleHttpLogger.LogVerbose(ex);
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
		/// Acts as a proxy server, sending the request to a different URL.  This method starts a new (and unpooled) thread to handle the response from the remote server.
		/// The "Host" header is rewritten (or added) and output as the first header.
		/// </summary>
		/// <param name="newUrl">The URL to proxy the original request to.</param>
		/// <param name="options">Optional options to control the behavior of the proxy request.</param>
		public async Task ProxyToAsync(string newUrl, Client.ProxyOptions options = null)
		{
			options?.bet?.Start("Entering ProxyToAsync");
			await Client.ProxyClient.ProxyRequest(this, new Uri(newUrl), options);
			options?.bet?.Stop();
		}
		#endregion

		#region Parameter parsing
		/// <summary>
		/// Parses the specified query string and returns a sorted list containing the arguments found in the specified query string.  Can also be used to parse the POST request body if the mimetype is "application/x-www-form-urlencoded".
		/// </summary>
		/// <param name="queryString"></param>
		/// <param name="requireQuestionMark"></param>
		/// <param name="preserveKeyCharacterCase">(Optional) If true, query string argument keys will be case sensitive.</param>
		/// <param name="convertPlusToSpace">(Optional) If true, query string argument values will have any plus signs converted to spaces before URL decoding.</param>
		/// <returns></returns>
		protected static SortedList<string, string> ParseQueryStringArguments(string queryString, bool requireQuestionMark = true, bool preserveKeyCharacterCase = false, bool convertPlusToSpace = false)
		{
			SortedList<string, string> arguments = new SortedList<string, string>();
			int idx = queryString.IndexOf('?');
			if (idx > -1)
				queryString = queryString.Substring(idx + 1);
			else if (requireQuestionMark)
				return arguments;
			idx = queryString.LastIndexOf('#');
			string hash = null;
			if (idx > -1)
			{
				hash = queryString.Substring(idx + 1);
				queryString = queryString.Remove(idx);
			}
			string[] parts = queryString.Split(new char[] { '&' });
			for (int i = 0; i < parts.Length; i++)
			{
				string[] argument = parts[i].Split(new char[] { '=' });
				if (argument.Length == 2)
				{
					if (convertPlusToSpace)
						argument[1] = argument[1].Replace('+', ' ');
					string key = Uri.UnescapeDataString(argument[0]);
					if (!preserveKeyCharacterCase)
						key = key.ToLower();
					string existingValue;
					if (arguments.TryGetValue(key, out existingValue))
						arguments[key] += "," + Uri.UnescapeDataString(argument[1]);
					else
						arguments[key] = Uri.UnescapeDataString(argument[1]);
				}
			}
			if (hash != null)
				arguments["#"] = hash;
			return arguments;
		}

		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <returns>The value of the key, or empty string if the key does not exist or has no value.</returns>
		public string GetParam(string key)
		{
			return GetQSParam(key);
		}
		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <param name="defaultValue">The default value to return, in case the value did not exist or was not compatible with the data type.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value.</returns>
		public int GetIntParam(string key, int defaultValue = 0)
		{
			return GetQSIntParam(key, defaultValue);
		}
		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <param name="defaultValue">The default value to return, in case the value did not exist or was not compatible with the data type.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value.</returns>
		public long GetLongParam(string key, long defaultValue = 0)
		{
			return GetQSLongParam(key, defaultValue);
		}
		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <param name="defaultValue">The default value to return, in case the value did not exist or was not compatible with the data type.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value.</returns>
		public double GetDoubleParam(string key, double defaultValue = 0)
		{
			return GetQSDoubleParam(key, defaultValue);
		}
		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <returns>The value of the key. This function interprets a value of "1" or "true" (case insensitive) as being true.  Any other parameter value is interpreted as false.</returns>
		public bool GetBoolParam(string key)
		{
			return GetQSBoolParam(key);
		}
		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <returns>The value of the key, or empty string if the key does not exist or has no value.</returns>
		public string GetQSParam(string key)
		{
			if (key == null)
				return "";
			string value;
			if (QueryString.TryGetValue(key.ToLower(), out value))
				return value;
			return "";
		}
		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <param name="defaultValue">The default value to return, in case the value did not exist or was not compatible with the data type.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value.</returns>
		public int GetQSIntParam(string key, int defaultValue = 0)
		{
			if (key == null)
				return defaultValue;
			int value;
			if (int.TryParse(GetQSParam(key.ToLower()), out value))
				return value;
			return defaultValue;
		}
		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <param name="defaultValue">The default value to return, in case the value did not exist or was not compatible with the data type.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value.</returns>
		public long GetQSLongParam(string key, long defaultValue = 0)
		{
			if (key == null)
				return defaultValue;
			long value;
			if (long.TryParse(GetQSParam(key.ToLower()), out value))
				return value;
			return defaultValue;
		}
		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <param name="defaultValue">The default value to return, in case the value did not exist or was not compatible with the data type.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value.</returns>
		public double GetQSDoubleParam(string key, double defaultValue = 0)
		{
			if (key == null)
				return defaultValue;
			double value;
			if (double.TryParse(GetQSParam(key.ToLower()), out value))
				return value;
			return defaultValue;
		}
		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <returns>The value of the key. This function interprets a value of "1" or "true" (case insensitive) as being true.  Any other parameter value is interpreted as false.</returns>
		public bool GetQSBoolParam(string key)
		{
			string param = GetQSParam(key);
			if (param == "1" || param.ToLower() == "true")
				return true;
			return false;
		}
		/// <summary>
		/// Returns the value of a parameter sent via POST with MIME type "application/x-www-form-urlencoded".
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <returns>The value of the key, or empty string if the key does not exist or has no value.</returns>
		public string GetPostParam(string key)
		{
			if (key == null)
				return "";
			string value;
			if (PostParams.TryGetValue(key.ToLower(), out value))
				return value;
			return "";
		}
		/// <summary>
		/// Returns the value of a parameter sent via POST with MIME type "application/x-www-form-urlencoded".
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <param name="defaultValue">The default value to return, in case the value did not exist or was not compatible with the data type.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value.</returns>
		public int GetPostIntParam(string key, int defaultValue = 0)
		{
			if (key == null)
				return defaultValue;
			int value;
			if (int.TryParse(GetPostParam(key.ToLower()), out value))
				return value;
			return defaultValue;
		}
		/// <summary>
		/// Returns the value of a parameter sent via POST with MIME type "application/x-www-form-urlencoded".
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <param name="defaultValue">The default value to return, in case the value did not exist or was not compatible with the data type.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value.</returns>
		public long GetPostLongParam(string key, long defaultValue = 0)
		{
			if (key == null)
				return defaultValue;
			long value;
			if (long.TryParse(GetPostParam(key.ToLower()), out value))
				return value;
			return defaultValue;
		}
		/// <summary>
		/// Returns the value of a parameter sent via POST with MIME type "application/x-www-form-urlencoded".
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <param name="defaultValue">The default value to return, in case the value did not exist or was not compatible with the data type.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value.</returns>
		public double GetPostDoubleParam(string key, double defaultValue = 0)
		{
			if (key == null)
				return defaultValue;
			double value;
			if (double.TryParse(GetPostParam(key.ToLower()), out value))
				return value;
			return defaultValue;
		}
		/// <summary>
		/// Returns the value of a parameter sent via POST with MIME type "application/x-www-form-urlencoded".
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <returns>The value of the key. This function interprets a value of "1" or "true" (case insensitive) as being true.  Any other parameter value is interpreted as false.</returns>
		public bool GetPostBoolParam(string key)
		{
			string param = GetPostParam(key);
			if (param == "1" || param.ToLower() == "true")
				return true;
			return false;
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
			bool readable = tcpClient.Client.Poll(0, System.Net.Sockets.SelectMode.SelectRead);
			if (readable)
			{
				// data is available for reading OR connection is closed.
				byte[] buffer = new byte[1];
				if (tcpClient.Client.Receive(buffer, SocketFlags.Peek) == 0)
					return false; // No data available, connection must be closed
								  // Data was available, connection may not be closed.
				bool writable = tcpClient.Client.Poll(0, System.Net.Sockets.SelectMode.SelectWrite);
				bool errored = tcpClient.Client.Poll(0, System.Net.Sockets.SelectMode.SelectError);
				return writable && !errored;
			}
			else
			{
				return true; // The read poll returned false, so the connection is supposedly open with no data available to read, which is the normal state.
			}
		}
		/// <summary>
		/// Tests the "Authorization" header and returns the NetworkCredential that the client authenticated with, or null if authentication was not successful. Only "Digest" authentication is supported at this time.  SimpleHttpServer does not guard against replay attacks.
		/// </summary>
		/// <param name="realm">Name of the system that is requesting authentication.  This string is shown to the user to help them understand which credential is being requested e.g. "example.com".</param>
		/// <param name="validCredentials">A collection of valid credentials which must be tested one at a time against the client's Authorization header.</param>
		/// <returns></returns>
		public NetworkCredential ValidateDigestAuth(string realm, IEnumerable<NetworkCredential> validCredentials)
		{
			string Authorization = GetHeaderValue("Authorization");
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

					string ha2 = Hash.GetMD5Hex(http_method + ":" + uri);

					foreach (NetworkCredential cred in validCredentials)
					{
						// Calculate the expected response based on the provided credentials and challenge parameters
						string ha1 = Hash.GetMD5Hex(cred.UserName + ":" + realm + ":" + cred.Password);
						string expectedResponse = Hash.GetMD5Hex(ha1 + ":" + nonce + ":" + nc + ":" + cnonce + ":" + qop + ":" + ha2);

						if (response == expectedResponse)
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
		/// Removes the given appPath from the start of the incoming request URL, if it is found there.  Updates the <see cref="request_url"/> and <see cref="requestedPage"/> properties.
		/// </summary>
		/// <param name="appPath">AppPath string.</param>
		public void RemoveAppPath(string appPath)
		{
			if (appPath == null)
				return;
			string ap = "/" + appPath.Trim('/', ' ', '\r', '\n', '\t');
			if (ap == "/")
				return;
			if (request_url.AbsolutePath.IStartsWith(ap))
			{
				UriBuilder uriBuilder = new UriBuilder(request_url);
				uriBuilder.Path = request_url.AbsolutePath.Substring(ap.Length);
				request_url = uriBuilder.Uri;
				requestedPage = request_url.AbsolutePath.StartsWith("/") ? request_url.AbsolutePath.Substring(1) : request_url.AbsolutePath;
			}
		}
		/// <summary>
		/// An exception containing an HTTP response code and text.
		/// </summary>
		internal class HttpProcessorException : Exception
		{
			/// <summary>
			/// Constructs an exception containing an HTTP response code and text.
			/// </summary>
			/// <param name="message">Http response code and text, e.g. "413 Entity Too Large"</param>
			public HttpProcessorException(string message) : base(message) { }
		}

		/// <summary>
		/// Occurs when the client committed a protocol violation.
		/// </summary>
		internal class HttpProtocolException : Exception
		{
			public HttpProtocolException(string message) : base(message) { }
		}

		/// <summary>
		/// Occurs when the end of stream is found during request processing. Inherits from <see cref="HttpProtocolException"/>.
		/// </summary>
		internal class EndOfStreamException : HttpProtocolException
		{
			public EndOfStreamException() : base("End of stream") { }
		}
	}

	public abstract class HttpServer : IDisposable
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
			public Binding Binding { get; private set; }
			private TcpListener tcpListener;
			private volatile bool isStopping = false;
			internal HttpServer Server;
			/// <summary>
			/// Constructs a ListenerData.
			/// </summary>
			/// <param name="server">Server which owns this ListenerData.</param>
			/// <param name="binding">Local service binding which this ListenerData will use.</param>
			public ListenerData(HttpServer server, Binding binding)
			{
				this.Server = server;
				this.Binding = binding;
			}
			/// <summary>
			/// Robustly listens on the Binding.  The task does not complete until Stop() is called.
			/// </summary>
			public async void Run()
			{
				while (!isStopping)
				{
					try
					{
						tcpListener = new TcpListener(Binding.Endpoint);
						tcpListener.Start();
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
						while (!isStopping && tcpListener.Server?.IsBound == true)
						{
							try
							{
								TcpClient s = await tcpListener.AcceptTcpClientAsync().ConfigureAwait(false);
								// TcpClient's timeouts are merely limits on Read() and Write() call blocking time.  If we try to read or write a chunk of data that legitimately takes longer than the timeout to finish, it will still time out even if data was being transferred steadily.
								if (s.ReceiveTimeout < 10000 && s.ReceiveTimeout != 0) // Timeout of 0 is infinite
									s.ReceiveTimeout = 10000;
								if (s.SendTimeout < 10000 && s.SendTimeout != 0) // Timeout of 0 is infinite
									s.SendTimeout = 10000;
								int? rbuf = Server.ReceiveBufferSize;
								if (rbuf != null)
									s.ReceiveBufferSize = rbuf.Value;
								int? sbuf = Server.SendBufferSize;
								if (sbuf != null)
									s.SendBufferSize = sbuf.Value;
								IProcessor processor = Server.MakeClientProcessor(s, Server, Server.certificateSelector, Binding.AllowedConnectionTypes);
								Server.pool.Enqueue(processor.Process);
							}
							catch (ObjectDisposedException ex)
							{
								if (!isStopping)
									SimpleHttpLogger.Log(ex, "SimpleHttp.HttpServer.ListenerData error processing TcpClient.");
							}
							catch (Exception ex)
							{
								if (!HttpProcessor.IsOrdinaryDisconnectException(ex))
									SimpleHttpLogger.Log(ex, "SimpleHttp.HttpServer.ListenerData error processing TcpClient.");
							}
						}
					}
					catch (SocketException ex)
					{
						SimpleHttpLogger.Log(ex, "SimpleHttp.HttpServer.ListenerData SocketException managing socket.");
						Thread.Sleep(10000);
					}
					catch (Exception ex)
					{
						SimpleHttpLogger.Log(ex, "SimpleHttp.HttpServer.ListenerData Exception managing socket.");
						Thread.Sleep(1000);
					}
					finally
					{
						try
						{
							tcpListener?.Stop();
						}
						catch { }
					}
				}
			}
			public void Stop()
			{
				isStopping = true;
				TcpListener l = tcpListener;
				tcpListener = null;
				try
				{
					l?.Stop();
				}
				catch (Exception ex)
				{
					SimpleHttpLogger.Log(ex);
				}
			}
			public override string ToString()
			{
				return "Listener " + Binding.ToString();
			}
		}
		public int? SendBufferSize = null;
		public int? ReceiveBufferSize = null;
		/// <summary>
		/// If true, the IP address of remote hosts will be learned from the HTTP header named "X-Real-IP".  Also requires the method <see cref="IsTrustedProxyServer"/> to return true.
		/// </summary>
		public bool XRealIPHeader = false;
		/// <summary>
		/// If true, the IP address of remote hosts will be learned from the HTTP header named "X-Forwarded-For".  Also requires the method <see cref="IsTrustedProxyServer"/> to return true.
		/// </summary>
		public bool XForwardedForHeader = false;
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
		/// The thread pool to use for processing client connections.
		/// </summary>
		public SimpleThreadPool pool = new SimpleThreadPool("SimpleHttp.HttpServer");
		/// <summary>
		/// List of ListenerData instances responsible for asynchronously accepting TCP clients.
		/// </summary>
		private List<ListenerData> listeners = new List<ListenerData>();
		/// <summary>
		/// 
		/// </summary>
		/// <param name="certificateSelector">(Optional) Certificate selector to use for https connections.  If null and an https-compatible endpoint was specified, a certificate is automatically created if necessary and loaded from "SimpleHttpServer-SslCert.pfx" in the same directory that the current executable is located in.</param>
		public HttpServer(ICertificateSelector certificateSelector = null)
		{
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
		public virtual IProcessor MakeClientProcessor(TcpClient s, HttpServer srv, ICertificateSelector certSelector, AllowedConnectionTypes allowedConnectionTypes)
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
				bindings.Add(new HttpServer.Binding(AllowedConnectionTypes.httpAndHttps, new IPEndPoint(IPAddress.Any, httpPort)));
			else
			{
				if (httpPort != -1)
					bindings.Add(new HttpServer.Binding(AllowedConnectionTypes.http, new IPEndPoint(IPAddress.Any, httpPort)));
				if (httpsPort != -1)
					bindings.Add(new HttpServer.Binding(AllowedConnectionTypes.https, new IPEndPoint(IPAddress.Any, httpsPort)));
			}
			SetBindings(bindings.ToArray());
		}
		/// <summary>
		/// Sets the collection of bindings which this server should listen on.  This method will start or stop listeners as necessary to transition from the current set of bindings to the new set of bindings.
		/// </summary>
		/// <param name="newBindings">All bindings which this server should listen on.</param>
		public void SetBindings(params Binding[] newBindings)
		{
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
				SocketLog("Stopped " + listener.ToString());
			}

			// Start new listeners
			foreach (ListenerData listener in toStart)
			{
				listener.Run();
				SocketLog("Started " + listener.ToString());
			}
		}
		private void SocketLog(string str)
		{
			if (shouldLogSocketBind())
				SimpleHttpLogger.Log(str);
			else
				SimpleHttpLogger.LogVerbose(str);
		}

		/// <summary>
		/// Stops listening for connections.
		/// </summary>
		public void Stop()
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
			try { pool.Stop(); } catch (Exception ex) { SimpleHttpLogger.Log(ex); }

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
		/// Handles an Http GET request.
		/// </summary>
		/// <param name="p">The HttpProcessor handling the request.</param>
		public abstract void handleGETRequest(HttpProcessor p);
		/// <summary>
		/// Handles an Http POST request.
		/// </summary>
		/// <param name="p">The HttpProcessor handling the request.</param>
		/// <param name="inputData">The input stream.  If the request's MIME type was "application/x-www-form-urlencoded", the StreamReader will be null and you can obtain the parameter values using p.PostParams, p.GetPostParam(), p.GetPostIntParam(), etc.</param>
		public abstract void handlePOSTRequest(HttpProcessor p, StreamReader inputData);
		/// <summary>
		/// Handles requests using less common Http verbs such as "HEAD" or "PUT". See <see cref="HttpMethods"/>.
		/// </summary>
		/// <param name="method">The HTTP method string, e.g. "HEAD" or "PUT". See <see cref="HttpMethods"/>.</param>
		/// <param name="p">The HttpProcessor handling the request.</param>
		public virtual void handleOtherRequest(HttpProcessor p, string method)
		{
			if (method == HttpMethods.HEAD)
			{
				HttpHeaderCollection additionalHeaders = new HttpHeaderCollection();
				additionalHeaders.Add("Allow", "GET");
				p.writeFailure("405 Method Not Allowed", additionalHeaders: additionalHeaders);
			}
			else
				p.writeFailure("501 Not Implemented");
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
		/// This method must return true for the <see cref="XForwardedForHeader"/> and <see cref="XRealIPHeader"/> flags to be honored.  This method should only return true if the provided remote IP address is trusted to provide the related headers.
		/// </summary>
		/// <param name="p">HttpProcessor</param>
		/// <param name="remoteIpAddress">True remote IP address of the client.</param>
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
	}
	#region Helper Classes
	/// <summary>
	/// <para>HTTP request methods</para>
	/// <para>This static class defines all the methods from https://developer.mozilla.org/en-US/docs/Web/HTTP/Methods on 2023-06-27, but SimpleHttpServer only fully supports GET and POST.</para>
	/// <para>HTTP defines a set of request methods to indicate the desired action to be performed for a given resource. Although they can also be nouns, these request methods are sometimes referred to as HTTP verbs. Each of them implements a different semantic, but some common features are shared by a group of them: e.g. a request method can be safe, idempotent, or cacheable.</para>
	/// </summary>
	public static class HttpMethods
	{
		/// <summary>
		/// The GET method requests a representation of the specified resource. Requests using GET should only retrieve data.
		/// </summary>
		public const string GET = "GET";
		/// <summary>
		/// The HEAD method asks for a response identical to a GET request, but without the response body.
		/// </summary>
		public const string HEAD = "HEAD";
		/// <summary>
		/// The POST method submits an entity to the specified resource, often causing a change in state or side effects on the server.
		/// </summary>
		public const string POST = "POST";
		/// <summary>
		/// The PUT method replaces all current representations of the target resource with the request payload.
		/// </summary>
		public const string PUT = "PUT";
		/// <summary>
		/// The DELETE method deletes the specified resource.
		/// </summary>
		public const string DELETE = "DELETE";
		/// <summary>
		/// The CONNECT method establishes a tunnel to the server identified by the target resource.
		/// </summary>
		public const string CONNECT = "CONNECT";
		/// <summary>
		/// The OPTIONS method describes the communication options for the target resource.
		/// </summary>
		public const string OPTIONS = "OPTIONS";
		/// <summary>
		/// The TRACE method performs a message loop-back test along the path to the target resource.
		/// </summary>
		public const string TRACE = "TRACE";
		/// <summary>
		/// The PATCH method applies partial modifications to a resource.
		/// </summary>
		public const string PATCH = "PATCH";
		/// <summary>
		/// Static HashSet containing all valid HTTP request method strings.
		/// </summary>
		private static HashSet<string> validMethods = GetValidMethods();
		/// <summary>
		/// Returns a HashSet containing all valid HTTP request method strings.
		/// </summary>
		/// <returns></returns>
		private static HashSet<string> GetValidMethods()
		{
			return new HashSet<string>(new string[] { GET, HEAD, POST, PUT, DELETE, CONNECT, OPTIONS, TRACE, PATCH });
		}
		/// <summary>
		/// Returns true if the given HTTP method (a.k.a. verb) is recognized as one that is valid.
		/// </summary>
		/// <param name="method"></param>
		/// <returns></returns>
		public static bool IsValid(string method)
		{
			return validMethods.Contains(method);
		}
	}
	class SimpleHttpServerThreadArgs
	{
		public int port;
		public AllowedConnectionTypes allowedConnectionTypes;
		public Action<int> SetActualPort;
		public Action<TcpListener> SetListenerField;

		public SimpleHttpServerThreadArgs(int port, AllowedConnectionTypes allowedConnectionTypes, Action<int> setActualPort, Action<TcpListener> setListenerField)
		{
			this.port = port;
			this.allowedConnectionTypes = allowedConnectionTypes;
			SetActualPort = setActualPort;
			SetListenerField = setListenerField;
		}
	}
	[Flags]
	public enum AllowedConnectionTypes
	{
		http = 0b1,
		https = 0b10,
		httpAndHttps = http | https
	}
	public enum CompressionType
	{
		None,
		GZip
	}
	public class Cookie
	{
		public string name;
		public string value;
		public TimeSpan expire;

		public Cookie(string name, string value, TimeSpan expire)
		{
			this.name = name;
			this.value = value;
			this.expire = expire;
		}
	}
	public class Cookies : IEnumerable<Cookie>
	{
		SortedList<string, Cookie> cookieCollection = new SortedList<string, Cookie>();
		/// <summary>
		/// Adds or updates a cookie with the specified name and value.  The cookie is set to expire immediately at the end of the browsing session.
		/// </summary>
		/// <param name="name">The cookie's name.</param>
		/// <param name="value">The cookie's value.</param>
		public void Add(string name, string value)
		{
			Add(name, value, TimeSpan.Zero);
		}
		/// <summary>
		/// Adds or updates a cookie with the specified name, value, and lifespan.
		/// </summary>
		/// <param name="name">The cookie's name.</param>
		/// <param name="value">The cookie's value.</param>
		/// <param name="expireTime">The amount of time before the cookie should expire.</param>
		public void Add(string name, string value, TimeSpan expireTime)
		{
			if (name == null)
				return;
			cookieCollection[name.ToLower()] = new Cookie(name, value, expireTime);
		}
		/// <summary>
		/// Gets the cookie with the specified name.  If the cookie is not found, null is returned;
		/// </summary>
		/// <param name="name">The name of the cookie.</param>
		/// <returns></returns>
		public Cookie Get(string name)
		{
			Cookie cookie;
			if (!cookieCollection.TryGetValue(name.ToLower(), out cookie))
				cookie = null;
			return cookie;
		}
		/// <summary>
		/// Gets the value of the cookie with the specified name.  If the cookie is not found, an empty string is returned;
		/// </summary>
		/// <param name="name">The name of the cookie.</param>
		/// <returns></returns>
		public string GetValue(string name)
		{
			Cookie cookie = Get(name);
			if (cookie == null)
				return "";
			return cookie.value;
		}
		/// <summary>
		/// Deletes the cookie with the specified name, returning true if the cookie was removed, false if it did not exist or was not removed.
		/// </summary>
		/// <param name="name">The name of the cookie.</param>
		public bool Remove(string name)
		{
			if (name != null)
				return cookieCollection.Remove(name.ToLower());
			return false;
		}
		/// <summary>
		/// Returns a string of "Set-Cookie: ..." headers (one for each cookie in the collection) separated by "\r\n".  There is no leading or trailing "\r\n".
		/// </summary>
		/// <returns>A string of "Set-Cookie: ..." headers (one for each cookie in the collection) separated by "\r\n".  There is no leading or trailing "\r\n".</returns>
		public override string ToString()
		{
			List<string> cookiesStr = new List<string>();
			foreach (Cookie cookie in cookieCollection.Values)
				cookiesStr.Add("Set-Cookie: " + cookie.name + "=" + cookie.value + (cookie.expire == TimeSpan.Zero ? "" : "; Max-Age=" + (long)cookie.expire.TotalSeconds) + "; Path=/");
			return string.Join("\r\n", cookiesStr);
		}
		/// <summary>
		/// Returns a Cookies instance populated by parsing the specified string.  The string should be the value of the "Cookie" header that was received from the remote client.  If the string is null or empty, an empty cookies collection is returned.
		/// </summary>
		/// <param name="str">The value of the "Cookie" header sent by the remote client.</param>
		/// <returns></returns>
		public static Cookies FromString(string str)
		{
			Cookies cookies = new Cookies();
			if (str == null)
				return cookies;
			str = Uri.UnescapeDataString(str);
			string[] parts = str.Split(';');
			for (int i = 0; i < parts.Length; i++)
			{
				int idxEquals = parts[i].IndexOf('=');
				if (idxEquals < 1)
					continue;
				string name = parts[i].Substring(0, idxEquals).Trim();
				string value = parts[i].Substring(idxEquals + 1).Trim();
				cookies.Add(name, value);
			}
			return cookies;
		}

		#region IEnumerable<Cookie> Members

		IEnumerator<Cookie> IEnumerable<Cookie>.GetEnumerator()
		{
			return cookieCollection.Values.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return cookieCollection.Values.GetEnumerator();
		}

		#endregion
	}
	public static class Extensions
	{
		/// <summary>
		/// Returns the date and time formatted for insertion as the expiration date in a "Set-Cookie" header.
		/// </summary>
		/// <param name="time"></param>
		/// <returns></returns>
		public static string ToCookieTime(this DateTime time)
		{
			return time.ToString("dd MMM yyyy hh:mm:ss GMT");
		}
		/// <summary>
		/// For linux compatibility. The HTTP protocol uses \r\n, but linux normally uses just \n.
		/// </summary>
		/// <param name="sw"></param>
		/// <param name="line"></param>
		public static void WriteLineRN(this TextWriter sw, string line)
		{
			sw.Write(line);
			sw.Write("\r\n");
		}
	}
	/// <summary>
	/// A class which handles error logging by the http server.  It allows you to (optionally) register an ILogger instance to use for logging.
	/// </summary>
	public static class SimpleHttpLogger
	{
		private static ILogger logger = null;
		private static bool logVerbose = false;
		/// <summary>
		/// (OPTIONAL) Keeps a static reference to the specified ILogger and uses it for http server error logging.  Only one logger can be registered at a time; attempting to register a second logger simply replaces the first one.
		/// </summary>
		/// <param name="loggerToRegister">The logger that should be used when an error message needs logged.  If null, logging will be disabled.</param>
		/// <param name="logVerboseMessages">If true, additional error reporting will be enabled.  These errors include things that can occur frequently during normal operation, so it may be spammy.</param>
		public static void RegisterLogger(ILogger loggerToRegister, bool logVerboseMessages = false)
		{
			logger = loggerToRegister;
			logVerbose = logVerboseMessages;
		}
		/// <summary>
		/// Unregisters the currently registered logger (if any) by calling RegisterLogger(null);
		/// </summary>
		public static void UnregisterLogger()
		{
			RegisterLogger(null);
		}
		internal static void Log(Exception ex, string additionalInformation = "")
		{
			try
			{
				if (logger != null)
					logger.Log(ex, additionalInformation);
			}
			catch (ThreadAbortException) { throw; }
			catch { }
		}
		internal static void Log(string str)
		{
			try
			{
				if (logger != null)
					logger.Log(str);
			}
			catch (ThreadAbortException) { throw; }
			catch { }
		}

		internal static void LogVerbose(Exception ex, string additionalInformation = "")
		{
			if (logVerbose)
				Log(ex, additionalInformation);
		}

		internal static void LogVerbose(string str)
		{
			if (logVerbose)
				Log(str);
		}
		internal static void LogRequest(DateTime time, string remoteHost, string requestMethod, string requestedUrl)
		{
			LogVerbose(remoteHost + "\t" + requestMethod + "\t" + requestedUrl);
			if (logger != null)
				try
				{
					logger.LogRequest(time, time.ToString("yyyy-MM-dd hh:mm:ss tt") + ":\t" + remoteHost + "\t" + requestMethod + "\t" + requestedUrl);
				}
				catch (ThreadAbortException) { throw; }
				catch { }
		}
	}
	/// <summary>
	/// An interface which handles logging of exceptions and strings.
	/// </summary>
	public interface ILogger
	{
		/// <summary>
		/// Log an exception, possibly with additional information provided to assist with debugging.
		/// </summary>
		/// <param name="ex">An exception that was caught.</param>
		/// <param name="additionalInformation">Additional information about the exception.</param>
		void Log(Exception ex, string additionalInformation = "");
		/// <summary>
		/// Log a string.
		/// </summary>
		/// <param name="str">A string to log.</param>
		void Log(string str);
		/// <summary>
		/// Log a request that was made to the server.
		/// </summary>
		/// <param name="time">The time of the request, from which the log file name will be chosen.</param>
		/// <param name="line">The string to log, including a timestamp and all information desired. This string should not contain line breaks.</param>
		void LogRequest(DateTime time, string line);
	}
	#endregion
	///// <summary>
	///// Legacy HttpServer.
	///// </summary>
	//public abstract class LEGACYHttpServer : IDisposable
	//{
	//	/// <summary>
	//	/// If > -1, the server was told to listen for http connections on this port.  Port 0 causes the socket library to choose its own port.
	//	/// </summary>
	//	protected readonly int port;
	//	/// <summary>
	//	/// If > -1, the server was told to listen for https connections on this port.  Port 0 causes the socket library to choose its own port.
	//	/// </summary>
	//	protected readonly int secure_port;
	//	protected int actual_port_http = -1;
	//	protected int actual_port_https = -1;
	//	/// <summary>
	//	/// The actual port the http server is listening on.  Will be -1 if not listening.
	//	/// </summary>
	//	public int Port_http
	//	{
	//		get
	//		{
	//			return actual_port_http;
	//		}
	//	}
	//	/// <summary>
	//	/// The actual port the https server is listening on.  Will be -1 if not listening.
	//	/// </summary>
	//	public int Port_https
	//	{
	//		get
	//		{
	//			return actual_port_https;
	//		}
	//	}
	//	public int? SendBufferSize = null;
	//	public int? ReceiveBufferSize = null;
	//	/// <summary>
	//	/// If true, the IP address of remote hosts will be learned from the HTTP header named "X-Real-IP".  Also requires the method <see cref="IsTrustedProxyServer"/> to return true.
	//	/// </summary>
	//	public bool XRealIPHeader = false;
	//	/// <summary>
	//	/// If true, the IP address of remote hosts will be learned from the HTTP header named "X-Forwarded-For".  Also requires the method <see cref="IsTrustedProxyServer"/> to return true.
	//	/// </summary>
	//	public bool XForwardedForHeader = false;
	//	protected volatile bool stopRequested = false;
	//	protected ICertificateSelector certificateSelector;
	//	private Thread thrHttp;
	//	private Thread thrHttps;
	//	private TcpListener unsecureListener = null;
	//	private TcpListener secureListener = null;
	//	/// <summary>
	//	/// Raised when a listening socket is bound to a port.  The Event Handler passes along a string which can be printed to the console, announcing this event.
	//	/// </summary>
	//	public event EventHandler<string> SocketBound = delegate { };
	//	/// <summary>
	//	/// Raised when an SSL connection is made using a certificate that will expire within the next 14 days.  This event will not be raised more than once in a 60 minute period (assuming the same HttpServer instance is used).
	//	/// The TimeSpan argument indicates the time to expiration, which may be less than or equal to TimeSpan.Zero if the certificate is expired.
	//	/// </summary>
	//	[Obsolete("This is no longer used.")]
	//	public event EventHandler<TimeSpan> CertificateExpirationWarning = delegate { };
	//	private DateTime timeOfNextCertificateExpirationWarning = DateTime.MinValue;

	//	private NetworkAddressInfo addressInfo = new NetworkAddressInfo();
	//	/// <summary>
	//	/// Gets information about the current network interfaces.
	//	/// You should work with a local reference to the returned object, because this method is not guaranteed to always return the same instance.
	//	/// </summary>
	//	/// <returns></returns>
	//	internal NetworkAddressInfo GetAddressInfo()
	//	{
	//		return addressInfo;
	//	}
	//	public readonly IPAddress bindAddr = IPAddress.Any;

	//	public SimpleThreadPool pool;
	//	/// <summary>
	//	/// 
	//	/// </summary>
	//	/// <param name="port">The port number on which to accept regular http connections. If -1, the server will not listen for http connections.</param>
	//	/// <param name="httpsPort">(Optional) The port number on which to accept https connections. If -1, the server will not listen for https connections.</param>
	//	/// <param name="cert">(Optional) Certificate to use for https connections.  If null and an httpsPort was specified, a certificate is automatically created if necessary and loaded from "SimpleHttpServer-SslCert.pfx" in the same directory that the current executable is located in.</param>
	//	/// <param name="bindAddr">If not null, the server will bind to this address.  Default: IPAddress.Any.</param>
	//	public HttpServer(int port, int httpsPort = -1, X509Certificate2 cert = null, IPAddress bindAddr = null) : this(port, httpsPort, SimpleCertificateSelector.FromCertificate(cert), bindAddr) { }
	//	/// <summary>
	//	/// 
	//	/// </summary>
	//	/// <param name="port">The port number on which to accept regular http connections. If -1, the server will not listen for http connections.</param>
	//	/// <param name="httpsPort">(Optional) The port number on which to accept https connections. If -1, the server will not listen for https connections.</param>
	//	/// <param name="certificateSelector">(Optional) Certificate selector to use for https connections.  If null and an httpsPort was specified, a certificate is automatically created if necessary and loaded from "SimpleHttpServer-SslCert.pfx" in the same directory that the current executable is located in.</param>
	//	/// <param name="bindAddr">If not null, the server will bind to this address.  Default: IPAddress.Any.</param>
	//	public HttpServer(int port, int httpsPort, ICertificateSelector certificateSelector, IPAddress bindAddr)
	//	{
	//		pool = new SimpleThreadPool("SimpleHttpServer");

	//		this.port = port;
	//		this.secure_port = httpsPort;
	//		this.certificateSelector = certificateSelector;
	//		if (bindAddr != null)
	//			this.bindAddr = bindAddr;

	//		if (this.port > 65535 || this.port < -1) this.port = -1;
	//		if (this.secure_port > 65535 || this.secure_port < -1) this.secure_port = -1;

	//		if (this.secure_port > -1)
	//		{
	//			if (this.certificateSelector == null)
	//				this.certificateSelector = SimpleCertificateSelector.FromCertificate(HttpServer.GetSelfSignedCertificate());
	//		}

	//		if (this.secure_port > -1 && this.secure_port == this.port)
	//		{
	//			thrHttps = new Thread(listen);
	//			thrHttps.IsBackground = true;
	//			thrHttps.Name = "Http and Https Combined Server Thread";
	//		}
	//		else
	//		{
	//			if (this.port > -1)
	//			{
	//				thrHttp = new Thread(listen);
	//				thrHttp.IsBackground = true;
	//				thrHttp.Name = "HttpServer Thread";
	//			}

	//			if (this.secure_port > -1)
	//			{
	//				thrHttps = new Thread(listen);
	//				thrHttps.IsBackground = true;
	//				thrHttps.Name = "HttpsServer Thread";
	//			}
	//		}
	//		NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
	//		NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
	//		UpdateNetworkAddresses();
	//	}

	//	#region IDisposable Support
	//	private bool disposedValue = false;

	//	protected virtual void Dispose(bool disposing)
	//	{
	//		if (!disposedValue)
	//		{
	//			if (disposing)
	//			{
	//				// Dispose managed objects
	//				Stop();
	//			}

	//			// Dispose unmanaged objects
	//			// Set large fields = null

	//			disposedValue = true;
	//		}
	//	}

	//	public void Dispose()
	//	{
	//		// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
	//		Dispose(true);
	//	}
	//	#endregion

	//	/// <summary>
	//	/// A function which produces an IProcessor instance, allowing this server to be used for protocols besides HTTP.  By default, this returns a standard HttpProcessor.
	//	/// </summary>
	//	/// <param name="s">The TcpClient which holds the connection from the client.</param>
	//	/// <param name="srv">The server instance which accepted the TCP connection from the client.</param>
	//	/// <param name="certSelector">An instance of a type deriving from ICertificateSelector, such as <see cref="SimpleCertificateSelector"/> or <see cref="ServerNameCertificateSelector"/>.</param>
	//	/// <param name="allowedConnectionTypes">Indicates whether the connection should support HTTP, HTTPS, or both.</param>
	//	/// <returns></returns>
	//	public virtual IProcessor MakeClientProcessor(TcpClient s, HttpServer srv, ICertificateSelector certSelector, AllowedConnectionTypes allowedConnectionTypes)
	//	{
	//		return new HttpProcessor(s, srv, certSelector, allowedConnectionTypes);
	//	}
	//	private void NetworkChange_NetworkAddressChanged(object sender, EventArgs e)
	//	{
	//		UpdateNetworkAddresses();
	//	}

	//	private void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
	//	{
	//		UpdateNetworkAddresses();
	//	}

	//	private void UpdateNetworkAddresses()
	//	{
	//		addressInfo = new NetworkAddressInfo(NetworkInterface.GetAllNetworkInterfaces());
	//	}
	//	/// <summary>
	//	/// Sets a new SSL certificate to be used for all future connections;
	//	/// </summary>
	//	/// <param name="newCertificate"></param>
	//	public void SetCertificate(X509Certificate2 newCertificate)
	//	{
	//		certificateSelector = SimpleCertificateSelector.FromCertificate(newCertificate);
	//	}
	//	/// <summary>
	//	/// Returns the date in local time after which the certificate is no longer valid.  If the certificate is null, returns DateTime.MaxValue.
	//	/// </summary>
	//	/// <returns></returns>
	//	[Obsolete("This doesn't work properly when using Server Name Indication to select a certificate.")]
	//	public DateTime GetCertificateExpiration()
	//	{
	//		if (certificateSelector != null)
	//			return (certificateSelector.GetCertificate(null) as X509Certificate2).NotAfter;
	//		return DateTime.MaxValue;
	//	}
	//	/// <summary>
	//	/// Returns the friendly name from the certificate.  If the certificate is null, returns DateTime.MaxValue.
	//	/// </summary>
	//	/// <returns></returns>
	//	[Obsolete("This doesn't work properly when using Server Name Indication to select a certificate.")]
	//	public string GetCertificateFriendlyName()
	//	{
	//		if (certificateSelector != null)
	//			return (certificateSelector.GetCertificate(null) as X509Certificate2).FriendlyName;
	//		return null;
	//	}

	//	private static object certCreateLock = new object();
	//	public static X509Certificate2 GetSelfSignedCertificate()
	//	{
	//		lock (certCreateLock)
	//		{
	//			X509Certificate2 ssl_certificate;
	//			FileInfo fiExe;
	//			try
	//			{
	//				fiExe = new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location);
	//			}
	//			catch
	//			{
	//				try
	//				{
	//					fiExe = new FileInfo(System.Windows.Forms.Application.ExecutablePath);
	//				}
	//				catch
	//				{
	//					fiExe = new FileInfo(Globals.ApplicationDirectoryBase + Globals.ExecutableNameWithExtension);
	//				}
	//			}
	//			string autoCertPassword = "N0t_V3ry-S3cure#lol";
	//			FileInfo fiCert = new FileInfo(fiExe.Directory.FullName + "/SimpleHttpServer-SslCert.pfx");
	//			if (fiCert.Exists)
	//			{
	//				try
	//				{
	//					ssl_certificate = new X509Certificate2(fiCert.FullName, autoCertPassword);
	//				}
	//				catch (Exception ex1)
	//				{
	//					try
	//					{
	//						ssl_certificate = new X509Certificate2(fiCert.FullName);
	//					}
	//					catch
	//					{
	//						throw ex1;
	//					}
	//				}
	//			}
	//			else
	//			{
	//				using (BPUtil.SimpleHttp.Crypto.CryptContext ctx = new BPUtil.SimpleHttp.Crypto.CryptContext())
	//				{
	//					ctx.Open();

	//					ssl_certificate = ctx.CreateSelfSignedCertificate(
	//						new BPUtil.SimpleHttp.Crypto.SelfSignedCertProperties
	//						{
	//							IsPrivateKeyExportable = true,
	//							KeyBitLength = 4096,
	//							Name = new X500DistinguishedName("cn=localhost"),
	//							ValidFrom = DateTime.Today.AddDays(-1),
	//							ValidTo = DateTime.Today.AddYears(100),
	//						});

	//					byte[] certData = ssl_certificate.Export(X509ContentType.Pfx, autoCertPassword);
	//					File.WriteAllBytes(fiCert.FullName, certData);
	//				}
	//				// Native cert generator. .NET 4.7.2 required.
	//				//using (System.Security.Cryptography.RSA key = System.Security.Cryptography.RSA.Create(2048))
	//				//{
	//				//	CertificateRequest request = new CertificateRequest("cn=localhost", key, System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);

	//				//	SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
	//				//	sanBuilder.AddDnsName("localhost");
	//				//	request.CertificateExtensions.Add(sanBuilder.Build());

	//				//	ssl_certificate = request.CreateSelfSigned(DateTime.Today.AddDays(-1), DateTime.Today.AddYears(100));

	//				//	byte[] certData = ssl_certificate.Export(X509ContentType.Pfx);
	//				//	File.WriteAllBytes(fiCert.FullName, certData);
	//				//}
	//			}
	//			return ssl_certificate;
	//		}
	//	}
	//	/// <summary>
	//	/// Listens for connections, somewhat robustly.  Does not return until the server is stopped or until more than 100 listener restarts occur in a single day.
	//	/// </summary>
	//	private void listen(object param)
	//	{
	//		try
	//		{
	//			SimpleHttpServerThreadArgs args = (SimpleHttpServerThreadArgs)param;

	//			int errorCount = 0;
	//			DateTime lastError = DateTime.Now;

	//			TcpListener listener = null;

	//			while (!stopRequested)
	//			{
	//				bool threwExceptionOuter = false;
	//				try
	//				{
	//					listener = new TcpListener(bindAddr, args.port);
	//					args.SetListenerField(listener);
	//					listener.Start();

	//					args.SetActualPort(((IPEndPoint)listener.LocalEndpoint).Port);

	//					try
	//					{
	//						if (args.allowedConnectionTypes == AllowedConnectionTypes.httpAndHttps)
	//							SocketBound(this, "Web Server listening on port " + actual_port_https + " (http and https)");
	//						else
	//							SocketBound(this, "Web Server listening on port " + (args.allowedConnectionTypes == AllowedConnectionTypes.https ? (actual_port_https + " (https)") : (actual_port_http + " (http)")));
	//					}
	//					catch (ThreadAbortException) { throw; }
	//					catch (Exception ex)
	//					{
	//						SimpleHttpLogger.Log(ex);
	//					}

	//					DateTime innerLastError = DateTime.Now;
	//					int innerErrorCount = 0;
	//					while (!stopRequested)
	//					{
	//						try
	//						{
	//							TcpClient s = listener.AcceptTcpClient();
	//							// TcpClient's timeouts are merely limits on Read() and Write() call blocking time.  If we try to read or write a chunk of data that legitimately takes longer than the timeout to finish, it will still time out even if data was being transferred steadily.
	//							if (s.ReceiveTimeout < 10000 && s.ReceiveTimeout != 0) // Timeout of 0 is infinite
	//								s.ReceiveTimeout = 10000;
	//							if (s.SendTimeout < 10000 && s.SendTimeout != 0) // Timeout of 0 is infinite
	//								s.SendTimeout = 10000;
	//							if (ReceiveBufferSize != null)
	//								s.ReceiveBufferSize = ReceiveBufferSize.Value;
	//							if (SendBufferSize != null)
	//								s.SendBufferSize = SendBufferSize.Value;
	//							//DateTime now = DateTime.Now;
	//							//if (cert != null && now.AddDays(14) > cert.NotAfter && timeOfNextCertificateExpirationWarning < now)
	//							//{
	//							//	timeOfNextCertificateExpirationWarning = now.AddHours(1);
	//							//	try
	//							//	{
	//							//		CertificateExpirationWarning(this, now - cert.NotAfter);
	//							//	}
	//							//	catch (ThreadAbortException) { throw; }
	//							//	catch (Exception ex)
	//							//	{
	//							//		SimpleHttpLogger.Log(ex);
	//							//	}
	//							//}
	//							IProcessor processor = MakeClientProcessor(s, this, certificateSelector, args.allowedConnectionTypes);
	//							pool.Enqueue(processor.Process);
	//							//	try
	//							//	{
	//							//		StreamWriter outputStream = new StreamWriter(s.GetStream());
	//							//		outputStream.WriteLineRN("HTTP/1.1 503 Service Unavailable");
	//							//		outputStream.WriteLineRN("Connection: close");
	//							//		outputStream.WriteLineRN("");
	//							//		outputStream.WriteLineRN("Server too busy");
	//							//	}
	//							//	catch (ThreadAbortException) { throw; }
	//						}
	//						catch (ThreadAbortException) { throw; }
	//						catch (Exception ex)
	//						{
	//							if (ex.Message == "A blocking operation was interrupted by a call to WSACancelBlockingCall")
	//							{
	//							}
	//							else
	//							{
	//								if (DateTime.Now.Hour != innerLastError.Hour || DateTime.Now.DayOfYear != innerLastError.DayOfYear)
	//								{
	//									innerLastError = DateTime.Now;
	//									innerErrorCount = 0;
	//								}
	//								if (++innerErrorCount > 10)
	//									throw ex;
	//								SimpleHttpLogger.Log(ex, "Inner Error count this hour: " + innerErrorCount);
	//								Thread.Sleep(1);
	//							}
	//						}
	//					}
	//				}
	//				catch (ThreadAbortException) { stopRequested = true; }
	//				catch (Exception ex)
	//				{
	//					if (ex.Message == "A blocking operation was interrupted by a call to WSACancelBlockingCall")
	//					{
	//					}
	//					else
	//					{
	//						if (DateTime.Now.DayOfYear != lastError.DayOfYear || DateTime.Now.Year != lastError.Year)
	//						{
	//							lastError = DateTime.Now;
	//							errorCount = 0;
	//						}
	//						if (++errorCount > 200)
	//							throw ex;
	//						SimpleHttpLogger.Log(ex, "Restarting listener. Outer Error count today: " + errorCount);
	//						threwExceptionOuter = true;
	//					}
	//				}
	//				finally
	//				{
	//					try
	//					{
	//						if (listener != null)
	//						{
	//							listener.Stop();
	//							if (threwExceptionOuter)
	//								Thread.Sleep(1000);
	//						}
	//					}
	//					catch (ThreadAbortException) { stopRequested = true; }
	//					catch (Exception) { }
	//				}
	//			}
	//		}
	//		catch (ThreadAbortException) { stopRequested = true; }
	//		catch (Exception ex)
	//		{
	//			SimpleHttpLogger.Log(ex, "Exception thrown in outer loop.  Exiting listener.");
	//		}
	//	}

	//	/// <summary>
	//	/// Starts listening for connections.
	//	/// </summary>
	//	public void Start()
	//	{
	//		if (thrHttp != null)
	//			thrHttp.Start(new SimpleHttpServerThreadArgs(this.port,
	//				AllowedConnectionTypes.http,
	//				port => this.actual_port_http = port,
	//				listener => this.unsecureListener = listener));
	//		if (thrHttps != null)
	//			thrHttps.Start(new SimpleHttpServerThreadArgs(this.secure_port,
	//				this.secure_port == this.port ? AllowedConnectionTypes.httpAndHttps : AllowedConnectionTypes.https,
	//				port => this.actual_port_https = port,
	//				listener => this.secureListener = listener));
	//	}

	//	/// <summary>
	//	/// Stops listening for connections.
	//	/// </summary>
	//	public void Stop()
	//	{
	//		if (stopRequested)
	//			return;
	//		stopRequested = true;

	//		try
	//		{
	//			NetworkChange.NetworkAvailabilityChanged -= NetworkChange_NetworkAvailabilityChanged;
	//			NetworkChange.NetworkAddressChanged -= NetworkChange_NetworkAddressChanged;
	//		}
	//		catch (Exception ex) { SimpleHttpLogger.Log(ex); }
	//		try { pool.Stop(); } catch (Exception ex) { SimpleHttpLogger.Log(ex); }
	//		if (unsecureListener != null)
	//			try { unsecureListener.Stop(); } catch (Exception ex) { SimpleHttpLogger.Log(ex); }
	//		if (secureListener != null && unsecureListener != secureListener)
	//			try { secureListener.Stop(); } catch (Exception ex) { SimpleHttpLogger.Log(ex); }
	//		if (thrHttp != null)
	//			try { thrHttp.Abort(); } catch (Exception ex) { SimpleHttpLogger.Log(ex); }
	//		if (thrHttps != null)
	//			try { thrHttps.Abort(); } catch (Exception ex) { SimpleHttpLogger.Log(ex); }
	//		try { stopServer(); } catch (Exception ex) { SimpleHttpLogger.Log(ex); }
	//		try { GlobalThrottledStream.ThrottlingManager.Shutdown(); } catch (Exception ex) { SimpleHttpLogger.Log(ex); }
	//	}

	//	/// <summary>
	//	/// Blocks the calling thread until the http listening threads finish or the timeout expires.  Call this after calling Stop() if you need to wait for the listener to clean up, such as if you intend to start another instance of the server using the same port(s).
	//	/// </summary>
	//	/// <param name="timeout_milliseconds">Maximum number of milliseconds to wait for the HttpServer Threads to stop.</param>
	//	public bool Join(int timeout_milliseconds = 2000)
	//	{
	//		bool success = true;
	//		System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
	//		int timeToWait = timeout_milliseconds;
	//		stopwatch.Start();
	//		if (timeToWait > 0)
	//		{
	//			try
	//			{
	//				if (thrHttp != null && thrHttp.IsAlive)
	//					success = thrHttp.Join(timeToWait) && success;
	//			}
	//			catch (ThreadAbortException) { throw; }
	//			catch (Exception ex)
	//			{
	//				SimpleHttpLogger.Log(ex);
	//			}
	//		}
	//		stopwatch.Stop();
	//		timeToWait = timeout_milliseconds - (int)stopwatch.ElapsedMilliseconds;
	//		if (timeToWait > 0)
	//		{
	//			try
	//			{
	//				if (thrHttps != null && thrHttps.IsAlive)
	//					success = thrHttps.Join(timeToWait) && success;
	//			}
	//			catch (ThreadAbortException) { throw; }
	//			catch (Exception ex)
	//			{
	//				SimpleHttpLogger.Log(ex);
	//			}
	//		}
	//		return success;
	//	}

	//	/// <summary>
	//	/// Handles an Http GET request.
	//	/// </summary>
	//	/// <param name="p">The HttpProcessor handling the request.</param>
	//	public abstract void handleGETRequest(HttpProcessor p);
	//	/// <summary>
	//	/// Handles an Http POST request.
	//	/// </summary>
	//	/// <param name="p">The HttpProcessor handling the request.</param>
	//	/// <param name="inputData">The input stream.  If the request's MIME type was "application/x-www-form-urlencoded", the StreamReader will be null and you can obtain the parameter values using p.PostParams, p.GetPostParam(), p.GetPostIntParam(), etc.</param>
	//	public abstract void handlePOSTRequest(HttpProcessor p, StreamReader inputData);
	//	/// <summary>
	//	/// This is called when the server is stopping.  Perform any cleanup work here.
	//	/// </summary>
	//	protected abstract void stopServer();

	//	/// <summary>
	//	/// If this method returns true, requests should be logged to a file.
	//	/// </summary>
	//	/// <returns></returns>
	//	public virtual bool shouldLogRequestsToFile()
	//	{
	//		return false;
	//	}

	//	/// <summary>
	//	/// This method must return true for the <see cref="XForwardedForHeader"/> and <see cref="XRealIPHeader"/> flags to be honored.  This method should only return true if the provided remote IP address is trusted to provide the related headers.
	//	/// </summary>
	//	/// <param name="remoteIpAddress"></param>
	//	/// <returns></returns>
	//	public virtual bool IsTrustedProxyServer(IPAddress remoteIpAddress)
	//	{
	//		return false;
	//	}
	//}
}