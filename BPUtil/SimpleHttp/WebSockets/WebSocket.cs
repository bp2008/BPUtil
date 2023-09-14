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
	/// A WebSocket server connection providing synchronous access methods. It is recommended to adjust the Tcp Socket's read and write timeouts as needed to avoid premature disconnection.
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

		protected Thread thrWebSocketRead;
		protected Action<WebSocketFrame> onMessageReceived = delegate { };
		protected Action<WebSocketCloseFrame> onClose = delegate { };
		protected object sendLock = new object();
		protected object startStopLock = new object();
		protected bool handshakePerformed = false;
		protected bool sentCloseCode = false;
		protected bool isClosing = false;
		private volatile bool abort = false;

		#region Constructors and Initialization
		/// <summary>
		/// Empty constructor for use by WebSocketClient.
		/// </summary>
		protected WebSocket()
		{
		}
		/// <summary>
		/// Creates a new WebSocket bound to a <see cref="TcpClient"/> that is already connected. It is recommended to adjust the Tcp Socket's read and write timeouts as needed to avoid premature disconnection. If TLS is being used, this is not the constructor you want.
		/// </summary>
		/// <param name="tcpc">A connected <see cref="TcpClient"/> to bind to the new WebSocket instance.</param>
		public WebSocket(TcpClient tcpc)
		{
			this.tcpClient = tcpc;
			this.tcpClient.NoDelay = true;
			this.tcpStream = tcpc.GetStream();
		}

		/// <summary>
		/// Creates a new WebSocket bound to an <see cref="HttpProcessor"/> that has already read the request headers.  The WebSocket handshake will be completed automatically.  It is recommended to adjust the Tcp Socket's read and write timeouts as needed to avoid premature disconnection.
		/// </summary>
		/// <param name="p">An <see cref="HttpProcessor"/> to bind to the new WebSocket instance.</param>
		public WebSocket(HttpProcessor p) : this(p.tcpClient)
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
			p.Response.WebSocketUpgradeSync();
		}

		/// <summary>
		/// Creates a new WebSocket bound to an <see cref="HttpProcessor"/>.  The WebSocket handshake will be completed automatically.  This constructor calls StartReading automatically. It is recommended to adjust the Tcp Socket's read and write timeouts as needed to avoid premature disconnection.
		/// </summary>
		/// <param name="p">An <see cref="HttpProcessor"/> to bind to the new WebSocket instance.</param>
		/// <param name="onMessageReceived">A callback method which is called whenever a message is received from the WebSocket.</param>
		/// <param name="onClose">A callback method which is called when the WebSocket is closed by the remote endpoint.</param>
		public WebSocket(HttpProcessor p, Action<WebSocketFrame> onMessageReceived, Action<WebSocketCloseFrame> onClose) : this(p)
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
		/// Attempts to close this WebSocket.
		/// </summary>
		public void Close()
		{
			lock (startStopLock)
			{
				isClosing = true;
				abort = true;
				this.tcpClient.Close();
			}
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
		private void WebSocketRead()
		{
			WebSocketCloseFrame closeFrame = null;
			try
			{
				WebSocketFrameHeader fragmentStart = null;
				List<byte[]> fragments = new List<byte[]>();
				ulong totalLength = 0;
				while (!abort)
				{
					WebSocketFrameHeader head = new WebSocketFrameHeader(tcpStream, isClient());

					if (head.opcode == WebSocketOpcode.Close)
					{
						closeFrame = new WebSocketCloseFrame(head, tcpStream);
						Send(WebSocketCloseCode.Normal);
						//SimpleHttpLogger.LogVerbose("WebSocket connection closed with code: "
						//	+ (ushort)closeFrame.CloseCode
						//	+ " (" + closeFrame.CloseCode + ")"
						//	 + (!string.IsNullOrEmpty(closeFrame.Message) ? " -- \"" + closeFrame.Message + "\"" : ""));
						return;
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
							throw new WebSocketException(WebSocketCloseCode.MessageTooBig, "Host does not accept payloads larger than " + WebSocket.MAX_PAYLOAD_BYTES + ". Payload length is now " + totalLength + ".");

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
							catch (ThreadAbortException) { }
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
				closeFrame = new WebSocketCloseFrame(isClient());
				closeFrame.CloseCode = WebSocketCloseCode.GoingAway;
				Try.Swallow(() =>
				{
					tcpClient.SendTimeout = Math.Min(tcpClient.SendTimeout, 1000);
					Send(closeFrame.CloseCode);
				});
			}
			catch (ThreadAbortException)
			{
				if (closeFrame == null)
				{
					closeFrame = new WebSocketCloseFrame(isClient());
					closeFrame.CloseCode = WebSocketCloseCode.GoingAway;
				}
				Try.Swallow(() =>
				{
					tcpClient.SendTimeout = Math.Min(tcpClient.SendTimeout, 1000);
					Send(closeFrame.CloseCode);
				});
			}
			catch (WebSocketException ex)
			{
				SimpleHttpLogger.LogVerbose(ex);
				if (closeFrame == null)
				{
					closeFrame = new WebSocketCloseFrame(isClient());
					if (ex.closeCode != null)
						closeFrame.CloseCode = ex.closeCode.Value;
					else
						closeFrame.CloseCode = WebSocketCloseCode.InternalError;
					closeFrame.Message = ex.CloseReason;
				}
				Try.Swallow(() => { Send(closeFrame.CloseCode); });
			}
			catch (Exception ex)
			{
				bool isDisconnect = HttpProcessor.IsOrdinaryDisconnectException(ex);
				if (!isDisconnect)
					SimpleHttpLogger.LogVerbose(ex);

				if (closeFrame == null)
				{
					closeFrame = new WebSocketCloseFrame(isClient());
					closeFrame.CloseCode = isDisconnect ? WebSocketCloseCode.ConnectionLost : WebSocketCloseCode.InternalError;
				}
				Try.Swallow(() => { Send(closeFrame.CloseCode); });
			}
			finally
			{
				try
				{
					if (closeFrame == null)
					{
						// This should not happen, but it is possible that further development could leave a code path where closeFrame did not get set.
						closeFrame = new WebSocketCloseFrame(isClient());
						closeFrame.CloseCode = WebSocketCloseCode.InternalError;
					}
					onClose(closeFrame);
				}
				catch (ThreadAbortException) { }
				catch (Exception) { }
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
			SendFrame(WebSocketOpcode.Text, ByteUtil.Utf8NoBOM.GetBytes(textBody));
		}
		/// <summary>
		/// Sends a binary frame to the remote endpoint.
		/// </summary>
		/// <param name="dataBody">Body text.</param>
		public void Send(byte[] dataBody)
		{
			SendFrame(WebSocketOpcode.Binary, dataBody);
		}

		/// <summary>
		/// Sends a close frame to the remote endpoint.  After calling this, you should close the underlying TCP connection.
		/// </summary>
		/// <param name="closeCode">The reason for the close. Note that some of the <see cref="WebSocketCloseCode"/> values are not intended to be sent.</param>
		/// <param name="message">A message to include in the close frame.  You can assume this message will not be shown to the user.  The message may be truncated to ensure the UTF8-Encoded length is 125 bytes or less.</param>
		public void Send(WebSocketCloseCode closeCode, string message = null)
		{
			if (sentCloseCode || closeCode == WebSocketCloseCode.None || closeCode == WebSocketCloseCode.TLSHandshakeFailed || closeCode == WebSocketCloseCode.ConnectionLost)
				return;

			byte[] msgBytes;
			if (message == null)
				msgBytes = new byte[0];
			else
			{
				if (message.Length > 125)
					message = message.Remove(125);

				msgBytes = ByteUtil.Utf8NoBOM.GetBytes(message);
				while (msgBytes.Length > 125 && message.Length > 0)
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

			SendFrame(WebSocketOpcode.Close, payload);

			sentCloseCode = true;
		}
		/// <summary>
		/// Sends a ping frame to the remote endpoint to help prevent the underlying socket/protocol from timing out.
		/// </summary>
		public void SendPing()
		{
			SendFrame(WebSocketOpcode.Ping, ByteUtil.GenerateRandomBytes(4));
		}
		internal void SendFrame(WebSocketOpcode opcode, byte[] data)
		{
			lock (sendLock)
			{
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
		}
		#endregion
	}
}
