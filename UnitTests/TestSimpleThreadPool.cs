using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
		[TestMethod]
		public void TestThreadsAllRun()
		{
			ParallelOptions po = new ParallelOptions();
			po.MaxDegreeOfParallelism = 10;
			ConcurrentQueue<string> failures = new ConcurrentQueue<string>();
			Parallel.For(0, po.MaxDegreeOfParallelism, po, i =>
			{
				try
				{
					SimpleThreadPool p = new SimpleThreadPool("Test Pool", 0, 16, 2000, true, (e, s) => Assert.Fail(e?.ToString() + " " + s));

					Assert.AreEqual(0, p.MinThreads);
					Assert.AreEqual(16, p.MaxThreads);
					Assert.AreEqual(0, p.CurrentBusyThreads);
					Assert.AreEqual(0, p.CurrentIdleThreads);
					Assert.AreEqual(0, p.CurrentLiveThreads);

					Stopwatch sw = Stopwatch.StartNew();
					int numThreads = 12;
					long threadsHaveRun = 0;
					for (int n = 0; n < numThreads; n++)
						p.Enqueue(() =>
						{
							Interlocked.Increment(ref threadsHaveRun);
							Thread.Sleep(1000);
						});

					while (Interlocked.Read(ref threadsHaveRun) < numThreads)
						Thread.Sleep(1);

					Assert.AreEqual(0, p.MinThreads);
					Assert.AreEqual(16, p.MaxThreads);
					// At this point all the threads should still be "busy" in their sleep calls. If not, then we've failed.
					int busyThreadCount = p.CurrentBusyThreads;
					Assert.AreEqual(numThreads, busyThreadCount, "Busy thread count was " + busyThreadCount + " != " + numThreads);
					Thread.Sleep(100);
					Assert.AreEqual(Interlocked.Read(ref threadsHaveRun), numThreads);

					p.Stop();
				}
				catch (Exception ex)
				{
					failures.Enqueue(ex.ToString());
				}
			});
			if (failures.Count > 0)
				Assert.Fail("TestThreadsAllRun encountered " + failures.Count + "/" + po.MaxDegreeOfParallelism + " failures."
					+ Environment.NewLine
					+ string.Join(Environment.NewLine + Environment.NewLine, failures));
		}
	}
}
