using BPUtil.IO;
using BPUtil.SimpleHttp.TLS.Implementation.HandshakeBodies.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp.TLS.Implementation
{
	public class ClientHello : HandshakeBody
	{
		/// <summary>
		/// The server name specified by the client. Null if not specified.
		/// </summary>
		public readonly string serverName;
		/// <summary>
		/// If true, this ClientHello specifies that the request is for TLS-ALPN-01 validation, which is part of ACME domain validation for automated certificate signing.
		/// </summary>
		public readonly bool isTlsAlpn01Validation = false;
		public ClientHello(IDataStream stream)
		{
			byte[] clientRandom = stream.ReadNBytes(32);

			byte sessionIdLength = (byte)stream.ReadByte(); // Can be 0
			byte[] sessionId = stream.ReadNBytes(sessionIdLength);

			ushort cipherSuitesLength = stream.ReadUInt16();
			byte[] cipherSuites = stream.ReadNBytes(cipherSuitesLength);

			byte compressionMethodsLength = (byte)stream.ReadByte(); // Typically 1 followed by a 0 byte indicating no compression.  TLS 1.3 does not allow compression as it is considered a vulnerability.
			byte[] compressionMethods = stream.ReadNBytes(compressionMethodsLength);

			ushort extensionsLength = stream.ReadUInt16();
			byte[] extensionsData = stream.ReadNBytes(extensionsLength);

			using (MemoryDataStream mds = new MemoryDataStream(extensionsData))
			{
				while (mds.Position < mds.Length)
				{
					Extension extension = Extension.Read(mds);
					if (extension is ServerNameExtension)
						serverName = (extension as ServerNameExtension).ServerNames.FirstOrDefault();
					else if (extension is ApplicationLayerProtocolNegotiationExtension)
						isTlsAlpn01Validation = (extension as ApplicationLayerProtocolNegotiationExtension).isTlsAlpn01();
				}
			}
		}
	}
}
