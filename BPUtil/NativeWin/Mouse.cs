using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.NativeWin
{
	/// <summary>
	/// Offers mouse-related methods.
	/// </summary>
	public static class Mouse
	{
		/// <summary>
		/// Sets the mouse position to a screen coordinate.
		/// </summary>
		/// <param name="x">Screen coordinate X</param>
		/// <param name="y">Screen coordinate Y</param>
		/// <returns></returns>
		[DllImport("User32.Dll")]
		public static extern long SetCursorPos(int x, int y);


		[DllImport("user32.dll")]
		private static extern void mouse_event(Int32 dwFlags, Int32 dx, Int32 dy, Int32 dwData, UIntPtr dwExtraInfo);
		private const int MOUSEEVENTF_MOVE = 0x0001;

		/// <summary>
		/// Moves the mouse cursor the specified amount. This method is capable of waking a monitor from sleep.
		/// </summary>
		/// <param name="dx">X pixel offset from current position.</param>
		/// <param name="dy">Y pixel offset from current position.</param>
		public static void MoveCursor(int dx, int dy)
		{
			mouse_event(MOUSEEVENTF_MOVE, dx, dy, 0, UIntPtr.Zero);
		}
	}
}
