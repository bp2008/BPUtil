using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.MVC
{
	/// <summary>
	/// A thread-safe collection of string key/value pairs.
	/// </summary>
	public class ViewDataContainer
	{
		private readonly ConcurrentDictionary<string, string> data = new ConcurrentDictionary<string, string>();

		/// <summary>
		/// Sets the specified key/value pair.
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="value">Value</param>
		public void Set(string key, string value)
		{
			data[key] = value;
		}
		/// <summary>
		/// Retrieves the value with the specified key. Returns null if the key is not found.
		/// </summary>
		/// <param name="key">Key</param>
		/// <returns></returns>
		public string Get(string key)
		{
			if (data.TryGetValue(key, out string value))
				return value;
			return null;
		}
		/// <summary>
		/// Tries to return the value with the specified key. Returns true if successful, false if the key was not found.
		/// </summary>
		/// <param name="key">Key</param>
		/// <param name="value">Value</param>
		/// <returns>True if the key was found.</returns>
		public bool TryGet(string key, out string value)
		{
			return data.TryGetValue(key, out value);
		}
	}
}
