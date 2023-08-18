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
	/// <para>A stream which provides read/write access to the next N bytes of another stream.</para>
	/// <para>If you specify a substream for the purpose of reading that is longer than the underlying stream, then the Substream will simply end when the underlying stream does, which may be earlier than you expected.</para>
	/// <para>If you specify a substream for the purpose of writing that is longer than the underlying stream, then the behavior will vary depending on the underlying stream, but typically (such as when the underlying stream is a FileStream) the underlying stream will become longer.</para>
	/// <para>If you attempt to write to a Substream more data than the length you specified during Substream construction, an EndOfStreamException will be thrown and the offending call to Write() will have no effect on the underlying stream.</para>
	/// <para>Operations such as Read/Write, Seek, Position get/set, are only supported if supported by the underlying stream.</para>
	/// <para>Behavior of the Substream will be incorrect if the underlying stream is modified while you're using the Substream.</para>
	/// <para>It is not necessary to dispose a Substream.</para>
	/// </summary>
	public class Substream : Stream
	{
		/// <summary>
		/// The underlying stream.
		/// </summary>
		private readonly Stream _stream = null;
		/// <summary>
		/// The byte offset into the underlying stream when this Substream was constructed.
		/// </summary>
		private readonly long _offset = 0;
		/// <summary>
		/// The length of the Substream, in bytes.
		/// </summary>
		private readonly long _length = 0;
		/// <summary>
		/// This Substream's current position between 0 and <see cref="_length"/>.
		/// </summary>
		private long _position = 0;
		/// <summary>
		/// Gets a value indicating if this Substream is currently positioned at the end of the stream.
		/// </summary>
		public bool EndOfStream => _position >= _length;

		/// <summary>
		/// Creates a Substream that represents the next <paramref name="length"/> bytes of the underlying stream from its current position.
		/// </summary>
		/// <param name="stream">The underlying stream.</param>
		/// <param name="length">The number of bytes of the underlying stream which can be read via this Substream.</param>
		public Substream(Stream stream, long length)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			if (!stream.CanRead)
				throw new ArgumentException("The underlying stream does not support reading.");
			if (length < 0)
				throw new ArgumentOutOfRangeException(nameof(length), "Length must be >= 0.");

			_stream = stream;
			_length = length;
			try
			{
				_offset = _stream.Position; // If this fails, _offset remains 0 and seeking is expected to be unsupported.
			}
			catch { }
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
			if (_position >= _length)
				return 0;
			int toRead = (int)Math.Min(count, _length - _position);
			int read = _stream.Read(buffer, offset, toRead);
			_position += read;
			return read;
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
			if (_position >= _length)
				return 0;
			cancellationToken.ThrowIfCancellationRequested();
			int toRead = (int)Math.Min(count, _length - _position);
			int read = await _stream.ReadAsync(buffer, offset, toRead, cancellationToken);
			_position += read;
			return read;
		}

		/// <inheritdoc />
		public override void Write(byte[] buffer, int offset, int count)
		{
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			if (offset != offset.Clamp(0, buffer.Length))
				throw new ArgumentOutOfRangeException(nameof(offset), "Offset " + offset + " is outside the bounds of the buffer (" + buffer.Length + ").");
			if (count < 0 || count > buffer.Length - offset)
				throw new ArgumentOutOfRangeException(nameof(count), "Count " + count + " must be no smaller than 0 but no larger than (buffer.Length - offset) (" + (buffer.Length - offset) + ").");
			if (_position + count > _length)
				throw new EndOfStreamException("Unable to write " + count + " bytes to Substream with " + (_length - _position) + " bytes remaining.");
			_stream.Write(buffer, offset, count);
		}
		/// <inheritdoc />
		public override bool CanRead => _stream.CanRead;
		/// <inheritdoc />
		public override bool CanSeek => _stream.CanSeek;
		/// <inheritdoc />
		public override bool CanWrite => _stream.CanWrite;
		/// <inheritdoc />
		public override long Length => _length;

		/// <inheritdoc />
		public override int ReadTimeout
		{
			get => _stream.ReadTimeout;
			set => _stream.ReadTimeout = value;
		}

		/// <inheritdoc />
		public override int WriteTimeout
		{
			get => _stream.WriteTimeout;
			set => _stream.WriteTimeout = value;
		}
		/// <inheritdoc />
		public override long Position
		{
			get => _stream.Position - _offset;
			set
			{
				if (value > _length)
					throw new ArgumentOutOfRangeException("value", "Value " + value + " is greater than the length of this Substream (" + _length + ").");
				_stream.Position = value + _offset;
			}
		}

		/// <inheritdoc />
		public override void Flush() => _stream.Flush();
		/// <inheritdoc />
		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			return _stream.FlushAsync(cancellationToken);
		}
		/// <inheritdoc />
		public override void SetLength(long value)
		{
			throw new NotSupportedException("Substream does not support SetLength().");
		}
		/// <inheritdoc />
		public override long Seek(long offset, SeekOrigin origin)
		{
			if (origin == SeekOrigin.Begin)
				SeekTo(offset);
			else if (origin == SeekOrigin.End)
				SeekTo(_length + offset);
			else if (origin == SeekOrigin.Current)
				SeekTo(_position + offset);
			return _position;
		}
		/// <summary>
		/// Seeks to the specified offset relative to the beginning of this Substream.  Includes bounds checking.
		/// </summary>
		/// <param name="offset">Offset relative to the beginning of this Substream.</param>
		private void SeekTo(long offset)
		{
			if (offset != offset.Clamp(0, _length))
				throw new ArgumentOutOfRangeException(nameof(offset));
			long newPosition = offset;
			_stream.Seek(_offset + offset, SeekOrigin.Begin);
			_position = newPosition;
		}
	}
}