using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	public static class EnumHelper
	{
		/// <summary>
		/// Returns a string that lists all the flag values matched by the enum.
		/// </summary>
		/// <param name="e">Enum to get flag values for.</param>
		/// <returns></returns>
		public static string GetAllMatchedFlagsStr(this Enum e)
		{
			return string.Join(", ", GetAllMatchedFlags(e));
		}
		/// <summary>
		/// Returns all the flag values matched by the enum.
		/// </summary>
		/// <param name="e">Enum to get flag values for.</param>
		/// <returns></returns>
		public static IEnumerable<Enum> GetAllMatchedFlags(this Enum e)
		{
			foreach (Enum value in Enum.GetValues(e.GetType()))
				if (e.HasFlag(value))
					yield return value;
		}
	}
}
