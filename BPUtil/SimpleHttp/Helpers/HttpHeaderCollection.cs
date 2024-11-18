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
		/// Gets the key of the HTTP header.
		/// </summary>
		public string Key { get; private set; }
		/// <summary>
		/// Gets or sets the value of the HTTP header.
		/// </summary>
		public string Value { get; set; }
		/// <summary>
		/// Maximum length we allow for an HTTP header key.
		/// </summary>
		public const int MAX_HEADER_KEY_LENGTH = 16384;
		/// <summary>
		/// Maximum length we allow for an HTTP header value.
		/// </summary>
		public const int MAX_HEADER_VALUE_LENGTH = 32768;
		/// <summary>
		/// Initializes an empty new instance of the HttpHeader class.
		/// </summary>
		public HttpHeader() { }
		/// <summary>
		/// Initializes a new instance of the HttpHeader class with the specified key and value.
		/// </summary>
		/// <param name="NameCase">Specifies how header names are normalized.</param>
		/// <param name="key">The key of the HTTP header.</param>
		/// <param name="value">The value of the HTTP header.</param>
		public HttpHeader(HeaderNameCase NameCase, string key, string value)
		{
			Key = NormalizeHeaderName(NameCase, key);
			Value = ValidateHeaderValue(value);
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
			else if (nameCase == HeaderNameCase.NoChange)
				return headerName;
			else if (nameCase == HeaderNameCase.TitleCase)
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
			else
				throw new ArgumentException("HTTP Header name case was unsupported: " + nameCase, nameof(nameCase));
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
			if (headerName.Length > MAX_HEADER_KEY_LENGTH)
				throw new ArgumentException("Header name is too long at " + headerName.Length + " characters.  Max supported length: " + MAX_HEADER_KEY_LENGTH + " characters.");
			if (!headerName.All(HeaderNameValidCharacters.Contains))
				throw new ArgumentException("Header name contains invalid characters: " + headerName + ".");
			return headerName;
		}
		/// <summary>
		/// Throws ArgumentException if the header value is null or is too long.
		/// </summary>
		/// <param name="headerValue">HTTP header value.</param>
		/// <returns>The header value.</returns>
		/// <exception cref="ArgumentException">If the given header value is invalid.</exception>
		public static string ValidateHeaderValue(string headerValue)
		{
			if (headerValue == null)
				throw new ArgumentException("Header value cannot be null.");
			if (headerValue.Length > MAX_HEADER_VALUE_LENGTH)
				throw new ArgumentException("Header value is too long at " + headerValue.Length + " characters.  Max supported length: " + MAX_HEADER_VALUE_LENGTH + " characters.");
			return headerValue;
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
		LowerCase,
		/// <summary>
		/// Header names are left exactly as entered (for testing).
		/// </summary>
		NoChange
	}

	// Rewrite this class to use a List<HttpHeader> with locked access.
	// The existing API for querying single headers should continue to combine header values, but do it at query time instead of add time.
	// Iterating should deliver duplicate header values as individual key value pairs.

	/// <summary>
	/// <para>Provides thread-safe (sychronized) read/write access to HTTP headers using case-insensitive keying.</para>
	/// <para>Note: The HttpHeader instances in this collection have mutable values, meaning their values could change at any time if they are being accessed by other threads.</para>
	/// <para>All header names are automatically normalized to title case for HTTP 1.1 unless otherwise specified in the HttpHeaderCollection constructor.</para>
	/// <para>This class preserves the order in which headers were added.</para>
	/// <para>Revised in 2024-11, this class no longer combines the values of headers sharing the same key, except in special cases:</para>
	/// <para>* Headers named "Cookie" are combined into one when added, with the values separated by semicolon and space: ("; ").</para>
	/// <para>* Some methods and properties return combined values if there are multiple headers with the same key.</para>
	/// </summary>
	public class HttpHeaderCollection : IEnumerable<HttpHeader>
	{
		/// <summary>
		/// Lock to hold while internally accessing the header collection.
		/// </summary>
		protected object myLock = new object();

		/// <summary>
		/// A list of headers in the order in which they were added to the collection.
		/// </summary>
		protected List<HttpHeader> headers = new List<HttpHeader>();

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
				HttpHeaderCollection c = new HttpHeaderCollection(NameCase);
				foreach (KeyValuePair<string, string> kvp in headers)
					c.Add(kvp);
				return c;
			}
			return null;
		}
		/// <summary>
		/// <para>Gets or sets the value of the header matching this key, or null.</para>
		/// <para>WARNING: Many headers can exist multiple times; if that happens this method returns their values concatenated with ", " as the separator.</para>
		/// <para>This is equivalent to using <see cref="Get(string)"/> or <see cref="Set(string, string)"/>.</para>
		/// </summary>
		/// <param name="headerName">Header Name (not case-sensitive).</param>
		/// <returns></returns>
		public string this[string headerName]
		{
			get => this.Get(headerName);
			set => this.Set(headerName, value);
		}
		/// <summary>
		/// <para>Gets an array of all the current HTTP Headers in the order in which they were added.</para>
		/// <para>Multiple headers may exist with the same key.</para>
		/// <para>Note: The HttpHeader instances in this collection have mutable values, meaning their values could change at any time if they are being accessed by other threads.</para>
		/// </summary>
		/// <returns></returns>
		public HttpHeader[] GetHeaderArray()
		{
			lock (myLock)
				return headers.ToArray();
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
		/// Gets the number of headers in the collection.
		/// </summary>
		public int Count()
		{
			lock (myLock)
				return headers.Count;
		}

		/// <summary>
		/// <para>Adds the specified header to the collection.</para>
		/// <para>(some headers can not exist more than once; this method will add or edit the existing value as needed)</para>
		/// </summary>
		/// <param name="headerName">Header Name (not case-sensitive; will be normalized according to <see cref="NameCase"/>)</param>
		/// <param name="value">Header Value (not allowed to be null)</param>
		public void Add(string headerName, string value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			lock (myLock)
			{
				if (headerName.IEquals("Cookie"))
				{
					HttpHeader cookieHeader = GetHeaders(headerName)?.FirstOrDefault();
					if (cookieHeader != null)
					{
						cookieHeader.Value = cookieHeader.Value + "; " + value;
						return;
					}
				}
				headers.Add(new HttpHeader(NameCase, headerName, value));
			}
		}

		/// <summary>
		/// <para>Adds the specified header to the collection.</para>
		/// <para>(some headers can not exist more than once; this method will add or edit the existing value as needed)</para>
		/// </summary>
		/// <param name="item">Header name and value.</param>
		public void Add(KeyValuePair<string, string> item)
		{
			Add(item.Key, item.Value);
		}

		/// <summary>
		/// <para>Copies the specified header and adds the copy to the collection, enforcing this collection's <see cref="NameCase"/>.</para>
		/// <para>(some headers can not exist more than once; this method will add or edit the existing value as needed)</para>
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
				headers.Clear();
			}
		}

		/// <summary>
		/// Returns true if this collection contains a header with the given name (not case-sensitive).
		/// </summary>
		/// <param name="headerName">Header name (not case-sensitive).</param>
		/// <returns></returns>
		public bool ContainsKey(string headerName)
		{
			lock (myLock)
			{
				return headers.Any(h => h.Key.IEquals(headerName));
			}
		}

		/// <summary>
		/// Removes all headers with the given name, returning true if any were removed, false if none were found.
		/// </summary>
		/// <param name="headerName">Header name (not case-sensitive).</param>
		/// <returns></returns>
		public bool Remove(string headerName)
		{
			lock (myLock)
			{
				int removed = headers.RemoveAll(h => h.Key.IEquals(headerName));
				return removed > 0;
			}
		}

		/// <summary>
		/// <para>Tries to get the header values with the specified name (not case-sensitive), returning true if successful, false if no header(s) existed with the specified name.  If no matching header existed, the <paramref name="values"/> output parameter will be an empty array.</para>
		/// </summary>
		/// <param name="headerName">Header name (not case-sensitive).</param>
		/// <param name="values">(Output) Header values.</param>
		/// <returns></returns>
		public bool TryGetValues(string headerName, out string[] values)
		{
			values = GetValues(headerName);
			return values.Length > 0;
		}

		/// <summary>
		/// <para>Tries to get the value of the first header with the specified name (not case-sensitive), returning true if successful, false if no header(s) existed with the specified name.  If no matching header existed, the <paramref name="value"/> output parameter will be set to <c>null</c>.</para>
		/// <para>WARNING: Many headers can exist multiple times; if that happens this method returns their values concatenated with ", " as the separator.</para>
		/// </summary>
		/// <param name="headerName">Header name (not case-sensitive).</param>
		/// <param name="value">(Output) Header value.</param>
		/// <returns></returns>
