using BPUtil.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static BPUtil.SimpleHttp.HttpProcessor;
using EndOfStreamException = BPUtil.SimpleHttp.HttpProcessor.EndOfStreamException;

namespace BPUtil.SimpleHttp
{
	/// <summary>
	/// <para>Contains an HTTP request.</para>
	/// <para>Construct via <see cref="FromStream"/> or <see cref="FromStreamAsync"/>.</para>
	/// <para>When finished, clean up via <see cref="CleanupSync"/> or <see cref="CleanupAsync"/> otherwise it is possible that the request body will not have been read to the end by the Http Server.</para>
	/// </summary>
	public class SimpleHttpRequest
	{
		/// <summary>
		/// The maximum size in bytes allowed for a "application/x-www-form-urlencoded" form (2 MiB).
		/// </summary>
		protected const int MAX_FORM_SIZE = 2 * 1024 * 1024;
		/// <summary>
		/// The Http method used.  i.e. "POST" or "GET"
		/// </summary>
		public string HttpMethod { get; private set; }
		/// <summary>
		/// The protocol version string sent by the client.  e.g. "HTTP/1.1"
		/// </summary>
		public string HttpProtocolVersionString { get; private set; }
		/// <summary>
		/// The requested url.
		/// </summary>
		public Uri Url { get; private set; }
		/// <summary>
		/// <para>The path to and name of the requested page, not including the first '/'.</para>
		/// <para>For example, if the URL was "/articles/science/moon.html?date=2011-10-21", requestedPage would be "articles/science/moon.html".</para>
		/// <para>URL-encoded characters remain url-encoded. E.g. "File%20Name.jpg".</para>
		/// </summary>
		public string Page { get; private set; }
		/// <summary>
		/// A map of HTTP header names and values. Header names are all normalized to standard capitalization, and header names are treated as case-insensitive.
		/// </summary>
		public HttpHeaderCollection Headers { get; private set; } = new HttpHeaderCollection();

		/// <summary>
		/// A SortedList mapping keys to values of parameters (case-insensitive keying).  This list is populated if and only if the request was a POST request with mimetype "application/x-www-form-urlencoded".
		/// </summary>
		private SortedList<string, string> PostParams { get; } = new SortedList<string, string>(StringComparer.OrdinalIgnoreCase);
		/// <summary>
		/// A SortedList mapping keys to values of parameters (case-insensitive keying).  This list is populated parameters that were appended to the url (the query string).  e.g. if the url is "mypage.html?arg1=value1&amp;arg2=value2", then there will be two parameters ("arg1" with value "value1" and "arg2" with value "value2")
		/// </summary>
		private SortedList<string, string> QueryString { get; } = new SortedList<string, string>(StringComparer.OrdinalIgnoreCase);
		/// <summary>
		/// If not null, the specified number is the value of the Content-Length header.
		/// </summary>
		public long? ContentLength { get; private set; } = null;

