using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BPUtil
{
	public static class Extensions
	{
		/// <summary>
		/// Returns a collection of all successful matches.  The collection is empty if there are no successful match objects.
		/// </summary>
		/// <param name="rx"></param>
		/// <param name="input"></param>
		/// <returns></returns>
		public static IEnumerable<Match> GetMatches(this Regex rx, string input)
		{
			Match m = rx.Match(input);
			while (m.Success)
			{
				yield return m;
				m = m.NextMatch();
			}
		}
	}
}
