using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace BPUtil.SimpleHttp
{
	/// <summary>
	/// A stream that throttles read and write operations.  The throttled speed varies depending on the number of other competing streams, in order to meet application-wide speed limits.
	/// </summary>
	public class GlobalThrottledStream : Stream, IDisposable
	{
		/// <summary>
		/// Gets a reference to the underlying stream.
		/// </summary>
		public Stream BaseStream
		{
			get { return originalStream; }
		}
		private Stream originalStream;
		private int ruleSetId;
		private uint remoteIpAddress;

		/// <summary>
		/// Initialized a GlobalThrottledStream.
		/// </summary>
		/// <param name="originalStream">The stream to throttle.</param>
		/// <param name="ruleSetId">The numeric ID  of the throttling ruleset that will be applied to this stream.  If an invalid ruleSetId is provided, no throttling will be done.</param>
		/// <param name="remoteIpAddress">The remote IP address that will be receiving this stream.</param>
		public GlobalThrottledStream(Stream originalStream, int ruleSetId, uint remoteIpAddress)
		{
			this.originalStream = originalStream;
			this.ruleSetId = ruleSetId;
			this.remoteIpAddress = remoteIpAddress;
		}

		/// <summary>
		/// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.  This method blocks until the throttled read operation has completed.
		/// </summary>
		/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
		/// <param name="count">The maximum number of bytes to be read from the current stream.</param>
		/// <returns>
		/// The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">The sum of offset and count is larger than the buffer length. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		/// <exception cref="T:System.NotSupportedException">The base stream does not support reading. </exception>
		/// <exception cref="T:System.ArgumentNullException">buffer is null. </exception>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">offset or count is negative. </exception>
		public override int Read(byte[] buffer, int offset, int count)
		{
			return ThrottlingManager.ThrottledRead(this, buffer, offset, count);
		}

		/// <summary>
		/// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.  This method blocks until the throttled write operation has completed.
		/// </summary>
		/// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
		/// <param name="count">The number of bytes to be written to the current stream.</param>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		/// <exception cref="T:System.NotSupportedException">The base stream does not support writing. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		/// <exception cref="T:System.ArgumentNullException">buffer is null. </exception>
		/// <exception cref="T:System.ArgumentException">The sum of offset and count is greater than the buffer length. </exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">offset or count is negative. </exception>
		public override void Write(byte[] buffer, int offset, int count)
		{
			ThrottlingManager.ThrottledWrite(this, buffer, offset, count);
		}

		#region Stream abstract members / Trivial implementations

		#region Properties
		/// <summary>
		/// Gets a value indicating whether the current stream supports reading.
		/// </summary>
		/// <returns>true if the stream supports reading; otherwise, false.</returns>
		public override bool CanRead
		{
			get
			{
				return originalStream.CanRead;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the current stream supports seeking.
		/// </summary>
		/// <value></value>
		/// <returns>true if the stream supports seeking; otherwise, false.</returns>
		public override bool CanSeek
		{
			get
			{
				return originalStream.CanSeek;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the current stream supports writing.
		/// </summary>
		/// <value></value>
		/// <returns>true if the stream supports writing; otherwise, false.</returns>
		public override bool CanWrite
		{
			get
			{
				return originalStream.CanWrite;
			}
		}

		/// <summary>
		/// Gets the length in bytes of the stream.
		/// </summary>
		/// <value></value>
		/// <returns>A long value representing the length of the stream in bytes.</returns>
		/// <exception cref="T:System.NotSupportedException">The base stream does not support seeking. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		public override long Length
		{
			get
			{
				return originalStream.Length;
			}
		}

		/// <summary>
		/// Gets or sets the position within the current stream.
		/// </summary>
		/// <value></value>
		/// <returns>The current position within the stream.</returns>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		/// <exception cref="T:System.NotSupportedException">The base stream does not support seeking. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		public override long Position
		{
			get
			{
				return originalStream.Position;
			}
			set
			{
				originalStream.Position = value;
			}
		}
		#endregion

		#region Methods Flush/Seek/SetLength/Close
		/// <summary>
		/// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
		/// </summary>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs.</exception>
		public override void Flush()
		{
			originalStream.Flush();
		}

		/// <summary>
		/// Sets the position within the current stream.
		/// </summary>
		/// <param name="offset">A byte offset relative to the origin parameter.</param>
		/// <param name="origin">A value of type <see cref="T:System.IO.SeekOrigin"></see> indicating the reference point used to obtain the new position.</param>
		/// <returns>
		/// The new position within the current stream.
		/// </returns>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		/// <exception cref="T:System.NotSupportedException">The base stream does not support seeking, such as if the stream is constructed from a pipe or console output. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		public override long Seek(long offset, SeekOrigin origin)
		{
			return originalStream.Seek(offset, origin);
		}

		/// <summary>
		/// Sets the length of the current stream.
		/// </summary>
		/// <param name="value">The desired length of the current stream in bytes.</param>
		/// <exception cref="T:System.NotSupportedException">The base stream does not support both writing and seeking, such as if the stream is constructed from a pipe or console output. </exception>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		public override void SetLength(long value)
		{
			originalStream.SetLength(value);
		}
		public override void Close()
		{
			originalStream.Close();
		}
		#endregion
		#endregion

		#region IDisposable Support
		private bool gts_disposedValue = false; // To detect redundant calls

		protected virtual new void Dispose(bool disposing)
		{
			if (!gts_disposedValue)
			{
				if (disposing)
				{
					try
					{
						if (originalStream != null)
							originalStream.Dispose();
					}
					catch (ThreadAbortException) { throw; }
					catch { }
				}

				gts_disposedValue = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		public new void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
		}
		#endregion

		public static class ThrottlingManager
		{
			#region Public API
			private static volatile bool abort = false;
			/// <summary>
			/// Calling this will enable web server bandwidth throttling and create the specified number of throttling rule sets and initialize them all to "unlimited" speed.
			/// 
			/// After calling this, you should modify each rule set via ThrottlingManager.BurstIntervalMs and ThrottlingManager.SetBytesPerSecond().
			/// 
			/// You should only call this function once; additional calls will do nothing.
			/// 
			/// Please note: throttling is incompatible with ASP.NET
			/// </summary>
			/// <param name="numberOfThrottlingRuleSetsToCreate"></param>
			public static void Initialize(int numberOfThrottlingRuleSetsToCreate)
			{
				if (initialized)
					return;
				if (numberOfThrottlingRuleSetsToCreate < 1)
					numberOfThrottlingRuleSetsToCreate = 1;
				for (int i = 0; i < numberOfThrottlingRuleSetsToCreate; i++)
					ruleSets.Add(new ThrottlingRuleSet());
				thrIOScheduler = new Thread(ioSchedulerLoop);
				thrIOScheduler.IsBackground = true;
				thrIOScheduler.Start();
				initialized = true;
			}
			/// <summary>
			/// Call this when it is time to shut down the app.  Any active, throttled I/O operations will stop because the scheduling thread will be shut down.
			/// </summary>
			public static void Shutdown()
			{
				abort = true;
			}

			/// <summary>
			/// Gets or sets the number of milliseconds between data reads/writes.  When setting this, the value will be clamped between 1 and 1000.
			/// </summary>
			public static int BurstIntervalMs
			{
				get
				{
					return burstIntervalMs;
				}
				set
				{
					if (value < 1)
						burstIntervalMs = 1;
					else if (value > 1000)
						burstIntervalMs = 1000;
					else
						burstIntervalMs = value;
				}
			}
			public static long GetBytesPerSecond(int ruleSetId)
			{
				if (IndexOutOfRange(ruleSetId))
					return 0;
				return ruleSets[ruleSetId].bytesPerSecond;
			}
			/// <summary>
			/// Sets the bytes per second throttle for this rule.  All streams using this rule share the same bandwidth pool that you set here.  (can be set at any time, even while streams are active).
			/// 
			/// If less than 0, the value is clamped to 0.
			/// If 0, the streams using this rule will be unthrottled.
			/// </summary>
			/// <param name="ruleSetId"></param>
			/// <param name="bytesPerSecond"></param>
			public static void SetBytesPerSecond(int ruleSetId, long bytesPerSecond)
			{
				if (IndexOutOfRange(ruleSetId))
					return;
				if (bytesPerSecond < 0)
					bytesPerSecond = 0;
				ruleSets[ruleSetId].bytesPerSecond = bytesPerSecond;
			}
			#endregion
			#region Private data and methods
			private static bool initialized = false;
			private static int burstIntervalMs = 10;
			private static List<ThrottlingRuleSet> ruleSets = new List<ThrottlingRuleSet>();
			private static Thread thrIOScheduler = null;

			private static bool IndexOutOfRange(int index)
			{
				return index < 0 || index >= ruleSets.Count;
			}

			private static void ioSchedulerLoop()
			{
				try
				{
					Stopwatch watch = new Stopwatch();
					EventWaitHandle ewh = new EventWaitHandle(false, EventResetMode.ManualReset);
					int lastIntervalMs = BurstIntervalMs;
					while (!abort)
					{
						try
						{
							watch.Restart();
							foreach (ThrottlingRuleSet ruleSet in ruleSets)
							{
								ruleSet.DoScheduledOperations(lastIntervalMs);
							}
							lastIntervalMs = BurstIntervalMs;
							int timeToWait = lastIntervalMs - (int)watch.ElapsedMilliseconds;
							if (timeToWait > 0)
								ewh.WaitOne(timeToWait);
						}
						catch (ThreadAbortException)
						{
							throw;
						}
						catch (Exception ex)
						{
							SimpleHttpLogger.Log(ex);
						}
					}
				}
				catch (ThreadAbortException)
				{
					foreach (ThrottlingRuleSet ruleSet in ruleSets)
						ruleSet.ReleaseAllWaitingThreads();
				}
			}
			#endregion
			#region Internal methods for GlobalThrottledStream to call
			internal static int ThrottledRead(GlobalThrottledStream stream, byte[] buffer, int offset, int count)
			{
				long bytesPerSecond = GetBytesPerSecond(stream.ruleSetId);
				if (bytesPerSecond == 0)
					return stream.originalStream.Read(buffer, offset, count);

				IOOperation ioop = new IOOperation(stream, buffer, offset, count, false);
				ThrottlingRuleSet ruleSet = ruleSets[stream.ruleSetId];
				return ruleSet.PerformBlockingIOOperation(ioop);
			}

			internal static void ThrottledWrite(GlobalThrottledStream stream, byte[] buffer, int offset, int count)
			{
				long bytesPerSecond = GetBytesPerSecond(stream.ruleSetId);
				if (bytesPerSecond == 0)
				{
					stream.originalStream.Write(buffer, offset, count);
					return;
				}
				IOOperation ioop = new IOOperation(stream, buffer, offset, count, true);
				ThrottlingRuleSet ruleSet = ruleSets[stream.ruleSetId];
				ruleSet.PerformBlockingIOOperation(ioop);
			}
			#endregion
		}
		/// <summary>
		/// Performs I/O operations for a group of streams.
		/// </summary>
		private class ThrottlingRuleSet
		{
			private bool abort = false;
			internal long bytesPerSecond = 0;
			internal ConcurrentQueue<EventWaitHandleWrapper> qWaitHandles = new ConcurrentQueue<EventWaitHandleWrapper>();

			internal int PerformBlockingIOOperation(IOOperation ioop)
			{
				EventWaitHandleWrapper task = new EventWaitHandleWrapper();
				task.remoteIpAddress = ioop.RemoteIpAddress;
				while (!abort && !ioop.Finished)
				{
					task.ewh.Reset(); // Reset the wait handle
					qWaitHandles.Enqueue(task); // This submits the I/O operation for scheduling
					task.ewh.WaitOne(); // Wait until the scheduled time
					ioop.ProcessBytes(task.allowedBytes);
				}
				return ioop.amountRead;
			}

			internal void DoScheduledOperations(int lastIntervalMs)
			{
				Dictionary<uint, List<EventWaitHandleWrapper>> dictRemoteIpAddresses = new Dictionary<uint, List<EventWaitHandleWrapper>>();
				{
					EventWaitHandleWrapper task;
					List<EventWaitHandleWrapper> tasks;
					while (qWaitHandles.TryDequeue(out task))
					{
						if (!dictRemoteIpAddresses.TryGetValue(task.remoteIpAddress, out tasks))
							dictRemoteIpAddresses[task.remoteIpAddress] = tasks = new List<EventWaitHandleWrapper>();
						tasks.Add(task);
					}
				}
				if (dictRemoteIpAddresses.Count > 0)
				{
					double fractionOfBitrateToAllowNow = (double)lastIntervalMs / 1000.0;
					double allowedBytes = bytesPerSecond * fractionOfBitrateToAllowNow;
					double eachRemoteIpAllowedBytes = allowedBytes / (double)dictRemoteIpAddresses.Count;
					foreach (var kvp in dictRemoteIpAddresses)
					{
						double eachTaskAllowedBytes = eachRemoteIpAllowedBytes / (double)kvp.Value.Count;
						if (eachTaskAllowedBytes <= 0)
							eachTaskAllowedBytes = 1000000000;
						foreach (EventWaitHandleWrapper t in kvp.Value)
						{
							t.allowedBytes = eachTaskAllowedBytes;
							t.ewh.Set(); // Unblock the thread that is handling the I/O operation.
						}
					}
				}
			}

			internal void ReleaseAllWaitingThreads()
			{
				abort = true;
				DoScheduledOperations(0);
			}
		}
		/// <summary>
		/// Maintans the state of a throttled read or write operation.
		/// </summary>
		private class IOOperation
		{
			private GlobalThrottledStream stream;
			private byte[] buffer;
			private int offset;
			private int count;
			private bool isWriteOp;
			internal int amountRead = 0;
			/// <summary>
			/// We can only transmit whole numbers of bytes, so this field stores the fractional remainder.
			/// This fractional part must be counted, or else data transmission will stop entirely if the 
			/// allowed bytes per interval goes below 1.
			/// </summary>
			private double leftOverFractionalBytes = 0;
			internal bool Finished
			{
				get
				{
					return count <= 0;
				}
			}
			internal uint RemoteIpAddress
			{
				get
				{
					return stream.remoteIpAddress;
				}
			}

			internal IOOperation(GlobalThrottledStream stream, byte[] buffer, int offset, int count, bool isWriteOp)
			{
				this.stream = stream;
				this.buffer = buffer;
				this.offset = offset;
				this.count = count;
				this.isWriteOp = isWriteOp;
			}

			internal void ProcessBytes(double allowedBytes)
			{
				int iAllowedBytes = (int)allowedBytes;
				leftOverFractionalBytes += (allowedBytes - iAllowedBytes);
				if (leftOverFractionalBytes > 1)
				{
					int additional = (int)leftOverFractionalBytes;
					iAllowedBytes += additional;
					leftOverFractionalBytes = leftOverFractionalBytes - additional;
				}
				int bytesToProcess = Math.Min(iAllowedBytes, count);

				if (isWriteOp)
				{
					stream.originalStream.Write(buffer, offset, bytesToProcess);
					offset += bytesToProcess;
					count -= bytesToProcess;
				}
				else
				{
					int read = stream.originalStream.Read(buffer, offset, bytesToProcess);
					offset += read;
					count -= read;
					amountRead += read;
					if (read < bytesToProcess)
						count = 0; // No data left to read at this moment.
				}
			}
		}
		/// <summary>
		/// Contains an EventWaitHandle and a value indicating the number of bytes that may be processed after the EventWaitHandle is released.
		/// </summary>
		private class EventWaitHandleWrapper
		{
			public EventWaitHandle ewh;
			public double allowedBytes = 0;
			public uint remoteIpAddress = 0;

			public EventWaitHandleWrapper()
			{
				ewh = new EventWaitHandle(false, EventResetMode.ManualReset);
			}
		}
	}
}
