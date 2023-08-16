using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp
{

	/// <summary>
	/// <para>A Stream that reads data using HTTP transfer-encoding: chunked.</para>
	/// <para>Only the Read and ReadAsync methods are intended to be used; all other methods and properties are either not implemented or are simply proxied to the underlying stream.  Writing to the stream is explicitly not allowed.</para>
	/// <para>The ReadableChunkedTransferEncodingStream does not automatically close the underlying stream.  As such, it is unnecessary to use the ReadableChunkedTransferEncodingStream within a "using" block or to call Close or Dispose.</para>
	/// </summary>
	public class ReadableChunkedTransferEncodingStream : Stream
	{
		private readonly Stream _stream;
		private bool streamEnded = false;

		/// <summary>
		/// Initializes a new instance of the ReadableChunkedTransferEncodingStream class.
		/// </summary>
		/// <param name="stream">The underlying stream to write to.</param>
		public ReadableChunkedTransferEncodingStream(Stream stream)
		{
			_stream = stream;
		}

		/// <inheritdoc />
		public override bool CanRead => _stream.CanRead;
		/// <inheritdoc />
		public override bool CanSeek => false;
		/// <inheritdoc />
		public override bool CanWrite => false;
		/// <inheritdoc />
		public override long Length => _stream.Length;
		/// <inheritdoc />
		public override long Position
		{
			get => _stream.Position;
			set => _stream.Position = value;
		}

		/// <inheritdoc />
		public override void Flush()
		{
			_stream.Flush();
		}
		/// <inheritdoc />
		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			return _stream.FlushAsync(cancellationToken);
		}
		long remainingThisChunk = 0;
		/// <inheritdoc />
		public override int Read(byte[] buffer, int offset, int count)
		{
			if (count > buffer.Length - offset)
				throw new ArgumentOutOfRangeException("count", "Reading " + count + " bytes at offset " + offset + " would require a " + (offset + count) + "-byte buffer.  The given buffer has length " + buffer.Length);
			if (streamEnded)
				return 0;

			int totalRead = 0;
			while (totalRead < count)
			{
				if (remainingThisChunk == 0)
					remainingThisChunk = ReadChunkSizeLine();
				if (remainingThisChunk == 0)
				{
					streamEnded = true;
					ReadChunkTrailer();
					return totalRead;
				}
				int toRead = (int)Math.Min(remainingThisChunk, count - totalRead);
				int justRead = _stream.Read(buffer, offset, toRead);
				if (justRead == 0)
					throw new EndOfStreamException("ReadableChunkedTransferEncodingStream encountered end of stream with " + remainingThisChunk + " bytes of a chunk remaining to read.");
				totalRead += justRead;
				offset += justRead;
				remainingThisChunk -= justRead;
				if (remainingThisChunk < 0)
					throw new ApplicationException("ReadableChunkedInputStream somehow read too much.");
				if (remainingThisChunk == 0)
					ReadChunkTrailer();
			}

			return totalRead;
		}

		private long ReadChunkSizeLine()
		{
			string chunkSizeLine = HttpProcessor.streamReadLine(_stream);
			if (chunkSizeLine == null)
				throw new EndOfStreamException("The end of the stream was encountered when attempting to read the next chunk header.");

			long chunkSize = long.Parse(chunkSizeLine.Split(';')[0], System.Globalization.NumberStyles.HexNumber);
			return chunkSize;
		}
		private void ReadChunkTrailer()
		{
			string trailerLine = HttpProcessor.streamReadLine(_stream);
			if (trailerLine != "")
				throw new InvalidDataException();
		}
		/// <inheritdoc />
		public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (count > buffer.Length - offset)
				throw new ArgumentOutOfRangeException("count", "Reading " + count + " bytes at offset " + offset + " would require a " + (offset + count) + "-byte buffer.  The given buffer has length " + buffer.Length);
			if (streamEnded)
				return 0;

			int totalRead = 0;
			while (totalRead < count)
			{
				cancellationToken.ThrowIfCancellationRequested();

				if (remainingThisChunk == 0)
					remainingThisChunk = await ReadChunkSizeLineAsync();
				if (remainingThisChunk == 0)
				{
					streamEnded = true;
					ReadChunkTrailer();
					return totalRead;
				}
				cancellationToken.ThrowIfCancellationRequested();
				int toRead = (int)Math.Min(remainingThisChunk, count - totalRead);
				int justRead = await _stream.ReadAsync(buffer, offset, toRead, cancellationToken);
				if (justRead == 0)
					throw new EndOfStreamException("ReadableChunkedTransferEncodingStream encountered end of stream with " + remainingThisChunk + " bytes of a chunk remaining to read.");
				totalRead += justRead;
				offset += justRead;
				remainingThisChunk -= justRead;
				if (remainingThisChunk < 0)
					throw new ApplicationException("ReadableChunkedInputStream somehow read too much.");
				cancellationToken.ThrowIfCancellationRequested();
				if (remainingThisChunk == 0)
					await ReadChunkTrailerAsync();
			}

			return totalRead;
		}

		private async Task<long> ReadChunkSizeLineAsync()
		{
			string chunkSizeLine = await Task.Run(() => HttpProcessor.streamReadLine(_stream));
			if (chunkSizeLine == null)
				throw new EndOfStreamException("The end of the stream was encountered when attempting to read the next chunk header.");

			long chunkSize = long.Parse(chunkSizeLine.Split(';')[0], System.Globalization.NumberStyles.HexNumber);
			return chunkSize;
		}
		private async Task ReadChunkTrailerAsync()
		{
			string trailerLine = await Task.Run(() => HttpProcessor.streamReadLine(_stream));
			if (trailerLine != "")
				throw new InvalidDataException();
		}

		///// <summary>
		///// Asynchronously reads a sequence of bytes from the current stream using chunked transfer encoding and advances the position within the stream by the number of bytes read.
		///// </summary>
		///// <param name="buffer">The buffer to write the data into.</param>
		///// <param name="offset">The byte offset in buffer at which to begin writing data from the stream.</param>
		///// <param name="count">The maximum number of bytes to read.</param>
		///// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is System.Threading.CancellationToken.None.</param>
		///// <returns>A task that represents the asynchronous read operation. The value of the TResult parameter contains the total number of bytes read into the buffer. The result value can be less than the number of bytes requested if the number of bytes currently available is less than the requested number, or it can be 0 (zero) if the end of the stream has been reached.</returns>
		//public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		//{
		//	return _stream.ReadAsync(buffer, offset, count, cancellationToken);
		//}
		/// <inheritdoc />
		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}
		/// <inheritdoc />
		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}
		/// <inheritdoc />
		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException("Writing to a ReadableChunkedTransferEncodingStream is not supported.");
		}

		/// <inheritdoc />
		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			throw new NotSupportedException("Writing to a ReadableChunkedTransferEncodingStream is not supported.");
		}
	}
}
