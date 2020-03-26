using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestObjectCache
	{
		[TestMethod]
		public void ObjectCacheMiscTypes()
		{
			{
				ObjectCache<long, bool> c = new ObjectCache<long, bool>(10000, 10);
				c.Add(0, false);
				c.Add(1, true);
				c.Add(2, false);
				c.Add(3, true);
				for (long n = -10; n < 10; n++)
				{
					if (n == 1 || n == 3)
						Assert.IsTrue(c.Get(n));
					else
						Assert.IsFalse(c.Get(n));
				}
			}
			{
				ObjectCache<sbyte, bool> c = new ObjectCache<sbyte, bool>(10000, 10);
				c.Add(0, false);
				c.Add(1, true);
				c.Add(2, false);
				c.Add(3, true);
				for (sbyte n = -10; n < 10; n++)
				{
					if (n == 1 || n == 3)
						Assert.IsTrue(c.Get(n));
					else
						Assert.IsFalse(c.Get(n));
				}
			}
			{
				ObjectCache<int, object> c = new ObjectCache<int, object>(10000, 10);
				c.Add(0, null);
				c.Add(1, true);
				c.Add(2, "test");
				Assert.IsNull(c.Get(0));
				Assert.IsTrue((bool)c.Get(1));
				Assert.AreEqual("test", (string)c.Get(2));
				Assert.IsNull(c.Get(3));

				// Test cache overflow
				c.Add(3, new byte[20000]);
				Assert.IsNull(c.Get(0));
				Assert.IsNull(c.Get(1));
				Assert.IsNull(c.Get(2));
				Assert.IsNull(c.Get(3));
			}
		}
	}
}
