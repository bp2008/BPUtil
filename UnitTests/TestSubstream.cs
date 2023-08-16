using System;
using System.IO;
using System.Threading;
using BPUtil;
using BPUtil.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestSubstream
	{
		[TestMethod]
		public void SubstreamReadOnLeft()
		{
			byte[] input = new byte[256];
			for (int i = 0; i < input.Length; i++)
				input[i] = (byte)i;

			using (MemoryStream ms = new MemoryStream(input))
			{
				Substream ss = ms.Substream(10);
				byte[] buf = ByteUtil.ReadToEnd(ss);
				Assert.AreEqual(10, buf.Length);
				for (int i = 0; i < buf.Length; i++)
					Assert.AreEqual(input[i], buf[i]);
			}
		}
		[TestMethod]
		public void SubstreamReadOnRight()
		{
			byte[] input = new byte[256];
			for (int i = 0; i < input.Length; i++)
				input[i] = (byte)i;

			using (MemoryStream ms = new MemoryStream(input))
			{
				ms.Seek(-15, SeekOrigin.End);
				Substream ss = ms.Substream(15);
				byte[] buf = ByteUtil.ReadToEnd(ss);
				Assert.AreEqual(15, buf.Length);
				for (int i = 0; i < buf.Length; i++)
					Assert.AreEqual(241 + input[i], buf[i]);
			}
		}
		[TestMethod]
		public void SubstreamReadOnMiddle()
		{
			byte[] input = new byte[256];
			for (int i = 0; i < input.Length; i++)
				input[i] = (byte)i;

			using (MemoryStream ms = new MemoryStream(input))
			{
				ms.Seek(50, SeekOrigin.Begin);
				Substream ss = ms.Substream(20);
				byte[] buf = ByteUtil.ReadToEnd(ss);
				Assert.AreEqual(20, buf.Length);
				for (int i = 0; i < buf.Length; i++)
					Assert.AreEqual(input[50 + i], buf[i]);
			}
		}
		[TestMethod]
		public void SubstreamReadSeeking()
		{
			byte[] input = new byte[256];
			for (int i = 0; i < input.Length; i++)
				input[i] = (byte)i;

			using (MemoryStream ms = new MemoryStream(input))
			{
				ms.Seek(50, SeekOrigin.Begin);
				Substream ss = ms.Substream(20);

				ss.Seek(5, SeekOrigin.Begin);
				byte[] buf = ByteUtil.ReadToEnd(ss);
				Assert.AreEqual(15, buf.Length);
				for (int i = 0; i < buf.Length; i++)
					Assert.AreEqual(input[55 + i], buf[i]);

				ss.Seek(5, SeekOrigin.Begin);
				buf = ByteUtil.ReadToEnd(ss);
				Assert.AreEqual(15, buf.Length);
				for (int i = 0; i < buf.Length; i++)
					Assert.AreEqual(input[55 + i], buf[i]);

				ss.Seek(-10, SeekOrigin.End);
				buf = ByteUtil.ReadToEnd(ss.Substream(5));
				Assert.AreEqual(5, buf.Length);
				for (int i = 0; i < buf.Length; i++)
					Assert.AreEqual(input[60 + i], buf[i]);

				buf = ByteUtil.ReadToEnd(ss.Substream(4));
				Assert.AreEqual(4, buf.Length);
				for (int i = 0; i < buf.Length; i++)
					Assert.AreEqual(input[65 + i], buf[i]);

				buf = ByteUtil.ReadToEnd(ss.Substream(1));
				Assert.AreEqual(1, buf.Length);
				for (int i = 0; i < buf.Length; i++)
					Assert.AreEqual(input[69 + i], buf[i]);
			}
		}
		[TestMethod]
		public void SubstreamReadEdgeCases()
		{
			byte[] input = new byte[256];
			for (int i = 0; i < input.Length; i++)
				input[i] = (byte)i;

			using (MemoryStream ms = new MemoryStream(input))
			{
				try
				{
					ms.Substream(-1);
					Assert.Fail("Expected exception.");
				}
				catch
				{
				}
				Assert.AreEqual(0, ByteUtil.ReadToEnd(ms.Substream(0)).Length);
				Assert.AreEqual(0, ms.Position);
				Assert.AreEqual(256, ByteUtil.ReadToEnd(ms.Substream(256)).Length);
				Assert.AreEqual(256, ms.Position);
				ms.Seek(0, SeekOrigin.Begin);
				Assert.AreEqual(0, ms.Position);

				Substream ssTooLong = ms.Substream(257); // This Substream is one byte too long.
				byte[] buf = ByteUtil.ReadNBytes(ssTooLong, 256);
				Assert.AreEqual(256, buf.Length);
				Assert.AreEqual(256, ssTooLong.Position);
				Assert.AreEqual(257, ssTooLong.Length);
				Assert.AreEqual(256, ms.Position);
				for (int i = 0; i < buf.Length; i++)
					Assert.AreEqual(input[i], buf[i]);
				buf = new byte[1];
				int read = ssTooLong.Read(buf, 0, 1);
				Assert.AreEqual(0, read, "Expected end of stream to be one byte early.");
				Assert.AreEqual(256, ms.Position);
				try
				{
					buf = ByteUtil.ReadNBytes(ssTooLong, 1);
					Assert.Fail("Expected exception from ReadNBytes method.");
				}
				catch
				{
				}
				Assert.AreEqual(256, ms.Position);
			}
		}
	}
}
