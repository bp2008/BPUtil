using BPUtil.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp.TLS.Implementation.HandshakeBodies.Extensions
{
	public class ServerNameExtension : Extension
	{
		public string[] ServerNames;
		protected override void Create()
		{
			List<string> serverNamesList = new List<string>();
			using (MemoryDataStream mds = new MemoryDataStream(ExtensionBody))
			{
				while (mds.Position < mds.Length)
				{
					ushort listEntryLength = mds.ReadUInt16();
					if (listEntryLength > 0)
					{
						byte[] listEntryBody = mds.ReadNBytes(listEntryLength);
						byte listEntryType = listEntryBody[0];
						if (listEntryType == 0) // DNS hostname
						{
							ushort hostnameLength = ByteUtil.ReadUInt16(listEntryBody, 1);
							byte[] hostnameBody = ByteUtil.SubArray(listEntryBody, 3, hostnameLength);
							serverNamesList.Add(Encoding.ASCII.GetString(hostnameBody));
						}
					}
				}
			}
			ServerNames = serverNamesList.ToArray();
		}
	}
}
