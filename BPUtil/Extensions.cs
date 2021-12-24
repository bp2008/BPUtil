using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

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
		/// <summary>
		/// Returns the exception messages from this exception and all inner exceptions in a text-only tree format. Stack traces are not included.
		/// </summary>
		/// <param name="ex">Exception to extract messages from.</param>
		/// <returns></returns>
		public static string FlattenMessages(this Exception ex)
		{
			StringBuilder sb = new StringBuilder();
			FlattenMessages(ex, sb, 0);
			return sb.ToString();
		}
		private static void FlattenMessages(Exception ex, StringBuilder sb, int level)
		{
			if (ex == null)
				return;
			if (level > 0)
			{
				sb.AppendLine();
				sb.Append(new string('\t', level)).Append('→');
			}
			sb.Append(ex.Message);
			if (ex is AggregateException) // ex may be, or inherit from, AggregateException
			{
				AggregateException agg = ex as AggregateException;
				if (agg.InnerExceptions != null)
					foreach (Exception inner in agg.InnerExceptions)
						FlattenMessages(ex.InnerException, sb, level + 1);
			}
			else
				FlattenMessages(ex.InnerException, sb, level + 1);
		}
		#endregion
		#region IEnumerable<string>
		/// <summary>
		/// Returns true if the collection contains the specified string.
		/// </summary>
		/// <param name="collection"></param>
		/// <param name="str"></param>
		/// <param name="ignoreCase"></param>
		/// <returns></returns>
		public static bool Contains(this IEnumerable<string> collection, string str, bool ignoreCase)
		{
			return collection.Any(s =>
			{
				if (s == str)
					return true;
				if (ignoreCase && str != null && s != null)
					return s.Equals(str, StringComparison.OrdinalIgnoreCase);
				return false;
			});

		}
		#endregion
		#region System.Drawing.Point
		/// <summary>
		/// Returns the distance between this point and another point.
		/// </summary>
		/// <param name="point">This point.</param>
		/// <param name="otherPoint">Second point from which to calculate distance.</param>
		/// <returns></returns>
		public static double DistanceFrom(this Point point, Point otherPoint)
		{
			int A = point.X - otherPoint.X;
			int B = point.Y - otherPoint.Y;
			return Math.Sqrt((A * A) + (B * B));
		}
		#endregion
		#region System.Windows.Forms.Form
		/// <summary>
		/// Sets the location of the form to be near the mouse pointer, preferably not directly on top of the mouse pointer, but entirely on-screen if possible.
		/// </summary>
		/// <param name="form">The form.</param>
		public static void SetLocationNearMouse(this Form form)
		{
			int offset = 10;
			int x = 0, y = 0;
			Point cursor = Cursor.Position;
			Screen screen = Screen.FromPoint(cursor);
			Rectangle workspace = screen.WorkingArea;
			Point centerScreen = new Point(workspace.X + (workspace.Width / 2), workspace.Y + (workspace.Height / 2));

			// Position the form near the cursor, extending away from the cursor toward the center of the screen.
			if (cursor.X <= centerScreen.X)
			{
				if (cursor.Y <= centerScreen.Y)
				{
					// Upper-left quadrant
					x = cursor.X + offset;
					y = cursor.Y + offset;
				}
				else
				{
					// Lower-left quadrant
					x = cursor.X + offset;
					y = cursor.Y - offset - form.Height;
				}
			}
			else
			{
				if (cursor.Y <= centerScreen.Y)
				{
					// Upper-right quadrant
					x = cursor.X - offset - form.Width;
					y = cursor.Y + offset;
				}
				else
				{
					// Lower-right quadrant
					x = cursor.X - offset - form.Width;
					y = cursor.Y - offset - form.Height;
				}
			}

			// Screen bounds check.  Keep form entirely within this screen if possible, but ensure that the top left corner is visible if all else fails.
			if (x >= workspace.X + (workspace.Width - form.Width))
				x = workspace.X + (workspace.Width - form.Width);
			if (x < workspace.X)
				x = workspace.X;
			if (y >= workspace.Y + (workspace.Height - form.Height))
				y = workspace.Y + (workspace.Height - form.Height);
			if (y < workspace.Y)
				y = workspace.Y;

			// Assign location
			form.StartPosition = FormStartPosition.Manual;
			form.Location = new Point(x, y);
		}
		#endregion
		#region System.Windows.Forms.ProgressBar
		/// <summary>
		/// FROM: https://derekwill.com/2014/06/24/combating-the-lag-of-the-winforms-progressbar/
		/// Sets the progress bar value, without using 'Windows Aero' animation.
		/// This is to work around a known WinForms issue where the progress bar 
		/// is slow to update. 
		/// </summary>
		public static void SetProgressNoAnimation(this ProgressBar pb, int value)
		{
			// To get around the progressive animation, we need to move the 
			// progress bar backwards.
			if (value == pb.Maximum)
			{
				// Special case as value can't be set greater than Maximum.
				pb.Maximum = value + 1;     // Temporarily Increase Maximum
				pb.Value = value + 1;       // Move past
				pb.Maximum = value;         // Reset maximum
			}
			else
			{
				pb.Value = value + 1;       // Move past
			}
			pb.Value = value;               // Move to correct value
		}
		#endregion
	}
}
