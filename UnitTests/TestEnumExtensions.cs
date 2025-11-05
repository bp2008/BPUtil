using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace UnitTests
{
	[TestClass]
	public class TestEnumExtensions
	{
		enum TestEnumOne
		{
			[Description("A")]
			A,
			[Description("B")]
			B,
			C
		}
		enum TestEnumTwo
		{
			[Description("B")]
			A,
			[Description("A")]
			B,
			C
		}
		[TestMethod]
		public void TestToDescriptionString()
		{
			Assert.AreEqual("A", TestEnumOne.A.ToDescriptionString());
			Assert.AreEqual("B", TestEnumOne.B.ToDescriptionString());
			Assert.AreEqual("C", TestEnumOne.C.ToDescriptionString());
			Assert.AreEqual("B", TestEnumTwo.A.ToDescriptionString());
			Assert.AreEqual("A", TestEnumTwo.B.ToDescriptionString());
			Assert.AreEqual("C", TestEnumTwo.C.ToDescriptionString());
		}
		[TestMethod]
		public void TestGetDescriptionString()
		{
			Assert.AreEqual("A", TestEnumOne.A.GetDescriptionString());
			Assert.AreEqual("B", TestEnumOne.B.GetDescriptionString());
			Assert.IsNull(TestEnumOne.C.GetDescriptionString());
			Assert.AreEqual("B", TestEnumTwo.A.GetDescriptionString());
			Assert.AreEqual("A", TestEnumTwo.B.GetDescriptionString());
			Assert.IsNull(TestEnumTwo.C.GetDescriptionString());
		}
		[TestMethod]
		public void TestGetCustomAttributes()
		{
			DescriptionAttribute[] descriptionAttributes = TestEnumOne.A.GetCustomAttributes<DescriptionAttribute>().ToArray();
			Assert.AreEqual(1, descriptionAttributes.Length);
			Assert.AreEqual("A", descriptionAttributes[0].Description);

			descriptionAttributes = TestEnumOne.B.GetCustomAttributes<DescriptionAttribute>().ToArray();
			Assert.AreEqual(1, descriptionAttributes.Length);
			Assert.AreEqual("B", descriptionAttributes[0].Description);

			descriptionAttributes = TestEnumOne.C.GetCustomAttributes<DescriptionAttribute>().ToArray();
			Assert.AreEqual(0, descriptionAttributes.Length);
		}
		[TestMethod]
		public void TestTryParseWithDescription()
		{
			TestEnumOne result1;
			Assert.IsTrue(EnumExtensions.TryParseWithDescription("A", out result1) && result1 == TestEnumOne.A);
			Assert.IsTrue(EnumExtensions.TryParseWithDescription("B", out result1) && result1 == TestEnumOne.B);
			Assert.IsTrue(EnumExtensions.TryParseWithDescription("C", out result1) && result1 == TestEnumOne.C);
			TestEnumTwo result2;
			Assert.IsTrue(EnumExtensions.TryParseWithDescription("A", out result2) && result2 == TestEnumTwo.B);
			Assert.IsTrue(EnumExtensions.TryParseWithDescription("B", out result2) && result2 == TestEnumTwo.A);
			Assert.IsTrue(EnumExtensions.TryParseWithDescription("C", out result2) && result2 == TestEnumTwo.C);
		}
		[TestMethod]
		public void TestParseWithDescription()
		{
			Assert.AreEqual(TestEnumOne.A, EnumExtensions.ParseWithDescription<TestEnumOne>("A"));
			Assert.AreEqual(TestEnumOne.B, EnumExtensions.ParseWithDescription<TestEnumOne>("B"));
			Assert.AreEqual(TestEnumOne.C, EnumExtensions.ParseWithDescription<TestEnumOne>("C"));
			Expect.Exception(() => { EnumExtensions.ParseWithDescription<TestEnumOne>("D"); });

			Assert.AreEqual(TestEnumTwo.B, EnumExtensions.ParseWithDescription<TestEnumTwo>("A"));
			Assert.AreEqual(TestEnumTwo.A, EnumExtensions.ParseWithDescription<TestEnumTwo>("B"));
			Assert.AreEqual(TestEnumTwo.C, EnumExtensions.ParseWithDescription<TestEnumTwo>("C"));
			Expect.Exception(() => { EnumExtensions.ParseWithDescription<TestEnumTwo>("D"); });
		}
		[TestMethod]
		public void TestParse()
		{
			Assert.AreEqual(TestEnumOne.A, EnumExtensions.Parse<TestEnumOne>("A"));
			Assert.AreEqual(TestEnumOne.B, EnumExtensions.Parse<TestEnumOne>("B"));
			Assert.AreEqual(TestEnumOne.C, EnumExtensions.Parse<TestEnumOne>("C"));
			Expect.Exception(() => { EnumExtensions.Parse<TestEnumOne>("D"); });

			Assert.AreEqual(TestEnumTwo.A, EnumExtensions.Parse<TestEnumTwo>("A"));
			Assert.AreEqual(TestEnumTwo.B, EnumExtensions.Parse<TestEnumTwo>("B"));
			Assert.AreEqual(TestEnumTwo.C, EnumExtensions.Parse<TestEnumTwo>("C"));
			Expect.Exception(() => { EnumExtensions.Parse<TestEnumTwo>("D"); });
		}
		[TestMethod]
		public void TestParseCaseSensitivity()
		{
			// Test functionality of case sensitivity.
			Expect.Exception(() => { EnumExtensions.Parse<TestEnumTwo>("a", false); });
			Expect.Exception(() => { EnumExtensions.Parse<TestEnumTwo>("c", false); });
			Assert.AreEqual(TestEnumTwo.A, EnumExtensions.Parse<TestEnumTwo>("a", true));
			Assert.AreEqual(TestEnumTwo.C, EnumExtensions.Parse<TestEnumTwo>("c", true));

			// Case sensitivity should be enabled by default.
			Expect.Exception(() => { EnumExtensions.Parse<TestEnumTwo>("a"); });
		}
		[TestMethod]
		public void TestParseWithDescriptionCaseSensitivity()
		{
			// Test functionality of case sensitivity.
			Expect.Exception(() => { EnumExtensions.ParseWithDescription<TestEnumTwo>("a", false); });
			Expect.Exception(() => { EnumExtensions.ParseWithDescription<TestEnumTwo>("c", false); });
			Assert.AreEqual(TestEnumTwo.B, EnumExtensions.ParseWithDescription<TestEnumTwo>("a", true));
			Assert.AreEqual(TestEnumTwo.C, EnumExtensions.ParseWithDescription<TestEnumTwo>("c", true));

			// Case sensitivity should be enabled by default.
			Expect.Exception(() => { EnumExtensions.ParseWithDescription<TestEnumTwo>("a"); });
		}
		[TestMethod]
		public void TestTryParseWithDescriptionCaseSensitivity()
		{
			// Test functionality of case sensitivity.
			Assert.IsFalse(EnumExtensions.TryParseWithDescription<TestEnumTwo>("a", false, out TestEnumTwo _));
			Assert.IsFalse(EnumExtensions.TryParseWithDescription<TestEnumTwo>("c", false, out TestEnumTwo _));
			Assert.IsTrue(EnumExtensions.TryParseWithDescription<TestEnumTwo>("A", true, out TestEnumTwo _1) && _1 == TestEnumTwo.B);
			Assert.IsTrue(EnumExtensions.TryParseWithDescription<TestEnumTwo>("C", true, out TestEnumTwo _2) && _2 == TestEnumTwo.C);

			// Case sensitivity should be enabled by default.
			Assert.IsFalse(EnumExtensions.TryParseWithDescription<TestEnumTwo>("a", out TestEnumTwo _));
		}
	}
}
