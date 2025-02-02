using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
		/// <param name="requestPath">(Optional) The path requested by the client.  If this path starts with '/', the '/' will be removed automatically (if there are multiple '/' at the start, only one is removed). (if null, defaults to httpProcessor.Request.Url.PathAndQuery)</param>
		/// <returns>Returns true if the request was handled by this MVC instance, false if the caller should try to handle the request another way.</returns>
		public bool ProcessRequest(HttpProcessor httpProcessor, string requestPath = null)
		{
			return TaskHelper.RunAsyncCodeSafely(() => ProcessRequestAsync(httpProcessor, requestPath));
		}
		/// <summary>
		/// Processes a request from a client, then returns true. Returns false if the request could not be processed. Exceptions thrown by a controller are caught here.
		/// </summary>
		/// <param name="httpProcessor">The HttpProcessor handling this request.</param>
		/// <param name="requestPath">(Optional) The path requested by the client.  If this path starts with '/', the '/' will be removed automatically (if there are multiple '/' at the start, only one is removed). (if null, defaults to httpProcessor.Request.Url.PathAndQuery)</param>
		/// <param name="cancellationToken">Cancellation Token</param>
		/// <returns>Returns true if the request was handled by this MVC instance, false if the caller should try to handle the request another way.</returns>
		public async Task<bool> ProcessRequestAsync(HttpProcessor httpProcessor, string requestPath = null, CancellationToken cancellationToken = default)
		{
			if (httpProcessor.Response.ResponseHeaderWritten)
				throw new Exception("MVCMain.ProcessRequest was called with an HttpProcessor that had already written a response.");
			if (requestPath == null)
				requestPath = httpProcessor.Request.Url.PathAndQuery;
			if (requestPath.StartsWith("/"))
				requestPath = requestPath.Substring(1);
			RequestContext context = new RequestContext(httpProcessor, requestPath);
			if (!controllerInfoMap.TryGetValue(context.ControllerName.ToUpper(), out ControllerInfo controllerInfo))
				return false;

			ActionResult actionResult = null;
			try
			{
				actionResult = await controllerInfo.CallMethod(context, cancellationToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException) { throw; }
			catch (Exception ex)
			{
				if (HttpProcessor.IsOrdinaryDisconnectException(ex))
					ex.Rethrow();
				actionResult = GenerateErrorPage(context, ex);
			}

			if (httpProcessor.Response.ResponseHeaderWritten) // Controller methods may handle their own response, in which case we will ignore the result.
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
				httpProcessor.Response.Set(actionResult.ContentType, 0, actionResult.ResponseStatus, context.additionalResponseHeaders);
			}
			else
			{
				HttpHeaderCollection additionalHeaders = new HttpHeaderCollection();
				bool addedContentEncoding = false;
				bool addedContentType = false;
				if (actionResult.Compress && body.Length >= 200 && httpProcessor.Request.BestCompressionMethod != null)
				{
					byte[] compressed = httpProcessor.Request.BestCompressionMethod.Compress(body);
					if (compressed.Length < body.Length)
					{
						additionalHeaders.Add("Content-Encoding", httpProcessor.Request.BestCompressionMethod.AlgorithmName);
						body = compressed;
					}
				}
				foreach (HttpHeader header in actionResult.headers)
				{
					if (!header.Key.IEquals("Content-Type"))
					{
						additionalHeaders.Add(header.Key, header.Value);
					}
				}
				if (context.additionalResponseHeaders != null)
				{
					foreach (HttpHeader header in context.additionalResponseHeaders)
					{
						if (addedContentEncoding && header.Key.IEquals("Content-Encoding"))
							continue;
						else if (addedContentType && header.Key.IEquals("Content-Type"))
							continue;
						else
							additionalHeaders.Add(header);
					}
				}
				httpProcessor.Response.Set(actionResult.ContentType, body.Length, actionResult.ResponseStatus, additionalHeaders);
				httpProcessor.Response.BodyContent = body;
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
