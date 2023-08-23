using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
	public class ProxyClient
	{
		/// <summary>
		/// Web origin which this client is bound to, all lower case. e.g. "https://example.com"
		/// </summary>
		public readonly string Origin;
		private TcpClient proxyClient;
		private Stream proxyStream;
		private CountdownStopwatch expireTimer = CountdownStopwatch.StartNew(TimeSpan.FromMinutes(60));
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
					ProxyClient client;
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
						result = await client.BeginProxyRequestAsync(p, uri, options);
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

					if (result.IsProxyClientReusable)
					{
						pool.Enqueue(client);
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

			if (p.responseWritten)
				throw new ApplicationException("This ProxyClient is unable to complete the current request because a response has already been written to the client making this request.");

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
			p.tcpClient.SendTimeout = p.tcpClient.ReceiveTimeout = options.networkTimeoutMs.Clamp(0, 45000) + 15000;

			///////////////////////////
			// PHASE 1: ANALYZE REQUEST //
			///////////////////////////
			// Send the original client's request to the remote server with any necessary modifications.

			// Collection of lower case header names that are "hop-by-hop" and should not be proxied.  Values in the "Connection" header are supposed to be added to this collection on a per-request basis, but this server does not currently recognize comma separated values in the Connection header, therefore that is not happening.
			HashSet<string> doNotProxyHeaders = new HashSet<string>(new string[] {
				"keep-alive", "transfer-encoding", "te", "connection", "trailer", "upgrade", "proxy-authorization", "proxy-authenticate", "host"
			});

			// Figure out the Connection header
			bool ourClientWantsConnectionClose = p.http_protocol_versionstring == "HTTP/1.0";
			bool ourClientWantsConnectionKeepAlive = !ourClientWantsConnectionClose;
			bool ourClientWantsConnectionUpgrade = false;
			string incomingConnectionHeader = p.GetHeaderValue("connection");
			string[] incomingConnectionHeaderValues = incomingConnectionHeader?
				.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(s => s.Trim())
				.ToArray();
			if (incomingConnectionHeaderValues != null)
			{
				if (incomingConnectionHeaderValues.Contains("close", true))
				{
					ourClientWantsConnectionClose = true;
					ourClientWantsConnectionKeepAlive = false;
				}
				else if (incomingConnectionHeaderValues.Contains("keep-alive", true))
				{
					ourClientWantsConnectionClose = false;
					ourClientWantsConnectionKeepAlive = true;
				}
				if (incomingConnectionHeaderValues.Contains("upgrade", true))
					ourClientWantsConnectionUpgrade = true;
			}
			if (!ourClientWantsConnectionClose && !ourClientWantsConnectionKeepAlive && !ourClientWantsConnectionUpgrade)
			{
				p.writeFullResponseUTF8("Connection header had unrecognized value.", "text/plain; charset=UTF-8", "400 Bad Request");
				return new ProxyResult(ProxyResultErrorCode.Error, "Connection header had unrecognized value: " + incomingConnectionHeader, false, false);
			}

			string requestHeader_Connection;
			if (ourClientWantsConnectionUpgrade)
				requestHeader_Connection = "upgrade";
			else
			{
				if (options.allowConnectionKeepalive)
					requestHeader_Connection = "keep-alive";
				else
					requestHeader_Connection = "close";
			}

			bool ourClientWantsWebsocket = ourClientWantsConnectionUpgrade && "websocket".IEquals(p.GetHeaderValue("upgrade"));

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
					await Connect(uri, host, options, proxyTiming);
					didConnect = true;
				}
				catch (GatewayTimeoutException ex)
				{
					if (options.allowGatewayTimeoutResponse)
					{
						p.writeFullResponseUTF8("", "text/plain; charset=utf-8", "504 Gateway Timeout");
						return new ProxyResult(ProxyResultErrorCode.GatewayTimeout, ex.ToHierarchicalString(), false, false);
					}
					ex.Rethrow();
				}
				catch (TLSNegotiationFailedException ex)
				{
					p.writeFullResponseUTF8("", "text/plain; charset=utf-8", "502 Bad Gateway");
					return new ProxyResult(ProxyResultErrorCode.TLSNegotiationError, ex.ToHierarchicalString(), false, false);
				}
			}
			else
				options.log.AppendLine("Reusing old connection");

			proxyClient.SendTimeout = proxyClient.ReceiveTimeout = options.networkTimeoutMs.Clamp(0, 45000) + 15000;

			// Connection to remote server is now established and ready for data transfer.
			///////////////////////////
			// PHASE 3: SEND REQUEST //
			///////////////////////////
			// Send the first line of our HTTP request.
			proxyTiming?.Start("Send Request");
			options.bet?.Start("Send Request");
			string requestLine = p.http_method + ' ' + uri.PathAndQuery + ' ' + p.http_protocol_versionstring;
			try
			{
				_ProxyString(ProxyDataDirection.RequestToServer, proxyStream, requestLine + "\r\n", snoopy);
				// After the first line comes the request headers.
				string outgoingHostHeader = host;
				if (!uri.IsDefaultPort)
					outgoingHostHeader += ":" + uri.Port;
				_ProxyString(ProxyDataDirection.RequestToServer, proxyStream, "Host: " + outgoingHostHeader + "\r\n", snoopy);
				_ProxyString(ProxyDataDirection.RequestToServer, proxyStream, "Connection: " + requestHeader_Connection + "\r\n", snoopy);
				if (requestHeader_Upgrade != null)
					_ProxyString(ProxyDataDirection.RequestToServer, proxyStream, "Upgrade: " + requestHeader_Upgrade + "\r\n", snoopy);
				if (p.RequestBodyStream is ReadableChunkedTransferEncodingStream)
					_ProxyString(ProxyDataDirection.RequestToServer, proxyStream, "Transfer-Encoding: chunked\r\n", snoopy);

				ProcessProxyHeaders(p, options); // Manipulate X-Forwarded-For, etc.

				options.RaiseBeforeRequestHeadersSent(this, p.httpHeaders);

				foreach (HttpHeader header in p.httpHeaders)
				{
					if (!doNotProxyHeaders.Contains(header.Key, true))
						_ProxyString(ProxyDataDirection.RequestToServer, proxyStream, header.Key + ": " + header.Value + "\r\n", snoopy);
				}
				// Now a blank line to indicate the end of the request headers.
				_ProxyString(ProxyDataDirection.RequestToServer, proxyStream, "\r\n", snoopy);
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
			if (p.RequestBodyStream != null)
				await CopyStreamUntilClosedAsync(ProxyDataDirection.RequestToServer, p.RequestBodyStream, proxyStream, snoopy);

			proxyStream.Flush();

			////////////////////////////
			// PHASE 4: READ RESPONSE //
			////////////////////////////
			// Read response from remote server
			proxyTiming?.Start("Read Response Headers");
			options.bet?.Start("Read Response Headers");
			HttpResponseStatusLine responseStatusLine = null;
			HttpHeaderCollection proxyHttpHeaders = new HttpHeaderCollection();
			int responseStatusCodeInt = 0;
			try
			{
				string statusLineStr = HttpProcessor.streamReadLine(proxyStream);
				if (statusLineStr == null)
					return new ProxyResult(ProxyResultErrorCode.ConnectionLost, "ProxyClient encountered end of stream reading response status line from the remote server. Should retry with a new connection.", false, true);
				try
				{
					responseStatusLine = new HttpResponseStatusLine(statusLineStr);
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

				int? responseStatusCodeIntNullable = NumberUtil.FirstInt(responseStatusLine.StatusCode);
				if (responseStatusCodeIntNullable == null)
				{
					options.log.AppendLine("ERROR: ProxyClient encountered invalid response status line: " + responseStatusLine.OriginalStatusLine);
					return new ProxyResult(ProxyResultErrorCode.BadGateway, "ProxyClient encountered invalid response status line: " + responseStatusLine.OriginalStatusLine, false, false);
				}
				responseStatusCodeInt = responseStatusCodeIntNullable.Value;

				// Read response headers from remote server
				HttpProcessor.readHeaders(proxyStream, proxyHttpHeaders);
			}
			catch (HttpProcessor.EndOfStreamException ex)
			{
				if (!didConnect)
				{
					options.log.AppendLine("Recycled proxy stream EOF while reading response from remote server. Will retry.");
					return new ProxyResult(ProxyResultErrorCode.ConnectionLost, "ProxyClient encountered unexpected end of stream while reading the response from the remote server. Should retry with a new connection.", false, true);
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

			// Decide how to respond
			options.bet?.Start("Proxy Process Response Headers");
			ProxyResponseDecision decision = ProxyResponseDecision.Undefined;
			long proxyContentLength = 0;
			bool remoteServerWantsKeepalive = false;
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

			if (p.http_method == "HEAD")
				decision = ProxyResponseDecision.NoBody;
			else if (proxyConnectionHeaderValues.Contains("upgrade", true) && proxyHttpHeaders.TryGetValue("Upgrade", out string proxyUpgradeHeader) && proxyUpgradeHeader.IEquals("websocket"))
			{
				options.log.AppendLine("This is a websocket connection.");
				decision = ProxyResponseDecision.Websocket;
			}
			else
			{
				remoteServerWantsKeepalive = proxyConnectionHeaderValues.Contains("keep-alive");

				if (!HttpServer.HttpStatusCodeCanHaveResponseBody(responseStatusCodeInt))
					decision = ProxyResponseDecision.NoBody;
				else if (proxyHttpHeaders.TryGetValue("Content-Length", out string proxyContentLengthStr) && long.TryParse(proxyContentLengthStr, out proxyContentLength) && proxyContentLength > -1)
				{
					// Content-Length
					decision = ProxyResponseDecision.ContentLength;
				}
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
							decision = ProxyResponseDecision.ReadChunked;
							if (proxyTransferEncodingValues.Length > 1)
								options.log.AppendLine("WARNING: Transfer-Encoding is not recognized fully: " + proxyTransferEncoding);
						}
						else
							options.log.AppendLine("WARNING: Transfer-Encoding is not recognized: " + proxyTransferEncoding);
					}
					if (decision == ProxyResponseDecision.Undefined)
					{
						if (remoteServerWantsKeepalive)
							decision = ProxyResponseDecision.NoBody;
						else
							decision = ProxyResponseDecision.UntilClosed;
					}
				}

				//// keep-alive responses must either specify a "Content-Length" or use "Transfer-Encoding: chunked".
				//if (remoteServerWantsKeepalive && decision != ProxyResponseDecision.ContentLength && decision != ProxyResponseDecision.Chunked)
				//{
				//	p.writeFullResponseUTF8("", "text/plain; charset=utf-8", "502 Bad Gateway");
				//	return new ProxyResult(ProxyResultErrorCode.BadGateway, "ProxyTo can't process this request because the remote server specified \"Connection: keepalive\" without specifying a \"Content-Length\" or using \"Transfer-Encoding: chunked\". The request was: " + requestLine + ". All headers:\r\n" + string.Join("\r\n", proxyHttpHeadersRaw.Select(i => i.Key + ": " + i.Value)), false, false);
				//}
			}

			options.log.AppendLine("Response will be read as: " + decision + (decision == ProxyResponseDecision.ContentLength ? (" (" + proxyContentLength + ")") : ""));

			// Rewrite redirect location to point at this server
			if (proxyHttpHeaders.ContainsKey("Location"))
			{
				if (!string.IsNullOrWhiteSpace(p.hostName)
					&& Uri.TryCreate(proxyHttpHeaders["Location"], UriKind.Absolute, out Uri redirectUri)
					&& redirectUri.Host.IEquals(host))
				{
					UriBuilder uriBuilder = new UriBuilder(redirectUri);
					uriBuilder.Scheme = p.secure_https ? "https" : "http";
					uriBuilder.Host = p.hostName;
					uriBuilder.Port = ((IPEndPoint)p.tcpClient.Client.LocalEndPoint).Port;
					proxyHttpHeaders["Location"] = uriBuilder.Uri.ToString();
				}
			}
			/////////////////////////////
			// PHASE 5: WRITE RESPONSE //
			/////////////////////////////
			// Write response status line
			responseStatusLine.Version = "1.1"; // We do not speak HTTP 1.0, but the server we talked to might.
			options.bet?.Start("Proxy Write Response: " + decision);
			Stream outgoingStream = p.tcpStream;
			p.responseWritten = true;
			_ProxyString(ProxyDataDirection.ResponseFromServer, outgoingStream, responseStatusLine + "\r\n", snoopy);

			// Write response headers
			bool wrapOutputStreamChunked = false;
			string responseConnectionHeader;
			if (decision == ProxyResponseDecision.Websocket)
				responseConnectionHeader = "upgrade";
			else if (ourClientWantsConnectionClose)
				responseConnectionHeader = "close";
			else
			{
				responseConnectionHeader = "keep-alive";
				p.keepAlive = true;
			}
			_ProxyString(ProxyDataDirection.ResponseFromServer, outgoingStream, "Connection: " + responseConnectionHeader + "\r\n", snoopy);

			if (decision == ProxyResponseDecision.Websocket)
				_ProxyString(ProxyDataDirection.ResponseFromServer, outgoingStream, "Upgrade: websocket\r\n", snoopy);
			else
			{
				if (p.keepAlive)
				{
					if (decision != ProxyResponseDecision.ContentLength && decision != ProxyResponseDecision.NoBody)
					{
						// No content-length was provided, and there is assumed to be a response body.  We must use "Transfer-Encoding: chunked" for our response.
						_ProxyString(ProxyDataDirection.ResponseFromServer, outgoingStream, "Transfer-Encoding: chunked\r\n", snoopy);
						wrapOutputStreamChunked = true;
					}
				}
			}

			options.RaiseBeforeResponseHeadersSent(this, proxyHttpHeaders);

			foreach (HttpHeader header in proxyHttpHeaders)
			{
				if (doNotProxyHeaders.Contains(header.Key, true))
					continue;
				_ProxyString(ProxyDataDirection.ResponseFromServer, outgoingStream, header.Key + ": " + header.Value + "\r\n", snoopy);
			}

			// Write blank line to indicate the end of the response headers
			_ProxyString(ProxyDataDirection.ResponseFromServer, outgoingStream, "\r\n", snoopy);

			// RESPONSE HEADERS ARE WRITTEN

			if (wrapOutputStreamChunked)
			{
				outgoingStream = new WritableChunkedTransferEncodingStream(outgoingStream);
				options.log.AppendLine("Response will be written with \"Transfer-Encoding: chunked\".");
			}

			// Handle the rest of the response based on the decision made earlier.
			if (decision == ProxyResponseDecision.ContentLength)
			{
				await CopyNBytesAsync(proxyContentLength, ProxyDataDirection.ResponseFromServer, proxyStream, outgoingStream, snoopy);
			}
			else if (decision == ProxyResponseDecision.ReadChunked)
			{
				await CopyChunkedResponseAsync(ProxyDataDirection.ResponseFromServer, proxyStream, outgoingStream, snoopy);
			}
			else if (decision == ProxyResponseDecision.UntilClosed)
			{
				await ProxyResponseToClientAsync(proxyStream, outgoingStream, snoopy);
			}
			else if (decision == ProxyResponseDecision.Websocket)
			{
				// Proxy response to client asynchronously (do not await)
				_ = ProxyResponseToClientAsync(proxyStream, outgoingStream, snoopy);

				// The current thread will handle additional incoming data from our client and proxy it to the remote server.
				await CopyStreamUntilClosedAsync(ProxyDataDirection.RequestToServer, outgoingStream, proxyStream, snoopy);
			}
			else if (decision == ProxyResponseDecision.NoBody)
			{
				// All done!
			}

			if (outgoingStream is WritableChunkedTransferEncodingStream)
				await (outgoingStream as WritableChunkedTransferEncodingStream).WriteFinalChunkAsync(new CancellationToken());

			//////////////////////
			// PHASE 6: CLEANUP //
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
			lastRequestDetails = p.TrueRemoteIPAddress + " -> " + p.hostName + " " + requestLine;
			return new ProxyResult(ProxyResultErrorCode.Success, null, remoteServerWantsKeepalive && options.allowConnectionKeepalive, false);
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
				IPAddress ip = await DnsHelper.GetHostAddressAsync(uri.DnsSafeHost);

				proxyClient = new TcpClient(ip.AddressFamily); // Create TcpClient with IPv4 or IPv6 support as needed. We are unable to construct one that can use both.
				proxyClient.NoDelay = true;

				try
				{
					proxyTiming?.Start("Connect");
					options.bet?.Start("Connect");
					await proxyClient.ConnectAsync(ip, uri.Port);
					proxyStream = proxyClient.GetStream();
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
						System.Net.Security.RemoteCertificateValidationCallback certCallback = null;
						if (options.acceptAnyCert)
							certCallback = (sender, certificate, chain, sslPolicyErrors) => true;
						proxyStream = new System.Net.Security.SslStream(proxyStream, false, certCallback, null);
						((System.Net.Security.SslStream)proxyStream).AuthenticateAsClient(host, null, HttpProcessor.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls, false);
					}
				}
				catch (Exception ex)
				{
					throw new TLSNegotiationFailedException("TLS Negotiation failed with remote host: " + uri.DnsSafeHost + ":" + uri.Port, ex);
				}
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
					p.httpHeaders.Remove(XFF);
					break;
				case ProxyHeaderBehavior.Create:
					p.httpHeaders[XFF] = p.TrueRemoteIPAddress.ToString();
					break;
				case ProxyHeaderBehavior.CombineUnsafe:
					p.httpHeaders.Add(XFF, p.TrueRemoteIPAddress.ToString());
					break;
				case ProxyHeaderBehavior.CombineIfTrustedElseCreate:
					if (IPAddressRange.WhitelistCheck(p.TrueRemoteIPAddress, options.proxyHeaderTrustedIpRanges))
						p.httpHeaders.Add(XFF, p.TrueRemoteIPAddress.ToString());
					else
						p.httpHeaders[XFF] = p.TrueRemoteIPAddress.ToString();
					break;
				case ProxyHeaderBehavior.PassthroughUnsafe:
					break;
				case ProxyHeaderBehavior.PassthroughIfTrustedElseDrop:
					if (!IPAddressRange.WhitelistCheck(p.TrueRemoteIPAddress, options.proxyHeaderTrustedIpRanges))
						p.httpHeaders.Remove(XFF);
					break;
				case ProxyHeaderBehavior.PassthroughIfTrustedElseCreate:
					if (!IPAddressRange.WhitelistCheck(p.TrueRemoteIPAddress, options.proxyHeaderTrustedIpRanges))
						p.httpHeaders[XFF] = p.TrueRemoteIPAddress.ToString();
					break;
				default:
					throw new Exception("Unhandled options.xForwardedFor: " + options.xForwardedFor);
			}

			// X-Forwarded-Proto
			switch (options.xForwardedProto)
			{
				case ProxyHeaderBehavior.Drop:
					p.httpHeaders.Remove(XFP);
					break;
				case ProxyHeaderBehavior.Create:
				case ProxyHeaderBehavior.CombineUnsafe:
				case ProxyHeaderBehavior.CombineIfTrustedElseCreate:
					p.httpHeaders[XFP] = p.secure_https ? "https" : "http";
					break;
				case ProxyHeaderBehavior.PassthroughUnsafe:
					break;
				case ProxyHeaderBehavior.PassthroughIfTrustedElseDrop:
					if (!IPAddressRange.WhitelistCheck(p.TrueRemoteIPAddress, options.proxyHeaderTrustedIpRanges))
						p.httpHeaders.Remove(XFP);
					break;
				case ProxyHeaderBehavior.PassthroughIfTrustedElseCreate:
					if (!IPAddressRange.WhitelistCheck(p.TrueRemoteIPAddress, options.proxyHeaderTrustedIpRanges))
						p.httpHeaders[XFP] = p.secure_https ? "https" : "http";
					break;
				default:
					throw new Exception("Unhandled options.xForwardedProto: " + options.xForwardedProto);
			}

			// X-Forwarded-Host
			switch (options.xForwardedHost)
			{
				case ProxyHeaderBehavior.Drop:
					p.httpHeaders.Remove(XFH);
					break;
				case ProxyHeaderBehavior.Create:
				case ProxyHeaderBehavior.CombineUnsafe:
				case ProxyHeaderBehavior.CombineIfTrustedElseCreate:
					p.httpHeaders.Set(XFH, p.httpHeaders.Get("Host") ?? "undefined");
					break;
				case ProxyHeaderBehavior.PassthroughUnsafe:
					break;
				case ProxyHeaderBehavior.PassthroughIfTrustedElseDrop:
					if (!IPAddressRange.WhitelistCheck(p.TrueRemoteIPAddress, options.proxyHeaderTrustedIpRanges))
						p.httpHeaders.Remove(XFH);
					break;
				case ProxyHeaderBehavior.PassthroughIfTrustedElseCreate:
					if (!IPAddressRange.WhitelistCheck(p.TrueRemoteIPAddress, options.proxyHeaderTrustedIpRanges))
						p.httpHeaders.Set(XFH, p.httpHeaders.Get("Host") ?? "undefined");
					break;
				default:
					throw new Exception("Unhandled options.xForwardedHost: " + options.xForwardedHost);
			}

			// X-Real-IP
			switch (options.xRealIp)
			{
				case ProxyHeaderBehavior.Drop:
					p.httpHeaders.Remove(XRI);
					break;
				case ProxyHeaderBehavior.Create:
				case ProxyHeaderBehavior.CombineUnsafe:
				case ProxyHeaderBehavior.CombineIfTrustedElseCreate:
					p.httpHeaders[XRI] = p.TrueRemoteIPAddress.ToString();
					break;
				case ProxyHeaderBehavior.PassthroughUnsafe:
					break;
				case ProxyHeaderBehavior.PassthroughIfTrustedElseDrop:
					if (!IPAddressRange.WhitelistCheck(p.TrueRemoteIPAddress, options.proxyHeaderTrustedIpRanges))
						p.httpHeaders.Remove(XRI);
					break;
				case ProxyHeaderBehavior.PassthroughIfTrustedElseCreate:
					if (!IPAddressRange.WhitelistCheck(p.TrueRemoteIPAddress, options.proxyHeaderTrustedIpRanges))
						p.httpHeaders[XRI] = p.TrueRemoteIPAddress.ToString();
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
		private static void _ProxyString(ProxyDataDirection Direction, Stream target, string str, ProxyDataBuffer snoopy)
		{
			if (snoopy != null)
				snoopy.AddItem(new ProxyDataItem(Direction, str));
			byte[] buf = ByteUtil.Utf8NoBOM.GetBytes(str);
			target.Write(buf, 0, buf.Length);
		}
		private static void _ProxyData(ProxyDataDirection Direction, Stream target, byte[] buf, int length, ProxyDataBuffer snoopy)
		{
			if (snoopy != null)
			{
				ProxyDataItem item;
				if (buf.Length != length)
					item = new ProxyDataItem(Direction, ByteUtil.SubArray(buf, 0, length));
				else
					item = new ProxyDataItem(Direction, (byte[])buf.Clone());
				snoopy?.AddItem(item);
			}
			target.Write(buf, 0, length);
		}
		private static async Task _ProxyStringAsync(ProxyDataDirection Direction, Stream target, string str, ProxyDataBuffer snoopy)
		{
			if (snoopy != null)
				snoopy.AddItem(new ProxyDataItem(Direction, str));
			byte[] buf = ByteUtil.Utf8NoBOM.GetBytes(str);
			await target.WriteAsync(buf, 0, buf.Length).ConfigureAwait(false);
		}
		private static async Task _ProxyDataAsync(ProxyDataDirection Direction, Stream target, byte[] buf, int length, ProxyDataBuffer snoopy)
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
			await target.WriteAsync(buf, 0, length).ConfigureAwait(false);
		}

		private static void ProxyResponseToClient(Stream serverStream, Stream clientStream, ProxyDataBuffer snoopy)
		{
			try
			{
				CopyStreamUntilClosed(ProxyDataDirection.ResponseFromServer, serverStream, clientStream, snoopy);
			}
			catch (Exception ex)
			{
				if (HttpProcessor.IsOrdinaryDisconnectException(ex))
					return;
				SimpleHttpLogger.LogVerbose(ex);
			}
		}

		private static async Task ProxyResponseToClientAsync(Stream serverStream, Stream clientStream, ProxyDataBuffer snoopy)
		{
			try
			{
				await CopyStreamUntilClosedAsync(ProxyDataDirection.ResponseFromServer, serverStream, clientStream, snoopy).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				if (HttpProcessor.IsOrdinaryDisconnectException(ex) || ex.GetExceptionOfType<ObjectDisposedException>() != null)
					return;
				SimpleHttpLogger.LogVerbose(ex);
			}
		}
		private static void CopyStreamUntilClosed(ProxyDataDirection Direction, Stream source, Stream target, ProxyDataBuffer snoopy)
		{
			byte[] buf = ByteUtil.BufferGet();
			try
			{
				int read = 1;
				while (read > 0)
				{
					read = source.Read(buf, 0, buf.Length);
					if (read > 0)
						_ProxyData(Direction, target, buf, read, snoopy);
				}
			}
			finally
			{
				ByteUtil.BufferRecycle(buf);
			}
		}
		private static async Task CopyStreamUntilClosedAsync(ProxyDataDirection Direction, Stream source, Stream target, ProxyDataBuffer snoopy)
		{
			byte[] buf = ByteUtil.BufferGet();
			try
			{
				int read = 1;
				while (read > 0)
				{
					read = await source.ReadAsync(buf, 0, buf.Length).ConfigureAwait(false);
					if (read > 0)
						await _ProxyDataAsync(Direction, target, buf, read, snoopy).ConfigureAwait(false);
				}
			}
			finally
			{
				ByteUtil.BufferRecycle(buf);
			}
		}
		private static long CopyNBytes(long N, ProxyDataDirection Direction, Stream source, Stream target, ProxyDataBuffer snoopy)
		{
			long totalProxied = 0;
			byte[] buf = ByteUtil.BufferGet();
			try
			{
				int read = 1;
				while (read > 0 && totalProxied < N)
				{
					read = source.Read(buf, 0, (int)Math.Min(N - totalProxied, buf.Length));
					if (read > 0)
						_ProxyData(Direction, target, buf, read, snoopy);
					totalProxied += read;
				}
				return totalProxied;
			}
			finally
			{
				ByteUtil.BufferRecycle(buf);
			}
		}
		private static async Task<long> CopyNBytesAsync(long N, ProxyDataDirection Direction, Stream source, Stream target, ProxyDataBuffer snoopy)
		{
			long totalProxied = 0;
			byte[] buf = ByteUtil.BufferGet();
			try
			{
				int read = 1;
				while (read > 0 && totalProxied < N)
				{
					read = await source.ReadAsync(buf, 0, (int)Math.Min(N - totalProxied, buf.Length)).ConfigureAwait(false);
					if (read > 0)
						await _ProxyDataAsync(Direction, target, buf, read, snoopy);
					totalProxied += read;
				}
				return totalProxied;
			}
			finally
			{
				ByteUtil.BufferRecycle(buf);
			}
		}
		private static void CopyChunkedResponse(ProxyDataDirection Direction, Stream source, Stream target, ProxyDataBuffer snoopy)
		{
			byte[] buf = ByteUtil.BufferGet();
			try
			{
				int bytesRead;
				while (true)
				{
					// Read chunk size
					string chunkSizeLine = HttpProcessor.streamReadLine(source);
					if (chunkSizeLine == null)
						break;

					int chunkSize = int.Parse(chunkSizeLine.Split(';')[0], System.Globalization.NumberStyles.HexNumber);

					// Writing of the chunk header/trailer is now handled by a ChunkedTransferEncodingStream.
					// Write chunk size
					//byte[] chunkSizeBytes = Encoding.ASCII.GetBytes(chunkSizeLine + "\r\n");
					//_ProxyData(ProxyDataDirection.ResponseFromServer, target, chunkSizeBytes, chunkSizeBytes.Length, snoopy);

					// Copy chunk data
					int bytesRemaining = chunkSize;
					while (bytesRemaining > 0)
					{
						bytesRead = source.Read(buf, 0, Math.Min(buf.Length, bytesRemaining));
						if (bytesRead == 0)
							throw new EndOfStreamException();

						_ProxyData(ProxyDataDirection.ResponseFromServer, target, buf, bytesRead, snoopy);
						bytesRemaining -= bytesRead;
					}

					// Read and write chunk trailer
					string trailerLine = HttpProcessor.streamReadLine(source);
					if (trailerLine != "")
						throw new InvalidDataException();

					// Writing of the chunk header/trailer is now handled by a ChunkedTransferEncodingStream.
					//byte[] trailerBytes = Encoding.ASCII.GetBytes("\r\n");
					//_ProxyData(ProxyDataDirection.ResponseFromServer, target, trailerBytes, trailerBytes.Length, snoopy);

					if (chunkSize == 0)
						break;
				}
			}
			finally
			{
				ByteUtil.BufferRecycle(buf);
			}
		}
		private static async Task CopyChunkedResponseAsync(ProxyDataDirection Direction, Stream source, Stream target, ProxyDataBuffer snoopy)
		{
			byte[] buf = ByteUtil.BufferGet();
			try
			{
				int bytesRead;
				while (true)
				{
					// Read chunk size
					string chunkSizeLine = HttpProcessor.streamReadLine(source);
					if (chunkSizeLine == null)
						break;

					int chunkSize = int.Parse(chunkSizeLine.Split(';')[0], System.Globalization.NumberStyles.HexNumber);

					// Writing of the chunk header/trailer is now handled by a ChunkedTransferEncodingStream.
					// Write chunk size
					//byte[] chunkSizeBytes = Encoding.ASCII.GetBytes(chunkSizeLine + "\r\n");
					//await _ProxyDataAsync(ProxyDataDirection.ResponseFromServer, target, chunkSizeBytes, chunkSizeBytes.Length, snoopy).ConfigureAwait(false);

					// Copy chunk data
					int bytesRemaining = chunkSize;
					while (bytesRemaining > 0)
					{
						bytesRead = await source.ReadAsync(buf, 0, Math.Min(buf.Length, bytesRemaining)).ConfigureAwait(false);
						if (bytesRead == 0)
							throw new EndOfStreamException();

						await _ProxyDataAsync(ProxyDataDirection.ResponseFromServer, target, buf, bytesRead, snoopy).ConfigureAwait(false);
						bytesRemaining -= bytesRead;
					}

					// Read and write chunk trailer
					string trailerLine = HttpProcessor.streamReadLine(source);
					if (trailerLine != "")
						throw new InvalidDataException();

					// Writing of the chunk header/trailer is now handled by a ChunkedTransferEncodingStream.
					//byte[] trailerBytes = Encoding.ASCII.GetBytes("\r\n");
					//await _ProxyDataAsync(ProxyDataDirection.ResponseFromServer, target, trailerBytes, trailerBytes.Length, snoopy).ConfigureAwait(false);

					if (chunkSize == 0)
						break;
				}
			}
			finally
			{
				ByteUtil.BufferRecycle(buf);
			}
		}

		[Serializable]
		private class GatewayTimeoutException : Exception
		{
			public GatewayTimeoutException()
			{
			}

			public GatewayTimeoutException(string message) : base(message)
			{
			}

			public GatewayTimeoutException(string message, Exception innerException) : base(message, innerException)
			{
			}

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
			/// Status text, e.g. "OK" or "Not Found".
			/// </summary>
			public string StatusText;
			/// <summary>
			/// Original string sent into the constructor.
			/// </summary>
			public string OriginalStatusLine;
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
		#endregion
	}
}
