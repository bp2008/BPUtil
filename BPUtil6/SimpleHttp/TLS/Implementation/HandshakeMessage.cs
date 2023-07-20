using BPUtil.IO;
using System;
using System.IO;

namespace BPUtil.SimpleHttp.TLS.Implementation
{
	/// <summary>
	/// A very limited representation of the first part of the TLS handshake.  Implemented far enough to read the server name provided by the client.  Based on: https://tls13.xargs.org/
	/// </summary>
	public class HandshakeMessage
	{
		public HandshakeType msg_type;
		public int length;

		public HandshakeBody body;
		public bool ClientUsedTLS;

		#region Fields for managing parsing state
		//protected bool headerRead = false;
		//protected MemoryDataStream mds = new MemoryDataStream();
		#endregion

		public HandshakeMessage() { }
		public HandshakeMessage(BasicDataStream stream)
		{
			// Learn about the TLS protocol, byte-by-byte, here: https://tls13.xargs.org/
			FragmentStream fragmentStream = (stream as BasicDataStream).originalStream as FragmentStream;
			ClientUsedTLS = fragmentStream.ClientUsedTLS;
			if (!ClientUsedTLS)
			{
				ClientUsedTLS = false;
				return;
			}

			msg_type = (HandshakeType)stream.ReadByte();

			byte[] lengthData = new byte[] { 0, (byte)stream.ReadByte(), (byte)stream.ReadByte(), (byte)stream.ReadByte() };
			length = ByteUtil.ReadInt32(lengthData, 0);

			byte[] messageBodyData = stream.ReadNBytes(length);

			using (MemoryDataStream messageBodyStream = new MemoryDataStream(messageBodyData))
			{
				// For TLS 1.2, the client version number is expected to be 3.3 (SSL 3.3 == TLS 1.2).  If the actual TLS version is 1.3, for backwards compatibility they use the same version number as TLS 1.2 and specify TLS 1.3 later in an extension.
				byte clientVersionMajor = (byte)messageBodyStream.ReadByte();
				byte clientVersionMinor = (byte)messageBodyStream.ReadByte();

				if (msg_type == HandshakeType.client_hello)
					body = new ClientHello(messageBodyStream);
				else
					throw new Exception("TLS Handshake type \"" + msg_type + "\" is not supported.");
			}
		}
	}
}
