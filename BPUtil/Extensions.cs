using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
#if NETFRAMEWORK || NET6_0_WIN
using System.Windows.Forms;
#endif

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
						FlattenMessages(inner, sb, level + 1);
			}
			else
				FlattenMessages(ex.InnerException, sb, level + 1);
		}
		/// <summary>
		/// Returns a string representation of the exception using an indented hierarchical format such that each inner exception is indented for easier readability.
		/// </summary>
		/// <param name="ex">Exception to print in indented hierarchical format.</param>
		/// <returns></returns>
		public static string ToHierarchicalString(this Exception ex)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(ex.GetType().ToString());
			if (ex.Message != null)
				sb.Append(": " + ex.Message);

			if (ex is AggregateException)
			{
				AggregateException agg = ex as AggregateException;
				if (agg.InnerExceptions != null)
				{
					sb.AppendLine();
					sb.AppendLine("Inner Exceptions:").AppendLine("[");
					bool first = true;
					foreach (Exception inner in agg.InnerExceptions)
					{
						if (!first)
							sb.AppendLine(",");
						first = false;
						sb.Append(StringUtil.Indent("{" + Environment.NewLine + StringUtil.Indent(inner.ToHierarchicalString()) + Environment.NewLine + "}"));
					}
					sb.AppendLine();
					sb.AppendLine("]");
				}
			}
			else if (ex.InnerException != null)
			{
				sb.AppendLine();
				sb.AppendLine("Inner Exception:").AppendLine("{" + Environment.NewLine + StringUtil.Indent(ex.InnerException.ToHierarchicalString()) + Environment.NewLine + "}");
			}
			if (!string.IsNullOrWhiteSpace(ex.StackTrace))
			{
				if (sb[sb.Length - 1] != '\r' && sb[sb.Length - 1] != '\n')
					sb.AppendLine();
				sb.Append(ex.StackTrace.Trim('\r', '\n'));
			}
			return sb.ToString();
		}
		/// <summary>
		/// Sets the stack trace for this Exception.
		/// </summary>
		/// <param name="target">The Exception</param>
		/// <param name="stack">The stack trace to assign to the Exception.</param>
		/// <returns></returns>
		public static Exception SetStackTrace(this Exception target, StackTrace stack) => _SetStackTrace(target, stack);

		/// <summary>
		/// Returns a function that efficiently sets an exception's stack trace. From https://stackoverflow.com/a/63685720/814569
		/// </summary>
		private static readonly Func<Exception, StackTrace, Exception> _SetStackTrace = new Func<Func<Exception, StackTrace, Exception>>(() =>
		{
			ParameterExpression target = Expression.Parameter(typeof(Exception));
			ParameterExpression stack = Expression.Parameter(typeof(StackTrace));
			Type traceFormatType = typeof(StackTrace).GetNestedType("TraceFormat", BindingFlags.NonPublic);
			MethodInfo toString = typeof(StackTrace).GetMethod("ToString", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { traceFormatType }, null);
			object normalTraceFormat = Enum.GetValues(traceFormatType).GetValue(0);
			MethodCallExpression stackTraceString = Expression.Call(stack, toString, Expression.Constant(normalTraceFormat, traceFormatType));
			FieldInfo stackTraceStringField = typeof(Exception).GetField("_stackTraceString", BindingFlags.NonPublic | BindingFlags.Instance);
			BinaryExpression assign = Expression.Assign(Expression.Field(target, stackTraceStringField), stackTraceString);
			return Expression.Lambda<Func<Exception, StackTrace, Exception>>(Expression.Block(assign, target), target, stack).Compile();
		})();
		/// <summary>
		/// Rethrows this caught exception in its original state.  This exists because the "throw;" command in some cases fails to restore the correct state.
		/// </summary>
		/// <param name="ex">The exception to be rethrown.</param>
		public static void Rethrow(this Exception ex)
		{
			System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex).Throw();
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
		#region System.IO.FileInfo
		/// <summary>
		/// Returns the file name without its extension.  If the file name has no traditional extension such as "config" or ".gitignore" then the entire file name is returned.
		/// </summary>
		/// <param name="fi"></param>
		/// <returns></returns>
		public static string NameWithoutExtension(this System.IO.FileInfo fi)
		{
			if (fi.Name.Length <= fi.Extension.Length || fi.Extension.Length == 0)
				return fi.Name;
			return fi.Name.Remove(fi.Name.Length - fi.Extension.Length);
		}
		/// <summary>
		/// Returns [FullName] with its extension removed.  If the file name has no traditional extension such as "config" or ".gitignore" then the entire file name is returned.
		/// </summary>
		/// <param name="fi"></param>
		/// <returns></returns>
		public static string FullNameWithoutExtension(this System.IO.FileInfo fi)
		{
			if (fi.Name.Length <= fi.Extension.Length || fi.Extension.Length == 0)
				return fi.FullName;
			return fi.FullName.Remove(fi.FullName.Length - fi.Extension.Length);
		}
		#endregion
