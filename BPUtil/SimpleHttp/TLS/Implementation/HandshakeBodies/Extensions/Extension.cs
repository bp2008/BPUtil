using BPUtil.IO;
using System;

namespace BPUtil.SimpleHttp.TLS.Implementation.HandshakeBodies.Extensions
{
	public abstract class Extension
	{
		public ExtensionType Type;
		public ushort Length;
		public byte[] ExtensionBody;
		protected Extension()
		{
		}
		private void LoadFromStream(ExtensionType type, IDataStream stream)
		{
			Type = type;
			Length = stream.ReadUInt16();
			ExtensionBody = stream.ReadNBytes(Length);
			Create();
		}
		/// <summary>
		/// This is called after <see cref="Type"/>, <see cref="Length"/>, and <see cref="ExtensionBody"/> have been read from the stream.
		/// </summary>
		protected abstract void Create();

		public static Extension Read(IDataStream stream)
		{
			Extension extension;

			ExtensionType type = (ExtensionType)stream.ReadUInt16();
			switch (type)
			{
				case ExtensionType.Server_Name:
					extension = new ServerNameExtension();
					break;
				default:
					extension = new DefaultExtension();
					break;

			}

			extension.LoadFromStream(type, stream);

			return extension;
		}
	}
}
