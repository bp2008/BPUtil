using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.Linux.InputListener
{
	public static class LinuxInputHelper
	{
		/// <summary>
		/// Gets the names of keyboard input devices.  May return null if there was an error.
		/// </summary>
		/// <returns></returns>
		public static string[] GetKeyboardInputNames()
		{
			string getKeyboardInputNamesCommand = "grep -E  'Handlers|EV=' /proc/bus/input/devices | grep -B1 'EV=120013' | grep -Eo 'event[0-9]+'";
			int exitCode = ProcessRunner.RunProcessAndWait("/bin/bash", "-c \"" + getKeyboardInputNamesCommand + "\"", out string std, out string err);
			if (exitCode != 0)
				return null;
			return std.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s=>s.Trim()).ToArray();
		}
	}
}
