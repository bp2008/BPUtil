using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil.Linux.InputListener
{
	/// <summary>
	/// A global input listener that listens to the specified input device.  Specialized for keyboards, but should be functional for other input devices.
	/// </summary>
	public class LinuxInputListener : IDisposable
	{
		public event EventHandler<LinuxInputEventArgs> InputEvent = delegate { };
		public event EventHandler<LinuxInputEventArgs> KeyDownEvent = delegate { };
		public event EventHandler<LinuxInputEventArgs> KeyUpEvent = delegate { };
		public event EventHandler<LinuxInputEventArgs> KeyAutoRepeatEvent = delegate { };
		private Thread listenerThread;
		public readonly string inputDevicePath;

		/// <summary>
		/// Gets a value indicating if the listener is listening.  False if the listener has errored out or been disposed.
		/// </summary>
		public bool IsAlive { get; private set; }
		/// <summary>
		/// Creates a new LinuxKeyListener on the specified input device path.
		/// </summary>
		/// <param name="inputDevicePath">Input device path. e.g. "/dev/input/event0"</param>
		public LinuxInputListener(string inputDevicePath)
		{
			this.inputDevicePath = inputDevicePath;
			IsAlive = true;
			listenerThread = new Thread(listenerLoop);
			listenerThread.Name = "Linux Input Listener: " + inputDevicePath;
			listenerThread.IsBackground = true;
			listenerThread.Start();
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// dispose managed state (managed objects).
				}

				// free unmanaged resources (unmanaged objects) and override a finalizer below.
				// set large fields to null.

				disposedValue = true;
			}
		}

		// override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~LinuxInputListener() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion

		private void listenerLoop()
		{
			try
			{
				using (FileStream fs = new FileStream(inputDevicePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
					while (!disposedValue)
					{
						LinuxInputEvent e = new LinuxInputEvent(fs);
						LinuxInputEventArgs args = new LinuxInputEventArgs(e);
						try
						{
							InputEvent(this, args);
						}
						catch (ThreadAbortException) { throw; }
						catch (Exception ex)
						{
							Logger.Debug(ex);
						}

						try
						{
							switch (args.KeyEventType)
							{
								case KeyEventType.Down:
									KeyDownEvent(this, args);
									break;
								case KeyEventType.Up:
									KeyUpEvent(this, args);
									break;
								case KeyEventType.AutoRepeat:
									KeyAutoRepeatEvent(this, args);
									break;
								default:
									break;
							}
						}
						catch (ThreadAbortException) { throw; }
						catch (Exception ex)
						{
							Logger.Debug(ex);
						}
					}
				}
			}
			catch (ThreadAbortException) { }
			catch (IOException ex)
			{
				Logger.Debug(ex, "The device may have been unplugged (" + inputDevicePath + ")");
			}
			catch (Exception ex)
			{
				Logger.Debug(ex);
			}
			finally
			{
				IsAlive = false;
			}
		}
	}

}
