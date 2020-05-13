using BPUtil.IO;
using System;
using System.IO;

namespace BPUtil.SimpleHttp.TLS.Implementation
{
	public class HandshakeMessage
	{
		public HandshakeType msg_type;
		public uint length;

		public HandshakeBody body;

		#region Fields for managing parsing state
		protected bool headerRead = false;
		protected MemoryDataStream mds = new MemoryDataStream();
		#endregion

		public HandshakeMessage() { }
		public HandshakeMessage(IDataStream stream)
		{
			msg_type = (HandshakeType)stream.ReadByte();

			byte[] lengthData = new byte[] { 0, (byte)stream.ReadByte(), (byte)stream.ReadByte(), (byte)stream.ReadByte() };
			length = ByteUtil.ReadUInt32(lengthData, 0);

			if (msg_type == HandshakeType.client_hello)
			{
				body = new ClientHello(stream);
			}
			else
				throw new Exception("TLS Handshake type \"" + msg_type + "\" is not supported.");
		}
	}
}
