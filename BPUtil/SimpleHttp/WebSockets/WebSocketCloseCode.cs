using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp.WebSockets
{
	public enum WebSocketCloseCode : ushort
	{
		/// <summary>
		/// The connection is closing normally (purpose fulfilled).
		/// </summary>
		Normal = 1000,
		/// <summary>
		/// The endpoint sending this close code is going away.
		/// </summary>
		GoingAway = 1001,
		/// <summary>
		/// A protocol error was encountered.
		/// </summary>
		ProtocolError = 1002,
		/// <summary>
		/// This endpoint does not support the type of data that was received (e.g. Text or Binary).
		/// </summary>
		UnacceptableData = 1003,
		/// <summary>
		/// No status code was present. DO NOT SEND THIS OVER THE NETWORK AS A CLOSE CODE.
		/// </summary>
		None = 1005,
		/// <summary>
		/// Connection was lost without receiving a close control frame. DO NOT SEND THIS OVER THE NETWORK AS A CLOSE CODE.
		/// </summary>
		ConnectionLost = 1006,
		/// <summary>
		/// Data was received that is not consistent with the data type. E.g. non-UTF8 data in a Text frame.
		/// </summary>
		DataFormat = 1007,
		/// <summary>
		/// A message violated a policy. To be used if no other close code is more appropriate.
		/// </summary>
		PolicyViolation = 1008,
		/// <summary>
		/// This endpoint is refusing to handle a message because it was too big.
		/// </summary>
		MessageTooBig = 1009,
		/// <summary>
		/// Sent by a client if the server did not implement a required WebSocket extension.
		/// </summary>
		MissingExtension = 1010,
		/// <summary>
		/// An unexpected error occurred.
		/// </summary>
		InternalError = 1011,
		/// <summary>
		/// TLS handshake failed. (e.g., the server certificate can't be verified). DO NOT SEND THIS OVER THE NETWORK AS A CLOSE CODE.
		/// </summary>
		TLSHandshakeFailed = 1015

	}
}
