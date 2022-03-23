using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// Contains logic to decide, given a set of backup dates, which dates to keep and which to delete.  The time between kept backups will increase exponentially.  Ideal operation requires consistent backups (e.g. no missed intervals), but the algorithm can also do a decent job of handling the situation where expected backups were not created.
	/// Behavior is similar to the log2rotate algorithm, but the implementation is simpler.
	/// </summary>
	public class BackupRotate
	{
		/// <summary>
		/// Defines the expected backup interval.  The algorithm works best when backup dates are exactly this far apart, but it is not a strict requirement.
		/// </summary>
		public TimeSpan BackupInterval = TimeSpan.FromDays(1);
		/// <summary>
		/// Constructs a BackupRotate instance with the default backup interval of 1 day.
		/// </summary>
		public BackupRotate() { }
		/// <summary>
		/// Constructs a BackupRotate instance with the specified backup interval.
		/// </summary>
		/// <param name="BackupInterval">Expected interval between backups. Default is 1 day.</param>
		public BackupRotate(TimeSpan BackupInterval)
		{
			this.BackupInterval = BackupInterval;
		}
		/// <summary>
		/// Returns an array of dates where the backups should be kept.
		/// </summary>
		/// <param name="allBackupsCurrentlyAvailable">A collection of dates with backups available.</param>
		/// <returns></returns>
		public DateTime[] DetermineBackupsToKeep(IEnumerable<DateTime> allBackupsCurrentlyAvailable)
		{
			List<DateTime> all = allBackupsCurrentlyAvailable.OrderByDescending(d => d).ToList();
			if (all.Count <= 2)
				return all.ToArray();

			TimeSpan interval = BackupInterval;

			List<DateTime> keepers = new List<DateTime>();

			DateTime DateRangeStart = all.First();
			DateTime DateRangeEnd = DateRangeStart.AddDays(1);

			DateTime oldest = default(DateTime);
			while (oldest != all.Last())
			{
				// Keep the oldest backup within the range.
				oldest = all.LastOrDefault(b => b >= DateRangeStart && b < DateRangeEnd);
				if (oldest != default(DateTime))
					keepers.Add(oldest);

				// Move the range backwards in time and double its size.
				DateRangeEnd = DateRangeStart;
				DateRangeStart = DateRangeStart.Add(interval.Negate());

				// Double the interval for next time.
				interval = interval.Add(interval);
			}

			return keepers.ToArray();
		}
		/// <summary>
		/// Returns an array of dates where the backups should be deleted.
		/// </summary>
		/// <param name="allBackupsCurrentlyAvailable">A collection of dates with backups available.</param>
		/// <returns></returns>
		public DateTime[] DetermineBackupsToDelete(IEnumerable<DateTime> allBackupsCurrentlyAvailable)
		{
			HashSet<DateTime> keepers = new HashSet<DateTime>(DetermineBackupsToKeep(allBackupsCurrentlyAvailable));
			return allBackupsCurrentlyAvailable
				.Where(b => !keepers.Contains(b))
				.OrderByDescending(d => d)
				.ToArray();
		}
	}
}
