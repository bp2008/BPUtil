using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// <para>This class scans all fields, properties, and IList indices of an object and its children to produce an iterable map of the object.</para>
	/// <para>This class supports detecting changes in both fields and properties.</para>
	/// <para>This class supports detecting additions, removals, and reordering of lists.</para>
	/// <para>This class supports detecting changes to nested objects, which may be a different type than the parent object.</para>
	/// <para>This class supports detecting changes to an IEnumerable and its elements, but does not support merging element-level changes.</para>
	/// </summary>
	public class ObjectFieldMap
	{
		/// <summary>
		/// <para>The keys are strings which identify the hierarchy of field names, property names, or IList indices which one must traverse to find the object whose value is stored as the value.</para>
		/// <para>Only primitive value types and strings are stored as the value.</para>
		/// </summary>
		private SortedList<string, object> map;

		/// <summary>
		/// Constructs an ObjectChangeHelper which stores the changes which the user made to the object and allows those changes to be replayed on a different object.
		/// </summary>
		/// <param name="original">The original object, before the user made changes.</param>
		/// <param name="modified">The modified object, including the user's changes.</param>
		public ObjectFieldMap(object original, object modified)
		{
			value = modified;
			typeOfMember = modified?.GetType();
			if (original == null || modified == null)
			{
				setValue = original != modified;
				return;
			}
			Type originalType = original.GetType();
			Type modifiedType = modified.GetType();
			if (originalType != modifiedType)
			{
				setValue = true;
				return;
			}
			//throw new ArgumentException("ObjectChangeHelper can't detect changes between original of type " + originalType.FullName + " and object of type " + modifiedType.FullName);

			if (modifiedType.IsPrimitive || modifiedType == typeof(string))
			{
				if (!Object.Equals(original, modified))
					setValue = true;
				return;
			}

			// These objects represent more complex types.  Compare the fields and properties.
			childChanges = new Dictionary<string, ObjectChangeHelper>();

			// Check if the object is a list
			if (original is IEnumerable originalEnumerable && modified is IEnumerable modifiedEnumerable)
			{
				IEnumerator modifiedEnumerator = modifiedEnumerable.GetEnumerator();
				foreach (object originalItem in originalEnumerable)
				{
					object modifiedItem = modifiedEnumerator.Current;
					if (!Object.Equals(originalItem, modifiedItem))
					{
						setValue = true;
						return;
					}
				}
			}

			// Check each field
			foreach (FieldInfo field in originalType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				object originalValue = field.GetValue(original);
				object modifiedValue = field.GetValue(modified);

				ObjectChangeHelper c = new ObjectChangeHelper(originalValue, modifiedValue);
				if (!c.IsNoOp)
					childChanges[field.Name] = c;
			}

			// Check each property
			foreach (PropertyInfo prop in originalType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
			{
				object originalValue = prop.GetValue(original);
				object modifiedValue = prop.GetValue(modified);

				ObjectChangeHelper c = new ObjectChangeHelper(originalValue, modifiedValue);
				if (!c.IsNoOp)
					childChanges[prop.Name] = c;
			}
		}
		/// <summary>
		/// Replays the user's changes on the given object.  Returns true if a change was made.  False if no change was made.
		/// </summary>
		/// <param name="obj">An object that is the current state of a database record, upon which the user's changes should be applied.</param>
		public bool Apply<T>(ref T obj)
		{
			if (IsNoOp)
				return false;
			if (obj == null)
			{
				if (value == null)
					return false;
				obj = (T)value;
				return true;
			}
			Type objType = obj.GetType();
			if (typeOfMember != null)
			{
				// This ObjectChangeHelper is bound to a specific type.
				if (objType != typeOfMember)
					throw new Exception("ObjectChangeHelper cannot be applied to this object because its stored information is for another type. Stored type " + typeOfMember.Name + " can't apply to type " + typeof(T).Name);
			}
			if (setValue)
			{
				if (!Object.Equals(obj, value))
				{
					obj = (T)value;
					return true;
				}
				return false;
			}

			bool madeChanges = false;
			if (childChanges != null && childChanges.Count > 0)
			{
				foreach (KeyValuePair<string, ObjectChangeHelper> change in childChanges)
				{
					string memberName = change.Key;
					MemberInfo myMember = objType.GetMember(memberName, MemberTypes.Property | MemberTypes.Field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SingleOrDefault();
					if (myMember == null)
						continue;

					ObjectChangeHelper changeHelper = change.Value;
					object child;
					if (myMember is FieldInfo myField)
					{
						child = myField.GetValue(obj);
						if (changeHelper.Apply(ref child))
						{
							myField.SetValue(obj, child);
							madeChanges = true;
						}
					}
					else if (myMember is PropertyInfo myProp)
					{
						child = myProp.GetValue(obj);
						if (changeHelper.Apply(ref child))
						{
							myProp.SetValue(obj, child);
							madeChanges = true;
						}
					}
				}
			}
			return madeChanges;
		}
		/// <summary>
		/// Returns a string describing the hierarchy of changes which are stored in this object.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return ToString(0);
		}
		private string Indent(int indents)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < indents; i++)
				sb.Append("  ");
			return sb.ToString();
		}
		private string ToString(int indents)
		{
			if (IsNoOp)
				return "No-Op";
			if (setValue)
				return Indent(indents) + (value == null ? "null" : value.ToString());
			else if (childChanges != null && childChanges.Count > 0)
			{
				StringBuilder sb = new StringBuilder();
				foreach (KeyValuePair<string, ObjectChangeHelper> change in childChanges)
				{
					sb.Append(Indent(indents));
					sb.AppendLine(change.Key + " -> ");
					sb.AppendLine(change.Value.ToString(indents + 1));
				}
				return sb.ToString();
			}
			else
				return "";
		}
	}
}
