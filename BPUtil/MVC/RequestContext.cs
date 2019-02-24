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
		public readonly HttpProcessor httpProcessor;
		public readonly string OriginalRequestPath;
		/// <summary>
		/// The "path" part of the URL.  E.g. for the url "Articles/Science/Moon.html?search=crater" the "path" part is "Articles/Science/Moon.html".
		/// </summary>
		public readonly string Path;
		/// <summary>
		/// The "query" part of the URL.  E.g. for the url "Articles/Science/Moon.html?search=crater" the "query" part is "search=crater".
		/// </summary>
		public readonly string Query;
		public readonly string ControllerName;
		public readonly string ActionName;
		public readonly string[] ActionArgs;
		public RequestContext(HttpProcessor httpProcessor, string requestPath)
		{
			this.httpProcessor = httpProcessor;
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
	}
}
