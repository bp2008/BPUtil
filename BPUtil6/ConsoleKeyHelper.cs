using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	public static class ConsoleKeyHelper
	{
		private static Type consoleKeyEnumType;
		static ConsoleKeyHelper()
		{
			if (Platform.IsUnix())
				consoleKeyEnumType = typeof(Linux.KeyCode);
			else
				consoleKeyEnumType = typeof(ConsoleKey);
		}
		/// <summary>
		/// Gets the name of the specified key, either using the System.ConsoleKey enum or the BPUtil.Linux.KeyCode enum as the source of names, depending on the current platform.
		/// </summary>
		/// <param name="keyCode"></param>
		/// <returns></returns>
		public static string GetKeyName(int keyCode)
		{
			return Enum.GetName(consoleKeyEnumType, keyCode);
		}
	}
}
