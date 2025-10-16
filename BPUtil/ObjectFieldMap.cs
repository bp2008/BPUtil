using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace BPUtil
{
	/// <summary>
	/// <para>This class scans all public instance-level fields and properties and stores them in a map.</para>
	/// </summary>
	public class ObjectFieldMap
	{
		public class FieldData
		{
			/// <summary>
			/// Object path string. Examples: "Name", "Child.Name", "Children"
			/// </summary>
			public string Path;
			/// <summary>
			/// Value to set at the location defined by the Path.
			/// </summary>
			public object Value;
			public FieldData(string path, object value) { Path = path; Value = value; }
			public override string ToString()
			{
				if (Value == null)
					return Path + " = null";
				else if (Value is string)
					return Path + " = \"" + Value + "\"";
				else if (Value is char)
					return Path + " = '" + Value + "'";
				else
					return Path + " = " + Value;
			}
			/// <summary>
			/// Returns true if the paths are the same or if one of the paths refers to an ancestor of the other path.
			/// </summary>
			/// <param name="pathA">FieldData path string.</param>
			/// <param name="pathB">FieldData path string.</param>
			/// <returns></returns>
			public static bool PathsAreSameBranch(string pathA, string pathB)
			{
				if (pathA == pathB)
					return true;
				if (pathA.Length > pathB.Length && pathA.StartsWith(pathB))
				{
					char nextChar = pathA[pathB.Length];
					return nextChar == '.' || nextChar == '[';
				}
				if (pathB.Length > pathA.Length && pathB.StartsWith(pathA))
				{
					char nextChar = pathB[pathA.Length];
					return nextChar == '.' || nextChar == '[';
				}
				return false;
			}
		}
		/// <summary>
		/// <para>The keys are strings which identify the field or property or IList index which contains the value.  Examples: "Name", "Child.Name", "Children[0].Name".</para>
		/// <para>All values are strings or primitive value types or null.</para>
		/// </summary>
		private List<FieldData> fieldDataList;
		/// <summary>
		/// No-params constructor is intentionally private.
		/// </summary>
		private ObjectFieldMap()
		{
			fieldDataList = new List<FieldData>();
		}
		/// <summary>
		/// Constructs an ObjectFieldMap from the given object.
		/// </summary>
		/// <param name="obj">The object which should be scanned.</param>
		public ObjectFieldMap(object obj)
		{
			if (obj == null)
				throw new ArgumentNullException(nameof(obj));

			Type objType = obj.GetType();
			if ((objType.IsValueType && objType.IsPrimitive) || objType == typeof(string))
				throw new ArgumentException("ObjectFieldMap can't be used directly on strings or primitive value types. Unsupported type: " + objType.FullName, nameof(obj));

			fieldDataList = new List<FieldData>();
			Scan(obj, null, new Stack<object>());
		}
		/// <summary>
		/// Scans the given object, putting its field and descendant fields into the map.
		/// </summary>
		/// <param name="obj">Object to scan (recursive)</param>
		/// <param name="path">Path of parent objects</param>
		/// <param name="ancestors">Stack of ancestor objects for loop detection.</param>
		private void Scan(object obj, string path, Stack<object> ancestors)
		{
			Type objType = obj?.GetType();
			if (obj == null || (objType.IsValueType && objType.IsPrimitive) || objType == typeof(string))
				fieldDataList.Add(new FieldData(path, obj));
			else
			{
				if (ancestors.Any(a => Object.ReferenceEquals(obj, a)))
					return; // Loop detected. If we process this descendant, we will never finish.
				ancestors.Push(obj);
				try
				{
					if (obj is IList list)
					{
						for (int i = 0; i < list.Count; i++)
						{
							Scan(list[i], StringUtil.DeNullify(path) + "[" + i + "]", ancestors);
						}
					}
					else
					{
						// Scan each field
						foreach (MemberInfo memberInfo in objType.GetMembers(BindingFlags.Public | BindingFlags.Instance))
						{
							if (memberInfo is FieldInfo fi)
								Scan(fi.GetValue(obj), ConcatPath(path, fi.Name), ancestors);
							else if (memberInfo is PropertyInfo pi)
								Scan(pi.GetValue(obj), ConcatPath(path, pi.Name), ancestors);
						}
					}
				}
				finally
				{
					ancestors.Pop();
				}
			}
		}
		private string ConcatPath(string pathRoot, string pathMore)
		{
			if (pathRoot != null)
				return pathRoot + "." + pathMore;
			return pathMore;
		}
		/// <summary>
		/// Returns a string describing the hierarchy of changes which are stored in this object, in the order of their declaration.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			foreach (FieldData item in fieldDataList)
			{
				if (sb.Length > 0)
					sb.AppendLine(", ");
				sb.Append(item.ToString());
			}
			return sb.ToString();
		}
		/// <summary>
		/// Returns a Dictionary mapping Path to Value, built from <see cref="fieldDataList"/>.
		/// </summary>
		/// <returns></returns>
		internal Dictionary<string, object> ToDictionary()
		{
			return fieldDataList.ToDictionary(f => f.Path, f => f.Value);
		}
		/// <summary>
		///  Gets <see cref="fieldDataList"/> as an array.
		/// </summary>
		/// <returns></returns>
		internal FieldData[] Unordered()
		{
			return fieldDataList.ToArray();
		}
		/// <summary>
		/// Gets <see cref="fieldDataList"/> as an array, sorted by Path in the specified order.
		/// </summary>
		/// <param name="descending">If true, it will be ordered descending (e.g. ["Z", "M", "A"]).</param>
		internal FieldData[] Ordered(bool descending = false)
		{
			FieldData[] arr = fieldDataList.ToArray();
			if (descending)
				Array.Sort(arr, (a, b) => -1 * StringSorting.CompareStringsContainingIntegers(a.Path, b.Path));
			else
				Array.Sort(arr, (a, b) => StringSorting.CompareStringsContainingIntegers(a.Path, b.Path));
			return arr;
		}
		#region Diff - Find changes between two objects
		/// <summary>
		/// Implements the XOR operator which returns a new ObjectFieldMap that contains only values from the [right] object that are different from those in the [left] object.
		/// </summary>
		/// <param name="left">Left side object.</param>
		/// <param name="right">Right side object.</param>
		/// <returns></returns>
		public static ObjectFieldMap operator ^(ObjectFieldMap left, ObjectFieldMap right)
		{
			return left.Diff(right);
		}
		/// <summary>
		/// Returns a new ObjectFieldMap that contains only values from the [other] object that are different from those in [this] object.  Same as using the XOR operator ([this] ^ [other]).
		/// </summary>
		/// <param name="other">Right side object to use with the XOR operator.</param>
		/// <returns></returns>
		public ObjectFieldMap Diff(ObjectFieldMap other)
		{
			string[] thisPaths = this.fieldDataList.Select(f => f.Path).ToArray();
			string[] otherPaths = other.fieldDataList.Select(f => f.Path).ToArray();
			HashSet<string> includedPaths = new HashSet<string>();

			Dictionary<string, object> thisMap = this.ToDictionary();
			Dictionary<string, object> otherMap = other.ToDictionary();
			ObjectFieldMap diff = new ObjectFieldMap();
			foreach (string path in thisPaths)
			{
				if (!includedPaths.Contains(path))
				{
					includedPaths.Add(path);
					object originalValue = thisMap[path];
					bool hasModifiedValue = otherMap.TryGetValue(path, out object modifiedValue);
					if (!hasModifiedValue)
					{
						// The [other] object does not have this exact path defined.
						// If it doesn't define another path in this branch, then we'll mark this path as being set to null.
						if (!otherPaths.Any(p => FieldData.PathsAreSameBranch(p, path)))
							diff.fieldDataList.Add(new FieldData(path, null));
					}
					else if (!Object.Equals(originalValue, modifiedValue))
						diff.fieldDataList.Add(new FieldData(path, modifiedValue));
				}
			}
			foreach (string path in otherPaths)
			{
				if (!includedPaths.Contains(path))
				{
					includedPaths.Add(path);
					bool hasOriginalValue = thisMap.TryGetValue(path, out object originalValue);
					object modifiedValue = otherMap[path];
					if (!hasOriginalValue || !Object.Equals(originalValue, modifiedValue))
						diff.fieldDataList.Add(new FieldData(path, modifiedValue));
				}
			}
			return diff;
		}
		#endregion
		#region Apply Changes to another object
		/// <summary>
		/// <para>WARNING: This method does not handle all IList operations correctly, such as setting the correct length of the IList or removing elements from an IList.</para>
		/// <para>It is recommended to use <see cref="ObjectMerge"/> instead of ObjectFieldMap if your goal is to 3-way merge objects.</para>
		/// <para>Applies the values stored in this ObjectFieldMap to the given object.  If any values are not compatible with the structure of the given object, an exception is thrown.</para>
		/// </summary>
		/// <param name="obj">Object to apply values to.</param>
		public void Apply(object obj)
		{
			if (obj == null)
				throw new ArgumentNullException(nameof(obj));

			Type objType = obj.GetType();
			if ((objType.IsValueType && objType.IsPrimitive) || objType == typeof(string))
				throw new ArgumentException("ObjectFieldMap can't be used directly on strings or primitive value types. Unsupported type: " + objType.FullName, nameof(obj));

			//// Order the FieldData so that the largest IList indexes will be encountered first, thereby ensuring that if an IList needs to be created, it will have sufficient capacity to contain all elements defined by the map.
			//FieldData[] ordered = Unordered(true);
			foreach (FieldData fd in fieldDataList)
			{
				Apply(obj, fd.Path, fd.Value);
			}
		}
		/// <summary>
		/// Sets the value of the given field or property or IList element.
		/// </summary>
		/// <param name="root">Object to modify.</param>
		/// <param name="path">Property or field name(s) and IList indices as they would be written in C# code.  Examples: "Name", "Child.Name", "Children[0].Name", "A.B[0].C.D[5]".</param>
		/// <param name="value">Value to set.</param>
		/// <exception cref="Exception">Throws if "key" is invalid, or if the key defines a location that can't be found or constructed using default constructors available for relevant types.</exception>
		private static void Apply(object root, string path, object value)
		{
			if (root == null)
				throw new ArgumentNullException(nameof(root));
			if (path == null)
				throw new ArgumentNullException(nameof(path));
			if (string.IsNullOrWhiteSpace(path))
				throw new ArgumentException("Field path is not valid.", nameof(path));

			// Traverse the object and its descendants to reach the location specified by [path].
			object obj = root;
			object parent = null;
			MemberInfo myMemberInfo = null;
			string[] pathSegments = path.Split('.');
			while (pathSegments.Length > 0)
			{
				// Parse the first path segment
				// This parser assumes the path is syntactically valid, and since the map containing the paths is private, it should be our fault if the path is not valid.
				string[] parts = pathSegments[0].Split(new char[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);

				string memberName; // One of these may be null, but not both.
				int? index; // One of these may be null, but not both.
				if (int.TryParse(parts[0], out int parsedIndex))
				{
					// This path segment starts with an IList index
					memberName = null;
					index = parsedIndex;
					pathSegments[0] = pathSegments[0].Substring(2 + index.ToString().Length);
				}
				else
				{
					// This path segment starts with a member name
					memberName = parts[0];
					pathSegments[0] = pathSegments[0].Substring(memberName.Length);
					if (parts.Length > 1)
					{
						// ... followed by an IList index
						index = int.Parse(parts[1]);
					}
					else
						index = null;
				}
				if (pathSegments[0] == "")
					pathSegments = pathSegments.Skip(1).ToArray();

				// Path segment parsing is complete

				// Have we reached the end of the path?
				if (pathSegments.Length == 0)
				{
					// We've reached the end of the path, the location defined by [path], so set the value.
					if (index == null)
						SetMemberValue(obj, memberName, value);
					else if (obj is IList list)
					{
						if (list.Count < index.Value + 1)
						{
							if (list.IsFixedSize)
							{
								if (parent == null)
									throw new Exception("ObjectFieldMap can't construct a new " + list.GetType() + " with longer length for you because it is the root object being modified.");
								IList longerList = LengthenFixedSizeList(list, index.Value + 1);
								SetMemberValue(parent, myMemberInfo, longerList);
								obj = list = longerList;
							}
							else
							{
								object defValue = GetDefaultValue(value);
								while (list.Count < index.Value + 1)
									list.Add(defValue);
							}
						}
						list[index.Value] = value;
					}
					else
						throw new Exception("Unable to set value at index " + index.Value + " because type " + obj.GetType().FullName + " does not inherit from IList.");
				}
				else
				{
					// We need to move one level deeper
					MemberInfo childMemberInfo = myMemberInfo = GetMemberInfo(obj, memberName);
					object referencedChild = GetMemberValue(obj, childMemberInfo);
					if (referencedChild == null)
					{
						// Part of the path we're trying to find does not exist yet.  Create it.  An exception will throw if an appropriate constructor does not exist.
						Type childType = GetMemberType(childMemberInfo);
						int? size = index + 1; // if index is null, size is also null
						referencedChild = ConstructInstanceOfType(childType, size);
						SetMemberValue(obj, childMemberInfo, referencedChild);
					}
					parent = obj;
					obj = referencedChild;
				}
			}
		}
		#endregion
		#region Helpers
		/// <summary>
		/// Gets the FieldInfo or PropertyInfo with the given name.  The field or property must be public and instance-level (non-static).
		/// </summary>
		/// <param name="obj">Object which has the field or property.</param>
		/// <param name="memberName">Field or property name.</param>
		/// <returns></returns>
		/// <exception cref="Exception">Throws if the requested member does not exist or is ambiguous.</exception>
		private static MemberInfo GetMemberInfo(object obj, string memberName)
		{
			MemberInfo mi = obj.GetType().GetMember(memberName, MemberTypes.Field | MemberTypes.Property, BindingFlags.Public | BindingFlags.Instance).Single();
			if (mi is FieldInfo fi)
				return fi;
			else if (mi is PropertyInfo pi)
				return pi;
			else
				throw new Exception("Unexpected MemberInfo type: " + mi.GetType().FullName);
		}
		/// <summary>
		/// Gets the value of the field or property.
		/// </summary>
		/// <param name="parent">Object which has the field or property.</param>
		/// <param name="memberInfo">FieldInfo or PropertyInfo</param>
		/// <returns></returns>
		private static object GetMemberValue(object parent, MemberInfo memberInfo)
		{
			if (memberInfo is FieldInfo fi)
				return fi.GetValue(parent);
			else if (memberInfo is PropertyInfo pi)
				return pi.GetValue(parent);
			else
				throw new Exception("Unexpected MemberInfo type: " + memberInfo.GetType().FullName);
		}
		/// <summary>
		/// Gets the value of the field or property with the given name.  The field or property must be public and instance-level (non-static).
		/// </summary>
		/// <param name="parent">Object which has the field or property.</param>
		/// <param name="memberName">Field or property name.</param>
		/// <returns></returns>
		/// <exception cref="Exception">Throws if the requested member does not exist or is ambiguous.</exception>
		private static object GetMemberValue(object parent, string memberName)
		{
			return GetMemberValue(parent, GetMemberInfo(parent, memberName));
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
		/// <summary>
		/// Sets the value of the field or property.  The field or property must be public and instance-level (non-static).
		/// </summary>
		/// <param name="parent">Object which has the field or property.</param>
		/// <param name="memberName">Field or property name.</param>
		/// <param name="value">Value to set.</param>
		/// <exception cref="Exception">Throws if the requested member does not exist or is ambiguous.</exception>
		private static void SetMemberValue(object parent, string memberName, object value)
		{
			SetMemberValue(parent, GetMemberInfo(parent, memberName), value);
		}
		/// <summary>
		/// Gets the type of the field or property.
		/// </summary>
		/// <param name="memberInfo">FieldInfo or PropertyInfo</param>
		/// <returns></returns>
		private static Type GetMemberType(MemberInfo memberInfo)
		{
			if (memberInfo is FieldInfo fi)
				return fi.FieldType;
			else if (memberInfo is PropertyInfo pi)
				return pi.PropertyType;
			else
				throw new Exception("Unexpected MemberInfo type: " + memberInfo.GetType().FullName);
		}
		/// <summary>
		/// Gets the type of the field or property with the given name.  The field or property must be public and instance-level (non-static).
		/// </summary>
		/// <param name="parent">Object which has the field or property.</param>
		/// <param name="memberName">Field or property name.</param>
		/// <returns></returns>
		/// <exception cref="Exception">Throws if the requested member does not exist or is ambiguous.</exception>
		private static Type GetMemberType(object parent, string memberName)
		{
			return GetMemberType(GetMemberInfo(parent, memberName));
		}
		/// <summary>
		/// Constructs an instance of the given Member type using the default constructor.  May throw an exception if a default constructor is unavailable.
		/// </summary>
		/// <param name="t">Type to construct</param>
		/// <param name="size">Optional size, if [t] inherits from IList.</param>
		/// <returns></returns>
		private static object ConstructInstanceOfType(Type t, int? size)
		{
			if (typeof(IList).IsAssignableFrom(t))
			{
				if (t.IsInterface)
					t = typeof(ArrayList);
				ConstructorInfo constructor = t.GetConstructor(new[] { typeof(int) });
				if (constructor == null)
					throw new ArgumentException("IList type must have a constructor that takes a single int parameter", nameof(t));
				return (IList)constructor.Invoke(new object[] { size.Value });
			}
			else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>))
			{
				if (t.IsInterface)
					t = typeof(List<>).MakeGenericType(t.GetGenericArguments());
				ConstructorInfo constructor = t.GetConstructor(new[] { typeof(int) });
				if (constructor == null)
					throw new ArgumentException("IList type must have a constructor that takes a single int parameter", nameof(t));
				return (IList)constructor.Invoke(new object[] { size.Value });
			}
			else
				return Activator.CreateInstance(t);
		}
		/// <summary>
		/// Constructs a new object of the same type with a new length and copies all values from the previous instance into the new one.
		/// </summary>
		/// <param name="originalList">Fixed-size list that needs to be longer.</param>
		/// <param name="newSize">New size to set</param>
		/// <returns>The new fixed-size list.</returns>
		private static IList LengthenFixedSizeList(IList originalList, int newSize)
		{
			IList longerList = (IList)ConstructInstanceOfType(originalList.GetType(), newSize);
			for (int i = 0; i < originalList.Count; i++)
				longerList[i] = originalList[i];
			return longerList;
		}
		/// <summary>
		/// Returns an object that is the default value for the type of the given object.
		/// </summary>
		/// <param name="obj">The object whose type should be inspected.</param>
		/// <returns></returns>
		private static object GetDefaultValue(object obj)
		{
			if (obj == null)
				return null;

			Type type = obj.GetType();
			return type.IsValueType ? Activator.CreateInstance(type) : null;
		}

		#endregion
	}
	/// <summary>
	/// Exception that occurs when an object is found among the descendants of itself during an ObjectFieldMap operation.
	/// </summary>
	[Serializable]
	public class ObjectFieldMapLoopException : Exception
	{
		internal ObjectFieldMapLoopException()
		{
		}

		internal ObjectFieldMapLoopException(string message) : base(message)
		{
		}

		internal ObjectFieldMapLoopException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
