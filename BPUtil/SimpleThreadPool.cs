using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using BPUtil.SimpleHttp;

namespace BPUtil
{
	public class SimpleThreadPool
	{
		/// <summary>
		/// A queue of actions to be performed by threads.
		/// </summary>
		WaitingQueue<Action> actionQueue = new WaitingQueue<Action>();
		int threadTimeoutMilliseconds;
		int _currentMinThreads;
		int _currentMaxThreads;
		int _currentLiveThreads = 0;
		int _currentIdleThreads = 0;
		int threadNamingCounter = -1;
		bool threadsAreBackgroundThreads;
		/// <summary>
		/// Gets the name of this thread pool.
		/// </summary>
		public string PoolName { get; }
		volatile bool abort = false;
		/// <summary>
		/// Gets the number of threads that are currently available, including those which are busy and those which are idle.
		/// </summary>
		public int CurrentLiveThreads
		{
			get
			{
				return Thread.VolatileRead(ref _currentLiveThreads);
			}
		}
		/// <summary>
		/// Gets the number of threads that are currently busy processing actions.
		/// </summary>
		public int CurrentBusyThreads
		{
			get
			{
				return CurrentLiveThreads - CurrentIdleThreads;
			}
		}
		/// <summary>
		/// Gets the number of threads that are currently busy processing actions.
		/// </summary>
		public int CurrentIdleThreads
		{
			get
			{
				return Thread.VolatileRead(ref _currentIdleThreads);
			}
		}
		/// <summary>
		/// Gets or sets the soft maximum number of threads this pool should have active at any given time.  It is possible for there to be temporarily more threads than this if certain race conditions are met.  If reducing the value, it may take some time for the number of threads to fall, as no special effort is taken to reduce the live thread count quickly.
		/// </summary>
		public int MaxThreads
		{
			get
			{
				return Thread.VolatileRead(ref _currentMaxThreads);
			}
			set
			{
				if (value < 1 || MinThreads > value)
					throw new Exception("MaxThreads must be >= 1 and >= MinThreads");
				Interlocked.Exchange(ref _currentMaxThreads, value);
			}
		}
		/// <summary>
		/// Gets or sets the minimum number of threads this pool should have active at any given time.  If increasing the value, it may take some time for the number of threads to rise, as no special effort is taken to reach this number.
		/// </summary>
		public int MinThreads
		{
			get
			{
				return Thread.VolatileRead(ref _currentMinThreads);
			}
			set
			{
				if (value < 0 || value > MaxThreads)
					throw new Exception("MinThreads must be >= 0 and <= MaxThreads");
				Interlocked.Exchange(ref _currentMinThreads, value);
			}
		}
		private Action<Exception, string> logErrorAction = SimpleHttpLogger.Log;
		/// <summary>
		/// Creates a new SimpleThreadPool.
		/// </summary>
		/// <param name="poolName">The name this pool shall have. Each thread created by this pool shall be named with this string followed by a space and an auto-incremented number.</param>
		/// <param name="minThreads">The minimum number of threads that should be kept alive at all times.</param>
		/// <param name="maxThreads">The largest number of threads this pool should attempt to have alive at any given time.  It is possible for there to be temporarily more threads than this if certain race conditions are met.</param>
		/// <param name="threadTimeoutMilliseconds">Threads with no work to do will automatically terminate after this many milliseconds (unless doing so would dishonor the [minThreads] limit).</param>
		/// <param name="useBackgroundThreads">If true, the application will be able to exit without waiting for all the threads in this thread pool to exit.  Background threads do not prevent a process from terminating. Once all foreground threads belonging to a process have terminated, the common language runtime ends the process. Any remaining background threads are stopped and do not complete.</param>
		/// <param name="logErrorAction">A method to use for logging exceptions.  If null, SimpleHttpLogger.Log will be used.</param>
		public SimpleThreadPool(string poolName, int minThreads = 6, int maxThreads = 32, int threadTimeoutMilliseconds = 60000, bool useBackgroundThreads = true, Action<Exception, string> logErrorAction = null)
		{
			this.PoolName = poolName;
			this.threadTimeoutMilliseconds = threadTimeoutMilliseconds;
			if (minThreads < 0 || minThreads > maxThreads)
				throw new ArgumentException("minThreads must be >= 0 and <= maxThreads", "minThreads");
			if (maxThreads < 1 || minThreads > maxThreads)
				throw new ArgumentException("maxThreads must be >= 1 and >= minThreads", "maxThreads");
			this._currentMinThreads = minThreads;
			this._currentMaxThreads = maxThreads;
			this.threadsAreBackgroundThreads = useBackgroundThreads;
			if (logErrorAction != null)
				this.logErrorAction = logErrorAction;
			SpawnNewThreads(minThreads);
		}
		/// <summary>
		/// Creates new threads.
		/// </summary>
		/// <returns></returns>
		private void SpawnNewThreads(int count)
		{
			if (abort)
				return;
			for (int i = 0; i < count; i++)
			{
				if (CurrentLiveThreads < MaxThreads)
				{
					Interlocked.Increment(ref _currentLiveThreads);
					Thread t = new Thread(threadLoop);
					t.IsBackground = threadsAreBackgroundThreads;
					t.Name = PoolName + " " + Interlocked.Increment(ref threadNamingCounter);
					t.Start();
				}
			}
		}
		/// <summary>
		/// Prevents the creation of new threads and prevents new actions from being enqueued.  This cannot be undone.
		/// </summary>
		public void Stop()
		{
			abort = true;
		}
		public void Enqueue(Action action)
		{
			if (abort)
				return;
			bool startNewThread = CurrentIdleThreads < 1;
			actionQueue.Enqueue(action);
			if (startNewThread || CurrentIdleThreads < 1)
				SpawnNewThreads(1); // Spawning the thread AFTER enqueuing ensures that the new thread won't need to wait (as long) for a job to do.
		}

		private void threadLoop()
		{
			try
			{
				Interlocked.Increment(ref _currentIdleThreads);
				while (true)
				{
					// Check for queued actions to perform.
					while (actionQueue.TryDequeue(out Action action, threadTimeoutMilliseconds))
					{
						Interlocked.Decrement(ref _currentIdleThreads);
						try
						{
							action();
						}
						catch (ThreadAbortException) { throw; }
						catch (Exception ex)
						{
							logErrorAction(ex, "Error on " + Thread.CurrentThread.Name);
						}
						finally
						{
							Interlocked.Increment(ref _currentIdleThreads);
						}
					}

					// Timeout has occurred.
					if (CurrentLiveThreads <= MinThreads)
						continue;// If this thread quit, we would dip below the minimum live threads limit.
					else
						return;// This thread is allowed to quit
				}
			}
			catch (OperationCanceledException) { }
			catch (ThreadAbortException) { }
			catch (Exception ex)
			{
				logErrorAction(ex, "Fatal error on \"" + Thread.CurrentThread.Name + "\" indicating a programming error.");
			}
			finally
			{
				Interlocked.Decrement(ref _currentIdleThreads);
				Interlocked.Decrement(ref _currentLiveThreads);
			}
		}
	}
}
