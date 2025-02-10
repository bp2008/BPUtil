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
		private Action<IEnumerable<T>> processItemsMethod;
		private EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
		/// <summary>
		/// The name of this PipelineThread.
		/// </summary>
		public readonly string PipelineName;
		/// <summary>
		/// If true, objects that were enqueued may be skipped by the pipeline scheduler in order to process only the latest object.  Suitable for pipelines where every object enqueued makes the previous items obsolete.
		/// </summary>
		public bool OnlyProcessLatestObject
		{
			get
			{
				return _onlyProcessLatestObject;
			}
			set
			{
				if (value && processItemMethod == null)
					throw new Exception("This PipelineThread was not constructed with a single-item processing method. The " + nameof(OnlyProcessLatestObject) + " option cannot be enabled in this state.");
				_onlyProcessLatestObject = value;
			}
		}
		public bool _onlyProcessLatestObject = false;

		private PipelineThread(Action<Exception> errorHandler, string pipelineName)
		{
			this.PipelineName = pipelineName ?? "PipelineThread<" + typeof(T) + ">";
			if (errorHandler == null)
				this.errorHandler = ex => Logger.Debug(ex);
			else
				this.errorHandler = errorHandler;

			thrBackground = new Thread(pipelineLoop);
			thrBackground.IsBackground = true;
			thrBackground.Name = PipelineName;
			thrBackground.Start();
		}
		/// <summary>
		/// <para>Constructs a new PipelineThread by passing in a method that processes one item at a time.</para>
		/// <para>The constructor starts the background thread.</para>
		/// <para>The PipelineThread contains a background thread (if the thread is still running when the application tries to exit, the thread will be terminated by the .NET runtime).</para>
		/// <para>The background thread is gracefully stopped when the instance of this class is disposed.</para>
		/// </summary>
		/// <param name="processItemMethod">The method which processes an item.  The method will be called from a background thread for each item that is enqueued.</param>
		/// <param name="errorHandler">Optional error handler.  If null, Logger.Debug is used.</param>
		/// <param name="pipelineName">Optional pipeline name which will be assigned to the background thread to help with debugging.</param>
		public PipelineThread(Action<T> processItemMethod, Action<Exception> errorHandler = null, string pipelineName = null) : this(errorHandler, pipelineName)
		{
			if (processItemMethod == null)
				throw new ArgumentNullException(nameof(processItemMethod));
			this.processItemMethod = processItemMethod;
		}
		/// <summary>
		/// <para>Constructs a new PipelineThread by passing in a method that can process multiple items at a time.</para>
		/// <para>The constructor starts the background thread.</para>
		/// <para>The PipelineThread contains a background thread (if the thread is still running when the application tries to exit, the thread will be terminated by the .NET runtime).</para>
		/// <para>The background thread is gracefully stopped when the instance of this class is disposed.</para>
		/// </summary>
		/// <param name="processItemsMethod">The method which processes a collection of one or more items.  The method will be called from a background thread each time there are items available in the queue.  If more than one item is available when a previous method execution completes, multiple items are passed in to the next execution of the method.</param>
		/// <param name="errorHandler">Optional error handler.  If null, Logger.Debug is used.</param>
		/// <param name="pipelineName">Optional pipeline name which will be assigned to the background thread to help with debugging.</param>
		public PipelineThread(Action<IEnumerable<T>> processItemsMethod, Action<Exception> errorHandler = null, string pipelineName = null) : this(errorHandler, pipelineName)
		{
			if (processItemsMethod == null)
				throw new ArgumentNullException(nameof(processItemsMethod));
			this.processItemsMethod = processItemsMethod;
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
				List<T> multipleItems = processItemsMethod != null ? new List<T>() : null;
				while (!abort)
				{
					while (!abort && !waitHandle.WaitOne(30000))
					{
					}

					T item;
					if (processItemMethod != null)
					{
						if (OnlyProcessLatestObject)
						{
							bool gotItem = false;
							T itemToProcess = default(T);
							while (queue.TryDequeue(out item))
							{
								itemToProcess = item;
								gotItem = true;
							}
							if (gotItem)
							{
								try
								{
									processItemMethod(itemToProcess);
								}
								catch (Exception ex)
								{
									errorHandler(ex);
								}
							}
						}
						else
						{
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
					else if (processItemsMethod != null)
					{
						while (queue.TryDequeue(out item))
						{
							multipleItems.Add(item);
						}
						if (multipleItems.Count > 0)
						{
							try
							{
								processItemsMethod(multipleItems);
							}
							catch (Exception ex)
							{
								errorHandler(ex);
							}
							multipleItems.Clear();
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
