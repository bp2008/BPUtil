using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BPUtil;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace UnitTests
{
	[TestClass]
	public class TestBackupRotate
	{
		[TestMethod]
		public void TestDetermineBackupsToDelete_Basic()
		{
			BackupRotate br_oneday = new BackupRotate(TimeSpan.FromDays(1));

			List<DateTime> input1 = new List<DateTime>();
			for (int i = 1; i <= 28; i++)
				input1.Add(new DateTime(2022, 2, i));

			DateTime[] expected1k = new DateTime[]
			{
				new DateTime(2022, 2, 26),
				new DateTime(2022, 2, 24),
				new DateTime(2022, 2, 23),
				new DateTime(2022, 2, 22),
				new DateTime(2022, 2, 20),
				new DateTime(2022, 2, 19),
				new DateTime(2022, 2, 18),
				new DateTime(2022, 2, 17),
				new DateTime(2022, 2, 16),
				new DateTime(2022, 2, 15),
				new DateTime(2022, 2, 14),
				new DateTime(2022, 2, 12),
				new DateTime(2022, 2, 11),
				new DateTime(2022, 2, 10),
				new DateTime(2022, 2, 9),
				new DateTime(2022, 2, 8),
				new DateTime(2022, 2, 7),
				new DateTime(2022, 2, 6),
				new DateTime(2022, 2, 5),
				new DateTime(2022, 2, 4),
				new DateTime(2022, 2, 3),
				new DateTime(2022, 2, 2)
			};
			DateTime[] actual1k = br_oneday.DetermineBackupsToDelete(input1);

			Expect.Equal(expected1k, actual1k);
		}
		[TestMethod]
		public void TestDetermineBackupsToKeep_Basic()
		{
			BackupRotate br_oneday = new BackupRotate(TimeSpan.FromDays(1));

			List<DateTime> input1 = new List<DateTime>();
			for (int i = 1; i <= 28; i++)
				input1.Add(new DateTime(2022, 2, i));

			DateTime[] expected1k = new DateTime[]
			{
				new DateTime(2022, 2, 28),
				new DateTime(2022, 2, 27),
				new DateTime(2022, 2, 25),
				new DateTime(2022, 2, 21),
				new DateTime(2022, 2, 13),
				new DateTime(2022, 2, 1)
			};
			DateTime[] actual1k = br_oneday.DetermineBackupsToKeep(input1);

			Expect.Equal(expected1k, actual1k);
		}
		[TestMethod]
		public void TestDetermineBackupsToKeep_Adv1()
		{
			BackupRotate br_oneday = new BackupRotate(TimeSpan.FromDays(1));

			DateTime oldest = new DateTime(2000, 1, 1);
			List<DateTime> backupDates = new List<DateTime>();
			//StringBuilder sb = new StringBuilder();
			DateTime newest;
			for (int i = 0; i < 9999; i++)
			{
				newest = oldest.AddDays(i);
				backupDates.Add(newest);
				backupDates = new List<DateTime>(br_oneday.DetermineBackupsToKeep(backupDates));
				Assert.AreEqual(newest, backupDates[0], "Newest kept date was " + backupDates[0] + ", expected " + newest);
				Assert.AreEqual(oldest, backupDates[backupDates.Count - 1], "Oldest kept date was " + backupDates[backupDates.Count - 1] + ", expected " + oldest);
				if (i > 2)
				{
					// These conditions should be true for i >= 3
					Assert.IsTrue(backupDates.Count >= Math.Log(i, 2), "backupDates.Count was " + backupDates.Count + " at i=" + i + ". Expected >= " + Math.Log(i, 2));
					Assert.IsTrue(backupDates.Count <= (2 * Math.Log(i, 2)), "backupDates.Count was " + backupDates.Count + " at i=" + i + ". Expected <= " + (2 * Math.Log(i, 2)));
				}
				//sb.AppendLine(string.Join(", ", backupDates.Select(d => d.ToString("yyyy/MM/dd"))));
			}
			//string result = sb.ToString();
			//Console.WriteLine(result);
		}
		[TestMethod]
		public void TestDetermineBackupsToKeep_Adv2()
		{
			BackupRotate br_oneday = new BackupRotate(TimeSpan.FromDays(1));

			DateTime oldest = new DateTime(2000, 1, 1);
			List<DateTime> backupDates = new List<DateTime>();
			//StringBuilder sb = new StringBuilder();
			DateTime newest;
			int done = 0;
			Random rand = new Random();
			for (int i = 0; i < 9999; i++)
			{
				if (i < 3 || rand.NextDouble() < 0.5)
				{
					newest = oldest.AddDays(i);
					backupDates.Add(newest);
					backupDates = new List<DateTime>(br_oneday.DetermineBackupsToKeep(backupDates));
					Assert.AreEqual(newest, backupDates[0], "Newest kept date was " + backupDates[0] + ", expected " + newest);
					Assert.AreEqual(oldest, backupDates[backupDates.Count - 1], "Oldest kept date was " + backupDates[backupDates.Count - 1] + ", expected " + oldest);
					if (done > 2)
					{
						// These conditions should be true for done >= 3
						// We can't guarantee a minimum size beyond 2
						Assert.IsTrue(backupDates.Count >= 2, "backupDates.Count was " + backupDates.Count + " at done=" + done + ". Expected >= 2.");
						Assert.IsTrue(backupDates.Count <= (2 * Math.Log(done, 2)), "backupDates.Count was " + backupDates.Count + " at done=" + done + ". Expected <= " + (2 * Math.Log(done, 2)));
					}
					//sb.AppendLine(i + ": " + string.Join(", ", backupDates.Select(d => d.ToString("yyyy/MM/dd"))));
					done++;
				}
			}
			//string result = sb.ToString();
			//Console.WriteLine(result);
		}
	}
}
