namespace BPUtil.SimpleHttp.WebSockets
{
	/// <summary>
	/// Enumeration of possible <see cref="WebSocket"/> states.
	/// </summary>
	public enum WebSocketState
	{
		/// <summary>
		/// The WebSocket handshake with the remote endpoint is not yet completed (or has not been started yet).
		/// </summary>
		Connecting,
		/// <summary>
		/// The initial state after the HTTP handshake has been completed.  The "Open" state means the WebSocket is connected and ready to send and receive data.
		/// </summary>
		Open,
		/// <summary>
		/// A close message was sent to the remote endpoint (but a close message was not received from them yet)
		/// </summary>
		CloseSent,
		/// <summary>
		/// A close message was received from the remote endpoint (but a close message was not sent to them yet).
		/// </summary>
		CloseReceived,
		/// <summary>
		/// Indicates the WebSocket close handshake completed gracefully (we received and sent a close message).
		/// </summary>
		Closed,
		/// <summary>
		/// An error occurred, such as connection loss or an unexpected exception, causing a non-graceful WebSocket close.
		/// </summary>
		Errored
	}
}
