using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil.MVC
{
	/// <summary>
	/// A result which parses a text file and replaces specially-tagged expressions with strings from the controller's ViewData.
	/// The tagging format is similar to ASP.NET razor pages where code is prefixed with '@' characters.  Literal '@' characters may be escaped by another '@' character.
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
		/// <param name="filePath">Path to a text file containing the view content.</param>
		/// <param name="ViewData">A ViewDataContainer containing values for expressions found within the view.</param>
		public ViewResult(string filePath, ViewDataContainer ViewData) : base(null)
		{
			string text = File.ReadAllText(filePath);
			ProcessView(text, ViewData);
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
			return System.Web.HttpUtility.HtmlEncode(str);
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
