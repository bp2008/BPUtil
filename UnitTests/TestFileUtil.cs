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
	public class TestFileUtil
	{
		[TestMethod]
		public void TestRelativePath()
		{
			TestRelativePath(@"C:/Folder", @"C:/Folder/File.txt", @"File.txt");
			TestRelativePath(@"C:/Folder/", @"C:/Folder/File.txt", @"File.txt");
			TestRelativePath(@"C:\Folder", @"C:/Folder/File.txt", @"File.txt");
			TestRelativePath(@"C:\Folder\", @"C:/Folder/File.txt", @"File.txt");
			TestRelativePath(@"C:\Folder/", @"C:/Folder/File.txt", @"File.txt");

			TestRelativePath(@"C:/Folder", @"C:\Folder\Subfolder\File.txt", @"Subfolder/File.txt");
			TestRelativePath(@"C:/Folder/", @"C:\Folder\Subfolder\File.txt", @"Subfolder/File.txt");

			Expect.Exception(() => { TestRelativePath(@"C:/Folder", @"C:\File.txt", @""); });
			Expect.Exception(() => { TestRelativePath(@"C:/Folder/", @"C:\File.txt", @""); });

			Expect.Exception(() => { TestRelativePath(@"C:/Folder", @"File.txt", @""); });
			Expect.Exception(() => { TestRelativePath(@"C:/Folder/", @"File.txt", @""); });

			Expect.Exception(() => { TestRelativePath(@"C:/Folder", @"C:/Folder/../File.Text", @""); });
			Expect.Exception(() => { TestRelativePath(@"C:/Folder/", @"C:/Folder/Subfolder/../File.Text", @""); });

			Expect.Exception(() => { TestRelativePath(@"C:/Folder", @"C:/FolderSubfolder/File.Text", @""); });
			Expect.Exception(() => { TestRelativePath(@"C:/Folder/", @"C:/FolderSubfolder/File.Text", @""); });
		}

		private void TestRelativePath(string rootPath, string targetPath, string expected)
		{
			Assert.AreEqual(expected, FileUtil.RelativePath(rootPath, targetPath));
		}
		[TestMethod]
		public void TestGetNonEscapingAbsolutePath()
		{
			// Test valid inputs
			Assert.AreEqual("C:/Folder/File.txt", FileUtil.GetNonEscapingAbsolutePath(@"C:/Folder", @"File.txt"));
			Assert.AreEqual("C:/Folder/File.txt", FileUtil.GetNonEscapingAbsolutePath(@"C:\Folder", @"File.txt"));
			Assert.AreEqual("C:/Folder/File.txt", FileUtil.GetNonEscapingAbsolutePath(@"C:/Folder/", @"File.txt"));
			Assert.AreEqual("C:/Folder/File.txt", FileUtil.GetNonEscapingAbsolutePath(@"C:\Folder/", @"File.txt"));
			Assert.AreEqual("C:/Folder/Subfolder", FileUtil.GetNonEscapingAbsolutePath(@"C:/Folder/", @"Subfolder"));
			Assert.AreEqual("C:/Folder/Subfolder/", FileUtil.GetNonEscapingAbsolutePath(@"C:/Folder/", @"Subfolder/"));
			Assert.AreEqual("C:/Folder/Subfolder/File.txt", FileUtil.GetNonEscapingAbsolutePath(@"C:/Folder/", @"Subfolder/File.txt"));
			Assert.AreEqual("C:/Folder/Subfolder/File.txt", FileUtil.GetNonEscapingAbsolutePath(@"C:\Folder/", @"Subfolder\File.txt"));

			// Test invalid inputs
			Assert.IsNull(FileUtil.GetNonEscapingAbsolutePath(null, "File.txt"));
			Assert.IsNull(FileUtil.GetNonEscapingAbsolutePath("C:/Folder", null));
			Assert.IsNull(FileUtil.GetNonEscapingAbsolutePath(null, null));

			// Test inputs that attempt to escape the root directory
			Assert.IsNull(FileUtil.GetNonEscapingAbsolutePath("C:/Folder", "../File.txt"));
			Assert.IsNull(FileUtil.GetNonEscapingAbsolutePath("C:/Folder", "C:/File.txt"));
			Assert.IsNull(FileUtil.GetNonEscapingAbsolutePath("C:/Folder", "D:/File.txt"));

			// "/Subfolder" with leading slash indicates an attempt to navigate from the root of the file system.
			Assert.IsNull(FileUtil.GetNonEscapingAbsolutePath("C:/Folder/", "/Subfolder"));
			Assert.IsNull(FileUtil.GetNonEscapingAbsolutePath("C:/Folder", "/Subfolder"));
			Assert.IsNull(FileUtil.GetNonEscapingAbsolutePath("C:/Folder/", "\\Subfolder"));
			Assert.IsNull(FileUtil.GetNonEscapingAbsolutePath("C:/Folder", "\\Subfolder"));
			// ... so it should be valid if the root directory is the root of the file system.
			Assert.AreEqual("C:/Subfolder", FileUtil.GetNonEscapingAbsolutePath(@"C:/", @"/Subfolder"));
		}
	}
}