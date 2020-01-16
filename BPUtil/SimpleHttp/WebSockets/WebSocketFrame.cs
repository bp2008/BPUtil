using System.IO;

namespace BPUtil.SimpleHttp.WebSockets
{
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

	public class WebSocketBinaryFrame : WebSocketFrame
	{
		public byte[] Data;

		internal WebSocketBinaryFrame(WebSocketFrameHeader head, Stream stream) : base(head)
		{
			if (!head.fin || head.opcode == WebSocketOpcode.Continuation)
				throw new WebSocketException(WebSocketCloseCode.InternalError, "WebSocketBinaryFrame stream constructor is not compatible with fragmented frames.");

			if (head.payloadLength > (ulong)WebSocket.MAX_PAYLOAD_BYTES)
				throw new WebSocketException(WebSocketCloseCode.MessageTooBig, null);

			this.Data = ByteUtil.ReadNBytes(stream, (int)head.payloadLength);
			Head.XORMask(this.Data);
		}
		internal WebSocketBinaryFrame(WebSocketFrameHeader head, byte[] data) : base(head)
		{
			this.Data = data;
			Head.XORMask(this.Data);
		}
	}

	public class WebSocketTextFrame : WebSocketBinaryFrame
	{
		public string Text;

		internal WebSocketTextFrame(WebSocketFrameHeader head, Stream stream) : base(head, stream)
		{
			this.Text = ByteUtil.ReadUtf8(Data);
		}
		internal WebSocketTextFrame(WebSocketFrameHeader head, byte[] data) : base(head, data)
		{
			this.Text = ByteUtil.ReadUtf8(Data);
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

		internal WebSocketCloseFrame() : base(new WebSocketFrameHeader(WebSocketOpcode.Close, 0), new byte[0]) { }

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