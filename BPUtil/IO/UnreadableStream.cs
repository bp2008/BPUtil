using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil.IO
{
	/// <summary>
	/// A stream which can have data buffers "un-read".  In other words, code can read some bytes from the stream, then put the bytes back into the stream afterward so they will be read again.
	/// </summary>
	public class UnreadableStream : Stream, IDisposable
	{
		/// <summary>
		/// The underlying stream (which was passed into the constructor).
		/// </summary>
		public Stream originalStream { get; protected set; }
		private bool leaveOpen;

		/// <summary>
		/// Queue of unread data buffers.
		/// </summary>
		protected Stack<byte[]> unreadData = new Stack<byte[]>();
		public UnreadableStream(Stream originalStream, bool leaveOpen)
		{
			this.originalStream = originalStream;
			this.leaveOpen = leaveOpen;
		}
		/// <summary>
		/// Closes the underlying stream if this UnreadableStream was configured to do so during construction.
		/// </summary>
		public new void Dispose()
		{
			if (!leaveOpen)
				originalStream.Dispose();
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
		/// <summary>
		/// Closes the underlying stream if this UnreadableStream was configured to do so during construction.
		/// </summary>
		public override void Close()
		{
			if (!leaveOpen)
				originalStream.Close();
		}
		#endregion
		#endregion
		/// <summary>
		/// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
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
			int read = 0;
			while (unreadData.Count > 0 && read < count)
			{
				// We have data that has been "un-read".
				byte[] next = unreadData.Pop();

				if (next.Length > count - read)
				{
					// [next] contains more than we need.  Read what we can from it.
					int bytesToGetFromNext = count - read;
					Array.Copy(next, 0, buffer, offset + read, bytesToGetFromNext);

					read += bytesToGetFromNext;

					// Add the unread section back onto the stack.
					unreadData.Push(ByteUtil.SubArray(next, bytesToGetFromNext, next.Length - bytesToGetFromNext));
				}
				else
				{
					// [next] contains less than we need, or exactly the right amount
					// We will be reading all of [next]
					Array.Copy(next, 0, buffer, offset + read, next.Length);

					read += next.Length;
				}
			}

			if (read < count)
			{
				// We haven't filled the buffer yet.  Read more if data is available, or if we haven't read anything yet.  If data is not available and we've already read something, then we need to skip this so we can return right away.
				if (read == 0 || (originalStream is NetworkStream && ((NetworkStream)originalStream).DataAvailable))
					read += originalStream.Read(buffer, offset + read, count - read);
			}
			return read;
		}
		/// <summary>
		/// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
		/// </summary>
		/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
		/// <param name="count">The maximum number of bytes to be read from the current stream.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns>
		/// The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
		/// </returns>
		/// <exception cref="T:System.ArgumentException">The sum of offset and count is larger than the buffer length. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		/// <exception cref="T:System.NotSupportedException">The base stream does not support reading. </exception>
		/// <exception cref="T:System.ArgumentNullException">buffer is null. </exception>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">offset or count is negative. </exception>
		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
		{
			int read = 0;
			while (unreadData.Count > 0 && read < count)
			{
				// We have data that has been "un-read".
				byte[] next = unreadData.Pop();

				if (next.Length > count - read)
				{
					// [next] contains more than we need.  Read what we can from it.
					int bytesToGetFromNext = count - read;
					Array.Copy(next, 0, buffer, offset + read, bytesToGetFromNext);

					read += bytesToGetFromNext;

					// Add the unread section back onto the stack.
					unreadData.Push(ByteUtil.SubArray(next, bytesToGetFromNext, next.Length - bytesToGetFromNext));
				}
				else
				{
					// [next] contains less than we need, or exactly the right amount
					// We will be reading all of [next]
					Array.Copy(next, 0, buffer, offset + read, next.Length);

					read += next.Length;
				}
			}

			if (read < count)
			{
				// We haven't filled the buffer yet.  Read more if data is available, or if we haven't read anything yet.  If data is not available and we've already read something, then we need to skip this so we can return right away.
				if (read == 0 || (originalStream is NetworkStream && ((NetworkStream)originalStream).DataAvailable))
					read += await originalStream.ReadAsync(buffer, offset + read, count - read, cancellationToken).ConfigureAwait(false);
			}
			return read;
		}

		/// <summary>
		/// Virtually un-does a read operation so that the data provided here will be the next data read.
		/// </summary>
		/// <param name="buffer">A data buffer.  This class will keep a reference to this data buffer.  DO NOT modify its contents or recycle it into an object pool!</param>
		public void Unread(byte[] buffer)
		{
			unreadData.Push(buffer);
		}

		/// <summary>
		/// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
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
			originalStream.Write(buffer, offset, count);
		}

		/// <summary>
		/// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
		/// </summary>
		/// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
		/// <param name="count">The number of bytes to be written to the current stream.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		/// <exception cref="T:System.NotSupportedException">The base stream does not support writing. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		/// <exception cref="T:System.ArgumentNullException">buffer is null. </exception>
		/// <exception cref="T:System.ArgumentException">The sum of offset and count is greater than the buffer length. </exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">offset or count is negative. </exception>
		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
		{
			await originalStream.WriteAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
		}
	}
}
