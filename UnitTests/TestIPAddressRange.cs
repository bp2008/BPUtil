using Microsoft.VisualStudio.TestTools.UnitTesting;
using BPUtil;

namespace UnitTests
{
	[TestClass]
	public class TestIPAddressRange
	{
		[TestMethod]
		public void TestSingleAddress()
		{
			IPAddressRange range = new IPAddressRange("127.0.0.1");
			Assert.IsTrue(range.IsInRange("127.0.0.1"));
			Assert.IsFalse(range.IsInRange("127.0.0.2"));
		}

		[TestMethod]
		public void TestAddressRange()
		{
			IPAddressRange range = new IPAddressRange("192.168.0.1 - 192.168.1.255");
			Assert.IsFalse(range.IsInRange("192.168.0.0"));
			Assert.IsTrue(range.IsInRange("192.168.0.1"));
			Assert.IsTrue(range.IsInRange("192.168.0.100"));
			Assert.IsTrue(range.IsInRange("192.168.0.255"));
			Assert.IsTrue(range.IsInRange("192.168.1.1"));
			Assert.IsTrue(range.IsInRange("192.168.1.100"));
			Assert.IsTrue(range.IsInRange("192.168.1.255"));
			Assert.IsFalse(range.IsInRange("192.168.2.1"));
			Assert.IsFalse(range.IsInRange("192.168.2.100"));
			Assert.IsFalse(range.IsInRange("192.168.2.255"));
		}

		[TestMethod]
		public void TestSubnet()
		{
			IPAddressRange range = new IPAddressRange("192.168.0.1/24");
			Assert.IsTrue(range.IsInRange("192.168.0.0"));
			Assert.IsTrue(range.IsInRange("192.168.0.1"));
			Assert.IsTrue(range.IsInRange("192.168.0.100"));
			Assert.IsTrue(range.IsInRange("192.168.0.255"));
			Assert.IsFalse(range.IsInRange("192.168.1.1"));
			Assert.IsFalse(range.IsInRange("192.168.1.100"));
			Assert.IsFalse(range.IsInRange("192.168.1.255"));
		}

		[TestMethod]
		public void TestCompareTo()
		{
			IPAddressRange range1 = new IPAddressRange("192.168.0.1/24");
			IPAddressRange range2 = new IPAddressRange("192.168.1.0/24");
			IPAddressRange range3 = new IPAddressRange("192.168.0.0 - 192.168.0.255");

			Assert.IsTrue(range1.CompareTo(range2) < 0);
			Assert.IsTrue(range2.CompareTo(range1) > 0);
			Assert.IsTrue(range1.CompareTo(range3) == 0);
		}

		[TestMethod]
		public void TestEqualityOperators()
		{
			IPAddressRange range1 = new IPAddressRange("192.168.0.1/24");
			IPAddressRange range2 = new IPAddressRange("192.168.0.1/24");
			IPAddressRange range3 = new IPAddressRange("192.168.1.0/24");

			Assert.IsTrue(range1 == range2);
			Assert.IsFalse(range1 == range3);
			Assert.IsTrue(range1 != range3);
			Assert.IsFalse(range1 != range2);
		}

	}
}