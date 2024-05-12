using System;

namespace BPUtil.SimpleHttp
{
	/// <summary>
	/// Options object for the `StaticFile` and `StaticFileAsync` methods.
	/// </summary>
	public class StaticFileOptions
	{
		/// <summary>
		/// (Default: null) If provided, this is the value of the Content-Type header to be sent in the response.  If null or empty, it will be determined from the file extension.
		/// </summary>
		public string ContentTypeOverride = null;
		/// <summary>
		/// (Default: true) If true, caching is provided for supported file extensions based on ETag or Last-Modified date.
		/// </summary>
		public bool CanCache = true;
		/// <summary>
		/// (Default: null) If not null, the "Content-Disposition" header will instruct the client to download the response to a file with this file name.
		/// </summary>
		public string DownloadAs
		{
			get
			{
				return _downloadAs;
			}
			set
			{
				if (value != null && StringUtil.MakeSafeForFileName(value) != value)
					throw new ArgumentException("Value is not a valid file name: " + value);
				_downloadAs = value;
			}
		}
		private string _downloadAs = null;
	}
}