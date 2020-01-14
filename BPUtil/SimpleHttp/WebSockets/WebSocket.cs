using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp.WebSockets
{
	/// <summary>
	/// A WebSocket server connection providing synchronous access methods.
	/// </summary>
	public class WebSocket
	{
		/// <summary>
		/// The maximum size of a payload this WebSocket will allow to be received. Any payloads exceeding this size will cause the WebSocket to be closed.
		/// </summary>
		public static int MAX_PAYLOAD_BYTES = 20000000;
		/// <summary>
		/// The HttpProcessor instance this WebSocket is bound to.
		/// </summary>
		public readonly HttpProcessor p;

		protected Thread thrWebSocketRead;
		protected Action<WebSocketBinaryFrame> onMessageReceived = delegate { };
		protected Action<WebSocketBinaryFrame> onClose = delegate { };
		protected object sendLock = new object();

		#region Constructors and Initialization
		/// <summary>
		/// Creates a new WebSocket bound to an <see cref="HttpProcessor"/>.
		/// </summary>
		/// <param name="p">An <see cref="HttpProcessor"/> to bind to the new WebSocket instance.</param>
		public WebSocket(HttpProcessor p)
		{
			this.p = p;

			if (!IsWebSocketRequest(p))
				throw new Exception("Unable to create a WebSocket from a connection that did not request a websocket upgrade.");

			string version = p.GetHeaderValue("Sec-WebSocket-Version");
			if (version != "13")
			{
				List<KeyValuePair<string, string>> additionalHeaders = new List<KeyValuePair<string, string>>();
				additionalHeaders.Add(new KeyValuePair<string, string>("Sec-WebSocket-Version", "13"));
				p.writeSuccess(responseCode: "400 Bad Request", additionalHeaders: additionalHeaders);
				p.outputStream.Flush();
				throw new Exception("An unsupported web socket version was requested (\"" + version + "\").");
			}
			p.writeWebSocketUpgrade();
			p.outputStream.Flush();
		}

		/// <summary>
		/// Creates a new WebSocket bound to an <see cref="HttpProcessor"/>.  This constructor starts a background thread to read from the web socket.
		/// </summary>
		/// <param name="p">An <see cref="HttpProcessor"/> to bind to the new WebSocket instance.</param>
		/// <param name="onMessageReceived">A callback method which is called whenever a message is received from the WebSocket.</param>
		/// <param name="onClose">A callback method which is called when the WebSocket is closed by the remote endpoint.</param>
		public WebSocket(HttpProcessor p, Action<WebSocketBinaryFrame> onMessageReceived, Action<WebSocketBinaryFrame> onClose) : this(p)
		{
			StartReading(onMessageReceived, onClose);
		}

		/// <summary>
		/// Starts a background thread to read from the web socket. If the WebSocket reading thread is already active, an exception is thrown.
		/// </summary>
		/// <param name="onMessageReceived">A callback method which is called whenever a message is received from the WebSocket.</param>
		/// <param name="onClose">A callback method which is called when the WebSocket is closed by the remote endpoint.</param>
		public void StartReading(Action<WebSocketBinaryFrame> onMessageReceived, Action<WebSocketBinaryFrame> onClose)
		{
			if (thrWebSocketRead != null)
				throw new Exception("WebSocket reading thread was already active!");

			this.onMessageReceived = onMessageReceived;
			this.onClose = onClose;

			thrWebSocketRead = new Thread(WebSocketRead);
			thrWebSocketRead.Name = "WebSocket Read";
			thrWebSocketRead.IsBackground = true;
			thrWebSocketRead.Start();
		}
		#endregion

		#region Reading Frames
		private void WebSocketRead()
		{
			try
			{
				WebSocketFrameHeader fragmentStart = null;
				List<byte[]> fragments = new List<byte[]>();
				ulong totalLength = 0;
				while (true)
				{
					WebSocketFrameHeader head = new WebSocketFrameHeader(p.tcpStream);

					if (head.opcode == WebSocketOpcode.Close)
					{
						WebSocketCloseFrame closeFrame = new WebSocketCloseFrame(head, p.tcpStream);
						Send(WebSocketCloseCode.Normal);
						try
						{
							onClose(closeFrame);
						}
						catch (ThreadAbortException) { }
						catch (Exception ex)
						{
							if (!p.isOrdinaryDisconnectException(ex))
								SimpleHttpLogger.Log(ex);
						}
						//SimpleHttpLogger.LogVerbose("WebSocket connection closed with code: "
						//	+ (ushort)closeFrame.CloseCode
						//	+ " (" + closeFrame.CloseCode + ")"
						//	 + (!string.IsNullOrEmpty(closeFrame.Message) ? " -- \"" + closeFrame.Message + "\"" : ""));
						return;
					}
					else if (head.opcode == WebSocketOpcode.Ping)
					{
						WebSocketPingFrame pingFrame = new WebSocketPingFrame(head, p.tcpStream);
						SendFrame(WebSocketOpcode.Pong, pingFrame.Data);
						continue;
					}
					else if (head.opcode == WebSocketOpcode.Pong)
					{
						WebSocketPongFrame pingFrame = new WebSocketPongFrame(head, p.tcpStream);
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
							throw new WebSocketException(WebSocketCloseCode.MessageTooBig);

						// Keep track of the frame that started each set of fragments
						if (fragmentStart == null)
						{
							if (head.opcode == WebSocketOpcode.Continuation)
								throw new WebSocketException(WebSocketCloseCode.ProtocolError, "Continuation frame did not follow a Text or Binary frame.");
							fragmentStart = head;
						}

						// Read the Frame's Payload
						fragments.Add(ByteUtil.ReadNBytes(p.tcpStream, (int)head.payloadLength));

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
								if (!p.isOrdinaryDisconnectException(ex))
									SimpleHttpLogger.Log(ex);
							}

							// Reset fragmentation state
							fragmentStart = null;
							fragments.Clear();
							totalLength = 0;
						}
					}
				}
			}
			catch (ThreadAbortException) { }
			catch (Exception ex)
			{
				if (!p.isOrdinaryDisconnectException(ex))
					SimpleHttpLogger.LogVerbose(ex);
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
		}
		internal void SendFrame(WebSocketOpcode opcode, byte[] data)
		{
			lock (sendLock)
			{
				// Write frame header
				WebSocketFrameHeader head = new WebSocketFrameHeader(opcode, data.Length);
				head.Write(p.tcpStream);
				// Write payload
				p.tcpStream.Write(data, 0, data.Length);
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
			if (p.http_method != "GET")
				return false;
			if (p.GetHeaderValue("Upgrade") != "websocket")
				return false;
			if (p.GetHeaderValue("Connection") != "Upgrade")
				return false;
			if (string.IsNullOrWhiteSpace(p.GetHeaderValue("Sec-WebSocket-Key")))
				return false;
			return true;
		}
		#endregion
	}
}
