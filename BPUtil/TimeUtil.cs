using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	public static class TimeUtil
	{
		private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		/// <summary>
		/// Returns a DateTime object based on the specified number of milliseconds since the Unix Epoch (1970/1/1 midnight UTC).
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
	}
}
