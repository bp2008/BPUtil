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
	}
}
