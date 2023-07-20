using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.MVC
{
	public class ExceptionHtmlResult : ErrorHtmlResult
	{
		public ExceptionHtmlResult(Exception ex) : base(null)
		{
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
			sb.AppendLine(ex != null ? ex.ToString() : "Exception details are unavailable to remote clients.");
			sb.AppendLine("</pre>");
			sb.AppendLine("</body>");
			sb.AppendLine("</html>");
			this.BodyStr = sb.ToString();
		}
	}
}
