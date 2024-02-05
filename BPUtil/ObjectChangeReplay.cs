using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// A class which scans two objects to determine what changed, then replays the changes on a third object.
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
