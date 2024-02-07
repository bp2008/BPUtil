using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BPUtil.MVC;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestMVC
	{
		private ViewDataContainer BuildViewData()
		{
			ViewDataContainer ViewData = new ViewDataContainer();
			dynamic ViewBag = new ViewBagContainer(ViewData);
			ViewData.Set("Title", "An <html> View Test");
			ViewBag.Body = "<div>Test text is the best text.</div><div><a href=\"mailto:me@example.com\">Mail</a></div>";
			return ViewData;
		}
		private string ProcessView(string input, ViewDataContainer ViewData)
		{
			ViewResult vr = new ViewResult();
			vr.ProcessView(input, ViewData);
			return Encoding.UTF8.GetString(vr.Body);
		}

		[TestMethod]
		public void TestMVCViewNoExpressionsNoViewData()
		{
			string input = "<html>\r\n<head><title>Test</title></head>\r\n<body>More</body>\r\n</html>";
			string output = ProcessView(input, null);
			Assert.AreEqual(input, output);
		}
		[TestMethod]
		public void TestMVCViewExpressions()
		{
			string input = "<html>\r\n<head><title>@HtmlEncode:Title</title></head>\r\n<body>@Body</body>\r\n</html>";
			string output = ProcessView(input, BuildViewData());
			string expectedOutput = "<html>\r\n<head><title>An &lt;html&gt; View Test</title></head>\r\n<body><div>Test text is the best text.</div><div><a href=\"mailto:me@example.com\">Mail</a></div></body>\r\n</html>";
			Assert.AreEqual(expectedOutput, output);
		}
		[TestMethod]
		public void TestMVCViewExpressions2()
		{
			string input = "<html><head><title>@HtmlEncode:Title</title></head><body>@Body</body></html>";
			string output = ProcessView(input, BuildViewData());
			string expectedOutput = "<html><head><title>An &lt;html&gt; View Test</title></head><body><div>Test text is the best text.</div><div><a href=\"mailto:me@example.com\">Mail</a></div></body></html>";
			Assert.AreEqual(expectedOutput, output);
		}
		[TestMethod]
		[ExpectedException(typeof(Exception), AllowDerivedTypes = true)]
		public void TestMVCViewUnknownVariables()
		{
			string input = "<html><head><title>@HtmlEncode:TITLE</title></head><body>@BODY</body>@Body</html>";
			ProcessView(input, BuildViewData());

			//string expectedOutput = "<html><head><title></title></head><body></body><div>Test text is the best text.</div><div><a href=\"mailto:me@example.com\">Mail</a></div></html>";
			//Assert.AreEqual(expectedOutput, output);
		}
		[TestMethod]
		public void TestMVCViewNoExpressions()
		{
			string input = "<html><head><title>Test</title></head><body>More</body></html>";
			string output = ProcessView(input, BuildViewData());
			Assert.AreEqual(input, output);
		}
		[TestMethod]
		public void TestMVCViewNoViewData()
		{
			string input = "<html><head><title>@HtmlEncode:Title</title></head><body>@Body</body></html>";
			string output = ProcessView(input, null);
			Assert.AreEqual(input, output);
		}
		[TestMethod]
		public void TestMVCViewNoViewDataBadExpressions()
		{
			string input = "<html><head><title>@Htmlencode:Title</title>@</head>@ <body>@Body</body>@@@</html>";
			string output = ProcessView(input, null);
			Assert.AreEqual(input, output);
		}
		[TestMethod]
		public void TestMVCViewBadExpressionNoViewData()
		{
			string input = "<html><head><title>@ </title></head><body>@Body</body></html>";
			string output = ProcessView(input, null);
			Assert.AreEqual(input, output);
		}
		[TestMethod]
		public void TestMVCViewBadExpression()
		{
			string input = "<html><head><title>@Htmlencode:Title</title></head><body>@Body</body></html>";
			bool threw = false;
			try
			{
				ProcessView(input, BuildViewData());
			}
			catch (Exception) { threw = true; }
			Assert.IsTrue(threw, "Test did not throw exception as expected due to invalid method name \"Htmlencode\".");
		}
		[TestMethod]
		public void TestMVCViewBadExpression2()
		{
			string input = "<html><head><title>@ </title></head><body>@Body</body></html>";
			bool threw = false;
			try
			{
				ProcessView(input, BuildViewData());
			}
			catch (Exception) { threw = true; }
			Assert.IsTrue(threw, "Test did not throw exception as expected due to empty expression \"<title>@ </title>\"");
		}
		[TestMethod]
		public void TestMVCViewBadExpression3()
		{
			string input = "<html><head><title>Test</title></head><body>@</body></html>";
			bool threw = false;
			try
			{
				ProcessView(input, BuildViewData());
			}
			catch (Exception) { threw = true; }
			Assert.IsTrue(threw, "Test did not throw exception as expected due to empty expression \"<body>@</body>\"");
		}
		[TestMethod]
		public void TestMVCViewAtEscape()
		{
			string input = "<html><head><title>@@@@@@</title></head><body>@@</body></html>";
			string output = ProcessView(input, BuildViewData());
			Assert.AreEqual("<html><head><title>@@@</title></head><body>@</body></html>", output);
		}
		[TestMethod]
		public void TestMVCViewAtEscapeFail()
		{
			string input = "<html><head><title>@@@@@@@</title></head><body>@@</body></html>";
			bool threw = false;
			try
			{
				ProcessView(input, BuildViewData());
			}
			catch (Exception) { threw = true; }
			Assert.IsTrue(threw, "Test did not throw exception as expected due to empty expression after 3 escaped '@' characters: \"<title>@@@@@@@</title>\"");
		}
		[TestMethod]
		public void TestMVCParenthesis()
		{
			string input = "<html>\r\n<head><title>@(HtmlEncode:Title)</title></head>\r\n<body>@(Body)</body>\r\n</html>";
			string output = ProcessView(input, BuildViewData());
			string expectedOutput = "<html>\r\n<head><title>An &lt;html&gt; View Test</title></head>\r\n<body><div>Test text is the best text.</div><div><a href=\"mailto:me@example.com\">Mail</a></div></body>\r\n</html>";
			Assert.AreEqual(expectedOutput, output);
		}
	}
}
