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
	/// <summary>
	/// A specialized WebSocketException with close code 4800.  Code 4800 is here defined to mean the first line of the HTTP response did not meet any basic expectations.
	/// </summary>
	public class WebSocketHttpResponseUnexpectedException : WebSocketException
	{
		/// <summary>
		/// This is the first line of the HTTP response.
		/// </summary>
		public readonly string HttpResponseFirstLine;
		/// <summary>
		/// A specialized WebSocketException with close code 4800.  Code 4800 is here defined to mean the first line of the HTTP response did not meet any basic expectations.
		/// </summary>
		/// <param name="httpResponseFirstLine">First line of the HTTP response.</param>
		public WebSocketHttpResponseUnexpectedException(string httpResponseFirstLine) : base((WebSocketCloseCode)4800, "Invalid first HTTP response line: \"" + httpResponseFirstLine + "\". Expected \"HTTP/1.1 101 Switching Protocols\".")
		{
			this.HttpResponseFirstLine = httpResponseFirstLine;
		}
	}
	/// <summary>
	/// A specialized WebSocketException with close code 4801.  Code 4801 is here defined to mean the HTTP response code was not "101" as expected.
	/// </summary>
	public class WebSocketHttpResponseCodeUnexpectedException : WebSocketException
	{
		/// <summary>
		/// If not null, this is the HTTP status code read from the response.
		/// </summary>
		public readonly int? StatusCode;
		/// <summary>
		/// If not null, this is the Reason Phrase given with the HTTP status code.
		/// </summary>
		public readonly string ReasonPhrase;
		/// <summary>
		/// A specialized WebSocketException with close code 4801.  Code 4801 is here defined to mean the HTTP response code was not "101" as expected.
		/// </summary>
		/// <param name="statusCode">HTTP status code that was provided with the HTTP response.</param>
		/// <param name="reasonPhrase">Reason Phrase that was provided with the HTTP response.</param>
		public WebSocketHttpResponseCodeUnexpectedException(int statusCode, string reasonPhrase) : base((WebSocketCloseCode)4801, "Unexpected HTTP response status: " + statusCode + " " + reasonPhrase)
		{
			this.StatusCode = statusCode;
			this.ReasonPhrase = reasonPhrase;
		}
	}
}
