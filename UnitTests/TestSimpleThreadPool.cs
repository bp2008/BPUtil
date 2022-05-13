using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	/// <summary>
	/// The tests in this class could be rewritten using EventWaitHandle to speed it up and eliminate most of the thread sleeping.
	/// </summary>
	[TestClass]
	public class TestSimpleThreadPool
	{
		[TestMethod]
		public void TestThreadPoolStopping()
		{
			SimpleThreadPool p = new SimpleThreadPool("Test Pool", 2, 6, 60000, true, (e, s) => Assert.Fail(e?.ToString() + " " + s));
			Thread.Sleep(250);

			Assert.AreEqual(2, p.MinThreads);
			Assert.AreEqual(6, p.MaxThreads);
			Assert.AreEqual(0, p.CurrentBusyThreads);
			Assert.AreEqual(2, p.CurrentIdleThreads);
			Assert.AreEqual(2, p.CurrentLiveThreads);

			p.Enqueue(() => { Thread.Sleep(1000); });

			Thread.Sleep(250);

			Assert.AreEqual(2, p.MinThreads);
			Assert.AreEqual(6, p.MaxThreads);
			Assert.AreEqual(1, p.CurrentBusyThreads);
			Assert.AreEqual(1, p.CurrentIdleThreads);
			Assert.AreEqual(2, p.CurrentLiveThreads);

			p.Stop();
			Thread.Sleep(250);

			Assert.AreEqual(2, p.MinThreads);
			Assert.AreEqual(6, p.MaxThreads);
			Assert.AreEqual(1, p.CurrentBusyThreads);
			Assert.AreEqual(0, p.CurrentIdleThreads);
			Assert.AreEqual(1, p.CurrentLiveThreads);

			Thread.Sleep(750);

			Assert.AreEqual(2, p.MinThreads);
			Assert.AreEqual(6, p.MaxThreads);
			Assert.AreEqual(0, p.CurrentBusyThreads);
			Assert.AreEqual(0, p.CurrentIdleThreads);
			Assert.AreEqual(0, p.CurrentLiveThreads);
		}
		[TestMethod]
		public void TestThreadExpiration()
		{
			SimpleThreadPool p = new SimpleThreadPool("Test Pool", 0, 2, 200, true, (e, s) => Assert.Fail(e?.ToString() + " " + s));
			Thread.Sleep(250);

			Assert.AreEqual(0, p.MinThreads);
			Assert.AreEqual(2, p.MaxThreads);
			Assert.AreEqual(0, p.CurrentBusyThreads);
			Assert.AreEqual(0, p.CurrentIdleThreads);
			Assert.AreEqual(0, p.CurrentLiveThreads);

			p.Enqueue(() => { Thread.Sleep(200); });

			Assert.AreEqual(0, p.MinThreads);
			Assert.AreEqual(2, p.MaxThreads);
			Assert.AreEqual(1, p.CurrentBusyThreads);
			Assert.AreEqual(0, p.CurrentIdleThreads);
			Assert.AreEqual(1, p.CurrentLiveThreads);

			Thread.Sleep(250);

			Assert.AreEqual(0, p.MinThreads);
			Assert.AreEqual(2, p.MaxThreads);
			Assert.AreEqual(0, p.CurrentBusyThreads);
			Assert.AreEqual(1, p.CurrentIdleThreads);
			Assert.AreEqual(1, p.CurrentLiveThreads);

			Thread.Sleep(250);

			Assert.AreEqual(0, p.MinThreads);
			Assert.AreEqual(2, p.MaxThreads);
			Assert.AreEqual(0, p.CurrentBusyThreads);
			Assert.AreEqual(0, p.CurrentIdleThreads);
			Assert.AreEqual(0, p.CurrentLiveThreads);
		}
	}
}
