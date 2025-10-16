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
	/// A WebSocket client connection providing synchronous access methods. It is recommended to adjust the Tcp Socket's read and write timeouts as needed to avoid premature disconnection.
	/// </summary>
	public class WebSocketClient : WebSocket
	{
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
		/// Creates a new WebSocket and connects it to the specified URL. It is recommended to adjust the Tcp Socket's read and write timeouts as needed to avoid premature disconnection.
		/// </summary>
		/// <param name="url">A URL to connect to.</param>
		/// <param name="acceptAnyCert">If true, any SSL certificate will be accepted from the remote server.</param>
		public WebSocketClient(string url, bool acceptAnyCert = false)
		{
			uri = new Uri(url);
			this.tcpClient = new TcpClient(uri.DnsSafeHost, uri.Port);
			this.tcpClient.NoDelay = true;
			this.tcpStream = this.tcpClient.GetStream();

			if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
			{
				RemoteCertificateValidationCallback certCallback = null;
				if (acceptAnyCert)
					certCallback = (sender, certificate, chain, sslPolicyErrors) => true;
				SslStream sslStream = new SslStream(this.tcpStream, false, certCallback, null);
#pragma warning disable SYSLIB0039
				sslStream.AuthenticateAsClient(uri.DnsSafeHost, null, System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls11 | System.Security.Authentication.SslProtocols.Tls, false);
#pragma warning restore SYSLIB0039
				this.tcpStream = sslStream;
			}

			// Send first part of handshake.
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
			CompleteWebSocketClientHandshake();
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
		private void CompleteWebSocketClientHandshake()
		{
			lock (startStopLock)
			{
				if (handshakePerformed)
					throw new Exception("The WebSocketClient handshake has already been performed.");
				handshakePerformed = true;
			}

			// Read HTTP response
			string firstResponseLine = ByteUtil.ReadPrintableASCIILine(tcpStream);
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
		#endregion
	}
}
