using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp
{

	/// <summary>
	/// <para>A Stream that writes data using HTTP transfer-encoding: chunked.</para>
	/// <para>Only the Write and WriteAsync methods are intended to be used; all other methods and properties are either not implemented or are simply proxied to the underlying stream.</para>
	/// <para>The WritableChunkedTransferEncodingStream does not automatically close the underlying stream.  However it does write the final chunk if that hasn't been done already.</para>
	/// </summary>
	public class WritableChunkedTransferEncodingStream : Stream
	{
		private readonly Stream _stream;
		private bool streamEnded = false;
		/// <summary>
		/// Initializes a new instance of the WritableChunkedTransferEncodingStream class.
		/// </summary>
		/// <param name="stream">The underlying stream to write to.</param>
		public WritableChunkedTransferEncodingStream(Stream stream)
		{
			_stream = stream;
		}
		/// <summary>
		/// Writes the final chunk if that hasn't been done already.  The underlying stream is not closed.  You should discontinue use of the WritableChunkedTransferEncodingStream after calling this.
		/// </summary>
		public override void Close()
		{
			if (!streamEnded)
				WriteFinalChunk();
		}

		/// <inheritdoc />
		public override bool CanRead => _stream.CanRead;
		/// <inheritdoc />
		public override bool CanSeek => _stream.CanSeek;
		/// <inheritdoc />
		public override bool CanWrite => _stream.CanWrite;
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
		/// <inheritdoc />
		public override int Read(byte[] buffer, int offset, int count)
		{
			return _stream.Read(buffer, offset, count);
		}
		/// <inheritdoc />
		public override long Seek(long offset, SeekOrigin origin)
		{
			return _stream.Seek(offset, origin);
		}
		/// <inheritdoc />
		public override void SetLength(long value)
		{
			_stream.SetLength(value);
		}
		/// <summary>
		/// Writes a sequence of bytes to the current stream using chunked transfer encoding.  If count is 0 (or lower) then this method will not write anything because an empty chunk is considered to be the end of the payload.
		/// </summary>
		/// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
		/// <param name="count">The number of bytes to be written to the current stream.</param>
		public override void Write(byte[] buffer, int offset, int count)
		{
			if (streamEnded)
				throw new ApplicationException("WritableChunkedTransferEncodingStream.Write() is not allowed to be called after WriteFinalChunk() or WriteFinalChunkAsync() is called.");
			if (count <= 0)
				return;
			WriteChunkHeader(count);
			_stream.Write(buffer, offset, count);
			WriteChunkTrailer();
		}

		/// <summary>
		/// Asynchronously writes a sequence of bytes to the current stream using chunked transfer encoding.  If count is 0 (or lower) then this method will not write anything because an empty chunk is considered to be the end of the payload.
		/// </summary>
		/// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
		/// <param name="count">The number of bytes to be written to the current stream.</param>
		/// <param name="cancellationToken">A CancellationToken which can be used to cancel the operation.</param>
		public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (streamEnded)
				throw new ApplicationException("WritableChunkedTransferEncodingStream.WriteAsync() is not allowed to be called after WriteFinalChunk() or WriteFinalChunkAsync() is called.");
			if (count <= 0)
				return;
			await WriteChunkHeaderAsync(count, cancellationToken);
			await _stream.WriteAsync(buffer, offset, count, cancellationToken);
			await WriteChunkTrailerAsync(cancellationToken);
		}
		/// <summary>
		/// Writes an empty chunk to the stream, indicating that the HTTP response is completed.  You should discontinue use of the WritableChunkedTransferEncodingStream after calling this.
		/// </summary>
		internal void WriteFinalChunk()
		{
			if (streamEnded)
				throw new ApplicationException("WritableChunkedTransferEncodingStream.WriteFinalChunk() is not allowed to be called after WriteTrailingChunk() or WriteFinalChunkAsync() is called.");
			streamEnded = true;
			WriteChunkHeader(0);
			WriteChunkTrailer();
		}

		/// <summary>
		/// Asynchronously writes an empty chunk to the stream, indicating that the HTTP response is completed.  You should discontinue use of the WritableChunkedTransferEncodingStream after calling this.
		/// </summary>
		/// <param name="cancellationToken">A CancellationToken which can be used to cancel the operation.</param>
		internal async Task WriteFinalChunkAsync(CancellationToken cancellationToken)
		{
			if (streamEnded)
				throw new ApplicationException("WritableChunkedTransferEncodingStream.WriteFinalChunkAsync() is not allowed to be called after WriteTrailingChunk() or WriteTrailingChunkAsync() is called.");
			streamEnded = true;
			await WriteChunkHeaderAsync(0, cancellationToken);
			await WriteChunkTrailerAsync(cancellationToken);
		}

		private void WriteChunkHeader(int count)
		{
			string header = count.ToString("X") + "\r\n";
			byte[] headerBytes = Encoding.ASCII.GetBytes(header);
			_stream.Write(headerBytes, 0, headerBytes.Length);
		}

		private Task WriteChunkHeaderAsync(int count, CancellationToken cancellationToken)
		{
			string header = count.ToString("X") + "\r\n";
			byte[] headerBytes = Encoding.ASCII.GetBytes(header);
			return _stream.WriteAsync(headerBytes, 0, headerBytes.Length, cancellationToken);
		}

		private void WriteChunkTrailer()
		{
			string trailer = "\r\n";
			byte[] trailerBytes = Encoding.ASCII.GetBytes(trailer);
			_stream.Write(trailerBytes, 0, trailerBytes.Length);
		}

		private Task WriteChunkTrailerAsync(CancellationToken cancellationToken)
		{
			string trailer = "\r\n";
			byte[] trailerBytes = Encoding.ASCII.GetBytes(trailer);
			return _stream.WriteAsync(trailerBytes, 0, trailerBytes.Length, cancellationToken);
		}
	}
}
