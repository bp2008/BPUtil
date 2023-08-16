using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil.IO
{
	/// <summary>
	/// <para>A stream which provides read access to data from a variable number of underlying streams.</para>
	/// <para>The only supported operation is Read.</para>
	/// <para>It is not necessary to dispose a ConcatenatedStream.</para>
	/// </summary>
	public class ConcatenatedStream : Stream
	{
		/// <summary>
		/// Function which accepts a stream index as an argument (which you may ignore if you don't care) and returns a Stream.  When the function returns null, the ConcatenatedStream will be at "End of Stream".
		/// </summary>
		private Func<int, Stream> getStream;
		/// <summary>
		/// Stores the current underlying stream between calls to Read().
		/// </summary>
		private Stream currentUnderlyingStream;
		/// <summary>
		/// The index of the next underlying stream.
		/// </summary>
		private int nextUnderlyingStreamIndex = 0;
		/// <summary>
		/// Creates a ConcatenatedStream which provides read access to data from a variable number of underlying streams.
		/// </summary>
		/// <param name="underlyingStreams">A collection of underlying streams which should be read to their ends, one at a time, in the sequence given.</param>
		public ConcatenatedStream(IEnumerable<Stream> underlyingStreams)
		{
			Queue<Stream> queue = new Queue<Stream>(underlyingStreams);
			getStream = idx =>
			{
				if (queue.Count == 0)
					return null;
				return queue.Dequeue();
			};
		}
		/// <summary>
		/// Creates a ConcatenatedStream which provides read access to data from a variable number of underlying streams.
		/// </summary>
		/// <param name="getStream">Function which accepts a stream index as an argument (starts at 0, increments by 1 for each call) and returns a Stream.  This function will be called as needed during Read operations until it returns null.  When the function returns null, the ConcatenatedStream will be at "End of Stream".</param>
		public ConcatenatedStream(Func<int, Stream> getStream)
		{
			this.getStream = getStream;
		}
		/// <inheritdoc />
		public override int Read(byte[] buffer, int offset, int count)
		{
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			if (offset != offset.Clamp(0, buffer.Length))
				throw new ArgumentOutOfRangeException(nameof(offset), "Offset " + offset + " is outside the bounds of the buffer (" + buffer.Length + ").");
			if (count < 0 || count > buffer.Length - offset)
				throw new ArgumentOutOfRangeException(nameof(count), "Count " + count + " must be no smaller than 0 but no larger than (buffer.Length - offset) (" + (buffer.Length - offset) + ").");

			int totalBytesRead = 0;

			if (currentUnderlyingStream == null)
			{
				currentUnderlyingStream = getStream(nextUnderlyingStreamIndex);
				nextUnderlyingStreamIndex++;
			}

			while (count > 0 && currentUnderlyingStream != null)
			{
				int bytesRead = currentUnderlyingStream.Read(buffer, offset, count);

				totalBytesRead += bytesRead;
				offset += bytesRead;
				count -= bytesRead;

				if (bytesRead == 0)
				{
					currentUnderlyingStream = getStream(nextUnderlyingStreamIndex);
					nextUnderlyingStreamIndex++;
				}
			}

			return totalBytesRead;
		}
		/// <inheritdoc />
		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			if (offset != offset.Clamp(0, buffer.Length))
				throw new ArgumentOutOfRangeException(nameof(offset), "Offset " + offset + " is outside the bounds of the buffer (" + buffer.Length + ").");
			if (count < 0 || count > buffer.Length - offset)
				throw new ArgumentOutOfRangeException(nameof(count), "Count " + count + " must be no smaller than 0 but no larger than (buffer.Length - offset) (" + (buffer.Length - offset) + ").");

			int totalBytesRead = 0;

			if (currentUnderlyingStream == null)
			{
				currentUnderlyingStream = getStream(nextUnderlyingStreamIndex);
				nextUnderlyingStreamIndex++;
			}

			while (count > 0 && currentUnderlyingStream != null)
			{
				cancellationToken.ThrowIfCancellationRequested();

				int bytesRead = await currentUnderlyingStream.ReadAsync(buffer, offset, count, cancellationToken);

				totalBytesRead += bytesRead;
				offset += bytesRead;
				count -= bytesRead;

				if (bytesRead == 0)
				{
					currentUnderlyingStream = getStream(nextUnderlyingStreamIndex);
					nextUnderlyingStreamIndex++;
				}
			}

			return totalBytesRead;
		}
		/// <summary>
		/// Returns true.
		/// </summary>
		public override bool CanRead => true;
		/// <summary>
		/// Returns false.
		/// </summary>
		public override bool CanSeek => false;
		/// <summary>
		/// Returns false.
		/// </summary>
		public override bool CanWrite => false;
		/// <summary>
		/// Not Implemented. Throws <see cref="NotImplementedException"/>.
		/// </summary>
		public override void Flush()
		{
			throw new NotImplementedException();
		}
		/// <summary>
		/// Not Implemented. Throws <see cref="NotImplementedException"/>.
		/// </summary>
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotImplementedException();
		}
		/// <summary>
		/// Not Implemented. Throws <see cref="NotImplementedException"/>.
		/// </summary>
		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}
		/// <summary>
		/// Not Implemented. Throws <see cref="NotImplementedException"/>.
		/// </summary>
		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}
		/// <summary>
		/// Not Implemented. Throws <see cref="NotImplementedException"/>.
		/// </summary>
		public override long Length => throw new NotImplementedException();
		/// <summary>
		/// Not Implemented. Throws <see cref="NotImplementedException"/>.
		/// </summary>
		public override long Position
		{
			get { throw new NotImplementedException(); }
			set { throw new NotImplementedException(); }
		}
	}
}