#if NET6_0
		public bool TryGetValue(string headerName, [MaybeNullWhen(false)] out string value)
#else
		public bool TryGetValue(string headerName, out string value)
#endif
		{
			value = Get(headerName);
			return value != null;
		}

		/// <summary>
		/// <para>Gets the values of the headers with the specified name, or an empty array if the header does not exist.</para>
		/// </summary>
		/// <param name="headerName">HTTP Header name (not case-sensitive)</param>
		/// <returns></returns>
		public string[] GetValues(string headerName)
		{
			lock (myLock)
			{
				return headers.Where(h => h.Key.IEquals(headerName)).Select(h => h.Value).ToArray();
			}
		}

		/// <summary>
		/// <para>Gets the value header with the specified name, or null if the header does not exist.</para>
		/// <para>WARNING: Many headers can exist multiple times; if that happens this method returns their values concatenated with ", " as the separator.</para>
		/// </summary>
		/// <param name="headerName">HTTP Header name (not case-sensitive)</param>
		/// <returns></returns>
		public string Get(string headerName)
		{
			string[] values = GetValues(headerName);
			if (values.Length == 0)
				return null;
			else
				return string.Join(", ", values);
		}

		/// <summary>
		/// <para>Gets an array of HttpHeaders with the specified name, or empty array if no matching header exists.</para>
		/// </summary>
		/// <param name="headerName">HTTP Header name (not case-sensitive)</param>
		/// <returns></returns>
		public HttpHeader[] GetHeaders(string headerName)
		{
			lock (myLock)
			{
				return headers.Where(h => h.Key.IEquals(headerName)).ToArray();
			}
		}
		/// <summary>
		/// <para>If zero headers exist with the given name, this method adds a new header with the given name and value.</para>
		/// <para>If one header exists with the given name, this method changes its value (keeping its place in the order).</para>
		/// <para>If multiple headers exists with the given name, this method changes the value of the first matching header (keeping its place in the order) and removes the other matching headers.</para>
		/// <para>(this is a convenience method intended to assign values to headers that must only exist once (such as "Content-Type")</para>
		/// </summary>
		/// <param name="headerName">HTTP Header name (not case-sensitive)</param>
		/// <param name="value">Value to set.  If null, all headers matching <paramref name="headerName"/> are removed from the collection.</param>
		public void Set(string headerName, string value)
		{
			if (value == null)
				Remove(headerName);
			else
			{
				lock (myLock)
				{
					HttpHeader hdr = headers.FirstOrDefault(h => h.Key.IEquals(headerName));
					if (hdr == null)
						Add(headerName, value);
					else
					{
						hdr.Value = value;
						headers.RemoveAll(h => h != hdr && h.Key.IEquals(headerName));
					}
				}
			}
		}
		/// <summary>
		/// <para>Given a complete HTTP header ("Name: value"), attempts to assign the header to this collection.</para>
		/// <para>If there is no colon in the header string, the the string is interpreted as a header name only and any headers with this name are removed from the collection.</para>
		/// <para>The full string value including any commas are assigned to a single header.  This method does not split the value into multiple headers.</para>
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
		/// <summary>
		/// Adds all headers from another HttpHeaderCollection to this HttpHeaderCollection.
		/// </summary>
		/// <param name="additionalResponseHeaders">Headers to add to this collection.</param>
		public void Merge(HttpHeaderCollection additionalResponseHeaders)
		{
			foreach (HttpHeader header in additionalResponseHeaders)
				this.Add(header);
		}
		/// <inheritdoc />
		public override string ToString()
		{
			return "{ \r\n" + string.Join("\r\n  ", this.GetHeaderArray().Select(s => s.ToString())) + " \r\n}";
		}
	}
}
