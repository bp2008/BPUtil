namespace BPUtil.SimpleHttp.Client
{
	/// <summary>
	/// Defines which behavior the ProxyClient will exhibit when dealing with a specific proxy-related HTTP header.
	/// </summary>
	public enum ProxyHeaderBehavior
	{
		/// <summary>
		/// Do not send the header.
		/// </summary>
		Drop = 0,
		/// <summary>
		/// Create our own value for this header, dropping whatever our client sent.
		/// </summary>
		Create = 1,
		/// <summary>
		/// Combine our values with existing values sent by our client, according to standards.
		/// </summary>
		CombineUnsafe = 2,
		/// <summary>
		/// If our client has a trusted IP address, Combine, otherwise Create.
		/// </summary>
		CombineIfTrustedElseCreate = 3,
		/// <summary>
		/// Pass through the values sent by our client without modifying them.
		/// </summary>
		PassthroughUnsafe = 4,
		/// <summary>
		/// If our client has a trusted IP address, Passthrough, otherwise Drop.
		/// </summary>
		PassthroughIfTrustedElseDrop = 5,
		/// <summary>
		/// If our client has a trusted IP address, Passthrough, otherwise Create.
		/// </summary>
		PassthroughIfTrustedElseCreate = 6
	}
}
