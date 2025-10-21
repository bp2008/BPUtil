using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.MVC
{
	/// <summary>
	/// An ActionResult that displays the "Server Error" title along with details of an exception in HTML format.
	/// </summary>
	public class ExceptionHtmlResult : ErrorHtmlResult
	{
		/// <summary>
		/// The exception whose details are being displayed.  Will be null if the client is not allowed to see exception details.
		/// </summary>
		public readonly Exception Exception;
		/// <summary>
		/// The exception message string.  Guaranteed to not be null.  If the client is not allowed to see exception details, this will be a generic error message.
		/// </summary>
		public readonly string Message;
		/// <summary>
		/// Constructs an ExceptionHtmlResult that displays the details of the given exception.
		/// </summary>
		/// <param name="ex">The Exception that happened.  Should be null if the client is not allowed to see exception details.</param>
		public ExceptionHtmlResult(Exception ex) : base(null)
		{
			this.Exception = ex;
			if (ex == null)
				this.Message = "An error occurred, but error details are unavailable to remote clients.";
			else
				this.Message = ex.ToString();
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("<!DOCTYPE html>");
			sb.AppendLine("<html>");
			sb.AppendLine("<head>");
			sb.AppendLine("<title>Server Error</title>");
			sb.AppendLine("</head>");
			sb.AppendLine("<body>");
			sb.Append("<pre style=\"");
			sb.Append("font-family: consolas, monospace, sans-serif;");
			sb.Append("margin: 10px;");
			sb.Append("border: 1px solid black;");
			sb.Append("padding: 10px;");
			sb.Append("background-color: #FFDDDD;");
			sb.Append("overflow: auto;");
			sb.AppendLine("\">");
			sb.AppendLine(StringUtil.HtmlEncode(this.Message));
			sb.AppendLine("</pre>");
			sb.AppendLine("</body>");
			sb.AppendLine("</html>");
			this.BodyStr = sb.ToString();
		}
	}
}
