using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	public static class ConsoleAppHelper
	{
		/// <summary>
		/// Creates a console window.  Use this once at startup, if desired, if your application does not normally 
		/// allocate a console window (as configured in Project Properties - Application - Output type).
		/// 
		/// Call this on Windows only.
		/// </summary>
		public static void AllocateConsole()
		{
			AllocConsole();
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool AllocConsole();
	}
}
