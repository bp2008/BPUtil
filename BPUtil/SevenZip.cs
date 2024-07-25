using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// Simple wrapper for 7za.exe (7zip command-line app).  Does not report progress.
	/// </summary>
	public static class SevenZip
	{
		/// <summary>
		/// Extract the contents of an archive. If there is an error, an exception will be thrown.
		/// </summary>
		/// <param name="sevenZipCommandLineExePath">Path of 7za.exe.</param>
		/// <param name="archivePath">Path to a 7zip-compatible archive (*.7z for example).</param>
		/// <param name="outputDirectory">Path to a directory into which to extract the contents of the archive.</param>
		/// <param name="threads">Number of threads the 7zip executable is allowed to use.</param>
		/// <exception cref="Exception">Thrown if any of the input paths are invalid or if 7za.exe returns a nonzero result.</exception>
		/// <param name="lowPriority">If true, the 7zip process will be assigned BelowNormal priority.</param>
		public static void Extract(string sevenZipCommandLineExePath, string archivePath, string outputDirectory, int threads = 2, bool lowPriority = false)
		{
			if (!File.Exists(sevenZipCommandLineExePath))
				throw new Exception("7zip command line executable not found at path \"" + sevenZipCommandLineExePath + "\"");
			if (!File.Exists(archivePath))
				throw new Exception("Input file not found for 7zip extraction: \"" + archivePath + "\"");

			Directory.CreateDirectory(outputDirectory);
			if (!Directory.Exists(outputDirectory))
				throw new Exception("Output directory could not be created: \"" + outputDirectory + "\"");

			threads = threads.Clamp(1, Environment.ProcessorCount);
			string std, err;
			int result = ProcessRunner.RunProcessAndWait(sevenZipCommandLineExePath, "x -mmt" + threads + " \"" + archivePath + "\" -o\"" + outputDirectory + "\"", out std, out err, Options(lowPriority));
			if (result != 0)
				throw new Exception("7zip failed to extract files in archive \"" + archivePath + "\": " + std + " " + err);
		}
		/// <summary>
		/// Lists the files in an archive.
		/// </summary>
		/// <param name="sevenZipCommandLineExePath">Path of 7za.exe.</param>
		/// <param name="archivePath">Path to a 7zip-compatible archive (*.7z for example).</param>
		/// <param name="lowPriority">If true, the 7zip process will be assigned BelowNormal priority.</param>
		/// <returns></returns>
		public static SevenZipFileData[] ListFiles(string sevenZipCommandLineExePath, string archivePath, bool lowPriority = false)
		{
			if (!File.Exists(sevenZipCommandLineExePath))
				throw new Exception("7zip command line executable not found at path \"" + sevenZipCommandLineExePath + "\"");
			if (!File.Exists(archivePath))
				throw new Exception("Input file not found for 7zip list files: \"" + archivePath + "\"");

			string std, err;
			int result = ProcessRunner.RunProcessAndWait(sevenZipCommandLineExePath, "l -slt \"" + archivePath + "\"", out std, out err, Options(lowPriority));
			if (result != 0)
				throw new Exception("7zip failed to list files in archive \"" + archivePath + "\": " + std + " " + err);

			std = std.Replace("\r\n", "\n");
			std = std.Replace("\r", "\n");

			StringParser parser = new StringParser(std);

			parser.ThrowAwayThrough("\n\n----------\n");
			if (parser.CurrentIndex <= 0)
				throw new Exception("7zip did not produce the expected output when listing files in archive \"" + archivePath + "\": " + std + " " + err);

			List<SevenZipFileData> files = new List<SevenZipFileData>();
			Dictionary<string, string> metadata = new Dictionary<string, string>();
			string separator = " = ";
			while (!parser.IsFinished())
			{
				string line = parser.GetUntil('\n');
				parser.ThrowAway();
				if (line.Length == 0)
				{
					// This line is a separator between file listings.
					if (metadata.Count > 0)
					{
						files.Add(new SevenZipFileData(metadata));
						metadata.Clear();
					}
				}
				else
				{
					int idxSeparator = line.IndexOf(separator);
					if (idxSeparator > -1)
					{
						// This line is a "key = value" pair.
						metadata.Add(line.Substring(0, idxSeparator), line.Substring(idxSeparator + separator.Length));
					}
					else
						throw new Exception("7zip file list output did not match expected format using archive \"" + archivePath + "\": " + std + " " + err);
				}
			}

			return files.ToArray();
		}
		/// <summary>
		/// Creates a new archive from the given input path (set <paramref name="createNew"/>=false to allow reusing an existing archive). If there is an error, an exception will be thrown.
		/// </summary>
		/// <param name="sevenZipCommandLineExePath">Path of 7za.exe.</param>
		/// <param name="archivePath">Path to a 7zip archive (*.7z).</param>
		/// <param name="sourcePath">Path of a file or directory to put into the new archive.</param>
		/// <param name="threads">Number of threads the 7zip executable is allowed to use.</param>
		/// <param name="lowPriority">If true, the 7zip process will be assigned BelowNormal priority.</param>
		/// <param name="createNew">If true, an exception will be thrown if the archive already exists. If false, items may be added to an existing archive.</param>
		public static void Create7zArchive(string sevenZipCommandLineExePath, string archivePath, string sourcePath, int threads = 2, bool lowPriority = false, bool createNew = true)
		{
			if (!File.Exists(sevenZipCommandLineExePath))
				throw new Exception("7zip command line executable not found at path \"" + sevenZipCommandLineExePath + "\"");
			if (createNew && FileUtil.Exists(archivePath))
				throw new Exception("Cannot create 7z archive because an object already exists at the path \"" + archivePath + "\".");
			if (!FileUtil.Exists(sourcePath))
				throw new Exception("Cannot add to 7z archive because nothing was found at the source path \"" + sourcePath + "\".");

			FileInfo outputFile = new FileInfo(archivePath);
			if (!outputFile.Directory.Exists)
			{
				string outDir = outputFile.Directory.FullName;
				Directory.CreateDirectory(outDir);
				if (!Directory.Exists(outDir))
					throw new Exception("7z archive output directory could not be created for \"" + archivePath + "\"");
			}

			threads = threads.Clamp(1, Environment.ProcessorCount);
			string std, err;
			int result = ProcessRunner.RunProcessAndWait(sevenZipCommandLineExePath, "a -t7z -mmt" + threads + " \"" + archivePath + "\" \"" + sourcePath + "\"", out std, out err, Options(lowPriority));
			if (result != 0)
				throw new Exception("7zip failed to add path \"" + sourcePath + "\" to archive \"" + archivePath + "\": " + std + " " + err);
		}
		/// <summary>
		/// Renames a file or folder in an archive.
		/// </summary>
		/// <param name="sevenZipCommandLineExePath">Path of 7za.exe.</param>
		/// <param name="archivePath">Path to an archive file (*.zip, *.7z, etc.).</param>
		/// <param name="sourcePath">Path to find, e.g. "folder/subfolder"</param>
		/// <param name="newPath">Path to assign, e.g. "folder/renamed"</param>
		/// <param name="lowPriority">If true, the 7zip process will be assigned BelowNormal priority.</param>
		public static void Rename(string sevenZipCommandLineExePath, string archivePath, string sourcePath, string newPath, bool lowPriority = false)
		{
			if (!File.Exists(sevenZipCommandLineExePath))
				throw new Exception("7zip command line executable not found at path \"" + sevenZipCommandLineExePath + "\"");
			if (!File.Exists(archivePath))
				throw new Exception("Archive file not found for 7zip rename operation: \"" + archivePath + "\"");

			int result = ProcessRunner.RunProcessAndWait(sevenZipCommandLineExePath, "rn \"" + archivePath + "\" \"" + sourcePath + "\" \"" + newPath + "\"", out string std, out string err, Options(lowPriority));
			if (result != 0)
				throw new Exception("7zip failed to rename path \"" + sourcePath + "\" to \"" + newPath + "\" in archive \"" + archivePath + "\": " + std + " " + err);
		}
		/// <summary>
		/// Updates a file in an archive.  Due to 7za API limitations, this can only update a file that is at the root of the archive, and the file name within the archive must be the same as it exists on disk.
		/// </summary>
		/// <param name="sevenZipCommandLineExePath">Path of 7za.exe.</param>
		/// <param name="archivePath">Path to an archive file (*.zip, *.7z, etc.).</param>
		/// <param name="sourcePath">Path of the file on disk which you want to update in the archive.  The file is expected to already exist at the root of the archive.  This function is supposed to also work to update a directory, but has not been tested that way at the time of this writing.</param>
		/// <param name="threads">Number of threads the 7zip executable is allowed to use.</param>
		/// <param name="lowPriority">If true, the 7zip process will be assigned BelowNormal priority.</param>
		public static void Update(string sevenZipCommandLineExePath, string archivePath, string sourcePath, int threads = 1, bool lowPriority = false)
		{
			if (!File.Exists(sevenZipCommandLineExePath))
				throw new Exception("7zip command line executable not found at path \"" + sevenZipCommandLineExePath + "\"");
			if (!File.Exists(archivePath))
				throw new Exception("Archive file not found for 7zip update operation: \"" + archivePath + "\"");
			if (!File.Exists(sourcePath))
				throw new Exception("Input file not found for 7zip update operation: \"" + sourcePath + "\"");

			threads = threads.Clamp(1, Environment.ProcessorCount);
			int result = ProcessRunner.RunProcessAndWait(sevenZipCommandLineExePath, "u -mmt" + threads + " \"" + archivePath + "\" \"" + sourcePath + "\"", out string std, out string err, Options(lowPriority));
			if (result != 0)
				throw new Exception("7zip failed to update file \"" + Path.GetFileName(sourcePath) + "\" from disk path \"" + sourcePath + "\" in archive \"" + archivePath + "\": " + std + " " + err);
		}
		private static ProcessRunnerOptions Options(bool lowPriority)
		{
			if (lowPriority)
				return new ProcessRunnerOptions(ProcessPriorityClass.BelowNormal);
			else
				return null;
		}
	}
	/// <summary>
	/// Data about a file in an archive.
	/// </summary>
	public class SevenZipFileData
	{
		public string Path;
		public long Size;
		public long PackedSize;
		public DateTime Modified;
		public DateTime Created;
		public DateTime Accessed;
		public SevenZipFileData(Dictionary<string, string> metadata)
		{
			metadata.TryGetValue("Path", out Path);
			if (metadata.TryGetValue("Size", out string sSize))
				long.TryParse(sSize, out Size);
			if (metadata.TryGetValue("PackedSize", out string sPackedSize))
				long.TryParse(sPackedSize, out PackedSize);
			if (metadata.TryGetValue("Modified", out string sModified))
				DateTime.TryParse(sModified, out Modified);
			if (metadata.TryGetValue("Created", out string sCreated))
				DateTime.TryParse(sCreated, out Created);
			if (metadata.TryGetValue("Accessed", out string sAccessed))
				DateTime.TryParse(sAccessed, out Accessed);
		}
	}
}
