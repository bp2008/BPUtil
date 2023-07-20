using System;
using System.IO;

namespace BPUtil.SimpleHttp.WebSockets
{
	/// <summary>
	/// Base class for a frame from a WebSocket.  This will actually be of type <see cref="WebSocketBinaryFrame"/> or <see cref="WebSocketTextFrame"/>.
	/// </summary>
	public class WebSocketFrame
	{
		/// <summary>
		/// The frame header.  If this frame was constructed from fragments, it is the header of the first fragment.
		/// </summary>
		internal WebSocketFrameHeader Head;
		internal WebSocketFrame(WebSocketFrameHeader head)
		{
			this.Head = head;
		}
	}

	/// <summary>
	/// A frame from a WebSocket. Contains a byte array of data.
	/// </summary>
	public class WebSocketBinaryFrame : WebSocketFrame
	{
		/// <summary>
		/// The unmasked data of this frame. Use Head.GetMaskedBytes(byte[]) to get masked data.
		/// </summary>
		public byte[] Data;

		internal WebSocketBinaryFrame(WebSocketFrameHeader head, Stream stream) : base(head)
		{
			if (!head.fin || head.opcode == WebSocketOpcode.Continuation)
				throw new WebSocketException(WebSocketCloseCode.InternalError, "WebSocketBinaryFrame stream constructor is not compatible with fragmented frames.");

			if (head.payloadLength > (ulong)WebSocket.MAX_PAYLOAD_BYTES)
				throw new WebSocketException(WebSocketCloseCode.MessageTooBig, "Host does not accept payloads larger than " + WebSocket.MAX_PAYLOAD_BYTES + ". " + head.payloadLength + " is too large.");

			this.Data = ByteUtil.ReadNBytes(stream, (int)head.payloadLength);
			Head.XORMask(this.Data);
		}
		internal WebSocketBinaryFrame(WebSocketFrameHeader head, byte[] data) : base(head)
		{
			Head.XORMask(data);
			this.Data = data;
		}
	}

	/// <summary>
	///  A frame from a WebSocket. Contains a string of data.
	/// </summary>
	public class WebSocketTextFrame : WebSocketBinaryFrame
	{
		/// <summary>
		/// Gets or sets the text of this frame by coverting to/from the Data property of the underlying WebSocketBinaryFrame. If you need to read this value multiple times, it is better to cache the result.
		/// </summary>
		public string Text
		{
			get
			{
				return ByteUtil.ReadUtf8(Data);
			}
			set
			{
				Data = ByteUtil.Utf8NoBOM.GetBytes(value);
			}
		}

		internal WebSocketTextFrame(WebSocketFrameHeader head, Stream stream) : base(head, stream)
		{
		}
		internal WebSocketTextFrame(WebSocketFrameHeader head, byte[] data) : base(head, data)
		{
		}
		internal WebSocketTextFrame(WebSocketFrameHeader head, string data) : base(head, ByteUtil.Utf8NoBOM.GetBytes(data))
		{
		}
	}

	public class WebSocketCloseFrame : WebSocketBinaryFrame
	{
		/// <summary>
		/// A status code indicating the reason the WebSocket was closed.
		/// </summary>
		public WebSocketCloseCode CloseCode;

		/// <summary>
		/// An optional message further describing the reason the WebSocket was closed.
		/// </summary>
		public string Message;

		/// <summary>
		/// Constructs a WebSocketCloseFrame.
		/// </summary>
		/// <param name="iAmClient">Pass true if this frame is being created by a WebSocketClient with the intent to send the frame to a server.</param>
		internal WebSocketCloseFrame(bool iAmClient) : base(new WebSocketFrameHeader(WebSocketOpcode.Close, 0, iAmClient), new byte[0]) { }

		internal WebSocketCloseFrame(WebSocketFrameHeader head, Stream stream) : base(head, stream)
		{
			if (Data.Length > 1)
				CloseCode = (WebSocketCloseCode)ByteUtil.ReadInt16(Data, 0);
			else
				CloseCode = WebSocketCloseCode.None;

			if (Data.Length > 2)
				Message = ByteUtil.ReadUtf8(Data, 2, Data.Length - 2);
		}
	}

	internal class WebSocketPingFrame : WebSocketBinaryFrame
	{
		internal WebSocketPingFrame(WebSocketFrameHeader head, Stream stream) : base(head, stream) { }
	}

	internal class WebSocketPongFrame : WebSocketBinaryFrame
	{
		internal WebSocketPongFrame(WebSocketFrameHeader head, Stream stream) : base(head, stream) { }
	}
}