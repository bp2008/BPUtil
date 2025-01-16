using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestCompression
	{

		[TestMethod]
		public void TestCompressionToAndFromByteArrays()
		{
			byte[] highlyCompressibleData = GetHighlyCompressibleData();

			{
				byte[] compressed = Compression.GZipCompress(highlyCompressibleData);
				Assert.IsTrue(compressed.Length < highlyCompressibleData.Length);
				byte[] decompressed = Compression.GZipDecompress(compressed);
				Assert.IsTrue(ByteUtil.ByteArraysMatch(highlyCompressibleData, decompressed));
			}
			{
				byte[] compressed = Compression.DeflateCompress(highlyCompressibleData);
				Assert.IsTrue(compressed.Length < highlyCompressibleData.Length);
				byte[] decompressed = Compression.DeflateDecompress(compressed);
				Assert.IsTrue(ByteUtil.ByteArraysMatch(highlyCompressibleData, decompressed));
			}
			{
				byte[] compressed = Compression.Compress(BPCompressionMethod.Gzip, highlyCompressibleData);
				Assert.IsTrue(compressed.Length < highlyCompressibleData.Length);
				byte[] decompressed = Compression.Decompress(BPCompressionMethod.Gzip, compressed);
				Assert.IsTrue(ByteUtil.ByteArraysMatch(highlyCompressibleData, decompressed));
			}
			{
				byte[] compressed = Compression.Compress(BPCompressionMethod.Deflate, highlyCompressibleData);
				Assert.IsTrue(compressed.Length < highlyCompressibleData.Length);
				byte[] decompressed = Compression.Decompress(BPCompressionMethod.Deflate, compressed);
				Assert.IsTrue(ByteUtil.ByteArraysMatch(highlyCompressibleData, decompressed));
			}
		}
		private static byte[] GetHighlyCompressibleData()
		{
			using (MemoryStream msHighlyCompressibleData = new MemoryStream())
			{
				for (int i = 0; i <= 255; i++)
				{
					for (int b = 0; b <= 255; b++)
					{
						msHighlyCompressibleData.WriteByte((byte)i);
					}
				}
				return msHighlyCompressibleData.ToArray();
			}
		}
	}
}
