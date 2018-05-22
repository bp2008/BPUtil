using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
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
		int ReadByte();
		short ReadInt16();
		ushort ReadUInt16();
		int ReadInt32();
		uint ReadUInt32();
		long ReadInt64();
		ulong ReadUInt64();
		float ReadFloat();
		double ReadDouble();
		string ReadUtf8(int lengthBytes);
		byte[] ReadNBytes(int length);
		byte[] ReadNBytesFromNetworkOrder(int length);
	}
}
