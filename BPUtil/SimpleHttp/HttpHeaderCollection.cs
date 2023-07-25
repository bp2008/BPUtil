using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp
{
	/// <summary>
	/// Provides thread-safe (synchronized) read/write access to HTTP headers using case-insensitive keying.  All header names are automatically normalized to title case.
	/// </summary>
	public class HttpHeaderCollection : IDictionary<string, string>
	{
		ConcurrentDictionary<string, string> dict = new ConcurrentDictionary<string, string>();
		/// <inheritdoc/>
		public string this[string key]
		{
			get => dict[GetKey(key)];
			set => dict[SetKey(key)] = value;
		}

		/// <inheritdoc/>
		public ICollection<string> Keys => dict.Keys.Select(NormalizeHeaderName).ToArray();

		/// <inheritdoc/>
		public ICollection<string> Values => dict.Values;

		/// <inheritdoc/>
		public int Count => dict.Count;
		/// <inheritdoc/>
		public bool IsReadOnly => false;

		/// <summary>Adds the specified header to the collection.  If the header already exists, its value will be appended to the existing value after a comma.</summary>
		/// <inheritdoc/>
		public void Add(string key, string value)
		{
			dict.AddOrUpdate(SetKey(key), value, (k, v) => v + "," + value);
		}

		/// <summary>Adds the specified header to the collection.  If the header already exists, its value will be appended to the existing value after a comma.</summary>
		/// <inheritdoc/>
		public void Add(KeyValuePair<string, string> item)
		{
			dict.AddOrUpdate(SetKey(item.Key), item.Value, (k, v) => v + "," + item.Value);
		}

		/// <inheritdoc/>
		public void Clear()
		{
			dict.Clear();
		}

		/// <inheritdoc/>
		public bool Contains(KeyValuePair<string, string> item)
		{
			return dict.TryGetValue(GetKey(item.Key), out string value) && item.Value == value;
		}

		/// <inheritdoc/>
		public bool ContainsKey(string key)
		{
			return dict.ContainsKey(GetKey(key));
		}

		/// <inheritdoc/>
		public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
		{
			foreach (KeyValuePair<string, string> item in this)
				array[arrayIndex++] = item;
		}

		/// <inheritdoc/>
		public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
		{
			return dict.Select(item => new KeyValuePair<string, string>(NormalizeHeaderName(item.Key), item.Value)).GetEnumerator();
		}

		/// <inheritdoc/>
		public bool Remove(string key)
		{
			return dict.TryRemove(GetKey(key), out string ignored);
		}

		/// <inheritdoc/>
		public bool Remove(KeyValuePair<string, string> item)
		{
			return Contains(item) && dict.TryRemove(GetKey(item.Key), out string ignored);
		}

		/// <inheritdoc/>
#if NET6_0
		public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
#else
		public bool TryGetValue(string key, out string value)
#endif
		{
			return dict.TryGetValue(GetKey(key), out value);
		}

		/// <inheritdoc/>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		private static readonly HashSet<char> HeaderNameValidCharacters = CreateHttpHeaderValidCharsHashSet();
		private static HashSet<char> CreateHttpHeaderValidCharsHashSet()
		{
			HashSet<char> s = new HashSet<char>();
			for (char c = '!'; c <= '~'; c++)
				if (c != ':')
					s.Add(c);
			return s;
		}
		/// <summary>
		/// Throws ArgumentException if the header name is null, empty, or contains invalid characters. Returns the header name unmodified.
		/// </summary>
		/// <param name="headerName">HTTP header name which may not be normalized yet.</param>
		/// <returns>The header name.</returns>
		/// <exception cref="ArgumentException">If the given header name is invalid.</exception>
		public static string ValidateHeaderName(string headerName)
		{
			if (string.IsNullOrEmpty(headerName))
				throw new ArgumentException("Header name cannot be null or empty.");
			if (headerName.Length > 16384)
				throw new ArgumentException("Header name is too long at " + headerName.Length + " characters.  Max supported length: 16384 characters.");
			if (!headerName.All(HeaderNameValidCharacters.Contains))
				throw new ArgumentException("Header name contains invalid characters: " + headerName + ".");
			return headerName;
		}
		/// <summary>
		/// Gets the header key for "setting" purposes (full validation is performed). Validates the header name and returns it in lower-case form.
		/// </summary>
		/// <param name="headerName">Header name.</param>
		/// <returns></returns>
		private static string SetKey(string headerName)
		{
			return ValidateHeaderName(headerName).ToLower();
		}
		/// <summary>
		/// Gets the header key for "getting" purposes (full validation is skipped). Efficiently returns a key which can be used to read the header value if it exists, without performing full validation on the header name.
		/// </summary>
		/// <param name="headerName">Header name.</param>
		/// <returns></returns>
		private static string GetKey(string headerName)
		{
			return headerName == null ? "" : headerName.ToLower();
		}

		/// <summary>
		/// Capitalizes the first character of each hyphen-separated word while making the rest of the characters lowercase.
		/// </summary>
		/// <param name="headerName">HTTP header name which may not be normalized yet.</param>
		/// <returns>The normalized header name.</returns>
		/// <exception cref="ArgumentException">If the given header name is invalid.</exception>
		public static string NormalizeHeaderName(string headerName)
		{
			ValidateHeaderName(headerName);
			string[] words = headerName.Split('-');
			for (int i = 0; i < words.Length; i++)
			{
				if (words[i].Length > 0)
					words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
			}
			return string.Join("-", words);
		}
	}
}
