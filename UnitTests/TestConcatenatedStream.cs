using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using BPUtil;
using BPUtil.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestConcatenatedStream
	{
		[TestMethod]
		public void ConcatenatedStreamCallback()
		{
			byte[] input = new byte[250];
			for (int i = 0; i < input.Length; i++)
				input[i] = (byte)i;

			ConcatenatedStream cs = new ConcatenatedStream(idx =>
			{
				if (idx < 5)
					return new MemoryStream(ByteUtil.SubArray(input, idx * 50, 50));
				else
					return null;
			});

			byte[] output = ByteUtil.ReadToEnd(cs);

			CollectionAssert.AreEqual(input, output);
		}

		[TestMethod]
		public void ConcatenatedStreamQueue()
		{
			byte[] input = new byte[250];
			for (int i = 0; i < input.Length; i++)
				input[i] = (byte)i;

			List<MemoryStream> subs = new List<MemoryStream>();
			for (int i = 0; i < 5; i++)
				subs.Add(new MemoryStream(ByteUtil.SubArray(input, i * 50, 50)));

			ConcatenatedStream cs = new ConcatenatedStream(subs);

			byte[] output = ByteUtil.ReadToEnd(cs);

			CollectionAssert.AreEqual(input, output);
		}

		[TestMethod]
		public void ConcatenatedSubstreams()
		{
			byte[] input = new byte[250];
			for (int i = 0; i < input.Length; i++)
				input[i] = (byte)i;
			List<Substream> subs = new List<Substream>();
			for (int i = 0; i < 5; i++)
			{
				MemoryStream msInput = new MemoryStream(input);
				msInput.Position = i * 50;
				subs.Add(msInput.Substream(50));
			}

			ConcatenatedStream cs = new ConcatenatedStream(subs);

			byte[] output = ByteUtil.ReadToEnd(cs);

			CollectionAssert.AreEqual(input, output);
		}
	}
}
