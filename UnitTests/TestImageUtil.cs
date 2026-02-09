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
			byte[] imageData = GenerageTestImage(ImageFormat.Bmp);
			VerifyImageFormatAndDimensions(imageData, "image/bmp", ".bmp");
		}

		[TestMethod]
		public void TestGif()
		{
			byte[] imageData = GenerageTestImage(ImageFormat.Gif);
			VerifyImageFormatAndDimensions(imageData, "image/gif", ".gif");
		}

		[TestMethod]
		public void TestPng()
		{
			byte[] imageData = GenerageTestImage(ImageFormat.Png);
			VerifyImageFormatAndDimensions(imageData, "image/png", ".png");
		}

		[TestMethod]
		public void TestJpeg()
		{
			byte[] imageData = GenerageTestImage(ImageFormat.Jpeg);
			VerifyImageFormatAndDimensions(imageData, "image/jpeg", ".jpg");
		}
		[TestMethod]
		public void TestWebPLossy()
		{
			VerifyImageFormatAndDimensions(Properties.Resources.LossyWebP, "image/webp", ".webp");
		}
		[TestMethod]
		public void TestWebPLossless()
		{
			VerifyImageFormatAndDimensions(Properties.Resources.LosslessWebP, "image/webp", ".webp");
		}
		private static byte[] GenerageTestImage(ImageFormat saveFormat)
		{
			using (Bitmap bmp = new Bitmap(W, H))
			{
				using (Graphics g = Graphics.FromImage(bmp))
				{
					g.Clear(Color.Fuchsia);
				}
				using (MemoryStream ms = new MemoryStream())
				{
					bmp.Save(ms, saveFormat);
					return ms.ToArray();
				}
			}
		}
		private static void VerifyImageFormatAndDimensions(byte[] imageData, string expectedMime, string expectedExt)
		{
			// 1) Test GetFormat(byte[]) and GetFormatOrNull(byte[])
			ImageFormatMetadata m1 = ImageUtil.GetFormat(imageData);
			Assert.IsNotNull(m1);
			Assert.AreEqual(expectedMime, m1.MimeType);
			Assert.AreEqual(expectedExt, m1.FileExtension);
			Assert.IsNotNull(ImageUtil.GetFormatOrNull(imageData));

			// 2) Test IDataStream overloads
			using (MemoryStream msStream = new MemoryStream(imageData))
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
			Size dimsFromBytes = ImageUtil.GetDimensions(imageData);
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
				File.WriteAllBytes(path, imageData);

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