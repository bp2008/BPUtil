using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// <para>A class which scans two objects to determine what changed, then replays the changes on a third object which may be different from the "Original" object.</para>
	/// <para>This behaves like a 3-way merge where any merge conflicts are automatically resolved by using your version of the data.</para>
	/// <para>WARNING: This can result in undesired behavior in certain cases, especially when arrays or lists are involved, as items may have been reordered such that your changes apply to the wrong items.</para>
	/// </summary>
	public class ObjectChangeReplay
	{
		object A;
		object B;
		/// <summary>
		/// Constructs an ObjectChangeReplay that stores the changes between objects A and B.
		/// </summary>
		/// <param name="A">"Original" object.</param>
		/// <param name="B">"Modified" object, containing 0 or more changes that make it different from the "Original" object.</param>
		public ObjectChangeReplay(object A, object B)
		{
			this.A = A;
			this.B = B;
		}
		/// <summary>
		/// Applies stored changes to object C.
		/// </summary>
		/// <param name="C">Object to apply changes on.</param>
		/// <returns>A copy of C with the stored changes applied.</returns>
		public T Apply<T>(T C)
		{
			ObjectMerge.MergeOptions opt = new ObjectMerge.MergeOptions();
			opt.ConflictResolution = ObjectMerge.ConflictResolution.TakeYours;
			return ObjectMerge.ThreeWayMerge((T)A, (T)B, C, opt);
		}
	}
}
