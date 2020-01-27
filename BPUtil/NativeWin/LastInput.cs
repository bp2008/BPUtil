using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.NativeWin
{
	public static class LastInput
	{
		/// <summary>
		/// Returns the approxmate age in milliseconds of the last user input.
		/// </summary>
		/// <returns>The approxmate age in milliseconds of the last user input.</returns>
		public static uint GetLastInputAgeMs()
		{
			LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
			lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
			lastInputInfo.dwTime = 0;


			if (GetLastInputInfo(ref lastInputInfo))
			{
				uint lastInput = lastInputInfo.dwTime;
				uint now = (uint)Environment.TickCount;
				uint idleTime = now - lastInput;
				if (idleTime > 0)
					return idleTime;
			}

			return 0;
		}
		/// <summary>
		/// Returns the approxmate time when the last user input occurred.
		/// </summary>
		/// <returns>The approxmate time when the last user input occurred.</returns>
		public static DateTime GetLastInputTime()
		{
			return DateTime.Now - TimeSpan.FromMilliseconds(GetLastInputAgeMs());
		}

		[DllImport("user32.dll")]
		static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
	}

	[StructLayout(LayoutKind.Sequential)]
	struct LASTINPUTINFO
	{
		public static readonly int SizeOf = Marshal.SizeOf(typeof(LASTINPUTINFO));

		[MarshalAs(UnmanagedType.U4)]
		public UInt32 cbSize;
		[MarshalAs(UnmanagedType.U4)]
		public UInt32 dwTime;
	}
}
