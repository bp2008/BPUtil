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
		/// <returns></returns>
		public static string ToDHMS(TimeSpan span)
		{
			StringBuilder sb = new StringBuilder();
			if (span.Days > 0)
				sb.Append(span.Days).Append("d");
			if (sb.Length > 0 || span.Hours > 0)
				sb.Append(span.Hours).Append("h");
			if (sb.Length > 0 || span.Minutes > 0)
				sb.Append(span.Minutes).Append("m");
			sb.Append(span.Seconds).Append("s");
			if (span.Ticks < 0)
				return "-" + sb.ToString();
			return sb.ToString();
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
