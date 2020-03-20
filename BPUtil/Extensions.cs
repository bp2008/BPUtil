using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BPUtil
{
	public static class Extensions
	{
		#region Regex
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
		#endregion
		#region HttpHeaders
		/// <summary>
		/// Returns the first value of the specified header, or null.
		/// </summary>
		/// <param name="headers"></param>
		/// <param name="name">Name of the header.</param>
		/// <returns>The first value of the specified header, or null.</returns>
		public static string GetFirstValue(this HttpHeaders headers, string name)
		{
			if (headers.TryGetValues(name, out IEnumerable<string> values))
				foreach (string value in values)
					return value;
			return null;
		}
		/// <summary>
		/// Returns the first value of the specified header interpreted as a 32-bit integer, or the fallback value.
		/// </summary>
		/// <param name="headers"></param>
		/// <param name="name">Name of the header.</param>
		/// <param name="fallbackValue">Value to return if the header does not exist or its value can't be parsed.</param>
		/// <returns>The first value of the specified header interpreted as a 32-bit integer, or the fallback value.</returns>
		public static int GetIntValue(this HttpHeaders headers, string name, int fallbackValue = -1)
		{
			if (int.TryParse(headers.GetFirstValue(name), out int value))
				return value;
			return fallbackValue;
		}
		/// <summary>
		/// Returns the first value of the specified header interpreted as a 64-bit integer, or the fallback value.
		/// </summary>
		/// <param name="headers"></param>
		/// <param name="name">Name of the header.</param>
		/// <param name="fallbackValue">Value to return if the header does not exist or its value can't be parsed.</param>
		/// <returns>The first value of the specified header interpreted as a 64-bit integer, or the fallback value.</returns>
		public static long GetLongValue(this HttpHeaders headers, string name, long fallbackValue = -1)
		{
			if (long.TryParse(headers.GetFirstValue(name), out long value))
				return value;
			return fallbackValue;
		}
		/// <summary>
		/// Returns the values of the specified header, or null.
		/// </summary>
		/// <param name="headers"></param>
		/// <param name="name">Name of the header.</param>
		/// <returns>The first value of the specified header, or null.</returns>
		public static string[] GetAllValues(this HttpResponseHeaders headers, string name)
		{
			if (headers.TryGetValues(name, out IEnumerable<string> values))
				return values.ToArray();
			return null;
		}
		#endregion
		#region Exception
		/// <summary>
		/// Traverses the Exception and its InnerException tree, looking for an Exception which is of the specified type, or which inherits from the specified type. If one is found, it is returned.  If none is found, null is returned.
		/// </summary>
		/// <typeparam name="T">The type of Exception to look for.</typeparam>
		/// <param name="baseException">This exception.</param>
		/// <param name="requireExactMatch">If true, the returned exception must be exactly the specified class, not a subclass of it.</param>
		/// <returns></returns>
		public static T GetExceptionOfType<T>(this Exception baseException, bool requireExactMatch = false) where T : Exception
		{
			if (requireExactMatch)
				return (T)baseException.GetExceptionWhere(ex => ex.GetType() == typeof(T));
			else
				return (T)baseException.GetExceptionWhere(ex => ex is T); // ex may be, or inherit from, T
		}
		/// <summary>
		/// Traverses the Exception and its InnerException tree, looking for an Exception which causes the given [<paramref name="where" />] method to return true. If one is found, it is returned.  If none is found, null is returned.
		/// </summary>
		/// <param name="baseException">This exception.</param>
		/// <param name="where">A function which returns true if the passed-in exception meets expectations.</param>
		/// <returns></returns>
		public static Exception GetExceptionWhere(this Exception baseException, Func<Exception, bool> where)
		{
			Exception ex = baseException;
			while (ex != null)
			{
				if (where(ex))
					return ex;
				else if (ex is AggregateException) // ex may be, or inherit from, AggregateException
				{
					AggregateException agg = ex as AggregateException;
					if (agg.InnerExceptions != null)
						foreach (Exception inner in agg.InnerExceptions)
						{
							Exception found = inner.GetExceptionWhere(where);
							if (found != null)
								return found;
						}
				}
				ex = ex.InnerException;
			}
			return null;
		}
		#endregion
	}
}
