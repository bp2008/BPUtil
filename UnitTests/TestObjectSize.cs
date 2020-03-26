using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestObjectSize
	{
		[TestMethod]
		public void TestObjectSizeExhaustive()
		{
			// Strings
			// strings have an overhead of 4 bytes for the m_stringLength field and 2 bytes for the m_firstChar field.
			Assert.AreEqual(14, ObjectSize.SizeOf("test"));

			Assert.AreEqual(16, ObjectSize.SizeOf("tests"));

			// Primitive Types

			Assert.AreEqual(1, ObjectSize.SizeOf(false));

			Assert.AreEqual(1, ObjectSize.SizeOf(true));

			Assert.AreEqual(1, ObjectSize.SizeOf(byte.MaxValue));

			Assert.AreEqual(1, ObjectSize.SizeOf(sbyte.MaxValue));

			Assert.AreEqual(2, ObjectSize.SizeOf(ushort.MaxValue));

			Assert.AreEqual(2, ObjectSize.SizeOf(short.MaxValue));

			Assert.AreEqual(4, ObjectSize.SizeOf(uint.MaxValue));

			Assert.AreEqual(4, ObjectSize.SizeOf(int.MaxValue));

			Assert.AreEqual(8, ObjectSize.SizeOf(ulong.MaxValue));

			Assert.AreEqual(8, ObjectSize.SizeOf(long.MaxValue));

			Assert.AreEqual(2, ObjectSize.SizeOf(char.MaxValue));

			Assert.AreEqual(4, ObjectSize.SizeOf(float.MaxValue));

			Assert.AreEqual(8, ObjectSize.SizeOf(double.MaxValue));

			Assert.AreEqual(16, ObjectSize.SizeOf(decimal.MaxValue));

			Assert.AreEqual(8, ObjectSize.SizeOf(DateTime.MinValue));

			Assert.AreEqual(ObjectSize.ReferenceSize, ObjectSize.SizeOf(UIntPtr.Zero));

			Assert.AreEqual(ObjectSize.ReferenceSize, ObjectSize.SizeOf(IntPtr.Zero));

			// Arrays of primitive types

			Assert.AreEqual(30, ObjectSize.SizeOf(new bool[30]));

			Assert.AreEqual(30, ObjectSize.SizeOf(new byte[30]));

			Assert.AreEqual(60, ObjectSize.SizeOf(new char[30]));

			Assert.AreEqual(60, ObjectSize.SizeOf(new short[30]));

			Assert.AreEqual(120, ObjectSize.SizeOf(new int[30]));

			Assert.AreEqual(240, ObjectSize.SizeOf(new long[30]));

			Assert.AreEqual(480, ObjectSize.SizeOf(new decimal[30]));

			Assert.AreEqual(0, ObjectSize.SizeOf(new decimal[0]));

			// Array of string

			string[] strArr = new string[] { "test", "tests" };
			int strArrExpectedSize = 30 + (ObjectSize.ReferenceSize * 2);
			Assert.AreEqual(strArrExpectedSize, ObjectSize.SizeOf(strArr));

			// Array of objects
			object[] objArr = new object[] {
				/* 14 */ "test",
				/* 1  */ byte.MinValue,
				/* 1  */ false,
				/* 8  */ long.MinValue
			};
			int objArrExpectedSize = 14 + 1 + 1 + 8 + (ObjectSize.ReferenceSize * 4);
			Assert.AreEqual(objArrExpectedSize, ObjectSize.SizeOf(objArr));

			// Generic Lists

			int list_overhead = 0;
			list_overhead += ObjectSize.ReferenceSize; // Reference to internal array
			list_overhead += 4; // (int) size
			list_overhead += 4; // (int) _version
			list_overhead += ObjectSize.ReferenceSize; // Reference to internal syncroot object (which will be null)

			List<byte> list_byte = new List<byte>(3);
			Assert.AreEqual(list_overhead + 3, ObjectSize.SizeOf(list_byte));

			List<string> list_string = new List<string>(strArr);
			Assert.AreEqual(list_overhead + strArrExpectedSize, ObjectSize.SizeOf(list_string));

			List<object> list_object = new List<object>(objArr);
			Assert.AreEqual(list_overhead + objArrExpectedSize, ObjectSize.SizeOf(list_object));

			List<object> list_object_nulls = new List<object>(3);
			Assert.AreEqual(list_overhead + (ObjectSize.ReferenceSize * 3), ObjectSize.SizeOf(list_object_nulls));

			// Complex objects

			object dyn = new { f1 = "tests", f2 = false, f3 = new { f1 = (int)234190, f2 = float.MinValue } };
			// Expected: ((ref size) + 16 + 1 + (ref size) + 4 + 4) = 24 + (ref size) + (ref size)
			Assert.AreEqual(25 + (ObjectSize.ReferenceSize * 2), ObjectSize.SizeOf(dyn));

			// Expected: ((ref size) + 16 + 1 + (ref size) + 4 + 4 + 4 + 4 + 4) = 36 + (ref size) + (ref size)
			Assert.AreEqual(37 + (ObjectSize.ReferenceSize * 2), ObjectSize.SizeOf(new InternalClass1()));
		}
		class InternalClass1
		{
			public string f1 = "tests";
			private bool f2 = false;
			protected InternalClass2 f3 = new InternalClass2();
			internal InternalStruct1 f4 = new InternalStruct1() { f1 = 234190, f2 = float.MinValue };
		}
		class InternalClass2
		{
			public int f1 = 234190;
			public float f2 = float.MinValue;
		}
		struct InternalStruct1
		{
			public int f1;
			public float f2;
			private int f3;
		}
	}
}
