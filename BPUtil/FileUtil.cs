using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	public static class FileUtil
	{
#if NETFRAMEWORK || NET6_0_WIN
		/// <summary>
		/// Allows "Full Control" permission to "Users".
		/// </summary>
		/// <param name="filePath">Path of file to set permission on.</param>
		public static void FullControlToUsers(string filePath)
		{
			FileSecurity oFileSecurity = new FileSecurity();
			oFileSecurity.AddAccessRule(new FileSystemAccessRule("Users", FileSystemRights.FullControl, AccessControlType.Allow));
			FileInfo fi = new FileInfo(filePath);
			fi.SetAccessControl(oFileSecurity);
		}
#endif

		/// <summary>
		/// Copies a directory and its contents to a new location.
		/// </summary>
		/// <param name="sourceDirName">Path to the source directory.</param>
		/// <param name="destDirName">Path to the destination directory.</param>
		/// <param name="copySubDirs">If true, subdirectories will be copied recursively.</param>
		public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
		{
			DirectoryInfo dir = new DirectoryInfo(sourceDirName);
			if (!dir.Exists)
				throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);

			Directory.CreateDirectory(destDirName);

			FileInfo[] files = dir.GetFiles();
			foreach (FileInfo file in files)
			{
				string tempPath = Path.Combine(destDirName, file.Name);
				file.CopyTo(tempPath, false);
			}

			if (copySubDirs)
			{
				DirectoryInfo[] dirs = dir.GetDirectories();
				foreach (DirectoryInfo subdir in dirs)
				{
					string tempPath = Path.Combine(destDirName, subdir.Name);
					DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
				}
			}
		}
		/// <summary>
		/// Moves a directory and its contents to a new location, overwriting existing content as necessary.
		/// </summary>
		/// <param name="sourceDirName">Path to the source directory.</param>
		/// <param name="destDirName">Path to the destination directory.</param>
		public static void DirectoryMove(string sourceDirName, string destDirName)
		{
			DirectoryInfo dir = new DirectoryInfo(sourceDirName);
			if (!dir.Exists)
				throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);

			Directory.CreateDirectory(destDirName);

			FileInfo[] files = dir.GetFiles();
			foreach (FileInfo file in files)
			{
				string tempPath = Path.Combine(destDirName, file.Name);
				FileMove(file.FullName, tempPath);
			}

			DirectoryInfo[] dirs = dir.GetDirectories();
			foreach (DirectoryInfo subdir in dirs)
			{
				string tempPath = Path.Combine(destDirName, subdir.Name);
				DirectoryMove(subdir.FullName, tempPath);
			}

			dir.Delete();
		}
		/// <summary>
		/// Moves a file to a new location, overwriting existing content as necessary.
		/// </summary>
		/// <param name="sourceFileName">Path to the source file.</param>
		/// <param name="destFileName">Path to the destination file.</param>
		public static void FileMove(string sourceFileName, string destFileName)
		{
			if (!File.Exists(sourceFileName))
				throw new DirectoryNotFoundException("Source file does not exist or could not be found: " + sourceFileName);

			FileInfo fiDest = new FileInfo(destFileName);
			if (fiDest.Exists)
			{
				if (fiDest.IsReadOnly)
					fiDest.IsReadOnly = false;
				fiDest.Delete();
			}

			if (GetDriveLetter(sourceFileName) == GetDriveLetter(destFileName))
			{
				File.Move(sourceFileName, destFileName);
			}
			else
			{
				File.Copy(sourceFileName, destFileName, true);
				File.Delete(sourceFileName);
			}
		}
		/// <summary>
		/// Returns the drive letter, capitalized, from the absolute Windows path ("c:/temp/file.txt" yields "C").  Returns null if a drive letter could not be identified.
		/// </summary>
		/// <param name="absolutePath"></param>
		/// <returns></returns>
		public static string GetDriveLetter(string absolutePath)
		{
			if (Platform.IsUnix())
				return null;
			int idxColon = absolutePath.IndexOf(':');
			if (idxColon < 1)
				return null;
			int idxSlash = absolutePath.IndexOfAny(new char[] { '/', '\\' });
			if (idxSlash > -1 && idxSlash < idxColon)
				return null;
			return absolutePath.Substring(0, idxColon).ToUpper();
		}
		/// <summary>
		/// Returns true if the specified path refers to an existing directory.  Just call Directory.Exists.  I wrote this method so I would stop looking for it.
		/// </summary>
		/// <param name="path">Path to a file or directory which may exist.</param>
		/// <returns></returns>
		public static bool IsDirectory(string path)
		{
			return Directory.Exists(path);
		}
		/// <summary>
		/// Returns true if the specified path refers to an existing file.  Just call File.Exists.  I wrote this method so I would stop looking for it.
		/// </summary>
		/// <param name="path">Path to a file or directory which may exist.</param>
		/// <returns></returns>
		public static bool IsFile(string path)
		{
			return File.Exists(path);
		}
		/// <summary>
		/// Returns true if a file or directory exists at the specified path.
		/// </summary>
		/// <param name="path">Path to a file or directory which may exist.</param>
		/// <returns></returns>
		public static bool Exists(string path)
		{
			return Directory.Exists(path) || File.Exists(path);
		}
		/// <summary>
		/// Moves up through the directory tree until the directory with the specified name is found (case-sensitive), then returns the absolute path to that directory.  If the requested ancestor directory is not found, returns null.
		/// </summary>
		/// <param name="path">Path to start at.</param>
		/// <param name="ancestorDirectoryName">Name of the directory to find (case-sensitive).</param>
		/// <returns></returns>
		public static string FindAncestorDirectory(string path, string ancestorDirectoryName)
		{
			DirectoryInfo di;
			if (IsFile(path))
				di = new FileInfo(path).Directory;
			else
				di = new DirectoryInfo(path);
			while (di != null && di.Name != ancestorDirectoryName)
				di = di.Parent;
			return di?.FullName;
		}
	}
}
