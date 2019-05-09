using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Web;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.IO.Compression;

// This file has been modified continuously since Nov 10, 2012 by Brian Pearce.
// Based on http://www.codeproject.com/Articles/137979/Simple-HTTP-Server-in-C

// offered to the public domain for any use with no restriction
// and also with no warranty of any kind, please enjoy. - David Jeske. 

// simple HTTP explanation
// http://www.jmarshall.com/easy/http/


namespace BPUtil.SimpleHttp
{
	public class HttpProcessor : IProcessor
	{
		public static UTF8Encoding Utf8NoBOM = new UTF8Encoding(false);
		private const int BUF_SIZE = 4096;
		private static int MAX_POST_SIZE = 10 * 1024 * 1024; // 10MB

		#region Fields and Properties
		/// <summary>
		/// The underlying tcpClient which handles the network connection.
		/// </summary>
		public TcpClient tcpClient;

		/// <summary>
		/// The HttpServer instance that accepted this request.
		/// </summary>
		public HttpServer srv;

		/// <summary>
		/// This stream is for reading and writing binary data.
		/// 
		/// Be careful to flush [tcpStream] or [outputStream] before switching between them!!
		/// 
		/// This stream is typically either a NetworkStream or a GzipStream.
		/// </summary>
		public Stream tcpStream;

		/// <summary>
		/// This stream is for writing text data.
		/// Be careful to flush [tcpStream] or [outputStream] before switching between them!!
		/// </summary>
		public StreamWriter outputStream;

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
		public Cookies requestCookies;

		/// <summary>
		/// The cookies to send to the remote client.
		/// </summary>
		public Cookies responseCookies = new Cookies();

		/// <summary>
		/// The Http method used.  i.e. "POST" or "GET"
		/// </summary>
		public string http_method;
		/// <summary>
		/// The base Uri for this server, containing its host name and port.
		/// </summary>
		public Uri base_uri_this_server;
		/// <summary>
		/// The requested url.
		/// </summary>
		public Uri request_url;
		/// <summary>
		/// The protocol version string sent by the client.  e.g. "HTTP/1.1"
		/// </summary>
		public string http_protocol_versionstring;
		/// <summary>
		/// The path to and name of the requested page, not including the first '/'
		/// 
		/// For example, if the URL was "/articles/science/moon.html?date=2011-10-21", requestedPage would be "articles/science/moon.html"
		/// </summary>
		public string requestedPage;
		/// <summary>
		/// A Dictionary mapping http header names to values. Names are all converted to lower case before being added to this Dictionary.
		/// </summary>
		public Dictionary<string, string> httpHeaders = new Dictionary<string, string>();

		/// <summary>
		/// A Dictionary mapping http header names to values. Names are left in their raw form, and may include capital letters.
		/// </summary>
		public Dictionary<string, string> httpHeadersRaw = new Dictionary<string, string>();

		/// <summary>
		/// A SortedList mapping lower-case keys to values of parameters.  This list is populated if and only if the request was a POST request with mimetype "application/x-www-form-urlencoded".
		/// </summary>
		public SortedList<string, string> PostParams = new SortedList<string, string>();
		/// <summary>
		/// A SortedList mapping keys to values of parameters.  No character case conversion is applied in this list.  This list is populated if and only if the request was a POST request with mimetype "application/x-www-form-urlencoded".
		/// </summary>
		public SortedList<string, string> RawPostParams = new SortedList<string, string>();
		/// <summary>
		/// A SortedList mapping lower-case keys to values of parameters.  This list is populated parameters that were appended to the url (the query string).  e.g. if the url is "mypage.html?arg1=value1&amp;arg2=value2", then there will be two parameters ("arg1" with value "value1" and "arg2" with value "value2")
		/// </summary>
		public SortedList<string, string> QueryString = new SortedList<string, string>();
		/// <summary>
		/// A SortedList mapping keys to values of parameters.  No character case conversion is applied in this list.  This list is populated parameters that were appended to the url (the query string).  e.g. if the url is "mypage.html?arg1=value1&amp;arg2=value2", then there will be two parameters ("arg1" with value "value1" and "arg2" with value "value2")
		/// </summary>
		public SortedList<string, string> RawQueryString = new SortedList<string, string>();

		/// <summary>
		/// The mimetype of the posted content.
		/// </summary>
		public string postContentType = "";

		/// <summary>
		/// The raw posted content as a string, populated only if the mimetype was "application/x-www-form-urlencoded"
		/// </summary>
		public string postFormDataRaw = "";

		/// <summary>
		/// A flag that is set when WriteSuccess(), WriteFailure(), or WriteRedirect() is called.
		/// </summary>
		public bool responseWritten = false;

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
		#endregion

		public readonly bool secure_https;
		private X509Certificate2 ssl_certificate;
		/// <summary>
		/// The type of compression that will be used for the response stream.
		/// </summary>
		public CompressionType compressionType { get; private set; } = CompressionType.None;

		/// <summary>
		/// This is a reference to the MemoryStream containing the post body, for internal use only by the ProxyTo method.
		/// </summary>
		private MemoryStream _internal_post_body_for_proxy;
		#endregion

