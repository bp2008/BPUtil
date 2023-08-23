using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
		public void TestValueCombining()
		{
			HttpHeaderCollection headers = new HttpHeaderCollection();
			headers.Add("My-Header", "MY-VALUE");
			Assert.AreEqual(1, headers.Count());
			Assert.AreEqual(headers.GetHeaderArray().Length, headers.Count());
			Assert.AreEqual("MY-VALUE", headers.GetHeaderArray()[0].Value);

			// Adding the same header again should not increase the count, because the values will be combined.
			headers.Add("MY-HEADER", "MY-VALUE");
			Assert.AreEqual(1, headers.Count());
			Assert.AreEqual(headers.GetHeaderArray().Length, headers.Count());
			Assert.AreEqual("MY-VALUE,MY-VALUE", headers.GetHeaderArray()[0].Value);

			headers.Add("MY-HEADER2", "MY-VALUE");
			Assert.AreEqual(2, headers.Count());
			Assert.AreEqual(headers.GetHeaderArray().Length, headers.Count());
			Assert.AreEqual("MY-VALUE,MY-VALUE", headers.GetHeaderArray()[0].Value);
			Assert.AreEqual("MY-VALUE", headers.GetHeaderArray()[1].Value);


			// "Cookie" header uses "; " as the separator instead of ","
			headers.Clear();
			Assert.AreEqual(0, headers.Count());

			headers.Add("Cookie", "n1=v");
			headers.Add("Cookie", "n2=v");
			Assert.AreEqual(1, headers.Count());
			Assert.AreEqual("n1=v; n2=v", headers.GetHeaderArray()[0].Value);

			// "Set-Cookie" allows multiple values.
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
		public void TestSetValue()
		{
			HttpHeaderCollection headers = new HttpHeaderCollection();
			headers.Add("My-Header", "A");
			Assert.AreEqual("A", headers.GetHeaderArray()[0].Value);

			headers.Add("My-Header", "B");
			Assert.AreEqual("A,B", headers.GetHeaderArray()[0].Value);

			headers.Set("My-Header", "C");
			Assert.AreEqual("C", headers.GetHeaderArray()[0].Value);

			headers.Set("My-Header", new string[] { "A", "B" });
			Assert.AreEqual("A,B", headers.GetHeaderArray()[0].Value);

			Assert.AreEqual(1, headers.Count());

			headers.Set("Set-Cookie", "n1=v");
			Assert.AreEqual(2, headers.Count());
			Assert.AreEqual("n1=v", headers.GetHeaderArray()[1].Value);

			headers.Set("Set-Cookie", "n2=v");
			Assert.AreEqual(2, headers.Count());
			Assert.AreEqual("n2=v", headers.GetHeaderArray()[1].Value);

			headers.Set("Set-Cookie", new string[] { "n3=v", "n4=v" });
			Assert.AreEqual(3, headers.Count());
			Assert.AreEqual("n3=v", headers.GetHeaderArray()[1].Value);
			Assert.AreEqual("n4=v", headers.GetHeaderArray()[2].Value);

			headers.Set("Set-Cookie", "n2=v");
			Assert.AreEqual(2, headers.Count());
			Assert.AreEqual("n2=v", headers.GetHeaderArray()[1].Value);
		}
		[TestMethod]
		public void TestGetValue()
		{
			HttpHeaderCollection headers = new HttpHeaderCollection();
			headers.Add("My-Header", "A");
			CollectionAssert.AreEqual(new string[] { "A" }, headers.GetValues("My-Header"));

			headers.Add("My-Header", "B");
			CollectionAssert.AreEqual(new string[] { "A,B" }, headers.GetValues("My-Header"));

			headers.Set("My-Header", "C");
			CollectionAssert.AreEqual(new string[] { "C" }, headers.GetValues("My-Header"));
			Assert.AreEqual("C", headers.GetHeaderArray()[0].Value);

			headers.Set("My-Header", new string[] { "A", "B" });
			CollectionAssert.AreEqual(new string[] { "A,B" }, headers.GetValues("My-Header"));

			Assert.AreEqual(1, headers.Count());

			headers.Set("Set-Cookie", "n1=v");
			Assert.AreEqual(2, headers.Count());
			CollectionAssert.AreEqual(new string[] { "n1=v" }, headers.GetValues("SET-cookie"));

			headers.Set("Set-Cookie", "n2=v");
			Assert.AreEqual(2, headers.Count());
			CollectionAssert.AreEqual(new string[] { "n2=v" }, headers.GetValues("Set-Cookie"));

			headers.Set("Set-Cookie", new string[] { "n3=v", "n4=v" });
			Assert.AreEqual(3, headers.Count());
			Assert.AreEqual("n3=v", headers.GetHeaderArray()[1].Value);
			Assert.AreEqual("n4=v", headers.GetHeaderArray()[2].Value);
			CollectionAssert.AreEqual(new string[] { "n3=v", "n4=v" }, headers.GetValues("Set-Cookie"));

			headers.Set("Set-Cookie", "n2=v");
			Assert.AreEqual(2, headers.Count());
			CollectionAssert.AreEqual(new string[] { "n2=v" }, headers.GetValues("Set-Cookie"));
		}
	}
}
