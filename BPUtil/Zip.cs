using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	public static class ZipExtensions
	{
		/// <summary>
		/// Creates a new entry in the zip archive using the specified entry name, data, compression level, and last write time.
		/// </summary>
		/// <param name="archive">The zip archive to add the new entry to.</param>
		/// <param name="entryName">The name of the entry to create.</param>
		/// <param name="data">The data to write to the new entry.</param>
		/// <param name="compressionLevel">The compression level to use when creating the new entry.</param>
		/// <param name="lastWriteTime">The last write time to set for the new entry.</param>
		/// <returns>The ZipArchiveEntry that was created.</returns>
		public static ZipArchiveEntry CreateEntryFromByteArray(this ZipArchive archive, string entryName, byte[] data, CompressionLevel compressionLevel, DateTime? lastWriteTime = null)
		{
			ZipArchiveEntry entry = archive.CreateEntry(entryName, compressionLevel);
			using (Stream entryStream = entry.Open())
				entryStream.Write(data, 0, data.Length);
			if (lastWriteTime == null)
				lastWriteTime = DateTime.Now;
			entry.LastWriteTime = lastWriteTime.Value;
			return entry;
		}
		/// <summary>
		/// Extracts the entry to an instance of the ZipArchiveEntryData class.
		/// </summary>
		/// <param name="entry">The entry to extract.</param>
		/// <returns>An instance of the ZipArchiveEntryData class containing the extracted data and last write time of the entry.</returns>
		public static ZipArchiveEntryData ExtractToObject(this ZipArchiveEntry entry)
		{
			return new ZipArchiveEntryData(entry);
		}
		/// <summary>
		/// Extracts the entry to a file.
		/// </summary>
		/// <param name="entry">The entry to extract.</param>
		/// <param name="filePath">The file path.</param>
		/// <param name="overwrite">True to overwrite the file if it already exists.</param>
		public static void ExtractToFile(this ZipArchiveEntry entry, string filePath, bool overwrite = false)
		{
			using (FileStream fs = new FileStream(filePath, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.Read))
			{
				using (Stream entryStream = entry.Open())
					entryStream.CopyTo(fs);
			}
			File.SetLastWriteTime(filePath, entry.LastWriteTime.DateTime);
		}
	}
	/// <summary>
	/// Represents the extracted data and last write time of a ZipArchiveEntry.
	/// </summary>
	public class ZipArchiveEntryData
	{
		/// <summary>
		/// The file name of the ZipArchiveEntry.
		/// </summary>
		public string FileName;
		/// <summary>
		/// The relative path of the ZipArchiveEntry.
		/// </summary>
		public string RelativePath;
		/// <summary>
		/// The extracted data of the ZipArchiveEntry.
		/// </summary>
		public byte[] Data;
		/// <summary>
		/// The last write time of the ZipArchiveEntry.
		/// </summary>
		public DateTime LastWriteTime;
		/// <summary>
		/// Constructs an empty ZipArchiveEntryData.
		/// </summary>
		public ZipArchiveEntryData()
		{
		}
		/// <summary>
		/// Constructs a ZipArchiveEntryData by extracting a ZipArchiveEntry.
		/// </summary>
		/// <param name="entry">The entry to extract.</param>
		public ZipArchiveEntryData(ZipArchiveEntry entry)
		{
			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (Stream entryStream = entry.Open())
					entryStream.CopyTo(memoryStream);
				Data = memoryStream.ToArray();
			}
			FileName = entry.Name;
			RelativePath = entry.FullName;
			LastWriteTime = entry.LastWriteTime.DateTime;
		}
		/// <summary>
		/// Constructs a ZipArchiveEntryData from data and a Last Write Time.
		/// </summary>
		/// <param name="fileName">The file name of the entry.</param>
		/// <param name="relativePath">The relative path of the entry.</param>
		/// <param name="data">The extracted data of the ZipArchiveEntry.</param>
		/// <param name="lastWriteTime">The last write time of the ZipArchiveEntry.</param>
		public ZipArchiveEntryData(string fileName, string relativePath, byte[] data, DateTime lastWriteTime)
		{
			this.FileName = fileName;
			this.RelativePath = relativePath;
			this.Data = data;
			this.LastWriteTime = lastWriteTime;
		}
	}
}
