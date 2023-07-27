using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// Offers chainable console writing methods that make it easier to write colored text.
	/// </summary>
	public class EConsole
	{
		/// <summary>
		/// Singleton instance of the EConsole class.
		/// </summary>
		public static readonly EConsole I = new EConsole();
		private EConsole() { }
		/// <summary>
		/// Writes text using the Red color.
		/// </summary>
		/// <param name="str">Text to write.</param>
		/// <returns>This instance of EConsole so you can chain method calls.</returns>
		public EConsole Red(string str)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write(str);
			Console.ResetColor();
			return this;
		}
		/// <summary>
		/// Writes text using the Red color, followed by a line break.
		/// </summary>
		/// <param name="str">Text to write.</param>
		/// <returns>This instance of EConsole so you can chain method calls.</returns>
		public EConsole RedLine(string str)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.Write(str);
			Console.ResetColor();
			Console.WriteLine();
			return this;
		}
		/// <summary>
		/// Writes text using the Green color.
		/// </summary>
		/// <param name="str">Text to write.</param>
		/// <returns>This instance of EConsole so you can chain method calls.</returns>
		public EConsole Green(string str)
		{
			Console.ForegroundColor = ConsoleColor.Green;
			Console.Write(str);
			Console.ResetColor();
			return this;
		}
		/// <summary>
		/// Writes text using the Green color, followed by a line break.
		/// </summary>
		/// <param name="str">Text to write.</param>
		/// <returns>This instance of EConsole so you can chain method calls.</returns>
		public EConsole GreenLine(string str)
		{
			Console.ForegroundColor = ConsoleColor.Green;
			Console.Write(str);
			Console.ResetColor();
			Console.WriteLine();
			return this;
		}

		/// <summary>
		/// Writes text using the Cyan color.
		/// </summary>
		/// <param name="str">Text to write.</param>
		/// <returns>This instance of EConsole so you can chain method calls.</returns>
		public EConsole Cyan(string str)
		{
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write(str);
			Console.ResetColor();
			return this;
		}
		/// <summary>
		/// Writes text using the Cyan color, followed by a line break.
		/// </summary>
		/// <param name="str">Text to write.</param>
		/// <returns>This instance of EConsole so you can chain method calls.</returns>
		public EConsole CyanLine(string str)
		{
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.Write(str);
			Console.ResetColor();
			Console.WriteLine();
			return this;
		}

		/// <summary>
		/// Writes text using the Magenta color.
		/// </summary>
		/// <param name="str">Text to write.</param>
		/// <returns>This instance of EConsole so you can chain method calls.</returns>
		public EConsole Magenta(string str)
		{
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.Write(str);
			Console.ResetColor();
			return this;
		}
		/// <summary>
		/// Writes text using the Magenta color, followed by a line break.
		/// </summary>
		/// <param name="str">Text to write.</param>
		/// <returns>This instance of EConsole so you can chain method calls.</returns>
		public EConsole MagentaLine(string str)
		{
			Console.ForegroundColor = ConsoleColor.Magenta;
			Console.Write(str);
			Console.ResetColor();
			Console.WriteLine();
			return this;
		}

		/// <summary>
		/// Writes text using the Yellow color.
		/// </summary>
		/// <param name="str">Text to write.</param>
		/// <returns>This instance of EConsole so you can chain method calls.</returns>
		public EConsole Yellow(string str)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write(str);
			Console.ResetColor();
			return this;
		}
		/// <summary>
		/// Writes text using the Yellow color, followed by a line break.
		/// </summary>
		/// <param name="str">Text to write.</param>
		/// <returns>This instance of EConsole so you can chain method calls.</returns>
		public EConsole YellowLine(string str)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write(str);
			Console.ResetColor();
			Console.WriteLine();
			return this;
		}

		/// <summary>
		/// Writes text using the current color.
		/// </summary>
		/// <param name="str">Text to write.</param>
		/// <returns>This instance of EConsole so you can chain method calls.</returns>
		public EConsole Write(string str)
		{
			Console.Write(str);
			return this;
		}
		/// <summary>
		/// Writes text using the current color, followed by a line break.
		/// </summary>
		/// <param name="str">Text to write.</param>
		/// <returns>This instance of EConsole so you can chain method calls.</returns>
		public EConsole WriteLine(string str = "")
		{
			Console.WriteLine(str);
			return this;
		}
		/// <summary>
		/// Writes text using the current color, followed by a line break.
		/// </summary>
		/// <param name="str">Text to write.</param>
		/// <returns>This instance of EConsole so you can chain method calls.</returns>
		public EConsole Line(string str = "")
		{
			Console.WriteLine(str);
			return this;
		}
	}
}
