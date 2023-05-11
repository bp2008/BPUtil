using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.MVC
{
	/// <summary>
	/// <para>A result which parses a text file and replaces specially-tagged expressions with strings from the controller's ViewData.  Expressions are case-sensitive!</para>
	/// <para>The tagging format is similar to ASP.NET razor pages where code is prefixed with '@' characters.  Literal '@' characters may be escaped by another '@' character.</para>
	/// <para>'@' indicates the start of a code expression.  Literal '@' characters may be inserted into the output by printing two '@' characters in sequence. E.g. "Email me at admin@@example.com"</para>
	/// <para>Valid characters inside a code expression are alphanumeric (a-z, A-Z, 0-9) and underscore.  Colon ':' is used as a separator between tokens in order to provide method names to transform a retrieved value as demonstrated in examples below:</para>
	/// <para>Examples:</para>
	/// <para>@AppRoot - Yields the raw value of the ViewData field named "AppRoot".</para>
	/// <para>@(AppRoot) - Yields the raw value of the ViewData field named "AppRoot". Wrapping the expression value in parenthesis allows you to terminate an expression that would otherwise have an ambiguous end point. e.g. "@AppRootimages/icon.jpg" vs "@(AppRoot)images/icon.jpg"</para>
	/// <para>@HtmlEncode:Banana - Yields the HTML-encoded value of the ViewData field named "Banana".</para>
	/// <para>@HtmlAttributeEncode:Banana - Yields the HTML-attribute-encoded value of the ViewData field named "Banana".</para>
	/// <para>@JavaScriptStringEncode:Banana - Yields the JavaScript string encoded value of the ViewData field named "Banana".</para>
	/// <para>@JavaScriptStringEncode:HtmlEncode:Banana - Yields the value from the ViewData field named "Banana" after being Html Encoded, then JavaScript string encoded.</para>
	/// <para>@(JavaScriptStringEncode:HtmlEncode:Banana) - Yields the same value as above, but with parenthesis wrapping to demonstrate usage.</para>
	/// </summary>
	public class ViewResult : HtmlResult
	{
		/// <summary>
		/// Constructs an empty ViewResult. You should call ProcessView on this instance.
		/// </summary>
		public ViewResult() : base(null) { }
		/// <summary>
		/// Contructs a ViewResult from the specified file.
		/// </summary>
		/// <param name="filePath">Path to a text file containing the view content. If this is a relative path within the current diretory, and the debugger is attached and this is a debug build and the project directory also contains the relative path, then the file is loaded from the project source directory instead of the bin directory.</param>
		/// <param name="ViewData">A ViewDataContainer containing values for expressions found within the view.</param>
		public ViewResult(string filePath, ViewDataContainer ViewData) : base(null)
		{
			if (System.Diagnostics.Debugger.IsAttached)
			{
				FileInfo fi = new FileInfo(filePath);
				string cwd = Directory.GetCurrentDirectory();
				if (fi.FullName.StartsWith(cwd))
				{
					DirectoryInfo di = new DirectoryInfo(cwd);
					if (di.Name == "Debug")
					{
						di = di.Parent;
						if (di?.Name == "x86" || di?.Name == "x64")
							di = di.Parent;
						if (di?.Name == "bin")
							di = di.Parent;
					}
					string debugPath = Path.Combine(di.FullName, filePath);
					if (File.Exists(debugPath))
						filePath = debugPath;
				}
			}
			if (File.Exists(filePath))
			{
				string text = File.ReadAllText(filePath);
				ProcessView(text, ViewData);
			}
			else
			{
				ResponseStatus = "404 Not Found";
			}
		}
		/// <summary>
		/// Processes the specified text as a view and sets this result body. Do not call this unless the constructor you used says to do so.
		/// </summary>
		/// <param name="viewText">The view's text.</param>
		/// <param name="ViewData">A ViewDataContainer containing values for expressions found within the view.</param>
		/// <returns></returns>
		public void ProcessView(string viewText, ViewDataContainer ViewData)
		{
			if (ViewData != null)
			{
				StringBuilder sb = new StringBuilder(viewText.Length);
				StringBuilder expressionBuffer = new StringBuilder();
				ViewParseState state = ViewParseState.HTML;
				bool expressionStartedWithParenthesis = false;
				foreach (char c in viewText)
				{
					if (state == ViewParseState.HTML)
					{
						if (c == '@')
							state = ViewParseState.Expression;
						else
							sb.Append(c);
					}
					else if (state == ViewParseState.Expression)
					{
						if (c == '@')
						{
							if (expressionBuffer.Length == 0)
							{
								// This was a sequence of two adjacent '@' characters, which is output as one literal '@' character.
								state = ViewParseState.HTML;
								sb.Append(c);
							}
							else
							{
								// New '@' ends the previous expression and starts a new one.
								sb.Append(ProcessExpression(expressionBuffer, ViewData));
							}
						}
						else if (expressionBuffer.Length == 0 && c == '(')
						{
							// Expressions can be within parenthesis in order 
							// to mark their end to allow for cases like:
							//    "@(AppRoot)images/icon.jpg"
							expressionStartedWithParenthesis = true;
						}
						else if ((c >= 'a' && c <= 'z')
							|| (c >= 'A' && c <= 'Z')
							|| (c >= '0' && c <= '9')
							|| c == '_'
							|| c == ':')
						{
							// This character gets added to the expression.
							expressionBuffer.Append(c);
						}
						else
						{
							if (expressionStartedWithParenthesis && c != ')')
								throw new Exception("Expression began with opening parenthesis but did not end with closing parenthesis.");
							// This character ends the expression.
							state = ViewParseState.HTML;
							sb.Append(ProcessExpression(expressionBuffer, ViewData));
							if (!expressionStartedWithParenthesis)
								sb.Append(c);
							expressionStartedWithParenthesis = false;
						}
					}
				}
				if (state == ViewParseState.Expression)
					sb.Append(ProcessExpression(expressionBuffer, ViewData));
				viewText = sb.ToString();
			}
			BodyStr = viewText;
		}

		/// <summary>
		/// Splits the expression into tokens, retrieves the requested data value, and processes it using methods specified in the expression as necessary.
		/// </summary>
		/// <param name="expressionBuffer"></param>
		/// <param name="ViewData"></param>
		/// <returns></returns>
		private string ProcessExpression(StringBuilder expressionBuffer, ViewDataContainer ViewData)
		{
			if (expressionBuffer.Length > 0)
			{
				string expression = expressionBuffer.ToString();
				expressionBuffer.Clear();

				string[] parts = expression.Split(':');
				if (!ViewData.TryGet(parts[parts.Length - 1], out string value))
					throw new Exception("A view expression referred to a key that does not exist in the view data.");
				for (int i = parts.Length - 2; i >= 0; i--)
					value = PerformExpressionMethod(parts[i], value);
				return value;
			}
			else
				throw new Exception("View contained an empty expression.");
		}

		/// <summary>
		/// Performs the named method on the given value.
		/// </summary>
		/// <param name="methodName">Name of the method to perform, e.g. "HtmlEncode".  This string must exactly match one of the hard-coded method names programmed within.</param>
		/// <param name="value">Value to pass into the named method.</param>
		/// <returns></returns>
		private string PerformExpressionMethod(string methodName, string value)
		{
			if (methodName == "HtmlEncode")
				return HtmlEncode(value);
			else if (methodName == "HtmlAttributeEncode")
				return HtmlAttributeEncode(value);
			else if (methodName == "JavaScriptStringEncode")
				return JavaScriptStringEncode(value);
			throw new Exception("Unknown method name \"" + methodName + "\" in expression.");
		}

		#region Methods Available to Expressions
		private string HtmlEncode(string str)
		{
			return StringUtil.HtmlEncode(str);
		}
		private string HtmlAttributeEncode(string str)
		{
			return StringUtil.HtmlAttributeEncode(str);
		}
		private string JavaScriptStringEncode(string str)
		{
			return StringUtil.JavaScriptStringEncode(str);
		}
		#endregion

		private enum ViewParseState
		{
			HTML,
			Expression
		}
	}
}
