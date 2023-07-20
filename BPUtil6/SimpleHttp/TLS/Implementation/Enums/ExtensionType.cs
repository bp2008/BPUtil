namespace BPUtil.SimpleHttp.TLS.Implementation
{
	public enum ExtensionType
	{
		Server_Name = 0x0000,
		Supported_Groups = 0x000a,
		EC_Point_Formats = 0x000b,
		Signature_Algorithms = 0x000d,
		Application_Layer_Protocol_Negotiation = 0x0010,
		Encrypt_Then_MAC = 0x0016,
		Extended_Master_Secret = 0x0017,
		Session_Ticket = 0x0023,
		Supported_Versions = 0x002b,
		PSK_Key_Exchange_Modes = 0x002d,
		Key_Share = 0x0033
	}
}
