using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// Static class offering the ability to show a message to the user either on the current console or in a GUI message box, or both depending on availability.  Currently, the user won't see messages if the current process is not interactive, but this may be upgraded in the future if needed to show messages on the desktop from a background process.
	/// </summary>
	public static class BPMessage
	{
		private static EConsole c = EConsole.I;
		/// <summary>
		/// <para>Shows an error message by printing it in red text on the console, and attempting to open a modal dialog box (this method may block until the user clicks "OK").</para>
		/// <para>WARNING: Exact blocking behavior is subject to change in the future.</para>
		/// </summary>
		/// <param name="message"></param>
		public static void ShowError(string message)
		{
			c.RedLine(message);
#if NETFRAMEWORK || NET6_0_WIN
			if (Environment.UserInteractive)
				System.Windows.Forms.MessageBox.Show(message);
#endif
		}
	}
}
