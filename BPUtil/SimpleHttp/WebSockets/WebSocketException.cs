using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp.WebSockets
{
	public class WebSocketException : Exception
	{
		public readonly WebSocketCloseCode? closeCode;
		private string _closeReason = null;
		public string CloseReason
		{
			get
			{
				if (_closeReason == null)
					_closeReason = GetCloseReason(closeCode);
				return _closeReason;
			}
			set
			{
				_closeReason = value;
			}
		}
		public WebSocketException(WebSocketCloseCode? closeCode) : this(closeCode, GetCloseReason(closeCode)) { }

		public WebSocketException(WebSocketCloseCode? closeCode, string message) : base(message)
		{
			this.closeCode = closeCode;
			if (!string.IsNullOrWhiteSpace(message))
				CloseReason = message;
		}

		public static string GetCloseReason(WebSocketCloseCode? closeCode)
		{
			if (closeCode != null)
				return "No reason was specified.";
			switch (closeCode)
			{
				case WebSocketCloseCode.Normal:
					return "Connection is closing normally.";
				case WebSocketCloseCode.GoingAway:
					return "This endpoint is going away.";
				case WebSocketCloseCode.ProtocolError:
					return "Protocol error.";
				case WebSocketCloseCode.UnacceptableData:
					return "A message contained a data type which this endpoint does not handle.";
				case WebSocketCloseCode.None:
					return "No reason was specified.";
				case WebSocketCloseCode.ConnectionLost:
					return "Connection Lost";
				case WebSocketCloseCode.DataFormat:
					return "Data was received with an invalid format.";
				case WebSocketCloseCode.PolicyViolation:
					return "A generic policy was violated.";
				case WebSocketCloseCode.MessageTooBig:
					return "A message was too big and could not be processed.";
				case WebSocketCloseCode.MissingExtension:
					return "The server did not implement a required WebSocket extension.";
				case WebSocketCloseCode.InternalError:
					return "An unexpected error occurred.";
				case WebSocketCloseCode.TLSHandshakeFailed:
					return "The TLS handshake with the remote endpoint failed.";
				default:
					return "Unknown close reason.";
			}
		}
	}
}
