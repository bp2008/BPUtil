using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// Enumeration of compression methods.
	/// </summary>
	public enum BPCompressionMethod
	{
#if NET6_0_OR_GREATER
		/// <summary>
		/// The "Brotli" algorithm. Brotli is more advanced than Gzip.
		/// </summary>
		Brotli,
#endif
		/// <summary>
		/// The "Gzip" algorithm.  Gzip is basically just Deflate with an additional header and checksum.
		/// </summary>
		Gzip,
		/// <summary>
		/// The "DEFLATE" algorithm.
		/// </summary>
		Deflate
	}
	/// <summary>
	/// <para>Provides static methods for compression and decompression.</para>
	/// <para>Supported Methods:</para>
	/// <para>DEFLATE</para>
	/// <para>GZip (Same as DEFLATE but adds a header and checksum)</para>
	/// <para>Brotli (more advanced compression common on the web), requires .NET 6.0 or newer build of BPUtil.</para>
	/// </summary>
	public static class Compression
	{
		#region Compression Shared
		/// <summary>
		/// Returns a new stream that compresses data when written to and decompresses data when read from.
		/// </summary>
		/// <param name="compressionMethod">The compression algorithm that determines which type of stream is created.</param>
		/// <param name="baseStream">The stream which will be passed into the compression stream's constructor, for it to be based on.  For Compression methods, this is the output stream.  For Decompression methods, this is the input stream.</param>
		/// <param name="compress">True if this stream will be used for compression.  False if it will be used for decompression.</param>
		/// <returns></returns>
		private static Stream GetCompressionStream(BPCompressionMethod compressionMethod, Stream baseStream, bool compress)
		{
			switch (compressionMethod)
			{
#if NET6_0_OR_GREATER
				case BPCompressionMethod.Brotli:
					if (compress)
						return new BrotliStream(baseStream, CompressionLevel.Optimal, true);
					else
						return new BrotliStream(baseStream, CompressionMode.Decompress, true);
#endif
				case BPCompressionMethod.Gzip:
					if (compress)
						return new GZipStream(baseStream, CompressionLevel.Optimal, true);
					else
						return new GZipStream(baseStream, CompressionMode.Decompress, true);
				case BPCompressionMethod.Deflate:
					if (compress)
						return new DeflateStream(baseStream, CompressionLevel.Optimal, true);
					else
						return new DeflateStream(baseStream, CompressionMode.Decompress, true);
				default:
					throw new Exception("Unsupported BPCompressionMethod: " + compressionMethod);
			}
		}
		#endregion
		#region LEGACY API: Compress using a byte[] as input and byte[] as output
#if NET6_0_OR_GREATER
		/// <summary>
		/// Compresses a buffer using Brotli.
		/// </summary>
		/// <param name="buffer">Uncompressed data.</param>
		/// <returns>Compressed data.</returns>
		public static byte[] BrotliCompress(byte[] buffer)
		{
			return Compress(BPCompressionMethod.Brotli, buffer);
		}
#endif
		/// <summary>
		/// Compresses a buffer using GZip.
		/// </summary>
		/// <param name="buffer">Uncompressed data.</param>
		/// <returns>Compressed data.</returns>
		public static byte[] GZipCompress(byte[] buffer)
		{
			return Compress(BPCompressionMethod.Gzip, buffer);
		}
		/// <summary>
		/// Compresses a buffer using DEFLATE.
		/// </summary>
		/// <param name="buffer">Uncompressed data.</param>
		/// <returns>Compressed data.</returns>
		public static byte[] DeflateCompress(byte[] buffer)
		{
			return Compress(BPCompressionMethod.Deflate, buffer);
		}
		#endregion
		#region LEGACY API: Decompress using a byte[] as input and byte[] as output
#if NET6_0_OR_GREATER
		/// <summary>
		/// Decompresses a buffer using Brotli.
		/// </summary>
		/// <param name="buffer">Brotli-compressed data.</param>
		/// <returns>Decompressed data.</returns>
		public static byte[] BrotliDecompress(byte[] buffer)
		{
			return Decompress(BPCompressionMethod.Brotli, buffer);
		}
#endif
		/// <summary>
		/// Decompresses a buffer using GZip.
		/// </summary>
		/// <param name="buffer">GZip-compressed data.</param>
		/// <returns>Decompressed data.</returns>
		public static byte[] GZipDecompress(byte[] buffer)
		{
			return Decompress(BPCompressionMethod.Gzip, buffer);
		}
		/// <summary>
		/// Decompresses a buffer using DEFLATE.
		/// </summary>
		/// <param name="buffer">DEFLATE-compressed data.</param>
		/// <returns>Decompressed data.</returns>
		public static byte[] DeflateDecompress(byte[] buffer)
		{
			return Decompress(BPCompressionMethod.Deflate, buffer);
		}
		#endregion
		#region Compression Methods (2025+) byte[] Input, byte[] output
		/// <summary>
		/// Compresses a buffer using the specified compression method.
		/// </summary>
		/// <param name="compressionMethod">A <see cref="BPCompressionMethod"/> that determines which algorithm is used.</param>
		/// <param name="buffer">Uncompressed data.</param>
		/// <returns>Compressed data.</returns>
		public static byte[] Compress(BPCompressionMethod compressionMethod, byte[] buffer)
		{
			using (MemoryStream outStream = new MemoryStream())
			{
				using (Stream compressionStream = GetCompressionStream(compressionMethod, outStream,true))
				{
					compressionStream.Write(buffer, 0, buffer.Length);
				}
				return outStream.ToArray();
			}
		}
		/// <summary>
		/// Decompresses a buffer using the specified compression method.
		/// </summary>
		/// <param name="compressionMethod">A <see cref="BPCompressionMethod"/> that determines which algorithm is used.</param>
		/// <param name="buffer">Compressed data.</param>
		/// <returns>Decompressed data.</returns>
		public static byte[] Decompress(BPCompressionMethod compressionMethod, byte[] buffer)
		{
			using (MemoryStream outStream = new MemoryStream())
			{
				using (MemoryStream inStream = new MemoryStream(buffer))
				using (Stream compressionStream = GetCompressionStream(compressionMethod, inStream, false))
				{
					compressionStream.CopyTo(outStream);
				}
				return outStream.ToArray();
			}
		}
		#endregion
		#region Compression Methods (2025+) File Input, byte[] output
		/// <summary>
		/// Compresses a file using the specified compression method.
		/// </summary>
		/// <param name="compressionMethod">A <see cref="BPCompressionMethod"/> that determines which algorithm is used.</param>
		/// <param name="filePathInput">Path of the input file.</param>
		/// <param name="fileShareInput">A constant that determines how the input file will be shared by processes.</param>
		/// <returns>Compressed data.</returns>
		public static byte[] Compress(BPCompressionMethod compressionMethod, string filePathInput, FileShare fileShareInput = FileShare.ReadWrite)
		{
			using (FileStream inStream = new FileStream(filePathInput, FileMode.Open, FileAccess.Read, fileShareInput))
			using (MemoryStream outStream = new MemoryStream())
			{
				using (Stream compressionStream = GetCompressionStream(compressionMethod, outStream, true))
				{
					inStream.CopyTo(compressionStream);
				}
				return outStream.ToArray();
			}
		}
		/// <summary>
		/// Decompresses a file using the specified compression method.
		/// </summary>
		/// <param name="compressionMethod">A <see cref="BPCompressionMethod"/> that determines which algorithm is used.</param>
		/// <param name="filePathInput">Path of the input file.</param>
		/// <param name="fileShareInput">A constant that determines how the input file will be shared by processes.</param>
		/// <returns>Decompressed data.</returns>
		public static byte[] Decompress(BPCompressionMethod compressionMethod, string filePathInput, FileShare fileShareInput = FileShare.ReadWrite)
		{
			using (FileStream inStream = new FileStream(filePathInput, FileMode.Open, FileAccess.Read, fileShareInput))
			using (MemoryStream outStream = new MemoryStream())
			{
				using (Stream compressionStream = GetCompressionStream(compressionMethod, inStream, false))
				{
					compressionStream.CopyTo(outStream);
				}
				return outStream.ToArray();
			}
		}
		#endregion
		#region Compression Methods (2025+) File Input, File output
		/// <summary>
		/// Compresses a file using the specified compression method.
		/// </summary>
		/// <param name="compressionMethod">A <see cref="BPCompressionMethod"/> that determines which algorithm is used.</param>
		/// <param name="filePathInput">Path of the input file.</param>
		/// <param name="filePathOutput">Path of the output file.</param>
		/// <param name="fileShareInput">A constant that determines how the input file will be shared by processes.</param>
		/// <param name="fileShareOutput">A constant that determines how the output file will be shared by processes.</param>
		/// <param name="fileModeOutput">A constant that determines how to operate if the output file already exists or not.</param>
		public static void Compress(BPCompressionMethod compressionMethod, string filePathInput, string filePathOutput, FileShare fileShareInput = FileShare.ReadWrite, FileShare fileShareOutput = FileShare.Read, FileMode fileModeOutput = FileMode.Create)
		{
			using (FileStream inStream = new FileStream(filePathInput, FileMode.Open, FileAccess.Read, fileShareInput))
			using (FileStream outStream = new FileStream(filePathOutput, fileModeOutput, FileAccess.Write, fileShareOutput))
			using (Stream compressionStream = GetCompressionStream(compressionMethod, outStream, true))
			{
				inStream.CopyTo(compressionStream);
			}
		}
		/// <summary>
		/// Decompresses a file using the specified compression method.
		/// </summary>
		/// <param name="compressionMethod">A <see cref="BPCompressionMethod"/> that determines which algorithm is used.</param>
		/// <param name="filePathInput">Path of the input file.</param>
		/// <param name="filePathOutput">Path of the output file.</param>
		/// <param name="fileShareInput">A constant that determines how the input file will be shared by processes.</param>
		/// <param name="fileShareOutput">A constant that determines how the output file will be shared by processes.</param>
		/// <param name="fileModeOutput">A constant that determines how to operate if the output file already exists or not.</param>
		public static void Decompress(BPCompressionMethod compressionMethod, string filePathInput, string filePathOutput, FileShare fileShareInput = FileShare.ReadWrite, FileShare fileShareOutput = FileShare.Read, FileMode fileModeOutput = FileMode.Create)
		{
			using (FileStream inStream = new FileStream(filePathInput, FileMode.Open, FileAccess.Read, fileShareInput))
			using (FileStream outStream = new FileStream(filePathOutput, fileModeOutput, FileAccess.Write, fileShareOutput))
			using (Stream compressionStream = GetCompressionStream(compressionMethod, inStream, false))
			{
				compressionStream.CopyTo(outStream);
			}
		}
		#endregion
		#region Compression Methods (2025+) byte[] Input, File output
		/// <summary>
		/// Compresses a buffer using the specified compression method.
		/// </summary>
		/// <param name="compressionMethod">A <see cref="BPCompressionMethod"/> that determines which algorithm is used.</param>
		/// <param name="buffer">Uncompressed data.</param>
		/// <param name="filePathOutput">Path of the output file.</param>
		/// <param name="fileShareOutput">A constant that determines how the output file will be shared by processes.</param>
		/// <param name="fileModeOutput">A constant that determines how to operate if the output file already exists or not.</param>
		public static void Compress(BPCompressionMethod compressionMethod, byte[] buffer, string filePathOutput, FileShare fileShareOutput = FileShare.Read, FileMode fileModeOutput = FileMode.Create)
		{
			using (FileStream outStream = new FileStream(filePathOutput, fileModeOutput, FileAccess.Write, fileShareOutput))
			using (Stream compressionStream = GetCompressionStream(compressionMethod, outStream, true))
			{
				compressionStream.Write(buffer, 0, buffer.Length);
			}
		}
		/// <summary>
		/// Decompresses a buffer using the specified compression method.
		/// </summary>
		/// <param name="compressionMethod">A <see cref="BPCompressionMethod"/> that determines which algorithm is used.</param>
		/// <param name="buffer">Compressed data.</param>
		/// <param name="filePathOutput">Path of the output file.</param>
		/// <param name="fileShareOutput">A constant that determines how the output file will be shared by processes.</param>
		/// <param name="fileModeOutput">A constant that determines how to operate if the output file already exists or not.</param>
		public static void Decompress(BPCompressionMethod compressionMethod, byte[] buffer, string filePathOutput, FileShare fileShareOutput = FileShare.Read, FileMode fileModeOutput = FileMode.Create)
		{
			using (FileStream outStream = new FileStream(filePathOutput, fileModeOutput, FileAccess.Write, fileShareOutput))
			using (MemoryStream inStream = new MemoryStream(buffer))
			using (Stream compressionStream = GetCompressionStream(compressionMethod, inStream, false))
			{
				compressionStream.CopyTo(outStream);
			}
		}
		#endregion
		#region Zip Files
		/// <summary>
		/// Adds a file to a .zip file, creating the zip file if necessary, otherwise opening and modifying it if it already exists.
		/// </summary>
		/// <param name="zipFilePath">The .zip file path.</param>
		/// <param name="fileName">The file name to add.</param>
		/// <param name="fileBody">The file body to add.</param>
		public static void AddFileToZip(string zipFilePath, string fileName, byte[] fileBody)
		{
			Robust.RetryPeriodic(() =>
			{
				using (FileStream fileStream = new FileStream(zipFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
				{
					using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Update))
					{
						ZipArchiveEntry entry = archive.CreateEntry(fileName);
						using (Stream entryStream = entry.Open())
						{
							entryStream.Write(fileBody, 0, fileBody.Length);
						}
					}
				}
			}, 50, 6);
		}

		/// <summary>
		/// Asynchronously adds a file to a .zip file, creating the zip file if necessary, otherwise opening and modifying it if it already exists.
		/// </summary>
		/// <param name="zipFilePath">The .zip file path.</param>
		/// <param name="fileName">The file name to add.</param>
		/// <param name="fileBody">The file body to add.</param>
		/// /// <param name="cancellationToken">Cancellation Token</param>
		public static async Task AddFileToZipAsync(string zipFilePath, string fileName, byte[] fileBody, CancellationToken cancellationToken = default)
		{
			await Robust.RetryPeriodicAsync(async () =>
			{
				using (FileStream fileStream = new FileStream(zipFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read))
				{
					using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Update))
					{
						ZipArchiveEntry entry = archive.CreateEntry(fileName);
						using (Stream entryStream = entry.Open())
						{
							await entryStream.WriteAsync(fileBody, 0, fileBody.Length, cancellationToken).ConfigureAwait(false);
						}
					}
				}
			}, 50, 6, cancellationToken).ConfigureAwait(false);
		}
		#endregion
	}
}
