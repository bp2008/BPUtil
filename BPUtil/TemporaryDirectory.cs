using System.IO;
using System.Threading;

namespace BPUtil
{
	/// <summary>
	/// Static class providing temporary directory paths.
	/// </summary>
	public static class TemporaryDirectory
	{
		private static long tempDirCounter = 0;
		private static object tempDirLock = new object();
		/// <summary>
		/// <para>WARNING: Do not use this class with programs that can have multiple instances running concurrently!</para>
		/// <para>Creates a unique temporary directory within this application's writable directory and returns the absolute path to it.  The path is constructed as (Globals.WritableDirectoryBase + "tmp_u/" + auto_increment_integer + "/").</para>
		/// <para>The directory is guaranteed to exist and be empty before this method returns.</para>
		/// <para>You should delete the directory and its contents when you are done with it.  In case that doesn't happen, this class will delete all previously existing temporary directories created by this function (Globals.WritableDirectoryBase + "tmp_u/") automatically the first time it is used (per process).</para>
		/// </summary>
		/// <returns></returns>
		public static string GetUniqueTemporaryDirectory()
		{
			string appTempDirBase = Globals.WritableDirectoryBase + "tmp_u/";
			if (Interlocked.Read(ref tempDirCounter) == 0)
			{
				lock (tempDirLock)
				{
					if (Interlocked.Read(ref tempDirCounter) == 0 && Directory.Exists(appTempDirBase))
					{
						Directory.Delete(appTempDirBase, true);
					}
				}
			}
			string myTempDirBase = appTempDirBase + Interlocked.Increment(ref tempDirCounter) + "/";
			if (Directory.Exists(myTempDirBase))
				Directory.Delete(myTempDirBase, true);
			Directory.CreateDirectory(myTempDirBase);
			return myTempDirBase;
		}
	}
}
