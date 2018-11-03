using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.Linux.InputListener
{
	public enum KeyEventType
	{
		/// <summary>
		/// This event was not recognized as a key event.
		/// </summary>
		NotKeyEvent,
		/// <summary>
		/// Raised when a key is pressed.
		/// </summary>
		Down,
		/// <summary>
		/// Raised when a key is released.
		/// </summary>
		Up,
		/// <summary>
		/// Raised repeatedly on a short interval when a key has been held down for some time. In a text editor, this would cause the key to be entered again as if it were another Down event.
		/// </summary>
		AutoRepeat
	}
}
