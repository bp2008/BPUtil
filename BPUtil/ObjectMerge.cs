using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// A class which performs a 3-way merge as is frequently done in source control systems, except this is done on .NET objects instead of on text files.
	/// </summary>
	public static class ObjectMerge

	{
		/// <summary>
		/// Merges your changes to an object with someone else's changes to the same object.  If the changes can't be automatically merged due to a conflict (e.g. both parties modified the same field differently), this method throws an ObjectMergeException containing details about the conflicts.
		/// </summary>
		/// <param name="baseObject">Base object, upon which two different parties made changes.</param>
		/// <param name="yourObject">Object with your changes.</param>
		/// <param name="theirObject">Object with the other party's changes.  It would be a common use case if [theirs] does not actually have any changes compared to [baseObject].</param>
		/// <returns>Returns the merged object.</returns>
		public static T ThreeWayMerge<T>(T baseObject, T yourObject, T theirObject)
		{
			if (baseObject == null)
				throw new ArgumentNullException(nameof(baseObject));
			if (yourObject == null)
				throw new ArgumentNullException(nameof(yourObject));
			if (theirObject == null)
				throw new ArgumentNullException(nameof(theirObject));

			ObjectFieldMap baseVersion = new ObjectFieldMap(baseObject);
			ObjectFieldMap yourVersion = new ObjectFieldMap(yourObject);
			ObjectFieldMap theirVersion = new ObjectFieldMap(theirObject);

			ObjectFieldMap yourDiff = baseVersion ^ yourVersion;
			ObjectFieldMap theirDiff = baseVersion ^ theirVersion;
			ObjectMergeConflict[] conflicts = GetConflicts(yourDiff, theirDiff);
			if (conflicts.Length > 0)
				throw new ObjectMergeException(conflicts);

			T mergeObject = theirObject.Copy();
			yourDiff.Apply(mergeObject);
			return mergeObject;
		}
		private static ObjectMergeConflict[] GetConflicts(ObjectFieldMap yourDiff, ObjectFieldMap theirDiff)
		{
			List<ObjectMergeConflict> conflicts = new List<ObjectMergeConflict>();

			string[] yourPaths = yourDiff.Unordered().Select(f => f.Path).ToArray();
			string[] theirPaths = theirDiff.Unordered().Select(f => f.Path).ToArray();

			Dictionary<string, object> yourMap = yourDiff.ToDictionary();
			Dictionary<string, object> theirMap = yourDiff.ToDictionary();

			foreach (string yourPath in yourPaths)
			{
				object yourValue = yourMap[yourPath];
				object theirValue;
				if (theirMap.TryGetValue(yourPath, out theirValue))
				{
					if (!Object.Equals(yourValue, theirValue))
					{
						// Conflicting change detected to the same path
						conflicts.Add(new ObjectMergeConflict(yourPath, yourValue, yourPath, theirValue));
					}
				}
				else
				{
					// No exact match.  It is possible the other changeset changed a descendant or ancestor.
					foreach (string theirPath in theirPaths)
					{
						if ((theirPath.Length > yourPath.Length && theirPath.StartsWith(yourPath))
							|| (theirPath.Length < yourPath.Length && yourPath.StartsWith(theirPath)))
						{
							// Changes were made to different nodes IN THE SAME BRANCH, which is a different kind of conflict.
							theirValue = theirMap[theirPath];
							conflicts.Add(new ObjectMergeConflict(yourPath, yourValue, theirPath, theirValue));
						}
					}
				}
			}
			return conflicts.ToArray();
		}
	}
	/// <summary>
	/// Represents a merge conflict from <see cref="ObjectMerge"/>.
	/// </summary>
	public class ObjectMergeConflict
	{
		public ObjectFieldMap.FieldData yours;
		public ObjectFieldMap.FieldData theirs;
		public ObjectMergeConflict() { }
		public ObjectMergeConflict(string yourPath, object yourValue, string theirPath, object theirValue)
		{
			yours = new ObjectFieldMap.FieldData(yourPath, yourValue);
			theirs = new ObjectFieldMap.FieldData(theirPath, theirValue);
		}

		/// <summary>
		/// Returns a string including the conflicting paths and values.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return "Yours:  " + yours.ToString() + Environment.NewLine
				+ "Theirs: " + theirs.ToString();
		}
	}

	/// <summary>
	/// Exception thrown by <see cref="ObjectMerge.ThreeWayMerge"/> when a conflict is found that cannot be resolved automatically.
	/// </summary>
	[Serializable]
	public class ObjectMergeException : Exception
	{
		public ObjectMergeConflict[] Conflicts;
		public ObjectMergeException(ObjectMergeConflict[] conflicts) : base(GenerateMessage(conflicts))
		{
			Conflicts = conflicts;
		}

		private static string GenerateMessage(ObjectMergeConflict[] conflicts)
		{
			return "--Merge Conflicts--" + Environment.NewLine + Environment.NewLine
				+ string.Join(Environment.NewLine + Environment.NewLine, (object[])conflicts);
		}
	}
}

