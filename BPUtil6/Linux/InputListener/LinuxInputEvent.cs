using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.Linux.InputListener
{
	public class LinuxInputEvent
	{
		/// <summary>
		/// (This is likely wrong) Seconds component of the time since the unix epoch.
		/// </summary>
		public uint seconds;
		/// <summary>
		/// (This is likely wrong) Microseconds component of the time since the unix epoch.
		/// </summary>
		public uint microseconds;
		/// <summary>
		/// Event type.  For key events this should always have the same value, 1.
		/// </summary>
		public ushort type;
		/// <summary>
		/// For key events, this is the key code.
		/// </summary>
		public ushort code;
		/// <summary>
		/// For key events, 0 is keyup, 1 is keydown, 2 is autorepeat.
		/// </summary>
		public uint value;
		public LinuxInputEvent() { }
		public LinuxInputEvent(Stream s)
		{
			seconds = ByteUtil.ReadUInt32LE(s);
			microseconds = ByteUtil.ReadUInt32LE(s);
			type = ByteUtil.ReadUInt16LE(s);
			code = ByteUtil.ReadUInt16LE(s);
			value = ByteUtil.ReadUInt32LE(s);
		}
	}
}
