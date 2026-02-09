using BPUtil;
using BPUtil.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace UnitTests
{
	[TestClass]
	public class TestImageUtil
	{
		private const int W = 123;
		private const int H = 45;

		[TestMethod]
		public void TestBmp()
		{
			VerifyImageFormatAndDimensions(ImageFormat.Bmp, "image/bmp", ".bmp");
		}

		[TestMethod]
		public void TestGif()
		{
			VerifyImageFormatAndDimensions(ImageFormat.Gif, "image/gif", ".gif");
		}

		[TestMethod]
		public void TestPng()
		{
			VerifyImageFormatAndDimensions(ImageFormat.Png, "image/png", ".png");
		}

		[TestMethod]
		public void TestJpeg()
		{
			VerifyImageFormatAndDimensions(ImageFormat.Jpeg, "image/jpeg", ".jpg");
		}

		private static void VerifyImageFormatAndDimensions(ImageFormat saveFormat, string expectedMime, string expectedExt)
		{
			// Generate an in-memory image
			byte[] data;
			using (Bitmap bmp = new Bitmap(W, H))
			{
				using (Graphics g = Graphics.FromImage(bmp))
				{
					g.Clear(Color.Fuchsia);
				}
				using (MemoryStream ms = new MemoryStream())
				{
					bmp.Save(ms, saveFormat);
					data = ms.ToArray();
				}
			}

			// 1) Test GetFormat(byte[]) and GetFormatOrNull(byte[])
			ImageFormatMetadata m1 = ImageUtil.GetFormat(data);
			Assert.IsNotNull(m1);
			Assert.AreEqual(expectedMime, m1.MimeType);
			Assert.AreEqual(expectedExt, m1.FileExtension);
			Assert.IsNotNull(ImageUtil.GetFormatOrNull(data));

			// 2) Test IDataStream overloads
			using (MemoryStream msStream = new MemoryStream(data))
			using (BasicDataStream bds = new BasicDataStream(msStream))
			{
				ImageFormatMetadata m2 = ImageUtil.GetFormat(bds);
				Assert.IsNotNull(m2);
				Assert.AreEqual(expectedMime, m2.MimeType);
				Assert.AreEqual(expectedExt, m2.FileExtension);

				// Reset stream to start for dimensions
				msStream.Seek(0, SeekOrigin.Begin);
				Size dimsFromStream = ImageUtil.GetDimensions(bds);
				Assert.AreEqual(W, dimsFromStream.Width);
				Assert.AreEqual(H, dimsFromStream.Height);
			}

			// 3) Test GetDimensions(byte[])
			Size dimsFromBytes = ImageUtil.GetDimensions(data);
			Assert.AreEqual(W, dimsFromBytes.Width);
			Assert.AreEqual(H, dimsFromBytes.Height);

			// 4) Test file-based overloads
			string tempFile = Path.GetTempFileName();
			string path = Path.ChangeExtension(tempFile, expectedExt);
			try
			{
				// Replace the temp file created with the one having proper extension
				if (File.Exists(tempFile))
					File.Delete(tempFile);
				File.WriteAllBytes(path, data);

				ImageFormatMetadata m3 = ImageUtil.GetFormat(path);
				Assert.IsNotNull(m3);
				Assert.AreEqual(expectedMime, m3.MimeType);
				Assert.AreEqual(expectedExt, m3.FileExtension);

				Size dimsFromFile = ImageUtil.GetDimensions(path);
				Assert.AreEqual(W, dimsFromFile.Width);
				Assert.AreEqual(H, dimsFromFile.Height);
			}
			finally
			{
				try
				{
					if (File.Exists(path))
						File.Delete(path);
					if (File.Exists(tempFile))
						File.Delete(tempFile);
				}
				catch { /* best-effort cleanup */ }
			}
		}
	}
}