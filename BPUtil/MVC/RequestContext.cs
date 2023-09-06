using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BPUtil.SimpleHttp;

namespace BPUtil.MVC
{
	public class RequestContext
	{
		/// <summary>
		/// The <see cref="HttpProcessor"/> that is handling the request.
		/// </summary>
		public readonly HttpProcessor httpProcessor;
		/// <summary>
		/// The <see cref="HttpServerBase"/> that accepted the connection.
		/// </summary>
		public readonly HttpServerBase Server;
		/// <summary>
		/// The full path string requested by the client.
		/// </summary>
		public readonly string OriginalRequestPath;
		/// <summary>
		/// The "path" part of the URL.  E.g. for the url "Articles/Science/Moon.html?search=crater" the "path" part is "Articles/Science/Moon.html".
		/// </summary>
		public readonly string Path;
		/// <summary>
		/// The "query" part of the URL.  E.g. for the url "Articles/Science/Moon.html?search=crater" the "query" part is "search=crater".
		/// </summary>
		public readonly string Query;
		/// <summary>
		/// Name of the controller as requested.
		/// </summary>
		public readonly string ControllerName;
		/// <summary>
		/// Action method name that was requested.
		/// </summary>
		public string ActionName { get; protected set; }
		/// <summary>
		/// Array of arguments (separated by '/' characters in the original path string).
		/// </summary>
		public string[] ActionArgs { get; protected set; }

		internal HttpHeaderCollection additionalResponseHeaders = new HttpHeaderCollection();
		/// <summary>
		/// Gets a list of additional headers to include in the response. Getting the list is not thread safe, and using the list is also not thread safe.
		/// </summary>
		public HttpHeaderCollection ResponseHeaders
		{
			get
			{
				if (additionalResponseHeaders == null)
					additionalResponseHeaders = new HttpHeaderCollection();
				return additionalResponseHeaders;
			}
		}

		/// <summary>
		/// Adds a header to the ResponseHeaders list.  This is simply a convenience method.
		/// </summary>
		/// <param name="key">Header name.</param>
		/// <param name="value">Header value.</param>
		public void AddResponseHeader(string key, string value)
		{
			ResponseHeaders.Add(new KeyValuePair<string, string>(key, value));
		}

		public RequestContext(HttpProcessor httpProcessor, string requestPath)
		{
			this.httpProcessor = httpProcessor;
			this.Server = httpProcessor.srv;
			this.OriginalRequestPath = requestPath;

			int idxQmark = requestPath.IndexOf('?');
			if (idxQmark == -1)
			{
				Path = requestPath;
				Query = "";
			}
			else
			{
				Path = requestPath.Substring(0, idxQmark);
				Query = requestPath.Substring(idxQmark + 1);
			}
			string[] pathParts = Path.Split('/');
			ControllerName = pathParts[0];

			if (pathParts.Length <= 1)
			{
				ActionName = "";
				ActionArgs = new string[0];
			}
			else
			{
				ActionName = pathParts[1];
				ActionArgs = pathParts.Skip(2).ToArray();
			}
			if (string.IsNullOrWhiteSpace(ActionName))
				ActionName = "Index";
		}
		/// <summary>
		/// Moves ActionName into the ActionArgs array as the new first element, and replaces ActionName with "Index".
		/// </summary>
		internal void AssumeActionNameIsArgumentForIndex()
		{
			ActionArgs = new string[] { ActionName }.Union(ActionArgs).ToArray();
			ActionName = "Index";
		}
	}
}
