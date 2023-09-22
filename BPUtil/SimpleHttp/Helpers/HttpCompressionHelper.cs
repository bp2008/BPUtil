using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.SimpleHttp
{
	/// <summary>
	/// Provides helper methods for HTTP response compression.
	/// </summary>
	public static class HttpCompressionHelper
	{
		/// <summary>
		/// A ConcurrentDictionary mapping file extensions in lower case to a value indicating if the file is likely to benefit from compression.
		/// </summary>
		public static ConcurrentDictionary<string, bool> commonlyCompressedExtensions = BuildCommonlyCompressedExtensionsDict();
		private static ConcurrentDictionary<string, bool> BuildCommonlyCompressedExtensionsDict()
		{
			ConcurrentDictionary<string, bool> d = new ConcurrentDictionary<string, bool>();
			d[".html"] = true;
			d[".htm"] = true;
			d[".js"] = true;
			d[".css"] = true;
			d[".txt"] = true;
			d[".svg"] = true;
			d[".xml"] = true;
			return d;
		}
		/// <summary>
		/// Returns true if the specified file extension has been flagged for compression (e.g. .html, .htm, .txt, .js, .css, .svg, .xml).
		/// </summary>
		/// <param name="extensionIncludingDot"></param>
		/// <returns></returns>
		public static bool FileTypeShouldBeCompressed(string extensionIncludingDot)
		{
			bool compress;
			return commonlyCompressedExtensions.TryGetValue(extensionIncludingDot, out compress) ? compress : false;
		}
	}
	/// <summary>
	/// A class which compresses or does not compress a content body, depending on its type and length.
	/// </summary>
	public class HttpCompressionBody
	{
		/// <summary>
		/// The http response body, which may be compressed with gzip or not; check ContentEncoding to find out.
		/// </summary>
		public byte[] body;
		/// <summary>
		/// "" or "br" or "gzip" or "deflate", etc, depending on whether or not the body was compressed during construction.
		/// </summary>
		public string ContentEncoding = "";

		/// <summary>
		/// An additionalHeaders instance that may have had "Content-Encoding" set in it.
		/// </summary>
		public HttpHeaderCollection headers;
		/// <summary>
		/// The HTTP Request object.
		/// </summary>
		public SimpleHttpRequest Request;

		public HttpCompressionBody(SimpleHttpRequest Request, byte[] uncompressedBody, string extensionIncludingDot)
		{
			this.Request = Request;
			headers = new HttpHeaderCollection();
			Initialize(uncompressedBody, extensionIncludingDot);
		}

		public HttpCompressionBody(SimpleHttpRequest Request, byte[] uncompressedBody, string extensionIncludingDot, HttpHeaderCollection headers)
		{
			this.Request = Request;
			this.headers = headers;
			Initialize(uncompressedBody, extensionIncludingDot);
		}

		private void Initialize(byte[] uncompressedBody, string extensionIncludingDot)
		{
			body = uncompressedBody;
			if (Request.BestCompressionMethod != null && uncompressedBody.Length > 200 && HttpCompressionHelper.FileTypeShouldBeCompressed(extensionIncludingDot))
			{
				byte[] compressed = Request.BestCompressionMethod.Compress(uncompressedBody);
				if (compressed.Length < uncompressedBody.Length)
				{
					body = compressed;
					headers["Content-Encoding"] = ContentEncoding = Request.BestCompressionMethod.AlgorithmName;
				}
			}
		}
	}
}
