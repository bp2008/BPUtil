using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
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
		/// <summary>
		/// Compresses a buffer using GZip.
		/// </summary>
		/// <param name="buffer">Uncompressed data</param>
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
		/// <param name="buffer">Compressed data</param>
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
		/// <param name="buffer">Uncompressed data</param>
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
		/// <param name="buffer">Compressed data</param>
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
		public static async Task AddFileToZipAsync(string zipFilePath, string fileName, byte[] fileBody)
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
							await entryStream.WriteAsync(fileBody, 0, fileBody.Length);
						}
					}
				}
			}, 50, 6);
		}
	}
}
