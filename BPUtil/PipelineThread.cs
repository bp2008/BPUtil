using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// <para>A class which allows you to queue objects for processing on a background thread.</para>
	/// <para>The PipelineThread contains a background thread (if the thread is still running when the application tries to exit, the thread will be terminated by the .NET runtime).</para>
	/// <para>The background thread is gracefully stopped when the instance of this class is disposed.</para>
	/// </summary>
	public class PipelineThread<T> : IDisposable
	{
		private Thread thrBackground;
		private bool abort = false;
		private ConcurrentQueue<T> queue = new ConcurrentQueue<T>();
		private Action<Exception> errorHandler;
		private Action<T> processItemMethod;
		private EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

		/// <summary>
		/// <para>Constructs a new PipelineThread and starts the background thread.</para>
		/// <para>The PipelineThread contains a background thread (if the thread is still running when the application tries to exit, the thread will be terminated by the .NET runtime).</para>
		/// <para>The background thread is gracefully stopped when the instance of this class is disposed.</para>
		/// </summary>
		/// <param name="processItemMethod">The method which processes an item.  The method will be called from a background thread for each item that is enqueued.</param>
		/// <param name="errorHandler">Optional error handler.  If null, Logger.Debug is used.</param>
		public PipelineThread(Action<T> processItemMethod, Action<Exception> errorHandler = null)
		{
			this.processItemMethod = processItemMethod;

			if (errorHandler == null)
				this.errorHandler = ex => Logger.Debug(ex);
			else
				this.errorHandler = errorHandler;

			thrBackground = new Thread(pipelineLoop);
			thrBackground.IsBackground = true;
			thrBackground.Name = "PipelineThread<" + typeof(T) + ">";
			thrBackground.Start();
		}
		/// <summary>
		/// Enqueues an object for processing on the background thread.
		/// </summary>
		/// <param name="obj">Object to enqueue for processing.</param>
		public void Enqueue(T obj)
		{
			queue.Enqueue(obj);
			waitHandle.Set();
		}
		/// <summary>
		/// Discards all queued items without processing them.
		/// </summary>
		public void Clear()
		{
			queue = new ConcurrentQueue<T>();
		}
		/// <summary>
		/// Gets the current size of the item queue.
		/// </summary>
		/// <returns></returns>
		public int QueueLength => queue.Count;
		/// <summary>
		/// Gets a value indicating if the pipeline has stopped (due to being disposed or due to a critical error in the pipeline loop).
		/// </summary>
		public bool Stopped { get; private set; }

		#region IDisposable
		private bool disposedValue;
		protected virtual void Dispose(bool disposing)
		{
			abort = true;
			waitHandle.Set();
			if (!disposedValue)
			{
				if (disposing)
				{
					// dispose managed state (managed objects)
				}

				// free unmanaged resources (unmanaged objects) and override finalizer
				// set large fields to null
				disposedValue = true;
			}
		}
		/// <summary>
		/// Stops the background thread.
		/// </summary>
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion

		private void pipelineLoop()
		{
			try
			{
				while (!abort)
				{
					while (!abort && waitHandle.WaitOne(30000))
					{
					}

					T item;
					while (!abort && queue.TryDequeue(out item))
					{
						try
						{
							processItemMethod(item);
						}
						catch (Exception ex)
						{
							errorHandler(ex);
						}
					}
				}
			}
			catch (Exception ex)
			{
				errorHandler(ex);
			}
			finally
			{
				Stopped = true;
			}
		}
	}
}
