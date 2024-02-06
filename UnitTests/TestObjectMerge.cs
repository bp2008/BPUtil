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
	public class TestObjectMerge
	{
		class CO
		{
			public SO X { get; set; }
			public SO Y;
			public int Z;
			public CO(SO x, SO y, int z) { X = x; Y = y; Z = z; }
		}
		class SO
		{
			public int A { get; set; }
			public int B;
			public SO(int a, int b) { A = a; B = b; }
		}
		[TestMethod]
		public void TestObjectMerge_1()
		{
		}
	}
}
