using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
	[TestClass]
	public class TestFileInfoExtensions
	{
		[TestMethod]
		public void TestNameWithoutExtension()
		{
			// Test with relative path inputs
			Assert.AreEqual("config", new FileInfo("config").NameWithoutExtension());
			Assert.AreEqual(".gitignore", new FileInfo(".gitignore").NameWithoutExtension());
			Assert.AreEqual("My", new FileInfo("My.exe").NameWithoutExtension());
			Assert.AreEqual("My.exe", new FileInfo("My.exe.config").NameWithoutExtension());

			// Test with absolute path inputs
			Assert.AreEqual("config", new FileInfo("C:\\config").NameWithoutExtension());
			Assert.AreEqual(".gitignore", new FileInfo("C:\\.gitignore").NameWithoutExtension());
			Assert.AreEqual("My", new FileInfo("C:\\My.exe").NameWithoutExtension());
			Assert.AreEqual("My.exe", new FileInfo("C:\\My.exe.config").NameWithoutExtension());
		}
		[TestMethod]
		public void TestFullNameWithoutExtension()
		{
			Assert.AreEqual("C:\\config", new FileInfo("C:\\config").FullNameWithoutExtension());
			Assert.AreEqual("C:\\.gitignore", new FileInfo("C:\\.gitignore").FullNameWithoutExtension());
			Assert.AreEqual("C:\\My", new FileInfo("C:\\My.exe").FullNameWithoutExtension());
			Assert.AreEqual("C:\\My.exe", new FileInfo("C:\\My.exe.config").FullNameWithoutExtension());
		}
	}
}
