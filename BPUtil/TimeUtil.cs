using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// Provides methods for working with JavaScript-compatible unix epoch timestamps.
	/// </summary>
	public static class TimeUtil
	{
		private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		/// <summary>
		/// Returns a DateTime object based on the specified number of milliseconds since the Unix Epoch (1970/1/1 midnight UTC).  The returned object uses the UTC timezone.
		/// </summary>
		/// <param name="ms_since_epoch">The number of milliseconds since the Unix Epoch (1970/1/1 midnight UTC).</param>
		/// <returns></returns>
		public static DateTime DateTimeFromEpochMS(long ms_since_epoch)
		{
			return UnixEpoch.AddMilliseconds(ms_since_epoch);
		}
		/// <summary>
		/// Returns the number of milliseconds since the Unix Epoch (1970/1/1 midnight UTC), calculated against the current time (UTC).
		/// </summary>
		/// <returns></returns>
		public static long GetTimeInMsSinceEpoch()
		{
			return (long)DateTime.UtcNow.Subtract(UnixEpoch).TotalMilliseconds;
		}
		/// <summary>
		/// Returns the number of milliseconds since the Unix Epoch (1970/1/1 midnight UTC) until the specified date.
		/// </summary>
		/// <param name="date">The date to calculate milliseconds for.</param>
		/// <returns>The number of milliseconds since the Unix Epoch (1970/1/1 midnight UTC).</returns>
		public static long GetTimeInMsSinceEpoch(DateTime date)
		{
			return (long)date.ToUniversalTime().Subtract(UnixEpoch).TotalMilliseconds;
		}
		/// <summary>
		/// Converts a TimeSpan to a compact unambiguous string format with precision to the second. E.g. "1d20h0m5s" or "1m30s" or "0s" or "-1m30s".
		/// </summary>
		/// <param name="span">A TimeSpan instance.</param>
		/// <param name="includeMilliseconds">(Optional; Default: false) Set true to include milliseconds.</param>
		/// <param name="padDigits">(Optional; Default: false) Set true to pad all integers to 2 digits except for the leftmost unit.</param>
		/// <returns></returns>
		public static string ToDHMS(TimeSpan span, bool includeMilliseconds = false, bool padDigits = false)
		{
			StringBuilder sb = new StringBuilder();
			bool leftmost = true;
			if (span.Days > 0)
				sb.Append(GetPadded(span.Days, padDigits, ref leftmost)).Append("d");
			if (sb.Length > 0 || span.Hours > 0)
				sb.Append(GetPadded(span.Hours, padDigits, ref leftmost)).Append("h");
			if (sb.Length > 0 || span.Minutes > 0)
				sb.Append(GetPadded(span.Minutes, padDigits, ref leftmost)).Append("m");
			sb.Append(GetPadded(span.Seconds, padDigits, ref leftmost)).Append("s");
			if (includeMilliseconds)
				sb.Append(span.Milliseconds.ToString().PadLeft(3, '0')).Append("ms");
			if (span.Ticks < 0)
				return "-" + sb.ToString();
			return sb.ToString();
		}
		private static string GetPadded(int num, bool padDigits, ref bool leftmost)
		{
			if (leftmost)
				leftmost = false;
			else if (padDigits)
				return num.ToString().PadLeft(2, '0');
			return num.ToString();
		}
		/// <summary>
		/// Converts a timespan in milliseconds to a compact unambiguous string format with precision to the second. E.g. "1d20h0m5s" or "1m30s" or "0s" or "-1m30s".
		/// </summary>
		/// <param name="timeMs">Time in milliseconds.</param>
		/// <returns></returns>
		public static string ToDHMS(long timeMs)
		{
			return ToDHMS(TimeSpan.FromMilliseconds(timeMs));
		}
	}
}
