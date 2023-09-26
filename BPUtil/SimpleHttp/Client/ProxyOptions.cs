using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp.Client
{

	/// <summary>
	/// Provides options to ProxyClient and the advanced  <see cref="HttpProcessor.ProxyToAsync(string, ProxyOptions)"/> method.
	/// </summary>
	public class ProxyOptions
	{
		private static long counter = 0;

		/// <summary>
		/// Unique identifier for this request. Counter starts at 0 each time the program is launched.
		/// </summary>
		public readonly long RequestId = Interlocked.Increment(ref counter);
		private int _connectTimeoutMs = 15000;
		/// <summary>
		/// <para>[Default: 15000] The connection timeout, in milliseconds.</para>
		/// <para>Clamped to the range [1000, 60000].</para>
		/// <para>This timeout applies only to the Connect operation (when connecting to the destination server to faciliate proxying).</para>
		/// </summary>
		public int connectTimeoutMs
		{
			get
			{
				return _connectTimeoutMs;
			}
			set
			{
				_connectTimeoutMs = value.Clamp(1000, 60000);
			}
		}
		private int _networkTimeoutMs = 60000;
		/// <summary>
		/// <para>[Default: 60000] The send and receive timeout to set for both TcpClients (incoming and outgoing), in milliseconds.</para>
		/// <para>Clamped to the range [1000, 600000].</para>
		/// <para>This timeout applies to:</para>
		/// <para>* Reading the HTTP request body from the client.</para>
		/// <para>* Reading the HTTP response header from the destination server.</para>
		/// <para>* All other proxy operations that send data on a network socket.</para>
		/// <para>If a destination sometimes has slow time-to-first-byte, you may need to increase this timeout.</para>
		/// <para>This timeout does not apply when reading a response body or WebSocket data because these actions often sit idle for extended periods of time.</para>
		/// </summary>
		public int networkTimeoutMs
		{
			get
			{
				return _networkTimeoutMs;
			}
			set
			{
				_networkTimeoutMs = value.Clamp(1000, 600000);
			}
		}
		/// <summary>
		/// [Default: false] If true, certificate validation will be disabled for outgoing https connections.
		/// </summary>
		public bool acceptAnyCert = false;
		/// <summary>
		/// [Default: null] If non-null, proxied communication will be copied into this object so you can snoop on it.
		/// </summary>
		public ProxyDataBuffer snoopy = null;
		/// <summary>
		/// [Default: null] The value of the host header, also used in SSL authentication. If null or whitespace, it is set from the [newUrl] parameter.
		/// </summary>
		public string host = null;
		/// <summary>
		/// [Default: null] Optional event timer for collecting timing data.
		/// </summary>
		public BasicEventTimer bet = null;
		/// <summary>
		/// [Default: true] If true, then this proxy utility will gracefully handle connection failure by responding with "504 Gateway Timeout".  If false, an exception will be thrown upon gateway timeout.
		/// </summary>
		public bool allowGatewayTimeoutResponse = true;
		/// <summary>
		/// [Default: true] Disable this if the server you're proxying to does not handle "Connection: keep-alive" properly.  For example, some servers may use "Connection: keep-alive" without providing any means to know when the response is completed, which can cause the proxy request to fail.  Similarly, some web servers may associate user/session data with a connection such that the proxied site could malfunction or leak data.  It is slower, but safer, to disable [allowConnectionKeepalive].
		/// </summary>
		public bool allowConnectionKeepalive = true;

		/// <summary>
		/// A StringBuilder suitable for logging operations for one Proxy request.
		/// </summary>
		public StringBuilder log = new StringBuilder();

		/// <summary>
		/// If true, a "Server-Timing" header will added to the response including proxy timing details. If a <see cref="bet"/> instance is not provided, one will be automatically created.
		/// </summary>
		public bool includeServerTimingHeader = false;
		/// <summary>
		/// An event that is raised before response headers are proxied from our client to the remote server, allowing for those headers to be viewed or modified.
		/// </summary>
		public event EventHandler<HttpProcessor> BeforeRequestHeadersSent = delegate { };
		/// <summary>
		/// An event that is raised before response headers are sent to from the remote server to our client, allowing for those headers to be viewed or modified.
		/// </summary>
		public event EventHandler<HttpProcessor> BeforeResponseHeadersSent = delegate { };
		/// <summary>
		/// Defines how to handle the "X-Forwarded-For" header.  Default: Drop.
		/// </summary>
		public ProxyHeaderBehavior xForwardedFor = ProxyHeaderBehavior.Drop;
		/// <summary>
		/// Defines how to handle the "X-Forwarded-Host" header.  Default: Drop.
		/// </summary>
		public ProxyHeaderBehavior xForwardedHost = ProxyHeaderBehavior.Drop;
		/// <summary>
		/// Defines how to handle the "X-Forwarded-Proto" header.  Default: Drop.
		/// </summary>
		public ProxyHeaderBehavior xForwardedProto = ProxyHeaderBehavior.Drop;
		/// <summary>
		/// Defines how to handle the "X-Real-Ip" header.  Default: Drop.
		/// </summary>
		public ProxyHeaderBehavior xRealIp = ProxyHeaderBehavior.Drop;
		/// <summary>
		/// The whitelist of IPAddressRange strings which the client IP must belong to in order to be trusted (see <see cref="ProxyHeaderBehavior"/>).
		/// </summary>
		public string[] proxyHeaderTrustedIpRanges;
		/// <summary>
		/// Cancellation Token.
		/// </summary>
		public CancellationToken cancellationToken = default;
		/// <summary>
		/// Domain names found in proxy response bodies will be replaced using keys and values from this list.  For each pair, Key is replaced with Value.
		/// </summary>
		public List<KeyValuePair<string, string>> responseHostnameSubstitutions = null;
		/// <summary>
		/// Regular Expression replacement will be performed on proxy response bodies using patterns and replacement strings from this list.  For each pair, Key is the pattern and Value is the replacement string.
		/// </summary>
		public List<KeyValuePair<string, string>> responseRegexReplacements = null;
		/// <summary>
		/// Gets a value indicating if this options instance is currently configured to fully buffer HTTP response bodies for processing before the body is transmitted to the client.  Doing this increases system resource usage and can add latency to affected requests.
		/// </summary>
		public bool requiresFullResponseBuffering =>
			(responseHostnameSubstitutions != null && responseHostnameSubstitutions.Count > 0)
			|| (responseRegexReplacements != null && responseRegexReplacements.Count > 0);

		/// <summary>
		/// Raises the BeforeRequestHeadersSent event.
		/// </summary>
		/// <param name="sender">Reference to the object which is raising the event.</param>
		/// <param name="processor">HttpProcessor which may be modified by those subscribing to the event.</param>
		internal void RaiseBeforeRequestHeadersSent(object sender, HttpProcessor processor)
		{
			try
			{
				BeforeRequestHeadersSent(sender, processor);
			}
			catch (Exception ex)
			{
				Logger.Debug(ex, "ProxyOptions.RaiseBeforeRequestHeadersSent");
			}
		}
		/// <summary>
		/// Raises the BeforeResponseHeadersSent event.
		/// </summary>
		/// <param name="sender">Reference to the object which is raising the event.</param>
		/// <param name="processor">HttpProcessor which may be modified by those subscribing to the event.</param>
		internal void RaiseBeforeResponseHeadersSent(object sender, HttpProcessor processor)
		{
			try
			{
				BeforeResponseHeadersSent(sender, processor);
			}
			catch (Exception ex)
			{
				Logger.Debug(ex, "ProxyOptions.RaiseBeforeResponseHeadersSent");
			}
		}
	}
}
