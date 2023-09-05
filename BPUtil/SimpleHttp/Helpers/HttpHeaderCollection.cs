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
	/// Represents an HTTP header with a key and value.
	/// </summary>
	public class HttpHeader : IComparable<HttpHeader>
	{
		/// <summary>
		/// The key of the HTTP header.
		/// </summary>
		public string Key { get; private set; }
		/// <summary>
		/// The value of the HTTP header.
		/// </summary>
		public string Value { get; set; }
		/// <summary>
		/// Initializes an empty new instance of the HttpHeader class.
		/// </summary>
		public HttpHeader() { }
		/// <summary>
		/// Initializes a new instance of the HttpHeader class with the specified key and value.
		/// </summary>
		/// <param name="key">The key of the HTTP header.</param>
		/// <param name="value">The value of the HTTP header.</param>
		public HttpHeader(string key, string value)
		{
			Key = key;
			Value = value;
		}
		/// <inheritdoc />
		public int CompareTo(HttpHeader other)
		{
			if (other == null)
				return 1;

			int result = string.Compare(Key, other.Key);
			if (result != 0)
				return result;

			return string.Compare(Value, other.Value);
		}
		/// <inheritdoc />
		public override bool Equals(object obj)
		{
			if (obj is HttpHeader other)
				return CompareTo(other) == 0;

			return false;
		}
		/// <inheritdoc />
		public override int GetHashCode()
		{
			int hash = 17;
			hash = hash * 23 + (Key != null ? StringComparer.Ordinal.GetHashCode(Key) : 0);
			hash = hash * 23 + (Value != null ? StringComparer.Ordinal.GetHashCode(Value) : 0);
			return hash;
		}
		/// <inheritdoc />
		public override string ToString()
		{
			return Key + ": " + Value;
		}
	}
	/// <summary>
	/// Specifies how header names are normalized.
	/// </summary>
	public enum HeaderNameCase
	{
		/// <summary>
		/// Header names are normalized to title case (for HTTP 1.1).
		/// </summary>
		TitleCase,
		/// <summary>
		/// Header names are normalized to lower case (for HTTP 2.0).
		/// </summary>
		LowerCase
	}

	// Rewrite this class to use a List<HttpHeader> with locked access.
	// The existing API for querying single headers should continue to combine header values, but do it at query time instead of add time.
	// Iterating should deliver duplicate header values as individual key value pairs.

	/// <summary>
	/// <para>Provides thread-safe (sychronized) read/write access to HTTP headers using case-insensitive keying.</para>
	/// <para>Note: Any HttpHeader instances obtained from this collection may be obtained and concurrently modified by other threads.</para>
	/// <para>All header names are automatically normalized to title case for HTTP 1.1.</para>
	/// <para>All header names are automatically normalized to title case for HTTP 1.1.</para>
	/// </summary>
	public class HttpHeaderCollection : IEnumerable<HttpHeader>
	{
		/// <summary>
		/// Lock to hold while internally accessing the header collection.
		/// </summary>
		protected object myLock = new object();

		/// <summary>
		/// A list of header keys, lower-case, in the order in which they were added to the collection.
		/// </summary>
		protected List<string> headerDefaultOrder = new List<string>();

		/// <summary>
		/// The internal keyed collection of HTTP headers using lower-case keys.
		/// </summary>
		protected Dictionary<string, string> dict = new Dictionary<string, string>();

		/// <summary>
		/// Specifies how header names are normalized.
		/// </summary>
		public readonly HeaderNameCase NameCase;

		/// <summary>
		/// Constructs a new empty HttpHeaderCollection.
		/// </summary>
		/// <param name="NameCase">Specifies how header names are normalized.</param>
		public HttpHeaderCollection(HeaderNameCase NameCase = HeaderNameCase.TitleCase)
		{
			this.NameCase = NameCase;
		}

		/// <summary>
		/// Constructs a new HttpHeaderCollection from the given list of Key/Value pairs.
		/// </summary>
		/// <param name="NameCase">Specifies how header names are normalized.</param>
		/// <param name="headers">Collection of Key/Value pairs.</param>
		public static HttpHeaderCollection FromPairs(HeaderNameCase NameCase, IEnumerable<KeyValuePair<string, string>> headers)
		{
			if (headers != null)
			{
				HttpHeaderCollection c = new HttpHeaderCollection();
				foreach (KeyValuePair<string, string> kvp in headers)
					c.Add(kvp);
				return c;
			}
			return null;
		}
		/// <summary>
		/// <para>Gets or sets the value of the first header matching this key, or null.</para>
		/// <para>Because the HttpHeaderCollection class automatically combines headers when they are added with the same name, most headers work fine with this. "Set-Cookie" however can have multiple distinct headers and this accessor is not compatible.</para>
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		public string this[string key]
		{
			get => this.GetValues(key)?[0];
			set => this.Set(key, value);
		}
		/// <summary>
		/// <para>Gets an array of all the current HTTP Headers in the order in which they were added.</para>
		/// <para>Multiple "Set-Cookie" headers may exist.  All other headers are automatically combined if they are <c>Add</c>ed with an existing key.</para>
		/// </summary>
		/// <returns></returns>
		public HttpHeader[] GetHeaderArray()
		{
			lock (myLock)
			{
				List<HttpHeader> headers = new List<HttpHeader>();
				foreach (string key in headerDefaultOrder)
				{
					string keyNormalized = NormalizeHeaderName(key);
					string value = dict[key];
					if (key == "set-cookie")
					{
						string[] parts = value.Split('\n');
						foreach (string part in parts)
							headers.Add(new HttpHeader(keyNormalized, part));
					}
					else
						headers.Add(new HttpHeader(keyNormalized, value));
				}
				return headers.ToArray();
			}
		}
		/// <inheritdoc />
		public IEnumerator<HttpHeader> GetEnumerator()
		{
			lock (myLock)
				return ((IEnumerable<HttpHeader>)GetHeaderArray()).GetEnumerator();
		}

		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator()
		{
			lock (myLock)
				return GetHeaderArray().GetEnumerator();
		}

		/// <summary>
		/// Gets the number of headers in the collection by returning <c>GetHeaderArray().Length</c>.
		/// </summary>
		public int Count()
		{
			return GetHeaderArray().Length;
		}

		/// <summary>
		/// <para>Adds the specified header to the collection.  If the header already exists, its value will be appended to the existing value after a comma.</para>
		/// <para>Cookie headers are a special case which are combined with semicolon and space instead of comma.</para>
		/// <para>Set-Cookie headers are a special case which are not appended to each other, instead, multiple Set-Cookie headers are allowed.</para>
		/// </summary>
		/// <param name="key">Header Name (not case-sensitive; will be normalized)</param>
		/// <param name="value">Header Value</param>
		public void Add(string key, string value)
		{
			key = SetKey(key);
			lock (myLock)
			{
				if (!dict.TryGetValue(key, out string existing))
				{
					headerDefaultOrder.Add(key);
					dict[key] = value;
					return;
				}
				if (key.IEquals("Set-Cookie"))
					dict[key] = existing + "\n" + value; // This special syntax needs parsed into multiple headers when iterating over the header collection.
				else if (key.IEquals("Cookie"))
					dict[key] = existing + "; " + value;
				else
					dict[key] = existing + "," + value;
			}
		}

		/// <summary>
		/// <para>Adds the specified header to the collection.  If the header already exists, its value will be appended to the existing value after a comma.</para>
		/// <para>Cookie headers are a special case which are combined with semicolon and space instead of comma.</para>
		/// <para>Set-Cookie headers are a special case which are not appended to each other, instead, multiple Set-Cookie headers are allowed.</para>
		/// </summary>
		/// <param name="item">Header name and value.</param>
		public void Add(KeyValuePair<string, string> item)
		{
			Add(item.Key, item.Value);
		}

		/// <summary>
		/// <para>Adds the specified header to the collection.  If the header already exists, its value will be appended to the existing value after a comma.</para>
		/// <para>Cookie headers are a special case which are combined with semicolon and space instead of comma.</para>
		/// <para>Set-Cookie headers are a special case which are not appended to each other, instead, multiple Set-Cookie headers are allowed.</para>
		/// </summary>
		/// <param name="item">Header to add.</param>
		public void Add(HttpHeader item)
		{
			Add(item.Key, item.Value);
		}

		/// <summary>
		/// Removes all headers from this collection, making it as if it was freshly constructed.
		/// </summary>
		public void Clear()
		{
			lock (myLock)
			{
				headerDefaultOrder.Clear();
				dict.Clear();
			}
		}

		/// <summary>
		/// Returns true if this collection contains a header with the given name (not case-sensitive).
		/// </summary>
		/// <param name="key">Header name (not case-sensitive).</param>
		/// <returns></returns>
		public bool ContainsKey(string key)
		{
			lock (myLock)
			{
				return dict.ContainsKey(GetKey(key));
			}
		}

		/// <summary>
		/// Removes all headers with the given name, returning true if any were removed, false if none were found.
		/// </summary>
		/// <param name="key">Header name (not case-sensitive).</param>
		/// <returns></returns>
		public bool Remove(string key)
		{
			key = GetKey(key);
			lock (myLock)
			{
				if (dict.Remove(key))
				{
					headerDefaultOrder.RemoveAll(s => s == key);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// <para>Tries to get the header values with the specified name (not case-sensitive), returning true if successful.</para>
		/// <para>Fully compatible with "Set-Cookie".</para>
		/// </summary>
		/// <param name="key">Header name (not case-sensitive).</param>
		/// <param name="values">(Output) Header values.</param>
		/// <returns></returns>
#if NET6_0
		public bool TryGetValues(string key, [MaybeNullWhen(false)] out string[] values)
#else
		public bool TryGetValues(string key, out string[] values)
#endif
		{
			key = GetKey(key);
			lock (myLock)
			{
				if (dict.TryGetValue(key, out string existing))
				{
					if (key == "set-cookie")
						values = existing.Split('\n');
					else
						values = new string[] { existing };
					return true;
				}
			}
			values = null;
			return false;
		}

		/// <summary>
		/// <para>Tries to get the value of the first header with the specified name (not case-sensitive), returning true if successful.</para>
		/// <para>NOT fully compatible with "Set-Cookie", as "Set-Cookie" can have multiple headers.</para>
		/// </summary>
		/// <param name="key">Header name (not case-sensitive).</param>
		/// <param name="value">(Output) Header value.</param>
		/// <returns></returns>
#if NET6_0
		public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
#else
		public bool TryGetValue(string key, out string value)
#endif
		{
			key = GetKey(key);
			lock (myLock)
			{
				if (dict.TryGetValue(key, out value))
				{
					if (key == "set-cookie")
						value = value.Split('\n')[0];
					return true;
				}
			}
			value = null;
			return false;
		}

		/// <summary>
		/// <para>Gets the values of the headers with the specified name, or null if the header does not exist.</para>
		/// <para>Fully compatible with "Set-Cookie".</para>
		/// </summary>
		/// <param name="headerName">HTTP Header name (not case-sensitive)</param>
		/// <returns></returns>
		public string[] GetValues(string headerName)
		{
			if (TryGetValues(headerName, out string[] v))
				return v;
			return null;
		}

		/// <summary>
		/// <para>Gets the value of the first header with the specified name, or null if the header does not exist.</para>
		/// <para>NOT fully compatible with "Set-Cookie", as "Set-Cookie" can have multiple headers.</para>
		/// </summary>
		/// <param name="headerName">HTTP Header name (not case-sensitive)</param>
		/// <returns></returns>
		public string Get(string headerName)
		{
			if (TryGetValue(headerName, out string v))
				return v;
			return null;
		}

		/// <summary>
		/// <para>Assigns multiple values to the header with the specified name.</para>
		/// <para>Because the HttpHeaderCollection class automatically combines headers, for most headers, this method will assign a single combined value.</para>
		/// <para>This overload is mainly meant for the "Set-Cookie" header, which can exist as multiple distinct headers.</para>
		/// </summary>
		/// <param name="headerName">HTTP Header name (not case-sensitive)</param>
		/// <param name="values">Values to set.  If null or empty array, the header is removed from the collection.</param>
		public void Set(string headerName, string[] values)
		{
			if (values == null || values.Length == 0)
				Remove(headerName);
			else
			{
				headerName = SetKey(headerName);
				string valueStr;
				if (headerName.IEquals("Set-Cookie"))
					valueStr = string.Join("\n", values); // This special syntax needs parsed into multiple headers when iterating over the header collection.
				else if (headerName.IEquals("Cookie"))
					valueStr = string.Join("; ", values);
				else
					valueStr = string.Join(",", values);
				lock (myLock)
				{
					if (!dict.ContainsKey(headerName))
						headerDefaultOrder.Add(headerName);
					dict[headerName] = valueStr;
				}
			}
		}
		/// <summary>
		/// <para>Assigns a single value to the header with the specified name, removing any existing values for the header.</para>
		/// </summary>
		/// <param name="headerName">HTTP Header name (not case-sensitive)</param>
		/// <param name="value">Value to set.  If null, the header is removed from the collection.</param>
		public void Set(string headerName, string value)
		{
			if (value == null || value.Length == 0)
				Remove(headerName);
			else
			{
				headerName = SetKey(headerName);
				lock (myLock)
				{
					if (!dict.ContainsKey(headerName))
						headerDefaultOrder.Add(headerName);
					dict[headerName] = value;
				}
			}
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
		/// Normalized the header name according to the method defined by <see cref="NameCase"/>.
		/// </summary>
		/// <param name="headerName">HTTP header name which may not be normalized yet.</param>
		/// <returns>The normalized header name.</returns>
		/// <exception cref="ArgumentException">If the given header name is invalid.</exception>
		public string NormalizeHeaderName(string headerName)
		{
			return NormalizeHeaderName(NameCase, headerName);
		}

		/// <summary>
		/// Normalized the header name according to the method specified.
		/// </summary>
		/// <param name="nameCase">Specifies how header names are normalized.</param>
		/// <param name="headerName">HTTP header name which may not be normalized yet.</param>
		/// <returns>The normalized header name.</returns>
		/// <exception cref="ArgumentException">If the given header name is invalid.</exception>
		public static string NormalizeHeaderName(HeaderNameCase nameCase, string headerName)
		{
			ValidateHeaderName(headerName);
			if (nameCase == HeaderNameCase.LowerCase)
				return headerName.ToLower();
			else
			{
				// Capitalizes the first character of each hyphen-separated word while making the rest of the characters lowercase.
				string[] words = headerName.Split('-');
				for (int i = 0; i < words.Length; i++)
				{
					if (words[i].Length > 0)
						words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
				}
				return string.Join("-", words);
			}
		}
		/// <summary>
		/// <para>Given a complete HTTP header ("Name: value"), attempts to assign the header to this collection.</para>
		/// <para>If there is no colon in the header string, the header is removed from the header collection.</para>
		/// <para>Throw an exception upon failure.</para>
		/// </summary>
		/// <param name="header">A complete HTTP header ("Name: value")</param>
		/// <exception cref="ArgumentNullException">If the [header] argument is null.</exception>
		/// <exception cref="ArgumentException">If the [header] argument is empty or whitespace.</exception>
		public void AssignHeaderFromString(string header)
		{
			if (header == null)
				throw new ArgumentNullException(nameof(header));
			if (string.IsNullOrWhiteSpace(header))
				throw new ArgumentException("Header string contains no visible characters.", nameof(header));

			int separator = header.IndexOf(':');
			if (separator == -1)
				this.Remove(header);
			else
			{
				string name = header.Substring(0, separator);
				int pos = separator + 1;
				while (pos < header.Length && header[pos] == ' ')
					pos++; // strip any spaces

				string value = header.Substring(pos);
				this.Set(name, value);
			}
		}
	}
}
