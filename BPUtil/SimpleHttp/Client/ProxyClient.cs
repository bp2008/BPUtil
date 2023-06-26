using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp.Client
{
	/// <summary>
	/// Provides advanced web proxying capability.  This class is very primitive and appears to struggle with effectively using connections that were kept alive, usually failing and needing to retry the request.  The speed/latency of the proxy is also questionable, as several hundred milliseconds of overhead are noticed.
	/// </summary>
	public class ProxyClient
	{
		/// <summary>
		/// Web origin which this client is bound to, all lower case. e.g. "https://example.com"
		/// </summary>
		public readonly string Origin;
		private TcpClient proxyClient;
		private Stream proxyStream;
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
		/// <param name="networkTimeoutMs">The send and receive timeout to set for both TcpClients, in milliseconds.</param>
		/// <param name="host">String to be used for TLS server name indication and the Host header. If null or whitespace, this is automatically populated from the [uri].</param>
		/// <param name="acceptAnyCert">If true, certificate validation will be disabled for outgoing https connections.</param>
		/// <param name="snoopy">If non-null, proxied communication will be copied into this object so you can snoop on it.</param>
		/// <param name="bet">Optional event timer for collecting timing data.</param>
		/// <returns></returns>
		public static async Task ProxyRequest(HttpProcessor p, Uri uri, int networkTimeoutMs = 60000, string host = null, bool acceptAnyCert = false, ProxyDataBuffer snoopy = null, BasicEventTimer bet = null)
		{
			bet?.Start("ProxyRequest Start");
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
					bet?.Start("ProxyRequest BeginProxyRequestAsync #" + numTries + " with new client");
				}
				else
				{
					Logger.Info("Got ProxyClient from pool");
					bet?.Start("ProxyRequest BeginProxyRequestAsync #" + numTries + " with existing client");
				}

				ProxyResult result = await client.BeginProxyRequestAsync(p, uri, networkTimeoutMs, host, acceptAnyCert, snoopy, bet);
				bet?.Start("ProxyRequest analyze result #" + numTries + ": " + result.ErrorCode);

				if (result.IsProxyClientReusable)
				{
					pool.Enqueue(client);
					Logger.Info("Recycling ProxyClient for future use.");
				}
				if (result.Success)
					break;
				else
				{
					if (!result.ShouldTryAgainWithAnotherConnection)
					{
						Logger.Info("ProxyRequest to uri \"" + uri.ToString() + "\" failed with error: " + result.ErrorMessage);
						break;
					}
				}
			}
			bet?.Start("ProxyRequest Finish Up");
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
		/// Serves as a reverse proxy, sending the request to the given uri.
		/// </summary>
		/// <param name="p">HttpProcessor instance that is requesting the proxied connection.</param>
		/// <param name="uri">The Uri which the request should be forwarded to.</param>
		/// <param name="networkTimeoutMs">The send and receive timeout to set for both TcpClients, in milliseconds.</param>
		/// <param name="host">String to be used for TLS server name indication and the Host header. If null or whitespace, this is automatically populated from the [uri].</param>
		/// <param name="acceptAnyCert">If true, certificate validation will be disabled for outgoing https connections.</param>
		/// <param name="snoopy">If non-null, proxied communication will be copied into this object so you can snoop on it.</param>
		/// <param name="bet">Optional event timer for collecting timing data.</param>
		/// <returns></returns>
		public async Task<ProxyResult> BeginProxyRequestAsync(HttpProcessor p, Uri uri, int networkTimeoutMs = 60000, string host = null, bool acceptAnyCert = false, ProxyDataBuffer snoopy = null, BasicEventTimer bet = null)
		{
			// Interally, we shall refer to the original client making the request to this application as "the client", while the destination URI points at "the server".

			bet?.Start("BeginProxyRequestAsync Init");
			
			if (p.responseWritten)
				throw new ApplicationException("This ProxyClient is unable to complete the current request because a response has already been written to the client making this request.");

			string origin = GetOriginLower(uri);
			if (Origin != origin)
				throw new ApplicationException("This ProxyClient is bound to a different web origin than the requested Uri.");

			if (string.IsNullOrWhiteSpace(host))
				host = uri.DnsSafeHost;

			bool mustConnect;
			if (proxyClient == null)
			{
				Logger.Info("Constructing new TcpClient");
				proxyClient = new TcpClient();
				mustConnect = true;
			}
			else
			{
				Logger.Info("Reusing old connection");
				mustConnect = false;
			}

			proxyClient.ReceiveTimeout = networkTimeoutMs;
			proxyClient.SendTimeout = networkTimeoutMs;

			if (mustConnect)
			{
				try
				{
					bet?.Start("BeginProxyRequestAsync Connect");
					await proxyClient.ConnectAsync(uri.DnsSafeHost, uri.Port);
					proxyStream = proxyClient.GetStream();
				}
				catch (Exception ex)
				{
					p.writeFullResponseUTF8("", "text/plain; charset=utf-8", "504 Gateway Timeout");
					return new ProxyResult(ProxyResultErrorCode.GatewayTimeout, "ProxyClient failed to connect to the remote host: " + uri.DnsSafeHost + ":" + uri.Port + "\r\n" + ex.ToHierarchicalString(), false, false);
				}
				try
				{
					if (uri.Scheme.IEquals("https"))
					{
						bet?.Start("BeginProxyRequestAsync TLS Negotiate");
						System.Net.Security.RemoteCertificateValidationCallback certCallback = null;
						if (acceptAnyCert)
							certCallback = (sender, certificate, chain, sslPolicyErrors) => true;
						proxyStream = new System.Net.Security.SslStream(proxyStream, false, certCallback, null);
						((System.Net.Security.SslStream)proxyStream).AuthenticateAsClient(host, null, System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls11 | System.Security.Authentication.SslProtocols.Tls, false);
					}
				}
				catch (Exception ex)
				{
					p.writeFullResponseUTF8("", "text/plain; charset=utf-8", "502 Bad Gateway");
					return new ProxyResult(ProxyResultErrorCode.TLSNegotiationError, "ProxyClient failed to negotiate TLS with the remote host: " + uri.DnsSafeHost + ":" + uri.Port + "\r\n" + ex.ToHierarchicalString(), false, false);
				}
			}
			bet?.Start("BeginProxyRequestAsync Process Request Headers");

			p.tcpClient.ReceiveTimeout = networkTimeoutMs;
			p.tcpClient.SendTimeout = networkTimeoutMs;

			// Connection to remote server is now established and ready for data transfer.
			// Send the original client's request to the remote server with any necessary modifications.

			string requestHeader_Connection = p.GetHeaderValue("connection");
			bool ourClientWantsConnectionClose = "close".IEquals(requestHeader_Connection);
			if (ourClientWantsConnectionClose)
				requestHeader_Connection = "keep-alive"; // Our client requested connection: close, but we we'll use keep-alive for our own proxy connection.

			string requestHeader_Host = p.GetHeaderValue("host");

			// Send the first line of our HTTP request.
			bet?.Start("BeginProxyRequestAsync Transmit Proxy Request");
			string requestLine = p.http_method + ' ' + uri.PathAndQuery + ' ' + p.http_protocol_versionstring;
			await _ProxyString(ProxyDataDirection.RequestToServer, proxyStream, requestLine + "\r\n", snoopy);
			// After the first line comes the request headers.
			await _ProxyString(ProxyDataDirection.RequestToServer, proxyStream, "Host: " + host + "\r\n", snoopy);
			if (requestHeader_Connection != null)
				await _ProxyString(ProxyDataDirection.RequestToServer, proxyStream, "Connection: " + requestHeader_Connection + "\r\n", snoopy);
			foreach (KeyValuePair<string, string> header in p.httpHeadersRaw)
			{
				if (!header.Key.IEquals("host") && !header.Key.IEquals("connection"))
					await _ProxyString(ProxyDataDirection.RequestToServer, proxyStream, header.Key + ": " + header.Value + "\r\n", snoopy);
			}
			// Now a blank line to indicate the end of the request headers.
			await _ProxyString(ProxyDataDirection.RequestToServer, proxyStream, "\r\n", snoopy);

			// Write the original POST body if there was one.
			if (p.PostBodyStream != null)
			{
				bet?.Start("BeginProxyRequestAsync Transmit POST body");
				long remember_position = p.PostBodyStream.Position;
				p.PostBodyStream.Seek(0, SeekOrigin.Begin);
				byte[] buf = p.PostBodyStream.ToArray();
				await _ProxyData(ProxyDataDirection.RequestToServer, proxyStream, buf, buf.Length, snoopy);
				p.PostBodyStream.Seek(remember_position, SeekOrigin.Begin);
			}

			// Read response from remote server
			string responseStatusLine = null;
			Dictionary<string, string> proxyHttpHeaders = new Dictionary<string, string>();
			Dictionary<string, string> proxyHttpHeadersRaw = new Dictionary<string, string>();
			try
			{
				bet?.Start("BeginProxyRequestAsync Read Status Line From Remote Server");
				responseStatusLine = HttpProcessor.streamReadLine(proxyStream);

				// Read response headers from remote server
				bet?.Start("BeginProxyRequestAsync Read Response Headers From Remote Server");
				HttpProcessor.readHeaders(proxyStream, proxyHttpHeaders, proxyHttpHeadersRaw);
			}
			catch (HttpProcessor.EndOfStreamException ex)
			{
				if (!mustConnect)
					return new ProxyResult(ProxyResultErrorCode.GatewayTimeout, "ProxyClient encountered unexpected end of stream while reading the response from the remote server. Should retry with a new connection.", false, true);
				System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).Throw();
			}

			// Decide how to respond
			bet?.Start("BeginProxyRequestAsync Process Response Headers");
			ProxyResponseDecision decision = ProxyResponseDecision.Undefined;
			long proxyContentLength = 0;
			bool remoteServerWantsKeepalive = false;
			if (proxyHttpHeaders.TryGetValue("connection", out string proxyConnectionHeader))
			{
				if (proxyConnectionHeader.IEquals("keep-alive"))
				{
					remoteServerWantsKeepalive = true;
					// keep-alive responses must either specify a "Content-Length" or use "Transfer-Encoding: chunked".
					if (proxyHttpHeaders.TryGetValue("content-length", out string proxyContentLengthStr) && long.TryParse(proxyContentLengthStr, out proxyContentLength) && proxyContentLength > -1)
					{
						// Content-Length
						decision = ProxyResponseDecision.ContentLength;
					}
					else if (proxyHttpHeaders.TryGetValue("transfer-encoding", out string proxyTransferEncoding) && proxyTransferEncoding.IEquals("chunked"))
					{
						// Transfer-Encoding: chunked
						decision = ProxyResponseDecision.Chunked;
					}
					else
					{
						p.writeFullResponseUTF8("", "text/plain; charset=utf-8", "502 Bad Gateway");
						return new ProxyResult(ProxyResultErrorCode.BadGateway, "ProxyTo can't process this request because the remote server specified \"Connection: keepalive\" without specifying a \"Content-Length\" or using \"Transfer-Encoding: chunked\". The request was: " + requestLine, false, false);
					}
				}
				else if (proxyConnectionHeader.IEquals("upgrade") && proxyHttpHeaders.TryGetValue("upgrade", out string proxyUpgradeHeader) && proxyUpgradeHeader.IEquals("websocket"))
					decision = ProxyResponseDecision.Websocket;
				else
					decision = ProxyResponseDecision.UntilClosed;
			}
			else
			{
				p.writeFullResponseUTF8("", "text/plain; charset=utf-8", "502 Bad Gateway");
				return new ProxyResult(ProxyResultErrorCode.BadGateway, "ProxyTo can't process this request because the remote server did not specify a \"Connection\" header. The request was: " + requestLine, false, false);
			}

			// Write response status line
			bet?.Start("BeginProxyRequestAsync Write Response: " + decision);
			p.responseWritten = true;
			await _ProxyString(ProxyDataDirection.ResponseFromServer, p.tcpStream, responseStatusLine + "\r\n", snoopy);

			// Write response headers
			if (ourClientWantsConnectionClose && !"close".IEquals(proxyConnectionHeader))
				proxyConnectionHeader = "close";
			else if (!ourClientWantsConnectionClose && "close".IEquals(proxyConnectionHeader))
				proxyConnectionHeader = "keep-alive";
			if (proxyConnectionHeader != null)
				await _ProxyString(ProxyDataDirection.ResponseFromServer, p.tcpStream, "Connection: " + proxyConnectionHeader + "\r\n", snoopy);

			foreach (KeyValuePair<string, string> header in proxyHttpHeadersRaw)
			{
				string key = header.Key;
				string value = header.Value;
				if (key.IEquals("connection"))
					continue;
				if (key.IEquals("location"))
				{
					if (!string.IsNullOrWhiteSpace(requestHeader_Host)
						&& Uri.TryCreate(value, UriKind.Absolute, out Uri redirectUri)
						&& redirectUri.Host.IEquals(host))
					{
						UriBuilder uriBuilder = new UriBuilder(redirectUri);
						uriBuilder.Host = requestHeader_Host;
						value = uriBuilder.Uri.ToString();
					}
				}
				await _ProxyString(ProxyDataDirection.ResponseFromServer, p.tcpStream, key + ": " + value + "\r\n", snoopy);
			}

			// Write blank line to indicate the end of the response headers
			await _ProxyString(ProxyDataDirection.ResponseFromServer, p.tcpStream, "\r\n", snoopy);

			// Handle the rest of the response based on the decision made earlier.
			if (decision == ProxyResponseDecision.ContentLength)
				await CopyNBytes(proxyContentLength, ProxyDataDirection.ResponseFromServer, proxyStream, p.tcpStream, snoopy);
			else if (decision == ProxyResponseDecision.Chunked)
				await CopyChunkedResponse(ProxyDataDirection.ResponseFromServer, proxyStream, p.tcpStream, snoopy);
			else if (decision == ProxyResponseDecision.UntilClosed)
				await ProxyResponseToClient(proxyStream, p.tcpStream, snoopy);
			else if (decision == ProxyResponseDecision.Websocket)
			{
				// Proxy response to client asynchronously (do not await)
				_ = ProxyResponseToClient(proxyStream, p.tcpStream, snoopy);

				// The current thread will handle additional incoming data from our client and proxy it to the remote server.
				await CopyStreamUntilClosed(ProxyDataDirection.RequestToServer, p.tcpStream, proxyStream, snoopy);
			}
			bet?.Start("BeginProxyRequestAsync Return result");
			return new ProxyResult(ProxyResultErrorCode.Success, null, remoteServerWantsKeepalive, false);
		}
		#region Helpers
		private enum ProxyResponseDecision
		{
			Undefined, ContentLength, Chunked, UntilClosed, Websocket
		}
		private static async Task _ProxyString(ProxyDataDirection Direction, Stream target, string str, ProxyDataBuffer snoopy)
		{
			if (snoopy != null)
				snoopy.AddItem(new ProxyDataItem(Direction, str));
			byte[] buf = ByteUtil.Utf8NoBOM.GetBytes(str);
			await target.WriteAsync(buf, 0, buf.Length).ConfigureAwait(false);
		}
		private static async Task _ProxyData(ProxyDataDirection Direction, Stream target, byte[] buf, int length, ProxyDataBuffer snoopy)
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

		private static async Task ProxyResponseToClient(Stream serverStream, Stream clientStream, ProxyDataBuffer snoopy)
		{
			try
			{
				await CopyStreamUntilClosed(ProxyDataDirection.ResponseFromServer, serverStream, clientStream, snoopy).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				if (HttpProcessor.IsOrdinaryDisconnectException(ex))
					return;
				SimpleHttpLogger.LogVerbose(ex);
			}
		}
		private static async Task CopyStreamUntilClosed(ProxyDataDirection Direction, Stream source, Stream target, ProxyDataBuffer snoopy)
		{
			byte[] buf = new byte[16000];
			int read = 1;
			while (read > 0)
			{
				read = await source.ReadAsync(buf, 0, buf.Length).ConfigureAwait(false);
				if (read > 0)
					await _ProxyData(Direction, target, buf, read, snoopy).ConfigureAwait(false);
			}
		}
		private static async Task<long> CopyNBytes(long N, ProxyDataDirection Direction, Stream source, Stream target, ProxyDataBuffer snoopy)
		{
			long totalProxied = 0;
			byte[] buf = new byte[16000];
			int read = 1;
			while (read > 0 && totalProxied < N)
			{
				read = await source.ReadAsync(buf, 0, (int)Math.Min(N - totalProxied, buf.Length)).ConfigureAwait(false);
				if (read > 0)
					await _ProxyData(Direction, target, buf, read, snoopy).ConfigureAwait(false);
				totalProxied += read;
			}
			return totalProxied;
		}
		private static async Task CopyChunkedResponse(ProxyDataDirection Direction, Stream source, Stream target, ProxyDataBuffer snoopy)
		{
			byte[] buf = new byte[8192];
			int bytesRead;
			while (true)
			{
				// Read chunk size
				string chunkSizeLine = HttpProcessor.streamReadLine(source);
				if (chunkSizeLine == null)
					break;

				int chunkSize = int.Parse(chunkSizeLine.Split(';')[0], System.Globalization.NumberStyles.HexNumber);

				// Write chunk size
				byte[] chunkSizeBytes = Encoding.ASCII.GetBytes(chunkSizeLine + "\r\n");
				await _ProxyData(ProxyDataDirection.ResponseFromServer, target, chunkSizeBytes, chunkSizeBytes.Length, snoopy).ConfigureAwait(false);

				// Copy chunk data
				int bytesRemaining = chunkSize;
				while (bytesRemaining > 0)
				{
					bytesRead = await source.ReadAsync(buf, 0, Math.Min(buf.Length, bytesRemaining)).ConfigureAwait(false);
					if (bytesRead == 0)
						throw new EndOfStreamException();

					await _ProxyData(ProxyDataDirection.ResponseFromServer, target, buf, bytesRead, snoopy).ConfigureAwait(false);
					bytesRemaining -= bytesRead;
				}

				// Read and write chunk trailer
				string trailerLine = HttpProcessor.streamReadLine(source);
				if (trailerLine != "")
					throw new InvalidDataException();

				byte[] trailerBytes = Encoding.ASCII.GetBytes("\r\n");
				await _ProxyData(ProxyDataDirection.ResponseFromServer, target, trailerBytes, trailerBytes.Length, snoopy).ConfigureAwait(false);

				if (chunkSize == 0)
					break;
			}
		}
		#endregion
	}
}
