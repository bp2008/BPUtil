using System;
using System.Threading;

namespace BPUtil
{
	/// <summary>
	/// Limits how many callers can hold access concurrently.
	/// </summary>
	public sealed class ConcurrentAccessLimiter : IDisposable
	{
		private readonly SemaphoreSlim semaphore;
		private bool isDisposed;

		/// <summary>
		/// Initializes a new instance of the <see cref="ConcurrentAccessLimiter"/> class.
		/// </summary>
		/// <param name="maxConcurrentOperations">Maximum number of concurrent operations allowed.</param>
		public ConcurrentAccessLimiter(int maxConcurrentOperations)
		{
			if (maxConcurrentOperations <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(maxConcurrentOperations), "Value must be greater than zero.");
			}

			semaphore = new SemaphoreSlim(maxConcurrentOperations, maxConcurrentOperations);
		}

		/// <summary>
		/// Attempts to acquire access and returns a disposable handle that releases access when disposed.  If access cannot be acquired, a TimeoutException is thrown.
		/// </summary>
		/// <param name="timeout">
		/// Optional amount of time to wait for access. When <see langword="null"/>, acquisition is attempted immediately.
		/// </param>
		/// <returns>
		/// A disposable handle that releases access when disposed.
		/// </returns>
		/// <exception cref="TimeoutException">Thrown when access cannot be acquired before the timeout expires.</exception>
		/// <exception cref="ObjectDisposedException">Thrown when the limiter has been disposed.</exception>
		public IDisposable GetDisposableAccess(TimeSpan? timeout = null)
		{
			ThrowIfDisposed();

			bool acquired = timeout.HasValue
				? semaphore.Wait(timeout.Value)
				: semaphore.Wait(0);

			if (!acquired)
			{
				throw new TimeoutException("Could not obtain concurrent access.");
			}

			return new AccessHandle(semaphore);
		}

		/// <summary>
		/// Executes the provided action after acquiring access, then automatically releases access.
		/// </summary>
		/// <param name="action">Action to execute after access is acquired.</param>
		/// <param name="timeout">
		/// Optional amount of time to wait for access. When <see langword="null"/>, acquisition is attempted immediately.
		/// </param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="action"/> is null.</exception>
		/// <exception cref="TimeoutException">Thrown when access cannot be acquired before the timeout expires.</exception>
		/// <exception cref="ObjectDisposedException">Thrown when the limiter has been disposed.</exception>
		public void RunWithAccess(Action action, TimeSpan? timeout = null)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}

			using (GetDisposableAccess(timeout))
			{
				action();
			}
		}

		/// <summary>
		/// Releases managed resources used by the limiter.
		/// </summary>
		public void Dispose()
		{
			if (isDisposed)
			{
				return;
			}

			semaphore.Dispose();
			isDisposed = true;
		}

		private void ThrowIfDisposed()
		{
			if (isDisposed)
			{
				throw new ObjectDisposedException(nameof(ConcurrentAccessLimiter));
			}
		}

		private sealed class AccessHandle : IDisposable
		{
			private readonly SemaphoreSlim semaphore;
			private int isReleased;

			public AccessHandle(SemaphoreSlim semaphore)
			{
				this.semaphore = semaphore;
			}

			public void Dispose()
			{
				if (Interlocked.Exchange(ref isReleased, 1) == 1)
				{
					return;
				}

				semaphore.Release();
			}
		}
	}
}
