using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp.WebSockets
{
	/// <summary>
	/// 
	/// From https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_servers
	/// 
	/// Each data frame (from the client to the server or vice-versa) follows this same format:
	///​​
	///	  0                   1                   2                   3
	///	  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
	///	 +-+-+-+-+-------+-+-------------+-------------------------------+
	///	 |F|R|R|R| opcode|M| Payload len |    Extended payload length    |
	///	 |I|S|S|S|  (4)  |A|     (7)     |             (16/64)           |
	///	 |N|V|V|V|       |S|             |   (if payload len==126/127)   |
	///	 | |1|2|3|       |K|             |                               |
	///	 +-+-+-+-+-------+-+-------------+ - - - - - - - - - - - - - - - +
	///	 |     Extended payload length continued, if payload len == 127  |
	///	 + - - - - - - - - - - - - - - - +-------------------------------+
	///	 |                               |Masking-key, if MASK set to 1  |
	///	 +-------------------------------+-------------------------------+
	///	 | Masking-key(continued)       |          Payload Data         |
	///	 +-------------------------------- - - - - - - - - - - - - - - - +
	///	 :                     Payload Data continued ...                :
	///	 + - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - +
	///	 |                     Payload Data continued ...                |
	///	 +---------------------------------------------------------------+
	/// </summary>
	internal class WebSocketFrameHeader
	{
		public bool fin;
		public WebSocketOpcode opcode;
		public bool mask;
		public byte[] maskBytes;
		public ulong payloadLength;

		public bool isControlFrame
		{
			get
			{
				return opcode == WebSocketOpcode.Close || opcode == WebSocketOpcode.Ping || opcode == WebSocketOpcode.Pong;
			}
		}
		internal WebSocketFrameHeader(WebSocketFrameHeader other)
		{
			fin = other.fin;
			opcode = other.opcode;
			mask = other.mask;
			maskBytes = other.maskBytes;
			payloadLength = other.payloadLength;
		}
		/// <summary>
		/// Constructs a non-fragmented WebSocketFrameHeader by deserializing it from a stream.
		/// </summary>
		/// <param name="networkStream">Stream to deserialize the header from.</param>
		/// <param name="iAmClient">Pass true if this header is being created by a WebSocketClient that is reading a header from a server.</param>
		internal WebSocketFrameHeader(Stream networkStream, bool iAmClient)
		{
			byte[] head = ByteUtil.ReadNBytes(networkStream, 2);
			fin = (head[0] & 0b10000000) > 0; // The FIN bit tells whether this is the last message in a series. If it's 0, then the server will keep listening for more parts of the message; otherwise, the server should consider the message delivered.
			opcode = (WebSocketOpcode)(head[0] & 0b00001111);
			mask = (head[1] & 0b10000000) > 0; // The MASK bit simply tells whether the message is encoded.
			if (!iAmClient && !mask)
				throw new WebSocketException(WebSocketCloseCode.ProtocolError, "Client must mask all outgoing frames.");
			else if (iAmClient && mask)
				throw new WebSocketException(WebSocketCloseCode.ProtocolError, "Server must not mask outgoing frames.");

			payloadLength = (ulong)(head[1] & 0b01111111);
			if (payloadLength == 126)
				payloadLength = ByteUtil.ReadUInt16(networkStream);
			if (payloadLength == 127)
				payloadLength = ByteUtil.ReadUInt64(networkStream);

			if (isControlFrame && !fin)
				throw new WebSocketException(WebSocketCloseCode.ProtocolError, "Control frame was fragmented");
			if (isControlFrame && payloadLength > 125)
				throw new WebSocketException(WebSocketCloseCode.ProtocolError, "Control frame exceeded maximum payload length");

			if (mask)
				maskBytes = ByteUtil.ReadNBytes(networkStream, 4);
		}
		/// <summary>
		/// Constructs a non-fragmented WebSocketFrameHeader for the purpose of serializing it to a stream with <see cref="Write(Stream)"/>.
		/// </summary>
		/// <param name="opcode">The opcode to include in the header.</param>
		/// <param name="payloadLength">The payload length to include in the header.</param>
		/// <param name="iAmClient">Pass true if this header is being created by a WebSocketClient with the intent to send the frame to a server.</param>
		internal WebSocketFrameHeader(WebSocketOpcode opcode, int payloadLength, bool iAmClient)
		{
			this.fin = true;
			this.opcode = opcode;
			this.mask = iAmClient;
			this.payloadLength = (ulong)payloadLength;
			if (mask)
				maskBytes = ByteUtil.GenerateRandomBytes(4);
		}

		/// <summary>
		/// Returns a new WebSocketFrameHeader constructed from a set of fragments.
		/// </summary>
		/// <param name="fragmentStart">The header of the frame that started the set of fragments.</param>
		/// <param name="payloadLength">The combined total length of the payloads of all fragments in the set.</param>
		/// <returns></returns>
		internal static WebSocketFrameHeader FromFragments(WebSocketFrameHeader fragmentStart, int payloadLength)
		{
			WebSocketFrameHeader h = new WebSocketFrameHeader(fragmentStart);
			h.payloadLength = (ulong)payloadLength;
			return h;
		}

		/// <summary>
		/// Writes the frame header to the specified stream.  After writing the header, you must separately write the payload.  Fragmented messages are not writable by this library.
		/// </summary>
		/// <param name="stream">The stream to write the frame header to.</param>
		internal void Write(Stream stream)
		{
			byte[] head;
			int maskSize = mask ? 4 : 0;
			if (payloadLength > ushort.MaxValue)
			{
				head = new byte[2 + 8 + maskSize];
				head[0] = GetFirstHeaderByte();
				head[1] = 127;
				ByteUtil.WriteUInt64(payloadLength, head, 2);

			}
			else if (payloadLength > 125)
			{
				head = new byte[2 + 2 + maskSize];
				head[0] = GetFirstHeaderByte();
				head[1] = 126;
				ByteUtil.WriteUInt16((ushort)payloadLength, head, 2);
			}
			else
			{
				head = new byte[2 + maskSize];
				head[0] = GetFirstHeaderByte();
				head[1] = (byte)payloadLength;
			}
			if (mask)
			{
				head[1] |= 0b10000000;
				Array.Copy(maskBytes, 0, head, head.Length - maskBytes.Length, maskBytes.Length);
			}
			stream.Write(head, 0, head.Length);
		}
		protected byte GetFirstHeaderByte()
		{
			return (byte)((fin ? 0b10000000 : 0) | ((byte)opcode & 0b00001111));
		}
		/// <summary>
		/// If the <see cref="mask"/> flag is set, masks the bytes in [buffer] using the mask specified by <see cref="maskBytes"/>.
		/// </summary>
		/// <param name="buffer"></param>
		internal void XORMask(byte[] buffer)
		{
			if (mask && buffer != null)
			{
				for (int i = 0; i < buffer.Length; i++)
					buffer[i] = (byte)(buffer[i] ^ maskBytes[i % maskBytes.Length]);
			}
		}
		/// <summary>
		/// Masks the data if necessary and returns a byte array. The original data array may be returned, or a masked copy of the data may be returned, but in either case the original data array will be unmodified.
		/// </summary>
		/// <returns></returns>
		public byte[] GetMaskedBytes(byte[] data)
		{
			if (mask)
			{
				byte[] masked = new byte[data.Length];
				Array.Copy(data, masked, data.Length);
				XORMask(masked);
				return masked;
			}
			return data;
		}
	}
}
