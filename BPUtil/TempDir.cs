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
	/// <para>Manages the lifecycle of a temporary directory, creating it in the constructor and deleting it when dispose is called.</para>
	/// <para>Use within using(TempDir tmp = new TempDir()) { code } or otherwise handle disposing reliably.</para>
	/// <para>In the event of an application or system crash or incorrect usage of this class, the temporary directory and its contents may remain on disk.</para>
	/// </summary>
	public class TempDir : IDisposable
	{
		/// <summary>
		/// Full path of this temporary directory.
		/// </summary>
		public readonly string FullName;
		/// <summary>
		/// Constructs a TempDir using a random directory name.
		/// </summary>
		public TempDir()
		{
			int tries = 500;
			string tmpPath = Path.GetTempPath();
			while (FullName == null)
			{
				FullName = Path.Combine(tmpPath, Path.GetRandomFileName());
				if (Directory.Exists(FullName))
				{
					FullName = null;
					if (--tries > 0)
						throw new Exception("Unable to construct a randomly named temporary directory.");
				}
			}
		}
		/// <summary>
		/// Constructs a TempDir using the specified unique directory name.
		/// </summary>
		/// <param name="dirName">A directory name which is unique to this application.  It will be created in the directory returned by <see cref="Path.GetTempPath"/>.</param>
		/// <param name="deleteIfExists">If true, and the specified directory already exists, it will be deleted along with all its contents (dangerous!).  If false, an exception will be thrown if the directory already exists.</param>
		public TempDir(string dirName, bool deleteIfExists)
		{
			FullName = Path.Combine(Path.GetTempPath(), dirName);
			if (Directory.Exists(FullName))
			{
				if (deleteIfExists)
					Directory.Delete(FullName, true);
				else
					throw new Exception("Temporary directory \"" + FullName + "\" already exists.");
			}
		}

		#region IDisposable
		private bool disposedValue;
		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// dispose managed state (managed objects)
					Directory.Delete(FullName, true);
				}
				disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
