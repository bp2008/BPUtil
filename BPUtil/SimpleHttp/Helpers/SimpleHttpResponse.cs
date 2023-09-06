using BPUtil.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp
{
	/// <summary>
	/// <para>Allows HTTP servers to configure an HTTP response.</para>
	/// <para>When finished, clean up via <see cref="FinishSync"/> or <see cref="FinishAsync"/> otherwise it is likely that the response will not have been written to the network stream.</para>
	/// </summary>
	public class SimpleHttpResponse
	{
		/// <summary>
		/// The HttpProcessor being used to handle the request.
		/// </summary>
		private HttpProcessor p;
		/// <summary>
		/// The response stream that has been created for the user.  May be null if the complete response body has already been written.
		/// </summary>
		private Stream responseStream;
		/// <summary>
		/// The type of compression that will be used for the response stream.
		/// </summary>
		private CompressionType compressionType = CompressionType.None;
		/// <summary>
		/// If true, Connection: keep-alive will be prevented.
		/// </summary>
		private bool preventKeepalive = false;
		private string _statusString = "404 Not Found";
		/// <summary>
		/// If this is not null when the response header is written, then it will be written as the response body at that time.  Afterward, the response body stream will be null.
		/// </summary>
		private byte[] bodyContent = null;
		private byte? _keepAliveTimeSeconds = null;
		private bool _checkAsyncUsage = true;
		#region Internal response stream references
		private Substream _substream;
		private GZipStream _gzipstream;
		private WritableChunkedTransferEncodingStream _chunkedstream;
		#endregion
		#region Public properties
		/// <summary>
		/// A flag that is set when the response header is written, which prevents an additional response header from being written accidentally.
		/// </summary>
		public bool ResponseHeaderWritten { get; internal set; } = false;
		/// <summary>
		/// The HTTP Status string to use in the response.  If unspecified, the default is "404 Not Found".
		/// </summary>
		public string StatusString
		{
			get
			{
				return _statusString;
			}
			set
			{
				if (ResponseHeaderWritten)
					throw new ApplicationException("The response header was already written.");
				int? responseStatusInt = NumberUtil.FirstInt(value);
				if (responseStatusInt == null)
					throw new ArgumentException("HTTP Response status string is an incorrect format: " + value, "StatusString");
				_statusString = value;
			}
		}
		/// <summary>
		/// A collection of new cookies to send to the remote client.
		/// </summary>
		public Cookies Cookies { get; } = new Cookies();
		/// <summary>
		/// <para>HTTP headers to include in the response.</para>
		/// <para>It is recommended to not get/set the "Set-Cookie" header directly via this collection.  Instead use the <see cref="Cookies"/> object.</para>
		/// <para>It is not safe to use the "Transfer-Encoding" header via this collection.</para>
		/// <para>It is not safe to use the "Content-Encoding" header via this collection if response compression has been enabled.</para>
		/// </summary>
		public HttpHeaderCollection Headers { get; private set; } = new HttpHeaderCollection();

		/// <summary>
		/// The length of the response body in bytes.  May be null if unspecified.  Setting a negative value is equivalent to setting null.
		/// </summary>
		public long? ContentLength
		{
			get
			{
				string l = Headers["Content-Length"];
				if (l == null)
					return null;
				if (long.TryParse(l, out long len) && len >= 0)
					return len;
				else
					throw new ApplicationException("The Content-Length header was not formatted correctly as an integer number >= 0: \"" + l + "\"");
			}
			set
			{
				Headers["Content-Length"] = value == null || value < 0 ? null : value.ToString();
			}
		}
		/// <summary>
		/// The Content-Type header for the response.  May be null if unspecified.
		/// </summary>
		public string ContentType
		{
			get
			{
				return Headers["Content-Type"];
			}
			set
			{
				if (ResponseHeaderWritten)
					throw new ApplicationException("The response header was already written.");
				Headers["Content-Type"] = value;
			}
		}
		/// <summary>
		/// <para>The number of seconds which the connection should be kept alive for.</para>
		/// <para>If zero, the connection should be closed when the response is written.</para>
		/// <para>This value is dependent on whether the client supports keep-alive, the current server load, and whether or not <see cref="PreventKeepalive"/> has been called.</para>
		/// </summary>
		public byte KeepaliveTimeSeconds
		{
			get
			{
				if (preventKeepalive)
					return 0;
				if (_keepAliveTimeSeconds == null)
					_keepAliveTimeSeconds = GetKeepAliveTimeSeconds();
				return _keepAliveTimeSeconds.Value;
			}
		}
		/// <summary>
		/// <para>If this is not null when the response header is written, then it will be written as the response body at that time.  Afterward, the response body stream will be null.</para>
		/// <para>Setting this value also sets the Content-Length header, either to the length of the byte array, or to null.</para>
		/// </summary>
		public byte[] BodyContent
		{
			get
			{
				return bodyContent;
			}
			set
			{
				bodyContent = value;
				ContentLength = value?.Length;
			}
		}
		#endregion
		internal SimpleHttpResponse(HttpProcessor p)
		{
			this.p = p;
		}
		#region All-In-One Response Methods

		#region Simple (equivalent to writeFailure)
		/// <summary>
		/// <para>Resets the response and configures a simple text-based response. Mainly used for error responses.</para>
		/// <para>Does not perform any I/O.  The response will be written later.</para>
		/// </summary>
		/// <param name="StatusString">The HTTP Status String, e.g. "404 Not Found".</param>
		/// <param name="Description">Optional simple text body. If null, the status string will be written again as the response body.</param>
		public void Simple(string StatusString, string Description = null)
		{
			if (Description == null)
				Description = StatusString;

			FullResponseUTF8(Description, "text/plain; charset=utf-8", StatusString);
		}
		#endregion
		#region Redirect
		/// <summary>
		/// <para>Resets the response, then synchronously writes a redirect header instructing the remote user's browser to load the URL you specify.</para>
		/// <para>This completes the response.</para>
		/// </summary>
		/// <param name="redirectToUrl">URL to redirect to.</param>
		public void Redirect(string redirectToUrl)
		{
			_Prep_Redirect(redirectToUrl);
			FinishSync();
		}
		/// <summary>
		/// <para>Resets the response, then asynchronously writes a redirect header instructing the remote user's browser to load the URL you specify.</para>
		/// <para>This completes the response.</para>
		/// </summary>
		/// <param name="redirectToUrl">URL to redirect to.</param>
		/// <param name="cancellationToken">Cancellation Token.</param>
		public Task RedirectAsync(string redirectToUrl, CancellationToken cancellationToken = default)
		{
			_Prep_Redirect(redirectToUrl);
			return FinishAsync(cancellationToken);
		}
		private void _Prep_Redirect(string redirectToUrl)
		{
			Reset("302 Found");
			Headers["Location"] = redirectToUrl;
			this.PreventKeepalive();
		}
		#endregion
		#region Static File
		/// <summary>
		/// <para>Synchronously writes a static file response with built-in caching support.</para>
		/// <para>This completes the response.</para>
		/// </summary>
		/// <param name="filePath">Path to the file on disk.</param>
		/// <param name="contentTypeOverride">If provided, this is the value of the Content-Type header to be sent in the response.  If null or empty, it will be determined from the file extension.</param>
		/// <param name="canCache">If true, caching is provided for supported file extensions based on ETag or Last-Modified date.</param>
		public void StaticFile(string filePath, string contentTypeOverride = null, bool canCache = true)
		{
			if (_checkAsyncUsage && p.IsAsync)
				throw new ApplicationException("This HttpProcessor is not in blocking/synchronous mode.");
			if (ResponseHeaderWritten)
				throw new ApplicationException("The response header was already written.");
			StaticFile(new FileInfo(filePath), contentTypeOverride, canCache);
		}

		/// <summary>
		/// <para>Asynchronously writes a static file response with built-in caching support.</para>
		/// <para>This completes the response.</para>
		/// </summary>
		/// <param name="filePath">Path to the file on disk.</param>
		/// <param name="contentTypeOverride">If provided, this is the value of the Content-Type header to be sent in the response.  If null or empty, it will be determined from the file extension.</param>
		/// <param name="canCache">If true, caching is provided for supported file extensions based on ETag or Last-Modified date.</param>
		/// <param name="cancellationToken">Cancellation Token.</param>
		public Task StaticFileAsync(string filePath, string contentTypeOverride = null, bool canCache = true, CancellationToken cancellationToken = default)
		{
			if (_checkAsyncUsage && !p.IsAsync)
				throw new ApplicationException("This HttpProcessor is not in async mode.");
			if (ResponseHeaderWritten)
				throw new ApplicationException("The response header was already written.");
			return StaticFileAsync(new FileInfo(filePath), contentTypeOverride, canCache, cancellationToken);
		}
		/// <summary>
		/// <para>Synchronously writes a static file response with built-in caching support.</para>
		/// <para>This completes the response.</para>
		/// </summary>
		/// <param name="fi">File on disk.</param>
		/// <param name="contentTypeOverride">If provided, this is the value of the Content-Type header to be sent in the response.  If null or empty, it will be determined from the file extension.</param>
		/// <param name="canCache">If true, caching is provided for supported file extensions based on ETag or Last-Modified date.</param>
		public void StaticFile(FileInfo fi, string contentTypeOverride = null, bool canCache = true)
		{
			if (_checkAsyncUsage && p.IsAsync)
				throw new ApplicationException("This HttpProcessor is not in blocking/synchronous mode.");

			if (!fi.Exists)
			{
				Simple("404 Not Found", null);
				return;
			}

			bool cacheHit = false;
			bool isCacheable = SetupStaticFileCommonHeaders(fi, contentTypeOverride, canCache);

			if (isCacheable)
			{
				Headers["ETag"] = MakeETagSync(fi);

				// If-None-Match
				// Succeeds if the ETag of the distant resource is different to each listed in this header. It performs a weak validation.
				string IfNoneMatch = p.Request.Headers.Get("If-None-Match");
				if (IfNoneMatch != null)
				{
					if (Headers["ETag"] == IfNoneMatch)
						cacheHit = true;
				}
				else
				{
					string IfModifiedSince = p.Request.Headers.Get("If-Modified-Since");
					if (IfModifiedSince != null && IfModifiedSince.IEquals(Headers["Last-Modified"]))
						cacheHit = true;
				}
			}

			if (cacheHit)
			{
				StatusString = "304 Not Modified";
			}
			else
			{
				ContentLength = fi.Length;
				WriteResponseHeaderIfNeededSync();
				using (FileStream fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					fs.CopyTo(responseStream, 81920);
			}

			FinishSync();
		}
		/// <summary>
		/// <para>Asynchronously writes a static file response with built-in caching support.</para>
		/// <para>This completes the response.</para>
		/// </summary>
		/// <param name="fi">File on disk.</param>
		/// <param name="contentTypeOverride">If provided, this is the value of the Content-Type header to be sent in the response.  If null or empty, it will be determined from the file extension.</param>
		/// <param name="canCache">If true, caching is provided for supported file extensions based on ETag or Last-Modified date.</param>
		/// <param name="cancellationToken">Cancellation Token.</param>
		public async Task StaticFileAsync(FileInfo fi, string contentTypeOverride = null, bool canCache = true, CancellationToken cancellationToken = default)
		{
			if (_checkAsyncUsage && !p.IsAsync)
				throw new ApplicationException("This HttpProcessor is not in async mode.");

			if (!fi.Exists)
			{
				Simple("404 Not Found", null);
				return;
			}

			bool cacheHit = false;
			bool isCacheable = SetupStaticFileCommonHeaders(fi, contentTypeOverride, canCache);

			if (isCacheable)
			{
				Headers["ETag"] = await MakeETagAsync(fi).ConfigureAwait(false);

				// If-None-Match
				// Succeeds if the ETag of the distant resource is different to each listed in this header. It performs a weak validation.
				string IfNoneMatch = p.Request.Headers.Get("If-None-Match");
				if (IfNoneMatch != null)
				{
					if (Headers["ETag"] == IfNoneMatch)
						cacheHit = true;
				}
				else
				{
					string IfModifiedSince = p.Request.Headers.Get("If-Modified-Since");
					if (IfModifiedSince != null && IfModifiedSince.IEquals(Headers["Last-Modified"]))
						cacheHit = true;
				}
			}

			if (cacheHit)
			{
				StatusString = "304 Not Modified";
			}
			else
			{
				ContentLength = fi.Length;
				await WriteResponseHeaderIfNeededAsync(cancellationToken).ConfigureAwait(false);
				using (FileStream fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					await fs.CopyToAsync(responseStream, 81920, cancellationToken).ConfigureAwait(false);
			}

			await FinishAsync(cancellationToken).ConfigureAwait(false);
		}

		private bool SetupStaticFileCommonHeaders(FileInfo fi, string contentTypeOverride, bool canCache)
		{
			Reset("200 OK");
			Headers["Content-Type"] = contentTypeOverride != null ? contentTypeOverride : Mime.GetMimeType(fi.Extension);
			if (canCache && p.srv.CanCacheFileExtension(fi.Extension))
			{
				Headers["Date"] = DateTime.UtcNow.ToString("R");
				Headers["Last-Modified"] = fi.GetLastWriteTimeUtcAndRepairIfBroken().ToString("R");
				Headers["Age"] = "0";
				Headers["Cache-Control"] = "max-age=604800, public";
				return true;
			}
			else
			{
				Headers["Cache-Control"] = "no-cache"; // Caching is technically allowed, but the user agent should always revalidate to ensure the cached value is not stale.
				return false;
			}
		}

		/// <summary>
		/// Creates an ETag for the given file, which is a string that is expected to be the same when the file content is the same, and different when the file content is different.  If an error occurs, this method will throw an exception, not return null.
		/// </summary>
		/// <param name="fi">FileInfo representing the file for which an ETag should be generated.  It is required that this file exists and is readable.</param>
		/// <returns></returns>
		private string MakeETagSync(FileInfo fi)
		{
			const long oneMillion = 1000000;
			// Use SHA1 hash. It is faster than MD5 or xxHash.
			using (System.Security.Cryptography.SHA1 sha = System.Security.Cryptography.SHA1.Create())
			{
				using (FileStream fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					Stream stream;
					if (fi.Length > 3 * oneMillion) // > 3MB
					{
						// ETag structure: Hash of [first 1MB, last 1MB, file size, file last modified date]
						stream = new ConcatenatedStream(idx =>
						{
							if (idx == 0)
							{
								return fs.Substream(oneMillion);
							}
							else if (idx == 1)
							{
								fs.Seek(-oneMillion, SeekOrigin.End);
								return fs.Substream(oneMillion);
							}
							else if (idx == 2)
							{
								byte[] buf = new byte[16];
								ByteUtil.WriteInt64(fi.Length, buf, 0);
								ByteUtil.WriteInt64(TimeUtil.GetTimeInMsSinceEpoch(fi.GetLastWriteTimeUtcAndRepairIfBroken()), buf, 8);
								return new MemoryStream(buf);
							}
							else
								return null;
						});
					}
					else
					{
						// ETag structure: Hash of [file content]
						stream = fs;
					}

					byte[] hash = sha.ComputeHash(stream);
					return Base64UrlMod.ToBase64UrlMod(hash);
				}
			}
		}
		/// <summary>
		/// Creates an ETag for the given file, which is a string that is expected to be the same when the file content is the same, and different when the file content is different.  If an error occurs, this method will throw an exception, not return null.
		/// </summary>
		/// <param name="fi">FileInfo representing the file for which an ETag should be generated.  It is required that this file exists and is readable.</param>
		/// <param name="cancellationToken">Cancellation Token.</param>
		/// <returns></returns>
		private async Task<string> MakeETagAsync(FileInfo fi, CancellationToken cancellationToken = default)
		{
			const long oneMillion = 1000000;
			// Use SHA1 hash. It is faster than MD5 or xxHash.
			using (System.Security.Cryptography.SHA1 sha = System.Security.Cryptography.SHA1.Create())
			{
				using (FileStream fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					Stream stream;
					if (fi.Length > 3 * oneMillion) // > 3MB
					{
						// ETag structure: Hash of [first 1MB, last 1MB, file size, file last modified date]
						stream = new ConcatenatedStream(idx =>
						{
							if (idx == 0)
							{
								return fs.Substream(oneMillion);
							}
							else if (idx == 1)
							{
								fs.Seek(-oneMillion, SeekOrigin.End);
								return fs.Substream(oneMillion);
							}
							else if (idx == 2)
							{
								byte[] buf = new byte[16];
								ByteUtil.WriteInt64(fi.Length, buf, 0);
								ByteUtil.WriteInt64(TimeUtil.GetTimeInMsSinceEpoch(fi.GetLastWriteTimeUtcAndRepairIfBroken()), buf, 8);
								return new MemoryStream(buf);
							}
							else
								return null;
						});
					}
					else
					{
						// ETag structure: Hash of [file content]
						stream = fs;
					}
#if NET6_0
					byte[] hash = await sha.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
#else
					byte[] hash = await TaskHelper.RunBlockingCodeSafely(() => sha.ComputeHash(stream), cancellationToken).ConfigureAwait(false);
#endif
					return Base64UrlMod.ToBase64UrlMod(hash);
				}
			}
		}
		#endregion
		#region Full Response

		/// <summary>
		/// <para>Resets the response and configures the response to deliver the specified payload.</para>
		/// <para>Does not perform any I/O.  The response will be written later.</para>
		/// </summary>
		/// <param name="body">Data to send in the response.  This string will be encoded as UTF8.  Must be non-null.</param>
		/// <param name="contentType">Content-Type header value. e.g. "text/html; charset=utf-8".  If null, the header is omitted.</param>
		/// <param name="responseCode">(OPTIONAL) The response code and optional status string.</param>
		public void FullResponseUTF8(string body, string contentType, string responseCode = "200 OK")
		{
			FullResponseBytes(ByteUtil.Utf8NoBOM.GetBytes(body), contentType, responseCode);
		}
		/// <summary>
		/// <para>Configures the specified response with the Content-Length header set appropriately.</para>
		/// <para>Clears the Headers collection and sets ContentLength and ContentType.</para>
		/// <para>Does not perform any I/O.  The response will be written later.</para>
		/// </summary>
		/// <param name="body">Data to send in the response.  Must be non-null.</param>
		/// <param name="contentType">Content-Type header value. e.g. "application/octet-stream".  If null, the header is omitted.</param>
		/// <param name="responseCode">(OPTIONAL) The response code and optional status string.</param>
		public void FullResponseBytes(byte[] body, string contentType, string responseCode = "200 OK")
		{
			if (body == null)
				throw new ArgumentNullException(nameof(body));
			Reset(responseCode);
			ContentLength = body.Length;
			ContentType = contentType;
			bodyContent = body;
		}

		#endregion
		#region WebSocket Upgrade
		/// <summary>
		/// <para>Resets the response, then writes response headers to finish the WebSocket handshake with the client. No extensions are supported (such as compression) at this time.</para>
		/// <para>Afterward, the tcpStream will be ready to hand over to the WebSocket, and this Response object will be finished.</para>
		/// </summary>
		public void WebSocketUpgradeSync()
		{
			_checkAsyncUsage = false;
			_Prep_WebSocketUpgrade();
			FinishSync();
		}

		/// <summary>
		/// <para>Resets the response, then writes response headers to finish the WebSocket handshake with the client. No extensions are supported (such as compression) at this time.</para>
		/// <para>Afterward, the tcpStream will be ready to hand over to the WebSocket, and this Response object will be finished.</para>
		/// </summary>
		/// <param name="cancellationToken">Cancellation Token</param>
		public Task WebSocketUpgradeAsync(CancellationToken cancellationToken = default)
		{
			if (_checkAsyncUsage && !p.IsAsync)
				throw new ApplicationException("This HttpProcessor is not in async mode.");
			_Prep_WebSocketUpgrade();
			return FinishAsync(cancellationToken);
		}
		private void _Prep_WebSocketUpgrade()
		{
			Reset("101 Switching Protocols");
			Headers["Upgrade"] = "websocket";
			Headers["Sec-WebSocket-Accept"] = WebSockets.WebSocket.CreateSecWebSocketAcceptValue(p.Request.Headers.Get("sec-websocket-key"));
		}
		#endregion
		#region Set (equivalent to legacy writeSuccess)
		/// <summary>
		/// <para>Resets the response, then sets the ContentType, ContentLength, StatusString, and Headers. Equivalent to the legacy `writeSuccess` method.</para>
		/// <para>Does not perform any I/O.  The response will be written later.</para>
		/// </summary>
		/// <param name="contentType">Content-Type header value. Null to remove the header.</param>
		/// <param name="contentLength">Content-Length header value.  Null or negative to remove the header.</param>
		/// <param name="responseStatus">HTTP response status string, E.g. "200 OK" or "404 Not Found".</param>
		/// <param name="additionalResponseHeaders">The current collection of headers is cleared and assigned copies of all the headers in this collection.</param>
		public void Set(string contentType, int? contentLength = null, string responseStatus = "200 OK", HttpHeaderCollection additionalResponseHeaders = null)
		{
			Reset(responseStatus);

			if (additionalResponseHeaders != null)
				foreach (HttpHeader header in additionalResponseHeaders)
					Headers.Add(header);

			ContentType = contentType;
			ContentLength = contentLength;
		}
		#endregion
		#region CloseWithoutResponse
		/// <summary>
		/// <para>Resets the response and configures it so the HTTP connection will close without writing a response.</para>
		/// <para>Prevents future changes to the response.</para>
		/// <para>Does not perform any I/O.  The response will be written later.</para>
		/// </summary>
		public void CloseWithoutResponse()
		{
			Reset();
			this.PreventKeepalive();
			ResponseHeaderWritten = true;
		}
		#endregion

		#endregion
		#region Get Response Stream
		/// <summary>
		/// Ensures that the response header is written and returns the stream which you can write a response to.  May be null if the complete response body has already been written.
		/// </summary>
		/// <returns></returns>
		public Stream GetResponseStreamSync()
		{
			if (_checkAsyncUsage && p.IsAsync)
				throw new ApplicationException("This HttpProcessor is not in blocking/synchronous mode.");
			if (!ResponseHeaderWritten)
				WriteResponseHeader();
			return responseStream;
		}
		/// <summary>
		/// Ensures that the response header is written and returns the stream which you can write a response to.  May be null if the complete response body has already been written.
		/// </summary>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns></returns>
		public async Task<Stream> GetResponseStreamAsync(CancellationToken cancellationToken = default)
		{
			if (_checkAsyncUsage && !p.IsAsync)
				throw new ApplicationException("This HttpProcessor is not in async mode.");
			if (!ResponseHeaderWritten)
				await WriteResponseHeaderAsync(cancellationToken).ConfigureAwait(false);
			return responseStream;
		}
		#endregion
		#region Build Response Header
		/// <summary>
		/// <para>Creates the response header (the string which is written before the response body).</para>
		/// <para>It consists of the status line and HTTP headers followed by an empty line.</para>
		/// <para>If this method is called more than once per response, an ApplicationException is thrown.</para>  
		/// </summary>
		/// <param name="chunkedTransferEncoding">(Output) True if chunked transfer encoding needs to be used to write a response body.</param>
		/// <returns>The response header.</returns>
		/// <exception cref="ApplicationException">If the response header can not be generated due to an error.</exception>
		private byte[] GetResponseHeader(out bool chunkedTransferEncoding)
		{
			if (ResponseHeaderWritten)
				throw new ApplicationException("A response has already been written to this stream.");
			ResponseHeaderWritten = true;

			int? responseStatusInt = NumberUtil.FirstInt(StatusString);
			if (responseStatusInt == null)
				throw new ApplicationException("Response code was not provided correctly in " + nameof(StatusString) + ": " + StatusString);

			if (p.Request.HttpMethod == "HEAD")
				bodyContent = null;
			if (bodyContent != null)
				ContentLength = bodyContent.Length;

			bool allowResponseBody = HttpServerBase.HttpStatusCodeCanHaveResponseBody(responseStatusInt.Value);
			if (!allowResponseBody)
			{
				if (ContentLength.HasValue && ContentLength > 0)
					throw new ApplicationException("The response status " + responseStatusInt + " does not allow a response body, but " + nameof(ContentLength) + " " + ContentLength + " was argued.");
			}

			StringBuilder sb = new StringBuilder();
			HashSet<string> reservedHeaderKeys = new HashSet<string>(new string[] { "Connection", "Keep-Alive" });

			sb.AppendLineRN("HTTP/1.1 " + StatusString);
			if (Headers["Upgrade"] == "websocket")
			{
				PreventKeepalive();
				sb.AppendLineRN("Connection: upgrade");
			}
			else if (KeepaliveTimeSeconds > 0)
			{
				sb.AppendLineRN("Connection: keep-alive");
				sb.AppendLineRN("Keep-Alive: timeout=" + (KeepaliveTimeSeconds - 1).Clamp(1, 60));
			}
			else
				sb.AppendLineRN("Connection: close");
			chunkedTransferEncoding = allowResponseBody && KeepaliveTimeSeconds > 0 && ContentLength == null;
			if (chunkedTransferEncoding)
				WriteReservedHeader(sb, reservedHeaderKeys, "Transfer-Encoding", "chunked");
			if (compressionType == CompressionType.GZip)
				WriteReservedHeader(sb, reservedHeaderKeys, "Content-Encoding", "gzip");
			string cookieStr = Cookies.ToString();
			if (!string.IsNullOrEmpty(cookieStr))
			{
				reservedHeaderKeys.Add("Set-Cookie");
				sb.AppendLineRN(cookieStr);
			}
			foreach (HttpHeader header in Headers)
			{
				if (reservedHeaderKeys.Contains(header.Key, true))
					throw new ApplicationException("SimpleHttpResponse.GetResponseHeader() HTTP Headers conflict: Header \"" + header.Key + "\" is already predetermined for this response.");
				sb.AppendLineRN(header.Key + ": " + header.Value);
			}
			sb.AppendLineRN("");
			return ByteUtil.Utf8NoBOM.GetBytes(sb.ToString());
		}
		private void WriteReservedHeader(StringBuilder sb, HashSet<string> reservedHeaderKeys, string key, string value)
		{
			reservedHeaderKeys.Add(key);
			sb.AppendLineRN(key + ": " + value);
		}
		private byte GetKeepAliveTimeSeconds()
		{
			bool keepAliveRequested = p.Request.HttpProtocolVersionString == "HTTP/1.1";
			if (p.Request.ConnectionHeaderValues != null)
			{
				if (p.Request.ConnectionHeaderValues.Contains("keep-alive", true))
					keepAliveRequested = true;
				if (p.Request.ConnectionHeaderValues.Contains("close", true))
					keepAliveRequested = false;
			}
			return keepAliveRequested && !p.ServerIsUnderHighLoad ? HttpProcessor.readTimeoutSeconds : (byte)0;
		}
		#endregion
		#region Write Response Header
		/// <summary>
		/// Writes the response header.  If <see cref="bodyContent"/> is not null, the complete response body is also written and <see cref="responseStream"/> is set to null.
		/// </summary>
		private void WriteResponseHeader()
		{
			byte[] responseHeader = GetResponseHeader(out bool chunkedTransferEncoding);
			p.tcpStream.Write(responseHeader, 0, responseHeader.Length);

			PrepareResponseStream(chunkedTransferEncoding);

			if (responseStream != null && responseStream != p.tcpStream)
				p.tcpStream.Flush(); // Flush the tcpStream to ensure that future writes to responseStream are not out of order.

			if (bodyContent != null)
			{
				responseStream.Write(bodyContent, 0, bodyContent.Length);
				FinishSync();
			}
		}
		/// <summary>
		/// Writes the response header.  If <see cref="bodyContent"/> is not null, the complete response body is also written and <see cref="responseStream"/> is set to null.
		/// </summary>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns>Returns a task that completes when the writing is finished.</returns>
		private async Task WriteResponseHeaderAsync(CancellationToken cancellationToken = default)
		{
			byte[] responseHeader = GetResponseHeader(out bool chunkedTransferEncoding);
			await p.tcpStream.WriteAsync(responseHeader, 0, responseHeader.Length, cancellationToken).ConfigureAwait(false);

			PrepareResponseStream(chunkedTransferEncoding);

			if (responseStream != null && responseStream != p.tcpStream)
				await p.tcpStream.FlushAsync(cancellationToken).ConfigureAwait(false); // Flush the tcpStream to ensure that future writes to responseStream are not out of order.

			if (bodyContent != null)
			{
				if (!(responseStream is Substream))
					throw new ApplicationException("!(responseStream is Substream)");
				await responseStream.WriteAsync(bodyContent, 0, bodyContent.Length, cancellationToken).ConfigureAwait(false);
				await FinishAsync(cancellationToken).ConfigureAwait(false);
			}
		}
		/// <summary>
		/// Called by the HttpProcessor after control returns from the Http Server to ensure that the configured response was written.
		/// </summary>
		internal void WriteResponseHeaderIfNeededSync()
		{
			if (_checkAsyncUsage && p.IsAsync)
				throw new ApplicationException("This HttpProcessor is not in blocking/synchronous mode.");
			if (!ResponseHeaderWritten)
				WriteResponseHeader();
		}
		/// <summary>
		/// Called by the HttpProcessor after control returns from the Http Server to ensure that the configured response was written.
		/// </summary>
		/// <param name="cancellationToken">Cancellation Token</param>
		internal Task WriteResponseHeaderIfNeededAsync(CancellationToken cancellationToken = default)
		{
			if (_checkAsyncUsage && !p.IsAsync)
				throw new ApplicationException("This HttpProcessor is not in async mode.");
			if (!ResponseHeaderWritten)
				return WriteResponseHeaderAsync(cancellationToken);
			return TaskHelper.CompletedTask;
		}
		#endregion
		#region Response Compression
		/// <summary>
		/// Automatically compresses the response body using gzip encoding, if the client requested it.
		/// Must be called BEFORE the response header is written.
		/// Note that the Content-Length header, if provided, should be the COMPRESSED length, so you likely won't know what value to use.  Omit the header instead.
		/// Returns true if the response will be compressed, and sets this.compressionType.
		/// </summary>
		/// <returns></returns>
		public virtual bool CompressResponseIfCompatible()
		{
			if (ResponseHeaderWritten)
				return false;
			if (p.Request.ClientRequestsGZipCompression)
			{
				compressionType = CompressionType.GZip;
				return true;
			}
			return false;
		}
		#endregion
		#region Create Response Stream
		/// <summary>
		/// Prepares the response body stream and assigns it to <see cref="responseStream"/>.
		/// </summary>
		/// <param name="chunkedTransferEncoding"></param>
		/// <returns></returns>
		/// <exception cref="ApplicationException"></exception>
		internal void PrepareResponseStream(bool chunkedTransferEncoding)
		{
			if (!ResponseHeaderWritten)
				throw new ApplicationException("Can't prepare response body stream before the response header is written.");
			if (responseStream != null)
				throw new ApplicationException("The response body stream was already prepared.");
			if (p.Request.HttpMethod == "HEAD")
				return; // No response should be written for "HEAD", therefore we should not wrap the output streams.

			Stream r = p.tcpStream;

			if (ContentLength != null && ContentLength > 0)
				r = _substream = r.Substream(ContentLength.Value); // This will throw an exception if we write too many bytes, and gives the Cleanup method a way to know if we did not write enough bytes.

			if (compressionType == CompressionType.GZip)
				r = _gzipstream = new GZipStream(r, CompressionLevel.Optimal, true);

			if (chunkedTransferEncoding)
				r = _chunkedstream = new WritableChunkedTransferEncodingStream(r);

			responseStream = r;
		}
		#endregion
		#region Cleanup Methods
		/// <summary>
		/// Ensures that the response header is written, the response is terminated correctly, and that all buffered response data is written.
		/// </summary>
		/// <exception cref="ApplicationException">If the server failed to write the promised number of bytes to the response body.</exception>
		public void FinishSync()
		{
			if (_checkAsyncUsage && p.IsAsync)
				throw new ApplicationException("This HttpProcessor is not in blocking/synchronous mode.");

			WriteResponseHeaderIfNeededSync();

			if (responseStream == null)
				return;

			if (_chunkedstream != null)
			{
				_chunkedstream.Close();
				_chunkedstream = null;
			}

			if (_gzipstream != null)
			{
				_gzipstream.Dispose();
				_gzipstream = null;
			}

			if (_substream != null && !_substream.EndOfStream)
				throw new ApplicationException("The HTTP server failed to write all " + ContentLength + " bytes of the response body.");

			p.tcpStream.Flush();

			responseStream = null;
		}
		/// <summary>
		/// Ensures that the response header is written, the response is terminated correctly, and that all buffered response data is written.
		/// </summary>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns></returns>
		/// <exception cref="ApplicationException">If the server failed to write the promised number of bytes to the response body.</exception>
		public async Task FinishAsync(CancellationToken cancellationToken = default)
		{
			if (_checkAsyncUsage && !p.IsAsync)
				throw new ApplicationException("This HttpProcessor is not in async mode.");

			await WriteResponseHeaderIfNeededAsync(cancellationToken).ConfigureAwait(false);

			if (responseStream == null)
				return;

			if (_chunkedstream != null)
			{
				await _chunkedstream.CloseAsync(cancellationToken).ConfigureAwait(false);
				_chunkedstream = null;
			}

			if (_gzipstream != null)
			{
				// It is safe to dispose GZipStream because it was created with the leaveOpen option.
#if NET6_0
				await _gzipstream.DisposeAsync().ConfigureAwait(false);
#else
				_gzipstream.Dispose();
#endif
				_gzipstream = null;
			}

			if (_substream != null && !_substream.EndOfStream)
				throw new ApplicationException("The HTTP server failed to write all " + ContentLength + " bytes of the response body. bodyContent?.Length: " + bodyContent?.Length);

			await p.tcpStream.FlushAsync().ConfigureAwait(false);

			responseStream = null;
		}
		#endregion
		#region Misc API
		/// <summary>
		/// <para>Call this method to forcibly set <see cref="KeepaliveTimeSeconds"/> to 0, which effectively causes the connection to close when the response is complete.</para>
		/// <para>If this method is called before the response header is written, it will also prevent "Connection: keep-alive" from being sent.</para>
		/// </summary>
		public void PreventKeepalive()
		{
			preventKeepalive = true;
		}
		/// <summary>
		/// Assigns a new <see cref="StatusString"/>, unsets <see cref="bodyContent"/>, and clears <see cref="Headers"/>. If the response header was already written, throws ApplicationException. 
		/// </summary>
		/// <param name="httpStatusString">Assigns this value to <see cref="StatusString"/>.</param>
		/// <exception cref="ApplicationException">Throws if the response header was already written.</exception>
		private void Reset(string httpStatusString = "404 Not Found")
		{
			if (ResponseHeaderWritten)
				throw new ApplicationException("The response header was already written.");
			StatusString = httpStatusString;
			bodyContent = null;
			Headers.Clear();
		}
		/// <inheritdoc/>
		public override string ToString()
		{
			if (!ResponseHeaderWritten)
				return "Nothing written yet (currently configured as " + StatusString + ")";
			if (Headers.Get("Upgrade") == "websocket")
				return "WebSocket";
			Stream rs = responseStream;
			if (rs != null)
			{
				if (rs is Substream)
				{
					Substream s = (Substream)rs;
					return StatusString + ", BODY: " + s.Length + " bytes " + (s.EndOfStream ? "(fully written)" : "(not fully written)");
				}
				if (rs is WritableChunkedTransferEncodingStream)
				{
					WritableChunkedTransferEncodingStream s = (WritableChunkedTransferEncodingStream)rs;
					return StatusString + ", BODY: " + s.PayloadBytesWritten + " bytes " + (s.StreamEnded ? "(fully written)" : "(and counting)");
				}
			}
			long? cl = ContentLength;
			if (cl != null)
				return StatusString + ", Content-Length: " + cl;
			if (rs != null)
				return StatusString + ", BODY: unknown";
			return StatusString;
		}
		/// <summary>
		/// Creates the response header data and prepares a response stream.  Returns the response header data.  It is critical that the caller writes the returned response header data to the tcpStream and flushes the tcpStream before writing anything to the response stream.
		/// </summary>
		/// <returns>Returns the response header data.</returns>
		internal byte[] PrepareForProxy(out Stream responseStream)
		{
			byte[] responseHeader = GetResponseHeader(out bool chunkedTransferEncoding);
			PrepareResponseStream(chunkedTransferEncoding);
			responseStream = this.responseStream;
			return responseHeader;
		}
		#endregion
	}
}