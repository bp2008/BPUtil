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
		private static SortedList<string, ControllerInfo> controllerInfoMap = new SortedList<string, ControllerInfo>();
		/// <summary>
		/// The Namespace containing the controllers for this instance.
		/// </summary>
		public string Namespace { get; protected set; }
		/// <summary>
		/// Creates a new API from a namespace.
		/// </summary>
		/// <param name="assembly">The assembly where the API handler classes are located. e.g. Assembly.GetExecutingAssembly()</param>
		/// <param name="namespaceStr">The namespace containing all the API handler classes. e.g. typeof(SomeAPIHandler).Namespace</param>
		public MVCMain(Assembly assembly, string namespaceStr)
		{
			this.Namespace = namespaceStr;
			IEnumerable<Type> controllerTypes = assembly.GetTypes().Where(IsController);
			foreach (Type t in controllerTypes)
				controllerInfoMap[t.Name] = new ControllerInfo(t);
		}

		/// <summary>
		/// Processes a request from a client, then returns true. Returns false if the request could not be processed. Exceptions thrown by a controller are not caught here.
		/// </summary>
		/// <param name="httpProcessor">The HttpProcessor handling this request.</param>
		/// <param name="requestPath">The path requested by the client, with leading '/' removed. (e.g. httpProcessor.requestedPage)</param>
		/// <returns></returns>
		public bool ProcessRequest(HttpProcessor httpProcessor, string requestPath)
		{
			if (httpProcessor.responseWritten)
				throw new Exception("MVCMain.ProcessRequest was called with an HttpProcessor that had already written a response.");

			RequestContext context = new RequestContext(httpProcessor, requestPath);
			if (!controllerInfoMap.TryGetValue(context.ControllerName, out ControllerInfo controllerInfo))
				return false;

			ActionResult actionResult = controllerInfo.CallMethod(context);

			if (httpProcessor.responseWritten) // Controller methods may handle their own response, in which case we will ignore the result.
				return true;

			if (actionResult == null)
				return false; // This could mean the method was not found, or that it decided to not provide a response.

			if (actionResult.Body == null)
			{
				httpProcessor.writeSuccess(actionResult.ContentType, 0, actionResult.ResponseStatus);
			}
			else
			{
				List<KeyValuePair<string, string>> additionalHeaders = null;
				if (actionResult.Compress && actionResult.Body.Length >= 32 && httpProcessor.ClientRequestsGZipCompression)
				{
					byte[] compressed = Compression.GZipCompress(actionResult.Body);
					if (compressed.Length < actionResult.Body.Length)
					{
						additionalHeaders = new List<KeyValuePair<string, string>>();
						additionalHeaders.Add(new KeyValuePair<string, string>("Content-Encoding", "gzip"));
						actionResult.Body = compressed;
					}
				}
				httpProcessor.writeSuccess(actionResult.ContentType, actionResult.Body.Length, actionResult.ResponseStatus, additionalHeaders);
				httpProcessor.outputStream.Flush();
				httpProcessor.tcpStream.Write(actionResult.Body, 0, actionResult.Body.Length);
			}
			return true;
		}

		private bool IsController(Type t)
		{
			return t.Namespace == Namespace && t.IsSubclassOf(typeof(Controller)) && !t.IsAbstract;
		}
	}
}
