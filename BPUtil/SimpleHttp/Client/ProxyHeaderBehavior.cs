namespace BPUtil.SimpleHttp.Client
{
	/// <summary>
	/// Defines which behavior the ProxyClient will exhibit when dealing with a specific proxy-related HTTP header.
	/// </summary>
	public enum ProxyHeaderBehavior
	{
		/// <summary>
		/// (Safe) Do not send the header.
		/// </summary>
		[Description("(Safe) Do not send the header.")]
		Drop = 0,
		/// <summary>
		/// (Safe) Dynamically generate the value for this header, dropping whatever our client sent.
		/// </summary>
		[Description("(Safe) Dynamically generate the value for this header, dropping whatever our client sent.")]
		Create = 1,
		/// <summary>
		/// (UNSAFE)  Combine our dynamically generated value with existing values sent by our client, according to web standards.  Be aware the client could spoof the header value.
		/// </summary>
		[Description("(UNSAFE) Combine our dynamically generated value with existing values sent by our client, according to web standards.  Be aware the client could spoof the header value.")]
		CombineUnsafe = 2,
		/// <summary>
		/// (USE WITH CARE)  Only if the client is connected from a trusted IP address, combine our dynamically generated value with existing values sent by our client, according to web standards.  Be aware the client could spoof the header value.  If the client is untrusted, we'll use our dynamically generated value.
		/// </summary>
		[Description("(USE WITH CARE) Only if the client is connected from a trusted IP address, combine our dynamically generated value with existing values sent by our client, according to web standards.  Be aware the client could spoof the header value.  If the client is untrusted, we'll use our dynamically generated value.")]
		CombineIfTrustedElseCreate = 3,
		/// <summary>
		/// (UNSAFE) Pass through the values sent by our client without modifying them.  Be aware the client could spoof the header value.
		/// </summary>
		[Description("(UNSAFE) Pass through the values sent by our client without modifying them.  Be aware the client could spoof the header value.")]
		PassthroughUnsafe = 4,
		/// <summary>
		/// (USE WITH CARE) Only if the client is connected from a trusted IP address, pass through the values sent by our client without modifying them.  If the client is untrusted, we'll drop the header.
		/// </summary>
		[Description("(USE WITH CARE) Only if the client is connected from a trusted IP address, pass through the values sent by our client without modifying them.  If the client is untrusted, we'll drop the header.")]
		PassthroughIfTrustedElseDrop = 5,
		/// <summary>
		/// (USE WITH CARE) Only if the client is connected from a trusted IP address, pass through the values sent by our client without modifying them.  If the client is untrusted, we'll use our dynamically generated value.
		/// </summary>
		[Description("(USE WITH CARE) Only if the client is connected from a trusted IP address, pass through the values sent by our client without modifying them.  If the client is untrusted, we'll use our dynamically generated value.")]
		PassthroughIfTrustedElseCreate = 6
	}
}
