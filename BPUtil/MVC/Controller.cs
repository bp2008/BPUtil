using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BPUtil.SimpleHttp;

namespace BPUtil.MVC
{
	/// <summary>
	/// <para>Base class for a Controller roughly equivalent to those available in ASP.NET MVC.</para>
	/// <para>The controller should define at least one ActionMethod.  An ActionMethod is a public method which returns an <see cref="ActionResult"/> (or a class derived from <see cref="ActionResult"/>).</para>
	/// <para>The controller must not define multiple ActionMethods with the same name.</para>
	/// <para>Normally the MVC framework will automatically write the <see cref="ActionResult"/> to the response stream, however if an ActionMethod utilizes Controller.Context.httpProcessor to write a response directly, then the MVC framework will not write the ActionResult to the response stream.  In this case it is perfectly acceptable for the ActionMethod to return null.</para>
	/// <para>If an ActionMethod returns null, but has not written its own response using Controller.Context.httpProcessor, then the MVC framework will treat the request the same as if it failed to be routed.</para>
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
		/// <summary>
		/// Cancellation Token which may be used to cancel execution of the action method being run.
		/// </summary>
		public CancellationToken CancellationToken { get; internal set; } = default;

		/// <summary>
		/// Constructs a new Controller.
		/// </summary>
		public Controller()
		{
			ViewBag = new ViewBagContainer(ViewData);
		}

		/// <summary>
		/// When overridden in a derived class, this method may allow or disallow access to the controller.  This is called before the client-specified action method is called.  If authorization fails, this should return an appropriate result such as an HTTP 403 Forbidden response, and the action method will not be called. If null, authorization will be assumed to have succeeded and the action method will be called.
		/// </summary>
		/// <returns>If authorization fails, this should return an appropriate result such as an HTTP 403 Forbidden response. If null, authorization will be assumed to have succeeded.</returns>
		protected internal virtual ActionResult OnAuthorization() { return null; }

		/// <summary>
		/// <para>When overridden in a derived class, this method may inspect and/or modify any ActionResult before it is sent to the client.</para>
		/// <para>If the action method throws an exception, the <c>PreprocessResult</c> method will not be called.  <see cref="OnActionMethodError"/> will be called instead.  If you have not overridden <c>OnActionMethodError</c>, the exception will be rethrown and handled by the <see cref="MVCMain"/> class, which converts the Exception into an <see cref="ExceptionHtmlResult"/>.</para>
		/// <para>Note: ActionResult instances returned by the <see cref="OnAuthorization"/> method ARE sent to <c>PreprocessResult</c>.</para>
		/// </summary>
		protected internal virtual ActionResult PreprocessResult(ActionResult result) { return result; }

		/// <summary>
		/// <para>This method is called with any Exception thrown by an action method.</para>
		/// <para>The default implementation of this method simply rethrows the exception.  However, this method can be overridden to return an ActionResult instead.  The ActionResult should gracefully communicate to the client</para>
		/// <para>When overridden in a derived class, this method may decide how to gracefully handle exceptions thrown by action methods.</para>
		/// <para>If the action method throws an exception, the ActionResult will be an <see cref="ExceptionHtmlResult"/>, and this method may want to replace it with another type of result (e.g. for API compatibility).</para>
		/// <para>Some types of errors are not sent through the <c>PreprocessResult</c> method, such as the StatusCodeResult which is generated if the caller used a disallowed HTTP method.</para>
		/// <para>Note: ActionResult instances returned by the <see cref="OnAuthorization"/> method ARE sent to <c>PreprocessResult</c>.</para>
		/// </summary>
		/// <param name="ex">The exception which was thrown by an action method.</param>
		/// <param name="clientIsAllowedToSeeExceptionDetails">If false, you should not return the exception message or stack trace in the ActionResult you return.</param>
		/// <exception cref="Exception">The exception which was thrown by an action method.</exception>
		protected virtual ActionResult OnActionMethodError(Exception ex, bool clientIsAllowedToSeeExceptionDetails)
		{
			ex.Rethrow();
			throw ex; // This line is not reached, but exists to satisfy the compiler.
		}
		/// <summary>
		/// Calls <see cref="OnActionMethodError"/>.
		/// </summary>
		/// <param name="ex">Argument to OnActionMethodError.</param>
		/// <returns>The result of calling <see cref="OnActionMethodError"/>.</returns>
		internal ActionResult CallOnActionMethodError(Exception ex)
		{
			return OnActionMethodError(ex, IsClientAllowedToSeeExceptionDetails(ex));
		}
		/// <summary>
		/// Returns true if the client is allowed to see the details of this exception.
		/// </summary>
		/// <param name="ex">Exception which the client may or may not be allowed to see.</param>
		/// <returns>Returns true if the client is allowed to see the details of this exception.</returns>
		protected internal bool IsClientAllowedToSeeExceptionDetails(Exception ex)
		{
			return MVCGlobals.RemoteClientsMaySeeExceptionDetails || Context.httpProcessor.IsLocalConnection || ex is ClientException;
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
		/// <param name="fileNameForBrowser">File name that the file should be downloaded as.  If null, a web browser will get the file name from the current URL.</param>
		protected virtual FileDownloadResult FileDownload(byte[] data, bool compress, string fileNameForBrowser = null)
		{
			return new FileDownloadResult(data, compress, fileNameForBrowser);
		}

		/// <summary>
		/// Returns a FileDownloadResult constructed from a file path.  Loads the entire file into memory. To write a file more efficiently, you should write it yourself using the httpProcessor and then return a null ActionResult.
		/// </summary>
		/// <param name="filePath">File path. The entire file will be loaded into memory.</param>
		/// <param name="compress">If true, the response should be compressed.</param>
		/// <param name="fileNameForBrowser">File name that the file should be downloaded as.  If null, a web browser will get the file name from the current URL.</param>
		protected virtual FileDownloadResult FileDownload(string filePath, bool compress, string fileNameForBrowser = null)
		{
			return new FileDownloadResult(filePath, compress, fileNameForBrowser);
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
