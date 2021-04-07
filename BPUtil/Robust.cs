using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// Provides methods to conveniently retry unreliable operations (typically file I/O).
	/// </summary>
	public static class Robust
	{
		/// <summary>
		/// Runs the specified action. If the action throws an exception, this method sleeps for a time and runs the action again. Sleep time is defined by the [delays] arguments, and the maximum number of retries is defined by the number of [delays] arguments.
		/// </summary>
		/// <param name="action">Action to run.</param>
		/// <param name="delays">
		/// <para>
		/// After failing to run an action, we sleep the thread for this many milliseconds. Each int from this array is used one time.
		/// </para>
		/// <para>
		/// Example: Given a [delays] array of [5,10,20], the Retry method will wait 5ms after the first failure, 10ms after the second failure, 20ms after the third failure, and if the action fails a fourth time, the exception is simply rethrown.
		/// </para>
		/// </param>
		public static void Retry(Action action, params int[] delays)
		{
			foreach (int ms in delays)
			{
				try
				{
					action();
					return;
				}
				catch
				{
					Thread.Sleep(ms);
				}
			}
			// This will be the final attempt to run the action, and we will not swallow exceptions.
			// It will also be the only attempt if the [delays] argument was empty.
			action();
		}
		/// <summary>
		/// Runs the specified action. If the action throws an exception, this method sleeps for a time and runs the action again. Sleep time is defined by the [sleepTimeMs] argument, and the maximum number of retries is defined by the [maxRetries] argument.
		/// </summary>
		/// <param name="action">Action to run.</param>
		/// <param name="sleepTimeMs">Time in milliseconds to sleep after the action fails.</param>
		/// <param name="maxRetries">Maximum number of times to retry.</param>
		public static void RetryPeriodic(Action action, int sleepTimeMs, int maxRetries)
		{
			for (int i = 0; i < maxRetries; i++)
			{
				try
				{
					action();
					return;
				}
				catch
				{
					Thread.Sleep(sleepTimeMs);
				}
			}
			// This will be the final attempt to run the action, and we will not swallow exceptions.
			action();
		}
	}
}
