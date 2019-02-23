using System.IO;
using System.Text;

namespace BPUtil.MVC
{
	public class ActionResult
	{
		/// <summary>
		/// The response body for an HTTP response.
		/// </summary>
		public byte[] Body;
		/// <summary>
		/// The Content-Type header for an HTTP response.
		/// </summary>
		public string ContentType;
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
		/// <param name="contentType">The Content-Type header for an HTTP response.</param>
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
		/// Constructs a FileDownloadResult from a byte array.
		/// </summary>
		/// <param name="data">File data.</param>
		/// <param name="compress">If true, the response should be compressed.</param>
		public FileDownloadResult(byte[] data, bool compress) : base(data, "application/octet-stream", compress) { }
		/// <summary>
		/// Constructs a FileDownloadResult from a file path.  This constructor loads the entire file into memory.
		/// </summary>
		/// <param name="filePath">File path. The entire file will be loaded into memory.</param>
		/// <param name="compress">If true, the response should be compressed.</param>
		public FileDownloadResult(string filePath, bool compress) : base(File.ReadAllBytes(filePath), "application/octet-stream", compress) { }
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
		/// Constructs a StringResult where the body is a UTF8-encoded string.
		/// </summary>
		/// <param name="str">The string to send in the response.</param>
		/// <param name="contentType">The Content-Type of the response.</param>
		public StringResult(string str, string contentType) : base(contentType)
		{
			if (str != null)
				Body = Encoding.UTF8.GetBytes(str);
			Compress = true;
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
		/// Constructs a JsonResult where the body is text containing JSON markup.
		/// </summary>
		/// <param name="json">JSON markup.</param>
		public JsonResult(string json) : base(json, "application/json") { }
	}
	#endregion
}