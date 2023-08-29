﻿using BPUtil.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// Contains helpful methods for working with binary data.
	/// </summary>
	public static class ByteUtil
	{
		/// <summary>
		/// UTF-8 encoding configured to NOT emit a byte order mark.
		/// </summary>
		public static readonly UTF8Encoding Utf8NoBOM = new UTF8Encoding(false);
		/// <summary>
		/// ISO-8859-1 encoding used by HTTP requests.
		/// </summary>
		public static readonly Encoding ISO_8859_1 = Encoding.GetEncoding("ISO-8859-1");
		/// <summary>
		/// Returns true of the content of the specified byte arrays exactly match each other, or if both arrays are null.
		/// </summary>
		/// <param name="a">An array to compare.</param>
		/// <param name="b">An array to compare.</param>
		/// <returns></returns>
		public static bool ByteArraysMatch(byte[] a, byte[] b)
		{
			if (a == null && b == null)
				return true;
			else if (a == null || b == null || a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++)
				if (a[i] != b[i])
					return false;
			return true;
		}
		/// <summary>
		/// Returns true if <paramref name="thisBytes"/> starts with <paramref name="thatBytes"/>.
		/// </summary>
		/// <param name="thisBytes">Base byte array</param>
		/// <param name="thatBytes">Byte array to look for at the start of the base byte array.</param>
		/// <returns></returns>
		public static bool StartsWith(byte[] thisBytes, byte[] thatBytes)
		{
			if (thisBytes == null)
				return thatBytes == null;
			for (int i = 0; i < thatBytes.Length; i += 1)
			{
				if (i == thisBytes.Length)
					return false;
				if (thisBytes[i] != thatBytes[i])
					return false;
			}
			return true;
		}
		/// <summary>
		/// <para>Returns true if the specified byte arrays `a` and `b` are the same length and if every bit which is set in `mask` is the same between `a` and `b`.</para>
		/// <para>e.g.</para>
		/// <para>a = 01</para>
		/// <para>b = 00</para>
		/// <para>If mask = 10 or mask = 00 then true. Because the second bit is not set in mask, it does not get compared.  If mask = 01 or mask = 11 then false.</para>
		/// </summary>
		/// <param name="a">An array to compare.</param>
		/// <param name="b">An array to compare.</param>
		/// <param name="mask">An array of equal or lesser length to `a` and `b`.</param>
		/// <returns></returns>
		public static bool CompareWithMask(byte[] a, byte[] b, byte[] mask)
		{
			if (a == null && b == null)
				return true;
			else if (a == null || b == null || a.Length != b.Length || mask.Length > a.Length)
				return false;
			for (int i = 0; i < mask.Length; i++)
			{
				if ((a[i] & mask[i]) != (b[i] & mask[i]))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Returns a new array containing the values of the first array XORed with the values of the second array.
		/// </summary>
		/// <param name="a">An array.</param>
		/// <param name="b">An array.</param>
		/// <returns></returns>
		public static byte[] XORByteArrays(byte[] a, byte[] b)
		{
			if (a.Length != b.Length)
				throw new ArgumentException("Array lengths do not match! (" + a.Length + ", " + b.Length + ")");
			byte[] result = new byte[a.Length];
			for (int i = 0; i < result.Length; i++)
				result[i] = (byte)(a[i] ^ b[i]);
			return result;
		}
		/// <summary>
		/// Computes the "bitwise and" of the values in each array, and returns a new array containing the results.
		/// e.g. [0,1,1] &amp; [1,0,1] = [0,0,1]
		/// </summary>
		/// <param name="a">An array.</param>
		/// <param name="b">An array.</param>
		/// <returns></returns>
		public static byte[] BitwiseAnd(byte[] a, byte[] b)
		{
			if (a.Length != b.Length)
				throw new ArgumentException("Array lengths do not match! (" + a.Length + ", " + b.Length + ")");
			byte[] result = new byte[a.Length];
			for (int i = 0; i < result.Length; i++)
				result[i] = (byte)(a[i] & b[i]);
			return result;
		}
		/// <summary>
		/// Computes the "bitwise or" of the values in each array, and returns a new array containing the results.
		/// e.g. [0,1,1] | [1,0,1] = [1,1,1]
		/// </summary>
		/// <param name="a">An array.</param>
		/// <param name="b">An array.</param>
		/// <returns></returns>
		public static byte[] BitwiseOr(byte[] a, byte[] b)
		{
			if (a.Length != b.Length)
				throw new ArgumentException("Array lengths do not match! (" + a.Length + ", " + b.Length + ")");
			byte[] result = new byte[a.Length];
			for (int i = 0; i < result.Length; i++)
				result[i] = (byte)(a[i] | b[i]);
			return result;
		}
		/// <summary>
		/// Inverts every byte in the array. e.g. [0001] => [1110]
		/// </summary>
		/// <param name="a">An array.</param>
		/// <returns></returns>
		public static void InvertBits(byte[] a)
		{
			if (a == null)
				return;
			for (int i = 0; i < a.Length; i++)
				a[i] = (byte)~(a[i]);
		}
		/// <summary>
		/// Returns a new byte array containing the inverse of the values of the source array. e.g. [0001] => [1110]
		/// </summary>
		/// <param name="a">An array.</param>
		/// <returns></returns>
		public static byte[] GetInverse(byte[] a)
		{
			if (a == null)
				return null;
			byte[] result = new byte[a.Length];
			for (int i = 0; i < result.Length; i++)
				result[i] = (byte)~(a[i]);
			return result;
		}
		/// <summary>
		/// Generates a byte array of the specified length, filled with cryptographically strong random values.
		/// </summary>
		/// <param name="numBytes">The length of the byte array to create.</param>
		/// <returns></returns>
		public static byte[] GenerateRandomBytes(int numBytes)
		{
			byte[] buf = new byte[numBytes];
			System.Security.Cryptography.RandomNumberGenerator.Create().GetBytes(buf);
			return buf;
		}

		/// <summary>
		/// <para>Reads a line of text from the input stream.  Each character must be a printable ASCII character [32-126] or "LF" [10] or "CR" [13].</para>
		/// <para>The line is considered ended when an "LF" character is reached or the end of the stream is reached.</para>
		/// <para>Any "CR" characters encountered are not added to the output string.</para>
		/// <para>Null is returned if the line exceeds the specified [maxLength] or if any out-of-range characters are encountered.</para>
		/// </summary>
		/// <param name="inputStream"></param>
		/// <param name="maxLength">Maximum length of the line.</param>
		/// <returns></returns>
		public static string ReadPrintableASCIILine(Stream inputStream, int maxLength = 32768)
		{
			int next_char;
			List<byte> data = new List<byte>();
			while (true)
			{
				next_char = inputStream.ReadByte();
				if (next_char == '\n') { break; }
				if (next_char == '\r') { continue; }
				if (next_char == -1) { break; };
				if (next_char < 32 || next_char > 126)
					return null;
				if (data.Count >= maxLength)
					return null;
				data.Add((byte)next_char);
			}
			return Encoding.ASCII.GetString(data.ToArray());
		}
		/// <summary>
		/// Converts a byte array to a hexidecimal string using either upper or lower case letters.
		/// </summary>
		/// <param name="buffer">The byte array to convert.</param>
		/// <param name="capitalLetters">If true, letters will be upper case.</param>
		/// <returns></returns>
		public static string ToHex(byte[] buffer, bool capitalLetters = true)
		{
			StringBuilder sBuilder = new StringBuilder();
			if (capitalLetters)
				for (int i = 0; i < buffer.Length; i++)
					sBuilder.Append(buffer[i].ToString("X2"));
			else
				for (int i = 0; i < buffer.Length; i++)
					sBuilder.Append(buffer[i].ToString("x2"));
			return sBuilder.ToString();
		}
		/// <summary>
		/// Concatenates a variable number of byte arrays into one byte array.
		/// </summary>
		/// <param name="arrays">The byte arrays to concatenate.</param>
		/// <returns>A new byte array that is the concatenation of all the input arrays.</returns>
		public static byte[] ConcatenateByteArrays(params byte[][] arrays)
		{
			int totalLength = 0;
			foreach (byte[] array in arrays)
				totalLength += array.Length;

			byte[] result = new byte[totalLength];
			int offset = 0;
			foreach (byte[] array in arrays)
			{
				Buffer.BlockCopy(array, 0, result, offset, array.Length);
				offset += array.Length;
			}

			return result;
		}
		#region byte[] Buffer Pooling
		private const int BufferSize = 81920;
		/// <summary>
		/// A pool which provides byte arrays with length of <see cref="BufferSize"/>.
		/// </summary>
		private static ObjectPool<byte[]> bufferPool = new ObjectPool<byte[]>(() => new byte[BufferSize], 256);
		/// <summary>
		/// Returns a byte[] with length of 81920 from an object pool.  The buffer may have been used previously and contain non-zero values.
		/// </summary>
		/// <returns>A byte[] with length 81920.  The buffer may have been used previously and contain non-zero values.</returns>
		public static byte[] BufferGet()
		{
			return bufferPool.GetObject();
		}
		/// <summary>
		/// Recycles a byte[] with length of 81920 back into the object pool.  The buffer will be given to another caller later without being zeroed-out.
		/// </summary>
		/// <exception cref="ArgumentNullException">If the buffer is null.</exception>
		/// <exception cref="ArgumentException">If the buffer is an improper length.</exception>
		public static void BufferRecycle(byte[] buffer)
		{
			if (buffer == null)
				throw new ArgumentNullException(nameof(buffer));
			if (buffer.Length != BufferSize)
				throw new ArgumentException("Refusing to recycle buffer of length " + buffer.Length + " because the pool requires buffers of length " + BufferSize + ".", nameof(buffer));
			bufferPool.PutObject(buffer);
		}
		#endregion
		#region Stream Helpers
		/// <summary>
		/// Reads all bytes until the end of the stream, then returns the read data as a byte array.
		/// </summary>
		/// <param name="stream">The stream to read data from.</param>
		/// <returns>All bytes from the current position to the end of the stream.</returns>
		public static byte[] ReadToEnd(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			if (stream is MemoryStream && (stream as MemoryStream).Position == 0)
				return ((MemoryStream)stream).ToArray();
			using (MemoryStream memoryStream = new MemoryStream())
			{
				stream.CopyTo(memoryStream);
				return memoryStream.ToArray();
			}
		}
		/// <summary>
		/// Reads all bytes until the end of the stream, then returns the read data as a byte array.
		/// </summary>
		/// <param name="stream">The stream to read data from.</param>
		/// <returns>All bytes from the current position to the end of the stream.</returns>
		public static async Task<byte[]> ReadToEndAsync(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			if (stream is MemoryStream && (stream as MemoryStream).Position == 0)
				return ((MemoryStream)stream).ToArray();
			using (MemoryStream memoryStream = new MemoryStream())
			{
				await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
				return memoryStream.ToArray();
			}
		}
		/// <summary>
		/// Contains the result of an async read operation.
		/// </summary>
		public class AsyncReadResult
		{
			/// <summary>
			/// True if the end of the stream is reached.
			/// </summary>
			public bool EndOfStream;
			/// <summary>
			/// The bytes that were read, or null if the max length was exceeded.
			/// </summary>
			public byte[] Data;
			/// <summary>
			/// Constructs a new AsyncReadResult.
			/// </summary>
			/// <param name="endOfStream">True if the end of the stream is reached.</param>
			/// <param name="data">The bytes that were read, or null if the max length was exceeded.</param>
			public AsyncReadResult(bool endOfStream, byte[] data)
			{
				EndOfStream = endOfStream;
				Data = data;
			}
		}
		/// <summary>
		/// Reads all bytes until the end of the stream, then returns true.  The read data is placed in an output argument byte array.  If maxLength is exceeded while reading, the read data is unavailable and the method returns false.
		/// </summary>
		/// <param name="stream">The stream to read data from.</param>
		/// <param name="maxLength">Maximium number of bytes to read before aborting.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns>An object indicating the result of the operation.</returns>
		public static async Task<AsyncReadResult> ReadToEndWithMaxLengthAsync(Stream stream, int maxLength, CancellationToken cancellationToken = default)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			if (maxLength < 0)
				throw new ArgumentOutOfRangeException(nameof(maxLength));

			if (stream is MemoryStream && (stream as MemoryStream).Position == 0)
			{
				if (stream.Length > maxLength)
					return new AsyncReadResult(false, null);
				else
				{
					byte[] data = ((MemoryStream)stream).ToArray();
					return new AsyncReadResult(true, data);
				}
			}
			using (MemoryStream memoryStream = new MemoryStream())
			{
				byte[] buf = BufferGet();
				try
				{
					int totalRead = 0;
					int read = 1;
					while (read > 0 && totalRead <= maxLength)
					{
						read = await stream.ReadAsync(buf, 0, buf.Length, cancellationToken).ConfigureAwait(false);
						if (read > 0)
							memoryStream.Write(buf, 0, read);
						totalRead += read;
					}
					if (totalRead > maxLength)
						return new AsyncReadResult(false, null);
					byte[] data = memoryStream.ToArray();
					return new AsyncReadResult(true, data);
				}
				finally
				{
					BufferRecycle(buf);
				}
			}
		}
		/// <summary>
		/// Reads all bytes until the end of the stream, then returns true.  The read data is placed in an output argument byte array.  If maxLength is exceeded while reading, the read data is unavailable and the method returns false.
		/// </summary>
		/// <param name="stream">The stream to read data from.</param>
		/// <param name="maxLength">Maximium number of bytes to read before aborting.</param>
		/// <param name="data">(Output) Byte array containing the read data, only if its length is maxLength or less.</param>
		/// <returns>True if data was read successfully within the <paramref name="maxLength"/> limit.</returns>
		public static bool ReadToEndWithMaxLength(Stream stream, int maxLength, out byte[] data)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			if (maxLength < 0)
				throw new ArgumentOutOfRangeException(nameof(maxLength));

			data = null;
			if (stream is MemoryStream && (stream as MemoryStream).Position == 0)
			{
				if (stream.Length > maxLength)
					return false;
				else
				{
					data = ((MemoryStream)stream).ToArray();
					return true;
				}
			}
			using (MemoryStream memoryStream = new MemoryStream())
			{
				byte[] buf = BufferGet();
				try
				{
					int totalRead = 0;
					int read = 1;
					while (read > 0 && totalRead <= maxLength)
					{
						read = stream.Read(buf, 0, buf.Length);
						if (read > 0)
							memoryStream.Write(buf, 0, read);
						totalRead += read;
					}
					if (totalRead > maxLength)
						return false;
					data = memoryStream.ToArray();
					return true;
				}
				finally
				{
					BufferRecycle(buf);
				}
			}
		}
		/// <summary>
		/// Reads data from the stream in 81920-byte chunks until the end of stream is reached.  The data is discarded as soon as it is read. If the stream supports seeking, it is simply seeked to the end.
		/// </summary>
		/// <param name="stream">The stream to read data from.</param>
		public static void DiscardUntilEndOfStream(Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			if (stream.CanSeek)
			{
				stream.Seek(0, SeekOrigin.End);
				return;
			}
			byte[] buf = BufferGet();
			try
			{
				int read = 1;
				while (read > 0)
					read = stream.Read(buf, 0, buf.Length);
			}
			finally
			{
				BufferRecycle(buf);
			}
		}
		/// <summary>
		/// Contains the result of an async discard operation.
		/// </summary>
		public class AsyncDiscardResult
		{
			/// <summary>
			/// True if the end of the stream is reached.
			/// </summary>
			public bool EndOfStream;
			/// <summary>
			/// The number of bytes that were read.
			/// </summary>
			public long BytesDiscarded;
			/// <summary>
			/// Constructs a new AsyncDiscardResult.
			/// </summary>
			/// <param name="endOfStream">True if the end of the stream is reached.</param>
			/// <param name="bytesDiscarded">The number of bytes that were read.</param>
			public AsyncDiscardResult(bool endOfStream, long bytesDiscarded)
			{
				EndOfStream = endOfStream;
				BytesDiscarded = bytesDiscarded;
			}
		}
		/// <summary>
		/// Reads data from the stream in 81920-byte chunks until the end of stream is reached.  The data is discarded as soon as it is read. If the stream supports seeking, it is simply seeked to the end.
		/// </summary>
		/// <param name="stream">The stream to read data from.</param>
		/// <param name="maxLength">If more than this many bytes are read, the method will return false without continuing to the end of the stream.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns>An object indicating the result of the operation.</returns>
		public static async Task<AsyncDiscardResult> DiscardUntilEndOfStreamWithMaxLengthAsync(Stream stream, long maxLength, CancellationToken cancellationToken = default)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			if (maxLength < 0)
				throw new ArgumentOutOfRangeException(nameof(maxLength));

			if (stream.CanSeek)
			{
				stream.Seek(0, SeekOrigin.End);
				return new AsyncDiscardResult(true, 0);
			}
			byte[] buf = BufferGet();
			try
			{
				long bytesDiscarded = 0;
				int read = 1;
				while (read > 0 && bytesDiscarded < maxLength)
				{
					int toRead = (int)Math.Min(buf.Length, maxLength - bytesDiscarded);
					read = await stream.ReadAsync(buf, 0, toRead, cancellationToken).ConfigureAwait(false);
					bytesDiscarded += read;
				}
				if (read != 0)
				{
					read = await stream.ReadAsync(buf, 0, 1, cancellationToken).ConfigureAwait(false);
					bytesDiscarded += read;
				}
				return new AsyncDiscardResult(read == 0, bytesDiscarded);
			}
			finally
			{
				BufferRecycle(buf);
			}
		}
		/// <summary>
		/// Reads data from the stream in 81920-byte chunks until the end of stream is reached.  The data is discarded as soon as it is read. If the stream supports seeking, it is simply seeked to the end.  Returns true if the end of the stream is reached.
		/// </summary>
		/// <param name="stream">The stream to read data from.</param>
		/// <param name="maxLength">If more than this many bytes are read, the method will return false without continuing to the end of the stream.</param>
		/// <param name="bytesDiscarded">(Output) The number of bytes that were read.</param>
		/// <returns>True if the end of the stream is reached.</returns>
		public static bool DiscardUntilEndOfStreamWithMaxLength(Stream stream, long maxLength, out long bytesDiscarded)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			if (maxLength < 0)
				throw new ArgumentOutOfRangeException(nameof(maxLength));

			if (stream.CanSeek)
			{
				bytesDiscarded = 0;
				stream.Seek(0, SeekOrigin.End);
				return true;
			}
			byte[] buf = BufferGet();
			try
			{
				bytesDiscarded = 0;
				int read = 1;
				while (read > 0 && bytesDiscarded < maxLength)
				{
					int toRead = (int)Math.Min(buf.Length, maxLength - bytesDiscarded);
					read = stream.Read(buf, 0, toRead);
					bytesDiscarded += read;
				}
				if (read != 0)
				{
					read = stream.Read(buf, 0, 1);
					bytesDiscarded += read;
				}
				return read == 0;
			}
			finally
			{
				BufferRecycle(buf);
			}
		}
		#endregion
		#region ReadNBytes
		/// <summary>
		/// Reads a specific number of bytes from the stream, returning a byte array.  Ordinary stream.Read operations are not guaranteed to read all the requested bytes.
		/// </summary>
		/// <param name="s">The stream to read from.</param>
		/// <param name="n">The number of bytes to read.</param>
		/// <returns></returns>
		public static byte[] ReadNBytes(IDataStream s, int n)
		{
			byte[] buffer = new byte[n];
			ReadBytes(s, buffer);
			return buffer;
		}
		/// <summary>
		/// Fills the given buffer with bytes from the stream. If unable to read that many bytes, this method throws an Exception.
		/// </summary>
		/// <param name="s">The stream to read from.</param>
		/// <param name="buffer">The buffer to fill.</param>
		/// <returns></returns>
		public static void ReadBytes(IDataStream s, byte[] buffer)
		{
			if (buffer.Length == 0)
				return; // Just to be explicit and sure about this behavior.
			int totalRead = 0;
			int justRead;
			try
			{
				do
					totalRead += (justRead = s.Read(buffer, totalRead, buffer.Length - totalRead));
				while (justRead > 0 && totalRead < buffer.Length);
			}
			catch (IOException ex)
			{
				if (ex.InnerException is SocketException)
					throw new EndOfStreamException("Stream was closed", ex);
				else
					throw;
			}
			catch (SocketException ex) { throw new EndOfStreamException("Stream was closed", ex); }
			if (totalRead < buffer.Length)
				throw new EndOfStreamException("Stream was closed");
			else if (totalRead > buffer.Length)
				throw new Exception("Somehow read too much from stream");
			return;
		}
		/// <summary>
		/// Reads a specific number of bytes from the stream, returning a byte array.  Ordinary stream.Read operations are not guaranteed to read all the requested bytes.
		/// </summary>
		/// <param name="s">The stream to read from.</param>
		/// <param name="n">The number of bytes to read.</param>
		/// <returns></returns>
		public static byte[] ReadNBytes(Stream s, int n)
		{
			byte[] buffer = new byte[n];
			ReadBytes(s, buffer);
			return buffer;
		}
		/// <summary>
		/// Fills the given buffer with bytes from the stream. If unable to read that many bytes, this method throws an Exception.
		/// </summary>
		/// <param name="s">The stream to read from.</param>
		/// <param name="buffer">The buffer to fill.</param>
		/// <returns></returns>
		public static void ReadBytes(Stream s, byte[] buffer)
		{
			if (buffer.Length == 0)
				return; // Just to be explicit and sure about this behavior.
			int totalRead = 0;
			int justRead;
			try
			{
				do
					totalRead += (justRead = s.Read(buffer, totalRead, buffer.Length - totalRead));
				while (justRead > 0 && totalRead < buffer.Length);
			}
			catch (IOException ex)
			{
				if (ex.InnerException is SocketException)
					throw new EndOfStreamException("Stream was closed", ex);
				else
					throw;
			}
			catch (SocketException ex) { throw new EndOfStreamException("Stream was closed", ex); }
			if (totalRead < buffer.Length)
				throw new EndOfStreamException("Stream was closed");
			else if (totalRead > buffer.Length)
				throw new Exception("Somehow read too much from stream");
			return;
		}
		/// <summary>
		/// Reads a specific number of bytes from the stream and performs NetworkToHostOrder on the resulting byte array before returning it.
		/// </summary>
		/// <param name="s">The stream to read from.</param>
		/// <param name="n">The number of bytes to read.</param>
		/// <returns></returns>
		public static byte[] ReadNBytesFromNetworkOrder(Stream s, int n)
		{
			return NetworkToHostOrder(ReadNBytes(s, n));
		}
		/// <summary>
		/// If the current system is Little Endian, reverses the order of the bytes.
		/// If the current system is Big Endian, the array is returned unmodified.
		/// </summary>
		/// <param name="buf">The byte array.</param>
		/// <returns></returns>
		public static byte[] NetworkToHostOrder(byte[] buf)
		{
			if (BitConverter.IsLittleEndian)
				Array.Reverse(buf);
			return buf;
		}
		/// <summary>
		/// Returns a new array containing the specified bytes from the source array.
		/// </summary>
		/// <param name="buf">The source byte array.</param>
		/// <param name="offset">The offset to begin copying bytes at.</param>
		/// <param name="length">The number of bytes to copy.</param>
		/// <returns></returns>
		public static byte[] SubArray(byte[] buf, int offset, int length)
		{
			byte[] dst = new byte[length];
			Array.Copy(buf, offset, dst, 0, length);
			return dst;
		}
		/// <summary>
		/// Copies a section of the specified array into a new array and performs a NetworkToHostOrder operation on the array before returning it.
		/// </summary>
		/// <param name="buf">The source byte array.</param>
		/// <param name="offset">The offset to begin copying bytes at.</param>
		/// <param name="length">The number of bytes to copy.</param>
		/// <returns></returns>
		public static byte[] NetworkToHostOrder(byte[] buf, int offset, int length)
		{
			return NetworkToHostOrder(SubArray(buf, offset, length));
		}
		#endregion
		#region Write to byte array (Big endian in the buffer)
		public static void WriteInt16(short num, byte[] buffer, int offset)
		{
			Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(num)), 0, buffer, offset, 2);
		}
		public static void WriteUInt16(ushort num, byte[] buffer, int offset)
		{
			Array.Copy(BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder((short)num)), 0, buffer, offset, 2);
		}
		public static void WriteInt32(int num, byte[] buffer, int offset)
		{
			Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(num)), 0, buffer, offset, 4);
		}
		public static void WriteUInt32(uint num, byte[] buffer, int offset)
		{
			Array.Copy(BitConverter.GetBytes((uint)IPAddress.HostToNetworkOrder((int)num)), 0, buffer, offset, 4);
		}
		public static void WriteInt64(long num, byte[] buffer, int offset)
		{
			Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(num)), 0, buffer, offset, 8);
		}
		public static void WriteUInt64(ulong num, byte[] buffer, int offset)
		{
			Array.Copy(BitConverter.GetBytes((ulong)IPAddress.HostToNetworkOrder((long)num)), 0, buffer, offset, 8);
		}
		public static void WriteFloat(float num, byte[] buffer, int offset)
		{
			Array.Copy(NetworkToHostOrder(BitConverter.GetBytes(num)), 0, buffer, offset, 4);
		}
		public static void WriteDouble(double num, byte[] buffer, int offset)
		{
			Array.Copy(NetworkToHostOrder(BitConverter.GetBytes(num)), 0, buffer, offset, 8);
		}
		/// <summary>
		/// Writes a string to the buffer at the specified offset. The string will be encoded as UTF8 with no byte order mark. Returns the number of bytes written.
		/// </summary>
		/// <param name="str">String to write.</param>
		/// <param name="buffer">The buffer to write to.</param>
		/// <param name="offset">The offset in the buffer to begin writing at.</param>
		public static int WriteUtf8(string str, byte[] buffer, int offset)
		{
			int maxLength = buffer.Length - offset;
			if (str.Length > maxLength)
				throw new ArgumentException("WriteUtf8(string, byte[], int) method received a string that is too large for the buffer it would be written to");
			byte[] bytes = Utf8NoBOM.GetBytes(str);
			if (bytes.Length > maxLength)
				throw new ArgumentException("WriteUtf8(string, byte[], int) method received a string that is too large for the buffer it would be written to, once UTF8-encoded.");
			Array.Copy(bytes, 0, buffer, offset, bytes.Length);
			return bytes.Length;
		}
		/// <summary>
		/// <para>Writes the length of the string as a 16 bit unsigned integer, then writes the string.</para>
		/// <para>The string will be encoded as UTF8 with no byte order mark.</para>
		/// <para>Returns the number of bytes written.</para>
		/// <para>Throws an exception if the byte array is larger than a 16 bit unsigned integer can hold.</para>
		/// </summary>
		/// <param name="str">String to write.</param>
		/// <param name="buffer">The buffer to write to.</param>
		/// <param name="offset">The offset in the buffer to begin writing at.</param>
		public static ushort WriteUtf8_16(string str, byte[] buffer, int offset)
		{
			if (str.Length > ushort.MaxValue)
				throw new ArgumentException("WriteUtf8_16 method cannot accept a string with length greater than " + ushort.MaxValue, "str");
			int maxLength = buffer.Length - (offset + 2);
			if (str.Length > maxLength)
				throw new ArgumentException("WriteUtf8_16(string, byte[], int) method received a string that is too large for the buffer it would be written to");
			byte[] bytes = Utf8NoBOM.GetBytes(str);
			if (bytes.Length > ushort.MaxValue)
				throw new ArgumentException("WriteUtf8_16 method cannot accept a string with UTF8 length greater than " + ushort.MaxValue, "str");
			if (bytes.Length > maxLength)
				throw new ArgumentException("WriteUtf8(string, byte[], int) method received a string that is too large for the buffer it would be written to, once UTF8-encoded.");
			WriteUInt16((ushort)bytes.Length, buffer, offset);
			Array.Copy(bytes, 0, buffer, offset + 2, bytes.Length);
			return (ushort)bytes.Length;
		}
		/// <summary>
		/// <para>Writes the length of the string as a 32 bit unsigned integer, then writes the string.</para>
		/// <para>The string will be encoded as UTF8 with no byte order mark.</para>
		/// <para>Returns the number of bytes written.</para>
		/// </summary>
		/// <param name="str">String to write.</param>
		/// <param name="buffer">The buffer to write to.</param>
		/// <param name="offset">The offset in the buffer to begin writing at.</param>
		public static uint WriteUtf8_32(string str, byte[] buffer, int offset)
		{
			int maxLength = buffer.Length - (offset + 2);
			if (str.Length > maxLength)
				throw new ArgumentException("WriteUtf8_16(string, byte[], int) method received a string that is too large for the buffer it would be written to");
			byte[] bytes = Utf8NoBOM.GetBytes(str);
			if (bytes.Length > maxLength)
				throw new ArgumentException("WriteUtf8(string, byte[], int) method received a string that is too large for the buffer it would be written to, once UTF8-encoded.");
			WriteUInt32((uint)bytes.Length, buffer, offset);
			Array.Copy(bytes, 0, buffer, offset + 4, bytes.Length);
			return (uint)bytes.Length;
		}
		#endregion
		#region Write to stream (Big endian in the stream)
		public static void WriteInt16(short num, Stream s)
		{
			s.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(num)), 0, 2);
		}
		public static void WriteUInt16(ushort num, Stream s)
		{
			s.Write(BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder((short)num)), 0, 2);
		}
		public static void WriteInt32(int num, Stream s)
		{
			s.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(num)), 0, 4);
		}
		public static void WriteUInt32(uint num, Stream s)
		{
			s.Write(BitConverter.GetBytes((uint)IPAddress.HostToNetworkOrder((int)num)), 0, 4);
		}
		public static void WriteInt64(long num, Stream s)
		{
			s.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(num)), 0, 8);
		}
		public static void WriteUInt64(ulong num, Stream s)
		{
			s.Write(BitConverter.GetBytes((ulong)IPAddress.HostToNetworkOrder((long)num)), 0, 8);
		}
		public static void WriteFloat(float num, Stream s)
		{
			s.Write(NetworkToHostOrder(BitConverter.GetBytes(num)), 0, 4);
		}
		public static void WriteDouble(double num, Stream s)
		{
			s.Write(NetworkToHostOrder(BitConverter.GetBytes(num)), 0, 8);
		}
		/// <summary>
		/// Writes a string to the specified stream. The string will be encoded as UTF8 with no byte order mark. Returns the number of bytes written.
		/// </summary>
		/// <param name="str">String to write.</param>
		/// <param name="s">Stream to write to.</param>
		public static int WriteUtf8(string str, Stream s)
		{
			byte[] bytes = Utf8NoBOM.GetBytes(str);
			s.Write(bytes, 0, bytes.Length);
			return bytes.Length;
		}
		/// <summary>
		/// <para>Writes the length of the string as a 16 bit unsigned integer, then writes the string.</para>
		/// <para>The string will be encoded as UTF8 with no byte order mark.</para>
		/// <para>Returns the number of bytes written.</para>
		/// <para>Throws an exception if the byte array is larger than a 16 bit unsigned integer can hold.</para>
		/// </summary>
		/// <param name="str">String to write.</param>
		/// <param name="s">Stream to write to.</param>
		/// <exception cref="ArgumentException">If the string is longer than 65535 characters or bytes.</exception>
		public static ushort WriteUtf8_16(string str, Stream s)
		{
			if (str.Length > ushort.MaxValue)
				throw new ArgumentException("WriteUtf8_16 method cannot accept a string with length greater than " + ushort.MaxValue, "str");
			byte[] bytes = Utf8NoBOM.GetBytes(str);
			if (bytes.Length > ushort.MaxValue)
				throw new ArgumentException("WriteUtf8_16 method cannot accept a string with UTF8 length greater than " + ushort.MaxValue, "str");
			WriteUInt16((ushort)bytes.Length, s);
			s.Write(bytes, 0, bytes.Length);
			return (ushort)bytes.Length;
		}
		/// <summary>
		/// <para>Writes the length of the string as a 32 bit unsigned integer, then writes the string.</para>
		/// <para>The string will be encoded as UTF8 with no byte order mark.</para>
		/// <para>Returns the number of bytes written.</para>
		/// </summary>
		/// <param name="str">String to write.</param>
		/// <param name="s">Stream to write to.</param>
		public static uint WriteUtf8_32(string str, Stream s)
		{
			byte[] bytes = Utf8NoBOM.GetBytes(str);
			WriteUInt32((uint)bytes.Length, s);
			s.Write(bytes, 0, bytes.Length);
			return (uint)bytes.Length;
		}
		#endregion
		#region Read from byte array (Big endian in the buffer)
		public static short ReadInt16(byte[] buffer, int offset)
		{
			return IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, offset));
		}
		public static ushort ReadUInt16(byte[] buffer, int offset)
		{
			return (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(buffer, offset));
		}
		public static int ReadInt32(byte[] buffer, int offset)
		{
			return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, offset));
		}
		public static uint ReadUInt32(byte[] buffer, int offset)
		{
			return (uint)IPAddress.NetworkToHostOrder((int)BitConverter.ToUInt32(buffer, offset));
		}
		public static long ReadInt64(byte[] buffer, int offset)
		{
			return IPAddress.NetworkToHostOrder(BitConverter.ToInt64(buffer, offset));
		}
		public static ulong ReadUInt64(byte[] buffer, int offset)
		{
			return (ulong)IPAddress.NetworkToHostOrder((long)BitConverter.ToUInt64(buffer, offset));
		}
		public static float ReadFloat(byte[] buffer, int offset)
		{
			return BitConverter.ToSingle(NetworkToHostOrder(buffer, offset, 4), 0);
		}
		public static double ReadDouble(byte[] buffer, int offset)
		{
			return BitConverter.ToDouble(NetworkToHostOrder(buffer, offset, 8), 0);
		}
		/// <summary>
		/// Converts all data from the buffer to a string assuming UTF8 encoding with no byte order mark.
		/// </summary>
		/// <param name="buffer">The buffer to convert.</param>
		/// <returns></returns>
		public static string ReadUtf8(byte[] buffer)
		{
			return Utf8NoBOM.GetString(buffer, 0, buffer.Length);
		}
		/// <summary>
		/// Reads the specified number of bytes from the buffer and converts them to a string assuming UTF8 encoding with no byte order mark.
		/// </summary>
		/// <param name="buffer">The buffer to read from.</param>
		/// <param name="offset">The offset to begin reading at.</param>
		/// <param name="byteLength">The number of bytes to read.</param>
		/// <returns></returns>
		public static string ReadUtf8(byte[] buffer, int offset, int byteLength)
		{
			if (offset < 0 || offset >= buffer.Length)
				throw new ArgumentException("offset must be >= 0 and < buffer.Length", "offset");
			if (buffer.Length - offset < byteLength)
				throw new ArgumentException("ReadUtf8(byte[" + buffer.Length + "], " + offset + ", " + byteLength + ") method instructed to read beyond the end of the buffer.");
			return Utf8NoBOM.GetString(buffer, offset, byteLength);
		}
		/// <summary>
		/// Reads a UTF8 string (no byte order mark) from the buffer, assuming the string's length is prepended as a 16 bit unsigned integer.
		/// </summary>
		/// <param name="buffer">The buffer to read from.</param>
		/// <param name="offset">The offset to begin reading at.</param>
		/// <returns></returns>
		public static string ReadUtf8_16(byte[] buffer, int offset)
		{
			return ReadUtf8_16(buffer, offset, out ushort ignored);
		}
		/// <summary>
		/// Reads a UTF8 string (no byte order mark) from the buffer, assuming the string's length is prepended as a 16 bit unsigned integer.
		/// </summary>
		/// <param name="buffer">The buffer to read from.</param>
		/// <param name="offset">The offset to begin reading at.</param>
		/// <param name="strLen">[out] Length in bytes of the string that was read. (does not include the 2 byte length of the length field)</param>
		/// <returns></returns>
		public static string ReadUtf8_16(byte[] buffer, int offset, out ushort strLen)
		{
			if (offset < 0 || offset >= buffer.Length)
				throw new ArgumentException("offset must be >= 0 and < buffer.Length", "offset");
			if (buffer.Length - offset < 2)
				throw new ArgumentException("ReadUtf8_16(byte[" + buffer.Length + "], " + offset + ") method cannot read byte length because there are not enough bytes remaining in the buffer.");
			strLen = ReadUInt16(buffer, offset);
			return ReadUtf8(buffer, offset + 2, strLen);
		}
		/// <summary>
		/// Reads a UTF8 string (no byte order mark) from the buffer, assuming the string's length is prepended as a 32 bit unsigned integer.
		/// </summary>
		/// <param name="buffer">The buffer to read from.</param>
		/// <param name="offset">The offset to begin reading at.</param>
		/// <returns></returns>
		public static string ReadUtf8_32(byte[] buffer, int offset)
		{
			return ReadUtf8_32(buffer, offset, out uint ignored);
		}
		/// <summary>
		/// Reads a UTF8 string (no byte order mark) from the buffer, assuming the string's length is prepended as a 32 bit unsigned integer.
		/// </summary>
		/// <param name="buffer">The buffer to read from.</param>
		/// <param name="offset">The offset to begin reading at.</param>
		/// <param name="strLen">[out] Length in bytes of the string that was read. (does not include the 4 byte length of the length field)</param>
		/// <returns></returns>
		public static string ReadUtf8_32(byte[] buffer, int offset, out uint strLen)
		{
			if (offset < 0 || offset >= buffer.Length)
				throw new ArgumentException("offset must be >= 0 and < buffer.Length", "offset");
			if (buffer.Length - offset < 4)
				throw new ArgumentException("ReadUtf8_32(byte[" + buffer.Length + "], " + offset + ") method cannot read byte length because there are not enough bytes remaining in the buffer.");
			strLen = ReadUInt32(buffer, offset);
			return ReadUtf8(buffer, offset + 4, (int)strLen);
		}
		#endregion
		#region Read from stream (Big endian on the stream)
		public static short ReadInt16(Stream s)
		{
			return IPAddress.NetworkToHostOrder(BitConverter.ToInt16(ReadNBytes(s, 2), 0));
		}
		public static ushort ReadUInt16(Stream s)
		{
			return (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(ReadNBytes(s, 2), 0));
		}
		public static int ReadInt32(Stream s)
		{
			return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(ReadNBytes(s, 4), 0));
		}
		public static uint ReadUInt32(Stream s)
		{
			return (uint)IPAddress.NetworkToHostOrder((int)BitConverter.ToUInt32(ReadNBytes(s, 4), 0));
		}
		public static long ReadInt64(Stream s)
		{
			return IPAddress.NetworkToHostOrder(BitConverter.ToInt64(ReadNBytes(s, 8), 0));
		}
		public static ulong ReadUInt64(Stream s)
		{
			return (ulong)IPAddress.NetworkToHostOrder((long)BitConverter.ToUInt64(ReadNBytes(s, 8), 0));
		}
		public static float ReadFloat(Stream s)
		{
			return BitConverter.ToSingle(ReadNBytesFromNetworkOrder(s, 4), 0);
		}
		public static double ReadDouble(Stream s)
		{
			return BitConverter.ToDouble(ReadNBytesFromNetworkOrder(s, 8), 0);
		}
		/// <summary>
		/// Reads the specified number of bytes from the stream and converts them to a string assuming UTF8 encoding with no byte order mark.
		/// </summary>
		/// <param name="s">The stream to read from.</param>
		/// <param name="byteLength">The number of bytes to read.</param>
		/// <returns></returns>
		public static string ReadUtf8(Stream s, int byteLength)
		{
			return Utf8NoBOM.GetString(ReadNBytes(s, byteLength), 0, byteLength);
		}
		/// <summary>
		/// Reads a UTF8 string (no byte order mark) from the stream, assuming the string's length is prepended as a 16 bit unsigned integer.
		/// </summary>
		/// <param name="s">The stream to read from.</param>
		/// <returns></returns>
		public static string ReadUtf8_16(Stream s)
		{
			int byteLength = ReadUInt16(s);
			return Utf8NoBOM.GetString(ReadNBytes(s, byteLength), 0, byteLength);
		}
		/// <summary>
		/// Reads a UTF8 string (no byte order mark) from the stream, assuming the string's length is prepended as a 32 bit unsigned integer.
		/// </summary>
		/// <param name="s">The stream to read from.</param>
		/// <returns></returns>
		public static string ReadUtf8_32(Stream s)
		{
			int byteLength = (int)ReadUInt32(s);
			return Utf8NoBOM.GetString(ReadNBytes(s, byteLength), 0, byteLength);
		}
		#endregion
		#region Read from stream (Little endian on the stream)
		public static short ReadInt16LE(Stream s)
		{
			if (BitConverter.IsLittleEndian)
				return BitConverter.ToInt16(ReadNBytes(s, 2), 0);
			return ReadInt16(s);
		}
		public static ushort ReadUInt16LE(Stream s)
		{
			if (BitConverter.IsLittleEndian)
				return BitConverter.ToUInt16(ReadNBytes(s, 2), 0);
			return ReadUInt16(s);
		}
		public static int ReadInt32LE(Stream s)
		{
			if (BitConverter.IsLittleEndian)
				return BitConverter.ToInt32(ReadNBytes(s, 4), 0);
			return ReadInt32(s);
		}
		public static uint ReadUInt32LE(Stream s)
		{
			if (BitConverter.IsLittleEndian)
				return BitConverter.ToUInt32(ReadNBytes(s, 4), 0);
			return ReadUInt32(s);
		}
		public static long ReadInt64LE(Stream s)
		{
			if (BitConverter.IsLittleEndian)
				return BitConverter.ToInt64(ReadNBytes(s, 8), 0);
			return ReadInt64(s);
		}
		public static ulong ReadUInt64LE(Stream s)
		{
			if (BitConverter.IsLittleEndian)
				return BitConverter.ToUInt64(ReadNBytes(s, 8), 0);
			return ReadUInt64(s);
		}
		public static float ReadFloatLE(Stream s)
		{
			if (BitConverter.IsLittleEndian)
				return BitConverter.ToSingle(ReadNBytes(s, 4), 0);
			return ReadFloat(s);
		}
		public static double ReadDoubleLE(Stream s)
		{
			if (BitConverter.IsLittleEndian)
				return BitConverter.ToDouble(ReadNBytes(s, 8), 0);
			return ReadDouble(s);
		}
		#endregion
	}
	[Obsolete("C# 7.0 supports binary literals in the format '0b0000_0001', rendering this enum obsolete.  Happily, any number of underscores may be inserted between the 0 and 1 digits, for readability purposes, and the compiler will ignore them.")]
	[Flags]
	public enum ByteFlagConstants : byte
	{
		b0000_0000 = 0
		, b0000_0001 = 1
		, b0000_0010 = 2
		, b0000_0100 = 4
		, b0000_1000 = 8
		, b0001_0000 = 16
		, b0010_0000 = 32
		, b0100_0000 = 64
		, b1000_0000 = 128
	}
}
