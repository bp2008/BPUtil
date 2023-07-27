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
		/// <summary>
		/// Gets or sets the max command size in characters for the <c>WriteUsageCommand</c> overload that does not accept a <c>commandSize</c> argument.
		/// </summary>
		public static byte MaxCommandSize = 12;
		/// <summary>
		/// Writes a usage command with the given name and description, and a max command size of <see cref="MaxCommandSize"/>.
		/// </summary>
		/// <param name="command">Command string.</param>
		/// <param name="commandDescription">Description of the command.</param>
		public static void WriteUsageCommand(string command, string commandDescription)
		{
			WriteUsageCommand(MaxCommandSize, command, commandDescription);
		}
		/// <summary>
		/// Writes a usage command with the given name and description.
		/// </summary>
		/// <param name="commandSize">Max length of a command string (strings are padded for consistent spacing).</param>
		/// <param name="command">Command string.</param>
		/// <param name="commandDescription">Description of the command.</param>
		public static void WriteUsageCommand(int commandSize, string command, string commandDescription)
		{
			int leftMarginSize = 4 + commandSize + 2;
			int descriptionLineSize = Console.BufferWidth - leftMarginSize;
			if (descriptionLineSize < 16)
			{
				leftMarginSize = 4;
				descriptionLineSize = 256;
			}
			EConsole.I.Write("    ").Yellow(command.PadRight(commandSize, ' ')).Write("- ");

			List<string> segments = StringUtil.SplitIntoSegments(commandDescription, descriptionLineSize, true);
			if (segments.Count > 0)
			{
				EConsole.I.Line(segments[0]);
				for (int i = 1; i < segments.Count; i++)
					EConsole.I.Write(new string(' ', leftMarginSize)).Line(segments[i]);
			}
		}
	}
}
