using System;

namespace BPUtil.SimpleHttp
{
	[Flags]
	public enum AllowedConnectionTypes
	{
		http = 0b1,
		https = 0b10,
		httpAndHttps = http | https
	}
}