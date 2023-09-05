using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// Offers helper methods for working with Tasks.
	/// </summary>
	public static class TaskHelper
	{
#if !NET6_0
		/// <summary>A task that has already completed successfully.</summary>
		private static Task s_completedTask;
#endif

		/// <summary>Gets a task that has already completed successfully.</summary>
		/// <remarks>May not always return the same instance.</remarks>        
		public static Task CompletedTask
		{
			get
			{
#if NET6_0
				return Task.CompletedTask;
#else
				Task completedTask = s_completedTask;
				if (completedTask == null)
					s_completedTask = completedTask = Task.Delay(0);
				return completedTask;
#endif
			}
		}
		private static SimpleThreadPool synchronousOperationPool = new SimpleThreadPool("Sync I/O from Async Method", 0, 256, 5000);
		/// <summary>
		/// <para>It is unsafe to run blocking code on a thread that is used for async/await.  Slowdowns and deadlocks can occur.</para>
		/// <para>This RunBlockingCodeSafely method can execute blocking I/O and other blocking code safely because it is executed on a background thread not from the standard .NET Thread Pool.</para>
		/// <para>DO NOT use async/await within the action.  Doing so will likely cause the remainder of the action to be executed on a standard .NET Thread Pool thread, which reintroduces the possibility of deadlocks.</para>
		/// </summary>
		/// <param name="action">Action to run containing blocking calls.</param>
		/// <param name="cancellationToken">Cancellation Token.  If you cancel this operation, we'll stop waiting for the action to complete.  If the action has already been started, it will continue executing normally.</param>
		/// <returns></returns>
		public static async Task RunBlockingCodeSafely(Action action, CancellationToken cancellationToken = default)
		{
			Exception actionException = null;
			SemaphoreSlim waitForFinish = new SemaphoreSlim(0, 1);
			synchronousOperationPool.Enqueue(() =>
			{
				try
				{
					cancellationToken.ThrowIfCancellationRequested();
					action();
				}
				catch (Exception ex)
				{
					actionException = ex;
				}
				finally
				{
					waitForFinish.Release();
				}
			});
			await waitForFinish.WaitAsync(cancellationToken).ConfigureAwait(false);
			actionException?.Rethrow();
		}
		/// <summary>
		/// <para>It is unsafe to run blocking code on a thread that is used for async/await.  Slowdowns and deadlocks can occur.</para>
		/// <para>This RunBlockingCodeSafely method can execute blocking I/O and other blocking code safely because it is executed on a background thread not from the standard .NET Thread Pool.</para>
		/// <para>DO NOT use async/await within the action.  Doing so will likely cause the remainder of the action to be executed on a standard .NET Thread Pool thread, which reintroduces the possibility of deadlocks.</para>
		/// </summary>
		/// <param name="action">Action to run containing blocking calls.</param>
		/// <param name="cancellationToken">Cancellation Token.  If you cancel this operation, we'll stop waiting for the action to complete.  If the action has already been started, it will continue executing normally.</param>
		/// <returns></returns>
		public static async Task<T> RunBlockingCodeSafely<T>(Func<T> action, CancellationToken cancellationToken = default)
		{
			Exception actionException = null;
			SemaphoreSlim waitForFinish = new SemaphoreSlim(0, 1);
#pragma warning disable IDE0034 // Simplify 'default' expression (VS 2017 requires the un-simplified syntax)
			T result = default(T);
#pragma warning restore IDE0034 // Simplify 'default' expression
			synchronousOperationPool.Enqueue(() =>
			{
				try
				{
					cancellationToken.ThrowIfCancellationRequested();
					result = action();
				}
				catch (Exception ex)
				{
					actionException = ex;
				}
				finally
				{
					waitForFinish.Release();
				}
			});
			await waitForFinish.WaitAsync(cancellationToken).ConfigureAwait(false);
			actionException?.Rethrow();
			return result;
		}
		/// <summary>
		/// <para>Safely runs an async function from within a synchronous context and waits for the async code to finish.</para>
		/// </summary>
		/// <param name="func">Function to run containing async calls.</param>
		/// <returns></returns>
		public static void RunAsyncCodeSafely(Func<Task> func)
		{
			// The caller could alternatively just do this to run its own func.
			Task.Run(() => func()).GetAwaiter().GetResult();
		}
		/// <summary>
		/// <para>Safely runs an async function from within a synchronous context and waits for the async code to finish.</para>
		/// </summary>
		/// <param name="func">Function to run containing async calls.</param>
		/// <param name="cancellationToken">Cancellation Token to be passed into the function.</param>
		/// <returns></returns>
		public static void RunAsyncCodeSafely(Func<CancellationToken, Task> func, CancellationToken cancellationToken = default)
		{
			Task.Run(() => func(cancellationToken)).GetAwaiter().GetResult();
		}
		/// <summary>
		/// <para>Safely runs an async function from within a synchronous context and waits for the async code to finish.</para>
		/// </summary>
		/// <param name="func">Function to run containing async calls.</param>
		/// <returns></returns>
		public static T RunAsyncCodeSafely<T>(Func<Task<T>> func)
		{
			return Task.Run(() => func()).GetAwaiter().GetResult();
		}
		/// <summary>
		/// <para>Safely runs an async function from within a synchronous context and waits for the async code to finish.</para>
		/// </summary>
		/// <param name="func">Function to run containing async calls.</param>
		/// <param name="cancellationToken">Cancellation Token to be passed into the function.</param>
		/// <returns></returns>
		public static T RunAsyncCodeSafely<T>(Func<CancellationToken, Task<T>> func, CancellationToken cancellationToken = default)
		{
			return Task.Run(() => func(cancellationToken)).GetAwaiter().GetResult();
		}
		/// <summary>
		/// Awaits the given task or throws <see cref="OperationCanceledException"/> if the task does not complete within the specified time period.
		/// </summary>
		/// <param name="task">The task to await.</param>
		/// <param name="timeoutMilliseconds">Timeout in milliseconds.</param>
		/// <returns>The task which was passed in.</returns>
		/// <exception cref="OperationCanceledException">If the task does not complete within the specified time period.</exception>
		public static async Task DoWithTimeout(Task task, int timeoutMilliseconds)
		{
			if (timeoutMilliseconds <= 0)
				throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds), "The timeout must be greater than 0 milliseconds.");
			Task completedTask = await Task.WhenAny(task, Task.Delay(timeoutMilliseconds)).ConfigureAwait(false);
			if (completedTask != task)
				throw new OperationCanceledException("The operation has timed out.");
		}
		/// <summary>
		/// Returns the result of the given task or throws <see cref="OperationCanceledException"/> if the task does not complete within the specified time period.
		/// </summary>
		/// <typeparam name="T">Type of result.</typeparam>
		/// <param name="task">The task to await.</param>
		/// <param name="timeoutMilliseconds">Timeout in milliseconds.</param>
		/// <returns>The result of the given task.</returns>
		/// <exception cref="OperationCanceledException">If the task does not complete within the specified time period.</exception>
		public static async Task<T> GetWithTimeout<T>(Task<T> task, int timeoutMilliseconds)
		{
			if (timeoutMilliseconds <= 0)
				throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds), "The timeout must be greater than 0 milliseconds.");
			Task completedTask = await Task.WhenAny(task, Task.Delay(timeoutMilliseconds)).ConfigureAwait(false);
			if (completedTask == task)
				return task.GetAwaiter().GetResult();
			throw new OperationCanceledException("The operation has timed out.");
		}

		/// <summary>
		/// Synchronously waits until the given condition returns true.  Returns true of the condition returned true, false if the operation timed out or was cancelled.
		/// </summary>
		/// <param name="condition">Function which should eventually return true.</param>
		/// <param name="timeoutMilliseconds">Maximum number of milliseconds to wait for <paramref name="condition"/> to return true.</param>
		/// <param name="waiter">(Optional) A WaitProgressivelyLonger instance that determines how long to delay between calls to <paramref name="condition"/>.  If null, uses <c>WaitProgressivelyLonger.Exponential(64, 2, 1)</c>.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns>Returns true of the condition returned true, false if the operation timed out or was cancelled.</returns>
		public static bool WaitUntilSync(Func<bool> condition, int timeoutMilliseconds, WaitProgressivelyLonger waiter = null, CancellationToken cancellationToken = default)
		{
			if (waiter == null)
				waiter = WaitProgressivelyLonger.Exponential(64, 2, 1);
			CountdownStopwatch timeout = CountdownStopwatch.StartNew(TimeSpan.FromMilliseconds(timeoutMilliseconds));
			while (!timeout.Finished && !cancellationToken.IsCancellationRequested)
			{
				if (condition())
					return true;
				Thread.Sleep((int)Math.Min(timeout.RemainingMilliseconds, waiter.GetNextTimeout()));
			}
			return condition();
		}

		/// <summary>
		/// Asynchronously waits until the given condition returns true.  Throws <see cref="OperationCanceledException"/> if the operation times out or is cancelled.
		/// </summary>
		/// <param name="condition">Function which should eventually return true.</param>
		/// <param name="timeoutMilliseconds">Maximum number of milliseconds to wait for <paramref name="condition"/> to return true.</param>
		/// <param name="waiter">(Optional) A WaitProgressivelyLonger instance that determines how long to delay between calls to <paramref name="condition"/>.  If null, uses <c>WaitProgressivelyLonger.Exponential(64, 2, 1)</c>.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns>A task that completes when the <paramref name="condition"/> returns true.</returns>
		public static async Task WaitUntilAsync(Func<bool> condition, int timeoutMilliseconds, WaitProgressivelyLonger waiter = null, CancellationToken cancellationToken = default)
		{
			if (waiter == null)
				waiter = WaitProgressivelyLonger.Exponential(64, 2, 1);
			Task timeoutTask = Task.Delay(timeoutMilliseconds, cancellationToken);
			while (!condition())
			{
				Task completedTask = await Task.WhenAny(timeoutTask, waiter.WaitAsync(cancellationToken)).ConfigureAwait(false);
				if (completedTask == timeoutTask)
					throw new OperationCanceledException("A timeout expired while waiting for a condition to be met.");
				cancellationToken.ThrowIfCancellationRequested();
			}
		}
	}
}
