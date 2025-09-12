using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// Provides methods to conveniently retry unreliable operations (such as file I/O).
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
		/// <para>Runs the specified action up to <c>[maxRetries] + 1</c> times or until the action does not throw an exception. If the action throws an exception, this method sleeps for a time and runs the action again. Sleep time is defined by the [sleepTimeMs] argument, and the maximum number of retries is defined by the [maxRetries] argument.</para>
		/// <para>After [maxRetries] are exhausted, if the action still throws an exception, the exception will not be caught by this method.</para>
		/// </summary>
		/// <param name="action">Action to run.</param>
		/// <param name="sleepTimeMs">Time in milliseconds to sleep after the action fails.</param>
		/// <param name="maxRetries">
		///	<para>
		/// Maximum number of times to retry.
		/// </para>
		/// <para>
		/// Example: Given a [maxRetries] of 2, the RetryPeriodic method will wait after the first two failures, and if the action fails a third time, the exception is thrown to the caller.
		/// </para>
		/// </param>
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
		/// <summary>
		/// <para>Runs the specified action. If the action throws an exception, this method sleeps for a time and runs the action again. Sleep time is defined by the [sleepTimeMs] argument, and the maximum number of retries is defined by the [maxRetries] argument.</para>
		/// <para>After [maxRetries] are exhausted, if the action still throws an exception, the exception will not be caught by this method.</para>
		/// </summary>
		/// <param name="action">Action to run.</param>
		/// <param name="sleepTimeMs">Time in milliseconds to sleep after the action fails.</param>
		/// <param name="maxRetries">
		///	<para>
		/// Maximum number of times to retry.
		/// </para>
		/// <para>
		/// Example: Given a [maxRetries] of 2, the RetryPeriodicAsync method will wait after the first two failures, and if the action fails a third time, the exception is thrown to the caller.
		/// </para>
		/// </param>
		/// <param name="cancellationToken">Cancellation Token</param>
		public static async Task RetryPeriodicAsync(Func<Task> action, int sleepTimeMs, int maxRetries, CancellationToken cancellationToken = default)
		{
			for (int i = 0; i < maxRetries; i++)
			{
				try
				{
					if (cancellationToken.IsCancellationRequested)
						break;
					await action().ConfigureAwait(false);
					return;
				}
				catch
				{
					await Task.Delay(sleepTimeMs, cancellationToken).ConfigureAwait(false);
				}
			}
			cancellationToken.ThrowIfCancellationRequested();
			// This will be the final attempt to run the action, and we will not swallow exceptions.
			await action().ConfigureAwait(false);
		}
		/// <summary>
		/// Runs the specified action. If the result is unacceptable or if the action throws an exception, this method sleeps for a time and runs the action again. Sleep time is defined by the [delays] arguments, and the maximum number of retries is defined by the number of [delays] arguments.
		/// </summary>
		/// <param name="action">Action to run.</param>
		/// <param name="until">Function that should return true when the desired result is achieved. If this method returns false, the action may be run again.</param>
		/// <param name="delays">
		/// <para>
		/// After failing to run an action, we sleep the thread for this many milliseconds. Each int from this array is used one time.
		/// </para>
		/// <para>
		/// Example: Given a [delays] array of [5,10,20], the Retry method will wait 5ms after the first failure, 10ms after the second failure, 20ms after the third failure, and if the action fails a fourth time, the exception is simply rethrown.
		/// </para>
		/// </param>
		public static void Retry(Action action, Func<bool> until, params int[] delays)
		{
			foreach (int ms in delays)
			{
				try
				{
					action();
					if (until())
						return;
				}
				catch { }
				Thread.Sleep(ms);
			}
			// This will be the final attempt to run the action, and we will not swallow exceptions.
			// It will also be the only attempt if the [delays] argument was empty.
			action();
		}
		/// <summary>
		/// Runs the specified function which returns a value. If the result is unacceptable or if the action throws an exception, this method sleeps for a time and runs the action again. Sleep time is defined by the [delays] arguments, and the maximum number of retries is defined by the number of [delays] arguments.
		/// </summary>
		/// <param name="func">Function to run.</param>
		/// <param name="until">Function that should return true when the desired result is achieved. If this method returns false, the function may be run again.</param>
		/// <param name="delays">
		/// <para>
		/// After failing to run an action, we sleep the thread for this many milliseconds. Each int from this array is used one time.
		/// </para>
		/// <para>
		/// Example: Given a [delays] array of [5,10,20], the Retry method will wait 5ms after the first failure, 10ms after the second failure, 20ms after the third failure, and if the action fails a fourth time, the exception is simply rethrown.
		/// </para>
		/// </param>
		public static T Retry<T>(Func<T> func, Func<T, bool> until, params int[] delays)
		{
			foreach (int ms in delays)
			{
				try
				{
					T result = func();
					if (until(result))
						return result;
				}
				catch { }
				Thread.Sleep(ms);
			}
			// This will be the final attempt to run the function, and we will not swallow exceptions.
			// It will also be the only attempt if the [delays] argument was empty.
			return func();
		}
		/// <summary>
		/// Runs the specified action. Each time the action fails (throws an exception), the caller will be given the option to cancel. Sleep time is defined by the [delays] arguments.
		/// </summary>
		/// <param name="action">Action to run.</param>
		/// <param name="shouldCancel">If the action fails (throws an exception), [shouldCancel] is called.  If [shouldCancel] returns true, the exception is rethrown. If [shouldCancel] returns false, the thread sleeps and the action is called again. The [shouldCancel] function is passed the exception that occurred.</param>
		/// <param name="delays">
		/// <para>
		/// After failing to run an action, if cancellation is not requested, we sleep the thread for this many milliseconds. Each int from this array is used one time until we reach the end of the array.  When we reach the last int in the array, the last int is used for all following sleeps.
		/// </para>
		/// <para>
		/// Example: Given a [delays] array of [5,10,20], the RetryUntilCancelled method will wait 5ms after the first failure, 10ms after the second failure, 20ms for all following failures.  Repeated execution stops only if [shouldCancel] returns true.
		/// </para></param>
		public static void RetryUntilCancelled(Action action, Func<Exception, bool> shouldCancel, params int[] delays)
		{
			if (delays.Length == 0)
				delays = new int[1];
			int i = 0;
			int max = delays.Length - 1;
			while (true)
			{
				try
				{
					action();
					return;
				}
				catch (Exception ex)
				{
					bool cancel;
					try
					{
						cancel = shouldCancel(ex);
					}
					catch (Exception ex2)
					{
						throw new AggregateException(ex2, ex);
					}
					if (cancel)
						throw;
					Thread.Sleep(delays[i]);
					if (i < max)
						i++;
				}
			}
		}
	}
}
