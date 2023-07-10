using System;
using System.Collections.Generic;
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
		private static void SetUri(HttpProcessor p, Uri uri)
		{
			PrivateAccessor.SetReadOnlyPropertyValue(p, "request_url", uri);
			PrivateAccessor.SetReadOnlyPropertyValue(p, "requestedPage", uri.AbsolutePath.StartsWith("/") ? uri.AbsolutePath.Substring(1) : uri.AbsolutePath);
		}
		private static void TestConfiguration(string uriBefore, string requestedPageBefore, string appPath, string uriAfter, string requestedPageAfter)
		{
			HttpProcessor p = new HttpProcessor(null, null, null, AllowedConnectionTypes.http);
			Uri a = new Uri(uriBefore);
			SetUri(p, a);
			Assert.AreEqual(a, p.request_url);
			Assert.AreEqual(requestedPageBefore, p.requestedPage);

			p.RemoveAppPath(appPath);

			Uri b = new Uri(uriAfter);
			Assert.AreEqual(b, p.request_url);
			Assert.AreEqual(requestedPageAfter, p.requestedPage);
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
