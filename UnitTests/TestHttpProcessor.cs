using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BPUtil;
using BPUtil.SimpleHttp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestHttpProcessor
	{
		private static SimpleHttpRequest CreateHttpRequest(string url)
		{
			Uri uri = new Uri(url);
			string request = "GET " + uri.ToString() + " HTTP/1.1\r\nConnection: close\r\n\r\n";
			byte[] requestBytes = ByteUtil.Utf8NoBOM.GetBytes(request);
			MemoryStream ms = new MemoryStream(requestBytes);
			return SimpleHttpRequest.FromStream(new Uri(uri.GetLeftPart(UriPartial.Authority)), ms);
		}
		private static void TestConfiguration(string uriBefore, string requestedPageBefore, string appPath, string uriAfter, string requestedPageAfter)
		{
			SimpleHttpRequest Request = CreateHttpRequest(uriBefore);
			Uri a = new Uri(uriBefore);
			Assert.AreEqual(a, Request.Url);
			Assert.AreEqual(requestedPageBefore, Request.Page);

			Request.RemoveAppPath(appPath);

			Uri b = new Uri(uriAfter);
			Assert.AreEqual(b, Request.Url);
			Assert.AreEqual(requestedPageAfter, Request.Page);
		}
		[TestMethod]
		public void TestRemoveAppPath()
		{
			string uri = "http://example.com:81";
			string expectedPage = "";
			TestConfiguration(uri, expectedPage, null, uri, expectedPage);
			TestConfiguration(uri, expectedPage, "", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "/", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "//", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "test", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "/test", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "test/", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "/test/", uri, expectedPage);


			uri = "http://example.com:81/";
			expectedPage = "";

			TestConfiguration(uri, expectedPage, null, uri, expectedPage);
			TestConfiguration(uri, expectedPage, "", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "/", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "//", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "test", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "/test", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "test/", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "/test/", uri, expectedPage);


			uri = "http://example.com:81//";
			expectedPage = "/";

			TestConfiguration(uri, expectedPage, null, uri, expectedPage);
			TestConfiguration(uri, expectedPage, "", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "/", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "//", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "test", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "/test", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "test/", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "/test/", uri, expectedPage);


			uri = "http://example.com:81/test";
			string uri2 = "http://example.com:81/";
			expectedPage = "test";
			string expectedPage2 = "";

			TestConfiguration(uri, expectedPage, null, uri, expectedPage);
			TestConfiguration(uri, expectedPage, "", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "/", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "//", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "test", uri2, expectedPage2);
			TestConfiguration(uri, expectedPage, "/test", uri2, expectedPage2);
			TestConfiguration(uri, expectedPage, "test/", uri2, expectedPage2);
			TestConfiguration(uri, expectedPage, "/test/", uri2, expectedPage2);


			uri = "http://example.com:81/test/";
			uri2 = "http://example.com:81/";
			expectedPage = "test/";
			expectedPage2 = "";

			TestConfiguration(uri, expectedPage, null, uri, expectedPage);
			TestConfiguration(uri, expectedPage, "", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "/", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "//", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "test", uri2, expectedPage2);
			TestConfiguration(uri, expectedPage, "/test", uri2, expectedPage2);
			TestConfiguration(uri, expectedPage, "test/", uri2, expectedPage2);
			TestConfiguration(uri, expectedPage, "/test/", uri2, expectedPage2);


			uri = "http://example.com:81/test/more";
			uri2 = "http://example.com:81/more";
			expectedPage = "test/more";
			expectedPage2 = "more";

			TestConfiguration(uri, expectedPage, null, uri, expectedPage);
			TestConfiguration(uri, expectedPage, "", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "/", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "//", uri, expectedPage);
			TestConfiguration(uri, expectedPage, "test", uri2, expectedPage2);
			TestConfiguration(uri, expectedPage, "/test", uri2, expectedPage2);
			TestConfiguration(uri, expectedPage, "test/", uri2, expectedPage2);
			TestConfiguration(uri, expectedPage, "/test/", uri2, expectedPage2);
		}
	}
}
