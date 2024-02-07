using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
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
		/// <para>Converts an object to a JSON string. Must be set by consumers of ObjectMerge prior to use.</para>
		/// <para>e.g. ObjectMerge.SerializeObject = JsonConvert.SerializeObject; ObjectMerge.DeserializeObject = JsonConvert.DeserializeObject;</para>
		/// </summary>
		public static Func<object, string> SerializeObject = obj => throw new Exception("ObjectMerge.SerializeObject must be assigned prior to using ObjectMerge.");
		/// <summary>
		/// <para>Converts a JSON string to an object. Must be set by consumers of ObjectMerge prior to use.</para>
		/// <para>e.g. ObjectMerge.SerializeObject = JsonConvert.SerializeObject; ObjectMerge.DeserializeObject = JsonConvert.DeserializeObject;</para>
		/// </summary>
		public static Func<string, Type, object> DeserializeObject = (str, type) => throw new Exception("ObjectMerge.DeserializeObject must be assigned prior to using ObjectMerge.");

		public static T ThreeWayMerge<T>(T baseObject, T yourObject, T theirObject, MergeOptions opt)
		{
			//if (baseObject == null)
			//	throw new ArgumentNullException(nameof(baseObject));
			//if (yourObject == null)
			//	throw new ArgumentNullException(nameof(yourObject));
			//if (theirObject == null)
			//	throw new ArgumentNullException(nameof(theirObject));

			if (opt == null)
				opt = new MergeOptions();

			T mergeResult = Merge(baseObject, yourObject, theirObject, null, opt);

			if (opt.MergeConflicts.Count > 0 && opt.ConflictResolution == ConflictResolution.Throw)
				throw new ObjectMergeException(opt.MergeConflicts.ToArray());

			return mergeResult;
		}
		public enum ConflictResolution
		{
			Throw,
			TakeBase,
			TakeYours,
			TakeTheirs
		}
		public class MergeOptions
		{
			public ConflictResolution ConflictResolution = ConflictResolution.Throw;
			public List<ObjectMergeConflict> MergeConflicts = new List<ObjectMergeConflict>();
			/// <summary>
			/// Stores visited objects for the purpose of loop detection and handling.
			/// </summary>
			internal Dictionary<object, object> Visited = new Dictionary<object, object>();
		}
		/// <summary>
		/// Internal Merge method which is called recursively.
		/// </summary>
		/// <typeparam name="T">Type of object</typeparam>
		/// <param name="baseObject">Object which you started from before making changes.</param>
		/// <param name="yourObject">Object produced by "you".</param>
		/// <param name="theirObject">Object produced by a third-party.</param>
		/// <param name="path">Path string for use in merge conflict reports. Starts as null for the root object.</param>
		/// <param name="opt">Options object</param>
		/// <returns></returns>
		/// <exception cref="Exception">If an unknown ConflictResolution is used.</exception>
		private static T Merge<T>(T baseObject, T yourObject, T theirObject, string path, MergeOptions opt)
		{
			// Care must be taken throughout this method to never return one of the input objects directly unless it is a string or primitive value type or null.
			// Otherwise we can end up with the merge result sharing references to mutable objects, and that is not typically desired in a situation where you want to merge 3 objects.

			if (baseObject == null && yourObject == null && theirObject == null)
				return default(T);

			Type baseType = baseObject?.GetType();
			Type yourType = yourObject?.GetType();
			Type theirType = theirObject?.GetType();
			Type type = baseType ?? yourType ?? theirType ?? typeof(T);
			if ((baseType != null && baseType != type)
				|| (yourType != null && yourType != type)
				|| (theirType != null && theirType != type))
				throw new ArgumentException("Type mismatch between merge arguments: [" + baseType + ", " + yourType + ", " + theirType + "]");

			Type nullableUnderlyingType = Nullable.GetUnderlyingType(type);
			if (nullableUnderlyingType != null)
				type = nullableUnderlyingType; // [type] was a nullable type, but now is the underlying type.

			// Now we branch.  Each object type can have different merging logic.
			if (type.IsValueType && type.IsPrimitive || type == typeof(string))
			{
				// Simple string or primitive value type.  The merge result will be one of them.

				if (Object.Equals(yourObject, theirObject))
					return yourObject; // Both "new" versions are the same, so return either of them.
				else if (Object.Equals(baseObject, yourObject))
					return theirObject; // "You" didn't make a change, so return "their" version.
				else if (Object.Equals(baseObject, theirObject))
					return yourObject; // "They" didn't make a change, so return "your" version.

				// Both "new" versions are different, so this is a merge conflict.
				opt.MergeConflicts.Add(new ObjectMergeConflict(path, baseObject, yourObject, theirObject));
				if (opt.ConflictResolution == ConflictResolution.Throw)
					return default(T);
				else if (opt.ConflictResolution == ConflictResolution.TakeBase)
					return baseObject;
				else if (opt.ConflictResolution == ConflictResolution.TakeYours)
					return yourObject;
				else if (opt.ConflictResolution == ConflictResolution.TakeTheirs)
					return theirObject;
				else
					throw new Exception("Unknown ConflictResolution option: " + opt.ConflictResolution);
			}
			else if (typeof(IEnumerable).IsAssignableFrom(type)
				|| (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
				|| typeof(IDictionary).IsAssignableFrom(type)
				|| (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>)))
			{
				// Multi-element collection
				// Serialize the entire collection into a string for the purpose of change detection on the object as a whole.
				string b = SerializeForCompare(baseObject);
				string y = SerializeForCompare(yourObject);
				string t = SerializeForCompare(theirObject);
				string mergeResult = Merge(b, y, t, path, opt);
				return (T)DeserializeForCompare(mergeResult, type);

				// An earlier version of ObjectMerge attempted to support per-element change detection and merging, but this class simply does not have the requisite knowledge to know which changes are safe and which are not.  The only safe thing is to consider any change in the collection as being incompatible with any other change in the collection.
			}
			else
			{
				// This branch intends to iterate over public instance fields and properties, merging each of them individually.

				// NULLS REQUIRE SPECIAL CASES:
				{
					// Check special cases where 2 of 3 objects are null
					if (yourObject == null && theirObject == null)
						return yourObject.Copy(); // Both "new" versions are the same, so return either of them.
					else if (baseObject == null && yourObject == null)
						return theirObject.Copy(); // "You" didn't make a change, so return "their" version.
					else if (baseObject == null && theirObject == null)
						return yourObject.Copy(); // "They" didn't make a change, so return "your" version.

					// Check special cases where 1 of 3 objects is null
					if (baseObject == null)
					{
						// Base object is null.  That means "you" and "they" both created new values which can be treated as a whole for conflict detection.
						// We could detect conflicts at a more granular level, but right now that isn't worth the programming and testing effort since it would only serve to make conflict reports more precise.
						string yourJson = SerializeForCompare(yourObject);
						string theirJson = SerializeForCompare(theirObject);
						if (yourJson != theirJson)
						{
							// "you" and "they" created different values
							opt.MergeConflicts.Add(new ObjectMergeConflict(path, baseObject, yourObject, theirObject));
							if (opt.ConflictResolution == ConflictResolution.Throw)
								return default(T);
							else if (opt.ConflictResolution == ConflictResolution.TakeBase)
								return default(T);
							else if (opt.ConflictResolution == ConflictResolution.TakeYours)
								return (T)DeserializeObject(yourJson, type);
							else if (opt.ConflictResolution == ConflictResolution.TakeTheirs)
								return (T)DeserializeObject(theirJson, type);
							else
								throw new Exception("Unknown ConflictResolution option: " + opt.ConflictResolution);
						}
						else
							return (T)DeserializeObject(yourJson, type); // No conflict.  Just return a copy of your version.
					}
					else if (yourObject == null)
					{
						string baseJson = SerializeForCompare(baseObject);
						string theirJson = SerializeForCompare(theirObject);
						if (baseJson != theirJson)
						{
							// "you" deleted the object, but "they" only changed it.
							opt.MergeConflicts.Add(new ObjectMergeConflict(path, baseObject, yourObject, theirObject));
							if (opt.ConflictResolution == ConflictResolution.Throw)
								return default(T);
							else if (opt.ConflictResolution == ConflictResolution.TakeBase)
								return baseObject.Copy();
							else if (opt.ConflictResolution == ConflictResolution.TakeYours)
								return default(T);
							else if (opt.ConflictResolution == ConflictResolution.TakeTheirs)
								return theirObject.Copy();
							else
								throw new Exception("Unknown ConflictResolution option: " + opt.ConflictResolution);
						}
						else
							return default(T); // "you" deleted, "they" didn't change.
					}
					else if (theirObject == null)
					{
						string baseJson = SerializeForCompare(baseObject);
						string yourJson = SerializeForCompare(yourObject);
						if (baseJson != yourJson)
						{
							// "they" deleted the object, but "you" only changed it.
							opt.MergeConflicts.Add(new ObjectMergeConflict(path, baseObject, yourObject, theirObject));
							if (opt.ConflictResolution == ConflictResolution.Throw)
								return default(T);
							else if (opt.ConflictResolution == ConflictResolution.TakeBase)
								return baseObject.Copy();
							else if (opt.ConflictResolution == ConflictResolution.TakeYours)
								return yourObject.Copy();
							else if (opt.ConflictResolution == ConflictResolution.TakeTheirs)
								return default(T);
							else
								throw new Exception("Unknown ConflictResolution option: " + opt.ConflictResolution);
						}
						else
							return default(T); // "they" deleted, "you" didn't change.
					}
				}

				// None of the objects is null.  We can iterate over fields and properties and recursively merge them.
				if (opt.Visited.TryGetValue(baseObject, out object previousResult))
					return (T)previousResult; // Loop detected

				T merged = (T)Activator.CreateInstance(type);
				opt.Visited[baseObject] = merged;
				foreach (MemberInfo memberInfo in type.GetMembers(BindingFlags.Public | BindingFlags.Instance))
				{
					if (memberInfo is FieldInfo || memberInfo is PropertyInfo)
					{
						object b = GetValueOrDefault(baseObject, memberInfo);
						object y = GetValueOrDefault(yourObject, memberInfo);
						object t = GetValueOrDefault(theirObject, memberInfo);
						string pathAdd = (path == null ? "" : ".") + memberInfo.Name;
						object mergeResult = Merge(b, y, t, path + pathAdd, opt);
						SetMemberValue(merged, memberInfo, mergeResult);
					}
				}
				return merged;
			}
		}
		/// <summary>
		/// Returns the type of the first non-null object passed in to this method. If all of the objects are null, returns the generic type's type, which may be just "Object".
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="objects">Array of objects which may be null.</param>
		/// <returns></returns>
		private static Type GetTypeOf<T>(params T[] objects)
		{
			for (int i = 0; i < objects.Length; i++)
				if (objects[i] != null)
					return objects[i].GetType();
			return typeof(T);
		}
		#region Helpers
		private static string SerializeForCompare(object o)
		{
			if (o == null)
				return null;
			return SerializeObject(o);
		}
		private static object DeserializeForCompare(string json, Type t)
		{
			if (json == null)
				return null;
			return DeserializeObject(json, t);
		}
		private static object GetValueOrDefault(object parent, MemberInfo memberInfo)
		{
			if (memberInfo is FieldInfo fi)
			{
				if (parent == null)
					return Activator.CreateInstance(fi.FieldType);
				return fi.GetValue(parent);
			}
			else if (memberInfo is PropertyInfo pi)
			{
				if (parent == null)
					return Activator.CreateInstance(pi.PropertyType);
				return pi.GetValue(parent);
			}
			return null;
		}
		/// <summary>
		/// Sets the value of the field or property.
		/// </summary>
		/// <param name="parent">Object which has the field or property.</param>
		/// <param name="memberInfo">FieldInfo or PropertyInfo</param>
		/// <param name="value">Value to set.</param>
		private static void SetMemberValue(object parent, MemberInfo memberInfo, object value)
		{
			if (memberInfo is FieldInfo fi)
				fi.SetValue(parent, value);
			else if (memberInfo is PropertyInfo pi)
				pi.SetValue(parent, value);
			else
				throw new Exception("Unexpected MemberInfo type: " + memberInfo.GetType().FullName);
		}
		#endregion
	}
	/// <summary>
	/// Represents a merge conflict from <see cref="ObjectMerge"/>.
	/// </summary>
	public class ObjectMergeConflict
	{
		public string path;
		public object baseValue;
		public object yourValue;
		public object theirValue;
		public ObjectMergeConflict() { }
		public ObjectMergeConflict(string path, object baseValue, object yourValue, object theirValue)
		{
			this.path = path;
			this.baseValue = baseValue;
			this.yourValue = yourValue;
			this.theirValue = theirValue;
		}

		/// <summary>
		/// Returns a string including the conflicting paths and values.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return path + " = " + Serialize(baseValue) + Environment.NewLine
				+ " * Yours:  " + Serialize(yourValue) + Environment.NewLine
				+ " * Theirs: " + Serialize(theirValue);
		}
		private string Serialize(object Value)
		{
			if (Value == null)
				return "null";
			else if (Value is string)
				return "\"" + Value + "\"";
			else if (Value is char)
				return "'" + Value + "'";
			else
				return Value.ToString();
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

