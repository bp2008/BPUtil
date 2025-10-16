using BPUtil.IO;
using BPUtil.SimpleHttp.Helpers;
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
		/// The type of compression that will be used for the response stream.  Null if no compression will be used.
		/// </summary>
		private CompressionMethod compressionMethod;
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
		private Stream _compressionstream;
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
				// Event streams sent by this server are normally sent in a kept-alive connection using `Transfer-Encoding: chunked`, and it seems to be fine.  To force event streams to use `Connection: close`, uncomment the following lines:
				//if (IsEventStream) 
				//	return 0;
				if (_keepAliveTimeSeconds == null)
					_keepAliveTimeSeconds = GetKeepAliveTimeSeconds();
				return _keepAliveTimeSeconds.Value;
			}
		}
		/// <summary>
		/// Gets a value indicating if the <c>Content-Type</c> response header indicates that this response is an Event Stream (SSE) response. (e.g. <c>"Content-Type: text/event-stream; charset=utf-8"</c>).
		/// </summary>
		public bool IsEventStream => ContentType != null && (ContentType.IEquals("text/event-stream") || ContentType.IStartsWith("text/event-stream;"));
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
		/// <param name="Description">Optional simple text body. If null, there will be no response body.</param>
		public void Simple(string StatusString, string Description = null)
		{
			if (Description == null)
				Description = "";

			FullResponseUTF8(Description, Description != "" ? "text/plain; charset=utf-8" : null, StatusString);
		}
		#endregion
		#region Redirect Permanent (302)
		/// <summary>
		/// <para>Resets the response, then synchronously writes a redirect header instructing the remote user's browser to load the URL you specify.</para>
		/// <para>Uses a 302 status code which should cause the client to repeat the request using GET with no request body.</para>
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
		/// <para>Uses a 302 status code which should cause the client to repeat the request using GET with no request body.</para>
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
		#region Redirect Temporary (307)
		/// <summary>
		/// <para>Resets the response, then synchronously writes a redirect header instructing the remote user's browser to load the URL you specify.</para>
		/// <para>Uses a 307 status code which should cause the client to repeat the request to the new URL with the original method and body.</para>
		/// <para>This completes the response.</para>
		/// </summary>
		/// <param name="redirectToUrl">URL to redirect to.</param>
		public void RedirectTemporary(string redirectToUrl)
		{
			_Prep_RedirectTemporary(redirectToUrl);
			FinishSync();
		}
		/// <summary>
		/// <para>Resets the response, then asynchronously writes a redirect header instructing the remote user's browser to load the URL you specify.</para>
		/// <para>Uses a 307 status code which should cause the client to repeat the request to the new URL with the original method and body.</para>
		/// <para>This completes the response.</para>
		/// </summary>
		/// <param name="redirectToUrl">URL to redirect to.</param>
		/// <param name="cancellationToken">Cancellation Token.</param>
		public Task RedirectTemporaryAsync(string redirectToUrl, CancellationToken cancellationToken = default)
		{
			_Prep_RedirectTemporary(redirectToUrl);
			return FinishAsync(cancellationToken);
		}
		private void _Prep_RedirectTemporary(string redirectToUrl)
		{
			Reset("307 Temporary Redirect");
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
		/// <param name="options">If provided, this object contains options for the static file transmission.</param>
		public void StaticFile(string filePath, StaticFileOptions options = null)
		{
			if (_checkAsyncUsage && p.IsAsync)
				throw new ApplicationException("This HttpProcessor is not in blocking/synchronous mode.");
			if (ResponseHeaderWritten)
				throw new ApplicationException("The response header was already written.");
			StaticFile(new FileInfo(filePath), options);
		}

		/// <summary>
		/// <para>Asynchronously writes a static file response with built-in caching support.</para>
		/// <para>This completes the response.</para>
		/// </summary>
		/// <param name="filePath">Path to the file on disk.</param>
		/// <param name="options">If provided, this object contains options for the static file transmission.</param>
		/// <param name="cancellationToken">Cancellation Token.</param>
		public Task StaticFileAsync(string filePath, StaticFileOptions options = null, CancellationToken cancellationToken = default)
		{
			if (_checkAsyncUsage && !p.IsAsync)
				throw new ApplicationException("This HttpProcessor is not in async mode.");
			if (ResponseHeaderWritten)
				throw new ApplicationException("The response header was already written.");
			return StaticFileAsync(new FileInfo(filePath), options, cancellationToken);
		}
		/// <summary>
		/// <para>Synchronously writes a static file response with built-in caching support.</para>
		/// <para>This completes the response.</para>
		/// </summary>
		/// <param name="fi">File on disk.</param>
		/// <param name="options">If provided, this object contains options for the static file transmission.</param>
		public void StaticFile(FileInfo fi, StaticFileOptions options = null)
		{
			if (_checkAsyncUsage && p.IsAsync)
				throw new ApplicationException("This HttpProcessor is not in blocking/synchronous mode.");
			if (options == null)
				options = new StaticFileOptions();

			if (!fi.Exists)
			{
				Simple("404 Not Found", null);
				return;
			}

			bool cacheHit = false;
			bool isCacheable = SetupStaticFileCommonHeaders(fi, options);

			if (isCacheable)
			{
				Headers["ETag"] = MakeETagSync(fi);
				cacheHit = CheckStaticFileCacheHit();
			}

			if (cacheHit)
			{
				StatusString = "304 Not Modified";
			}
			else
			{
				SetupStaticFileCompression(fi);
				ByteRange[] ranges = GetRequestedByteRanges(fi.Length);
				if (ranges == null)
				{
					ContentLength = fi.Length;
					WriteResponseHeaderIfNeededSync();
					if (p.Request.HttpMethod != "HEAD")
						using (FileStream fs = OpenReadable(fi))
							fs.CopyTo(responseStream, 81920);
				}
				else if (ranges.Length == 0)
				{
					StatusString = "416 Requested Range Not Satisfiable";
					ContentLength = 0;
					WriteResponseHeaderIfNeededSync();
				}
				else
				{
					ByteRangeResponseSetupShared(fi, ranges, out string trueContentType, out bool multiPart, out string boundary);

					WriteResponseHeaderIfNeededSync();

					if (p.Request.HttpMethod != "HEAD")
					{
						using (FileStream fs = OpenReadable(fi))
						{
							foreach (ByteRange range in ranges)
							{
								if (multiPart)
								{
									byte[] subHeader = GetMultiPartByteRangeSubHeader(fi, trueContentType, boundary, range);
									responseStream.Write(subHeader, 0, subHeader.Length);
								}
								fs.Seek(range.Start, SeekOrigin.Begin);
								fs.Substream((range.End - range.Start) + 1).CopyTo(responseStream, 81920);
								if (multiPart)
									responseStream.Write(ByteUtil.Utf8NoBOM.GetBytes("\r\n"), 0, 2);
							}
						}
						if (multiPart)
						{
							byte[] trailer = ByteUtil.Utf8NoBOM.GetBytes("--" + boundary + "--");
							responseStream.Write(trailer, 0, trailer.Length);
						}
					}
				}
			}

			FinishSync();
		}
		/// <summary>
		/// <para>Asynchronously writes a static file response with built-in caching support.</para>
		/// <para>This completes the response.</para>
		/// </summary>
		/// <param name="fi">File on disk.</param>
		/// <param name="options">If provided, this object contains options for the static file transmission.</param>
		/// <param name="cancellationToken">Cancellation Token.</param>
		public async Task StaticFileAsync(FileInfo fi, StaticFileOptions options = null, CancellationToken cancellationToken = default)
		{
			if (_checkAsyncUsage && !p.IsAsync)
				throw new ApplicationException("This HttpProcessor is not in async mode.");
			if (options == null)
				options = new StaticFileOptions();

			if (!fi.Exists)
			{
				Simple("404 Not Found", null);
				return;
			}

			bool cacheHit = false;
			bool isCacheable = SetupStaticFileCommonHeaders(fi, options);

			if (isCacheable)
			{
				Headers["ETag"] = await MakeETagAsync(fi).ConfigureAwait(false);
				cacheHit = CheckStaticFileCacheHit();
			}

			if (cacheHit)
			{
				StatusString = "304 Not Modified";
			}
			else
			{
				SetupStaticFileCompression(fi);
				ByteRange[] ranges = GetRequestedByteRanges(fi.Length);
				if (ranges == null)
				{
					ContentLength = fi.Length;
					await WriteResponseHeaderIfNeededAsync(cancellationToken).ConfigureAwait(false);
					if (p.Request.HttpMethod != "HEAD")
						using (FileStream fs = OpenReadable(fi))
							await fs.CopyToAsync(responseStream, 81920, cancellationToken).ConfigureAwait(false);
				}
				else if (ranges.Length == 0)
				{
					StatusString = "416 Requested Range Not Satisfiable";
					ContentLength = 0;
					await WriteResponseHeaderIfNeededAsync(cancellationToken).ConfigureAwait(false);
				}
				else
				{
					ByteRangeResponseSetupShared(fi, ranges, out string trueContentType, out bool multiPart, out string boundary);

					await WriteResponseHeaderIfNeededAsync(cancellationToken).ConfigureAwait(false);

					if (p.Request.HttpMethod != "HEAD")
					{
						using (FileStream fs = OpenReadable(fi))
						{
							foreach (ByteRange range in ranges)
							{
								if (multiPart)
								{
									byte[] subHeader = GetMultiPartByteRangeSubHeader(fi, trueContentType, boundary, range);
									await responseStream.WriteAsync(subHeader, 0, subHeader.Length, cancellationToken).ConfigureAwait(false);
								}
								fs.Seek(range.Start, SeekOrigin.Begin);
								await fs.Substream((range.End - range.Start) + 1).CopyToAsync(responseStream, 81920, cancellationToken).ConfigureAwait(false);
								if (multiPart)
									await responseStream.WriteAsync(ByteUtil.Utf8NoBOM.GetBytes("\r\n"), 0, 2, cancellationToken).ConfigureAwait(false);
							}
						}
						if (multiPart)
						{
							byte[] trailer = ByteUtil.Utf8NoBOM.GetBytes("--" + boundary + "--");
							await responseStream.WriteAsync(trailer, 0, trailer.Length, cancellationToken).ConfigureAwait(false);
						}
					}
				}
			}

			await FinishAsync(cancellationToken).ConfigureAwait(false);
		}

		private bool SetupStaticFileCommonHeaders(FileInfo fi, StaticFileOptions options)
		{
			Reset("200 OK");
			Headers["Content-Type"] = options.ContentTypeOverride != null ? options.ContentTypeOverride : Mime.GetMimeType(fi.Extension);
			Headers["Accept-Ranges"] = "bytes";
			if (!string.IsNullOrWhiteSpace(options.DownloadAs))
			{
				Headers["Content-Disposition"] = "attachment; filename=\"" + options.DownloadAs + "\"";
			}
			if (options.CanCache && p.srv.CanCacheFileExtension(fi.Extension))
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

		private void SetupStaticFileCompression(FileInfo fi)
		{
			if (fi.Length > 200 && HttpCompressionHelper.FileTypeShouldBeCompressed(fi.Extension))
				CompressResponseIfCompatible();
		}

		private bool CheckStaticFileCacheHit()
		{
			string IfNoneMatch = p.Request.Headers.Get("If-None-Match");
			if (IfNoneMatch != null)
			{
				if (Headers["ETag"] == IfNoneMatch)
					return true;
			}
			else
			{
				string IfModifiedSince = p.Request.Headers.Get("If-Modified-Since");
				if (IfModifiedSince != null && IfModifiedSince.IEquals(Headers["Last-Modified"]))
					return true;
			}
			return false;
		}

		class ByteRange
		{
			public long Start;
			public long End;
			public ByteRange(long start, long end)
			{
				Start = start;
				End = end;
			}
		}
		/// <summary>
		/// <para>This method has no side-effects, but may throw an exception if the client requested an invalid byte range.</para>
		/// <para>This method returns an array detailing the byte ranges that were requested in the request's "Range" header.</para>
		/// <para>The array will be empty if none of the requested byte ranges are valid.</para>
		/// <para>The array will be null if this is not a byte range request.</para>
		/// </summary>
		/// <param name="fileLength">Length of the file, in bytes.</param>
		/// <returns>Array of byte ranges that were requested.  Empty if all requested ranges were invalid.  Null if this is not a byte range request.</returns>
		private ByteRange[] GetRequestedByteRanges(long fileLength)
		{
			string IfRange = p.Request.Headers.Get("If-Range");
			if (IfRange != null)
			{
				if (Headers["ETag"] == IfRange || IfRange.IEquals(Headers["Last-Modified"]))
				{
					// If-Range condition check succeeded.  Proceed normally.
				}
				else
					return null; // If-Range condition check failed.  Return null to indicate that this should not be treated as a byte range request.
			}

			string rangeHeader = p.Request.Headers.Get("Range");

			if (!string.IsNullOrEmpty(rangeHeader))
			{
				List<ByteRange> ranges = new List<ByteRange>();
				foreach (string rangeStr in rangeHeader.Replace("bytes=", "").Split(','))
				{
					string[] parts = rangeStr.Split('-');
					if (parts.Length >= 1 && long.TryParse(parts[0].Trim(), out long start))
					{
						if (start < 0)
							start = 0;
						if (start > fileLength - 1)
							continue;
						long end;
						if (parts.Length < 2 || !long.TryParse(parts[1].Trim(), out end))
							end = long.MaxValue;
						if (end > fileLength - 1)
							end = fileLength - 1;
						if (end < start)
							throw new HttpProcessor.HttpProcessorException("400 Bad Request", "An invalid byte range was requested.");
						ranges.Add(new ByteRange(start, end));
					}
				}

				ranges.Sort((a, b) => a.Start.CompareTo(b.Start));

				for (int i = 0; i < ranges.Count - 1; i++)
				{
					if (ranges[i].End >= ranges[i + 1].Start)
						throw new HttpProcessor.HttpProcessorException("400 Bad Request", "Overlapping byte ranges were requested.");
				}
				return ranges.ToArray();
			}
			return null;
		}

		private void ByteRangeResponseSetupShared(FileInfo fi, ByteRange[] ranges, out string trueContentType, out bool multiPart, out string boundary)
		{
			StatusString = "206 Partial Content";
			trueContentType = ContentType;
			multiPart = ranges.Length > 1;
			boundary = multiPart ? DateTime.Now.Ticks.ToString("x") : "";
			ContentLength = GetRangeRequestContentLength(ranges, fi.Length, trueContentType, boundary);

			if (!multiPart)
				Headers["Content-Range"] = "bytes " + ranges[0].Start + "-" + ranges[0].End + "/" + fi.Length;
			else
				ContentType = "multipart/byteranges; boundary=" + boundary;
		}
		private long GetRangeRequestContentLength(ByteRange[] ranges, long fileLength, string contentType, string boundary)
		{
			long contentLength = 0;
			for (int i = 0; i < ranges.Length; i++)
			{
				contentLength += (ranges[i].End - ranges[i].Start) + 1;
				if (ranges.Length > 1)
				{
					// This will be a multipart range response, which has a bunch of overhead.

					// A multipart range response body with 2 ranges looks like:
					// --boundary
					// Content-Type: contentType
					// Content-Range: bytes start-end/fileLength
					// [ ... empty line ... ]
					// [ ... byte range ... ]
					// --boundary[\r\n]
					// Content-Type: contentType[\r\n]
					// Content-Range: bytes start-end/fileLength[\r\n]
					// [\r\n]
					// [ ... byte range ... ][\r\n]
					// --boundary--

					contentLength += 49 + boundary.Length + contentType.Length + ranges[i].Start.ToString("D").Length + ranges[i].End.ToString("D").Length + fileLength.ToString("D").Length;

					// The lengths of all the constant stuff per byte range adds up to 49, so the code above is optimized.
					// The following commented code adds it up the slower way, demonstrating how we come up with 49.
					//contentLength += "--".Length + boundary.Length + "\r\n".Length
					//	+ "Content-Type: ".Length + contentType.Length + "\r\n".Length
					//	+ "Content-Range: bytes ".Length
					//		+ ranges[i].Start.ToString("D").Length
					//		+ "-".Length
					//		+ ranges[i].End.ToString("D").Length
					//		+ "/".Length
					//		+ fileLength.ToString("D").Length
					//	+ "\r\n".Length
					//	+ "\r\n".Length
					//	/* byte range bytes go here */
					//	+ "\r\n".Length;
				}
			}
			if (ranges.Length > 1)
				contentLength += boundary.Length + 4; // Final `--boundary--` end marker for a multipart range response.
			return contentLength;
		}

		private static byte[] GetMultiPartByteRangeSubHeader(FileInfo fi, string trueContentType, string boundary, ByteRange range)
		{
			return ByteUtil.Utf8NoBOM.GetBytes("--" + boundary + "\r\n"
					+ "Content-Type: " + trueContentType + "\r\n"
					+ "Content-Range: bytes " + range.Start + "-" + range.End + "/" + fi.Length + "\r\n\r\n");
		}

		private static FileStream OpenReadable(FileInfo fi)
		{
			return new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
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
					return '"' + Base64UrlMod.ToBase64UrlMod(hash) + '"';
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
#if NET6_0_OR_GREATER
					byte[] hash = await sha.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
#else
					byte[] hash = await TaskHelper.RunBlockingCodeSafely(() => sha.ComputeHash(stream), cancellationToken).ConfigureAwait(false);
#endif
					return '"' + Base64UrlMod.ToBase64UrlMod(hash) + '"';
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
		/// <param name="additionalResponseHeaders">Optional collection of HTTP headers to include in the HTTP response.</param>
		public void WebSocketUpgradeSync(HttpHeaderCollection additionalResponseHeaders = null)
		{
			_checkAsyncUsage = false;
			_Prep_WebSocketUpgrade(additionalResponseHeaders);
			FinishSync();
		}

		/// <summary>
		/// <para>Resets the response, then writes response headers to finish the WebSocket handshake with the client. No extensions are supported (such as compression) at this time.</para>
		/// <para>Afterward, the tcpStream will be ready to hand over to the WebSocket, and this Response object will be finished.</para>
		/// </summary>
		/// <param name="additionalResponseHeaders">Optional collection of HTTP headers to include in the HTTP response.</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		public Task WebSocketUpgradeAsync(HttpHeaderCollection additionalResponseHeaders = null, CancellationToken cancellationToken = default)
		{
			if (_checkAsyncUsage && !p.IsAsync)
				throw new ApplicationException("This HttpProcessor is not in async mode.");
			_Prep_WebSocketUpgrade(additionalResponseHeaders);
			return FinishAsync(cancellationToken);
		}
		private void _Prep_WebSocketUpgrade(HttpHeaderCollection additionalResponseHeaders)
		{
			Reset("101 Switching Protocols");
			if (additionalResponseHeaders != null)
				Headers.Merge(additionalResponseHeaders);
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

			if (compressionMethod != null)
				ContentLength = null; // We're using compression, so clear the Content-Length header because it is almost certainly incorrect.  This will force chunked transfer encoding.

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
			if (compressionMethod != null)
				WriteReservedHeader(sb, reservedHeaderKeys, "Content-Encoding", compressionMethod.AlgorithmName);
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
				// bodyContent can be used without Substream since I fixed response compression.
				//if (!(responseStream is Substream))
				//	throw new ApplicationException("!(responseStream is Substream). responseStream is " + (responseStream == null ? "null" : responseStream.GetType().Name));
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
				// bodyContent can be used without Substream since I fixed response compression.
				//if (!(responseStream is Substream))
				//	throw new ApplicationException("!(responseStream is Substream). responseStream is " + (responseStream == null ? "null" : responseStream.GetType().Name));
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
		/// Configures this response so that the response stream will apply compression if a compatible algorithm was requested by the client.
		/// Must be called BEFORE the response header is written.
		/// The Content-Length header should be the COMPRESSED length, which you have no way of knowing when using this API.  Therefore the Content-Length header will be cleared when writing the response header, which forces "Transfer-Encoding: chunked" to be used to stream a variable-length response.
		/// Returns true if the response will be compressed, and sets this.compressionMethod.
		/// </summary>
		/// <returns></returns>
		public virtual bool CompressResponseIfCompatible()
		{
			if (ResponseHeaderWritten)
				return false;
			if (p.Request.BestCompressionMethod != null)
			{
				compressionMethod = p.Request.BestCompressionMethod;
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

			if (ContentLength != null && ContentLength >= 0)
				r = _substream = r.Substream(ContentLength.Value); // This will throw an exception if we write too many bytes, and gives the Cleanup method a way to know if we did not write enough bytes.

			if (chunkedTransferEncoding)
				r = _chunkedstream = new WritableChunkedTransferEncodingStream(r);

			if (compressionMethod != null)
				r = _compressionstream = compressionMethod.CreateCompressionStream(r);

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

			if (_compressionstream != null)
			{
				_compressionstream.Dispose();
				_compressionstream = null;
			}

			if (_chunkedstream != null)
			{
				_chunkedstream.Close();
				_chunkedstream = null;
			}

			if (_substream != null && !_substream.EndOfStream)
				throw new ApplicationException("The HTTP server failed to write all " + ContentLength + " bytes of the response body. bodyContent?.Length: " + bodyContent?.Length);

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

			if (_compressionstream != null)
			{
				// It is safe to dispose the compression stream because it was created with the leaveOpen option.
#if NET6_0_OR_GREATER
				await _compressionstream.DisposeAsync().ConfigureAwait(false);
#else
				_compressionstream.Dispose();
#endif
				_compressionstream = null;
			}

			if (_chunkedstream != null)
			{
				await _chunkedstream.CloseAsync(cancellationToken).ConfigureAwait(false);
				_chunkedstream = null;
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
		/// Assigns a new <see cref="StatusString"/>, unsets <see cref="bodyContent"/>, unsets the compression method, and clears <see cref="Headers"/>. If the response header was already written, throws ApplicationException. 
		/// </summary>
		/// <param name="httpStatusString">Assigns this value to <see cref="StatusString"/>.</param>
		/// <exception cref="ApplicationException">Throws if the response header was already written.</exception>
		public void Reset(string httpStatusString = "404 Not Found")
		{
			if (ResponseHeaderWritten)
				throw new ApplicationException("The response header was already written.");
			StatusString = httpStatusString;
			bodyContent = null;
			compressionMethod = null;
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
					return StatusString + ", BODY: " + s.PayloadBytesWritten + " bytes " + (s.EndOfStream ? "(fully written)" : "(and counting)");
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
		/// Gets a dynamic object containing a summary of the state of this SimpleHttpResponse.
		/// </summary>
		/// <returns>A dynamic object containing a summary of the state of this SimpleHttpResponse.</returns>
		public object GetSummary()
		{
			if (!ResponseHeaderWritten)
				return new { Type = "Pending", HeaderWritten = false, Status = StatusString };
			if (Headers.Get("Upgrade") == "websocket")
				return new { Type = "WebSocket", HeaderWritten = true, Status = StatusString };
			return new { Type = "Regular", HeaderWritten = true, Status = StatusString, Body = GetBodySummary() };
		}
		/// <summary>
		/// Gets a dynamic object containing a summary of the state of the response body.
		/// </summary>
		/// <returns>A dynamic object containing a summary of the state of the response body.</returns>
		private object GetBodySummary()
		{
			Stream rs = responseStream;
			if (rs != null)
			{
				if (rs is Substream)
				{
					Substream s = (Substream)rs;
					return new { Written = s.Position, Size = s.Length };
				}
				if (rs is WritableChunkedTransferEncodingStream)
				{
					WritableChunkedTransferEncodingStream s = (WritableChunkedTransferEncodingStream)rs;
					if (s.EndOfStream)
						return new { Written = s.PayloadBytesWritten, Size = s.PayloadBytesWritten };
					else
						return new { Written = s.PayloadBytesWritten };
				}
			}
			long? cl = ContentLength;
			if (cl != null)
				return new { Size = cl };
			if (rs != null)
				return new { };
			return null;
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

		/// <summary>
		/// Sets the Server-Timing header if the given <see cref="BasicEventTimer"/> is not null.
		/// </summary>
		/// <param name="serverTiming"><see cref="BasicEventTimer"/> containing Server-Timing data, or null.</param>
		internal void SetServerTiming(BasicEventTimer serverTiming)
		{
			if (serverTiming != null)
				Headers["Server-Timing"] = serverTiming.ToServerTimingHeader();
		}
		#endregion
	}
}