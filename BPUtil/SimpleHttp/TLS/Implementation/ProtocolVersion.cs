namespace BPUtil.SimpleHttp.TLS.Implementation
{
	public struct ProtocolVersion
	{
		public byte major;
		public byte minor;

		public bool IsSupported()
		{
			return major == 3 && (minor == 1 || minor == 2 || minor == 3);
		}
		public override string ToString()
		{
			return major + "." + minor;
		}
	}
}
