using System;
using System.IO;

namespace BPUtil.IO
{
	/// <summary>
	/// A stream you can use to wrap another stream to provide convenient Read and Write functions for various common types.
	/// </summary>
	public class BasicDataStream : Stream, IDataStream, IDisposable
	{
		public readonly Stream originalStream;
		public readonly bool leaveOpen;
		/// <summary>
		/// Initializes a new instance of the BasicDataStream class by wrapping another stream.
		/// </summary>
		/// <param name="stream">The original stream to wrap.</param>
		/// <param name="leaveOpen">If true, the inner stream will not be closed or disposed when the outer stream is.</param>
		public BasicDataStream(Stream stream, bool leaveOpen = false)
		{
			originalStream = stream;
			this.leaveOpen = leaveOpen;
		}

		/// <summary>
		/// Closes the underlying stream if this BasicDataStream was configured to do so during construction.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (!leaveOpen)
					originalStream.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Writes a block of bytes to the current stream using data read from a buffer. With this overload, the entire buffer will be written.
		/// </summary>
		/// <param name="buffer">The buffer to write data from.</param>
		public void Write(byte[] buffer)
		{
			Write(buffer, 0, buffer.Length);
		}
		public void WriteInt16(short num)
		{
			ByteUtil.WriteInt16(num, this);
		}
		public void WriteUInt16(ushort num)
		{
			ByteUtil.WriteUInt16(num, this);
		}
		public void WriteInt32(int num)
		{
			ByteUtil.WriteInt32(num, this);
		}
		public void WriteUInt32(uint num)
		{
			ByteUtil.WriteUInt32(num, this);
		}
		public void WriteInt64(long num)
		{
			ByteUtil.WriteInt64(num, this);
		}
		public void WriteUInt64(ulong num)
		{
			ByteUtil.WriteUInt64(num, this);
		}
		public void WriteFloat(float num)
		{
			ByteUtil.WriteFloat(num, this);
		}
		public void WriteDouble(double num)
		{
			ByteUtil.WriteDouble(num, this);
		}
		/// <summary>
		/// Writes the string as UTF8 (no byte order mark).
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public int WriteUtf8(string str)
		{
			return ByteUtil.WriteUtf8(str, this);
		}
		/// <summary>
		/// <para>Writes the length of the string as a 16 bit unsigned integer, then writes the string.</para>
		/// <para>The string will be encoded as UTF8 with no byte order mark.</para>
		/// <para>Returns the number of bytes written.</para>
		/// <para>Throws an exception if the byte array is larger than a 16 bit unsigned integer can hold.</para>
		/// </summary>
		/// <param name="str">String to write.</param>
		/// <exception cref="ArgumentException">If the string is longer than 65535 characters or bytes.</exception>
		public ushort WriteUtf8_16(string str)
		{
			return ByteUtil.WriteUtf8_16(str, this);
		}
		/// <summary>
		/// <para>Writes the length of the string as a 32 bit unsigned integer, then writes the string.</para>
		/// <para>The string will be encoded as UTF8 with no byte order mark.</para>
		/// <para>Returns the number of bytes written.</para>
		/// </summary>
		/// <param name="str">String to write.</param>
		public uint WriteUtf8_32(string str)
		{
			return ByteUtil.WriteUtf8_32(str, this);
		}
		public short ReadInt16()
		{
			return ByteUtil.ReadInt16(this);
		}
		public ushort ReadUInt16()
		{
			return ByteUtil.ReadUInt16(this);
		}
		public int ReadInt32()
		{
			return ByteUtil.ReadInt32(this);
		}
		public uint ReadUInt32()
		{
			return ByteUtil.ReadUInt32(this);
		}
		public long ReadInt64()
		{
			return ByteUtil.ReadInt64(this);
		}
		public ulong ReadUInt64()
		{
			return ByteUtil.ReadUInt64(this);
		}
		public float ReadHalf()
		{
			return ByteUtil.ReadHalf(this);
		}
		public float ReadFloat()
		{
			return ByteUtil.ReadFloat(this);
		}
		public double ReadDouble()
		{
			return ByteUtil.ReadDouble(this);
		}
		public string ReadUtf8(int lengthBytes)
		{
			return ByteUtil.ReadUtf8(this, lengthBytes);
		}
		/// <summary>
		/// Reads a UTF8 string (no byte order mark) from the stream, assuming the string's length is prepended as a 16 bit unsigned integer.
		/// </summary>
		/// <returns></returns>
		public string ReadUtf8_16()
		{
			return ByteUtil.ReadUtf8_16(this);
		}
		/// <summary>
		/// Reads a UTF8 string (no byte order mark) from the stream, assuming the string's length is prepended as a 32 bit unsigned integer.
		/// </summary>
		/// <returns></returns>
		public string ReadUtf8_32()
		{
			return ByteUtil.ReadUtf8_32(this);
		}
		/// <summary>
		/// Reads a specific number of bytes from the stream, returning a byte array.  Ordinary stream.Read operations are not guaranteed to read all the requested bytes.
		/// </summary>
		/// <param name="length">The number of bytes to read.</param>
		/// <returns></returns>
		public byte[] ReadNBytes(int length)
		{
			return ByteUtil.ReadNBytes((Stream)this, length);
		}
		/// <summary>
		/// Reads a specific number of bytes from the stream, returning a byte array.  Ordinary stream.Read operations are not guaranteed to read all the requested bytes.
		/// </summary>
		/// <param name="length">The number of bytes to read.</param>
		/// <returns></returns>
		public byte[] ReadNBytesFromNetworkOrder(int length)
		{
			return ByteUtil.ReadNBytesFromNetworkOrder((Stream)this, length);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return originalStream.Read(buffer, offset, count);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			originalStream.Write(buffer, offset, count);
		}
		/// <summary>
		/// Moves the current position by the specified number of bytes.
		/// </summary>
		/// <param name="numBytes">Number of bytes to offset the current seek position.</param>
		public void Skip(int numBytes)
		{
			Seek(numBytes, SeekOrigin.Current);
		}
		public sbyte ReadSByte()
		{
			return ByteUtil.ReadSByte(this);
		}
		public byte ReadActualByte()
		{
			return (byte)ReadByte();
		}
		public short ReadInt16LE()
		{
			return ByteUtil.ReadInt16LE(this);
		}
		public ushort ReadUInt16LE()
		{
			return ByteUtil.ReadUInt16LE(this);
		}
		public int ReadInt32LE()
		{
			return ByteUtil.ReadInt32LE(this);
		}
		public uint ReadUInt32LE()
		{
			return ByteUtil.ReadUInt32LE(this);
		}
		public long ReadInt64LE()
		{
			return ByteUtil.ReadInt64LE(this);
		}
		public ulong ReadUInt64LE()
		{
			return ByteUtil.ReadUInt64LE(this);
		}
		public float ReadHalfLE()
		{
			return ByteUtil.ReadHalfLE(this);
		}
		public float ReadFloatLE()
		{
			return ByteUtil.ReadFloatLE(this);
		}
		public double ReadDoubleLE()
		{
			return ByteUtil.ReadDoubleLE(this);
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
			if (!leaveOpen)
				originalStream.Close();
		}
		#endregion
		#endregion
	}
}
