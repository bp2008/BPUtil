using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// <para>A class which helps you track the status of complex tasks that perform multiple concurrent operations.</para>
	/// <para>Example code:</para>
	/// <code>
	/// BranchStatus root = new BranchStatus("File Transfer");
	/// root.Status = "Sending file";
	/// 
	/// BranchStatus child1 = root.Branch("Connection 1");
	/// child1.Status = "650 Kbps";
	/// 
	/// BranchStatus child2 = root.Branch("Connection 2");
	/// child1.Status = "500 Kbps";
	/// 
	/// Console.WriteLine(root.ToString());
	/// // File Transfer: Sending file
	/// // * Connection 1: 650 Kbps
	/// // * Connection 2: 500 Kbps
	/// </code>
	/// </summary>
	public class BranchStatus : IDisposable
	{
		private List<BranchStatus> branches = new List<BranchStatus>();
		private ReaderWriterLockSlim branchLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
		private BranchStatus parent;
		private int depth;
		/// <summary>
		/// Event raised when the name or status string changed for this BranchStatus or its descendants.
		/// </summary>
		public event EventHandler OnChange = delegate { };
		/// <summary>
		/// Optional branch name.  If provided, the name will always be printed before the branch's status string when using <see cref="ToString"/>.
		/// </summary>
		public string Name
		{
			get => _name;
			set
			{
				string newValue = value ?? "";
				if (_name != newValue)
				{
					_name = value ?? "";
					OnChange(this, EventArgs.Empty);
				}
			}
		}
		private string _name = "";

		/// <summary>
		/// Gets or sets the status string for this branch. Null is automatically converted to empty string.
		/// </summary>
		public string Status
		{
			get => _status;
			set
			{
				string newValue = value ?? "";
				if (_status != newValue)
				{
					_status = value ?? "";
					OnChange(this, EventArgs.Empty);
				}
			}
		}
		private string _status = "";
		/// <summary>
		/// Constructs a new root BranchStatus instance which is not a child of any other BranchStatus.
		/// </summary>
		/// <param name="Name">Optional branch name.  If provided, the name will always be printed before the branch's status string when using <see cref="ToString"/>.</param>
		/// <param name="Status">Initial status string from this branch.</param>
		public BranchStatus(string Name = null, string Status = null)
		{
			this.Name = Name;
			depth = 0;
		}
		/// <summary>
		/// Creates a new BranchStatus as a child of another BranchStatus.
		/// </summary>
		/// <param name="Name">Optional branch name.  If provided, the name will always be printed before the branch's status string.</param>
		/// <param name="Status">Initial status string from this branch.</param>
		/// <param name="parent">The parent BranchStatus which owns this new BranchStatus.</param>
		private BranchStatus(string Name, string Status, BranchStatus parent)
		{
			this.parent = parent;
			this.depth = parent.depth + 1;
			this.Name = Name;
			this.Status = Status;
		}
		/// <summary>
		/// <para>Creates a new BranchStatus which tracks the status of a sequence of operations independently of other branches belonging to the BranchingStatus.</para>
		/// <para>You should dispose the BranchStatus when you want it to be removed from its parent.</para>
		/// </summary>
		/// <param name="Name">Optional branch name.  If provided, the name will always be printed before the branch's status string.</param>
		/// <param name="Status">Initial status string from this branch.</param>
		public BranchStatus Branch(string Name = null, string Status = null)
		{
			BranchStatus child = new BranchStatus(Name, Status, this);
			branchLock.EnterWriteLock();
			try
			{
				branches.Add(child);
			}
			finally
			{
				branchLock.ExitWriteLock();
			}
			child.OnChange += Child_OnChange;
			Child_OnChange(child, EventArgs.Empty);
			return child;
		}

		private void Child_OnChange(object sender, EventArgs e)
		{
			OnChange(this, EventArgs.Empty);
		}
		/// <summary>
		/// Prints the BranchStatus tree as a plain text string with line breaks.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			for (int i = depth; i > 1; i--)
				sb.Append("  ");
			if (depth > 0)
				sb.Append("* ");
			if (!string.IsNullOrWhiteSpace(Name))
				sb.Append(Name).Append(": ");
			sb.AppendLine(Status);

			branchLock.EnterReadLock();
			try
			{
				foreach (BranchStatus child in branches)
					sb.Append(child.ToString());
			}
			finally
			{
				branchLock.ExitReadLock();
			}

			return sb.ToString();
		}
		private void RemoveChild(BranchStatus child)
		{
			child.OnChange -= Child_OnChange;
			branchLock.EnterWriteLock();
			try
			{
				branches.Remove(child);
			}
			finally
			{
				branchLock.ExitWriteLock();
			}
		}
		#region IDisposable

		private bool disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// Remove self from the parent's branches.
					parent?.RemoveChild(this);
					branchLock.EnterWriteLock();
					try
					{
						for (int i = 0; i < branches.Count; i++)
							branches[branches.Count - 1].Dispose(); // Just dispose the last item in the list each time, and ignore the [i] variable.
					}
					finally
					{
						branchLock.ExitWriteLock();
					}
				}

				// free unmanaged resources (unmanaged objects) and override finalizer
				// set large fields to null
				disposedValue = true;
			}
		}

		// override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~BranchStatus()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }

		/// <summary>
		/// Disposes this BranchStatus, removing it from its parent's branch list.
		/// </summary>
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}
