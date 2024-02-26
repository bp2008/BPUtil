using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	/// <summary>
	/// Tests for the "Copy" extension method on objects which creates a deep copy.
	/// </summary>
	[TestClass]
	public class TestObjectCopy
	{
		class CO
		{
			public SO X { get; set; }
			public LO Y;
			public int Z;
			public CO(SO x, LO y, int z) { X = x; Y = y; Z = z; }
		}
		class SO
		{
			public int A { get; set; }
			public int B;
			public SO(int a, int b) { A = a; B = b; }
		}
		class LO
		{
			public int[] A { get; set; }
			public List<string> B;
			public Dictionary<string, int> C;
			public LO(int[] a, List<string> b, Dictionary<string, int> c) { A = a; B = b; C = c; }
		}
		[TestMethod]
		public void TestObjectCopy_1()
		{
			SO so = new SO(50, 65);

			Dictionary<string, int> dic = new Dictionary<string, int>();
			dic["test"] = 4;
			dic["my"] = 2;
			dic["object"] = 6;

			LO lo = new LO(new int[] { 1, 2, 3 }, new List<string>(new string[] { "A", "B" }), dic);

			CO co = new CO(so, lo, 255);

			CO copy = co.Copy();

			TestOriginalContent(co);
			TestOriginalContent(copy);

			// Make modifications to "co"'s data at every level.
			co.X.A = 99;
			co.X.B = 99;
			co.Y.A[1] = 99;
			co.Y.B[0] = "Different";
			co.Y.C.Remove("test");
			co.Y.C["object"] = 5;
			co.Z = 99;

			// "copy" should be unchanged because it should contain no shared references to mutable objects.
			TestOriginalContent(copy);
		}
		private void TestOriginalContent(CO co)
		{
			Assert.AreEqual(50, co.X.A);
			Assert.AreEqual(65, co.X.B);

			Assert.AreEqual(3, co.Y.A.Length);
			Assert.AreEqual(1, co.Y.A[0]);
			Assert.AreEqual(2, co.Y.A[1]);
			Assert.AreEqual(3, co.Y.A[2]);

			Assert.AreEqual(2, co.Y.B.Count);
			Assert.AreEqual("A", co.Y.B[0]);
			Assert.AreEqual("B", co.Y.B[1]);

			Assert.AreEqual(3, co.Y.C.Count);
			Assert.AreEqual(4, co.Y.C["test"]);
			Assert.AreEqual(2, co.Y.C["my"]);
			Assert.AreEqual(6, co.Y.C["object"]);

			Assert.AreEqual(255, co.Z);
		}
		class TO
		{
			public Thread thr;
			//public event EventHandler OnStop = delegate { };
			public TO() { }
			public TO(Action action)
			{
				thr = new Thread(() => { action(); });
				thr.Name = "TestObjectCopy_2";
				thr.IsBackground = true;
				thr.Start();
			}
		}
		//[TestMethod]
		//public void TestObjectCopy_2()
		//{
		//	bool stop = false;
		//	long stopCount = 0;
		//	TO to = new TO(() =>
		//	{
		//		Thread.Sleep(5000);
		//		stop = true;
		//		Interlocked.Increment(ref stopCount);
		//	});

		//	TO copy = to.Copy();
		//	while (!stop)
		//	{
		//		copy = to.Copy();
		//		byte[] buf = new byte[1024 * 64];
		//		StaticRandom.NextBytes(buf);
		//	}

		//	Thread.Sleep(500);
		//	Assert.AreEqual(1, stopCount);
			
		//}
	}
}
