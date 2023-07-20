using BPUtil.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp.TLS.Implementation.HandshakeBodies.Extensions
{
	/// <summary>
	/// Represents an ALPN (Application-Layer Protocol Negotiation) extension.
	/// </summary>
	public class ApplicationLayerProtocolNegotiationExtension : Extension
	{
		public string[] Protocols;
		protected override void Create()
		{
			List<string> protocolList = new List<string>();
			using (MemoryDataStream mds = new MemoryDataStream(ExtensionBody))
			{
				ushort remainingBodyLength = mds.ReadUInt16();
				if (mds.Position + remainingBodyLength != mds.Length)
					throw new Exception("TLS ALPN parsing failed due to invalid length at start of ALPN extension body.");
				while (mds.Position < mds.Length)
				{
					byte stringLength = (byte)mds.ReadByte();
					if (stringLength > 0 && mds.Position + stringLength <= mds.Length)
					{
						byte[] stringBody = mds.ReadNBytes(stringLength);
						protocolList.Add(Encoding.ASCII.GetString(stringBody));
					}
					else
						throw new Exception("TLS ALPN parsing failed due to invalid string length.");
				}
			}
			Protocols = protocolList.ToArray();
		}
		public bool isTlsAlpn01()
		{
			return Protocols?.Contains("acme-tls/1") == true;
		}
	}
}
