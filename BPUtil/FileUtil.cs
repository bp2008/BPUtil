using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
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
			SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
			oFileSecurity.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl, AccessControlType.Allow));
			FileInfo fi = new FileInfo(filePath);
			fi.SetAccessControl(oFileSecurity);
		}
		/// <summary>
		/// For the given directory, allows "Full Control" permission to "Users".
		/// </summary>
		/// <param name="directoryPath">Path of the directory to set permission on.</param>
		public static void DirectoryFullControlToUsers(string directoryPath)
		{
			DirectorySecurity directorySecurity = new DirectorySecurity();
			SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
			FileSystemAccessRule rule = new FileSystemAccessRule(sid, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow);
			directorySecurity.AddAccessRule(rule);
			DirectoryInfo di = new DirectoryInfo(directoryPath);
			di.SetAccessControl(directorySecurity);
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

			File.Move(sourceFileName, destFileName);
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
		/// <summary>
		/// Asynchronously opens a text file, reads all text in the file with the specified encoding, and then closes the file.
		/// </summary>
		/// <param name="path">The file to open for reading.</param>
		/// <param name="encoding">The encoding applied to the contents of the file.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/>.</param>
		/// <returns>A task that represents the asynchronous read operation, which wraps the string containing all text in the file.</returns>
		public static Task<string> ReadAllTextAsync(string path, Encoding encoding, CancellationToken cancellationToken = default)
		{
#if NET6_0
			return File.ReadAllTextAsync(path, encoding, cancellationToken);
#else
			using (StreamReader sr = new StreamReader(path, encoding))
				return sr.ReadToEndAsync();
#endif
		}
		/// <summary>
		/// Asynchronously creates a new file, writes the specified string to the file using the specified encoding, and then closes the file. If the target file already exists, it is overwritten.
		/// </summary>
		/// <param name="path">The file to write to.</param>
		/// <param name="contents">The string to write to the file.</param>
		/// <param name="encoding">The encoding to apply to the string.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/>.</param>
		/// <returns>A task that represents the asynchronous write operation.</returns>
		public static Task WriteAllTextAsync(string path, string contents, Encoding encoding, CancellationToken cancellationToken = default)
		{
#if NET6_0
			return File.WriteAllTextAsync(path, contents, encoding, cancellationToken);
#else
			using (StreamWriter sw = new StreamWriter(path, false, encoding))
				return sw.WriteAsync(contents);
#endif
		}
		/// <summary>
		/// <para>Creates a relative path string, given two paths that share a root.</para>
		/// <para>Inputted paths may use either backslash (\) or forward slash (/), but the outputted path will always use forward slash (/).</para>
		/// <para>Paths are treated as case-sensitive, so, e.g. "C:/Folder" is not equivalent to "c:/Folder" or "C:/folder".</para>
		/// <para>Examples:</para>
		/// <para>RelativePath("C:/Folder", "C:/Folder/File.txt") -&gt; "File.txt"</para>
		/// <para>RelativePath("C:/Folder/", "C:/Folder/File.txt") -&gt; "File.txt"</para>
		/// <para>RelativePath("C:/Folder", "C:/Folder/Subfolder/File.txt") -&gt; "Subfolder/File.txt"</para>
		/// <para>RelativePath("C:/Folder", "C:/File.txt") -&gt; "../File.txt"</para>
		/// <para>RelativePath("C:/Folder", "File.txt") -&gt; "File.txt"</para>
		/// </summary>
		/// <param name="rootPath">Path of a folder which shall be the base of the relative path produced by this function. E.g. "C:/Folder".</param>
		/// <param name="targetPath">The path that defines the target file or folder.  Must begin with <paramref name="rootPath"/>.  (e.g. "C:/Folder/File.txt").</param>
		/// <returns>The target path relative to the root path.</returns>
		/// <exception cref="ArgumentNullException">If an argument is null.</exception>
		/// <exception cref="ArgumentException">If an argument does not represent a valid URI.</exception>
		/// <exception cref="InvalidOperationException">If the Uri class is unable to make a relative URI from the given inputs.</exception>
		public static string RelativePath(string rootPath, string targetPath)
		{
			if (string.IsNullOrWhiteSpace(rootPath))
				throw new ArgumentException("Root path is null or empty", nameof(rootPath));
			if (string.IsNullOrWhiteSpace(targetPath))
				throw new ArgumentException("Target path is null or empty", nameof(targetPath));

			rootPath = rootPath.Trim().Replace('\\', '/');

			targetPath = targetPath.Trim().Replace('\\', '/');

			if (!targetPath.StartsWith(rootPath))
				throw new ArgumentException("Target path did not begin with Root path.");

			targetPath = targetPath.Substring(rootPath.Length);
			if (targetPath != "")
			{
				if (!rootPath.EndsWith("/") && !targetPath.StartsWith("/"))
					throw new ArgumentException("Target path is not relative to the root path.");
				if (targetPath.Contains("../"))
					throw new ArgumentException("Target path is not allowed to include \"../\" after the root path.");
				targetPath = targetPath.TrimStart('/');
			}
			return targetPath;
		}
		/// <summary>
		/// <para>Returns the absolute path to the given relative file.</para>
		/// <para>Inputted paths may use either backslash (\) or forward slash (/), but the outputted path will always use forward slash (/).</para>
		/// <para>If the inputted paths are invalid or if the relative path escapes from the root directory (e.g. "../file.txt"), null is returned.</para>
		/// </summary>
		/// <param name="rootPath">Path of a root directory from which the relative path is evaluated.</param>
		/// <param name="relativePath">Path to a file or directory which is relative to <paramref name="rootPath"/>.</param>
		/// <returns></returns>
		public static string GetNonEscapingAbsolutePath(string rootPath, string relativePath)
		{
			try
			{
				DirectoryInfo diRoot = new DirectoryInfo(rootPath);
				string baseDir = diRoot.FullName.Replace('\\', '/').TrimEnd('/') + '/';

				FileInfo fiTarget = new FileInfo(Path.Combine(diRoot.FullName, relativePath));
				string absPath = fiTarget.FullName.Replace('\\', '/');

				if (absPath.StartsWith(baseDir))
					return absPath;
			}
			catch { }
			return null;
		}
	}
}