		public HttpProcessor(TcpClient s, HttpServer srv, X509Certificate2 ssl_certificate = null)
		{
			this.ssl_certificate = ssl_certificate;
			this.secure_https = ssl_certificate != null;
			this.tcpClient = s;
			this.base_uri_this_server = new Uri("http" + (this.secure_https ? "s" : "") + "://" + s.Client.LocalEndPoint.ToString(), UriKind.Absolute);
			this.srv = srv;
		}

		public HttpProcessor(bool secure_https)
		{
			this.secure_https = secure_https;
		}

		private string streamReadLine(Stream inputStream, int maxLength = 32768)
		{
			int next_char;
			bool endOfStream = false;
			StringBuilder data = new StringBuilder();
			while (true)
			{
				next_char = inputStream.ReadByte();
				if (next_char == '\n') { break; }
				if (next_char == '\r') { continue; }
				if (next_char == -1)
				{
					endOfStream = true;
					break;
				};
				if (data.Length >= maxLength)
					throw new HttpProcessorException("413 Entity Too Large");
				data.Append(Convert.ToChar(next_char));
			}
			if (endOfStream && data.Length == 0)
				return null;
			return data.ToString();
		}
		private class HttpProcessorException : Exception
		{
			public HttpProcessorException(string message) : base(message) { }
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
				if (this.secure_https)
				{
					try
					{
						tcpStream = new System.Net.Security.SslStream(tcpStream, false, null, null);
						((System.Net.Security.SslStream)tcpStream).AuthenticateAsServer(ssl_certificate, false, System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls11 | System.Security.Authentication.SslProtocols.Tls, false);
					}
					catch (ThreadAbortException) { throw; }
					catch (SocketException) { throw; }
					catch (Exception ex)
					{
						SimpleHttpLogger.LogVerbose(ex);
						return;
					}
				}
				//int inputStreamThrottlingRuleset = 1;
				//int outputStreamThrottlingRuleset = 0;
				//if (IsLanConnection)
				//	inputStreamThrottlingRuleset = outputStreamThrottlingRuleset = 2;
				//inputStream =  new GlobalThrottledStream(tcpStream, inputStreamThrottlingRuleset, RemoteIPAddressInt);
				//rawOutputStream = new GlobalThrottledStream(tcpStream, outputStreamThrottlingRuleset, RemoteIPAddressInt);
				outputStream = new StreamWriter(tcpStream, Utf8NoBOM);
				try
				{
					parseRequest();
					readHeaders();
					if (srv.XRealIPHeader)
					{
						string headerValue = GetHeaderValue("x-real-ip");
						if (!string.IsNullOrWhiteSpace(headerValue))
						{
							IPAddress addr;
							if (IPAddress.TryParse(headerValue, out addr))
								remoteIpAddress = addr;
						}
					}
					if (srv.XForwardedForHeader)
					{
						string headerValue = GetHeaderValue("x-forwarded-for");
						if (!string.IsNullOrWhiteSpace(headerValue))
						{
							IPAddress addr;
							if (IPAddress.TryParse(headerValue, out addr))
								remoteIpAddress = addr;
						}
					}
					RawQueryString = ParseQueryStringArguments(this.request_url.Query, preserveKeyCharacterCase: true);
					QueryString = ParseQueryStringArguments(this.request_url.Query);
					requestCookies = Cookies.FromString(GetHeaderValue("Cookie", ""));
					try
					{
						if (http_method.Equals("GET"))
						{
							if (shouldLogRequestsToFile())
								SimpleHttpLogger.LogRequest(DateTime.Now, this.RemoteIPAddressStr, "GET", request_url.OriginalString);
							handleGETRequest();
						}
						else if (http_method.Equals("POST"))
						{
							if (shouldLogRequestsToFile())
								SimpleHttpLogger.LogRequest(DateTime.Now, this.RemoteIPAddressStr, "POST", request_url.OriginalString);
							handlePOSTRequest();
						}
					}
					catch (ThreadAbortException) { throw; }
					catch (Exception e)
					{
						if (!isOrdinaryDisconnectException(e))
							SimpleHttpLogger.Log(e);
						if (!responseWritten)
							writeFailure("500 Internal Server Error");
					}
					finally
					{
						if (!responseWritten)
							this.writeFailure();
					}
				}
				catch (ThreadAbortException) { throw; }
				catch (HttpProcessorException e)
				{
					SimpleHttpLogger.LogVerbose(e);
					if (shouldLogRequestsToFile())
						SimpleHttpLogger.LogRequest(DateTime.Now, this.RemoteIPAddressStr, "FAIL", request_url.OriginalString);
					if (!responseWritten)
						this.writeFailure(e.Message);
				}
				catch (Exception e)
				{
					if (!isOrdinaryDisconnectException(e))
						SimpleHttpLogger.LogVerbose(e);
					if (shouldLogRequestsToFile())
						SimpleHttpLogger.LogRequest(DateTime.Now, this.RemoteIPAddressStr, "FAIL", request_url.OriginalString);
					if (!responseWritten)
						this.writeFailure("400 Bad Request", "The request cannot be fulfilled due to bad syntax.");
				}
				outputStream.Flush();
				tcpStream.Flush();
				// For some reason, GZip compression only works if we dispose streams here, not in the finally block.
				try
				{
					outputStream.Dispose();
				}
				catch (ThreadAbortException) { throw; }
				catch (Exception ex) { SimpleHttpLogger.LogVerbose(ex); }
				try
				{
					tcpStream.Dispose();
				}
				catch (ThreadAbortException) { throw; }
				catch (Exception ex) { SimpleHttpLogger.LogVerbose(ex); }
				outputStream = null;
				tcpStream = null;
			}
			catch (ThreadAbortException) { throw; }
			catch (Exception ex)
			{
				if (!isOrdinaryDisconnectException(ex))
					SimpleHttpLogger.LogVerbose(ex);
			}
			finally
			{
				try
				{
					if (tcpClient != null)
						tcpClient.Close();
				}
				catch (ThreadAbortException) { throw; }
				catch (Exception ex) { SimpleHttpLogger.LogVerbose(ex); }
				try
				{
					if (tcpStream != null)
						tcpStream.Dispose();
				}
				catch (ThreadAbortException) { throw; }
				catch (Exception ex) { SimpleHttpLogger.LogVerbose(ex); }
			}
		}

