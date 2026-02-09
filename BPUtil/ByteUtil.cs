using BPUtil.IO;
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
		/// <param name="a">First array to compare. May be null.</param>
		/// <param name="b">Second array to compare. May be null.</param>
		/// <returns>True if both arrays are null or if they have the same length and identical contents; otherwise false.</returns>
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
		/// <param name="thisBytes">Base byte array to test. May be null.</param>
		/// <param name="thatBytes">Prefix byte array to look for. If null, this method returns false unless <paramref name="thisBytes"/> is also null.</param>
		/// <returns>True when <paramref name="thatBytes"/> is a prefix of <paramref name="thisBytes"/> or both are null; otherwise false.</returns>
		public static bool StartsWith(byte[] thisBytes, byte[] thatBytes)
		{
			if (thisBytes == null)
				return thatBytes == null;
			else if (thatBytes == null)
				return false;
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
		/// Compares two byte arrays and returns an integer indicating the relative position of [a] versus [b]. Uses string ordering rules.
		/// </summary>
		/// <param name="a">First byte array.</param>
		/// <param name="b">Second byte array.</param>
		/// <returns>
		/// -1 if <paramref name="a"/> is lexically less than <paramref name="b"/>,
		/// 1 if <paramref name="a"/> is greater than <paramref name="b"/>,
		/// 0 if they are equal.
		/// </returns>
		public static int CompareByteArraysLikeStrings(byte[] a, byte[] b)
		{
			int max = Math.Min(a.Length, b.Length);
			for (int i = 0; i < max; i++)
			{
				if (a[i] < b[i])
					return -1;
				else if (b[i] < a[i])
					return 1;
			}
			if (a.Length < b.Length)
				return -1;
			else if (b.Length < a.Length)
				return 1;
			return 0;
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
		/// <param name="mask">An array of equal or lesser length to <paramref name="a"/> and <paramref name="b"/>.</param>
		/// <returns>True if arrays match under the provided mask; otherwise false.</returns>
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
		/// <param name="a">First input array. Must have same length as <paramref name="b"/>.</param>
		/// <param name="b">Second input array. Must have same length as <paramref name="a"/>.</param>
		/// <returns>A new byte array where each element is the XOR of corresponding elements from <paramref name="a"/> and <paramref name="b"/>.</returns>
		/// <exception cref="ArgumentException">Thrown when arrays have different lengths.</exception>
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
		/// <param name="a">First input array. Must have same length as <paramref name="b"/>.</param>
		/// <param name="b">Second input array. Must have same length as <paramref name="a"/>.</param>
		/// <returns>A new byte array where each element is the bitwise AND of corresponding elements from <paramref name="a"/> and <paramref name="b"/>.</returns>
		/// <exception cref="ArgumentException">Thrown when arrays have different lengths.</exception>
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
		/// <param name="a">First input array. Must have same length as <paramref name="b"/>.</param>
		/// <param name="b">Second input array. Must have same length as <paramref name="a"/>.</param>
		/// <returns>A new byte array where each element is the bitwise OR of corresponding elements from <paramref name="a"/> and <paramref name="b"/>.</returns>
		/// <exception cref="ArgumentException">Thrown when arrays have different lengths.</exception>
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
		/// <param name="a">Array to invert in-place. If null, the method does nothing.</param>
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
		/// <param name="a">Source array. May be null.</param>
		/// <returns>A new array containing inverted bytes, or null if <paramref name="a"/> is null.</returns>
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
		/// <param name="numBytes">The length of the byte array to create. Must be &gt;= 0.</param>
		/// <returns>A new byte array of length <paramref name="numBytes"/> filled with random bytes.</returns>
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
		/// <param name="inputStream">Stream to read from. Must be readable.</param>
		/// <param name="maxLength">Maximum length of the line. If exceeded, null is returned.</param>
		/// <returns>The read line as an ASCII string, or null if the line contains invalid characters or exceeds <paramref name="maxLength"/>.</returns>
		public static string ReadPrintableASCIILine(Stream inputStream, int maxLength = 32768)
		{
			int next_char;
			List<byte> data = new List<byte>();
			while (true)
			{
				next_char = inputStream.ReadByte();
				if (next_char == '\n') { break; }
				if (next_char == '\r') { continue; }
				if (next_char == -1) { break; }
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
		/// <param name="buffer">The byte array to convert. Must not be null.</param>
		/// <param name="capitalLetters">If true, letters will be upper case; otherwise lower case.</param>
		/// <returns>A hexadecimal string representation of <paramref name="buffer"/>.</returns>
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
		/// Converts a byte to a hexidecimal string using either upper or lower case letters.
		/// </summary>
		/// <param name="v">The value to convert.</param>
		/// <param name="capitalLetters">If true, letters will be upper case; otherwise lower case.</param>
		/// <returns>Two-character hexadecimal representation of <paramref name="v"/>.</returns>
		public static string ToHex(byte v, bool capitalLetters = true)
		{
			if (capitalLetters)
				return v.ToString("X2");
			else
				return v.ToString("x2");
		}
		/// <summary>
		/// Converts a numeric value to a hexidecimal string using either upper or lower case letters and little endian encoding.
		/// </summary>
		/// <param name="v">The value to convert.</param>
		/// <param name="capitalLetters">If true, letters will be upper case; otherwise lower case.</param>
		/// <returns>Hex string representing the 4-byte little-endian encoding of <paramref name="v"/>.</returns>
		public static string ToHexLE(uint v, bool capitalLetters = true)
		{
			byte[] buffer = new byte[4];
			WriteUInt32LE(v, buffer, 0);
			return ByteUtil.ToHex(buffer, capitalLetters);
		}
		/// <summary>
		/// Converts a numeric value to a hexidecimal string using either upper or lower case letters and big endian encoding.
		/// </summary>
		/// <param name="v">The value to convert.</param>
		/// <param name="capitalLetters">If true, letters will be upper case; otherwise lower case.</param>
		/// <returns>Hex string representing the 4-byte big-endian encoding of <paramref name="v"/>.</returns>
		public static string ToHex(uint v, bool capitalLetters = true)
		{
			byte[] buffer = new byte[4];
			WriteUInt32(v, buffer, 0);
			return ByteUtil.ToHex(buffer, capitalLetters);
		}
		/// <summary>
		/// Converts a numeric value to a hexidecimal string using either upper or lower case letters and little endian encoding.
		/// </summary>
		/// <param name="v">The value to convert.</param>
		/// <param name="capitalLetters">If true, letters will be upper case; otherwise lower case.</param>
		/// <returns>Hex string representing the 8-byte little-endian encoding of <paramref name="v"/>.</returns>
		public static string ToHexLE(ulong v, bool capitalLetters = true)
		{
			byte[] buffer = new byte[8];
			WriteUInt64LE(v, buffer, 0);
			return ByteUtil.ToHex(buffer, capitalLetters);
		}
		/// <summary>
		/// Converts a numeric value to a hexidecimal string using either upper or lower case letters and big endian encoding.
		/// </summary>
		/// <param name="v">The value to convert.</param>
		/// <param name="capitalLetters">If true, letters will be upper case; otherwise lower case.</param>
		/// <returns>Hex string representing the 8-byte big-endian encoding of <paramref name="v"/>.</returns>
		public static string ToHex(ulong v, bool capitalLetters = true)
		{
			byte[] buffer = new byte[8];
			WriteUInt64(v, buffer, 0);
			return ByteUtil.ToHex(buffer, capitalLetters);
		}
		/// <summary>
		/// Concatenates a variable number of byte arrays into one byte array.
		/// </summary>
		/// <param name="arrays">The byte arrays to concatenate. Null arguments are not supported.</param>
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
		/// <summary>
		/// Converts a ushort to a float using binary16 (half) encoding.
		/// </summary>
		/// <param name="binary16">A ushort containing floating point data in IEEE 754 binary16 format.</param>
		/// <returns>The corresponding single-precision floating point value (32-bit float).</returns>
		public static float DecodeHalf(ushort binary16)
		{
			// Extract the sign (1 bit), exponent (5 bits), and fraction (10 bits)
			int sign = (binary16 >> 15) & 0x0001;
			int exponent = (binary16 >> 10) & 0x001F;
			int fraction = binary16 & 0x03FF;

			// Handle special cases
			if (exponent == 0)
			{
				// Subnormal number or zero
				if (fraction == 0)
				{
					// Zero
					return (sign == 1) ? -0.0f : 0.0f;
				}
				else
				{
					// Subnormal number
					return (float)((sign == 1 ? -1 : 1) * Math.Pow(2, -14) * (fraction / 1024.0));
				}
			}
			else if (exponent == 0x1F)
			{
				// Infinity or NaN
				if (fraction == 0)
				{
					// Infinity
					return (sign == 1) ? float.NegativeInfinity : float.PositiveInfinity;
				}
				else
				{
					// NaN
					return float.NaN;
				}
			}

			// Normalized number
			return (float)((sign == 1 ? -1 : 1) * Math.Pow(2, exponent - 15) * (1 + fraction / 1024.0));
		}
		#region byte[] Buffer Pooling
		/// <summary>
		/// The size of the buffers, in bytes, that are returned from <see cref="BufferGet"/> and accepted by <see cref="BufferRecycle(byte[])"/>.
		/// </summary>
		public const int BufferSize = 81920;
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
		/// <param name="buffer">The buffer to recycle. Must not be null and must have length equal to <see cref="BufferSize"/>.</param>
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
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns>All bytes from the current position to the end of the stream.</returns>
		public static async Task<byte[]> ReadToEndAsync(Stream stream, CancellationToken cancellationToken = default)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			if (stream is MemoryStream && (stream as MemoryStream).Position == 0)
				return ((MemoryStream)stream).ToArray();
			using (MemoryStream memoryStream = new MemoryStream())
			{
				await stream.CopyToAsync(memoryStream, 81920, cancellationToken).ConfigureAwait(false);
				return memoryStream.ToArray();
			}
		}
		/// <summary>
		/// Contains the result of an async read operation.
		/// </summary>
		public class ReadToEndResult
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
			/// Constructs a new ReadResult.
			/// </summary>
			/// <param name="endOfStream">True if the end of the stream is reached.</param>
			/// <param name="data">The bytes that were read, or null if the max length was exceeded.</param>
			public ReadToEndResult(bool endOfStream, byte[] data)
			{
				EndOfStream = endOfStream;
				Data = data;
			}
		}
		/// <summary>
		/// Reads all bytes until the end of the stream.  If maxLength is exceeded while reading, the read data is unavailable and the result indicates that <c>EndOfStream</c> is false.
		/// </summary>
		/// <param name="stream">The stream to read data from.</param>
		/// <param name="maxLength">Maximium number of bytes to read before aborting.</param>
		/// <param name="timeoutMilliseconds">If greater than 0, the operation will time out if no progress is made for this many milliseconds.  Upon timeout, <see cref="OperationCanceledException"/> will be thrown.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns>An object containing the result of the operation.</returns>
		/// <exception cref="TimeoutException">If the read operation times out.</exception>
		public static async Task<ReadToEndResult> ReadToEndWithMaxLengthAsync(Stream stream, int maxLength, int timeoutMilliseconds, CancellationToken cancellationToken = default)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			if (maxLength < 0)
				throw new ArgumentOutOfRangeException(nameof(maxLength));

			if (stream is MemoryStream && (stream as MemoryStream).Position == 0)
			{
				if (stream.Length > maxLength)
					return new ReadToEndResult(false, null);
				else
					return new ReadToEndResult(true, ((MemoryStream)stream).ToArray());
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
						read = await ReadAsyncWithTimeout(stream, buf, 0, buf.Length, timeoutMilliseconds, cancellationToken).ConfigureAwait(false);
						if (read > 0)
							memoryStream.Write(buf, 0, read);
						totalRead += read;
					}
					if (totalRead > maxLength)
						return new ReadToEndResult(false, null);
					return new ReadToEndResult(true, memoryStream.ToArray());
				}
				finally
				{
					BufferRecycle(buf);
				}
			}
		}
		/// <summary>
		/// Reads all bytes until the end of the stream.  If maxLength is exceeded while reading, the read data is unavailable and the result indicates that <c>EndOfStream</c> is false.
		/// </summary>
		/// <param name="stream">The stream to read data from.</param>
		/// <param name="maxLength">Maximium number of bytes to read before aborting.</param>
		/// <returns>An object containing the result of the operation.</returns>
		public static ReadToEndResult ReadToEndWithMaxLength(Stream stream, int maxLength)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			if (maxLength < 0)
				throw new ArgumentOutOfRangeException(nameof(maxLength));

			if (stream is MemoryStream && (stream as MemoryStream).Position == 0)
			{
				if (stream.Length > maxLength)
					return new ReadToEndResult(false, null);
				else
					return new ReadToEndResult(true, ((MemoryStream)stream).ToArray());
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
						return new ReadToEndResult(false, null);
					return new ReadToEndResult(true, memoryStream.ToArray());
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
		public class DiscardToEndResult
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
			public DiscardToEndResult(bool endOfStream, long bytesDiscarded)
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
		/// <param name="timeoutMilliseconds">If greater than 0, the operation will time out if no progress is made for this many milliseconds.  Upon timeout, <see cref="OperationCanceledException"/> will be thrown.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns>An object indicating the result of the operation.</returns>
		/// <exception cref="TimeoutException">If the read operation times out.</exception>
		public static async Task<DiscardToEndResult> DiscardUntilEndOfStreamWithMaxLengthAsync(Stream stream, long maxLength, int timeoutMilliseconds, CancellationToken cancellationToken = default)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			if (maxLength < 0)
				throw new ArgumentOutOfRangeException(nameof(maxLength));

			if (stream.CanSeek)
			{
				stream.Seek(0, SeekOrigin.End);
				return new DiscardToEndResult(true, 0);
			}
			byte[] buf = BufferGet();
			try
			{
				long bytesDiscarded = 0;
				int read = 1;
				while (read > 0 && bytesDiscarded < maxLength)
				{
					int toRead = (int)Math.Min(buf.Length, (maxLength + 1) - bytesDiscarded);
					read = await ReadAsyncWithTimeout(stream, buf, 0, toRead, timeoutMilliseconds, cancellationToken).ConfigureAwait(false);
					bytesDiscarded += read;
				}
				return new DiscardToEndResult(read == 0, bytesDiscarded);
			}
			finally
			{
				BufferRecycle(buf);
			}
		}

		/// <summary>
		/// Reads data from the stream in 81920-byte chunks until the end of stream is reached.  The data is discarded as soon as it is read. If the stream supports seeking, it is simply seeked to the end.
		/// </summary>
		/// <param name="stream">The stream to read data from.</param>
		/// <param name="maxLength">If more than this many bytes are read, the method will return false without continuing to the end of the stream.</param>
		/// <returns>An object indicating the result of the operation.</returns>
		public static DiscardToEndResult DiscardUntilEndOfStreamWithMaxLength(Stream stream, long maxLength)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof(stream));
			if (maxLength < 0)
				throw new ArgumentOutOfRangeException(nameof(maxLength));

			if (stream.CanSeek)
			{
				stream.Seek(0, SeekOrigin.End);
				return new DiscardToEndResult(true, 0);
			}
			byte[] buf = BufferGet();
			try
			{
				long bytesDiscarded = 0;
				int read = 1;
				while (read > 0 && bytesDiscarded < maxLength)
				{
					int toRead = (int)Math.Min(buf.Length, (maxLength + 1) - bytesDiscarded);
					read = stream.Read(buf, 0, toRead);
					bytesDiscarded += read;
				}
				return new DiscardToEndResult(read == 0, bytesDiscarded);
			}
			finally
			{
				BufferRecycle(buf);
			}
		}
		/// <summary>
		/// Reads a line of text from a binary input stream.  The text ends when '\n' is encountered or the end of the stream is encountered.  If End of Stream is reached without reading anything, returns null. '\r' characters are counted against <paramref name="maxLength"/> but not included in the returned string.
		/// </summary>
		/// <param name="inputStream">Input stream to read a line of text from.</param>
		/// <param name="maxLength">Maximum line length.  If '\n' is not encountered before the text grows to this many characters, an exception is thrown.</param>
		/// <returns>A line of text read from a binary input stream</returns>
		/// <exception cref="SimpleHttp.HttpProcessor.HttpProcessorException">Throws if the line length reaches the limit before the line ends.</exception>
		public static string HttpStreamReadLine(Stream inputStream, int maxLength = 16384)
		{
			int charsConsumed = 0;
			int next_char;
			bool endOfStream = false;
			bool didRead = false;
			StringBuilder data = new StringBuilder();
			while (true)
			{
				next_char = inputStream.ReadByte();
				if (next_char == -1)
				{
					endOfStream = true;
					break;
				}
				didRead = true;
				if (next_char == '\n')
					break;
				charsConsumed++;
				if (charsConsumed > maxLength)
					throw new SimpleHttp.HttpProcessor.HttpProcessorException("413 Entity Too Large");
				if (next_char != '\r')
					data.Append(Convert.ToChar(next_char));
			}
			if (endOfStream && !didRead)
				return null;
			return data.ToString();
		}
		/// <summary>
		/// Asynchronously reads a line of text from a binary input stream.  The text ends when '\n' is encountered or the end of the stream is encountered.  Returns null if End of Stream is reached without reading anything. '\r' characters are counted against <paramref name="maxLength"/> but not included in the returned string.
		/// </summary>
		/// <param name="stream">Input stream to read a line of text from.</param>
		/// <param name="timeoutMilliseconds">If greater than 0, the operation will time out if no progress is made for this many milliseconds.  Upon timeout, <see cref="OperationCanceledException"/> will be thrown.</param>
		/// <param name="maxLength">Maximum line length.  If '\n' is not encountered before the text grows to this many characters, an exception is thrown.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns>The read line, or null if end of stream reached before any data was read.</returns>
		/// <exception cref="SimpleHttp.HttpProcessor.HttpProcessorException">If the line of text is longer than <paramref name="maxLength"/>.</exception>
		/// <exception cref="OperationCanceledException">If the timeout is exceeded during any ReadAsync operation.</exception>
		/// <exception cref="TimeoutException">If the read operation times out.</exception>
		public static async Task<string> HttpStreamReadLineAsync(UnreadableStream stream, int timeoutMilliseconds, int maxLength = 16384, CancellationToken cancellationToken = default)
		{
			StringBuilder sb = new StringBuilder();
			byte[] buffer = ByteUtil.BufferGet();
			try
			{
				int charsConsumed = 0;
				int read = 1;
				bool didRead = false;
				while (read > 0)
				{
					read = await ReadAsyncWithTimeout(stream, buffer, 0, buffer.Length, timeoutMilliseconds, cancellationToken).ConfigureAwait(false);
					if (read > 0)
					{
						didRead = true;
						for (int i = 0; i < read; i++)
						{
							if (buffer[i] == '\n')
							{
								i++;
								stream.Unread(ByteUtil.SubArray(buffer, i, read - i));
								return sb.ToString();
							}
							charsConsumed++;
							if (charsConsumed > maxLength)
								throw new SimpleHttp.HttpProcessor.HttpProcessorException("413 Entity Too Large");
							if (buffer[i] != '\r')
								sb.Append(Convert.ToChar(buffer[i]));
						}
					}
				}
				if (read == 0 && !didRead)
					return null; // end of stream
				return sb.ToString();
			}
			finally
			{
				ByteUtil.BufferRecycle(buffer);
			}
		}
		/// <summary>
		/// <para>Asynchronously reads a line of text from the stream, then throws <see cref="SimpleHttp.HttpProcessor.HttpProcessorException" /> if the line turned out to be longer than <paramref name="maxLength"/>.</para>
		/// <para>This method reads the entire line into memory before checking the length, so it could be used maliciously to waste large amounts of memory.  For untrusted input, use <see cref="HttpStreamReadLineAsync(UnreadableStream, int, int, CancellationToken)"/> instead.</para>
		/// </summary>
		/// <param name="reader">StreamReader to read from.</param>
		/// <param name="maxLength">Maximum line length in characters.</param>
		/// <returns>Line read from the StreamReader, or null if end of stream reached.</returns>
		/// <exception cref="SimpleHttp.HttpProcessor.HttpProcessorException">If the line of text is longer than <paramref name="maxLength"/>.</exception>
		public static async Task<string> RiskyHttpStreamReadLineAsync(StreamReader reader, int maxLength = 16384)
		{
			string str = await reader.ReadLineAsync().ConfigureAwait(false);
			if (str == null)
				return null;
			if (str.Length >= maxLength)
				throw new SimpleHttp.HttpProcessor.HttpProcessorException("413 Entity Too Large");
			return str;
		}
		/// <summary>
		/// Returns a Task that reads data from the given stream into a buffer with a timeout.  You should await the task.
		/// </summary>
		/// <param name="stream">Stream to read data from.</param>
		/// <param name="buffer">Buffer to read data into.</param>
		/// <param name="offset">Offset into the buffer where data should begin to be written.</param>
		/// <param name="length">Number of bytes to attempt to read.</param>
		/// <param name="timeoutMilliseconds">If greater than 0, the operation will be cancelled if it does not complete within this many milliseconds.  Upon timeout, <see cref="OperationCanceledException"/> will be thrown.</param>
		/// <param name="cancellationToken">Cancellation Token.  A time-based cancellation token will be linked with this one such that the operation is canceled if the given token is canceled or if the time-based token is canceled because time ran out.</param>
		/// <returns>The number of bytes read from the stream.</returns>
		/// <exception cref="OperationCanceledException">If cancellation was requested.</exception>
		/// <exception cref="TimeoutException">If the read operation times out.</exception>
		public static async Task<int> ReadAsyncWithTimeout(Stream stream, byte[] buffer, int offset, int length, int timeoutMilliseconds, CancellationToken cancellationToken = default)
		{
			if (timeoutMilliseconds <= 0)
			{
				return await stream.ReadAsync(buffer, offset, length, cancellationToken).ConfigureAwait(false);
			}
			else
			{
				using (CancellationTokenSource ctsTimeout = new CancellationTokenSource(timeoutMilliseconds))
				using (CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, ctsTimeout.Token))
				{
					try
					{
						return await stream.ReadAsync(buffer, offset, length, cts.Token).ConfigureAwait(false);
					}
					catch (OperationCanceledException ex)
					{
						if (!cancellationToken.IsCancellationRequested)
							throw new TimeoutException("The read operation timed out.", ex);
						ex.Rethrow();
						throw; // Will not be hit; but is necessary for the compiler to know that we won't fall through without throwing.
					}
				}
			}
		}
		/// <summary>
		/// Reads a signed byte from the specified stream.
		/// </summary>
		/// <param name="stream">Stream to read from.</param>
		/// <returns>The read signed byte.</returns>
		public static sbyte ReadSByte(Stream stream)
		{
			return (sbyte)ReadNBytes(stream, 1)[0];
		}
		#endregion
		#region ReadNBytes
		/// <summary>
		/// Reads a specific number of bytes from the stream, returning a byte array.  Ordinary stream.Read operations are not guaranteed to read all the requested bytes.
		/// </summary>
		/// <param name="s">The stream to read from.</param>
		/// <param name="n">The number of bytes to read.</param>
		/// <returns>A new byte array of length <paramref name="n"/> containing the read bytes.</returns>
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
		/// <exception cref="EndOfStreamException">Thrown if the stream ends before the buffer is completely filled.</exception>
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
		/// <returns>A new byte array of length <paramref name="n"/> containing the read bytes.</returns>
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
		/// <exception cref="EndOfStreamException">Thrown if the stream ends before the buffer is completely filled.</exception>
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
		/// <returns>The read bytes with order reversed on little-endian systems so they represent network (big-endian) order on the host.</returns>
		public static byte[] ReadNBytesFromNetworkOrder(Stream s, int n)
		{
			return NetworkToHostOrder(ReadNBytes(s, n));
		}
		/// <summary>
		/// If the current system is Little Endian, reverses the order of the bytes.
		/// If the current system is Big Endian, the array is returned unmodified.
		/// </summary>
		/// <param name="buf">The byte array.</param>
		/// <returns>The possibly reversed array. Note: reversal is performed in-place on the input array instance.</returns>
		public static byte[] NetworkToHostOrder(byte[] buf)
		{
			if (BitConverter.IsLittleEndian)
				Array.Reverse(buf);
			return buf;
		}
		/// <summary>
		/// Returns a new array containing the specified bytes from the source array.  This overload copies all bytes after the specified offset.
		/// </summary>
		/// <param name="buf">The source byte array.</param>
		/// <param name="offset">The offset to begin copying bytes at.</param>
		/// <returns>A new byte array containing bytes from <paramref name="offset"/> to the end of <paramref name="buf"/>.</returns>
		public static byte[] SubArray(byte[] buf, int offset)
		{
			return SubArray(buf, offset, buf.Length - offset);
		}
		/// <summary>
		/// Returns a new array containing the specified bytes from the source array.
		/// </summary>
		/// <param name="buf">The source byte array.</param>
		/// <param name="offset">The offset to begin copying bytes at.</param>
		/// <param name="length">The number of bytes to copy.</param>
		/// <returns>A new byte array containing the requested range of bytes.</returns>
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
		/// <returns>A new byte array containing the copied bytes, reversed if running on a little-endian host.</returns>
		public static byte[] NetworkToHostOrder(byte[] buf, int offset, int length)
		{
			return NetworkToHostOrder(SubArray(buf, offset, length));
		}
		#endregion
		#region Write to byte array (Big endian in the buffer)
		/// <summary>
		/// Writes a 16-bit signed integer to the buffer in big endian format.
		/// </summary>
		/// <param name="num">The value to write.</param>
		/// <param name="buffer">The buffer to write to.</param>
		/// <param name="offset">The offset in the buffer to begin writing at.</param>
		public static void WriteInt16(short num, byte[] buffer, int offset)
		{
			Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(num)), 0, buffer, offset, 2);
		}
		/// <summary>
		/// Writes a 16-bit unsigned integer to the buffer in big endian format.
		/// </summary>
		/// <param name="num">The value to write.</param>
		/// <param name="buffer">The buffer to write to.</param>
		/// <param name="offset">The offset in the buffer to begin writing at.</param>
		public static void WriteUInt16(ushort num, byte[] buffer, int offset)
		{
			Array.Copy(BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder((short)num)), 0, buffer, offset, 2);
		}
		/// <summary>
		/// Writes a 32-bit signed integer to the buffer in big endian format.
		/// </summary>
		/// <param name="num">The value to write.</param>
		/// <param name="buffer">The buffer to write to.</param>
		/// <param name="offset">The offset in the buffer to begin writing at.</param>
		public static void WriteInt32(int num, byte[] buffer, int offset)
		{
			Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(num)), 0, buffer, offset, 4);
		}
		/// <summary>
		/// Writes a 32-bit unsigned integer to the buffer in big endian format.
		/// </summary>
		/// <param name="num">The value to write.</param>
		/// <param name="buffer">The buffer to write to.</param>
		/// <param name="offset">The offset in the buffer to begin writing at.</param>
		public static void WriteUInt32(uint num, byte[] buffer, int offset)
		{
			Array.Copy(BitConverter.GetBytes((uint)IPAddress.HostToNetworkOrder((int)num)), 0, buffer, offset, 4);
		}
		/// <summary>
		/// Writes a 64-bit signed integer to the buffer in big endian format.
		/// </summary>
		/// <param name="num">The value to write.</param>
		/// <param name="buffer">The buffer to write to.</param>
		/// <param name="offset">The offset in the buffer to begin writing at.</param>
		public static void WriteInt64(long num, byte[] buffer, int offset)
		{
			Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(num)), 0, buffer, offset, 8);
		}
		/// <summary>
		/// Writes a 64-bit unsigned integer to the buffer in big endian format.
		/// </summary>
		/// <param name="num">The value to write.</param>
		/// <param name="buffer">The buffer to write to.</param>
		/// <param name="offset">The offset in the buffer to begin writing at.</param>
		public static void WriteUInt64(ulong num, byte[] buffer, int offset)
		{
			Array.Copy(BitConverter.GetBytes((ulong)IPAddress.HostToNetworkOrder((long)num)), 0, buffer, offset, 8);
		}
		/// <summary>
		/// Writes a 32-bit floating point number to the buffer in big endian format.
		/// </summary>
		/// <param name="num">The value to write.</param>
		/// <param name="buffer">The buffer to write to.</param>
		/// <param name="offset">The offset in the buffer to begin writing at.</param>
		public static void WriteFloat(float num, byte[] buffer, int offset)
		{
			Array.Copy(NetworkToHostOrder(BitConverter.GetBytes(num)), 0, buffer, offset, 4);
		}
		/// <summary>
		/// Writes a 64-bit floating point number to the buffer in big endian format.
		/// </summary>
		/// <param name="num">The value to write.</param>
		/// <param name="buffer">The buffer to write to.</param>
		/// <param name="offset">The offset in the buffer to begin writing at.</param>
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
		/// <returns>The number of bytes written to the buffer (UTF8 encoded length).</returns>
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
		/// <returns>The UTF8 encoded byte length of the written string.</returns>
		public static ushort WriteUtf8_16(string str, byte[] buffer, int offset)
		{
			if (str.Length > ushort.MaxValue)
				throw new ArgumentException("WriteUtf8_16 method cannot accept a string with length greater than " + ushort.MaxValue, "str");
			int maxLength = buffer.Length - (offset + 2);
			if (str.Length > maxLength)
				throw new ArgumentException("WriteUtf8_16(string, byte[], int) method received a string that is too large for the buffer it would be written to.");
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
		/// <returns>The UTF8 encoded byte length of the written string.</returns>
		public static uint WriteUtf8_32(string str, byte[] buffer, int offset)
		{
			int maxLength = buffer.Length - (offset + 4);
			if (str.Length > maxLength)
				throw new ArgumentException("WriteUtf8_32(string, byte[], int) method received a string that is too large for the buffer it would be written to.");
			byte[] bytes = Utf8NoBOM.GetBytes(str);
			if (bytes.Length > maxLength)
				throw new ArgumentException("WriteUtf8_32(string, byte[], int) method received a string that is too large for the buffer it would be written to, once UTF8-encoded.");
			WriteUInt32((uint)bytes.Length, buffer, offset);
			Array.Copy(bytes, 0, buffer, offset + 4, bytes.Length);
			return (uint)bytes.Length;
		}
		#endregion
		#region Write to byte array (Little endian in the buffer)
		/// <summary>
		/// Writes a 16-bit signed integer to the buffer in little endian format.
		/// </summary>
		/// <param name="num">The value to write.</param>
		/// <param name="buffer">The buffer to write to.</param>
		/// <param name="offset">The offset in the buffer to begin writing at.</param>
		public static void WriteInt16LE(short num, byte[] buffer, int offset)
		{
			Array.Copy(BitConverter.GetBytes(num), 0, buffer, offset, 2);
		}
		/// <summary>
		/// Writes a 16-bit unsigned integer to the buffer in little endian format.
		/// </summary>
		/// <param name="num">The value to write.</param>
		/// <param name="buffer">The buffer to write to.</param>
		/// <param name="offset">The offset in the buffer to begin writing at.</param>
		public static void WriteUInt16LE(ushort num, byte[] buffer, int offset)
		{
			Array.Copy(BitConverter.GetBytes(num), 0, buffer, offset, 2);
		}
		/// <summary>
		/// Writes a 32-bit signed integer to the buffer in little endian format.
		/// </summary>
		/// <param name="num">The value to write.</param>
		/// <param name="buffer">The buffer to write to.</param>
		/// <param name="offset">The offset in the buffer to begin writing at.</param>
		public static void WriteInt32LE(int num, byte[] buffer, int offset)
		{
			Array.Copy(BitConverter.GetBytes(num), 0, buffer, offset, 4);
		}
		/// <summary>
		/// Writes a 32-bit unsigned integer to the buffer in little endian format.
		/// </summary>
		/// <param name="num">The value to write.</param>
		/// <param name="buffer">The buffer to write to.</param>
		/// <param name="offset">The offset in the buffer to begin writing at.</param>
		public static void WriteUInt32LE(uint num, byte[] buffer, int offset)
		{
			Array.Copy(BitConverter.GetBytes((int)num), 0, buffer, offset, 4);
		}
		/// <summary>
		/// Writes a 64-bit signed integer to the buffer in little endian format.
		/// </summary>
		/// <param name="num">The value to write.</param>
		/// <param name="buffer">The buffer to write to.</param>
		/// <param name="offset">The offset in the buffer to begin writing at.</param>
		public static void WriteInt64LE(long num, byte[] buffer, int offset)
		{
			Array.Copy(BitConverter.GetBytes(num), 0, buffer, offset, 8);
		}
		/// <summary>
		/// Writes a 64-bit unsigned integer to the buffer in little endian format.
		/// </summary>
		/// <param name="num">The value to write.</param>
		/// <param name="buffer">The buffer to write to.</param>
		/// <param name="offset">The offset in the buffer to begin writing at.</param>
		public static void WriteUInt64LE(ulong num, byte[] buffer, int offset)
		{
			Array.Copy(BitConverter.GetBytes(num), 0, buffer, offset, 8);
		}
		/// <summary>
		/// Writes a 32-bit floating point number to the buffer in little endian format.
		/// </summary>
		/// <param name="num">The value to write.</param>
		/// <param name="buffer">The buffer to write to.</param>
		/// <param name="offset">The offset in the buffer to begin writing at.</param>
		public static void WriteFloatLE(float num, byte[] buffer, int offset)
		{
			Array.Copy(BitConverter.GetBytes(num), 0, buffer, offset, 4);
		}
		/// <summary>
		/// Writes a 64-bit floating point number to the buffer in little endian format.
		/// </summary>
		/// <param name="num">The value to write.</param>
		/// <param name="buffer">The buffer to write to.</param>
		/// <param name="offset">The offset in the buffer to begin writing at.</param>
		public static void WriteDoubleLE(double num, byte[] buffer, int offset)
		{
			Array.Copy(BitConverter.GetBytes(num), 0, buffer, offset, 8);
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
		/// <returns>The UTF8 encoded byte length of the written string.</returns>
		public static ushort WriteUtf8_16LE(string str, byte[] buffer, int offset)
		{
			if (str.Length > ushort.MaxValue)
				throw new ArgumentException("WriteUtf8_16LE method cannot accept a string with length greater than " + ushort.MaxValue, "str");
			int maxLength = buffer.Length - (offset + 2);
			if (str.Length > maxLength)
				throw new ArgumentException("WriteUtf8_16LE(string, byte[], int) method received a string that is too large for the buffer it would be written to.");
			byte[] bytes = Utf8NoBOM.GetBytes(str);
			if (bytes.Length > ushort.MaxValue)
				throw new ArgumentException("WriteUtf8_16LE method cannot accept a string with UTF8 length greater than " + ushort.MaxValue, "str");
			if (bytes.Length > maxLength)
				throw new ArgumentException("WriteUtf8(string, byte[], int) method received a string that is too large for the buffer it would be written to, once UTF8-encoded.");
			WriteUInt16LE((ushort)bytes.Length, buffer, offset);
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
		/// <returns>The UTF8 encoded byte length of the written string.</returns>
		public static uint WriteUtf8_32LE(string str, byte[] buffer, int offset)
		{
			int maxLength = buffer.Length - (offset + 4);
			if (str.Length > maxLength)
				throw new ArgumentException("WriteUtf8_32LE(string, byte[], int) method received a string that is too large for the buffer it would be written to.");
			byte[] bytes = Utf8NoBOM.GetBytes(str);
			if (bytes.Length > maxLength)
				throw new ArgumentException("WriteUtf8_32LE(string, byte[], int) method received a string that is too large for the buffer it would be written to, once UTF8-encoded.");
			WriteUInt32LE((uint)bytes.Length, buffer, offset);
			Array.Copy(bytes, 0, buffer, offset + 4, bytes.Length);
			return (uint)bytes.Length;
		}
		#endregion
		#region Write to stream (Big endian in the stream)
		/// <summary>
		/// Writes a 16-bit signed integer to the stream in big endian format.
		/// </summary>
		/// <param name="num">The value to write.</param>
		/// <param name="s">Stream to write to.</param>
		public static void WriteInt16(short num, Stream s)
		{
			s.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(num)), 0, 2);
		}
		/// <summary>
		/// Writes a 16-bit unsigned integer to the stream in big endian format.
		/// </summary>
		/// <param name="num">The value to write.</param>
		/// <param name="s">Stream to write to.</param>
		public static void WriteUInt16(ushort num, Stream s)
		{
			s.Write(BitConverter.GetBytes((ushort)IPAddress.HostToNetworkOrder((short)num)), 0, 2);
		}
		/// <summary>
		/// Writes a 32-bit signed integer to the stream in big endian format.
		/// </summary>
		/// <param name="num">The value to write.</param>
		/// <param name="s">Stream to write to.</param>
		public static void WriteInt32(int num, Stream s)
		{
			s.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(num)), 0, 4);
		}
		/// <summary>
		/// Writes a 32-bit unsigned integer to the stream in big endian format.
		/// </summary>
		/// <param name="num">The value to write.</param>
		/// <param name="s">Stream to write to.</param>
		public static void WriteUInt32(uint num, Stream s)
		{
			s.Write(BitConverter.GetBytes((uint)IPAddress.HostToNetworkOrder((int)num)), 0, 4);
		}
		/// <summary>
		/// Writes a 64-bit signed integer to the stream in big endian format.
		/// </summary>
		/// <param name="num">The value to write.</param>
		/// <param name="s">Stream to write to.</param>
		public static void WriteInt64(long num, Stream s)
		{
			s.Write(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(num)), 0, 8);
		}
		/// <summary>
		/// Writes a 64-bit unsigned integer to the stream in big endian format.
		/// </summary>
		/// <param name="num">The value to write.</param>
		/// <param name="s">Stream to write to.</param>
		public static void WriteUInt64(ulong num, Stream s)
		{
			s.Write(BitConverter.GetBytes((ulong)IPAddress.HostToNetworkOrder((long)num)), 0, 8);
		}
		/// <summary>
		/// Writes a 32-bit floating point number to the stream in big endian format.
		/// </summary>
		/// <param name="num">The value to write.</param>
		/// <param name="s">Stream to write to.</param>
		public static void WriteFloat(float num, Stream s)
		{
			s.Write(NetworkToHostOrder(BitConverter.GetBytes(num)), 0, 4);
		}
		/// <summary>
		/// Writes a 64-bit floating point number to the stream in big endian format.
		/// </summary>
		/// <param name="num">The value to write.</param>
		/// <param name="s">Stream to write to.</param>
		public static void WriteDouble(double num, Stream s)
		{
			s.Write(NetworkToHostOrder(BitConverter.GetBytes(num)), 0, 8);
		}
		/// <summary>
		/// Writes a string to the specified stream. The string will be encoded as UTF8 with no byte order mark. Returns the number of bytes written.
		/// </summary>
		/// <param name="str">String to write.</param>
		/// <param name="s">Stream to write to.</param>
		/// <returns>The number of bytes written to the stream (UTF8 encoded length).</returns>
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
		/// <returns>The UTF8 encoded byte length of the written string.</returns>
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
		/// <returns>The UTF8 encoded byte length of the written string.</returns>
		public static uint WriteUtf8_32(string str, Stream s)
		{
			byte[] bytes = Utf8NoBOM.GetBytes(str);
			WriteUInt32((uint)bytes.Length, s);
			s.Write(bytes, 0, bytes.Length);
			return (uint)bytes.Length;
		}
		#endregion
		#region Read from byte array (Big endian in the buffer)
		/// <summary>
		/// Reads a 16-bit signed integer from the buffer in big endian format.
		/// </summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Offset to begin reading in the buffer.</param>
		/// <returns>The read signed 16-bit integer value.</returns>
		public static short ReadInt16(byte[] buffer, int offset)
		{
			return IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, offset));
		}
		/// <summary>
		/// Reads a 16-bit unsigned integer from the buffer in big endian format.
		/// </summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Offset to begin reading in the buffer.</param>
		/// <returns>The read unsigned 16-bit integer value.</returns>
		public static ushort ReadUInt16(byte[] buffer, int offset)
		{
			return (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(buffer, offset));
		}
		/// <summary>
		/// Reads a 32-bit signed integer from the buffer in big endian format.
		/// </summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Offset to begin reading in the buffer.</param>
		/// <returns>The read signed 32-bit integer value.</returns>
		public static int ReadInt32(byte[] buffer, int offset)
		{
			return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, offset));
		}
		/// <summary>
		/// Reads a 32-bit unsigned integer from the buffer in big endian format.
		/// </summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Offset to begin reading in the buffer.</param>
		/// <returns>The read unsigned 32-bit integer value.</returns>
		public static uint ReadUInt32(byte[] buffer, int offset)
		{
			return (uint)IPAddress.NetworkToHostOrder((int)BitConverter.ToUInt32(buffer, offset));
		}
		/// <summary>
		/// Reads a 64-bit signed integer from the buffer in big endian format.
		/// </summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Offset to begin reading in the buffer.</param>
		/// <returns>The read signed 64-bit integer value.</returns>
		public static long ReadInt64(byte[] buffer, int offset)
		{
			return IPAddress.NetworkToHostOrder(BitConverter.ToInt64(buffer, offset));
		}
		/// <summary>
		/// Reads a 64-bit unsigned integer from the buffer in big endian format.
		/// </summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Offset to begin reading in the buffer.</param>
		/// <returns>The read unsigned 64-bit integer value.</returns>
		public static ulong ReadUInt64(byte[] buffer, int offset)
		{
			return (ulong)IPAddress.NetworkToHostOrder((long)BitConverter.ToUInt64(buffer, offset));
		}
		/// <summary>
		/// Reads a 2-byte floating point number (binary16) in big endian format where the most significant bit is the sign, then comes 5 exponent bits, then 10 fraction bits.
		/// </summary>
		/// <param name="buffer">Buffer to read the 2-byte floating point number from.</param>
		/// <param name="offset">Offset to begin reading in the buffer.</param>
		/// <returns>The converted 32-bit float value.</returns>
		public static float ReadHalf(byte[] buffer, int offset)
		{
			return DecodeHalf(ReadUInt16(buffer, offset));
		}
		/// <summary>
		/// Reads a 32-bit floating point number from the buffer in big endian format.
		/// </summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Offset to begin reading in the buffer.</param>
		/// <returns>The read 32-bit float value.</returns>
		public static float ReadFloat(byte[] buffer, int offset)
		{
			return BitConverter.ToSingle(NetworkToHostOrder(buffer, offset, 4), 0);
		}
		/// <summary>
		/// Reads a 64-bit floating point number from the buffer in big endian format.
		/// </summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Offset to begin reading in the buffer.</param>
		/// <returns>The read 64-bit double value.</returns>
		public static double ReadDouble(byte[] buffer, int offset)
		{
			return BitConverter.ToDouble(NetworkToHostOrder(buffer, offset, 8), 0);
		}
		/// <summary>
		/// Converts all data from the buffer to a string assuming UTF8 encoding with no byte order mark.
		/// </summary>
		/// <param name="buffer">The buffer to convert.</param>
		/// <returns>The UTF8-decoded string.</returns>
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
		/// <returns>The UTF8-decoded string of the specified bytes.</returns>
		/// <exception cref="ArgumentException">Thrown if offset or byteLength specify an invalid range in the buffer.</exception>
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
		/// <returns>The decoded string.</returns>
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
		/// <returns>The decoded string.</returns>
		/// <exception cref="ArgumentException">Thrown if offset is invalid or there are not enough bytes to read the length or string.</exception>
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
		/// <returns>The decoded string.</returns>
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
		/// <returns>The decoded string.</returns>
		/// <exception cref="ArgumentException">Thrown if offset is invalid or there are not enough bytes to read the length or string.</exception>
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
		#region Read from byte array (Little endian in the buffer)
		/// <summary>
		/// Reads a 16-bit signed integer from the buffer in little endian format.
		/// </summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Offset to begin reading in the buffer.</param>
		/// <returns>The read signed 16-bit integer value.</returns>
		public static short ReadInt16LE(byte[] buffer, int offset)
		{
			return BitConverter.ToInt16(buffer, offset);
		}
		/// <summary>
		/// Reads a 16-bit unsigned integer from the buffer in little endian format.
		/// </summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Offset to begin reading in the buffer.</param>
		/// <returns>The read unsigned 16-bit integer value.</returns>
		public static ushort ReadUInt16LE(byte[] buffer, int offset)
		{
			return BitConverter.ToUInt16(buffer, offset);
		}
		/// <summary>
		/// Reads a 32-bit signed integer from the buffer in little endian format.
		/// </summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Offset to begin reading in the buffer.</param>
		/// <returns>The read signed 32-bit integer value.</returns>
		public static int ReadInt32LE(byte[] buffer, int offset)
		{
			return BitConverter.ToInt32(buffer, offset);
		}
		/// <summary>
		/// Reads a 32-bit unsigned integer from the buffer in little endian format.
		/// </summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Offset to begin reading in the buffer.</param>
		/// <returns>The read unsigned 32-bit integer value.</returns>
		public static uint ReadUInt32LE(byte[] buffer, int offset)
		{
			return BitConverter.ToUInt32(buffer, offset);
		}
		/// <summary>
		/// Reads a 64-bit signed integer from the buffer in little endian format.
		/// </summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Offset to begin reading in the buffer.</param>
		/// <returns>The read signed 64-bit integer value.</returns>
		public static long ReadInt64LE(byte[] buffer, int offset)
		{
			return BitConverter.ToInt64(buffer, offset);
		}
		/// <summary>
		/// Reads a 64-bit unsigned integer from the buffer in little endian format.
		/// </summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Offset to begin reading in the buffer.</param>
		/// <returns>The read unsigned 64-bit integer value.</returns>
		public static ulong ReadUInt64LE(byte[] buffer, int offset)
		{
			return BitConverter.ToUInt64(buffer, offset);
		}
		/// <summary>
		/// Reads a 2-byte floating point number (binary16) in little endian format where the most significant bit is the sign, then comes 5 exponent bits, then 10 fraction bits.
		/// </summary>
		/// <param name="buffer">Buffer to read the 2-byte floating point number from.</param>
		/// <param name="offset">Offset to begin reading in the buffer.</param>
		/// <returns>The converted 32-bit float value.</returns>
		public static float ReadHalfLE(byte[] buffer, int offset)
		{
			return DecodeHalf(ReadUInt16LE(buffer, offset));
		}
		/// <summary>
		/// Reads a 32-bit floating point number from the buffer in little endian format.
		/// </summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Offset to begin reading in the buffer.</param>
		/// <returns>The read 32-bit float value.</returns>
		public static float ReadFloatLE(byte[] buffer, int offset)
		{
			return BitConverter.ToSingle(buffer, offset);
		}
		/// <summary>
		/// Reads a 64-bit floating point number from the buffer in little endian format.
		/// </summary>
		/// <param name="buffer">Buffer to read from.</param>
		/// <param name="offset">Offset to begin reading in the buffer.</param>
		/// <returns>The read 64-bit double value.</returns>
		public static double ReadDoubleLE(byte[] buffer, int offset)
		{
			return BitConverter.ToDouble(buffer, offset);
		}
		#endregion
		#region Read from stream (Big endian on the stream)
		/// <summary>
		/// Reads a 16-bit signed integer from the stream in big endian format.
		/// </summary>
		/// <param name="s">Stream to read from.</param>
		/// <returns>The read signed 16-bit integer value.</returns>
		public static short ReadInt16(Stream s)
		{
			return IPAddress.NetworkToHostOrder(BitConverter.ToInt16(ReadNBytes(s, 2), 0));
		}
		/// <summary>
		/// Reads a 16-bit unsigned integer from the stream in big endian format.
		/// </summary>
		/// <param name="s">Stream to read from.</param>
		/// <returns>The read unsigned 16-bit integer value.</returns>
		public static ushort ReadUInt16(Stream s)
		{
			return (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(ReadNBytes(s, 2), 0));
		}
		/// <summary>
		/// Reads a 32-bit signed integer from the stream in big endian format.
		/// </summary>
		/// <param name="s">Stream to read from.</param>
		/// <returns>The read signed 32-bit integer value.</returns>
		public static int ReadInt32(Stream s)
		{
			return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(ReadNBytes(s, 4), 0));
		}
		/// <summary>
		/// Reads a 32-bit unsigned integer from the stream in big endian format.
		/// </summary>
		/// <param name="s">Stream to read from.</param>
		/// <returns>The read unsigned 32-bit integer value.</returns>
		public static uint ReadUInt32(Stream s)
		{
			return (uint)IPAddress.NetworkToHostOrder((int)BitConverter.ToUInt32(ReadNBytes(s, 4), 0));
		}
		/// <summary>
		/// Reads a 64-bit signed integer from the stream in big endian format.
		/// </summary>
		/// <param name="s">Stream to read from.</param>
		/// <returns>The read signed 64-bit integer value.</returns>
		public static long ReadInt64(Stream s)
		{
			return IPAddress.NetworkToHostOrder(BitConverter.ToInt64(ReadNBytes(s, 8), 0));
		}
		/// <summary>
		/// Reads a 64-bit unsigned integer from the stream in big endian format.
		/// </summary>
		/// <param name="s">Stream to read from.</param>
		/// <returns>The read unsigned 64-bit integer value.</returns>
		public static ulong ReadUInt64(Stream s)
		{
			return (ulong)IPAddress.NetworkToHostOrder((long)BitConverter.ToUInt64(ReadNBytes(s, 8), 0));
		}
		/// <summary>
		/// Reads a 2-byte floating point number (binary16) in big endian format where the most significant bit is the sign, then comes 5 exponent bits, then 10 fraction bits.
		/// </summary>
		/// <param name="s">Stream to read the 2-byte floating point number from.</param>
		/// <returns>The converted 32-bit float value.</returns>
		public static float ReadHalf(Stream s)
		{
			return DecodeHalf(ReadUInt16(s));
		}
		/// <summary>
		/// Reads a 32-bit floating point number from the stream in big endian format.
		/// </summary>
		/// <param name="s">Stream to read from.</param>
		/// <returns>The read 32-bit float value.</returns>
		public static float ReadFloat(Stream s)
		{
			return BitConverter.ToSingle(ReadNBytesFromNetworkOrder(s, 4), 0);
		}
		/// <summary>
		/// Reads a 64-bit floating point number from the stream in big endian format.
		/// </summary>
		/// <param name="s">Stream to read from.</param>
		/// <returns>The read 64-bit double value.</returns>
		public static double ReadDouble(Stream s)
		{
			return BitConverter.ToDouble(ReadNBytesFromNetworkOrder(s, 8), 0);
		}
		/// <summary>
		/// Reads the specified number of bytes from the stream and converts them to a string assuming UTF8 encoding with no byte order mark.
		/// </summary>
		/// <param name="s">The stream to read from.</param>
		/// <param name="byteLength">The number of bytes to read.</param>
		/// <returns>The decoded UTF8 string.</returns>
		public static string ReadUtf8(Stream s, int byteLength)
		{
			return Utf8NoBOM.GetString(ReadNBytes(s, byteLength), 0, byteLength);
		}
		/// <summary>
		/// Reads a UTF8 string (no byte order mark) from the stream, assuming the string's length is prepended as a 16 bit unsigned integer.
		/// </summary>
		/// <param name="s">The stream to read from.</param>
		/// <returns>The decoded string.</returns>
		public static string ReadUtf8_16(Stream s)
		{
			int byteLength = ReadUInt16(s);
			return Utf8NoBOM.GetString(ReadNBytes(s, byteLength), 0, byteLength);
		}
		/// <summary>
		/// Reads a UTF8 string (no byte order mark) from the stream, assuming the string's length is prepended as a 32 bit unsigned integer.
		/// </summary>
		/// <param name="s">The stream to read from.</param>
		/// <returns>The decoded string.</returns>
		public static string ReadUtf8_32(Stream s)
		{
			int byteLength = (int)ReadUInt32(s);
			return Utf8NoBOM.GetString(ReadNBytes(s, byteLength), 0, byteLength);
		}
		#endregion
		#region Read from stream (Little endian on the stream)
		/// <summary>
		/// Reads a 16-bit signed integer from the stream in little endian format.
		/// </summary>
		/// <param name="s">Stream to read from.</param>
		/// <returns>The read signed 16-bit integer value.</returns>
		public static short ReadInt16LE(Stream s)
		{
			if (BitConverter.IsLittleEndian)
				return BitConverter.ToInt16(ReadNBytes(s, 2), 0);
			return ReadInt16(s);
		}
		/// <summary>
		/// Reads a 16-bit unsigned integer from the stream in little endian format.
		/// </summary>
		/// <param name="s">Stream to read from.</param>
		/// <returns>The read unsigned 16-bit integer value.</returns>
		public static ushort ReadUInt16LE(Stream s)
		{
			if (BitConverter.IsLittleEndian)
				return BitConverter.ToUInt16(ReadNBytes(s, 2), 0);
			return ReadUInt16(s);
		}
		/// <summary>
		/// Reads a 32-bit signed integer from the stream in little endian format.
		/// </summary>
		/// <param name="s">Stream to read from.</param>
		/// <returns>The read signed 32-bit integer value.</returns>
		public static int ReadInt32LE(Stream s)
		{
			if (BitConverter.IsLittleEndian)
				return BitConverter.ToInt32(ReadNBytes(s, 4), 0);
			return ReadInt32(s);
		}
		/// <summary>
		/// Reads a 32-bit unsigned integer from the stream in little endian format.
		/// </summary>
		/// <param name="s">Stream to read from.</param>
		/// <returns>The read unsigned 32-bit integer value.</returns>
		public static uint ReadUInt32LE(Stream s)
		{
			if (BitConverter.IsLittleEndian)
				return BitConverter.ToUInt32(ReadNBytes(s, 4), 0);
			return ReadUInt32(s);
		}
		/// <summary>
		/// Reads a 64-bit signed integer from the stream in little endian format.
		/// </summary>
		/// <param name="s">Stream to read from.</param>
		/// <returns>The read signed 64-bit integer value.</returns>
		public static long ReadInt64LE(Stream s)
		{
			if (BitConverter.IsLittleEndian)
				return BitConverter.ToInt64(ReadNBytes(s, 8), 0);
			return ReadInt64(s);
		}
		/// <summary>
		/// Reads a 64-bit unsigned integer from the stream in little endian format.
		/// </summary>
		/// <param name="s">Stream to read from.</param>
		/// <returns>The read unsigned 64-bit integer value.</returns>
		public static ulong ReadUInt64LE(Stream s)
		{
			if (BitConverter.IsLittleEndian)
				return BitConverter.ToUInt64(ReadNBytes(s, 8), 0);
			return ReadUInt64(s);
		}
		/// <summary>
		/// Reads a 2-byte floating point number (binary16) in little endian format where the most significant bit is the sign, then comes 5 exponent bits, then 10 fraction bits.
		/// </summary>
		/// <param name="s">Stream to read the 2-byte floating point number from.</param>
		/// <returns>The converted 32-bit float value.</returns>
		public static float ReadHalfLE(Stream s)
		{
			return DecodeHalf(ReadUInt16LE(s));
		}
		/// <summary>
		/// Reads a 32-bit floating point number from the stream in little endian format.
		/// </summary>
		/// <param name="s">Stream to read from.</param>
		/// <returns>The read 32-bit float value.</returns>
		public static float ReadFloatLE(Stream s)
		{
			if (BitConverter.IsLittleEndian)
				return BitConverter.ToSingle(ReadNBytes(s, 4), 0);
			return ReadFloat(s);
		}
		/// <summary>
		/// Reads a 64-bit floating point number from the stream in little endian format.
		/// </summary>
		/// <param name="s">Stream to read from.</param>
		/// <returns>The read 64-bit double value.</returns>
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