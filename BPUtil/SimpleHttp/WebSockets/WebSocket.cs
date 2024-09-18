using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp.WebSockets
{
	/// <summary>
	/// A WebSocket server connection providing synchronous access methods. It is recommended to adjust the Tcp Socket's read and write timeouts as needed to avoid premature disconnection. Also set <see cref="MAX_PAYLOAD_BYTES"/> if you expect to receive larger single payloads than 20,000,000 bytes.
	/// </summary>
	public class WebSocket
	{
		/// <summary>
		/// The maximum size of a payload this WebSocket will allow to be received. Any payloads exceeding this size will cause the WebSocket to be closed.
		/// </summary>
		public static int MAX_PAYLOAD_BYTES = 20000000;

		/// <summary>
		/// The TcpClient instance this WebSocket is bound to. Do not use the GetStream method, as it does not support TLS (instead use <see cref="tcpStream"/>).
		/// </summary>
		public TcpClient tcpClient { get; protected set; }
		/// <summary>
		/// The readable/writeable stream for the data connection.  Typically either NetworkStream or SslStream.
		/// </summary>
		public Stream tcpStream { get; protected set; }
		/// <summary>
		/// Gets or sets the amount of time the underlying <see cref="System.Net.Sockets.TcpClient"/> will wait to receive data once a synchronous/blocking read operation is initiated.
		/// </summary>
		/// <returns>The time-out value of the connection in milliseconds. The default value is 0.</returns>
		public int ReceiveTimeout { get { return tcpClient.ReceiveTimeout; } set { tcpClient.ReceiveTimeout = value; } }
		/// <summary>
		/// Gets or sets the amount of time the underlying <see cref="System.Net.Sockets.TcpClient"/> will wait for a synchronous/blocking send operation to complete successfully.
		/// </summary>
		/// <returns>The send time-out value, in milliseconds. The default is 0.</returns>
		public int SendTimeout { get { return tcpClient.SendTimeout; } set { tcpClient.SendTimeout = value; } }
		public WebSocketState State = WebSocketState.Connecting;

		protected Thread thrWebSocketRead;
		protected Action<WebSocketFrame> onMessageReceived = delegate { };
		protected Action<WebSocketCloseFrame> onClose = delegate { };
		protected object sendLock = new object();
		protected object startStopLock = new object();
		protected bool handshakePerformed = false;
		protected bool sentCloseFrame = false;
		protected bool receivedCloseFrame = false;
		/// <summary>
		/// When this is true, the WebSocket will silently refuse to send additional data.
		/// </summary>
		protected bool isClosing = false;
		/// <summary>
		/// When this is set to false, the WebSocket's read thread will begin exiting.
		/// </summary>
		private volatile bool expectingMoreMessages = true;

		#region Constructors and Initialization
		/// <summary>
		/// Empty constructor for use by WebSocketClient.
		/// </summary>
		protected WebSocket()
		{
		}
		/// <summary>
		/// Creates a new WebSocket bound to a <see cref="TcpClient"/> that is already connected. It is recommended to adjust the Tcp Socket's read and write timeouts as needed to avoid premature disconnection. If TLS is being used, this constructor will try to bypass it and red/write diretly from the TcpClient's native stream.  This constructor does not complete the WebSocket handshake.  This constructor does not automatically start reading WebSocket frames from the stream. 
		/// </summary>
		/// <param name="tcpc">A connected <see cref="TcpClient"/> to bind to the new WebSocket instance.</param>
		public WebSocket(TcpClient tcpc)
		{
			this.tcpClient = tcpc;
			this.tcpClient.NoDelay = true;
			this.tcpStream = tcpc.GetStream();
		}

		/// <summary>
		/// Creates a new WebSocket bound to an <see cref="HttpProcessor"/> that has already read the request headers.  The WebSocket handshake will be completed synchronously before the constructor returns.  It is recommended to adjust the Tcp Socket's read and write timeouts as needed to avoid premature disconnection. If you use this constructor, you must call <see cref="StartReading"/> yourself otherwise you have no way to receive any data from this WebSocket.
		/// </summary>
		/// <param name="p">An <see cref="HttpProcessor"/> to bind to the new WebSocket instance.</param>
		/// <param name="additionalResponseHeaders">Optional collection of HTTP headers to include in the HTTP response.</param>
		public WebSocket(HttpProcessor p, HttpHeaderCollection additionalResponseHeaders = null) : this(p.tcpClient)
		{
			if (!IsWebSocketRequest(p))
				throw new Exception("Unable to create a WebSocket from a connection that did not request a websocket upgrade.");

			this.tcpStream = p.tcpStream;
			handshakePerformed = true;
			string version = p.Request.Headers.Get("Sec-WebSocket-Version");
			if (version != "13")
			{
				HttpHeaderCollection headers = new HttpHeaderCollection();
				headers.Set("Sec-WebSocket-Version", "13");
				throw new HttpProcessor.HttpProcessorException("400 Bad Request", "An unsupported web socket version was requested (\"" + version + "\").", headers);
			}
			p.Response.WebSocketUpgradeSync(additionalResponseHeaders);
			State = WebSocketState.Open;
		}

		/// <summary>
		/// Creates a new WebSocket bound to an <see cref="HttpProcessor"/>.  The WebSocket handshake will be completed synchronously before the constructor returns.  This constructor calls <see cref="StartReading"/> automatically. It is recommended to adjust the Tcp Socket's read and write timeouts as needed to avoid premature disconnection.
		/// </summary>
		/// <param name="p">An <see cref="HttpProcessor"/> to bind to the new WebSocket instance.</param>
		/// <param name="onMessageReceived">A callback method which is called whenever a message is received from the WebSocket.</param>
		/// <param name="onClose">A callback method which is called when the WebSocket is closed by the remote endpoint.</param>
		/// <param name="additionalResponseHeaders">Optional collection of HTTP headers to include in the HTTP response.</param>
		public WebSocket(HttpProcessor p, Action<WebSocketFrame> onMessageReceived, Action<WebSocketCloseFrame> onClose, HttpHeaderCollection additionalResponseHeaders = null) : this(p, additionalResponseHeaders)
		{
			StartReading(onMessageReceived, onClose);
		}

		/// <summary>
		/// Starts a background thread to read from the web socket. If the WebSocket reading thread is already active, an exception is thrown.
		/// </summary>
		/// <param name="onMessageReceived">A callback method which is called whenever a message is received from the WebSocket.</param>
		/// <param name="onClose">A callback method which is called when the WebSocket is closed by the remote endpoint.</param>
		public void StartReading(Action<WebSocketFrame> onMessageReceived, Action<WebSocketCloseFrame> onClose)
		{
			lock (startStopLock)
			{
				if (isClosing)
					throw new Exception("The WebSocket instance has already started closing.");
				if (!handshakePerformed)
					throw new Exception("The WebSocket handshake has not been performed yet!");
				if (thrWebSocketRead != null)
					throw new Exception("WebSocket reading thread was already active!");

				this.onMessageReceived = onMessageReceived;
				this.onClose = onClose;

				thrWebSocketRead = new Thread(WebSocketRead);
				thrWebSocketRead.Name = "WebSocket Read";
				thrWebSocketRead.IsBackground = true;
				thrWebSocketRead.Start();
			}
		}
		/// <summary>
		/// Instructs the WebSocket to close gracefully.  This method returns as soon as we've sent a Close Frame, but the connection may remain open for several seconds while a background thread waits for a close frame from the remote host.  To use a custom close code, call <see cref="SendCloseFrame(WebSocketCloseCode, string)"/> instead of Close.
		/// </summary>
		/// <param name="closeCode">The reason for the close. Note that some of the <see cref="WebSocketCloseCode"/> values are not intended to be sent.</param>
		/// <param name="message">A message to include in the close frame.  You can assume this message will not be shown to the user.  The message may be truncated to ensure the UTF8-Encoded length is 125 bytes or less.</param>
		public void Close(WebSocketCloseCode closeCode = WebSocketCloseCode.Normal, string message = null)
		{
			SendCloseFrame(closeCode, message);
		}
		/// <summary>
		/// Returns true if this WebSocket is acting as a client.
		/// </summary>
		/// <returns></returns>
		protected virtual bool isClient()
		{
			return false;
		}

		#endregion

		#region Reading Frames
		/// <summary>
		/// This method runs on a background thread and closes the tcp connection before it returns.
		/// </summary>
		private void WebSocketRead()
		{
			WebSocketCloseFrame closeFrame = null;
			try
			{
				WebSocketFrameHeader fragmentStart = null;
				List<byte[]> fragments = new List<byte[]>();
				ulong totalLength = 0;
				while (expectingMoreMessages)
				{
					try
					{
						WebSocketFrameHeader head = new WebSocketFrameHeader(tcpStream, isClient());

						if (head.opcode == WebSocketOpcode.Close)
						{
							isClosing = true;
							expectingMoreMessages = false;
							receivedCloseFrame = true;
							closeFrame = new WebSocketCloseFrame(head, tcpStream);
							//SimpleHttpLogger.LogVerbose("WebSocket connection closed with code: "
							//	+ (ushort)closeFrame.CloseCode
							//	+ " (" + closeFrame.CloseCode + ")"
							//	 + (!string.IsNullOrEmpty(closeFrame.Message) ? " -- \"" + closeFrame.Message + "\"" : ""));
							continue;
						}
						else if (head.opcode == WebSocketOpcode.Ping)
						{
							WebSocketPingFrame pingFrame = new WebSocketPingFrame(head, tcpStream);
							SendFrame(WebSocketOpcode.Pong, pingFrame.Data);
							continue;
						}
						else if (head.opcode == WebSocketOpcode.Pong)
						{
							WebSocketPongFrame pongFrame = new WebSocketPongFrame(head, tcpStream);
							continue;
						}
						else if (head.opcode == WebSocketOpcode.Continuation || head.opcode == WebSocketOpcode.Text || head.opcode == WebSocketOpcode.Binary)
						{
							// The WebSocket protocol supports payload fragmentation, which is the 
							// reason for much of the complexity to follow.
							// (The primary purpose of fragmentation is to allow sending a message
							// that is of unknown size when the message is started without having to
							// buffer that message.)

							// Validate Payload Length
							totalLength += head.payloadLength;
							if (totalLength > (ulong)MAX_PAYLOAD_BYTES)
								throw new WebSocketException(WebSocketCloseCode.MessageTooBig, "Host does not accept payloads larger than " + WebSocket.MAX_PAYLOAD_BYTES + ". Payload length was " + totalLength + ".");

							// Keep track of the frame that started each set of fragments
							if (fragmentStart == null)
							{
								if (head.opcode == WebSocketOpcode.Continuation)
									throw new WebSocketException(WebSocketCloseCode.ProtocolError, "Continuation frame did not follow a Text or Binary frame.");
								fragmentStart = head;
							}

							// Read the Frame's Payload
							fragments.Add(ByteUtil.ReadNBytes(tcpStream, (int)head.payloadLength));

							if (head.fin)
							{
								// This ends a set of 1 or more fragments.

								// Assemble the final payload.
								byte[] payload;
								if (fragments.Count == 1)
									payload = fragments[0];
								else
								{
									// We must assemble a fragmented payload.
									payload = new byte[(int)totalLength];
									int soFar = 0;
									for (int i = 0; i < fragments.Count; i++)
									{
										byte[] part = fragments[i];
										fragments[i] = null;
										Array.Copy(part, 0, payload, soFar, part.Length);
										soFar += part.Length;
									}
								}

								// Call onMessageReceived callback
								try
								{
									if (fragmentStart.opcode == WebSocketOpcode.Text)
										onMessageReceived(new WebSocketTextFrame(fragmentStart, payload));
									else
										onMessageReceived(new WebSocketBinaryFrame(fragmentStart, payload));
								}
								catch (Exception ex)
								{
									if (!HttpProcessor.IsOrdinaryDisconnectException(ex))
										SimpleHttpLogger.Log(ex);
								}

								// Reset fragmentation state
								fragmentStart = null;
								fragments.Clear();
								totalLength = 0;
							}
						}
					}
					catch (WebSocketException ex)
					{
						SimpleHttpLogger.LogVerbose(ex);
						if (closeFrame == null)
							closeFrame = new WebSocketCloseFrame(isClient(), ex.closeCode ?? WebSocketCloseCode.InternalError, ex.CloseReason);
						// This closeFrame is handled by the "finally" block, causing the connection to close soon, but otherwise we continue to try receiving frames from the remote host.
					}
					catch (Exception ex)
					{
						bool isDisconnect = HttpProcessor.IsOrdinaryDisconnectException(ex);
						if (isDisconnect)
						{
							// Don't wait for a close frame.
							isClosing = true;
							expectingMoreMessages = false;
							receivedCloseFrame = true;
							sentCloseFrame = true;
						}
						else
							SimpleHttpLogger.LogVerbose(ex);
						//SimpleHttpLogger.Log(ex);

						if (closeFrame == null)
							closeFrame = new WebSocketCloseFrame(isClient(), isDisconnect ? WebSocketCloseCode.ConnectionLost : WebSocketCloseCode.InternalError);
					}
					finally
					{
						if (closeFrame != null)
						{
							// A close frame was generated.  Send it if we haven't sent one yet.
							SendCloseFrame(closeFrame.CloseCode, closeFrame.Message);
							// Wait for a close frame from the remote endpoint, then trigger termination of this thread.
							if (!receivedCloseFrame)
							{
								try
								{
									tcpClient.ReceiveTimeout = Math.Min(tcpClient.ReceiveTimeout, 5000);
									SetTimeout.OnBackground(() =>
									{
										IntervalSleeper sleeper = new IntervalSleeper(10);
										sleeper.SleepUntil(5000, () => receivedCloseFrame);
										expectingMoreMessages = false;
									}, 0);
								}
								catch (ObjectDisposedException)
								{
									// The above access to ReceiveTimeout can throw an ObjectDisposedException because the underlying Socket was disposed already.
									expectingMoreMessages = false;
								}
							}
						}
					}
				}
			}
			finally
			{
				try
				{
					if (closeFrame == null)
					{
						// This should not happen, but it is possible that further development could leave a code path where closeFrame did not get set.
						closeFrame = new WebSocketCloseFrame(isClient(), WebSocketCloseCode.InternalError, "Unexpected code path.");
						SimpleHttpLogger.Log("An unexpected code path resulted in a closeFrame being generated in WebSocketRead()'s finally block.");
					}
					SendCloseFrame(closeFrame.CloseCode, closeFrame.Message);
				}
				catch { }
				// Close the underlying connection.
				try { this.tcpClient.Close(); } catch { }
				// Notify the caller that the WebSocket is closed.
				try { onClose(closeFrame); } catch { }
			}
		}
		#endregion

		#region Writing Frames

		/// <summary>
		/// Sends a text frame to the remote endpoint.
		/// </summary>
		/// <param name="textBody">Body text.</param>
		public void Send(string textBody)
		{
			if (!isClosing)
				SendFrame(WebSocketOpcode.Text, ByteUtil.Utf8NoBOM.GetBytes(textBody));
		}
		/// <summary>
		/// Sends a binary frame to the remote endpoint.
		/// </summary>
		/// <param name="dataBody">Body text.</param>
		public void Send(byte[] dataBody)
		{
			if (!isClosing)
				SendFrame(WebSocketOpcode.Binary, dataBody);
		}
		/// <summary>
		/// <para>Sends a close frame to the remote endpoint, only if a close frame has not already been sent.</para>
		/// <para>Prevents further writing to the WebSocket.</para>
		/// <para>After calling this, the connection may remain open for a while as we wait to receive a close frame from the remote endpoint (unless it has already been received).  Eventually, the underlying TCP connection will be closed.</para>
		/// </summary>
		/// <param name="closeCode">The reason for the close. Note that some of the <see cref="WebSocketCloseCode"/> values are not intended to be sent.</param>
		/// <param name="message">A message to include in the close frame.  You can assume this message will not be shown to the user.  The message may be truncated to ensure the UTF8-Encoded length is 125 bytes or less.</param>
		private void SendCloseFrame(WebSocketCloseCode closeCode, string message = null)
		{
			if (sentCloseFrame || closeCode == WebSocketCloseCode.None || closeCode == WebSocketCloseCode.TLSHandshakeFailed || closeCode == WebSocketCloseCode.ConnectionLost)
				return;
			try
			{
				tcpClient.SendTimeout = Math.Min(tcpClient.SendTimeout, 5000);
				lock (startStopLock)
				{
					isClosing = true;
					byte[] msgBytes;
					if (message == null)
						msgBytes = new byte[0];
					else
					{
						if (message.Length > 123)
							message = message.Remove(123);

						msgBytes = ByteUtil.Utf8NoBOM.GetBytes(message);
						while (msgBytes.Length > 123 && message.Length > 0)
						{
							message = message.Remove(message.Length - 1);
							msgBytes = ByteUtil.Utf8NoBOM.GetBytes(message);
						}
						if (message.Length == 0)
							msgBytes = new byte[0];
					}

					byte[] payload = new byte[2 + msgBytes.Length];
					ByteUtil.WriteUInt16((ushort)closeCode, payload, 0);
					Array.Copy(msgBytes, 0, payload, 2, msgBytes.Length);

					lock (sendLock)
					{
						if (sentCloseFrame)
							return;
						SendFrame(WebSocketOpcode.Close, payload);
						tcpStream.Flush();
						sentCloseFrame = true;
					}
				}
			}
			catch (Exception ex)
			{
				SimpleHttpLogger.LogVerbose(ex);
			}
		}
		/// <summary>
		/// Sends a ping frame to the remote endpoint to help prevent the underlying socket/protocol from timing out.
		/// </summary>
		public void SendPing()
		{
			if (!isClosing)
				SendFrame(WebSocketOpcode.Ping, ByteUtil.GenerateRandomBytes(4));
		}
		internal void SendFrame(WebSocketOpcode opcode, byte[] data)
		{
			if (sentCloseFrame)
				return;
			lock (sendLock)
			{
				if (sentCloseFrame)
					return;
				// Write frame header
				WebSocketFrameHeader head = new WebSocketFrameHeader(opcode, data.Length, isClient());
				head.Write(tcpStream);
				// Write payload
				data = head.GetMaskedBytes(data);
				tcpStream.Write(data, 0, data.Length);
			}
		}

		#endregion

		#region Helpers
		/// <summary>
		/// Returns true of the HTTP connection has requested to be upgraded to a WebSocket.
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		public static bool IsWebSocketRequest(HttpProcessor p)
		{
			if (p.Request.HttpMethod != "GET")
				return false;
			if (p.Request.Headers.Get("Upgrade") != "websocket")
				return false;
			string[] connectionHeaderValues = p.Request.Headers.Get("Connection")?
				.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(s => s.Trim())
				.ToArray();
			if (connectionHeaderValues == null || !connectionHeaderValues.Contains("upgrade", true))
				return false;
			if (string.IsNullOrWhiteSpace(p.Request.Headers.Get("Sec-WebSocket-Key")))
				return false;
			return true;
		}

		/// <summary>
		/// Given a "Sec-WebSocket-Key" header value from a WebSocket client, returns the value of the "Sec-WebSocket-Accept" header that the server should provide in its response to complete the handshake.
		/// </summary>
		/// <param name="SecWebSocketKeyClientValue">"Sec-WebSocket-Key" header value from a WebSocket client</param>
		/// <returns></returns>
		public static string CreateSecWebSocketAcceptValue(string SecWebSocketKeyClientValue)
		{
			return Hash.GetSHA1Base64(SecWebSocketKeyClientValue + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
		}
		/// <summary>
		/// Completes the WebSocket handshake, only necessary if you constructed the WebSocket via a constructor that does not automatically do it.
		/// </summary>
		/// <param name="expectedPath">The absolute path you expected the client to request, e.g. "/WebSocket".  If it does not match (case-sensitive), an exception will be thrown and the handshake will not be completed.</param>
		/// <exception cref="Exception"></exception>
		public void CompleteWebSocketHandshake(string expectedPath)
		{
			lock (startStopLock)
			{
				if (handshakePerformed)
					throw new Exception("The WebSocket handshake has already been performed.");
				handshakePerformed = true;
			}

			// Read HTTP Request line
			string request = ByteUtil.ReadPrintableASCIILine(tcpStream);
			if (request == null)
				throw new Exception("HTTP protocol error: Line was unreadable.");
			string[] tokens = request.Split(' ');
			if (tokens.Length != 3)
				throw new Exception("invalid http request line: " + request);
			string http_method = tokens[0].ToUpper();
			if (http_method != "GET")
				throw new Exception("WebSocket protocol error: HTTP request method was not \"GET\"");

			Uri request_url;
			if (tokens[1].StartsWith("http://") || tokens[1].StartsWith("https://"))
				request_url = new Uri(tokens[1]);
			else
			{
				Uri base_uri_this_server = new Uri("http://" + tcpClient.Client.LocalEndPoint.ToString(), UriKind.Absolute);
				request_url = new Uri(base_uri_this_server, tokens[1]);
			}

			string requestedPath = request_url.AbsolutePath.StartsWith("/") ? request_url.AbsolutePath : "/" + request_url.AbsolutePath;

			if (requestedPath != expectedPath)
				throw new Exception("WebSocket handshake could not be completed because the requested path \"" + requestedPath + "\" did not match the expected path \"" + expectedPath + "\"");

			string http_protocol_versionstring = tokens[2];

			// Read HTTP Headers
			Dictionary<string, string> httpHeaders = new Dictionary<string, string>();
			string line;
			while ((line = ByteUtil.ReadPrintableASCIILine(tcpStream)) != "")
			{
				if (line == null)
					throw new Exception("HTTP protocol error: Line was unreadable.");
				int separator = line.IndexOf(':');
				if (separator == -1)
					throw new Exception("invalid http header line: " + line);
				string name = line.Substring(0, separator);
				int pos = separator + 1;
				while (pos < line.Length && line[pos] == ' ')
					pos++; // strip any spaces

				string value = line.Substring(pos, line.Length - pos);

				string nameLower = name.ToLower();
				if (httpHeaders.TryGetValue(nameLower, out string existingValue))
					httpHeaders[nameLower] = existingValue + "," + value;
				else
					httpHeaders[nameLower] = value;
			}

			if (!httpHeaders.TryGetValue("connection", out string header_connection))
				throw new Exception("WebSocket handshake could not complete due to missing required http header \"Connection\".");

			string[] connectionHeaderValues = header_connection
				.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(s => s.Trim())
				.ToArray();

			if (!connectionHeaderValues.Contains("upgrade", true))
				throw new Exception("WebSocket handshake could not complete due to header \"Connection: " + header_connection + "\". Expected: \"Connection: Upgrade\".");

			if (!httpHeaders.TryGetValue("upgrade", out string header_upgrade))
				throw new Exception("WebSocket handshake could not complete due to missing required http header \"Upgrade\".");
			if (header_upgrade != "websocket")
				throw new Exception("WebSocket handshake could not complete due to header \"Upgrade: " + header_upgrade + "\". Expected: \"Upgrade: websocket\".");

			if (!httpHeaders.TryGetValue("sec-websocket-key", out string header_sec_websocket_key))
				throw new Exception("WebSocket handshake could not complete due to missing required http header \"Sec-Websocket-Key\".");
			if (string.IsNullOrWhiteSpace(header_sec_websocket_key))
				throw new Exception("WebSocket handshake could not complete due to header \"Sec-Websocket-Key\" having empty value.");

			// Done reading the client's part of the handshake.
			// Write the server part of the handshake.
			ByteUtil.WriteUtf8("HTTP/1.1 101 Switching Protocols\r\n", tcpStream);
			ByteUtil.WriteUtf8("Upgrade: websocket\r\n", tcpStream);
			ByteUtil.WriteUtf8("Connection: Upgrade\r\n", tcpStream);
			ByteUtil.WriteUtf8("Sec-WebSocket-Accept: " + CreateSecWebSocketAcceptValue(header_sec_websocket_key) + "\r\n", tcpStream);
			ByteUtil.WriteUtf8("\r\n", tcpStream);

			State = WebSocketState.Open;
		}
		#endregion
	}
}
