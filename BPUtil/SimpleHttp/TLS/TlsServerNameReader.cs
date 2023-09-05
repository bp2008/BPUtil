using BPUtil.IO;
using BPUtil.SimpleHttp.TLS.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp.TLS
{
	/// <summary>
	/// Data from peeking at the beginning of the TLS negotiation.
	/// </summary>
	public class TlsPeekData
	{
		/// <summary>
		/// The server name from the TLS Server Name extension. Null if unavailable.
		/// </summary>
		public readonly string ServerName;
		/// <summary>
		/// True if the client requested protocol `acme-tls/1` via ALPN. This is part of ACME validation.
		/// </summary>
		public readonly bool IsTlsAlpn01Validation;
		/// <summary>
		/// Constructs a new TlsPeekData.
		/// </summary>
		/// <param name="serverName">The server name from the TLS Server Name extension. Null if unavailable.</param>
		/// <param name="isTlsAlpn01Validation">True if the client requested protocol `acme-tls/1` via ALPN. This is part of ACME validation.</param>
		public TlsPeekData(string serverName, bool isTlsAlpn01Validation)
		{
			ServerName = serverName;
			IsTlsAlpn01Validation = isTlsAlpn01Validation;
		}
	}
	/// <summary>
	/// Uses BPUtil's very limited TLS implementation to read the TLS Client Hello in order to get the name of the server. Based on: https://tls13.xargs.org/
	/// </summary>
	public class TlsServerNameReader
	{
		/// <summary>
		/// Reads the TLS Client Hello from the tcp stream, parsing out the server name provided by the Server Name extension. Returns a stream that behaves as if nothing had been read.
		/// </summary>
		/// <param name="tcpStream">The stream that is expected to begin a TLS 1.0, 1.1, or 1.2 handshake.</param>
		/// <param name="serverName">The server name from the TLS Server Name extension.  Or null.</param>
		/// <param name="clientUsedTls">True if the client used the TLS protocol (https), false if not.</param>
		/// <param name="isTlsAlpn01Validation">True if the client requested protocol `acme-tls/1` via ALPN. This is part of ACME validation.</param>
		/// <returns></returns>
		/// <exception cref="Exception">Throws if there is a problem parsing the TLS Client Hello.</exception>
		[Obsolete("Use TlsServerNameReader.TryGetTlsClientHelloServerNames instead of TlsServerNameReader.Read.  The former is more efficient because it does not require an UnreadableStream.")]
		public static Stream Read(Stream tcpStream, out string serverName, out bool clientUsedTls, out bool isTlsAlpn01Validation)
		{
			clientUsedTls = true;

			UnreadableStream unread = new UnreadableStream(tcpStream, false);

			FragmentStream readerOfClientHello = new FragmentStream(() =>
			{
				TLSPlaintext fragment = new TLSPlaintext(tcpStream);
				unread.Unread(fragment.data_header);
				if (fragment.isTlsHandshake)
				{
					unread.Unread(fragment.data_fragment);

					if (fragment.type != ContentType.handshake)
						throw new Exception("TLS protocol error: Fragment began with byte " + (byte)fragment.type + ". Expected " + (byte)ContentType.handshake);
					if (!fragment.version.IsSupported())
						throw new Exception("Unsupported TLS protocol version: " + fragment.version);
				}
				return fragment;
			});

			HandshakeMessage firstHandshakeMessage = new HandshakeMessage(new BasicDataStream(readerOfClientHello));
			clientUsedTls = firstHandshakeMessage.ClientUsedTLS;

			if (clientUsedTls && firstHandshakeMessage.msg_type == HandshakeType.client_hello)
			{
				ClientHello clientHello = firstHandshakeMessage.body as ClientHello;
				serverName = clientHello.serverName;
				isTlsAlpn01Validation = clientHello.isTlsAlpn01Validation;
			}
			else
			{
				serverName = null;
				isTlsAlpn01Validation = false;
			}

			return unread;
		}
		/// <summary>
		/// <para>Determines if the socket contains an unread TLS Client Hello, and loads the server name provided by the client via TLS Server Name Indication.</para>
		/// <para>This method only peeks at the network stream and does not remove any bytes from it.</para>
		/// <para>Returns an object containing the information learned from peeking at the TLS Client Hello message.  Null if this was not recognized as a TLS request.</para>
		/// </summary>
		/// <param name="socket">The network socket.</param>
		/// <returns></returns>
		public static TlsPeekData TryGetTlsClientHelloServerNames(Socket socket)
		{
			// Create a buffer to hold the data
			byte[] buffer = new byte[10];

			// Peek at the data in the socket without removing it
			// Wait until the socket has the data available and then use a normal blocking Receive method (which won't block).
			if (!TaskHelper.WaitUntilSync(() => socket.Available >= 10, 5000))
				throw new OperationCanceledException("Timed out waiting for the client to send data on the socket.");
			int bytesRead = socket.Receive(buffer, 0, buffer.Length, SocketFlags.Peek);

			// Check if the data is a TLS "Client Hello"
			if (buffer[0] == 0x16 && buffer[1] == 0x03 && buffer[5] == 0x01)
			{
				// Load the entire "Client Hello" into the buffer
				int length = (buffer[3] << 8) + buffer[4] + 5;
				if (socket.ReceiveBufferSize < length)
				{
					SimpleHttpLogger.LogVerbose("Increasing socket.ReceiveBufferSize from " + socket.ReceiveBufferSize + " to " + length + " to read full TLS Client Hello at once.");
					socket.ReceiveBufferSize = length;
				}
				buffer = length <= ByteUtil.BufferSize ? ByteUtil.BufferGet() : new byte[length];
				try
				{
					if (!TaskHelper.WaitUntilSync(() => socket.Available >= length, 2000))
						throw new OperationCanceledException("Timed out waiting for the client to send the full TLS Client Hello message.");
					bytesRead = socket.Receive(buffer, 0, length, SocketFlags.Peek);
					return GetTlsPeekData(buffer, length);
				}
				finally
				{
					if (buffer.Length == ByteUtil.BufferSize)
						ByteUtil.BufferRecycle(buffer);
				}
			}
			else
			{
				return null;
			}
		}
		/// <summary>
		/// <para>Determines if the socket contains an unread TLS Client Hello, and loads the server name provided by the client via TLS Server Name Indication.</para>
		/// <para>This method only peeks at the network stream and does not remove any bytes from it.</para>
		/// <para>Returns an object containing the information learned from peeking at the TLS Client Hello message.  Null if this was not recognized as a TLS request.</para>
		/// </summary>
		/// <param name="socket">The network socket.</param>
		/// <param name="cancellationToken">Cancellation token to allow cancelling the asynchronous request.</param>
		/// <returns></returns>
		public static async Task<TlsPeekData> TryGetTlsClientHelloServerNamesAsync(Socket socket, CancellationToken cancellationToken = default)
		{
			// Create a buffer to hold the data
			byte[] buffer = new byte[10];

			// Peek at the data in the socket without removing it
			// Wait until the socket has the data available and then use a normal blocking Receive method (which won't block).
			await TaskHelper.WaitUntilAsync(() => socket.Available >= 10, 5000, null, cancellationToken).ConfigureAwait(false);
			int bytesRead = socket.Receive(buffer, 0, buffer.Length, SocketFlags.Peek);

			// Check if the data is a TLS "Client Hello"
			if (buffer[0] == 0x16 && buffer[1] == 0x03 && buffer[5] == 0x01)
			{
				// Load the entire "Client Hello" into the buffer
				int length = (buffer[3] << 8) + buffer[4] + 5;
				if (socket.ReceiveBufferSize < length)
				{
					SimpleHttpLogger.LogVerbose("Increasing socket.ReceiveBufferSize from " + socket.ReceiveBufferSize + " to " + length + " to read full TLS Client Hello at once.");
					socket.ReceiveBufferSize = length;
				}
				buffer = length <= ByteUtil.BufferSize ? ByteUtil.BufferGet() : new byte[length];
				try
				{
					await TaskHelper.WaitUntilAsync(() => socket.Available >= length, 2000, null, cancellationToken).ConfigureAwait(false);
					bytesRead = socket.Receive(buffer, 0, length, SocketFlags.Peek);
					return GetTlsPeekData(buffer, length);
				}
				finally
				{
					if (buffer.Length == ByteUtil.BufferSize)
						ByteUtil.BufferRecycle(buffer);
				}
			}
			else
			{
				return null;
			}
		}

		private static TlsPeekData GetTlsPeekData(byte[] buffer, int length)
		{
			using (MemoryStream ms = new MemoryStream(buffer, 0, length))
			{
				FragmentStream readerOfClientHello = new FragmentStream(() =>
				{
					TLSPlaintext fragment = new TLSPlaintext(ms);
					if (fragment.isTlsHandshake)
					{
						if (fragment.type != ContentType.handshake)
							throw new Exception("TLS protocol error: Fragment began with byte " + (byte)fragment.type + ". Expected " + (byte)ContentType.handshake);
						if (!fragment.version.IsSupported())
							throw new Exception("Unsupported TLS protocol version: " + fragment.version);
					}
					return fragment;
				});

				HandshakeMessage firstHandshakeMessage = new HandshakeMessage(new BasicDataStream(readerOfClientHello));

				if (firstHandshakeMessage.ClientUsedTLS && firstHandshakeMessage.msg_type == HandshakeType.client_hello)
				{
					ClientHello clientHello = firstHandshakeMessage.body as ClientHello;
					return new TlsPeekData(clientHello.serverName, clientHello.isTlsAlpn01Validation);
				}
				else
				{
					return null;
				}
			}
		}
	}
}