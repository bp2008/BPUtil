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
	public static class HttpCompressionHelper
	{
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
		/// "" or "gzip" depending on whether or not the body was encoded or not.
		/// </summary>
		public string ContentEncoding = "";

		/// <summary>
		/// An additionalHeaders instance that may have had ["Content-Encoding", "gzip"] added to it.
		/// </summary>
		public List<KeyValuePair<string, string>> additionalHeaders;

		public HttpCompressionBody(byte[] uncompressedBody, string extensionIncludingDot)
		{
			additionalHeaders = new List<KeyValuePair<string, string>>();
			Initialize(uncompressedBody, extensionIncludingDot);
		}

		public HttpCompressionBody(byte[] uncompressedBody, string extensionIncludingDot, ref List<KeyValuePair<string, string>> additionalHeaders)
		{
			this.additionalHeaders = additionalHeaders;
			Initialize(uncompressedBody, extensionIncludingDot);
		}

		private void Initialize(byte[] uncompressedBody, string extensionIncludingDot)
		{
			body = uncompressedBody;
			if (uncompressedBody.Length > 40 && HttpCompressionHelper.FileTypeShouldBeCompressed(extensionIncludingDot))
			{
				byte[] compressed = GzipCompress(body);
				if (compressed.Length < uncompressedBody.Length)
				{
					body = compressed;
					ContentEncoding = "gzip";
					additionalHeaders.Add(new KeyValuePair<string, string>("Content-Encoding", ContentEncoding));
				}
			}
		}

		private byte[] GzipCompress(byte[] raw)
		{
			using (MemoryStream ms = new MemoryStream())
			{
				using (GZipStream gzip = new GZipStream(ms, CompressionLevel.Optimal, true))
				{
					gzip.Write(raw, 0, raw.Length);
				}
				return ms.ToArray();
			}
		}
	}
}
