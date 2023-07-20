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
		[Obsolete("Use NativeWin.WinConsole instead.", false)]
		public static void AllocateConsole()
		{
			if (!Platform.IsUnix())
				AllocConsole();
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool AllocConsole();


		/// <summary>
		/// Escapes backslashes and double-quotation marks by prepending backslashes.
		/// </summary>
		/// <param name="str">Unescaped string.</param>
		/// <param name="wrapInDoubleQuotes">If true, the return value will be wrapped in double quotes.</param>
		/// <returns>A string suitable to be used as a command line argument.</returns>
		public static string EscapeCommandLineArgument(string str, bool wrapInDoubleQuotes = false)
		{
			string dqWrap = wrapInDoubleQuotes ? "\"" : "";
			return dqWrap + str.Replace("\\", "\\\\").Replace("\"", "\\\"") + dqWrap;
		}
	}
}
