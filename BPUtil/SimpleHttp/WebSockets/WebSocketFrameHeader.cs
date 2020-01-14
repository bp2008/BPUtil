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
		internal WebSocketFrameHeader(Stream networkStream)
		{
			byte[] head = ByteUtil.ReadNBytes(networkStream, 2);
			fin = (head[0] & 0b10000000) > 0; // The FIN bit tells whether this is the last message in a series. If it's 0, then the server will keep listening for more parts of the message; otherwise, the server should consider the message delivered.
			opcode = (WebSocketOpcode)(head[0] & 0b00001111);
			mask = (head[1] & 0b10000000) > 0; // The MASK bit simply tells whether the message is encoded.
			if (!mask)
				throw new WebSocketException(WebSocketCloseCode.ProtocolError, "Client must mask all outgoing frames."); // TODO: Send a "Close frame" with a status code of 1002 (protocol error) // Clients MUST mask all frames they send.

			payloadLength = (ulong)(head[1] & 0b01111111);
			if (payloadLength == 126)
				payloadLength = ByteUtil.ReadUInt16(networkStream);
			if (payloadLength == 127)
				payloadLength = ByteUtil.ReadUInt64(networkStream);

			if (isControlFrame && !fin)
				throw new WebSocketException(WebSocketCloseCode.ProtocolError, "Control frame was fragmented");
			if (isControlFrame && payloadLength > 125)
				throw new WebSocketException(WebSocketCloseCode.ProtocolError, "Control frame exceeded maximum payload length");

			maskBytes = ByteUtil.ReadNBytes(networkStream, 4);
		}
		/// <summary>
		/// Constructs a non-fragmented WebSocketFrameHeader for the purpose of serializing it to a stream with <see cref="Write(Stream)"/>.
		/// </summary>
		/// <param name="opcode">The opcode to include in the header.</param>
		/// <param name="payloadLength">The payload length to include in the header.</param>
		internal WebSocketFrameHeader(WebSocketOpcode opcode, int payloadLength)
		{
			this.fin = true;
			this.opcode = opcode;
			this.mask = false;
			this.payloadLength = (ulong)payloadLength;
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
		/// Writes the frame header to the specified stream.  After writing the header, you must separately write the payload.  Fragmented messages are not supported by this library.
		/// </summary>
		/// <param name="stream">The stream to write the frame header to.</param>
		internal void Write(Stream stream)
		{
			byte[] head;
			if (payloadLength > ushort.MaxValue)
			{
				head = new byte[2 + 8];
				head[0] = GetFirstHeaderByte();
				head[1] = 127;
				ByteUtil.WriteUInt64(payloadLength, head, 2);

			}
			else if (payloadLength > 125)
			{
				head = new byte[2 + 2];
				head[0] = GetFirstHeaderByte();
				head[1] = 126;
				ByteUtil.WriteUInt16((ushort)payloadLength, head, 2);
			}
			else
			{
				head = new byte[2];
				head[0] = GetFirstHeaderByte();
				head[1] = (byte)payloadLength;
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
			if (mask)
			{
				for (int i = 0; i < buffer.Length; i++)
					buffer[i] = (byte)(buffer[i] ^ maskBytes[i % maskBytes.Length]);
			}
		}
	}
}
