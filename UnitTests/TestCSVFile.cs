using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestCSVFile
	{
		[TestMethod]
		public void TestEmptyCsvCreatesEmptyHeadingsAndRows()
		{
			var csv = "";
			var cf = new CSVFile(csv, false, false);
			Assert.IsNotNull(cf.Headings);
			Assert.IsNotNull(cf.Rows);
			Assert.AreEqual(0, cf.Headings.Length);
			Assert.AreEqual(0, cf.Rows.Length);
		}

		[TestMethod]
		public void TestSimpleParse()
		{
			var csv = "a,b,c\n1,2,3";
			var cf = new CSVFile(csv, false, false);
			Assert.AreEqual(2, cf.Rows.Length);
			CollectionAssert.AreEqual(new[] { "a", "b", "c" }, cf.Rows[0]);
			CollectionAssert.AreEqual(new[] { "1", "2", "3" }, cf.Rows[1]);
		}

		[TestMethod]
		public void TestHeadingsAreRecognized()
		{
			var csv = "h1,h2\nv1,v2\nv3,v4";
			var cf = new CSVFile(csv, false, true);
			CollectionAssert.AreEqual(new[] { "h1", "h2" }, cf.Headings);
			Assert.AreEqual(2, cf.Rows.Length);
			CollectionAssert.AreEqual(new[] { "v1", "v2" }, cf.Rows[0]);
			CollectionAssert.AreEqual(new[] { "v3", "v4" }, cf.Rows[1]);
		}

		[TestMethod]
		public void TestQuotedFieldsWithCommasAndEscapedQuotes()
		{
			// CSV row contains a comma in first field and escaped quotes in second field.
			var csv = "\"hello, world\",\"she said \"\"hi\"\"\",plain\n1,2,3";
			var cf = new CSVFile(csv, false, false);
			Assert.AreEqual(2, cf.Rows.Length);
			CollectionAssert.AreEqual(new[] { "hello, world", "she said \"hi\"", "plain" }, cf.Rows[0]);
			CollectionAssert.AreEqual(new[] { "1", "2", "3" }, cf.Rows[1]);
		}

		[TestMethod]
		public void TestNewlineInsideQuotedFieldIsHandled()
		{
			// The second cell contains a newline inside quotes and spans two physical lines.
			var csv = "a,\"b\nb2\",c\n1,2,3";
			var cf = new CSVFile(csv, false, false);
			Assert.AreEqual(2, cf.Rows.Length);
			Assert.AreEqual("a", cf.Rows[0][0]);
			Assert.AreEqual("b" + Environment.NewLine + "b2", cf.Rows[0][1]); // Newlines inside quoted fields are normalized to Environment.NewLine
			Assert.AreEqual("c", cf.Rows[0][2]);
		}

		[TestMethod]
		public void TestDumpToStringAndToStringEncodeFields()
		{
			var csv = "h1,h2\nv1,v2";
			var cf = new CSVFile(csv, false, true);
			// DumpToString always quotes fields via CSV.EncodeAsCsvField
			var expectedDump = string.Format("\"h1\",\"h2\"{0}\"v1\",\"v2\"", Environment.NewLine);
			Assert.AreEqual(expectedDump, cf.DumpToString());
			// ToString returns the headings row (encoded) when headings exist
			Assert.AreEqual("\"h1\",\"h2\"", cf.ToString());
		}

		[TestMethod]
		public void TestStreamingReadEnumerableProducesRows()
		{
			var csv = "a,b\n1,2\n3,4";
			using (var sr = new StringReader(csv))
			{
				var rows = CSVFile.StreamingRead(sr).ToList();
				Assert.AreEqual(3, rows.Count);
				CollectionAssert.AreEqual(new[] { "a", "b" }, rows[0]);
				CollectionAssert.AreEqual(new[] { "1", "2" }, rows[1]);
				CollectionAssert.AreEqual(new[] { "3", "4" }, rows[2]);
			}
		}

		[TestMethod]
		public void TestStreamingReadCallbackCanStopEarly()
		{
			var csv = "a,b\n1,2\n3,4";
			var seen = new List<string[]>();
			using (var sr = new StringReader(csv))
			{
				// Stop after receiving the first data row (keep headings + first data row)
				CSVFile.StreamingRead(sr, row =>
				{
					seen.Add(row);
					// stop after first row processed
					return false;
				});
			}
			Assert.AreEqual(1, seen.Count);
			CollectionAssert.AreEqual(new[] { "a", "b" }, seen[0]);
		}

		[TestMethod]
		public void TestUnequalColumnsThrowsException()
		{
			var csv = "a,b,c\n1,2\n2,3,4";
			try
			{
				var cf = new CSVFile(csv, false, false);
				Assert.Fail("Expected exception for unequal number of columns in CSV rows");
			}
			catch (Exception ex)
			{
				Assert.IsTrue(ex.Message.Contains("Unequal number of columns in CSV rows"), "Unexpected exception message: " + ex.Message);
			}
		}
	}
}
