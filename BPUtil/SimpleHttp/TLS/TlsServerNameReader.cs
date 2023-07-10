using BPUtil.IO;
using BPUtil.SimpleHttp.TLS.Implementation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net.Sockets;

namespace BPUtil.SimpleHttp.TLS
{
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
		/// <returns></returns>
		/// <exception cref="Exception">Throws if there is a problem parsing the TLS Client Hello.</exception>
		[Obsolete("Use TlsServerNameReader.TryGetTlsClientHelloServerNames instead of TlsServerNameReader.Read.  The former is more efficient because it does not require an UnreadableStream.")]
		public static Stream Read(Stream tcpStream, out string serverName, out bool clientUsedTls)
		{
			clientUsedTls = true;

			UnreadableStream unread = new UnreadableStream(tcpStream);

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
			}
			else
				serverName = null;

			return unread;
		}
		/// <summary>
		/// <para>Determines if the socket contains an unread TLS Client Hello, and loads the server name provided by the client via TLS Server Name Indication.</para>
		/// <para>Returns true if the client is requesting TLS, false otherwise. This method only peeks at the network stream and does not remove any bytes from it.</para>
		/// <para>The server name will be null if the client did not provide it via TLS Server Name Indication.</para>
		/// </summary>
		/// <param name="socket">The network socket.</param>
		/// <param name="serverName">The server name as indicated by the TLS Server Name Indication extension.  The server name will be null if the client did not provide it.</param>
		/// <returns></returns>
		public static bool TryGetTlsClientHelloServerNames(Socket socket, out string serverName)
		{
			// Create a buffer to hold the data
			byte[] buffer = new byte[10];

			// Peek at the data in the socket without removing it
			int bytesRead = socket.Receive(buffer, 0, buffer.Length, SocketFlags.Peek);

			// Check if the data is a TLS "Client Hello"
			if (buffer[0] == 0x16 && buffer[1] == 0x03 && buffer[5] == 0x01)
			{
				// Load the entire "Client Hello" into the buffer
				int length = (buffer[3] << 8) + buffer[4] + 5;
				buffer = new byte[length];
				bytesRead = socket.Receive(buffer, 0, length, SocketFlags.Peek);

				using (MemoryStream ms = new MemoryStream(buffer))
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
						serverName = clientHello.serverName;
						return true;
					}
					else
					{
						serverName = null;
						return false;
					}

					// Broken implementation from Bing chat:
					//// Find the start of the extensions
					//int extensionsStart = 43 + buffer[43];
					//if (extensionsStart + 2 < bytesRead)
					//{
					//	// Get the length of the extensions
					//	int extensionsLength = (buffer[extensionsStart] << 8) + buffer[extensionsStart + 1];
					//	int extensionsEnd = extensionsStart + 2 + extensionsLength;

					//	// Loop through the extensions
					//	int i = extensionsStart + 2;
					//	while (i + 4 <= extensionsEnd)
					//	{
					//		// Get the type and length of the extension
					//		int type = (buffer[i] << 8) + buffer[i + 1];
					//		int extLength = (buffer[i + 2] << 8) + buffer[i + 3];

					//		// Check if this is the Server Name Indication extension
					//		if (type == 0x00 && i + 9 <= extensionsEnd)
					//		{
					//			// Get the length of the server name list
					//			int listLength = (buffer[i + 7] << 8) + buffer[i + 8];

					//			// Loop through the server name list
					//			int j = i + 9;
					//			while (j + 3 <= i + extLength)
					//			{
					//				// Get the type and length of the server name
					//				int nameType = buffer[j];
					//				int nameLength = (buffer[j + 1] << 8) + buffer[j + 2];

					//				// Check if this is a DNS hostname
					//				if (nameType == 0x00 && j + nameLength <= i + extLength)
					//				{
					//					// Get the server name
					//					serverName = System.Text.Encoding.ASCII.GetString(buffer, j + 3, nameLength);
					//					return true;
					//				}

					//				j += nameLength;
					//			}
					//		}

					//		i += extLength;
					//	}
					//}
				}
			}
			else
			{
				serverName = null;
				return false;
			}
		}
	}
}