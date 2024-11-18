using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using BPUtil;
using BPUtil.SimpleHttp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestHttpHeaderCollection
	{
		[TestMethod]
		public void TestCookieCombining()
		{
			HttpHeaderCollection headers = new HttpHeaderCollection();
			headers.Add("Cookie", "n1=v");
			headers.Add("Cookie", "n2=v");
			Assert.AreEqual(1, headers.Count());
			// "Cookie" header uses "; " as the separator instead of ", " which is standard for normal header combining.
			Assert.AreEqual("n1=v; n2=v", headers.GetHeaderArray()[0].Value);

			// "Set-Cookie" should not be combined.
			headers.Clear();
			Assert.AreEqual(0, headers.Count());

			headers.Add("Set-Cookie", "n1=v");
			headers.Add("Set-Cookie", "n2=v");
			headers.Add("Set-Cookie", "n3=v");
			Assert.AreEqual(3, headers.Count());
			Assert.AreEqual("n1=v", headers.GetHeaderArray()[0].Value);
			Assert.AreEqual("n2=v", headers.GetHeaderArray()[1].Value);
			Assert.AreEqual("n3=v", headers.GetHeaderArray()[2].Value);
		}
		[TestMethod]
		public void TestHeaderClear()
		{
			HttpHeaderCollection headers = new HttpHeaderCollection();
			headers.Add("MY-HEADER", "MY-VALUE");
			Assert.AreEqual(1, headers.Count());
			headers.Clear();
			Assert.AreEqual(0, headers.Count());
		}
		[TestMethod]
		public void TestTitleCase()
		{
			HttpHeaderCollection headers = new HttpHeaderCollection(HeaderNameCase.TitleCase);
			headers.Add("MY-HEADER", "MY-VALUE");
			Assert.AreEqual("My-Header", headers.GetHeaderArray()[0].Key);
			Assert.AreEqual("MY-VALUE", headers.GetHeaderArray()[0].Value);
		}
		[TestMethod]
		public void TestLowerCase()
		{
			HttpHeaderCollection headers = new HttpHeaderCollection(HeaderNameCase.LowerCase);
			headers.Add("MY-HEADER", "MY-VALUE");
			Assert.AreEqual("my-header", headers.GetHeaderArray()[0].Key);
			Assert.AreEqual("MY-VALUE", headers.GetHeaderArray()[0].Value);
		}
		[TestMethod]
		public void TestHeaderCountAndRemove()
		{
			HttpHeaderCollection headers = new HttpHeaderCollection();
			Assert.AreEqual(0, headers.Count());
			Assert.AreEqual(0, headers.GetHeaderArray().Length);
			headers.Add("h1", "v");
			Assert.AreEqual(1, headers.Count());
			Assert.AreEqual(1, headers.GetHeaderArray().Length);
			headers.Add("h2", "v");
			Assert.AreEqual(2, headers.Count());
			Assert.AreEqual(2, headers.GetHeaderArray().Length);
			headers.Add("h3", "v");
			Assert.AreEqual(3, headers.Count());
			Assert.AreEqual(3, headers.GetHeaderArray().Length);
			headers.Remove("h2");
			Assert.AreEqual(2, headers.Count());
			Assert.AreEqual(2, headers.GetHeaderArray().Length);
			headers.Remove("h1");
			Assert.AreEqual(1, headers.Count());
			Assert.AreEqual(1, headers.GetHeaderArray().Length);
			headers.Remove("h3");
			Assert.AreEqual(0, headers.Count());
			Assert.AreEqual(0, headers.GetHeaderArray().Length);
		}
		[TestMethod]
		public void TestSetValueCanDeleteSingleHeader()
		{
			HttpHeaderCollection headers = new HttpHeaderCollection();
			headers.Add("My-Header", "A");
			Assert.AreEqual("A", headers.GetHeaderArray()[0].Value);
			Assert.AreEqual(1, headers.Count());

			headers.Set("My-header", null);
			Assert.AreEqual(0, headers.Count());
		}
		[TestMethod]
		public void TestSetValueCanDeleteMultipleHeaders()
		{
			HttpHeaderCollection headers = new HttpHeaderCollection();
			headers.Add("My-Header", "A");
			headers.Add("My-Header", "B");
			headers.Add("My-HEADER", "C");
			Assert.AreEqual(3, headers.Count());

			headers.Set("MY-header", null);
			Assert.AreEqual(0, headers.Count());
		}
		[TestMethod]
		public void TestSetValueCanReplaceSingleHeader()
		{
			HttpHeaderCollection headers = new HttpHeaderCollection();
			headers.Add("My-Header", "A");
			Assert.AreEqual("A", headers["MY-HEADER"]);

			headers.Set("MY-header", "V");
			Assert.AreEqual("V", headers["MY-HEADER"]);

			headers.Set("MY-header", "");
			Assert.AreEqual("", headers["MY-HEADER"]);
		}
		[TestMethod]
		public void TestSetValueCanReplaceMultipleHeaders()
		{
			HttpHeaderCollection headers = new HttpHeaderCollection();
			headers.Add("My-Header", "A");
			headers.Add("My-Header", "B");
			headers.Add("My-HEADER", "C");
			Assert.AreEqual("A, B, C", headers["MY-HEADER"]);

			headers.Set("MY-header", "V");
			Assert.AreEqual("V", headers["MY-HEADER"]);
		}
		[TestMethod]
		public void TestSetValueCanAddHeader()
		{
			HttpHeaderCollection headers = new HttpHeaderCollection();
			headers.Set("My-Header", "A");
			Assert.AreEqual(1, headers.Count());
			Assert.AreEqual("A", headers["MY-HEADER"]);

			headers.Set("MY-header2", "V");
			Assert.AreEqual(2, headers.Count());
			Assert.AreEqual("V", headers["MY-HEADER2"]);
		}
		[TestMethod]
		public void TestGetterAndSetter()
		{
			HttpHeaderCollection headers = new HttpHeaderCollection();
			headers["My-Header"] = "A";
			Assert.AreEqual(1, headers.Count());
			Assert.AreEqual("A", headers["MY-HEADER"]);

			headers["My-Header"] = "V";
			Assert.AreEqual(1, headers.Count());
			Assert.AreEqual("V", headers["MY-HEADER"]);

			headers.Add("My-Header", "A");
			headers.Add("My-Header", "B");
			headers.Add("My-HEADER", "C");
			Assert.AreEqual(4, headers.Count());
			Assert.AreEqual("V, A, B, C", headers["MY-HEADER"]);

			headers["My-Header"] = null;
			Assert.AreEqual(0, headers.Count());
		}
		[TestMethod]
		public void TestNonDuplicatedHeader()
		{
			HttpHeaderCollection headers = new HttpHeaderCollection();
			headers.Add("My-Header", "A");
			Assert.AreEqual("A", headers.Get("My-Header"));
			Assert.AreEqual("A", headers["My-Header"]);
			CollectionAssert.AreEqual(new string[] { "A" }, headers.GetValues("My-Header"));
			Assert.AreEqual(1, headers.Count());
			Assert.AreEqual(1, headers.GetHeaderArray().Length);
			Assert.AreEqual("A", headers.GetHeaderArray()[0].Value);
		}
		[TestMethod]
		public void TestDuplicatedHeader()
		{
			HttpHeaderCollection headers = new HttpHeaderCollection();
			headers.Add("My-Header", "B");
			headers.Add("My-Header", "A");
			Assert.AreEqual("B, A", headers.Get("My-Header"));
			Assert.AreEqual("B, A", headers["My-Header"]);
			CollectionAssert.AreEqual(new string[] { "B", "A" }, headers.GetValues("My-Header"));
			Assert.AreEqual(2, headers.Count());
			Assert.AreEqual(2, headers.GetHeaderArray().Length);
			Assert.AreEqual("B", headers.GetHeaderArray()[0].Value);
			Assert.AreEqual("A", headers.GetHeaderArray()[1].Value);

			headers.Add("My-Header", "C");
			Assert.AreEqual("B, A, C", headers.Get("My-Header"));
			Assert.AreEqual("B, A, C", headers["My-Header"]);
			CollectionAssert.AreEqual(new string[] { "B", "A", "C" }, headers.GetValues("My-Header"));
			Assert.AreEqual(3, headers.Count());
			Assert.AreEqual(3, headers.GetHeaderArray().Length);
			Assert.AreEqual("B", headers.GetHeaderArray()[0].Value);
			Assert.AreEqual("A", headers.GetHeaderArray()[1].Value);
			Assert.AreEqual("C", headers.GetHeaderArray()[2].Value);
		}
		[TestMethod]
		public void TestGetHeaders()
		{
			HttpHeaderCollection headers = new HttpHeaderCollection();
			headers.Add("CONTENT-LENGTH", "123");
			headers.Add("CONTENT-TYPE", "text/plain");
			Assert.AreEqual(2, headers.Count());
			Assert.AreEqual(2, headers.GetHeaderArray().Length);

			Assert.AreEqual(1, headers.GetHeaders("Content-Length").Length);
			Assert.AreEqual("Content-Length", headers.GetHeaders("Content-Length")[0].Key);
			Assert.AreEqual("123", headers.GetHeaders("Content-Length")[0].Value);

			Assert.AreEqual(1, headers.GetHeaders("Content-Type").Length);
			Assert.AreEqual("Content-Type", headers.GetHeaders("Content-Type")[0].Key);
			Assert.AreEqual("text/plain", headers.GetHeaders("Content-Type")[0].Value);
		}
		[TestMethod]
		public void TestCaseSensitivity()
		{
			// Default case is title case
			HttpHeaderCollection headers = new HttpHeaderCollection();
			headers.Add("CONTENT-LENGTH", "123");

			HttpHeader[] h = headers.GetHeaders("CONTENT-length");
			Assert.AreEqual(1, h.Length);
			Assert.AreEqual("Content-Length", h[0].Key);
			Assert.AreEqual("123", h[0].Value);

			headers = new HttpHeaderCollection(HeaderNameCase.LowerCase);
			headers.Add("CONTENT-LENGTH", "123");

			h = headers.GetHeaders("CONTENT-length");
			Assert.AreEqual(1, h.Length);
			Assert.AreEqual("content-length", h[0].Key);
			Assert.AreEqual("123", h[0].Value);
		}
		[TestMethod]
		public void TestHeaderNullAndEmptyAndWhitespaceInputs()
		{
			HttpHeaderCollection headers = new HttpHeaderCollection();
			Expect.Exception(() =>
			{
				headers.Add(null, "value");
			}, "Expected exception when using null key input.");
			Assert.AreEqual(0, headers.Count());

			Expect.Exception(() =>
			{
				headers.Add("", "value");
			}, "Expected exception when using empty key input.");
			Assert.AreEqual(0, headers.Count());

			Expect.Exception(() =>
			{
				headers.Add(" ", "value");
			}, "Expected exception when using \" \" key input.");
			Assert.AreEqual(0, headers.Count());

			Expect.Exception(() =>
			{
				headers.Add("Key", null);
			}, "Expected exception when using null value input.");
			Assert.AreEqual(0, headers.Count());

			headers.Add("Key", "");
			Assert.AreEqual(1, headers.Count());
			Assert.AreEqual("", headers["Key"]);
			headers.Clear();

			headers.Add("Key", " ");
			Assert.AreEqual(1, headers.Count());
			Assert.AreEqual(" ", headers["Key"]);
			headers.Clear();
		}
		[TestMethod]
		public void TestHeaderKeyLimit()
		{
			Assert.AreEqual(16384, HttpHeader.MAX_HEADER_KEY_LENGTH);
			Assert.AreEqual(32768, HttpHeader.MAX_HEADER_VALUE_LENGTH);

			StringBuilder sb = new StringBuilder();
			while (sb.Length < HttpHeader.MAX_HEADER_KEY_LENGTH)
				sb.Append('A');

			HttpHeaderCollection headers = new HttpHeaderCollection();
			headers.Add(sb.ToString(), "value"); // Accept header at length limit
			Assert.AreEqual(1, headers.Count());
			headers.Clear();

			sb.Append('A');
			Expect.Exception(() =>
			{
				headers.Add(sb.ToString(), "value");
			}, "Expected exception when exceeding length limit.");

			Assert.AreEqual(0, headers.Count());

			while (sb.Length < HttpHeader.MAX_HEADER_VALUE_LENGTH)
				sb.Append('A');
			headers.Add("Key", sb.ToString()); // Accept header at length limit
			Assert.AreEqual(1, headers.Count());
			headers.Clear();

			sb.Append('A');
			Expect.Exception(() =>
			{
				headers.Add("Key", sb.ToString());
			}, "Expected exception when exceeding length limit.");
			Assert.AreEqual(0, headers.Count());
		}
	}
}
