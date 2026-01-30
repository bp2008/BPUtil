using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// Utility methods involving threads.
	/// </summary>
	public static class ThreadUtil
	{
		/// <summary>
		/// Runs the specified actions concurrently, using as many threads as there are actions. If any action throws an exception, it will be propagated to the caller.
		/// </summary>
		/// <param name="actions">Actions to run concurrently.</param>
		public static void RunConcurrently(params Action[] actions)
		{
			ParallelOptions parallelOptions = new ParallelOptions();
			parallelOptions.MaxDegreeOfParallelism = actions.Length;
			Parallel.ForEach(actions, parallelOptions, a =>
			{
				a();
			});
		}
		/// <summary>
		/// <para>Sleeps the current thread until the target time, then returns a TimeSpan indicating how much time was waited, or TimeSpan.Zero if no time was waited.</para>
		/// <para>If the returned TimeSpan is less than or equal to TimeSpan.Zero, it indicates that the target time was in the past and no sleep occurred.</para>
		/// <para>The thread wakes every 60 seconds to check if the target time has been reached, in case the system clock changes.</para>
		/// </summary>
		/// <param name="targetTime">The target time to sleep until.</param>
		/// <returns>The TimeSpan that was waited.</returns>
		public static TimeSpan SleepUntil(DateTime targetTime)
		{
			Stopwatch sw = new Stopwatch();
			TimeSpan timeToWait = targetTime - DateTime.Now;
			if (timeToWait > TimeSpan.Zero)
			{
				sw.Start();
				while (timeToWait > TimeSpan.Zero)
				{
					Thread.Sleep((int)Math.Min(60000, (long)timeToWait.TotalMilliseconds));
					timeToWait = targetTime - DateTime.Now;
				}
			}
			return sw.Elapsed;
		}
	}
}
