using System;
using System.Collections.Generic;
using System.Threading;
using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestDummyValues
	{
		[TestMethod]
		public void TestDummyValues_1()
		{
			SO so = new SO();
			DummyValues.SetDummyValues(so);

			AssertSO(so);

			CO co = new CO();
			DummyValues.SetDummyValues(co);

			AssertCO(co);
		}
		[TestMethod]
		public void TestDummyValues_2()
		{
			SO so = (SO)DummyValues.GetDummyValue(typeof(SO));

			AssertSO(so);

			CO co = (CO)DummyValues.GetDummyValue(typeof(CO));

			AssertCO(co);
		}
		private void AssertCO(CO co)
		{
			Assert.AreEqual("test", co._string);
			AssertSO(co._so);
			Assert.AreEqual(2, co._soList.Count);
			AssertSO(co._soList[0]);
			AssertSO(co._soList[1]);
			Assert.AreEqual(2, co._soArray.Length);
			AssertSO(co._soArray[0]);
			AssertSO(co._soArray[1]);
		}
		private void AssertSO(SO so)
		{
			Assert.IsTrue(so._bool);
			Assert.AreEqual((sbyte)1, so._sbyte);
			Assert.AreEqual((byte)2, so._byte);
			Assert.AreEqual((short)3, so._short);
			Assert.AreEqual((ushort)4, so._ushort);
			Assert.AreEqual((int)5, so._int);
			Assert.AreEqual((uint)6, so._uint);
			Assert.AreEqual((long)7, so._long);
			Assert.AreEqual((ulong)8, so._ulong);
			Assert.AreEqual((float)9, so._float);
			Assert.AreEqual((double)10, so._double);
			Assert.AreEqual((decimal)11, so._decimal);
			Assert.AreEqual((EN)12, so._enum);
			Assert.AreEqual("test", so._string);
			Assert.AreEqual(2, so._byteArray.Length);
			Assert.AreEqual((byte)2, so._byteArray[0]);
			Assert.AreEqual((byte)2, so._byteArray[1]);
			Assert.AreEqual(2, so._stringArray.Length);
			Assert.AreEqual("test", so._stringArray[0]);
			Assert.AreEqual("test", so._stringArray[1]);
		}
#pragma warning disable CS0649
		class CO
		{
			public string _string;
			public SO _so;
			public List<SO> _soList;
			public SO[] _soArray;
		}
		class SO
		{
			public bool _bool;
			public sbyte _sbyte;
			public byte _byte;
			public short _short;
			public ushort _ushort;
			public int _int;
			public uint _uint;
			public long _long;
			public ulong _ulong;
			public float _float;
			public double _double;
			public decimal _decimal;
			public EN _enum;
			public string _string;
			public byte[] _byteArray;
			public string[] _stringArray;
		}
#pragma warning restore CS0649
		enum EN
		{
			None = 0
		}
	}
}
