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
	/// <para>Base class for a Controller roughly equivalent to those available in ASP.NET MVC.</para>
	/// <para>The controller should define at least one ActionMethod.  An ActionMethod is a public method which returns an <see cref="ActionResult"/> (or a class derived from <see cref="ActionResult"/>).</para>
	/// <para>The controller must not define multiple ActionMethods with the same name.</para>
	/// <para>Normally the MVC framework will automatically write the <see cref="ActionResult"/> to the response stream, however if an ActionMethod utilizes <see cref="Controller.Context.httpProcessor"/> to write a response directly, then the MVC framework will not write the ActionResult to the response stream.  In this case it is perfectly acceptable for the ActionMethod to return null.</para>
	/// <para>If an ActionMethod returns null, but has not written its own response using <see cref="Controller.Context.httpProcessor"/>, then the MVC framework will treat the request the same as if it failed to be routed.</para>
	/// <para>If an ActionMethod throws an exception, the MVC framework will return an error message in HTML format.  If this format is not acceptable, then your ActionMethod should handle its own exceptions.</para>
	/// <para>Request routing within the MVC framework is done automatically following simple rules.</para>
	/// <para>======================================</para>
	/// <para>Request routing examples:</para>
	/// <para>The URL "https://localhost/MyController" will route to the controller named "MyController" and call the ActionMethod named "Index" with no arguments.</para>
	/// <para>The URL "https://localhost/MyController/Index" is equivalent to the above example, for routing purposes.</para>
	/// <para>The URL "https://localhost/MYCONTROLLER/INDEX" is equivalent to the above example for routing purposes.</para>
	/// <para>The URL "https://localhost/Apple/Banana/Cherry/5" will route to the controller named "Apple" and call the ActionMethod named "Banana", providing two arguments "Cherry" and 5.  However if the "Apple" Controller does not have an ActionMethod named "Banana", then the "Index" ActionMethod is called instead with three arguments "Banana", "Cherry", and 5.</para>
	/// <para>======================================</para>
	/// <para>These are the routing rules:</para>
	/// <para>* The "Path" part of the URL is split on '/' characters.</para>
	/// <para>* The first segment is the Controller name, matched case-insensitive.  The first segment is required, but all following segments are optional.</para>
	/// <para>* The second segment is the ActionMethod name, matched case-insensitive. If this second segment is not provided, it is assumed that the desired ActionMethod name is "Index" and that the ActionMethod takes no arguments. If the second segment of the Path is provided but does not match an ActionMethod, then it is assumed that the desired ActionMethod name is "Index" and the second segment value is used as the first argument.</para>
	/// <para>* All following segments are arguments for the ActionMethod. Arguments must be provided in the order and format that is delared in the ActionMethod method declaration.  If the client provides too many or too few arguments, or incompatible argument values, it is an error.  Optional parameters to the method may be omitted from the query, but you cannot skip an optional parameter and declare a value for a later optional parameter; the arguments must be in order.</para>
	/// <para>* To provide a value for a Boolean type argument to an ActionMethod, use "1" or "true" (case-insensitive) for true, or any other value for false (even empty string!).</para>
	/// <para>======================================</para>
	/// <para>If these built-in routing rules are not sufficient, it is possible to have an ActionMethod pull arguments from the POST body or from the Query String.</para>
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
		/// A wrapper around ViewData which can have key/value pairs added and read as dynamic properties.
		/// </summary>
		public readonly dynamic ViewBag;

		public Controller()
		{
			ViewBag = new ViewBagContainer(ViewData);
		}

		/// <summary>
		/// When overridden in a derived class, this method may allow or disallow access to the controller.  This is called before the client-specified action method is called.
		/// </summary>
		/// <param name="result">If authorization fails, this should be set to an appropriate result such as an HTTP 403 Forbidden response. If null, authorization will be assumed to have succeeded.</param>
		public virtual void OnAuthorization(ref ActionResult result) { }

		/// <summary>
		/// When overridden in a derived class, this method may modify any ActionResult before it is sent to the client.
		/// </summary>
		public virtual void PreprocessResult(ActionResult result) { }

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
		/// <param name="obj">Object to be returned as JSON markup.</param>
		protected virtual JsonResult Json(object obj)
		{
			return new JsonResult(obj);
		}

		/// <summary>
		/// Returns a StatusCodeResult with a custom response status.
		/// </summary>
		/// <param name="responseStatus">HTTP response status.</param>
		protected virtual StatusCodeResult StatusCode(string responseStatus)
		{
			return new StatusCodeResult(responseStatus);
		}

		/// <summary>
		/// <para>Returns a <see cref="ViewResult"/> created from the specified text file.  This controller instance's <see cref="ViewData"/> will be used for view processing.</para>
		/// <para>The given text file will be processed by replacing specially-tagged expressions with strings from <see cref="ViewData"/>.
		/// The tagging format is similar to ASP.NET razor pages where code is prefixed with '@' characters.  Literal '@' characters may be escaped by another '@' character.</para>
		/// </summary>
		/// <param name="filePath">Path to a text file containing the view content.</param>
		protected virtual ViewResult View(string filePath)
		{
			return new ViewResult(filePath, ViewData);
		}
	}
}
