using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// <para>Provides static methods for compression and decompression.</para>
	/// <para>Supported Methods:</para>
	/// <para>DEFLATE</para>
	/// <para>GZip (Same as DEFLATE but adds a header and checksum)</para>
	/// </summary>
	public static class Compression
	{
#if NET6_0
		/// <summary>
		/// Compresses a buffer using Brotli.
		/// </summary>
		/// <param name="buffer">Uncompressed data.</param>
		/// <returns>Compressed data.</returns>
		public static byte[] BrotliCompress(byte[] buffer)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				using (BrotliStream stream = new BrotliStream(ms, CompressionLevel.Optimal, true))
				{
					stream.Write(buffer, 0, buffer.Length);
				}
				return ms.ToArray();
			}
		}
		/// <summary>
		/// Decompresses a buffer using Brotli.
		/// </summary>
		/// <param name="buffer">Brotli-compressed data.</param>
		/// <returns>Decompressed data.</returns>
		public static byte[] BrotliDecompress(byte[] buffer)
		{
			using (MemoryStream outStream = new MemoryStream())
			{
				using (MemoryStream inStream = new MemoryStream(buffer))
				{
					using (BrotliStream stream = new BrotliStream(inStream, CompressionMode.Decompress, true))
					{
						stream.CopyTo(outStream);
					}
				}
				return outStream.ToArray();
			}
		}
#endif
		/// <summary>
		/// Compresses a buffer using GZip.
		/// </summary>
		/// <param name="buffer">Uncompressed data.</param>
		/// <returns>Compressed data.</returns>
		public static byte[] GZipCompress(byte[] buffer)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				using (GZipStream gZipStream = new GZipStream(ms, CompressionLevel.Optimal, true))
				{
					gZipStream.Write(buffer, 0, buffer.Length);
				}
				return ms.ToArray();
			}
		}
		/// <summary>
		/// Decompresses a buffer using GZip.
		/// </summary>
		/// <param name="buffer">GZip-compressed data.</param>
		/// <returns>Decompressed data.</returns>
		public static byte[] GZipDecompress(byte[] buffer)
		{
			using (MemoryStream outStream = new MemoryStream())
			{
				using (MemoryStream inStream = new MemoryStream(buffer))
				{
					using (GZipStream gZipStream = new GZipStream(inStream, CompressionMode.Decompress, true))
					{
						gZipStream.CopyTo(outStream);
					}
				}
				return outStream.ToArray();
			}
		}
		/// <summary>
		/// Compresses a buffer using DEFLATE.
		/// </summary>
		/// <param name="buffer">Uncompressed data.</param>
		/// <returns>Compressed data.</returns>
		public static byte[] DeflateCompress(byte[] buffer)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				using (DeflateStream deflateStream = new DeflateStream(ms, CompressionLevel.Optimal, true))
				{
					deflateStream.Write(buffer, 0, buffer.Length);
				}
				return ms.ToArray();
			}
		}
		/// <summary>
		/// Decompresses a buffer using DEFLATE.
		/// </summary>
		/// <param name="buffer">DEFLATE-compressed data.</param>
		/// <returns>Decompressed data.</returns>
		public static byte[] DeflateDecompress(byte[] buffer)
		{
			using (MemoryStream outStream = new MemoryStream())
			{
				using (MemoryStream inStream = new MemoryStream(buffer))
				{
					using (DeflateStream deflateStream = new DeflateStream(inStream, CompressionMode.Decompress, true))
					{
						deflateStream.CopyTo(outStream);
					}
				}
				return outStream.ToArray();
			}
		}

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
	}
}
