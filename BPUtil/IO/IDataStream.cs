using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.IO
{
	public interface IDataStream
	{
		void Write(byte[] buffer, int offset, int count);
		void Write(byte[] buffer);
		int Read(byte[] buffer, int offset, int count);
		void WriteByte(byte b);
		void WriteInt16(short num);
		void WriteUInt16(ushort num);
		void WriteInt32(int num);
		void WriteUInt32(uint num);
		void WriteInt64(long num);
		void WriteUInt64(ulong num);
		void WriteFloat(float num);
		void WriteDouble(double num);
		int WriteUtf8(string str);
		ushort WriteUtf8_16(string str);
		uint WriteUtf8_32(string str);
		sbyte ReadSByte();
		byte ReadActualByte();
		int ReadByte();
		short ReadInt16();
		ushort ReadUInt16();
		int ReadInt32();
		uint ReadUInt32();
		long ReadInt64();
		ulong ReadUInt64();
		float ReadHalf();
		float ReadFloat();
		double ReadDouble();
		string ReadUtf8(int lengthBytes);
		string ReadUtf8_16();
		string ReadUtf8_32();
		short ReadInt16LE();
		ushort ReadUInt16LE();
		int ReadInt32LE();
		uint ReadUInt32LE();
		long ReadInt64LE();
		ulong ReadUInt64LE();
		float ReadHalfLE();
		float ReadFloatLE();
		double ReadDoubleLE();
		byte[] ReadNBytes(int length);
		byte[] ReadNBytesFromNetworkOrder(int length);
		/// <summary>
		/// Moves the current position by the specified number of bytes.
		/// </summary>
		/// <param name="numBytes">Number of bytes to offset the current seek position.</param>
		void Skip(int numBytes);
		/// <summary>
		/// Gets a value indicating whether the current stream supports seeking.
		/// </summary>
		bool CanSeek { get; }
		/// <summary>
		/// Gets the length in bytes of the stream.
		/// </summary>
		/// <exception cref="T:System.NotSupportedException">The base stream does not support seeking. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		long Length { get; }
		/// <summary>
		/// Gets or sets the position within the current stream.
		/// </summary>
		/// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
		/// <exception cref="T:System.NotSupportedException">The base stream does not support seeking. </exception>
		/// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
		long Position { get; set; }
	}
}
