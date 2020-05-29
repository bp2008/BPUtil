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
		/// <param name="value">Value. If null, the key is removed from the collection.</param>
		public void Set(string key, string value)
		{
			data[key] = value;
		}
		/// <summary>
		/// <para>Retrieves the value with the specified key.</para>
		/// <para>
		/// Returns null if the key is not found, and also if the stored value was null. To differentiate between the cases, use <see cref="TryGet(string, out string)"/>.
		/// </para>
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
		/// <summary>
		/// Deletes the specified key (and its value) from the collection, returning true if an item was deleted.
		/// </summary>
		/// <param name="key">Key</param>
		public bool Delete(string key)
		{
			return data.TryRemove(key, out string ignored);
		}
	}
}
