using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;

namespace UnitTests
{
	[TestClass]
	public class TestCSV
	{
		[TestMethod]
		public void TestLooseEncodeAsCsvField()
		{
			Assert.AreEqual("Test", CSV.LooseEncodeAsCsvField("Test"));
			Assert.AreEqual("\"Te,st\"", CSV.LooseEncodeAsCsvField("Te,st"));
			Assert.AreEqual("\"Te\rst\"", CSV.LooseEncodeAsCsvField("Te\rst"));
			Assert.AreEqual("\"Te\nst\"", CSV.LooseEncodeAsCsvField("Te\nst"));
			Assert.AreEqual("\"Te\r\nst\"", CSV.LooseEncodeAsCsvField("Te\r\nst"));
			Assert.AreEqual("\"Te\"\"st\"", CSV.LooseEncodeAsCsvField("Te\"st"));
			Assert.AreEqual("\"Te\"\"\"\"st\"", CSV.LooseEncodeAsCsvField("Te\"\"st"));
			Assert.AreEqual("\"\"\"quoted string\"\"\"", CSV.LooseEncodeAsCsvField("\"quoted string\""));
			Assert.AreEqual("\t", CSV.LooseEncodeAsCsvField("\t"));
			Assert.AreEqual(GetAllAllowedUnescapedCSVCharacters(), CSV.LooseEncodeAsCsvField(GetAllAllowedUnescapedCSVCharacters()));
		}
		[TestMethod]
		public void TestEncodeAsCsvField()
		{
			Assert.AreEqual("\"Test\"", CSV.EncodeAsCsvField("Test"));
			Assert.AreEqual("\"Te,st\"", CSV.EncodeAsCsvField("Te,st"));
			Assert.AreEqual("\"Te\rst\"", CSV.EncodeAsCsvField("Te\rst"));
			Assert.AreEqual("\"Te\nst\"", CSV.EncodeAsCsvField("Te\nst"));
			Assert.AreEqual("\"Te\r\nst\"", CSV.EncodeAsCsvField("Te\r\nst"));
			Assert.AreEqual("\"Te\"\"st\"", CSV.EncodeAsCsvField("Te\"st"));
			Assert.AreEqual("\"Te\"\"\"\"st\"", CSV.EncodeAsCsvField("Te\"\"st"));
			Assert.AreEqual("\"\"\"quoted string\"\"\"", CSV.EncodeAsCsvField("\"quoted string\""));
			Assert.AreEqual("\"\"", CSV.EncodeAsCsvField("\t"));
			Assert.AreEqual("\"" + GetAllAllowedUnescapedCSVCharacters() + "\"", CSV.EncodeAsCsvField(GetAllAllowedUnescapedCSVCharacters()));
		}
		[TestMethod]
		public void TestStripInvalidCsvCharacters()
		{
			Assert.AreEqual("Test", CSV.StripInvalidCsvCharacters("Test"));
			Assert.AreEqual("Te,st", CSV.StripInvalidCsvCharacters("Te,st"));
			Assert.AreEqual("Te\rst", CSV.StripInvalidCsvCharacters("Te\rst"));
			Assert.AreEqual("Te\nst", CSV.StripInvalidCsvCharacters("Te\nst"));
			Assert.AreEqual("Te\r\nst", CSV.StripInvalidCsvCharacters("Te\r\nst"));
			Assert.AreEqual("Te\"st", CSV.StripInvalidCsvCharacters("Te\"st"));
			Assert.AreEqual("Te\"\"st", CSV.StripInvalidCsvCharacters("Te\"\"st"));
			Assert.AreEqual("\"quoted string\"", CSV.StripInvalidCsvCharacters("\"quoted string\""));
			Assert.AreEqual("", CSV.StripInvalidCsvCharacters("\t"));
			Assert.AreEqual(GetAllAllowedUnescapedCSVCharacters(), CSV.StripInvalidCsvCharacters(GetAllAllowedUnescapedCSVCharacters()));
		}
		private string GetAllAllowedUnescapedCSVCharacters()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append((char)32);
			sb.Append((char)33);
			for (char c = (char)35; c <= (char)43; c++)
				sb.Append(c);
			for (char c = (char)45; c <= (char)126; c++)
				sb.Append(c);
			return sb.ToString();
		}
		[TestMethod]
		public void TestRemoveTrailingEmptyCsvValues()
		{
			string input = "Line1\r\n"
				+ "Line2,,Line2,,\r\n"
				+ "Line3,,Line3,\"\",\r\n"
				+ "Line4,,Line4,,\"\"\r\n"
				+ ",\"\",\r\n"
				+ ",\"\r\n\",";
			string expected = "Line1\r\n"
				+ "Line2,,Line2\r\n"
				+ "Line3,,Line3\r\n"
				+ "Line4,,Line4\r\n"
				+ "\r\n"
				+ ",\"\r\n\"";
			string output = CSV.RemoveTrailingEmptyCsvValues(input);
			Assert.AreEqual(expected, output);
		}
	}
}
