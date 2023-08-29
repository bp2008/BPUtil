using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using BPUtil;
using BPUtil.SimpleHttp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestChunkedTransferEncodingStreams
	{
		private MemoryStream GetOneTwoThreeTestChunkedStream()
		{
			MemoryStream ms = new MemoryStream();

			ms.Write(ByteUtil.Utf8NoBOM.GetBytes("3\r\n"), 0, 3); // Chunk header
			ms.Write(ByteUtil.Utf8NoBOM.GetBytes("One"), 0, 3); // Chunk payload
			ms.Write(ByteUtil.Utf8NoBOM.GetBytes("\r\n"), 0, 2); // Chunk trailer

			ms.Write(ByteUtil.Utf8NoBOM.GetBytes("3\r\n"), 0, 3);
			ms.Write(ByteUtil.Utf8NoBOM.GetBytes("Two"), 0, 3);
			ms.Write(ByteUtil.Utf8NoBOM.GetBytes("\r\n"), 0, 2);

			ms.Write(ByteUtil.Utf8NoBOM.GetBytes("5\r\n"), 0, 3);
			ms.Write(ByteUtil.Utf8NoBOM.GetBytes("Three"), 0, 5);
			ms.Write(ByteUtil.Utf8NoBOM.GetBytes("\r\n"), 0, 2);

			ms.Write(ByteUtil.Utf8NoBOM.GetBytes("0\r\n"), 0, 3); // Final chunk header
			ms.Write(ByteUtil.Utf8NoBOM.GetBytes("\r\n"), 0, 2); // Final chunk trailer

			return ms;
		}
		[TestMethod]
		public void WritableBasic()
		{
			byte[] expected = GetOneTwoThreeTestChunkedStream().ToArray();

			MemoryStream ms = new MemoryStream();
			using (WritableChunkedTransferEncodingStream chunkWriter = new WritableChunkedTransferEncodingStream(ms))
			{
				foreach (string str in new string[] { "One", "Two", "Three" })
				{
					byte[] bytes = ByteUtil.Utf8NoBOM.GetBytes(str);
					chunkWriter.Write(bytes, 0, bytes.Length);
				}
			}
			byte[] actual = ms.ToArray();
			CollectionAssert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void ReadableBasic()
		{
			byte[] expected = ByteUtil.Utf8NoBOM.GetBytes("OneTwoThree");

			MemoryStream ms = GetOneTwoThreeTestChunkedStream();

			ms.Seek(0, SeekOrigin.Begin);
			ReadableChunkedTransferEncodingStream chunkReader = new ReadableChunkedTransferEncodingStream(ms);
			byte[] actual = ByteUtil.ReadToEnd(chunkReader);
			CollectionAssert.AreEqual(expected, actual);

			ms.Seek(0, SeekOrigin.Begin);
			chunkReader = new ReadableChunkedTransferEncodingStream(ms);
			actual = ByteUtil.ReadToEndAsync(chunkReader).GetAwaiter().GetResult();
			CollectionAssert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void ReadWriteChain()
		{
			string loremIpsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";
			byte[] expected = ByteUtil.Utf8NoBOM.GetBytes(loremIpsum);

			// Write each word one at a time so they are their own chunks.
			MemoryStream ms = new MemoryStream();
			using (WritableChunkedTransferEncodingStream chunkWriter = new WritableChunkedTransferEncodingStream(ms))
			{
				string[] words = loremIpsum.Split(' ');
				for (int i = 0; i < words.Length; i++)
				{
					string word = words[i];
					byte[] buf = ByteUtil.Utf8NoBOM.GetBytes(word + (i == words.Length - 1 ? "" : " "));
					chunkWriter.Write(buf, 0, buf.Length);
				}
			}
			ms.Seek(0, SeekOrigin.Begin);
			ReadableChunkedTransferEncodingStream chunkReader = new ReadableChunkedTransferEncodingStream(ms);
			byte[] actual = ByteUtil.ReadToEnd(chunkReader);

			CollectionAssert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void ReadWriteChainAsync()
		{
			string loremIpsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";
			byte[] expected = ByteUtil.Utf8NoBOM.GetBytes(loremIpsum);

			// Write each word one at a time so they are their own chunks.
			MemoryStream ms = new MemoryStream();
			using (WritableChunkedTransferEncodingStream chunkWriter = new WritableChunkedTransferEncodingStream(ms))
			{
				string[] words = loremIpsum.Split(' ');
				for (int i = 0; i < words.Length; i++)
				{
					string word = words[i];
					byte[] buf = ByteUtil.Utf8NoBOM.GetBytes(word + (i == words.Length - 1 ? "" : " "));
					chunkWriter.WriteAsync(buf, 0, buf.Length).Wait();
				}
			}
			ms.Seek(0, SeekOrigin.Begin);
			ReadableChunkedTransferEncodingStream chunkReader = new ReadableChunkedTransferEncodingStream(ms);
			byte[] actual = ByteUtil.ReadToEndAsync(chunkReader).GetAwaiter().GetResult();

			CollectionAssert.AreEqual(expected, actual);
		}
	}
}
