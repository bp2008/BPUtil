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
	public class TestStringExtensions
	{
		[TestMethod]
		public void TestIEquals()
		{
			string str1 = "ABCD01";
			string str2 = "AbCd01";
			string str3 = "abcd01";
			string str4 = "BCDA01";

			Assert.IsTrue(str1.IEquals(str1));
			Assert.IsTrue(str1.IEquals(str2));
			Assert.IsTrue(str1.IEquals(str3));
			Assert.IsFalse(str1.IEquals(str4));

			Assert.IsTrue(str2.IEquals(str1));
			Assert.IsTrue(str2.IEquals(str2));
			Assert.IsTrue(str2.IEquals(str3));
			Assert.IsFalse(str2.IEquals(str4));

			Assert.IsTrue(str3.IEquals(str1));
			Assert.IsTrue(str3.IEquals(str2));
			Assert.IsTrue(str3.IEquals(str3));
			Assert.IsFalse(str3.IEquals(str4));

			Assert.IsFalse(str4.IEquals(str1));
			Assert.IsFalse(str4.IEquals(str2));
			Assert.IsFalse(str4.IEquals(str3));
			Assert.IsTrue(str4.IEquals(str4));

			Assert.IsFalse(str1.IEquals(null));
			Assert.IsFalse(str2.IEquals(null));
			Assert.IsFalse(str3.IEquals(null));
			Assert.IsFalse(str4.IEquals(null));
		}
	}
}
