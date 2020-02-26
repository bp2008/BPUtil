using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp.TLS.Implementation
{
	/// <summary>
	/// A read-only stream which provides to the reader a view of TLS fragment payloads.  This class buffers one entire TLS fragment at a time (maximum size of one fragment is 16384 + 5 = 16389 bytes).  Fragmentation is supported, however it is up to the caller to provide fragments and handle interleaving.  The first fragment received locks this instance to accept only fragments of that type.  If a fragment is provided that is not the same type as the first fragment, an exception is thrown.
	/// </summary>
	public class FragmentStream : Stream
	{
		protected Func<TLSPlaintext> getFragmentCallback;
		protected TLSPlaintext currentFragment = null;
		protected ContentType? firstFragmentType = null;
		protected int position = 0;

		/// <summary>
		/// Creates a new FragmentStream that will be locked to the type of the first fragment it is provided.
		/// </summary>
		/// <param name="getFragmentCallback">A callback function that should return a fragment when requested.  This class will call the callback during Read operations when more data is required.</param>
		public FragmentStream(Func<TLSPlaintext> getFragmentCallback)
		{
			this.getFragmentCallback = getFragmentCallback;
		}
		#region Stream abstract members / Trivial implementations

		#region Properties
		/// <summary>
		/// (true) Gets a value indicating whether the current stream supports reading.
		/// </summary>
		/// <returns>true if the stream supports reading; otherwise, false.</returns>
		public override bool CanRead
		{
			get
			{
				return true;
			}
		}

		/// <summary>
		/// (false) Gets a value indicating whether the current stream supports seeking.
		/// </summary>
		/// <value></value>
		/// <returns>true if the stream supports seeking; otherwise, false.</returns>
		public override bool CanSeek
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// (false) Gets a value indicating whether the current stream supports writing.
		/// </summary>
		/// <value></value>
		/// <returns>true if the stream supports writing; otherwise, false.</returns>
		public override bool CanWrite
		{
			get
			{
				return false;
			}
		}

		/// <summary>
		/// Not supported. Will throw an exception.
		/// </summary>
		/// <value></value>
		/// <returns>A long value representing the length of the stream in bytes.</returns>
		/// <exception cref="T:System.NotSupportedException">The base stream does not support seeking. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		public override long Length
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		/// <summary>
		/// Not supported. Will throw an exception.
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
				throw new NotSupportedException();
			}
			set
			{
				throw new NotSupportedException();
			}
		}
		#endregion

		#region Methods Flush/Seek/SetLength/Close
		/// <summary>
		/// Not supported. Does nothing.
		/// </summary>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs.</exception>
		public override void Flush()
		{
		}

		/// <summary>
		/// Not supported. Will throw an exception.
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
			throw new NotSupportedException();
		}

		/// <summary>
		/// Not supported. Will throw an exception.
		/// </summary>
		/// <param name="value">The desired length of the current stream in bytes.</param>
		/// <exception cref="T:System.NotSupportedException">The base stream does not support both writing and seeking, such as if the stream is constructed from a pipe or console output. </exception>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}
		/// <summary>
		/// Not supported. Does nothing.
		/// </summary>
		public override void Close()
		{
		}
		#endregion
		#endregion

		/// <summary>
		/// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read. This stream will block until it can read the requested number of bytes, only returning less if the end of stream is reached.
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
			if (currentFragment == null)
			{
				currentFragment = getFragmentCallback();
				firstFragmentType = currentFragment.type;
			}
			int read = 0;
			while (read < count)
			{
				if (position >= currentFragment.data_fragment.Length)
				{
					currentFragment = getFragmentCallback();
					position = 0;
					if (firstFragmentType != currentFragment.type)
						throw new Exception("FragmentStream was given a fragment of type " + currentFragment.type + " but was already locked to type " + firstFragmentType + ". The caller is responsible for handling interleaved fragments.");
				}
				int toRead = Math.Min(count - read, currentFragment.data_fragment.Length - position);
				Array.Copy(currentFragment.data_fragment, position, buffer, offset + read, toRead);
				position += toRead;
				read += toRead;
			}
			return read;
		}

		/// <summary>
		/// Not Implemented.
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
			throw new NotSupportedException();
		}
	}
}
