using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.NativeWin
{
	public static class PathCheck
	{
		public static bool ExistsOnPath(string fileName)
		{
			return GetFullPath(fileName) != null;
		}

		public static string GetFullPath(string fileName)
		{
			if (File.Exists(fileName))
				return Path.GetFullPath(fileName);

			string fullPath = GetFullPathUsingPathValues(fileName, Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User));
			if(fullPath == null)
				fullPath = GetFullPathUsingPathValues(fileName, Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine));
			return fullPath;
		}
		private static string GetFullPathUsingPathValues(string fileName, string pathValues)
		{
			foreach (string path in pathValues.Split(Path.PathSeparator))
			{
				string fullPath = Path.Combine(path, fileName);
				if (File.Exists(fullPath))
					return fullPath;
			}
			return null;
		}
	}
}
