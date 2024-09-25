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
	}
}
