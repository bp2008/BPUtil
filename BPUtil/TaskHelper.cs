using System;
using System.Collections.Generic;
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
	}
}
