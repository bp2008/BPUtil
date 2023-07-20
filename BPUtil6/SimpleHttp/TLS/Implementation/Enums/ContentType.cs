namespace BPUtil.SimpleHttp.TLS.Implementation
{
	public enum ContentType : byte
	{
		change_cipher_spec = 20,
		alert = 21,
		handshake = 22,
		application_data = 23
	}
}
