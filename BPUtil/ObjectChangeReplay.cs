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
		ObjectFieldMap diff;
		/// <summary>
		/// Constructs an ObjectChangeReplay that stores the changes between objects A and B.
		/// </summary>
		/// <param name="A">"Original" object.</param>
		/// <param name="B">"Modified" object, containing 0 or more changes that make it different from the "Original" object.</param>
		public ObjectChangeReplay(object A, object B)
		{
			ObjectFieldMap aMap = new ObjectFieldMap(A);
			ObjectFieldMap bMap = new ObjectFieldMap(B);
			diff = aMap ^ bMap;
		}
		/// <summary>
		/// Applies stored changes to object C.
		/// </summary>
		/// <param name="C">Object to apply changes on.</param>
		public void Apply(object C)
		{
			diff.Apply(C);
		}
	}
}
