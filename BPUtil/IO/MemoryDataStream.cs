using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using BPUtil;

namespace BPUtil
{
	public class MemoryDataStream : MemoryStream, IDataStream
	{
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
	}
}
