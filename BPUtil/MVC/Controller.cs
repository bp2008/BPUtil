using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BPUtil.SimpleHttp;

namespace BPUtil.MVC
{
	/// <summary>
	/// Base class for a Controller roughly equivalent to those available in ASP.NET MVC.
	/// The controller should define at least one ActionMethod.  An ActionMethod is a public method which returns an <see cref="ActionResult"/> (or a class derived from <see cref="ActionResult"/>).  The controller must not define multiple ActionMethods with the same name.
	/// </summary>
	public abstract class Controller
	{
		/// <summary>
		/// The context of this request.
		/// </summary>
		public RequestContext Context { get; internal set; }
		/// <summary>
		/// A container which contains data accessible from view pages.
		/// </summary>
		public readonly ViewDataContainer ViewData = new ViewDataContainer();
		/// <summary>
		/// A wrapper around ViewData which can have key/value pairs added and read as dy namic properties.
		/// </summary>
		public readonly dynamic ViewBag;

		public Controller()
		{
			ViewBag = new ViewBagContainer(ViewData);
		}

		/// <summary>
		/// When overridden in a derived class, this method may return false to disallow access to the controller.
		/// </summary>
		/// <returns>True if the controller is usable in the current request context.  False if the controller must not be used.</returns>
		public virtual bool OnAuthorization()
		{
			return true;
		}

		/// <summary>
		/// Returns a BinaryResult where the body is binary data.
		/// </summary>
		/// <param name="data">The data to send in the response.</param>
		/// <param name="contentType">The Content-Type of the response.</param>
		/// <param name="compress">If true, the response should be compressed.</param>
		/// <returns></returns>
		protected virtual BinaryResult Binary(byte[] data, string contentType, bool compress)
		{
			return new BinaryResult(data, contentType, compress);
		}

		/// <summary>
		/// Returns a FileDownloadResult constructed from a byte array.
		/// </summary>
		/// <param name="data">File data.</param>
		/// <param name="compress">If true, the response should be compressed.</param>
		protected virtual FileDownloadResult FileDownload(byte[] data, bool compress)
		{
			return new FileDownloadResult(data, compress);
		}

		/// <summary>
		/// Returns a FileDownloadResult constructed from a file path.  Loads the entire file into memory. To write a file more efficiently, you should write it yourself using the httpProcessor and then return a null ActionResult.
		/// </summary>
		/// <param name="filePath">File path. The entire file will be loaded into memory.</param>
		/// <param name="compress">If true, the response should be compressed.</param>
		protected virtual FileDownloadResult FileDownload(string filePath, bool compress)
		{
			return new FileDownloadResult(filePath, compress);
		}

		/// <summary>
		/// Returns a result where the body is a Jpeg image.
		/// </summary>
		/// <param name="imgData">Jpeg image data.</param>
		/// <returns></returns>
		protected virtual JpegImageResult JpegImage(byte[] imgData)
		{
			return new JpegImageResult(imgData);
		}

		/// <summary>
		/// Returns a result where the body is a PNG image.
		/// </summary>
		/// <param name="imgData">PNG image data.</param>
		/// <returns></returns>
		protected virtual PngImageResult PngImage(byte[] imgData)
		{
			return new PngImageResult(imgData);
		}

		/// <summary>
		/// Returns a StringResult where the body is a UTF8-encoded string.
		/// </summary>
		/// <param name="str">The string to send in the response.</param>
		/// <param name="contentType">The Content-Type of the response.</param>
		/// <returns></returns>
		protected virtual StringResult String(string str, string contentType)
		{
			return new StringResult(str, contentType);
		}

		/// <summary>
		/// Returns a PlainTextResult.
		/// </summary>
		/// <param name="str">Plain text string</param>
		protected virtual PlainTextResult PlainText(string str)
		{
			return new PlainTextResult(str);
		}

		/// <summary>
		/// Returns an EmptyResult, which is a PlainTextResult with an empty string.
		/// </summary>
		protected virtual EmptyResult Empty()
		{
			return new EmptyResult();
		}

		/// <summary>
		/// Returns an ErrorResult, which is a PlainTextResult with a custom response status.
		/// </summary>
		/// <param name="errorMessage">Plain text error message.</param>
		/// <param name="responseStatus">HTTP response status.</param>
		protected virtual ErrorResult Error(string errorMessage, string responseStatus = "500 Internal Server Error")
		{
			return new ErrorResult(errorMessage, responseStatus);
		}

		/// <summary>
		/// Returns an ErrorHtmlResult, which is an HtmlResult with a custom response status.
		/// </summary>
		/// <param name="errorMessageHtml">Error message with HTML markup.</param>
		/// <param name="responseStatus">HTTP response status.</param>
		protected virtual ErrorHtmlResult ErrorHtml(string errorMessageHtml, string responseStatus = "500 Internal Server Error")
		{
			return new ErrorHtmlResult(errorMessageHtml, responseStatus);
		}

		/// <summary>
		/// Returns an HtmlResult where the body is text containing HTML markup.
		/// </summary>
		/// <param name="html">HTML markup.</param>
		protected virtual HtmlResult Html(string html)
		{
			return new HtmlResult(html);
		}

		/// <summary>
		/// Returns a JsonResult where the body is text containing JSON markup.
		/// </summary>
		/// <param name="json">JSON markup.</param>
		protected virtual JsonResult Json(string json)
		{
			return new JsonResult(json);
		}
	}
}
