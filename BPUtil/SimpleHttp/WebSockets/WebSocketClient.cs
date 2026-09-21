using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp.WebSockets
{
	/// <summary>
	/// <para>A WebSocket client connection providing synchronous access methods.</para>
	/// <para>The connect and handshake phases are bounded by timeouts (see <see cref="WebSocketClient(string, bool, int, int, int, int, bool)"/>), so the constructor cannot block indefinitely against an unresponsive server.</para>
	/// <para>After the constructor returns, the socket's <see cref="WebSocket.ReceiveTimeout"/> and <see cref="WebSocket.SendTimeout"/> are the requested session timeouts (default 0, meaning infinite).  It remains the caller's responsibility to choose read and write timeouts suitable for the lifetime of the connection.  TCP keepalive is enabled by default so that a dead peer is eventually detected by the operating system even without read timeouts.</para>
	/// </summary>
	public class WebSocketClient : WebSocket
	{
		/// <summary>
		/// The default connect timeout in milliseconds (10000) used by <see cref="WebSocketClient(string, bool)"/>.
		/// </summary>
		public const int DefaultConnectTimeoutMs = 10000;
		/// <summary>
		/// The default handshake timeout in milliseconds (15000) used by <see cref="WebSocketClient(string, bool)"/>.
		/// </summary>
		public const int DefaultHandshakeTimeoutMs = 15000;
		/// <summary>
		/// The maximum number of HTTP header lines which will be accepted in the server's handshake response.
		/// </summary>
		private const int MaxResponseHeaderLines = 100;
		/// <summary>
		/// The Uri which this client connected to.
		/// </summary>
		public readonly Uri uri;
		/// <summary>
		/// The value of the "Sec-WebSocket-Key" header which this client sent during the connection phase.
		/// </summary>
		public readonly string SecWebsocketKey;
		/// <summary>
		/// The collection of HTTP headers sent by the WebSocket server.
		/// </summary>
		public HttpHeaderCollection ResponseHeaders { get; private set; }
		/// <summary>
		/// <para>Creates a new WebSocket and connects it to the specified URL, using a connect timeout of <see cref="DefaultConnectTimeoutMs"/> (10000 ms), a handshake timeout of <see cref="DefaultHandshakeTimeoutMs"/> (15000 ms), and TCP keepalive enabled.</para>
		/// <para>When the constructor returns, the socket's read and write timeouts are 0 (infinite).  It is recommended to adjust them as needed for the lifetime of the connection.</para>
		/// <para>See <see cref="WebSocketClient(string, bool, int, int, int, int, bool)"/> for details and for the exceptions this constructor can throw.</para>
		/// </summary>
		/// <param name="url">A URL to connect to.</param>
		/// <param name="acceptAnyCert">If true, any SSL certificate will be accepted from the remote server.</param>
		/// <exception cref="WebSocketClientTimeoutException">The connect did not complete within 10000 ms, or the handshake did not complete within 15000 ms.</exception>
		public WebSocketClient(string url, bool acceptAnyCert = false)
			: this(url, acceptAnyCert, DefaultConnectTimeoutMs, DefaultHandshakeTimeoutMs)
		{
		}
		/// <summary>
		/// <para>Creates a new WebSocket and connects it to the specified URL, performing the TLS handshake (for https and wss URLs) and the HTTP upgrade handshake before returning.</para>
		/// <para>If the constructor throws, the underlying connection has already been closed.</para>
		/// </summary>
		/// <param name="url">A URL to connect to.</param>
		/// <param name="acceptAnyCert">If true, any SSL certificate will be accepted from the remote server.</param>
		/// <param name="connectTimeoutMs">Maximum time in milliseconds to spend resolving the host name and establishing the TCP connection.  If this elapses, a <see cref="WebSocketClientTimeoutException"/> is thrown with <see cref="WebSocketClientTimeoutException.Phase"/> = <see cref="WebSocketClientPhase.Connect"/>.  A value less than or equal to 0 means no timeout (not recommended).  Suggested value: <see cref="DefaultConnectTimeoutMs"/> (10000).</param>
		/// <param name="handshakeTimeoutMs">Maximum time in milliseconds for the entire handshake after the TCP connection is established: the TLS handshake, sending the HTTP upgrade request, and receiving the complete HTTP upgrade response.  This is an overall deadline, not a per-read timeout, so a server that sends data very slowly cannot extend it.  It is also applied as the socket's read and write timeout during the handshake.  If this elapses, the connection is closed and a <see cref="WebSocketClientTimeoutException"/> is thrown with <see cref="WebSocketClientTimeoutException.Phase"/> indicating which part of the handshake was in progress.  A value less than or equal to 0 means no timeout (not recommended).  Default: <see cref="DefaultHandshakeTimeoutMs"/> (15000).</param>
		/// <param name="sessionReceiveTimeoutMs">The socket read timeout in milliseconds to apply after the handshake succeeds (see <see cref="WebSocket.ReceiveTimeout"/>).  Default: 0 (infinite), which is how previous versions left the socket.</param>
		/// <param name="sessionSendTimeoutMs">The socket write timeout in milliseconds to apply after the handshake succeeds (see <see cref="WebSocket.SendTimeout"/>).  Default: 0 (infinite), which is how previous versions left the socket.</param>
		/// <param name="tcpKeepAlive">If true (default), TCP keepalive is enabled on the socket (first probe after 30 seconds idle, then every 5 seconds, on platforms that allow these timings to be configured), so that a half-open connection to a peer that has disappeared is eventually torn down by the operating system.</param>
		/// <exception cref="WebSocketClientTimeoutException">The connect or handshake did not complete within the specified timeout.  This is a <see cref="TimeoutException"/> whose message names the phase, the host:port, and the timeout.  If the timeout was reported by a failed socket operation, that exception is the <see cref="Exception.InnerException"/>.</exception>
		/// <exception cref="SocketException">The TCP connection could not be established (e.g. connection refused or host not found).</exception>
		/// <exception cref="EndOfStreamException">The server closed the connection before sending a complete HTTP upgrade response.  This is an <see cref="IOException"/>.</exception>
		/// <exception cref="IOException">The connection failed during the TLS handshake or the HTTP upgrade (e.g. the server closed or reset the connection).</exception>
		/// <exception cref="System.Security.Authentication.AuthenticationException">The TLS handshake failed, e.g. certificate validation failed.</exception>
		/// <exception cref="WebSocketHttpResponseCodeUnexpectedException">The server responded with an HTTP status other than 101.</exception>
		/// <exception cref="WebSocketHttpResponseUnexpectedException">The first line of the server's response was not a recognizable HTTP status line.</exception>
		public WebSocketClient(string url, bool acceptAnyCert, int connectTimeoutMs, int handshakeTimeoutMs = DefaultHandshakeTimeoutMs, int sessionReceiveTimeoutMs = 0, int sessionSendTimeoutMs = 0, bool tcpKeepAlive = true)
		{
			uri = new Uri(url);
			string hostAndPort = uri.DnsSafeHost + ":" + uri.Port;
			WebSocketClientPhase phase = WebSocketClientPhase.Connect;
			HandshakeDeadline deadline = null;
			try
			{
				this.tcpClient = ConnectWithTimeout(uri.DnsSafeHost, uri.Port, connectTimeoutMs, hostAndPort);
				this.tcpClient.NoDelay = true;
				if (tcpKeepAlive)
					EnableTcpKeepAlive(this.tcpClient.Client);

				// Bound each read and write of the handshake, and also the handshake as a whole.
				int handshakeSocketTimeout = handshakeTimeoutMs > 0 ? handshakeTimeoutMs : 0;
				this.tcpClient.ReceiveTimeout = handshakeSocketTimeout;
				this.tcpClient.SendTimeout = handshakeSocketTimeout;
				if (handshakeTimeoutMs > 0)
					deadline = new HandshakeDeadline(this.tcpClient, handshakeTimeoutMs);

				this.tcpStream = this.tcpClient.GetStream();

				if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) || uri.Scheme.Equals("wss", StringComparison.OrdinalIgnoreCase))
				{
					phase = WebSocketClientPhase.TlsHandshake;
					RemoteCertificateValidationCallback certCallback = null;
					if (acceptAnyCert)
						certCallback = (sender, certificate, chain, sslPolicyErrors) => true;
					SslStream sslStream = new SslStream(this.tcpStream, false, certCallback, null);
					this.tcpStream = sslStream;
#pragma warning disable SYSLIB0039
					sslStream.AuthenticateAsClient(uri.DnsSafeHost, null, TLS.TlsNegotiate.Tls13 | System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls11 | System.Security.Authentication.SslProtocols.Tls, false);
#pragma warning restore SYSLIB0039
				}

				// Send first part of handshake.
				phase = WebSocketClientPhase.HttpUpgradeRequest;
				string host = uri.DnsSafeHost + (uri.IsDefaultPort ? "" : (":" + uri.Port));
				string origin = uri.Scheme + "://" + host;
				SecWebsocketKey = Convert.ToBase64String(ByteUtil.GenerateRandomBytes(16));

				WriteLine("GET " + uri.PathAndQuery + " HTTP/1.1");
				WriteLine("Host: " + host);
				WriteLine("Origin: " + origin);
				WriteLine("Upgrade: websocket");
				WriteLine("Connection: Upgrade");
				WriteLine("Sec-WebSocket-Key: " + SecWebsocketKey);
				WriteLine("Sec-WebSocket-Version: 13");
				WriteLine("");

				// Receive and validate server's handshake response
				phase = WebSocketClientPhase.HttpUpgradeResponse;
				CompleteWebSocketClientHandshake();

				if (deadline != null)
				{
					deadline.Dispose();
					if (deadline.Expired)
						throw new WebSocketClientTimeoutException(phase, hostAndPort, handshakeTimeoutMs);
				}

				// Handshake succeeded.  Apply the session timeouts (0 by default, which is how previous versions left the socket).
				this.tcpClient.ReceiveTimeout = sessionReceiveTimeoutMs;
				this.tcpClient.SendTimeout = sessionSendTimeoutMs;
				SetOpen();
			}
			catch (Exception ex)
			{
				SetErrored();
				if (deadline != null)
					deadline.Dispose();
				// Never leave a half-built connection open, and never continue using a socket after a timeout.
				CloseQuietly();
				if (!(ex is TimeoutException) && phase != WebSocketClientPhase.Connect && ((deadline != null && deadline.Expired) || IsSocketTimeout(ex)))
					throw new WebSocketClientTimeoutException(phase, hostAndPort, handshakeTimeoutMs, ex);
				throw;
			}
		}

		private static byte[] endOfLineBytes = new byte[] { 13, 10 };
		private void WriteLine(string line)
		{
			byte[] buf = ByteUtil.Utf8NoBOM.GetBytes(line);
			tcpStream.Write(buf, 0, buf.Length);
			tcpStream.Write(endOfLineBytes, 0, endOfLineBytes.Length);
		}
		/// <summary>
		/// Returns true if this WebSocket is acting as a client.
		/// </summary>
		/// <returns></returns>
		protected override bool isClient()
		{
			return true;
		}


		#region Helpers
		/// <summary>
		/// Creates a TcpClient and connects it to the specified host and port, throwing <see cref="WebSocketClientTimeoutException"/> if the connection is not established within the timeout.  DNS resolution is included in the timeout.  If the host resolves to multiple addresses, they are tried in order.
		/// </summary>
		private static TcpClient ConnectWithTimeout(string host, int port, int connectTimeoutMs, string hostAndPort)
		{
			TcpClient tcpc = CreateDualModeTcpClient();
			try
			{
				IAsyncResult ar = tcpc.BeginConnect(host, port, null, null);
				if (!ar.AsyncWaitHandle.WaitOne(connectTimeoutMs > 0 ? connectTimeoutMs : Timeout.Infinite))
					throw new WebSocketClientTimeoutException(WebSocketClientPhase.Connect, hostAndPort, connectTimeoutMs);
				tcpc.EndConnect(ar); // Throws the original SocketException if the connect failed.
				return tcpc;
			}
			catch
			{
				// Closing also abandons a pending connect attempt.
				try { tcpc.Close(); } catch { }
				throw;
			}
		}

		/// <summary>
		/// Creates an unconnected TcpClient able to connect to both IPv4 and IPv6 addresses, as the <see cref="TcpClient(string, int)"/> constructor could.
		/// </summary>
		private static TcpClient CreateDualModeTcpClient()
		{
			if (Socket.OSSupportsIPv6)
			{
				TcpClient tcpc = new TcpClient(AddressFamily.InterNetworkV6);
				try
				{
					tcpc.Client.DualMode = true;
					return tcpc;
				}
				catch
				{
					try { tcpc.Close(); } catch { }
				}
			}
			return new TcpClient(AddressFamily.InterNetwork);
		}

		/// <summary>
		/// Enables TCP keepalive on the socket, with a 30 second idle time and 5 second probe interval on platforms that allow these timings to be configured.
		/// </summary>
		private static void EnableTcpKeepAlive(Socket socket)
		{
			try
			{
				socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
			}
			catch
			{
				return;
			}
#if NETFRAMEWORK
			try
			{
				byte[] ka = new byte[12];
				BitConverter.GetBytes((uint)1).CopyTo(ka, 0); // enable
				BitConverter.GetBytes((uint)30000).CopyTo(ka, 4); // idle ms before first probe
				BitConverter.GetBytes((uint)5000).CopyTo(ka, 8); // ms between probes
				socket.IOControl(IOControlCode.KeepAliveValues, ka, null);
			}
			catch { } // Not supported on every platform; keepalive remains enabled with the OS default timings.
#else
			try
			{
				socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, 30); // seconds idle before first probe
				socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, 5); // seconds between probes
			}
			catch { } // Not supported on every platform; keepalive remains enabled with the OS default timings.
