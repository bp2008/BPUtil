using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BPUtil;

namespace BPUtil.IO
{
	public class MemoryDataStream : MemoryStream, IDataStream
	{
		/// <summary>
		/// Initializes a new non-resizable instance of the MemoryDataStream class by reading a specified number of bytes from the provided IDataStream.
		/// </summary>
		/// <param name="stream">The IDataStream to copy data from for initialization of the MemoryDataStream.</param>
		/// <param name="length">The number of bytes to read from the IDataStream.  This will be the size of the new MemoryDataStream.</param>
		public MemoryDataStream(IDataStream stream, int length) : base(ByteUtil.ReadNBytes(stream, length))
		{
		}

		/// <summary>
		/// Initializes a new non-resizable instance of the MemoryDataStream class by reading a specified number of bytes from the provided Stream.
		/// </summary>
		/// <param name="stream">The Stream to copy data from for initialization of the MemoryDataStream.</param>
		/// <param name="length">The number of bytes to read from the Stream.  This will be the size of the new MemoryDataStream.</param>
		public MemoryDataStream(Stream stream, int length) : base(ByteUtil.ReadNBytes(stream, length))
		{
		}

		/// <summary>
		/// Initializes a new non-resizable instance of the MemoryDataStream class based on the specified byte array.
		/// </summary>
		/// <param name="buffer">The array of unsigned bytes from which to create the current stream.</param>
		public MemoryDataStream(byte[] buffer) : base(buffer)
		{
		}

		/// <summary>
		/// Initializes a new instance of the MemoryDataStream class with an expandable capacity initialized as specified.
		/// </summary>
		/// <param name="capacity">The initial size of the internal array in bytes.</param>
		public MemoryDataStream(int capacity) : base(capacity)
		{
		}

		/// <summary>
		/// Initializes a new instance of the MemoryDataStream class with an expandable capacity initialized to zero.
		/// </summary>
		public MemoryDataStream()
		{
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
	}
}
