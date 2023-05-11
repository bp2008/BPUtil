using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BPUtil.SimpleHttp;

namespace BPUtil.MVC
{
	/// <summary>
	/// A class which grants access to functionality similar to ASP.NET MVC, but very lightweight.
	/// </summary>
	public class MVCMain
	{
		/// <summary>
		/// A map of Controller type names to ControllerInfo instances.
		/// </summary>
		private SortedList<string, ControllerInfo> controllerInfoMap = new SortedList<string, ControllerInfo>();

		/// <summary>
		/// The Namespace containing the controllers for this instance.
		/// </summary>
		public string Namespace { get; protected set; }
		public Action<RequestContext, Exception> ErrorHandler { get; protected set; }
		/// <summary>
		/// Creates a new API from a namespace.
		/// </summary>
		/// <param name="assembly">The assembly where the API controller classes are located. e.g. Assembly.GetExecutingAssembly()</param>
		/// <param name="namespaceStr">The namespace containing all the API controller classes. e.g. typeof(SomeAPIHandler).Namespace</param>
		/// <param name="ErrorHandler">A function accepting a RequestContext and an Exception for logging purposes.</param>
		public MVCMain(Assembly assembly, string namespaceStr, Action<RequestContext, Exception> ErrorHandler = null)
		{
			this.Namespace = namespaceStr;
			this.ErrorHandler = ErrorHandler;
			IEnumerable<Type> controllerTypes = assembly.GetTypes().Where(IsController);
			foreach (Type t in controllerTypes)
			{
				if (controllerInfoMap.ContainsKey(t.Name.ToUpper()))
					throw new Exception("Namespace \"" + Namespace + "\" defines multiple Controllers with the same name: \"" + t.Name + "\". This is unsupported.");
				controllerInfoMap[t.Name.ToUpper()] = new ControllerInfo(t);
			}
		}

		/// <summary>
		/// Processes a request from a client, then returns true. Returns false if the request could not be processed. Exceptions thrown by a controller are caught here.
		/// </summary>
		/// <param name="httpProcessor">The HttpProcessor handling this request.</param>
		/// <param name="requestPath">(Optional) The path requested by the client.  If this path starts with '/', the '/' will be removed automatically (if there are multiple '/' at the start, only one is removed). (if null, defaults to httpProcessor.request_url.PathAndQuery)</param>
		/// <returns></returns>
		public bool ProcessRequest(HttpProcessor httpProcessor, string requestPath = null)
		{
			if (httpProcessor.responseWritten)
				throw new Exception("MVCMain.ProcessRequest was called with an HttpProcessor that had already written a response.");
			if (requestPath == null)
			{
				requestPath = httpProcessor.request_url.PathAndQuery;
				if (requestPath.StartsWith("/"))
					requestPath = requestPath.Substring(1);
			}
			RequestContext context = new RequestContext(httpProcessor, requestPath);
			if (!controllerInfoMap.TryGetValue(context.ControllerName.ToUpper(), out ControllerInfo controllerInfo))
				return false;

			ActionResult actionResult = null;
			try
			{
				actionResult = controllerInfo.CallMethod(context);
			}
			catch (Exception ex)
			{
				actionResult = GenerateErrorPage(context, ex);
			}

			if (httpProcessor.responseWritten) // Controller methods may handle their own response, in which case we will ignore the result.
				return true;

			if (actionResult == null)
				return false; // This could mean the method was not found, or that it decided to not provide a response.

			byte[] body = null;

			try
			{
				body = actionResult.Body;
			}
			catch (Exception ex)
			{
				actionResult = GenerateErrorPage(context, ex);
				body = actionResult.Body;
			}

			if (body == null)
			{
				httpProcessor.writeSuccess(actionResult.ContentType, 0, actionResult.ResponseStatus, context.additionalResponseHeaders, keepAlive: httpProcessor.keepAliveRequested);
			}
			else
			{
				List<KeyValuePair<string, string>> additionalHeaders = new List<KeyValuePair<string, string>>();
				bool addedContentEncoding = false;
				bool addedContentType = false;
				if (actionResult.Compress && body.Length >= 32 && httpProcessor.ClientRequestsGZipCompression)
				{
					byte[] compressed = Compression.GZipCompress(body);
					if (compressed.Length < body.Length)
					{
						additionalHeaders.Add(new KeyValuePair<string, string>("Content-Encoding", "gzip"));
						body = compressed;
					}
				}
				foreach (HttpHeader header in actionResult.headers)
				{
					if (!header.Name.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
					{
						additionalHeaders.Add(new KeyValuePair<string, string>(header.Name, header.Value));
					}
				}
				if (context.additionalResponseHeaders != null)
				{
					foreach (KeyValuePair<string, string> header in context.additionalResponseHeaders)
					{
						if (addedContentEncoding && header.Key == "Content-Encoding")
							continue;
						else if (addedContentType && header.Key == "Content-Type")
							continue;
						else
							additionalHeaders.Add(header);
					}
				}
				httpProcessor.writeSuccess(actionResult.ContentType, body.Length, actionResult.ResponseStatus, additionalHeaders, keepAlive: httpProcessor.keepAliveRequested);
				httpProcessor.outputStream.Flush();
				httpProcessor.tcpStream.Write(body, 0, body.Length);
			}
			return true;
		}

		/// <summary>
		/// Returns true if the specified type is a controller we can create an instance of.  It must be in <see cref="Namespace"/> (specified in constructor).
		/// </summary>
		/// <param name="t">The type which might be a controller we can create an instance of.</param>
		/// <returns></returns>
		private bool IsController(Type t)
		{
			return (t.Namespace == Namespace || (t.Namespace != null && t.Namespace.StartsWith(Namespace + ".")))
				&& typeof(Controller).IsAssignableFrom(t) && !t.IsAbstract;
		}

		/// <summary>
		/// Returns an error page showing details of an exception that was thrown.
		/// </summary>
		/// <param name="context"></param>
		/// <param name="ex"></param>
		/// <returns></returns>
		private ActionResult GenerateErrorPage(RequestContext context, Exception ex)
		{
			if (!(ex is ClientException) && ErrorHandler != null)
			{
				try
				{
					ErrorHandler(context, ex);
				}
				catch { }
			}
			if (MVCGlobals.RemoteClientsMaySeeExceptionDetails || context.httpProcessor.IsLocalConnection || ex is ClientException)
				return new ExceptionHtmlResult(ex);
			else
				return new ExceptionHtmlResult(null);
		}
	}
}
