using BPUtil.IO;
using BPUtil.SimpleHttp.TLS.Implementation;
using System;
using System.Collections.Generic;
using System.IO;

namespace BPUtil.SimpleHttp.TLS
{
	/// <summary>
	/// Uses BPUtil's very limited TLS implementation to read the TLS Client Hello in order to get the name of the server.
	/// </summary>
	public class TlsServerNameReader
	{
		/// <summary>
		/// Reads the TLS Client Hello from the tcp stream, parsing out the server name provided by the Server Name extension. Returns a stream that behaves as if nothing had been read.
		/// </summary>
		/// <param name="tcpStream">The stream that is expected to begin a TLS 1.0, 1.1, or 1.2 handshake.</param>
		/// <param name="serverName">The server name from the TLS Server Name extension.  Or null.</param>
		/// <returns></returns>
		/// <exception cref="Exception">Throws if there is a problem parsing the TLS Client Hello.</exception>
		public static Stream Read(Stream tcpStream, out string serverName)
		{
			UnreadableStream unread = new UnreadableStream(tcpStream);

			FragmentStream readerOfClientHello = new FragmentStream(() =>
			{
				TLSPlaintext fragment = new TLSPlaintext(tcpStream);
				unread.Unread(fragment.data_header);
				unread.Unread(fragment.data_fragment);

				if (fragment.type != ContentType.handshake)
					throw new Exception("TLS protocol error: Fragment began with byte " + (byte)fragment.type + ". Expected " + (byte)ContentType.handshake);
				if (!fragment.version.IsSupported())
					throw new Exception("Unsupported TLS protocol version: " + fragment.version);

				return fragment;
			});

			HandshakeMessage firstHandshakeMessage = new HandshakeMessage(new BasicDataStream(readerOfClientHello));
			
			if (firstHandshakeMessage.msg_type == HandshakeType.client_hello)
			{
				ClientHello clientHello = firstHandshakeMessage.body as ClientHello;
				serverName = clientHello.serverName;
			}
			else
				serverName = null;

			return unread;
		}
	}
}