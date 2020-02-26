using System.IO;

namespace BPUtil.SimpleHttp.TLS.Implementation
{
	/// <summary>
	/// Represents one TLS record fragment.
	/// </summary>
	public class TLSPlaintext
	{
		/// <summary>
		/// The header of the record layer fragment.
		/// </summary>
		public byte[] data_header;

		/// <summary>
		/// The body of the record layer fragment.
		/// </summary>
		public byte[] data_fragment;

		public ContentType type;
		public ProtocolVersion version;

		public TLSPlaintext() { }
		public TLSPlaintext(Stream stream)
		{
			data_header = ByteUtil.ReadNBytes(stream, 5);
			type = (ContentType)data_header[0];
			version = new ProtocolVersion()
			{
				major = data_header[1],
				minor = data_header[2]
			};
			ushort length = ByteUtil.ReadUInt16(data_header, 3);
			data_fragment = ByteUtil.ReadNBytes(stream, length);
		}
	}
}
