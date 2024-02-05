using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestObjectFieldMap
	{
		[TestMethod]
		public void TestObjectFieldMap_ArgumentExceptions()
		{
			try
			{
				new ObjectFieldMap(null);
				Assert.Fail("Expected exception from passing in null");
			}
			catch { }
			try
			{
				new ObjectFieldMap("str");
				Assert.Fail("Expected exception from passing in string");
			}
			catch { }
			try
			{
				new ObjectFieldMap(1);
				Assert.Fail("Expected exception from passing in int");
			}
			catch { }
		}
		[TestMethod]
		public void TestObjectFieldMap_List()
		{
			List<int> list = new List<int>();
			list.Add(2);
			list.Add(4);
			list.Add(8);
			ObjectFieldMap map = new ObjectFieldMap(list);
			Assert.AreEqual("[0] = 2" + Environment.NewLine
				+ "[1] = 4" + Environment.NewLine
				+ "[2] = 8", map.ToString());
		}
		[TestMethod]
		public void TestObjectFieldMap_SimpleObject()
		{
			SO obj = new SO(1, "s");
			ObjectFieldMap map = new ObjectFieldMap(obj);
			Assert.AreEqual("A = 1" + Environment.NewLine + "B = \"s\"", map.ToString());
		}
		[TestMethod]
		public void TestObjectFieldMap_SimpleObject2()
		{
			SO2 obj = new SO2(' ');
			ObjectFieldMap map = new ObjectFieldMap(obj);
			Assert.AreEqual("c = ' '", map.ToString());

			obj = new SO2((char)0);
			map = new ObjectFieldMap(obj);
			Assert.AreEqual("c = '\0'", map.ToString());
		}
		[TestMethod]
		public void TestObjectFieldMap_ComplexObject1()
		{
			CO obj = new CO(new SO(1, "s"), null, "&");
			ObjectFieldMap map = new ObjectFieldMap(obj);
			Assert.AreEqual("X.A = 1" + Environment.NewLine
				+ "X.B = \"s\"" + Environment.NewLine
				+ "Y = null" + Environment.NewLine
				+ "Z = \"&\"", map.ToString());
		}
		[TestMethod]
		public void TestObjectFieldMap_ComplexObject2()
		{
			CO obj = new CO(new SO(1, "s"), null, null);
			ObjectFieldMap map = new ObjectFieldMap(obj);
			Assert.AreEqual("X.A = 1" + Environment.NewLine
				+ "X.B = \"s\"" + Environment.NewLine
				+ "Y = null" + Environment.NewLine
				+ "Z = null", map.ToString());
		}
		[TestMethod]
		public void TestObjectFieldMap_ComplexObject3()
		{
			CO obj = new CO(new SO(1, "s"), new SO(2, null), "m");
			ObjectFieldMap map = new ObjectFieldMap(obj);
			Assert.AreEqual("X.A = 1" + Environment.NewLine
				+ "X.B = \"s\"" + Environment.NewLine
				+ "Y.A = 2" + Environment.NewLine
				+ "Y.B = null" + Environment.NewLine
				+ "Z = \"m\"", map.ToString());
		}
		[TestMethod]
		public void TestObjectFieldMap_ComplexObject4()
		{
			CO4 obj = new CO4(new SO(-1, ""), new double[] { 0.1, -0.9, double.MaxValue }, new string[] { "First", "", null });
			ObjectFieldMap map = new ObjectFieldMap(obj);
			Assert.AreEqual("X.A = -1" + Environment.NewLine
				+ "X.B = \"\"" + Environment.NewLine
				+ "Y[0] = 0.1" + Environment.NewLine
				+ "Y[1] = -0.9" + Environment.NewLine
				+ "Y[2] = 1.79769313486232E+308" + Environment.NewLine
				+ "Z[0] = \"First\"" + Environment.NewLine
				+ "Z[1] = \"\"" + Environment.NewLine
				+ "Z[2] = null", map.ToString());
		}
		[TestMethod]
		public void TestLoop_LoopObject()
		{

			COLoopA loopedA = new COLoopA();
			COLoopB loopedB = new COLoopB();
			loopedA.child = loopedB;
			loopedB.child = loopedA;

			// Test that ObjectFieldMap does not process loops.  If it does, this method will either infinite loop or will stack overflow.

			ObjectFieldMap map = new ObjectFieldMap(loopedA);
			Assert.AreEqual("name = \"A\"" + Environment.NewLine
				+ "child.name = \"B\"", map.ToString());

			map = new ObjectFieldMap(loopedB);
			Assert.AreEqual("name = \"B\"" + Environment.NewLine
				+ "child.name = \"A\"", map.ToString());
		}
		class SO
		{
			public int A { get; set; }
			public string B;
			public SO(int a, string b) { A = a; B = b; }
		}
		class SO2
		{
			public char c;
			public SO2(char c) { this.c = c; }
		}
		class CO
		{
			public SO X { get; set; }
			public SO Y;
			public string Z;
			public CO(SO x, SO y, string z) { X = x; Y = y; Z = z; }
		}
		class CO4
		{
			public SO X { get; set; }
			public double[] Y;
			public string[] Z;
			public CO4(SO x, double[] y, string[] z) { X = x; Y = y; Z = z; }
		}
		class COLoopA
		{
			public string name = "A";
			public COLoopB child;
		}
		class COLoopB
		{
			public string name = "B";
			public COLoopA child;
		}
	}
}
