using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.IO
{
	/// <summary>
	/// A stream which preserves a copy of all read data so it can be inspected later. 
	/// </summary>
	public class SnoopReadableStream : Stream
	{
		private readonly Stream _innerStream;
		private readonly MemoryStream _snoopBuffer = new MemoryStream();
		private readonly bool leaveOpen = false;

		/// <summary>
		/// Constructs a SnoopReadableStream that can read from the given stream and cache a copy of all data read for later inspection.  Call <see cref="Data"/> to get a copy of all data that has been read so far.
		/// </summary>
		/// <param name="innerStream">The stream you need to read from.</param>
		/// <param name="leaveOpen">True to keep <paramref name="innerStream"/> open when the SnoopReadableStream is closed or disposed. If true, you should close or dispose <paramref name="innerStream"/> separately.</param>
		/// <exception cref="ArgumentNullException">If <paramref name="innerStream"/> is null.</exception>
		public SnoopReadableStream(Stream innerStream, bool leaveOpen = false)
		{
			_innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
			this.leaveOpen = leaveOpen;
		}
		/// <summary>
		/// Gets all buffered data that has been read so far.  This is not efficient.  Make a local reference to this if you need to refer to it multiple times.
		/// </summary>
		/// <returns></returns>
		public byte[] Data => _snoopBuffer.ToArray();
		/// <summary>
		/// Gets all buffered data that has been read so far and converts it to a string using UTF-8 encoding.  This is not efficient.  Make a local reference to this if you need to refer to it multiple times.
		/// </summary>
		/// <returns></returns>
		public string DataAsUtf8 => ByteUtil.Utf8NoBOM.GetString(Data);
		/// <summary>
		/// Reads data from the inner stream and also caches it for later retrieval by <see cref="Data"/>.
		/// </summary>
		/// <param name="buffer">Buffer to read into.</param>
		/// <param name="offset">Offset into the buffer where the data should be written.</param>
		/// <param name="count">Maximum number of bytes to read.</param>
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
			int bytesRead = _innerStream.Read(buffer, offset, count);
			if (bytesRead > 0)
				_snoopBuffer.Write(buffer, offset, bytesRead);
			return bytesRead;
		}

		/// <summary>
		/// Gets a value indicating if the inner stream supports reading.
		/// </summary>
		public override bool CanRead => _innerStream.CanRead;
		/// <summary>
		/// Gets a value indicating if the inner stream supports seeking.
		/// </summary>
		public override bool CanSeek => _innerStream.CanSeek;
		/// <summary>
		/// Gets a value indicating if the inner stream supports writing.
		/// </summary>
		public override bool CanWrite => _innerStream.CanWrite;
		/// <summary>
		/// Gets the length of the inner stream in bytes.
		/// </summary>
		public override long Length => _innerStream.Length;

		public override long Position
		{
			get => _innerStream.Position;
			set => _innerStream.Position = value;
		}

		/// <summary>
		/// Flushes the inner stream.
		/// </summary>
		public override void Flush() => _innerStream.Flush();
		/// <summary>
		/// Sets the position within the inner stream.
		/// </summary>
		/// <param name="offset">A byte offset relative to the origin parameter.</param>
		/// <param name="origin">A value of type System.IO.SeekOrigin indicating the reference point used to obtain the new position.</param>
		/// <returns>The new position within the inner stream.</returns>
		public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
		/// <summary>
		/// Sets the length of the inner stream.
		/// </summary>
		/// <param name="value">Length in bytes.</param>
		public override void SetLength(long value) => _innerStream.SetLength(value);

		/// <summary>
		/// Writes a sequence of bytes to the inner stream and advances the current position within the inner stream by the number of bytes written.
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
		public override void Write(byte[] buffer, int offset, int count) => _innerStream.Write(buffer, offset, count);
		/// <summary>
		/// Closes the inner stream if this SnoopReadableStream was constructed with <c>leaveOpen</c> set to false.
		/// </summary>
		public override void Close()
		{
			if (!leaveOpen)
				_innerStream.Close();
		}
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				_snoopBuffer?.Dispose();
				if (!leaveOpen)
					_innerStream?.Dispose();
			}
			base.Dispose(disposing);
		}
	}
}
