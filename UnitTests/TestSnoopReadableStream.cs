using System;
using System.IO;
using System.Threading;
using BPUtil;
using BPUtil.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestSnoopReadableStream
	{
		[TestMethod]
		public void SnoopReadableStream_Readable()
		{
			byte[] input = new byte[256];
			for (int i = 0; i < input.Length; i++)
				input[i] = (byte)i;

			using (MemoryStream ms = new MemoryStream(input))
			using (SnoopReadableStream snoop = new SnoopReadableStream(ms))
			{
				// snoop.Data should be empty
				Assert.AreEqual(0, snoop.Data.Length);

				Assert.IsTrue(snoop.CanRead);

				byte[] buf = new byte[512];
				int bytesRead = snoop.Read(buf, 0, buf.Length);
				Assert.AreEqual(input.Length, bytesRead, "Read did not return the expected number of bytes.");
				Assert.IsTrue(ByteUtil.StartsWith(buf, input));

				// Snoop.Data should match input.
				byte[] snooped = snoop.Data;
				Assert.AreEqual(bytesRead, snooped.Length, "SnoopReadableStream did not cache the expected number of bytes.");
				Assert.IsTrue(ByteUtil.ByteArraysMatch(snooped, input));
			}
		}
		[TestMethod]
		public void SnoopReadableStream_ReadableString()
		{
			byte[] input = ByteUtil.Utf8NoBOM.GetBytes("Test");
			using (MemoryStream ms = new MemoryStream(input))
			using (SnoopReadableStream snoop = new SnoopReadableStream(ms))
			{
				// snoop.Data should be empty
				Assert.AreEqual(0, snoop.Data.Length);

				Assert.IsTrue(snoop.CanRead);

				byte[] buf = new byte[512];
				int bytesRead = snoop.Read(buf, 0, buf.Length);
				Assert.AreEqual(4, bytesRead, "Read did not return the expected number of bytes.");
				Assert.IsTrue(ByteUtil.StartsWith(buf, input));

				// Snoop.Data should match input.
				byte[] snooped = snoop.Data;
				Assert.AreEqual(bytesRead, snooped.Length, "SnoopReadableStream did not cache the expected number of bytes.");
				Assert.IsTrue(ByteUtil.ByteArraysMatch(snooped, input));

				Assert.AreEqual("Test", snoop.DataAsUtf8, "SnoopReadableStream DataAsUtf8 did not have expected value.");
			}
		}
		[TestMethod]
		public void SnoopReadableStream_Writable()
		{
			byte[] input = new byte[256];
			for (int i = 0; i < input.Length; i++)
				input[i] = (byte)i;

			using (MemoryStream ms = new MemoryStream())
			using (SnoopReadableStream snoop = new SnoopReadableStream(ms))
			{
				Assert.IsTrue(snoop.CanWrite);

				// snoop.Data should be empty
				Assert.AreEqual(0, snoop.Data.Length);

				snoop.Write(input, 0, input.Length);

				byte[] buf = ms.ToArray();
				Assert.AreEqual(input.Length, buf.Length, "Failed to write expected number of bytes.");
				Assert.IsTrue(ByteUtil.ByteArraysMatch(buf, input));

				// SnoopReadableStream does not snoop on data written to it, only read from it.
				Assert.AreEqual(0, snoop.Data.Length);
			}
		}
		class DiposeTestStream : Stream, IDisposable
		{
			public int disposed = 0;

			/// <summary>
			/// Closes the underlying stream if this UnreadableStream was configured to do so during construction.
			/// </summary>
			protected override void Dispose(bool disposing)
			{
				disposed++;
			}

			#region Ignored Boilerplate
			public override bool CanRead => throw new NotImplementedException();

			public override bool CanSeek => throw new NotImplementedException();

			public override bool CanWrite => throw new NotImplementedException();

			public override long Length => throw new NotImplementedException();

			public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

			public override void Flush()
			{
				throw new NotImplementedException();
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				throw new NotImplementedException();
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				throw new NotImplementedException();
			}

			public override void SetLength(long value)
			{
				throw new NotImplementedException();
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				throw new NotImplementedException();
			}
			#endregion
		}
		[TestMethod]
		public void SnoopReadableStream_InnerDisposalIsAutomaticOnDispose()
		{
			using (DiposeTestStream s = new DiposeTestStream())
			{
				using (SnoopReadableStream snoop = new SnoopReadableStream(s))
				{
					Assert.AreEqual(0, s.disposed);
				}
				Assert.AreEqual(1, s.disposed);
			}
		}
		[TestMethod]
		public void SnoopReadableStream_InnerDisposalIsAutomaticOnClose()
		{
			using (DiposeTestStream s = new DiposeTestStream())
			{
				using (SnoopReadableStream snoop = new SnoopReadableStream(s))
				{
					Assert.AreEqual(0, s.disposed);
					snoop.Close();
					Assert.AreEqual(1, s.disposed);
				}
				Assert.AreEqual(2, s.disposed);
			}
		}
		[TestMethod]
		public void SnoopReadableStream_InnerDisposalAvoidedOnDispose()
		{
			using (DiposeTestStream s = new DiposeTestStream())
			{
				using (SnoopReadableStream snoop = new SnoopReadableStream(s, true))
				{
					Assert.AreEqual(0, s.disposed);
				}
				Assert.AreEqual(0, s.disposed);
			}
		}
		[TestMethod]
		public void SnoopReadableStream_InnerDisposalAvoidedOnClose()
		{
			using (DiposeTestStream s = new DiposeTestStream())
			{
				using (SnoopReadableStream snoop = new SnoopReadableStream(s, true))
				{
					Assert.AreEqual(0, s.disposed);
					snoop.Close();
					Assert.AreEqual(0, s.disposed);
				}
				Assert.AreEqual(0, s.disposed);
			}
		}
	}
}
