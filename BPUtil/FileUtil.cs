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
		/// <summary>
		/// Allows "Full Control" permission to "Users".
		/// </summary>
		/// <param name="filePath">Path of file to set permission on.</param>
		public static void FullControlToUsers(string filePath)
		{
			FileSecurity oFileSecurity = new FileSecurity();
			oFileSecurity.AddAccessRule(new FileSystemAccessRule("Users", FileSystemRights.FullControl, AccessControlType.Allow));
			File.SetAccessControl(filePath, oFileSecurity);
		}

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
	}
}
