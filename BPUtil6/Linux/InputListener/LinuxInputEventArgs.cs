using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.Linux.InputListener
{
	public class LinuxInputEventArgs : EventArgs
	{
		public readonly LinuxInputEvent RawEventData;
		public LinuxInputEventType Type
		{
			get
			{
				return (LinuxInputEventType)RawEventData.type;
			}
		}
		/// <summary>
		/// Event code (e.g. Key Code)
		/// </summary>
		public ushort Code
		{
			get
			{
				return RawEventData.code;
			}
		}
		/// <summary>
		/// Returns the type of key event, or NotKeyEvent if this event was not recognized as a key event.
		/// </summary>
		public KeyEventType KeyEventType
		{
			get
			{
				if (Type == LinuxInputEventType.Key)
				{
					if (RawEventData.value == 0)
						return KeyEventType.Up;
					else if (RawEventData.value == 1)
						return KeyEventType.Down;
					else if (RawEventData.value == 2)
						return KeyEventType.AutoRepeat;
				}
				return KeyEventType.NotKeyEvent;
			}
		}
		public LinuxInputEventArgs(LinuxInputEvent e)
		{
			this.RawEventData = e;
		}
	}
}