#endif
		}

		/// <summary>
		/// Returns true if the exception or any of its inner exceptions is a <see cref="SocketException"/> indicating a socket timeout.
		/// </summary>
		private static bool IsSocketTimeout(Exception ex)
		{
			for (; ex != null; ex = ex.InnerException)
				if (ex is SocketException se && se.SocketErrorCode == SocketError.TimedOut)
					return true;
			return false;
		}

		/// <summary>
		/// Closes the stream and TcpClient, ignoring any exceptions.
		/// </summary>
		private void CloseQuietly()
		{
			try { this.tcpStream?.Dispose(); } catch { }
			try { this.tcpClient?.Close(); } catch { }
		}

		/// <summary>
		/// Reads a line of the server's handshake response, following the same rules as <see cref="ByteUtil.ReadPrintableASCIILine"/> except that reaching the end of the stream before the end of the line throws <see cref="EndOfStreamException"/> instead of returning a truncated line.
		/// </summary>
		/// <returns>The line, or null if the line contains invalid characters or is too long.</returns>
		/// <exception cref="EndOfStreamException">The server closed the connection before sending a complete line.</exception>
		private string ReadResponseLine()
		{
			const int maxLength = 32768;
			List<byte> data = new List<byte>();
			while (true)
			{
				int next_char = tcpStream.ReadByte();
				if (next_char == '\n') { break; }
				if (next_char == '\r') { continue; }
				if (next_char == -1)
					throw new EndOfStreamException("The server at " + uri.DnsSafeHost + ":" + uri.Port + " closed the connection " + (ResponseHeaders == null ? "without sending a complete HTTP upgrade response status line" : "before sending all HTTP upgrade response headers") + ".");
				if (next_char < 32 || next_char > 126)
					return null;
				if (data.Count >= maxLength)
					return null;
				data.Add((byte)next_char);
			}
			return Encoding.ASCII.GetString(data.ToArray());
		}

		private void CompleteWebSocketClientHandshake()
		{
			lock (startStopLock)
			{
				if (handshakePerformed)
					throw new Exception("The WebSocketClient handshake has already been performed.");
				handshakePerformed = true;
			}

			// Read HTTP response
			string firstResponseLine = ReadResponseLine();
			if (firstResponseLine == null)
				throw new Exception("HTTP protocol error: Line was unreadable.");
			if (firstResponseLine != "HTTP/1.1 101 Switching Protocols")
			{
				string[] tokens = firstResponseLine.Split(' ');
				if (tokens.Length == 3 && tokens[0] == "HTTP/1.1" && int.TryParse(tokens[1], out int statusCode))
					throw new WebSocketHttpResponseCodeUnexpectedException(statusCode, tokens[2]);
				else
					throw new WebSocketHttpResponseUnexpectedException(firstResponseLine);
			}

			// Read HTTP Headers
			ResponseHeaders = new HttpHeaderCollection();
			string line;
			int headerLineCount = 0;
			while ((line = ReadResponseLine()) != "")
			{
				if (line == null)
					throw new Exception("HTTP protocol error: Line was unreadable.");
				if (++headerLineCount > MaxResponseHeaderLines)
					throw new Exception("HTTP protocol error: The WebSocket handshake response had more than " + MaxResponseHeaderLines + " header lines.");
				int separator = line.IndexOf(':');
				if (separator == -1)
					throw new Exception("invalid http header line: " + line);
				string name = line.Substring(0, separator);
				int pos = separator + 1;
				while (pos < line.Length && line[pos] == ' ')
					pos++; // strip any spaces
				string value = line.Substring(pos, line.Length - pos);
				ResponseHeaders.Add(name, value);
			}

			if (!ResponseHeaders.TryGetValue("Connection", out string header_connection))
				throw new Exception("WebSocket handshake could not complete due to missing required http header \"Connection\".");

			string[] connectionHeaderValues = header_connection
				.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(s => s.Trim())
				.ToArray();

			if (!connectionHeaderValues.Contains("upgrade", true))
				throw new Exception("WebSocket handshake could not complete due to header \"Connection: " + header_connection + "\". Expected: \"Connection: Upgrade\".");

			if (!ResponseHeaders.TryGetValue("Upgrade", out string header_upgrade))
				throw new Exception("WebSocket handshake could not complete due to missing required http header \"Upgrade\".");
			if (header_upgrade != "websocket")
				throw new Exception("WebSocket handshake could not complete due to header \"Upgrade: " + header_upgrade + "\". Expected: \"Upgrade: websocket\".");

			if (!ResponseHeaders.TryGetValue("Sec-Websocket-Accept", out string header_sec_websocket_accept))
				throw new Exception("WebSocket handshake could not complete due to missing required http header \"Sec-Websocket-Accept\".");
			if (header_sec_websocket_accept != CreateSecWebSocketAcceptValue(SecWebsocketKey))
				throw new Exception("WebSocket handshake could not complete due to header \"Sec-Websocket-Accept: " + header_sec_websocket_accept + "\" with unexpected value.");

			// Done reading and validating the response to our handshake.
		}

		/// <summary>
		/// Closes a TcpClient unless this object is disposed before a deadline elapses.  This bounds the total duration of a sequence of blocking socket operations, which per-operation socket timeouts cannot do.
		/// </summary>
		private sealed class HandshakeDeadline : IDisposable
		{
			private readonly object syncLock = new object();
			private readonly TcpClient tcpClient;
			private readonly Timer timer;
			private bool finished = false;
			/// <summary>
			/// True if the deadline elapsed before <see cref="Dispose"/> was called, in which case the TcpClient has been closed.
			/// </summary>
			public bool Expired { get; private set; }
			public HandshakeDeadline(TcpClient tcpClient, int timeoutMs)
			{
				this.tcpClient = tcpClient;
				timer = new Timer(OnDeadline, null, timeoutMs, Timeout.Infinite);
			}
			private void OnDeadline(object state)
			{
				lock (syncLock)
				{
					if (finished)
						return;
					finished = true;
					Expired = true;
				}
				// Closing the socket makes any blocked read or write fail.
				try { tcpClient.Close(); } catch { }
			}
			public void Dispose()
			{
				lock (syncLock)
					finished = true;
				timer.Dispose();
			}
		}
		#endregion
	}
}