		/// <summary>
		/// <para>Gets the Stream containing the request body.</para>
		/// <para>IMPORTANT: By default, this stream does not support seeking or length querying (it can only be read to the end one time!).  If you require the ability to seek or read multiple times, you must call <see cref="GetRequestBodyMemoryStream"/> at least one time BEFORE reading from RequestBodyStream.</para>
		/// <para>It will begin positioned at the beginning of the request body and end at the end of the request body.</para>
		/// <para>For requests that did not provide a body, this will be null.</para>
		/// </summary>
		public Stream RequestBodyStream { get; private set; }
		/// <summary>
		/// Values provided in the "Connection" header, each trimmed of whitespace.  E.g. ["keep-alive", "Upgrade"].  Null if there was no "Connection" header.
		/// </summary>
		public string[] ConnectionHeaderValues { get; private set; }
		/// <summary>
		/// The cookies sent by the remote client.
		/// </summary>
		public Cookies Cookies { get; private set; }
		/// <summary>
		/// Returns true if the client has requested gzip compression.
		/// </summary>
		public bool ClientRequestsGZipCompression
		{
			get
			{
				string acceptEncoding = Headers.Get("Accept-Encoding") ?? "";
				string[] types = acceptEncoding.Split(',');
				foreach (string type in types)
					if (type.Trim().ToLower() == "gzip")
						return true;
				return false;
			}
		}
		/// <summary>
		/// Constructs a SimpleHttpRequest from a list of text lines that were read from a stream.
		/// </summary>
		/// <param name="baseUriThisServer">Base URI of this web server, to be included in the Request.Url if the client does not provide an absolute URI in the request.</param>
		/// <param name="lines">List of lines including the HTTP request line and headers.</param>
		/// <param name="stream">The stream which the request body can be read from.</param>
		private SimpleHttpRequest(Uri baseUriThisServer, List<string> lines, Stream stream)
		{
			if (lines == null)
				throw new ArgumentNullException(nameof(lines));
			if (lines.Count == 0)
				throw new EndOfStreamException();
			try
			{
				ParseRequestLine(baseUriThisServer, lines[0]);
				ParseHeaders(Headers, lines);

				ConnectionHeaderValues = Headers.Get("Connection")?
					.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
					.Select(s => s.Trim())
					.ToArray();

				ParseQueryStringArguments(QueryString, Url.Query, true, false);

				Cookies = Cookies.FromString(Headers.Get("Cookie"));

				CreateRequestBodyStream(stream);
			}
			catch (Exception ex)
			{
				throw new HttpProcessor.HttpProtocolException("An error occurred when parsing the HTTP request.", ex);
			}
		}
		#region Factory Methods
		/// <summary>
		/// Synchronously reads an HTTP request from the given stream.  Returns null if no request arrives before the stream is closed.
		/// </summary>
		/// <param name="baseUriThisServer">Base URI of this web server, to be included in the Request.Url if the client does not provide an absolute URI in the request.</param>
		/// <param name="stream">Stream to read from.</param>
		/// <returns></returns>
		/// <exception cref="EndOfStreamException">If the end of the stream is encountered before the request headers are fully read.</exception>
		public static SimpleHttpRequest FromStream(Uri baseUriThisServer, Stream stream)
		{
			List<string> lines = ReadHttpHeaderSectionSync(stream);
			if (lines == null)
				return null; // End of stream before a request arrived.  Very common with "Connection: keep-alive" when another request does not arrive.
			SimpleHttpRequest request = new SimpleHttpRequest(baseUriThisServer, lines, stream);
			request.ReadPostForm();
			return request;
		}
		/// <summary>
		/// Asynchronously reads an HTTP request from the given stream.  Returns null if no request arrives before the stream is closed.
		/// </summary>
		/// <param name="baseUriThisServer">Base URI of this web server, to be included in the Request.Url if the client does not provide an absolute URI in the request.</param>
		/// <param name="stream">Stream to read from.</param>
		/// <param name="timeoutMilliseconds">Timeout per read operation, in milliseconds.  This is an important part of implementing connection: keep-alive.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns></returns>
		/// <exception cref="EndOfStreamException">If the end of the stream is encountered before the request headers are fully read.</exception>
		/// <exception cref="OperationCanceledException">If the async operation is cancelled or a timeout occurs while reading the request.</exception>
		public static async Task<SimpleHttpRequest> FromStreamAsync(Uri baseUriThisServer, UnreadableStream stream, int timeoutMilliseconds, CancellationToken cancellationToken = default)
		{
			List<string> lines = await ReadHttpHeaderSectionAsync(stream, timeoutMilliseconds, cancellationToken).ConfigureAwait(false);
			if (lines == null)
				return null; // End of stream before a request arrived.  Very common with "Connection: keep-alive" when another request does not arrive.
			SimpleHttpRequest request = new SimpleHttpRequest(baseUriThisServer, lines, stream);
			await request.ReadPostFormAsync(timeoutMilliseconds, cancellationToken).ConfigureAwait(false);
			return request;
		}
		#endregion
		#region Cleanup Methods
		private void FastCleanupIfPossible()
		{
			if (RequestBodyStream != null)
			{
				if (RequestBodyStream is MemoryStream
					|| (RequestBodyStream is Substream && (RequestBodyStream as Substream).EndOfStream)
					|| (RequestBodyStream is ReadableChunkedTransferEncodingStream && (RequestBodyStream as ReadableChunkedTransferEncodingStream).EndOfStream))
				{
					// Stream is already ended. We're fine.
					RequestBodyStream.Dispose();
					RequestBodyStream = null;
				}
			}
		}
		/// <summary>
		/// Ensures that the RequestBodyStream has been read to the end so the connection can be reused for another request.
		/// </summary>
		internal void CleanupSync()
		{
			FastCleanupIfPossible();
			if (RequestBodyStream != null)
			{
				// Attempt to discard a certain amount of unread request body, to allow for simple web servers to ignore request bodies.
				int bytesToRead = 125000;
				ByteUtil.DiscardToEndResult discardResult = ByteUtil.DiscardUntilEndOfStreamWithMaxLength(RequestBodyStream, bytesToRead);
				RequestBodyStream.Dispose();
				RequestBodyStream = null;
				if (discardResult.EndOfStream)
				{
					if (discardResult.BytesDiscarded > 0)
						SimpleHttpLogger.LogVerbose("Request body was not fully read by the server.  The remainder of the stream was discarded (" + discardResult.BytesDiscarded + " bytes).");
					// else // Nothing was discarded, meaning the server did read the entire request body.
				}
				else
				{
					throw new HttpRequestBodyNotReadException("Request body was not fully read by the server, and there was more than " + StringUtil.FormatNetworkBytes(bytesToRead) + " remaining.  Closing this connection.");
				}
			}
		}
		/// <summary>
		/// Ensures that the RequestBodyStream has been read to the end so the connection can be reused for another request.
		/// </summary>
		/// <param name="timeoutMilliseconds">If greater than 0, the operation will be cancelled if no progress is made for this many milliseconds.  Upon timeout, <see cref="OperationCanceledException"/> will be thrown.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <exception cref="OperationCanceledException">If the operation was cancelled or timed out.</exception>
		/// <exception cref="HttpRequestBodyNotReadException">If the request body was not read, and the request body was too large for this method to discard efficiently.</exception>
		internal async Task CleanupAsync(int timeoutMilliseconds = 5000, CancellationToken cancellationToken = default)
		{
			FastCleanupIfPossible();
			if (RequestBodyStream != null)
			{
				// Attempt to discard a certain amount of unread request body, to allow for simple web servers to ignore request bodies.
				int bytesToRead = 125000;
				ByteUtil.DiscardToEndResult discardResult = await ByteUtil.DiscardUntilEndOfStreamWithMaxLengthAsync(RequestBodyStream, bytesToRead, timeoutMilliseconds, cancellationToken).ConfigureAwait(false);
#if NET6_0
				await RequestBodyStream.DisposeAsync().ConfigureAwait(false);
#else
				RequestBodyStream.Dispose();
#endif
				RequestBodyStream = null;
				if (discardResult.EndOfStream)
				{
					if (discardResult.BytesDiscarded > 0)
						SimpleHttpLogger.LogVerbose("Request body was not fully read by the server.  The remainder of the stream was discarded (" + discardResult.BytesDiscarded + " bytes).");
					// else // Nothing was discarded, meaning the server did read the entire request body.
				}
				else
				{
					throw new HttpRequestBodyNotReadException("Request body was not fully read by the server, and there was more than " + StringUtil.FormatNetworkBytes(bytesToRead) + " remaining.  Closing this connection.");
				}
			}
		}
		#endregion
		#region ReadRequestThroughHeaders
		/// <summary>
		/// Reads lines of text until an empty line is read.  The returned list of strings represents a full request header or response header.  Returns null if the end of stream is encountered before any data is read.
		/// </summary>
		/// <param name="stream">Stream to read from.</param>
		/// <returns>The returned list of strings represents a full request header or response header.  Returns null if the end of stream is encountered before any data is read.</returns>
		/// <exception cref="EndOfStreamException">If the end of the stream is encountered before the full Http Header section is read.</exception>
		private static List<string> ReadHttpHeaderSectionSync(Stream stream)
		{
			List<string> lines = new List<string>();
			while (true)
			{
				string line = ByteUtil.HttpStreamReadLine(stream);
				if (line == null)
				{
					if (lines.Count == 0)
						return null;
					throw new EndOfStreamException();
				}
				else if (line == "")
					return lines;
				lines.Add(line);
			}
		}
		/// <summary>
		/// Reads lines of text until an empty line is read.  The returned list of strings represents a full request header or response header.  Returns null if the end of stream is encountered before any data is read.
		/// </summary>
		/// <param name="stream">Stream to read from.</param>
		/// <param name="timeoutMilliseconds">Read timeout in milliseconds.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns>The returned list of strings represents a full request header or response header.  Returns null if the end of stream is encountered before any data is read.</returns>
		/// <exception cref="EndOfStreamException">If the end of the stream is encountered before the full Http Header section is read.</exception>
		internal static async Task<List<string>> ReadHttpHeaderSectionAsync(UnreadableStream stream, int timeoutMilliseconds, CancellationToken cancellationToken)
		{
			List<string> lines = new List<string>();
			while (true)
			{
				string line = await ByteUtil.HttpStreamReadLineAsync(stream, timeoutMilliseconds, cancellationToken: cancellationToken).ConfigureAwait(false);
				if (line == null)
				{
					if (lines.Count == 0)
						return null;
					throw new EndOfStreamException();
				}
				else if (line == "")
					return lines;
				lines.Add(line);
			}
		}
		#endregion
		#region Request Line and Header Parsing
		private void ParseRequestLine(Uri baseUriThisServer, string requestLine)
		{
			string[] tokens = requestLine.Split(' ');
			if (tokens.Length != 3)
				throw new HttpProtocolException("invalid http request line: " + requestLine);
			HttpMethod = tokens[0].ToUpper();
			if (!HttpMethods.IsValid(HttpMethod))
				throw new HttpProcessorException("501 Not Implemented");
			try
			{
				if (tokens[1].IStartsWith("http://") || tokens[1].IStartsWith("https://") || tokens[1].IStartsWith("ws://") || tokens[1].IStartsWith("wss://"))
					Url = new Uri(tokens[1]);
				else
					Url = new Uri(baseUriThisServer, tokens[1]);
			}
			catch (Exception ex)
			{
				throw new HttpProtocolException("Invalid URL given in http request: " + requestLine, ex);
			}

			Page = Url.AbsolutePath.StartsWith("/") ? Url.AbsolutePath.Substring(1) : Url.AbsolutePath;

			HttpProtocolVersionString = tokens[2];
		}
		/// <summary>
		/// Parses HTTP headers from <paramref name="lines"/> beginning at index 1, placing the headers into <paramref name="Headers"/>.
		/// </summary>
		/// <param name="Headers"></param>
		/// <param name="lines"></param>
		/// <exception cref="HttpProtocolException"></exception>
		internal static void ParseHeaders(HttpHeaderCollection Headers, List<string> lines)
		{
			for (int i = 1; i < lines.Count; i++)
			{
				string line = lines[i];
				int separator = line.IndexOf(':');
				if (separator == -1)
					throw new HttpProtocolException("invalid http header line: " + line);
				string name = line.Substring(0, separator);
				int pos = separator + 1;
				while (pos < line.Length && line[pos] == ' ')
					pos++; // strip any spaces

				string value = line.Substring(pos);
				Headers.Add(name, value);
			}
		}
		#endregion
		#region QueryString/Form Parameter parsing
		/// <summary>
		/// Parses the specified query string and returns a sorted list containing the arguments found in the specified query string.  Can also be used to parse the POST request body if the mimetype is "application/x-www-form-urlencoded".
		/// </summary>
		/// <param name="arguments">SortedList to put parsed arguments into.</param>
		/// <param name="queryString">The string to parse.</param>
		/// <param name="requireQuestionMark">If true, the query string must begin with a question mark. For GET requests.</param>
		/// <param name="convertPlusToSpace">If true, query string argument values will have any plus signs converted to spaces before URL decoding. For POSTed "application/x-www-form-urlencoded".</param>
		/// <returns></returns>
		protected static void ParseQueryStringArguments(SortedList<string, string> arguments, string queryString, bool requireQuestionMark, bool convertPlusToSpace)
		{
			int idx = queryString.IndexOf('?');
			if (idx > -1)
				queryString = queryString.Substring(idx + 1);
			else if (requireQuestionMark)
				return;
			idx = queryString.LastIndexOf('#');
			string hash = null;
			if (idx > -1)
			{
				hash = queryString.Substring(idx + 1);
				queryString = queryString.Remove(idx);
			}
			string[] parts = queryString.Split(new char[] { '&' });
			for (int i = 0; i < parts.Length; i++)
			{
				string[] argument = parts[i].Split(new char[] { '=' });
				if (argument.Length == 2)
				{
					if (convertPlusToSpace)
						argument[1] = argument[1].Replace('+', ' ');
					string key = Uri.UnescapeDataString(argument[0]);
					string existingValue;
					if (arguments.TryGetValue(key, out existingValue))
						arguments[key] += "," + Uri.UnescapeDataString(argument[1]);
					else
						arguments[key] = Uri.UnescapeDataString(argument[1]);
				}
			}
			if (hash != null)
				arguments["#"] = hash;
			return;
		}
		#endregion
		#region QueryString/Form Parameter Retrieval

		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <returns>The value of the key, or empty string if the key does not exist or has no value.</returns>
		public string GetParam(string key)
		{
			return GetQSParam(key);
		}
		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <param name="defaultValue">The default value to return, in case the value did not exist or was not compatible with the data type.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value.</returns>
		public int GetIntParam(string key, int defaultValue = 0)
		{
			return GetQSIntParam(key, defaultValue);
		}
		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <param name="defaultValue">The default value to return, in case the value did not exist or was not compatible with the data type.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value.</returns>
		public long GetLongParam(string key, long defaultValue = 0)
		{
			return GetQSLongParam(key, defaultValue);
		}
		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <param name="defaultValue">The default value to return, in case the value did not exist or was not compatible with the data type.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value.</returns>
		public double GetDoubleParam(string key, double defaultValue = 0)
		{
			return GetQSDoubleParam(key, defaultValue);
		}
		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <returns>The value of the key. This function interprets a value of "1" or "true" (case insensitive) as being true.  Any other parameter value is interpreted as false.</returns>
		public bool GetBoolParam(string key)
		{
			return GetQSBoolParam(key);
		}
		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <returns>The value of the key, or empty string if the key does not exist or has no value.</returns>
		public string GetQSParam(string key)
		{
			if (key == null)
				return "";
			string value;
			if (QueryString.TryGetValue(key.ToLower(), out value))
				return value;
			return "";
		}
		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <param name="defaultValue">The default value to return, in case the value did not exist or was not compatible with the data type.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value.</returns>
		public int GetQSIntParam(string key, int defaultValue = 0)
		{
			if (key == null)
				return defaultValue;
			int value;
			if (int.TryParse(GetQSParam(key.ToLower()), out value))
				return value;
			return defaultValue;
		}
		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <param name="defaultValue">The default value to return, in case the value did not exist or was not compatible with the data type.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value.</returns>
		public long GetQSLongParam(string key, long defaultValue = 0)
		{
			if (key == null)
				return defaultValue;
			long value;
			if (long.TryParse(GetQSParam(key.ToLower()), out value))
				return value;
			return defaultValue;
		}
		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <param name="defaultValue">The default value to return, in case the value did not exist or was not compatible with the data type.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value.</returns>
		public double GetQSDoubleParam(string key, double defaultValue = 0)
		{
			if (key == null)
				return defaultValue;
			double value;
			if (double.TryParse(GetQSParam(key.ToLower()), out value))
				return value;
			return defaultValue;
		}
		/// <summary>
		/// Returns the value of the Query String parameter with the specified key.
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <returns>The value of the key. This function interprets a value of "1" or "true" (case insensitive) as being true.  Any other parameter value is interpreted as false.</returns>
		public bool GetQSBoolParam(string key)
		{
			string param = GetQSParam(key);
			if (param == "1" || param.ToLower() == "true")
				return true;
			return false;
		}
		/// <summary>
		/// Returns the value of a parameter sent via POST with MIME type "application/x-www-form-urlencoded".
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <returns>The value of the key, or empty string if the key does not exist or has no value.</returns>
		public string GetPostParam(string key)
		{
			if (key == null)
				return "";
			string value;
			if (PostParams.TryGetValue(key.ToLower(), out value))
				return value;
			return "";
		}
		/// <summary>
		/// Returns the value of a parameter sent via POST with MIME type "application/x-www-form-urlencoded".
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <param name="defaultValue">The default value to return, in case the value did not exist or was not compatible with the data type.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value.</returns>
		public int GetPostIntParam(string key, int defaultValue = 0)
		{
			if (key == null)
				return defaultValue;
			int value;
			if (int.TryParse(GetPostParam(key.ToLower()), out value))
				return value;
			return defaultValue;
		}
		/// <summary>
		/// Returns the value of a parameter sent via POST with MIME type "application/x-www-form-urlencoded".
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <param name="defaultValue">The default value to return, in case the value did not exist or was not compatible with the data type.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value.</returns>
		public long GetPostLongParam(string key, long defaultValue = 0)
		{
			if (key == null)
				return defaultValue;
			long value;
			if (long.TryParse(GetPostParam(key.ToLower()), out value))
				return value;
			return defaultValue;
		}
		/// <summary>
		/// Returns the value of a parameter sent via POST with MIME type "application/x-www-form-urlencoded".
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <param name="defaultValue">The default value to return, in case the value did not exist or was not compatible with the data type.</param>
		/// <returns>The value of the key, or [defaultValue] if the key does not exist or has no suitable value.</returns>
		public double GetPostDoubleParam(string key, double defaultValue = 0)
		{
			if (key == null)
				return defaultValue;
			double value;
			if (double.TryParse(GetPostParam(key.ToLower()), out value))
				return value;
			return defaultValue;
		}
		/// <summary>
		/// Returns the value of a parameter sent via POST with MIME type "application/x-www-form-urlencoded".
		/// </summary>
		/// <param name="key">A case insensitive key.</param>
		/// <returns>The value of the key. This function interprets a value of "1" or "true" (case insensitive) as being true.  Any other parameter value is interpreted as false.</returns>
		public bool GetPostBoolParam(string key)
		{
			string param = GetPostParam(key);
			if (param == "1" || param.ToLower() == "true")
				return true;
			return false;
		}
		#endregion
		#region ReadRequestBody
		private void CreateRequestBodyStream(Stream stream)
		{
			if (HttpMethod != "TRACE")
			{
				string content_length_str = Headers.Get("Content-Length");
				if (!string.IsNullOrWhiteSpace(content_length_str))
				{
					if (long.TryParse(content_length_str, out long content_len))
					{
						if (content_len < 0)
							throw new HttpProtocolException("Content-Length was not valid.");
						ContentLength = content_len;
						SimpleHttpLogger.LogVerbose("Request body will be read as Substream(" + content_len + ").");
						RequestBodyStream = stream.Substream(content_len);
					}
				}
				else
				{
					string transferEncoding = Headers.Get("Transfer-Encoding");
					if (transferEncoding != null)
					{
						string[] transferEncodingHeaderValues = transferEncoding
							.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
							.Select(s => s.Trim())
							.ToArray();
						if (transferEncodingHeaderValues.Contains("chunked") && transferEncodingHeaderValues.Length == 1) // No support for multiple transfer encodings currently.
						{
							SimpleHttpLogger.LogVerbose("Request body will be read as ReadableChunkedTransferEncodingStream.");
							RequestBodyStream = new ReadableChunkedTransferEncodingStream(stream);
						}
					}
				}
				if (RequestBodyStream == null)
				{
					if (HttpMethod == "POST" || HttpMethod == "PUT" || HttpMethod == "PATCH")
					{
						SimpleHttpLogger.LogVerbose("The request did not specify the length of its content via a method understood by this server.  This server requires that all POST, PUT, and PATCH requests include a \"Content-Length\" header or use \"Transfer-Encoding: chunked\".");
						throw new HttpProtocolException("411 Length Required");
					}
				}
			}
		}
		private void ReadPostForm()
		{
			if (HttpMethod != "TRACE")
			{
				string contentType = Headers.Get("Content-Type");
				if (contentType != null && contentType.IContains("application/x-www-form-urlencoded"))
				{
					if (RequestBodyStream == null)
					{
						SimpleHttpLogger.LogVerbose("The request specified \"Content-Type: application/x-www-form-urlencoded\" but this server was unable to read the request body.");
						throw new HttpProtocolException("411 Length Required");
					}
					bool lengthLimitExceeded = false;
					if (ContentLength != null)
						lengthLimitExceeded = ContentLength > MAX_FORM_SIZE;
					if (!lengthLimitExceeded)
					{
						ByteUtil.ReadToEndResult readResult = ByteUtil.ReadToEndWithMaxLength(RequestBodyStream, MAX_FORM_SIZE);
						if (readResult.EndOfStream)
						{
							string formDataRaw = ByteUtil.Utf8NoBOM.GetString(readResult.Data);
							ParseQueryStringArguments(PostParams, formDataRaw, false, true);
							RequestBodyStream = new MemoryStream(readResult.Data);
						}
						else
							lengthLimitExceeded = true;
					}
					if (lengthLimitExceeded)
					{
						SimpleHttpLogger.LogVerbose("Content-Length (" + ContentLength + ") too big for a \"application/x-www-form-urlencoded\" request.  Server can handle up to " + MAX_FORM_SIZE + " bytes.");
						throw new HttpProtocolException("413 Request Entity Too Large");
					}
				}
			}
		}
		private async Task ReadPostFormAsync(int timeoutMilliseconds, CancellationToken cancellationToken = default)
		{
			string contentType = Headers.Get("Content-Type");
			if (contentType != null && contentType.IContains("application/x-www-form-urlencoded"))
			{
				if (RequestBodyStream == null)
				{
					SimpleHttpLogger.LogVerbose("The request specified \"Content-Type: application/x-www-form-urlencoded\" but this server was unable to read the request body.");
					throw new HttpProtocolException("411 Length Required");
				}
				bool lengthLimitExceeded = false;
				if (ContentLength != null)
					lengthLimitExceeded = ContentLength > MAX_FORM_SIZE;
				if (!lengthLimitExceeded)
				{
					ByteUtil.ReadToEndResult readResult = await ByteUtil.ReadToEndWithMaxLengthAsync(RequestBodyStream, MAX_FORM_SIZE, timeoutMilliseconds, cancellationToken).ConfigureAwait(false);
					if (readResult.EndOfStream)
					{
						string formDataRaw = ByteUtil.Utf8NoBOM.GetString(readResult.Data);
						ParseQueryStringArguments(PostParams, formDataRaw, false, true);
						RequestBodyStream = new MemoryStream(readResult.Data);
					}
					else
						lengthLimitExceeded = true;
				}
				if (lengthLimitExceeded)
				{
					SimpleHttpLogger.LogVerbose("Content-Length (" + ContentLength + ") too big for a \"application/x-www-form-urlencoded\" request.  Server can handle up to " + MAX_FORM_SIZE + " bytes.");
					throw new HttpProtocolException("413 Request Entity Too Large");
				}
			}
		}
		#endregion
		#region Misc API
		/// <summary>
		/// <para>IMPORTANT: If you call this method, you must do it before reading from <see cref="RequestBodyStream"/>, otherwise you will not get the entire request body.</para>
		/// <para>The first call to this method reads the remainder of the request body into a <see cref="MemoryStream"/>, seeks it to Position 0, and assigns it to the <see cref="RequestBodyStream"/> property.</para>
		/// <para>Further calls to this method only return a reference to the existing MemoryStream without changing its seek position.</para>
		/// <para>If there is no request body, this method returns null.</para>
		/// </summary>
		/// <param name="maxLength">[Default: 10 million] Maximium number of bytes to read before aborting. If the request body is larger, an Exception will be thrown.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <exception cref="Exception">Throws if the requesty body is larger than the provided limit.</exception>
		public async Task<MemoryStream> GetRequestBodyMemoryStream(int maxLength = 10 * 1000 * 1000, CancellationToken cancellationToken = default)
		{
			if (RequestBodyStream == null)
				return null;
			if (RequestBodyStream is MemoryStream)
				return (MemoryStream)RequestBodyStream;
			ByteUtil.ReadToEndResult result = await ByteUtil.ReadToEndWithMaxLengthAsync(RequestBodyStream, maxLength, HttpProcessor.readTimeoutSeconds * 1000, cancellationToken).ConfigureAwait(false);
			if (result.EndOfStream)
			{
				MemoryStream ms = new MemoryStream(result.Data);
				RequestBodyStream = ms;
				return ms;
			}
			else
				throw new Exception("Request body was too large (max " + StringUtil.FormatNetworkBytes(maxLength) + ").");
		}
		/// <summary>
		/// Removes the given appPath from the start of the incoming request URL, if it is found there.  Updates the <see cref="Url"/> and <see cref="Page"/> properties.
		/// </summary>
		/// <param name="appPath">AppPath string.</param>
		public void RemoveAppPath(string appPath)
		{
			if (appPath == null)
				return;
			string ap = "/" + appPath.Trim('/', ' ', '\r', '\n', '\t');
			if (ap == "/")
				return;
			if (Url.AbsolutePath.IStartsWith(ap))
			{
				UriBuilder uriBuilder = new UriBuilder(Url);
				uriBuilder.Path = Url.AbsolutePath.Substring(ap.Length);
				Url = uriBuilder.Uri;
				Page = Url.AbsolutePath.StartsWith("/") ? Url.AbsolutePath.Substring(1) : Url.AbsolutePath;
			}
		}
		#endregion
	}
}
