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
#if !NET6_0_OR_GREATER
		/// <summary>A task that has already completed successfully.</summary>
		private static Task s_completedTask;
#endif

		/// <summary>Gets a task that has already completed successfully.</summary>
		/// <remarks>May not always return the same instance.</remarks>        
		public static Task CompletedTask
		{
			get
			{
#if NET6_0_OR_GREATER
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
		/// <param name="doIfCancelled">(Optional) Method to call if the timeout occurs before the task completes.</param>
		/// <returns>The task which was passed in.</returns>
		/// <exception cref="OperationCanceledException">If the task does not complete within the specified time period.</exception>
		public static async Task DoWithTimeout(Task task, int timeoutMilliseconds, Func<Task> doIfCancelled = null)
		{
			if (timeoutMilliseconds <= 0)
				throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds), "The timeout must be greater than 0 milliseconds.");
			Task completedTask = await Task.WhenAny(task, Task.Delay(timeoutMilliseconds)).ConfigureAwait(false);
			if (completedTask != task)
			{
				if (doIfCancelled != null)
					await doIfCancelled().ConfigureAwait(false);
				else
					throw new OperationCanceledException("TaskHelper.DoWithTimeout: The operation has timed out.");
			}
			else if (completedTask.IsFaulted)
				await completedTask; // This will throw the exception if the task faulted.
		}
		/// <summary>
		/// Returns the result of the given task or throws <see cref="OperationCanceledException"/> if the task does not complete within the specified time period.
		/// </summary>
		/// <typeparam name="T">Type of result.</typeparam>
		/// <param name="task">The task to await.</param>
		/// <param name="timeoutMilliseconds">Timeout in milliseconds.</param>
		/// <param name="doIfCancelled">(Optional) Method to call if the timeout occurs before the task completes.</param>
		/// <returns>The result of the given task.</returns>
		/// <exception cref="OperationCanceledException">If the task does not complete within the specified time period.</exception>
		public static async Task<T> GetWithTimeout<T>(Task<T> task, int timeoutMilliseconds, Func<Task<T>> doIfCancelled = null)
		{
			if (timeoutMilliseconds <= 0)
				throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds), "The timeout must be greater than 0 milliseconds.");
			Task completedTask = await Task.WhenAny(task, Task.Delay(timeoutMilliseconds)).ConfigureAwait(false);
			if (completedTask != task)
			{
				if (doIfCancelled != null)
					return await doIfCancelled().ConfigureAwait(false);
				else
					throw new OperationCanceledException("TaskHelper.GetWithTimeout: The operation has timed out.");
			}
			else if (completedTask.IsFaulted)
				await completedTask; // This will throw the exception if the task faulted.
			return task.GetAwaiter().GetResult();
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
				else if (completedTask.IsFaulted)
					await completedTask; // This will throw the exception if the task faulted.
				cancellationToken.ThrowIfCancellationRequested();
			}
		}
		/// <summary>
		/// Awaits with cancellation an async Task that does not natively support cancellation.
		/// </summary>
		/// <param name="task">A Task which has already been started.  This should do your async work.</param>
		/// <param name="cancellationToken">Cancellation Token to observe for cancellation.</param>
		/// <param name="doIfCancelled">(Optional) Method to call if cancellation occurs before the task completes.</param>
		/// <returns></returns>
		/// <exception cref="OperationCanceledException">If the task is canceled and <paramref name="doIfCancelled"/> is not provided.</exception>
		public static async Task DoWithCancellation(Task task, CancellationToken cancellationToken, Func<Task> doIfCancelled = null)
		{
			TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
			using (cancellationToken.Register(() => taskCompletionSource.TrySetResult(true)))
			{
				Task completedTask = await Task.WhenAny(task, taskCompletionSource.Task).ConfigureAwait(false);
				if (completedTask != task)
				{
					if (doIfCancelled != null)
						await doIfCancelled().ConfigureAwait(false);
					else
						throw new OperationCanceledException("TaskHelper.DoWithCancellation: The operation was cancelled.");
				}
				else if (completedTask.IsFaulted)
					await completedTask; // This will throw the exception if the task faulted.
			}
		}
		/// <summary>
		/// Awaits with cancellation an async Task that does not natively support cancellation.
		/// </summary>
		/// <param name="task">A Task which has already been started.  This should do your async work.</param>
		/// <param name="timeoutMilliseconds">Number of milliseconds to wait for the task to complete before triggering cancellation.  Must be greater than 0.</param>
		/// <param name="cancellationToken">Cancellation Token to observe for cancellation.</param>
		/// <param name="doIfCancelled">(Optional) Method to call if cancellation or the timeout occurs before the task completes.  This method receives a bool argument indicating if the cancellation was due to the timeout (<paramref name="timeoutMilliseconds"/>).</param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException">If <paramref name="timeoutMilliseconds"/> is not greater than 0.</exception>
		/// <exception cref="OperationCanceledException">If the task is canceled and <paramref name="doIfCancelled"/> is not provided.</exception>
		public static async Task DoWithCancellation(Task task, int timeoutMilliseconds, CancellationToken cancellationToken, Func<bool, Task> doIfCancelled = null)
		{
			if (timeoutMilliseconds <= 0)
				throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds), nameof(timeoutMilliseconds) + " must be > 0.");

			TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
			using (CancellationTokenSource ctsTimeout = new CancellationTokenSource(timeoutMilliseconds))
			using (CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ctsTimeout.Token))
			using (cts.Token.Register(() => taskCompletionSource.TrySetResult(true)))
			{
				Task completedTask = await Task.WhenAny(task, taskCompletionSource.Task).ConfigureAwait(false);
				if (completedTask != task)
				{
					if (doIfCancelled != null)
						await doIfCancelled(ctsTimeout.IsCancellationRequested).ConfigureAwait(false);
					else
						throw new OperationCanceledException("TaskHelper.DoWithCancellation: The operation " + (ctsTimeout.IsCancellationRequested ? "timed out." : "was cancelled."));
				}
				else if (completedTask.IsFaulted)
					await completedTask; // This will throw the exception if the task faulted.
			}
		}
		/// <summary>
		/// <para>Runs an action that performs a synchronous operation, but does it on a background thread.</para>
		/// <para>Returns a SynchronousOperationHandle that can be waited on to block until the action has completed.</para>
		/// <para>This method is meant to be called from a synchronous context.</para>
		/// </summary>
		/// <param name="action">Action to run on a background thread.</param>
		/// <returns></returns>
		public static SynchronousOperationHandle RunSynchronousOperationOnBackgroundThread(Action action)
		{
			SynchronousOperationHandle handle = new SynchronousOperationHandle();
			synchronousOperationPool.Enqueue(() =>
			{
				try
				{
					action();
				}
				catch (Exception ex)
				{
					handle.Error = ex;
				}
				finally
				{
					handle.End();
				}
			});
			return handle;
		}
		/// <summary>
		/// An object that allows you to check the status of a synchronous operation executing on a background thread, and wait for the result.
		/// </summary>
		public class SynchronousOperationHandle
		{
			private EventWaitHandle ewh = new EventWaitHandle(false, EventResetMode.ManualReset);
			/// <summary>
			/// Gets the error that was thrown by the operation, or null if none was thrown.
			/// </summary>
			public Exception Error { get; internal set; } = null;
			/// <summary>
			/// Gets a value indicating if the operation has ended.
			/// </summary>
			public bool Ended => ewh.WaitOne(0);
			/// <summary>
			/// Marks the SynchronousOperation as ended.
			/// </summary>
			public void End()
			{
				ewh.Set();
			}
			/// <summary>
			/// Waits indefinitely for the operation to end, and if an exception occurred during execution of the operation, the exeption is thrown by this method.
			/// </summary>
			/// <returns></returns>
			public void WaitUntilEnded()
			{
				ewh.WaitOne();
				if (Error != null)
					throw Error;
			}
			/// <summary>
			/// Waits indefinitely for the operation to end.  Does not throw an exception if one occurred during execution of the operation.  See <see cref="Error"/>.
			/// </summary>
			/// <returns></returns>
			public void WaitUntilEndedNoThrow()
			{
				ewh.WaitOne();
			}
			/// <summary>
			/// Waits for the operation to end, with a timeout. Returns true if the operation ended, false if the timeout was reached.  If an exception occurred during execution of the operation, the exeption is thrown by this method.
			/// </summary>
			/// <returns></returns>
			public bool WaitUntilEnded(TimeSpan timeout)
			{
				bool result = ewh.WaitOne(timeout);
				if (Error != null)
					throw Error;
				return result;
			}
			/// <summary>
			/// Waits for the operation to end, with a timeout. Returns true if the operation ended, false if the timeout was reached.  Does not throw an exception if one occurred during execution of the operation.  See <see cref="Error"/>.
			/// </summary>
			/// <returns></returns>
			public bool WaitUntilEndedNoThrow(TimeSpan timeout)
			{
				return ewh.WaitOne(timeout);
			}
		}
	}
}
