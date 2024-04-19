using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Net;

namespace UnitTests
{
	[TestClass]
	public class TestIpRangeLite
	{
		[TestMethod]
		public void Test_IpRangeLite()
		{
			IpRangeLite ipr = new IpRangeLite("127.0.0.1", "127.255.255.255");
			Assert.AreEqual("127.0.0.1", ipr.low.ToString());
			Assert.AreEqual("127.255.255.255", ipr.high.ToString());

			Assert.IsTrue(ipr.Contains("127.30.40.50"));
			Assert.IsTrue(ipr.Contains(IPAddress.Parse("127.30.40.50")));

			Assert.IsFalse(ipr.Contains("192.168.0.1"));
			Assert.IsFalse(ipr.Contains(IPAddress.Parse("192.168.0.1")));

			// Test edge cases
			Assert.IsTrue(ipr.Contains("127.0.0.1"));
			Assert.IsTrue(ipr.Contains(IPAddress.Parse("127.0.0.1")));
			Assert.IsTrue(ipr.Contains("127.255.255.255"));
			Assert.IsTrue(ipr.Contains(IPAddress.Parse("127.255.255.255")));

			Assert.IsFalse(ipr.Contains("126.255.255.255"));
			Assert.IsFalse(ipr.Contains(IPAddress.Parse("126.255.255.255")));
			Assert.IsFalse(ipr.Contains("128.0.0.1"));
			Assert.IsFalse(ipr.Contains(IPAddress.Parse("128.0.0.1")));

			// Test another IP range, declared in reverse order.
			IpRangeLite ipr2 = new IpRangeLite("172.16.30.52", "172.16.30.50");
			Assert.AreEqual("172.16.30.50", ipr2.low.ToString());
			Assert.AreEqual("172.16.30.52", ipr2.high.ToString());

			Assert.IsFalse(ipr2.Contains("172.16.30.49"));
			Assert.IsTrue(ipr2.Contains("172.16.30.50"));
			Assert.IsTrue(ipr2.Contains("172.16.30.51"));
			Assert.IsTrue(ipr2.Contains("172.16.30.52"));
			Assert.IsFalse(ipr2.Contains("172.16.30.53"));

			// Test with IPv6
			IpRangeLite ipr3 = new IpRangeLite("2605::7", "2605::5");
			Assert.IsFalse(ipr3.Contains("2605::4"));
			Assert.IsTrue(ipr3.Contains("2605::5"));
			Assert.IsTrue(ipr3.Contains("2605::6"));
			Assert.IsTrue(ipr3.Contains("2605::7"));
			Assert.IsFalse(ipr3.Contains("2605::8"));
			Assert.IsFalse(ipr3.Contains("2604::6"));
			Assert.IsFalse(ipr3.Contains("2607::6"));

			Assert.IsFalse(ipr3.Contains("2604:ff00::6"));
			Assert.IsFalse(ipr3.Contains("2604::ff00:6"));
		}
	}
}
