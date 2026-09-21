using System;

namespace BPUtil.SimpleHttp.WebSockets
{
	/// <summary>
	/// Identifies a phase of establishing a <see cref="WebSocketClient"/> connection.
	/// </summary>
	public enum WebSocketClientPhase
	{
		/// <summary>
		/// Resolving the host name and establishing the TCP connection.
		/// </summary>
		Connect,
		/// <summary>
		/// Performing the TLS handshake (https and wss URLs only).
		/// </summary>
		TlsHandshake,
		/// <summary>
		/// Sending the HTTP upgrade request.
		/// </summary>
		HttpUpgradeRequest,
		/// <summary>
		/// Receiving the HTTP upgrade response.
		/// </summary>
		HttpUpgradeResponse
	}
	/// <summary>
	/// Thrown by the <see cref="WebSocketClient"/> constructor when the connect or handshake does not complete within the allowed time.  The connection has been closed when this is thrown.
	/// </summary>
	public class WebSocketClientTimeoutException : TimeoutException
	{
		/// <summary>
		/// The phase which was in progress when the timeout occurred.
		/// </summary>
		public readonly WebSocketClientPhase Phase;
		/// <summary>
		/// The "host:port" string identifying the remote endpoint.
		/// </summary>
		public readonly string HostAndPort;
		/// <summary>
		/// The timeout, in milliseconds, which was exceeded.
		/// </summary>
		public readonly int TimeoutMs;
		/// <summary>
		/// Constructs a WebSocketClientTimeoutException.
		/// </summary>
		/// <param name="phase">The phase which was in progress when the timeout occurred.</param>
		/// <param name="hostAndPort">The "host:port" string identifying the remote endpoint.</param>
		/// <param name="timeoutMs">The timeout, in milliseconds, which was exceeded.</param>
		/// <param name="innerException">(Optional) The exception thrown by the socket operation that timed out or was interrupted.</param>
		public WebSocketClientTimeoutException(WebSocketClientPhase phase, string hostAndPort, int timeoutMs, Exception innerException = null)
			: base("WebSocketClient timed out during " + GetPhaseDescription(phase) + " with " + hostAndPort + " after " + timeoutMs + " ms.", innerException)
		{
			Phase = phase;
			HostAndPort = hostAndPort;
			TimeoutMs = timeoutMs;
		}
		/// <summary>
		/// Returns a short human-readable description of the phase, e.g. "TLS handshake".
		/// </summary>
		/// <param name="phase">The phase.</param>
		/// <returns>A short human-readable description of the phase.</returns>
		public static string GetPhaseDescription(WebSocketClientPhase phase)
		{
			switch (phase)
			{
				case WebSocketClientPhase.Connect:
					return "connect";
				case WebSocketClientPhase.TlsHandshake:
					return "TLS handshake";
				case WebSocketClientPhase.HttpUpgradeRequest:
					return "HTTP upgrade request";
				case WebSocketClientPhase.HttpUpgradeResponse:
					return "HTTP upgrade response";
				default:
					return phase.ToString();
			}
		}
	}
}