		public bool isOrdinaryDisconnectException(Exception ex)
		{
			if (ex is IOException)
			{
				if (ex.InnerException != null && ex.InnerException is SocketException)
				{
					//if (ex.InnerException.Message.Contains("An established connection was aborted by the software in your host machine")
					//	|| ex.InnerException.Message.Contains("An existing connection was forcibly closed by the remote host")
					//	|| ex.InnerException.Message.Contains("The socket has been shut down") /* Mono/Linux */
					//	|| ex.InnerException.Message.Contains("Connection reset by peer") /* Mono/Linux */
					//	|| ex.InnerException.Message.Contains("The socket is not connected") /* Mono/Linux */
					//	)
					return true; // Connection aborted.  This happens often enough that reporting it can be excessive.
				}
			}
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
		/// Parses the first line of the http request to get the request method, url, and protocol version.
		/// </summary>
		private void parseRequest()
		{
			string request = streamReadLine(tcpStream);
			if (request == null)
				throw new Exception("End of stream");
			string[] tokens = request.Split(' ');
			if (tokens.Length != 3)
				throw new Exception("invalid http request line: " + request);
			http_method = tokens[0].ToUpper();

			if (tokens[1].StartsWith("http://") || tokens[1].StartsWith("https://"))
				request_url = new Uri(tokens[1]);
			else
				request_url = new Uri(base_uri_this_server, tokens[1]);

			requestedPage = request_url.AbsolutePath.StartsWith("/") ? request_url.AbsolutePath.Substring(1) : request_url.AbsolutePath;

			http_protocol_versionstring = tokens[2];
		}

		/// <summary>
		/// Parses the http headers
		/// </summary>
		private void readHeaders()
		{
			string line;
			while ((line = streamReadLine(tcpStream)) != "")
			{
				if (line == null)
					throw new Exception("End of stream");
				int separator = line.IndexOf(':');
				if (separator == -1)
					throw new Exception("invalid http header line: " + line);
				string name = line.Substring(0, separator);
				int pos = separator + 1;
				while (pos < line.Length && line[pos] == ' ')
					pos++; // strip any spaces

				string value = line.Substring(pos, line.Length - pos);
				AddOrUpdateHeaderValue(name, value);
			}
		}

		/// <summary>
		/// Adds or updates the header with the specified value.  If the header already has a value in our map(s), a comma will be appended, then the new value will be appended.
		/// </summary>
		/// <param name="headerName"></param>
		/// <param name="value"></param>
		private void AddOrUpdateHeaderValue(string headerName, string value)
		{
			string lower = headerName.ToLower();
			string existingValue = "";

			if (httpHeaders.TryGetValue(lower, out existingValue))
				httpHeaders[lower] = existingValue + "," + value;
			else
				httpHeaders[lower] = value;

			if (httpHeadersRaw.TryGetValue(headerName, out existingValue))
				httpHeadersRaw[headerName] = existingValue + "," + value;
			else
				httpHeadersRaw[headerName] = value;
		}

		/// <summary>
		/// Asks the HttpServer to handle this request as a GET request.  If the HttpServer does not write a response code header, this will write a generic failure header.
		/// </summary>
		private void handleGETRequest()
		{
			srv.handleGETRequest(this);
		}
		/// <summary>
		/// This post data processing just reads everything into a memory stream.
		/// This is fine for smallish things, but for large stuff we should really
		/// hand an input stream to the request processor. However, the input stream 
		/// we hand to the user's code needs to see the "end of the stream" at this 
		/// content length, because otherwise it won't know where the end is!
		/// 
		/// If the HttpServer does not write a response code header, this will write a generic failure header.
		/// </summary>
		private void handlePOSTRequest()
		{
			int content_len = 0;
			using (MemoryStream ms = new MemoryStream())
			{
				_internal_post_body_for_proxy = ms;
				string content_length_str = GetHeaderValue("Content-Length");
				if (!string.IsNullOrWhiteSpace(content_length_str))
				{
					if (int.TryParse(content_length_str, out content_len))
					{
						if (content_len > MAX_POST_SIZE)
						{
							SimpleHttpLogger.LogVerbose("POST Content-Length(" + content_len + ") too big for this simple server.  Server can handle up to " + MAX_POST_SIZE);
							this.writeFailure("413 Request Entity Too Large", "Request Too Large");
							return;
						}
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
				else
				{
					this.writeFailure("411 Length Required", "The request did not specify the length of its content.");
					SimpleHttpLogger.LogVerbose("The request did not specify the length of its content.  This server requires that all POST requests include a Content-Length header.");
					return;
				}

				postContentType = GetHeaderValue("Content-Type");
				if (postContentType != null && postContentType.Contains("application/x-www-form-urlencoded"))
				{
					StreamReader sr = new StreamReader(ms, Utf8NoBOM);
					postFormDataRaw = sr.ReadToEnd();

					RawPostParams = ParseQueryStringArguments(postFormDataRaw, false, true, convertPlusToSpace: true);
					PostParams = ParseQueryStringArguments(postFormDataRaw, false, convertPlusToSpace: true);

					try
					{
						srv.handlePOSTRequest(this, null);
					}
					finally
					{
						sr.Dispose();
					}
				}
				else
				{
					srv.handlePOSTRequest(this, new StreamReader(ms, Utf8NoBOM));
				}
				_internal_post_body_for_proxy = null;
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
		public void EnableCompressionIfSet()
		{
			if (!responseWritten)
				return;
			if (compressionType == CompressionType.GZip)
			{
				if (tcpStream is GZipStream)
					return;
				outputStream.Flush();
				tcpStream.Flush();
				tcpStream = new GZipStream(tcpStream, CompressionLevel.Optimal, false);
				outputStream = new StreamWriter(tcpStream);
			}
		}
		public bool ClientRequestsGZipCompression
		{
			get
			{
				string acceptEncoding = GetHeaderValue("Accept-Encoding");
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
		/// <param name="contentLength">(OPTIONAL) The length of your response, in bytes, if you know it.</param>
		/// <param name="responseCode">(OPTIONAL) The response code and optional status string.</param>
		/// <param name="additionalHeaders">(OPTIONAL) Additional headers to include in the response.</param>
		public virtual void writeSuccess(string contentType = "text/html; charset=UTF-8", long contentLength = -1, string responseCode = "200 OK", List<KeyValuePair<string, string>> additionalHeaders = null)
		{
			responseWritten = true;
			outputStream.WriteLineRN("HTTP/1.1 " + responseCode);
			if (!string.IsNullOrEmpty(contentType))
				outputStream.WriteLineRN("Content-Type: " + contentType);
			if (contentLength > -1)
				outputStream.WriteLineRN("Content-Length: " + contentLength);
			if (compressionType == CompressionType.GZip)
				outputStream.WriteLineRN("Content-Encoding: gzip");
			string cookieStr = responseCookies.ToString();
			if (!string.IsNullOrEmpty(cookieStr))
				outputStream.WriteLineRN(cookieStr);
			if (additionalHeaders != null)
				foreach (KeyValuePair<string, string> header in additionalHeaders)
					outputStream.WriteLineRN(header.Key + ": " + header.Value);
			outputStream.WriteLineRN("Connection: close");
			outputStream.WriteLineRN("");
			//if (contentLength > 1500)
			tcpClient.NoDelay = true;
			EnableCompressionIfSet();
		}

		/// <summary>
		/// Writes a failure response header.  Call this one time to return an error response.
		/// </summary>
		/// <param name="code">(OPTIONAL) The http error code (including explanation entity).  For example: "404 Not Found" where 404 is the error code and "Not Found" is the explanation.</param>
		/// <param name="description">(OPTIONAL) A description string to send after the headers as the response.  This is typically shown to the remote user in his browser.  If null, the code string is sent here.  If "", no response body is sent by this function, and you may or may not want to write your own.</param>
		public virtual void writeFailure(string code = "404 Not Found", string description = null)
		{
			responseWritten = true;
			outputStream.WriteLineRN("HTTP/1.1 " + code);
			outputStream.WriteLineRN("Connection: close");
			outputStream.WriteLineRN("");
			if (description == null)
				outputStream.WriteLineRN(code);
			else if (description != "")
				outputStream.WriteLineRN(description);
		}

		/// <summary>
		/// Writes a redirect header instructing the remote user's browser to load the URL you specify.  Call this one time and do not write any other data to the response stream.
		/// </summary>
		/// <param name="redirectToUrl">URL to redirect to.</param>
		public virtual void writeRedirect(string redirectToUrl)
		{
			responseWritten = true;
			outputStream.WriteLineRN("HTTP/1.1 302 Found");
			outputStream.WriteLineRN("Location: " + redirectToUrl);
			outputStream.WriteLineRN("Connection: close");
			outputStream.WriteLineRN("");
		}

		/// <summary>
		/// Writes response headers to finish the WebSocket handshake with the client. No extensions are supported (such as compression) at this time.
		/// This method is supposed to facilitate linking two WebSocket clients together using this server as a proxy.
		/// NOTE: This functionality is untested and likely does not work as intended.
		/// </summary>
		public virtual void writeWebSocketProxy()
		{
			responseWritten = true;
			outputStream.WriteLineRN("HTTP/1.1 101 Switching Protocols");
			outputStream.WriteLineRN("Upgrade: websocket");
			outputStream.WriteLineRN("Connection: Upgrade");
			outputStream.WriteLineRN("Sec-WebSocket-Accept: " + CreateWebSocketResponseKey(this.GetHeaderValue("sec-websocket-key")));
			outputStream.WriteLineRN("");
		}
		protected static string CreateWebSocketResponseKey(string base64Key)
		{
			SHA1 sha1 = new SHA1CryptoServiceProvider();
			byte[] hashData = sha1.ComputeHash(Utf8NoBOM.GetBytes(base64Key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")); // It may be incorrect to use Utf8NoBOM here.
			return Convert.ToBase64String(hashData);
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
		/// Acts as a proxy server, sending the request to a different URL.  This method starts a new (and unpooled) thread to handle the response from the remote server.
		/// The "Host" header is rewritten (or added) and output as the first header.
		/// </summary>
		/// <param name="newUrl">The URL to proxy the original request to.</param>
		/// <param name="networkTimeoutMs">The send and receive timeout to set for both TcpClients, in milliseconds.</param>
		/// <param name="acceptAnyCert">If true, certificate validation will be disabled for outgoing https connections.</param>
		/// <param name="snoopy">If non-null, proxied communication will be copied into this object so you can snoop on it.</param>
		/// <param name="host">The value of the host header, also used in SSL authentication. If null or whitespace, it is set from the [newUrl] parameter.</param>
		/// <param name="singleRequestOnly">If true, a Connection: close header will be added, and any existing Connection header will be dropped.</param>
		public void ProxyTo(string newUrl, int networkTimeoutMs = 60000, bool acceptAnyCert = false, ProxyDataBuffer snoopy = null, string host = null, bool singleRequestOnly = false)
		{
			responseWritten = true;
			//try
			//{
			// Connect to the server we're proxying to.
			Uri newUri = new Uri(newUrl);
			if (string.IsNullOrWhiteSpace(host))
				host = newUri.DnsSafeHost;
			TcpClient proxyClient = new TcpClient();
			proxyClient.ReceiveTimeout = this.tcpClient.ReceiveTimeout = networkTimeoutMs;
			proxyClient.SendTimeout = this.tcpClient.SendTimeout = networkTimeoutMs;
			proxyClient.Connect(newUri.DnsSafeHost, newUri.Port);
			Stream proxyStream = proxyClient.GetStream();
			if (newUri.Scheme == "https")
			{
				//try
				//{
				System.Net.Security.RemoteCertificateValidationCallback certCallback = null;
				if (acceptAnyCert)
					certCallback = (sender, certificate, chain, sslPolicyErrors) => true;
				proxyStream = new System.Net.Security.SslStream(proxyStream, false, certCallback, null);
				((System.Net.Security.SslStream)proxyStream).AuthenticateAsClient(host, null, System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls11 | System.Security.Authentication.SslProtocols.Tls, false);
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
			if (singleRequestOnly)
				_ProxyString(ProxyDataDirection.RequestToServer, proxyStream, "Connection: close\r\n", snoopy);
			foreach (KeyValuePair<string, string> header in httpHeadersRaw)
			{
				string keyLower = header.Key.ToLower();
				if (keyLower != "host" && (!singleRequestOnly || keyLower != "connection"))
					_ProxyString(ProxyDataDirection.RequestToServer, proxyStream, header.Key + ": " + header.Value + "\r\n", snoopy);
			}
			_ProxyString(ProxyDataDirection.RequestToServer, proxyStream, "\r\n", snoopy);

			// Write the original POST body if there was one.
			if (_internal_post_body_for_proxy != null)
			{
				long remember_position = _internal_post_body_for_proxy.Position;
				_internal_post_body_for_proxy.Seek(0, SeekOrigin.Begin);
				byte[] buf = _internal_post_body_for_proxy.ToArray();
				_ProxyData(ProxyDataDirection.RequestToServer, proxyStream, buf, buf.Length, snoopy);
				_internal_post_body_for_proxy.Seek(remember_position, SeekOrigin.Begin);
			}

			// Start a thread to connect to newUrl and proxy its response to our client.
			Thread parentThread = Thread.CurrentThread;
			Thread ResponseProxyThread = new Thread(() =>
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
					SimpleHttpLogger.LogVerbose(ex);
				}
			});
			ResponseProxyThread.IsBackground = true;
			ResponseProxyThread.Start();

			// The current thread will handle any additional incoming data from our client and proxy it to newUrl.
			this.tcpClient.NoDelay = true;
			CopyStreamUntilClosed(ProxyDataDirection.RequestToServer, this.tcpStream, proxyStream, snoopy);
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
			ProxyDataItem item = new ProxyDataItem(Direction, str);
			snoopy?.AddItem(item);
			//DebugLogStreamWrite(item.ToString());
			byte[] buf = Utf8NoBOM.GetBytes(str);
			target.Write(buf, 0, buf.Length);
		}
		private void _ProxyData(ProxyDataDirection Direction, Stream target, byte[] buf, int length, ProxyDataBuffer snoopy)
		{
			if (buf.Length != length)
				buf = ByteUtil.SubArray(buf, 0, length);
			ProxyDataItem item = new ProxyDataItem(Direction, buf);
			snoopy?.AddItem(item);
			//DebugLogStreamWrite(item.ToString() + "\r\n");
			target.Write(buf, 0, buf.Length);
		}
		//private object DebugWriterLock = new object();
		//private int rnd = StaticRandom.Next();
		//private void DebugLogStreamWrite(string content)
		//{
		//	lock (DebugWriterLock)
		//	{
		//		File.AppendAllText(rnd + ".txt", content);
		//	}
		//}
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
		public double GetDoubleParam(string key, int defaultValue = 0)
		{
			return GetQSDoubleParam(key, defaultValue);
		}
		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value. This function interprets a value of "1" or "true" (case insensitive) as being true.  Any other parameter value is interpreted as false.</returns>
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
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value. This function interprets a value of "1" or "true" (case insensitive) as being true.  Any other parameter value is interpreted as false.</returns>
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
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value. This function interprets a value of "1" or "true" (case insensitive) as being true.  Any other parameter value is interpreted as false.</returns>
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
	}

	public abstract class HttpServer : IDisposable
	{
		/// <summary>
		/// If > -1, the server was told to listen for http connections on this port.  Port 0 causes the socket library to choose its own port.
		/// </summary>
		protected readonly int port;
		/// <summary>
		/// If > -1, the server was told to listen for https connections on this port.  Port 0 causes the socket library to choose its own port.
		/// </summary>
		protected readonly int secure_port;
		protected int actual_port_http = -1;
		protected int actual_port_https = -1;
		/// <summary>
		/// The actual port the http server is listening on.  Will be -1 if not listening.
		/// </summary>
		public int Port_http
		{
			get
			{
				return actual_port_http;
			}
		}
		/// <summary>
		/// The actual port the http server is listening on.  Will be -1 if not listening.
		/// </summary>
		public int Port_https
		{
			get
			{
				return actual_port_https;
			}
		}
		public int? SendBufferSize = null;
		public int? ReceiveBufferSize = null;
		/// <summary>
		/// If true, the IP address of remote hosts will be learned from the HTTP header named "X-Real-IP".
		/// </summary>
		public bool XRealIPHeader = false;
		/// <summary>
		/// If true, the IP address of remote hosts will be learned from the HTTP header named "X-Forwarded-For".
		/// </summary>
		public bool XForwardedForHeader = false;
		protected volatile bool stopRequested = false;
		protected X509Certificate2 ssl_certificate;
		private Thread thrHttp;
		private Thread thrHttps;
		private TcpListener unsecureListener = null;
		private TcpListener secureListener = null;
		/// <summary>
		/// Raised when a listening socket is bound to a port.  The Event Handler passes along a string which can be printed to the console, announcing this event.
		/// </summary>
		public event EventHandler<string> SocketBound = delegate { };
		/// <summary>
		/// Raised when an SSL connection is made using a certificate that will expire within the next 14 days.  This event will not be raised more than once in a 60 minute period (assuming the same HttpServer instance is used).
		/// The TimeSpan argument indicates the time to expiration, which may be less than or equal to TimeSpan.Zero if the certificate is expired.
		/// </summary>
		public event EventHandler<TimeSpan> CertificateExpirationWarning = delegate { };
		private DateTime timeOfNextCertificateExpirationWarning = DateTime.MinValue;

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
		public readonly IPAddress bindAddr = IPAddress.Any;

		public SimpleThreadPool pool;
		/// <summary>
		/// 
		/// </summary>
		/// <param name="port">The port number on which to accept regular http connections. If -1, the server will not listen for http connections.</param>
		/// <param name="httpsPort">(Optional) The port number on which to accept https connections. If -1, the server will not listen for https connections.</param>
		/// <param name="cert">(Optional) Certificate to use for https connections.  If null and an httpsPort was specified, a certificate is automatically created if necessary and loaded from "SimpleHttpServer-SslCert.pfx" in the same directory that the current executable is located in.</param>
		/// <param name="bindAddr">If not null, the server will bind to this address.  Default: IPAddress.Any.</param>
		public HttpServer(int port, int httpsPort = -1, X509Certificate2 cert = null, IPAddress bindAddr = null)
		{
			pool = new SimpleThreadPool("SimpleHttpServer");

			this.port = port;
			this.secure_port = httpsPort;
			this.ssl_certificate = cert;
			if (bindAddr != null)
				this.bindAddr = bindAddr;

			if (this.port > 65535 || this.port < -1) this.port = -1;
			if (this.secure_port > 65535 || this.secure_port < -1) this.secure_port = -1;

			if (this.port > -1)
			{
				thrHttp = new Thread(listen);
				thrHttp.IsBackground = true;
				thrHttp.Name = "HttpServer Thread";
			}

			if (this.secure_port > -1)
			{
				if (ssl_certificate == null)
					ssl_certificate = HttpServer.GetSelfSignedCertificate();
				thrHttps = new Thread(listen);
				thrHttps.IsBackground = true;
				thrHttps.Name = "HttpsServer Thread";
			}
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
		/// <param name="s"></param>
		/// <param name="srv"></param>
		/// <param name="cert"></param>
		/// <returns></returns>
		public virtual IProcessor MakeClientProcessor(TcpClient s, HttpServer srv, X509Certificate2 cert)
		{
			return new HttpProcessor(s, srv, cert);
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
		/// <summary>
		/// Sets a new SSL certificate to be used for all future connections;
		/// </summary>
		/// <param name="newCertificate"></param>
		public void SetCertificate(X509Certificate2 newCertificate)
		{
			ssl_certificate = newCertificate;
		}
		/// <summary>
		/// Returns the date in local time after which the certificate is no longer valid.  If the certificate is null, returns DateTime.MaxValue.
		/// </summary>
		/// <returns></returns>
		public DateTime GetCertificateExpiration()
		{
			if (ssl_certificate != null)
				return ssl_certificate.NotAfter;
			return DateTime.MaxValue;
		}
		/// <summary>
		/// Returns the date in local time after which the certificate is no longer valid.  If the certificate is null, returns DateTime.MaxValue.
		/// </summary>
		/// <returns></returns>
		public string GetCertificateFriendlyName()
		{
			if (ssl_certificate != null)
				return ssl_certificate.FriendlyName;
			return null;
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
					try
					{
						fiExe = new FileInfo(System.Windows.Forms.Application.ExecutablePath);
					}
					catch
					{
						fiExe = new FileInfo(Globals.ApplicationDirectoryBase + Globals.ExecutableNameWithExtension);
					}
				}
				FileInfo fiCert = new FileInfo(fiExe.Directory.FullName + "/SimpleHttpServer-SslCert.pfx");
				if (fiCert.Exists)
					ssl_certificate = new X509Certificate2(fiCert.FullName, "N0t_V3ry-S3cure#lol");
				else
				{
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

						byte[] certData = ssl_certificate.Export(X509ContentType.Pfx, "N0t_V3ry-S3cure#lol");
						File.WriteAllBytes(fiCert.FullName, certData);
					}
				}
				return ssl_certificate;
			}
		}

		/// <summary>
		/// Listens for connections, somewhat robustly.  Does not return until the server is stopped or until more than 100 listener restarts occur in a single day.
		/// </summary>
		private void listen(object param)
		{
			try
			{
				bool isSecureListener = (bool)param;

				int errorCount = 0;
				DateTime lastError = DateTime.Now;

				TcpListener listener = null;

				while (!stopRequested)
				{
					bool threwExceptionOuter = false;
					try
					{
						listener = new TcpListener(bindAddr, isSecureListener ? secure_port : port);
						if (isSecureListener)
							secureListener = listener;
						else
							unsecureListener = listener;
						listener.Start();
						if (isSecureListener)
							actual_port_https = ((IPEndPoint)listener.LocalEndpoint).Port;
						else
							actual_port_http = ((IPEndPoint)listener.LocalEndpoint).Port;
						try
						{
							SocketBound(this, "Web Server listening on port " + (isSecureListener ? (actual_port_https + " (https)") : (actual_port_http + " (http)")));
						}
						catch (ThreadAbortException) { throw; }
						catch (Exception ex)
						{
							SimpleHttpLogger.Log(ex);
						}

						DateTime innerLastError = DateTime.Now;
						int innerErrorCount = 0;
						while (!stopRequested)
						{
							try
							{
								TcpClient s = listener.AcceptTcpClient();
								// TcpClient's timeouts are merely limits on Read() and Write() call blocking time.  If we try to read or write a chunk of data that legitimately takes longer than the timeout to finish, it will still time out even if data was being transferred steadily.
								if (s.ReceiveTimeout < 10000 && s.ReceiveTimeout != 0) // Timeout of 0 is infinite
									s.ReceiveTimeout = 10000;
								if (s.SendTimeout < 10000 && s.SendTimeout != 0) // Timeout of 0 is infinite
									s.SendTimeout = 10000;
								if (ReceiveBufferSize != null)
									s.ReceiveBufferSize = ReceiveBufferSize.Value;
								if (SendBufferSize != null)
									s.SendBufferSize = SendBufferSize.Value;
								X509Certificate2 cert = isSecureListener ? ssl_certificate : null;
								DateTime now = DateTime.Now;
								if (cert != null && now.AddDays(14) > cert.NotAfter && timeOfNextCertificateExpirationWarning < now)
								{
									timeOfNextCertificateExpirationWarning = now.AddHours(1);
									try
									{
										CertificateExpirationWarning(this, now - cert.NotAfter);
									}
									catch (ThreadAbortException) { throw; }
									catch (Exception ex)
									{
										SimpleHttpLogger.Log(ex);
									}
								}
								HttpProcessor processor = new HttpProcessor(s, this, cert);
								pool.Enqueue(processor.Process);
								//	try
								//	{
								//		StreamWriter outputStream = new StreamWriter(s.GetStream());
								//		outputStream.WriteLineRN("HTTP/1.1 503 Service Unavailable");
								//		outputStream.WriteLineRN("Connection: close");
								//		outputStream.WriteLineRN("");
								//		outputStream.WriteLineRN("Server too busy");
								//	}
								//	catch (ThreadAbortException) { throw; }
							}
							catch (ThreadAbortException) { throw; }
							catch (Exception ex)
							{
								if (ex.Message == "A blocking operation was interrupted by a call to WSACancelBlockingCall")
								{
								}
								else
								{
									if (DateTime.Now.Hour != innerLastError.Hour || DateTime.Now.DayOfYear != innerLastError.DayOfYear)
									{
										innerLastError = DateTime.Now;
										innerErrorCount = 0;
									}
									if (++innerErrorCount > 10)
										throw ex;
									SimpleHttpLogger.Log(ex, "Inner Error count this hour: " + innerErrorCount);
									Thread.Sleep(1);
								}
							}
						}
					}
					catch (ThreadAbortException) { stopRequested = true; }
					catch (Exception ex)
					{
						if (ex.Message == "A blocking operation was interrupted by a call to WSACancelBlockingCall")
						{
						}
						else
						{
							if (DateTime.Now.DayOfYear != lastError.DayOfYear || DateTime.Now.Year != lastError.Year)
							{
								lastError = DateTime.Now;
								errorCount = 0;
							}
							if (++errorCount > 200)
								throw ex;
							SimpleHttpLogger.Log(ex, "Restarting listener. Outer Error count today: " + errorCount);
							threwExceptionOuter = true;
						}
					}
					finally
					{
						try
						{
							if (listener != null)
							{
								listener.Stop();
								if (threwExceptionOuter)
									Thread.Sleep(1000);
							}
						}
						catch (ThreadAbortException) { stopRequested = true; }
						catch (Exception) { }
					}
				}
			}
			catch (ThreadAbortException) { stopRequested = true; }
			catch (Exception ex)
			{
				SimpleHttpLogger.Log(ex, "Exception thrown in outer loop.  Exiting listener.");
			}
		}

		/// <summary>
		/// Starts listening for connections.
		/// </summary>
		public void Start()
		{
			if (thrHttp != null)
				thrHttp.Start(false);
			if (thrHttps != null)
				thrHttps.Start(true);
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
			if (unsecureListener != null)
				try { unsecureListener.Stop(); } catch (Exception ex) { SimpleHttpLogger.Log(ex); }
			if (secureListener != null)
				try { secureListener.Stop(); } catch (Exception ex) { SimpleHttpLogger.Log(ex); }
			if (thrHttp != null)
				try { thrHttp.Abort(); } catch (Exception ex) { SimpleHttpLogger.Log(ex); }
			if (thrHttps != null)
				try { thrHttps.Abort(); } catch (Exception ex) { SimpleHttpLogger.Log(ex); }
			try { stopServer(); } catch (Exception ex) { SimpleHttpLogger.Log(ex); }
			try { GlobalThrottledStream.ThrottlingManager.Shutdown(); } catch (Exception ex) { SimpleHttpLogger.Log(ex); }
		}

		/// <summary>
		/// Blocks the calling thread until the http listening threads finish or the timeout expires.  Call this after calling Stop() if you need to wait for the listener to clean up, such as if you intend to start another instance of the server using the same port(s).
		/// </summary>
		/// <param name="timeout_milliseconds">Maximum number of milliseconds to wait for the HttpServer Threads to stop.</param>
		public bool Join(int timeout_milliseconds = 2000)
		{
			bool success = true;
			System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
			int timeToWait = timeout_milliseconds;
			stopwatch.Start();
			if (timeToWait > 0)
			{
				try
				{
					if (thrHttp != null && thrHttp.IsAlive)
						success = thrHttp.Join(timeToWait) && success;
				}
				catch (ThreadAbortException) { throw; }
				catch (Exception ex)
				{
					SimpleHttpLogger.Log(ex);
				}
			}
			stopwatch.Stop();
			timeToWait = timeout_milliseconds - (int)stopwatch.ElapsedMilliseconds;
			if (timeToWait > 0)
			{
				try
				{
					if (thrHttps != null && thrHttps.IsAlive)
						success = thrHttps.Join(timeToWait) && success;
				}
				catch (ThreadAbortException) { throw; }
				catch (Exception ex)
				{
					SimpleHttpLogger.Log(ex);
				}
			}
			return success;
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
		/// This is called when the server is stopping.  Perform any cleanup work here.
		/// </summary>
		protected abstract void stopServer();

		public virtual bool shouldLogRequestsToFile()
		{
			return false;
		}
	}
	#region Helper Classes
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
		/// Adds a cookie with the specified name and value.  The cookie is set to expire immediately at the end of the browsing session.
		/// </summary>
		/// <param name="name">The cookie's name.</param>
		/// <param name="value">The cookie's value.</param>
		public void Add(string name, string value)
		{
			Add(name, value, TimeSpan.Zero);
		}
		/// <summary>
		/// Adds a cookie with the specified name, value, and lifespan.
		/// </summary>
		/// <param name="name">The cookie's name.</param>
		/// <param name="value">The cookie's value.</param>
		/// <param name="expireTime">The amount of time before the cookie should expire.</param>
		public void Add(string name, string value, TimeSpan expireTime)
		{
			if (name == null)
				return;
			name = name.ToLower();
			cookieCollection[name] = new Cookie(name, value, expireTime);
		}
		/// <summary>
		/// Gets the cookie with the specified name.  If the cookie is not found, null is returned;
		/// </summary>
		/// <param name="name">The name of the cookie.</param>
		/// <returns></returns>
		public Cookie Get(string name)
		{
			Cookie cookie;
			if (!cookieCollection.TryGetValue(name, out cookie))
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
}