#if NETFRAMEWORK || NET6_0_WIN
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
		/// <summary>
		/// If the center of the form is not visible on any of the screens, moves the form to the screen whose center is closest to the form in its old position.
		/// </summary>
		/// <param name="form">The form to move.</param>
		public static void MoveOnscreenIfOffscreen(this Form form)
		{
			// Check if the center of the form is visible on any of the screens
			bool formIsVisible = false;
			Point formCenter = new Point(form.Left + form.Width / 2, form.Top + form.Height / 2);
			foreach (Screen screen in Screen.AllScreens)
			{
				if (screen.WorkingArea.Contains(formCenter))
				{
					formIsVisible = true;
					break;
				}
			}

			// If the center of the form is not visible, move it to the screen whose center is closest to the form in its old position
			if (!formIsVisible)
			{
				Screen closestScreen = Screen.AllScreens[0];
				double minDistance = double.MaxValue;
				foreach (Screen screen in Screen.AllScreens)
				{
					Point screenCenter = new Point(screen.WorkingArea.Left + screen.WorkingArea.Width / 2, screen.WorkingArea.Top + screen.WorkingArea.Height / 2);
					double distance = Math.Sqrt(Math.Pow(screenCenter.X - formCenter.X, 2) + Math.Pow(screenCenter.Y - formCenter.Y, 2));
					if (distance < minDistance)
					{
						minDistance = distance;
						closestScreen = screen;
					}
				}
				int x = closestScreen.WorkingArea.Left + (closestScreen.WorkingArea.Width - form.Width) / 2;
				int y = closestScreen.WorkingArea.Top + (closestScreen.WorkingArea.Height - form.Height) / 2;
				form.Location = new Point(x, y);
			}
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
#endif
		#region String
		/// <summary>
		/// Case-insensitive equals. Shorthand for Equals(other, StringComparison.OrdinalIgnoreCase).
		/// </summary>
		/// <param name="str">This string.</param>
		/// <param name="other">String to compare with.</param>
		/// <returns></returns>
		public static bool IEquals(this string str, string other)
		{
			return str.Equals(other, StringComparison.OrdinalIgnoreCase);
		}
		/// <summary>
		/// Case-insensitive contains. Shorthand for .IndexOf(other, StringComparison.OrdinalIgnoreCase) > -1.
		/// </summary>
		/// <param name="str">This string.</param>
		/// <param name="other">String to compare with.</param>
		/// <returns></returns>
		public static bool IContains(this string str, string other)
		{
			return str.IndexOf(other, StringComparison.OrdinalIgnoreCase) > -1;
		}
		/// <summary>
		/// Determines whether the beginning of this string instance matches the specified string when compared without case-sensitivity. IStartsWith is shorthand for this.StartsWith(value, StringComparison.OrdinalIgnoreCase).
		/// </summary>
		/// <param name="str">This string.</param>
		/// <param name="value">The string to find at the start of this string.</param>
		/// <returns></returns>
		public static bool IStartsWith(this string str, string value)
		{
			return str.StartsWith(value, StringComparison.OrdinalIgnoreCase);
		}
		/// <summary>
		/// Determines whether the end of this string instance matches the specified string when compared without case-sensitivity. IEndsWith is shorthand for this.EndsWith(value, StringComparison.OrdinalIgnoreCase).
		/// </summary>
		/// <param name="str">This string.</param>
		/// <param name="value">The string to find at the start of this string.</param>
		/// <returns></returns>
		public static bool IEndsWith(this string str, string value)
		{
			return str.EndsWith(value, StringComparison.OrdinalIgnoreCase);
		}
		#endregion
		#region HashSet<T>
		/// <summary>
		/// Adds the specified elements to the HashSet.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="set">This HashSet</param>
		/// <param name="objects">Collection of objects to add to the HashSet.</param>
		/// <returns>Returns the number of objects that were added (not counting objects that already existed in the set).</returns>
		public static int AddRange<T>(this HashSet<T> set, IEnumerable<T> objects)
		{
			int added = 0;
			foreach (T o in objects)
				if (set.Add(o))
					added++;
			return added;
		}
		#endregion
	}
}
