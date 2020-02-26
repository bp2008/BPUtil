using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.IO
{
	public class UnreadableStream : Stream, IDisposable
	{
		/// <summary>
		/// The underlying stream (which was passed into the constructor).
		/// </summary>
		public Stream originalStream { get; protected set; }

		/// <summary>
		/// Queue of unread data buffers.
		/// </summary>
		protected BPQueue<byte[]> unreadData = new BPQueue<byte[]>();
		public UnreadableStream(Stream originalStream)
		{
			this.originalStream = originalStream;
		}
		public new void Dispose()
		{
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
		public override void Close()
		{
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
			while (!unreadData.IsEmpty && read < count)
			{
				// We have data that has been "un-read".
				unreadData.TryPeek(out byte[] next);

				if (next.Length > count - read)
				{
					// [next] contains more than we need.  Read what we can from it.
					int bytesToGetFromNext = count - read;
					Array.Copy(next, 0, buffer, offset + read, bytesToGetFromNext);

					read += bytesToGetFromNext;

					// Remove the read data from the queue
					unreadData.ReplaceFront(ByteUtil.SubArray(next, bytesToGetFromNext, next.Length - bytesToGetFromNext));
				}
				else
				{
					// [next] contains less than we need, or exactly the right amount
					// We will be reading all of [next]
					Array.Copy(next, 0, buffer, offset + read, next.Length);

					read += next.Length;

					// Remove the read data from the queue
					unreadData.TryDequeue(out next);
				}
			}
			if (read == 0) // We didn't have any unread data, so just pass this method call on to the original stream.
				return originalStream.Read(buffer, offset, count);

			if (read < count)
			{
				int justRead = originalStream.Read(buffer, offset + read, count);
				if (justRead < 0)
					return read;
				read += justRead;
			}
			return read;
		}

		/// <summary>
		/// Virtually un-does a read operation, allowing the next read operation(s) to read this data as if it was still on the stream.
		/// </summary>
		/// <param name="buffer">A data buffer.  This class will keep a reference to this data buffer.  DO NOT modify its contents!</param>
		public void Unread(byte[] buffer)
		{
			unreadData.Enqueue(buffer);
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
	}
}
