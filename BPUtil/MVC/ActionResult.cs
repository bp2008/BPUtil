﻿using BPUtil.SimpleHttp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BPUtil.MVC
{
	public class ActionResult
	{
		/// <summary>
		/// The response body for an HTTP response. This property may be overridden by derived ActionResult types and should not be assumed to be efficient (it may not be simply backed by a field).
		/// </summary>
		public virtual byte[] Body { get; set; }

		/// <summary>
		/// A collection of HTTP headers for an HTTP response.
		/// </summary>
		public HttpHeaderCollection headers = new HttpHeaderCollection();

		/// <summary>
		/// Gets or sets the Content-Type header. Null indicates no header exists (getter), or deletes the header (setter).
		/// </summary>
		public string ContentType
		{
			get
			{
				return headers.Get("Content-Type");
			}
			set
			{
				headers.Set("Content-Type", value);
			}
		}

		/// <summary>
		/// The HTTP response status consisting of a 3-digit number optionally followed by one space character and a textual "Reason Phrase".  The Reason Phrase may not contain \r or \n characters. e.g. "200 OK" or "404 Not Found"
		/// </summary>
		public string ResponseStatus = "200 OK";
		/// <summary>
		/// If true, the response may be compressed before being transmitted to the client. Very small results, and any results which end up larger once compressed, will not be transmitted in compressed form.
		/// </summary>
		public bool Compress = true;
		/// <summary>
		/// Constructs a new ActionResult with the specified ContentType.
		/// </summary>
		/// <param name="contentType">The Content-Type header for an HTTP response.  Null to have no Content-Type.</param>
		public ActionResult(string contentType)
		{
			this.ContentType = contentType;
		}
	}
	#region Binary Results
	/// <summary>
	/// A result where the body is binary data.
	/// </summary>
	public class BinaryResult : ActionResult
	{
		/// <summary>
		/// Constructs a BinaryResult
		/// </summary>
		/// <param name="data">The data to send in the response.</param>
		/// <param name="contentType">The Content-Type of the response.</param>
		/// <param name="compress">If true, the response should be compressed.</param>
		public BinaryResult(byte[] data, string contentType, bool compress) : base(contentType)
		{
			Body = data;
			Compress = compress;
		}
	}
	/// <summary>
	/// A result where the body is a file download ("Content-Type: application/octet-stream").
	/// </summary>
	public class FileDownloadResult : BinaryResult
	{
		/// <summary>
		/// Sets the file name that the file should be downloaded as.  If null, a web browser will get the file name from the current URL.
		/// </summary>
		private string FileNameForBrowser
		{
			set
			{
				if (value == null)
					headers.Remove("Content-Disposition");
				else
					headers["Content-Disposition"] = "attachment; filename=\"" + value + "\"";
			}
		}
		/// <summary>
		/// Constructs a FileDownloadResult from a byte array.
		/// </summary>
		/// <param name="data">File data.</param>
		/// <param name="compress">If true, the response should be compressed.</param>
		/// <param name="fileNameForBrowser">File name that the file should be downloaded as.  If null, a web browser will get the file name from the current URL.</param>
		public FileDownloadResult(byte[] data, bool compress, string fileNameForBrowser = null) : base(data, "application/octet-stream", compress)
		{
			FileNameForBrowser = fileNameForBrowser;
		}
		/// <summary>
		/// Constructs a FileDownloadResult from a file path.  This constructor loads the entire file into memory.
		/// </summary>
		/// <param name="filePath">File path. The entire file will be loaded into memory.</param>
		/// <param name="compress">If true, the response should be compressed.</param>
		/// <param name="fileNameForBrowser">File name that the file should be downloaded as.  If null, a web browser will get the file name from the current URL.</param>
		public FileDownloadResult(string filePath, bool compress, string fileNameForBrowser = null) : base(File.ReadAllBytes(filePath), "application/octet-stream", compress)
		{
			FileNameForBrowser = fileNameForBrowser;
		}
	}
	/// <summary>
	/// A result where the body is a JPEG image.
	/// </summary>
	public class JpegImageResult : BinaryResult
	{
		public JpegImageResult(byte[] imgData) : base(imgData, "image/jpeg", false) { }
	}
	/// <summary>
	/// A result where the body is a PNG image.
	/// </summary>
	public class PngImageResult : BinaryResult
	{
		/// <summary>
		/// Constructs a PngImageResult where the body is a PNG image in a byte array.
		/// </summary>
		/// <param name="imgData">PNG image data.</param>
		public PngImageResult(byte[] imgData) : base(imgData, "image/png", false) { }
	}
	#endregion

	#region String-based Results
	/// <summary>
	/// A result where the body is a UTF8-encoded string.
	/// </summary>
	public class StringResult : ActionResult
	{
		/// <summary>
		/// The string result. This property may be overridden by derived StringResult types and should not be assumed to be efficient (it may not be simply backed by a field).
		/// </summary>
		public virtual string BodyStr { get; set; }

		public override byte[] Body
		{
			get
			{
				string str = BodyStr;
				if (str == null)
					return null;
				return ByteUtil.Utf8NoBOM.GetBytes(str);
			}
			set
			{
				byte[] ba = value;
				if (ba == null)
					BodyStr = null;
				else
					BodyStr = ByteUtil.Utf8NoBOM.GetString(ba);
			}
		}
		/// <summary>
		/// Constructs a StringResult where the body is a UTF8-encoded string.
		/// </summary>
		/// <param name="contentType">The Content-Type of the response.</param>
		public StringResult(string contentType) : base(contentType)
		{
			Compress = true;
		}
		/// <summary>
		/// Constructs a StringResult where the body is a UTF8-encoded string.
		/// </summary>
		/// <param name="str">The string to send in the response.</param>
		/// <param name="contentType">The Content-Type of the response.</param>
		public StringResult(string str, string contentType) : this(contentType)
		{
			if (str != null)
				BodyStr = str;
		}
	}
	/// <summary>
	/// A result where the body is a plain text string.
	/// </summary>
	public class PlainTextResult : StringResult
	{
		/// <summary>
		/// Constructs a PlainTextResult.
		/// </summary>
		/// <param name="str">Plain text string</param>
		public PlainTextResult(string str) : base(str, "text/plain; charset=utf-8") { }
	}
	/// <summary>
	/// A result where the body is an empty plain text string.
	/// </summary>
	public class EmptyResult : PlainTextResult
	{
		/// <summary>
		/// Constructs an EmptyResult, which is a PlainTextResult with an empty string.
		/// </summary>
		public EmptyResult() : base("") { }
	}
	/// <summary>
	/// A result where the body is a plain-text error message and the response status has a custom value.
	/// </summary>
	public class ErrorResult : PlainTextResult
	{
		/// <summary>
		/// Constructs an ErrorResult, which is a PlainTextResult with a custom response status.
		/// </summary>
		/// <param name="errorMessage">Plain text error message.</param>
		/// <param name="responseStatus">HTTP response status.</param>
		public ErrorResult(string errorMessage, string responseStatus = "500 Internal Server Error") : base(errorMessage)
		{
			ResponseStatus = responseStatus;
		}
	}
	/// <summary>
	/// A result where the body is an HTML error message and the response status has a custom value.
	/// </summary>
	public class ErrorHtmlResult : HtmlResult
	{
		/// <summary>
		/// Constructs an ErrorHtmlResult, which is an HtmlResult with a custom response status.
		/// </summary>
		/// <param name="errorMessageHtml">Error message with HTML markup.</param>
		/// <param name="responseStatus">HTTP response status.</param>
		public ErrorHtmlResult(string errorMessageHtml, string responseStatus = "500 Internal Server Error") : base(errorMessageHtml)
		{
			ResponseStatus = responseStatus;
		}
	}
	/// <summary>
	/// A result where the body is HTML text.
	/// </summary>
	public class HtmlResult : StringResult
	{
		/// <summary>
		/// Constructs an HtmlResult where the body is text containing HTML markup.
		/// </summary>
		/// <param name="html">HTML markup.</param>
		public HtmlResult(string html) : base(html, "text/html; charset=utf-8") { }
	}
	/// <summary>
	/// A result where the body is JSON text.
	/// </summary>
	public class JsonResult : StringResult
	{
		/// <summary>
		/// The object to serialize as JSON.
		/// </summary>
		public virtual object BodyObj { get; set; }

		public override string BodyStr
		{
			get
			{
				object obj = BodyObj;
				if (obj == null)
					return null;
				return MvcJson.SerializeObject(obj);
			}
			set
			{
				string json = value;
				if (json == null)
					BodyObj = null;
				else
					BodyObj = MvcJson.DeserializeObject(json);
			}
		}
		/// <summary>
		/// Constructs a JsonResult where the body is text containing JSON markup.
		/// </summary>
		/// <param name="obj">Object to serialize as JSON.</param>
		public JsonResult(object obj) : base("application/json")
		{
			BodyObj = obj;
		}
	}
	/// <summary>
	/// A result where there is no body and the response status has a custom value.
	/// </summary>
	public class StatusCodeResult : ActionResult
	{
		/// <summary>
		/// Constructs an ErrorResult, which is a PlainTextResult with a custom response status.
		/// </summary>
		/// <param name="responseStatus">HTTP response status.</param>
		public StatusCodeResult(string responseStatus = "500 Internal Server Error") : base(null)
		{
			ResponseStatus = responseStatus;
		}
	}
	#endregion
}