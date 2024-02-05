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
	/// <para>There is a fairly common problem in computer science wherein two users load the same database record, make changes, and then commit their changes.  Whoever commits their changes second overwrites the changes made by the first user.  This class assists in resolving that issue by making it possible to commit only the fields and properties that changed.</para>
	/// <para>This class supports detecting changes in both fields and properties.</para>
	/// <para>This class supports detecting additions, removals, and reordering of lists.</para>
	/// <para>This class supports detecting changes to nested objects, which may be a different type than the parent object.</para>
	/// </summary>
	public class ObjectChangeHelper2<T>
	{
		private Dictionary<string, object> changes = new Dictionary<string, object>();

		/// <summary>
		/// Constructs an ObjectChangeHelper which stores the changes which the user made to the object and allows those changes to be replayed on a different object.
		/// </summary>
		/// <param name="original">The original object, before the user made changes.</param>
		/// <param name="modified">The modified object, including the user's changes.</param>
		public ObjectChangeHelper2(T original, T modified)
		{
			// Compare [original] and [modified] to determine which fields have changed.  Keep track of the changes in a private field of this class so the changes can be applied by the Apply method.
			CompareAndStoreChanges(original, modified, "");
		}
		private void CompareAndStoreChanges(object original, object modified, string prefix)
		{
			if (original == null || modified == null)
				return;

			// Check if the object is a list
			if (original is IList originalList && modified is IList modifiedList)
			{
				for (int i = 0; i < Math.Min(originalList.Count, modifiedList.Count); i++)
				{
					CompareAndStoreChanges(originalList[i], modifiedList[i], $"{prefix}[{i}]");
				}
			}
			else
			{
				// Check each field
				foreach (var field in original.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
				{
					var originalValue = field.GetValue(original);
					var modifiedValue = field.GetValue(modified);

					if (!object.Equals(originalValue, modifiedValue))
					{
						changes[$"{prefix}.{field.Name}"] = modifiedValue;
					}
					else if (!field.FieldType.IsPrimitive && field.FieldType != typeof(string))
					{
						// Recurse into nested objects
						CompareAndStoreChanges(originalValue, modifiedValue, $"{prefix}.{field.Name}");
					}
				}

				// Check each property
				foreach (var prop in original.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
				{
					var originalValue = prop.GetValue(original);
					var modifiedValue = prop.GetValue(modified);

					if (!object.Equals(originalValue, modifiedValue))
					{
						changes[$"{prefix}.{prop.Name}"] = modifiedValue;
					}
					else if (!prop.PropertyType.IsPrimitive && prop.PropertyType != typeof(string))
					{
						// Recurse into nested objects
						CompareAndStoreChanges(originalValue, modifiedValue, $"{prefix}.{prop.Name}");
					}
				}
			}
		}
		/// <summary>
		/// Replays the user's changes on the given object.
		/// </summary>
		/// <param name="obj">An object that is the current state of a database record, upon which the user's changes should be applied.</param>
		public T Apply(T obj)
		{
			foreach (var change in changes)
			{
				var parts = change.Key.Split('.');
				var fieldOrPropName = parts[parts.Length - 1];
				var parent = GetParentObject(parts, obj);

				if (parent is IList parentList && int.TryParse(fieldOrPropName, out var index))
				{
					parentList[index] = change.Value;
				}
				else
				{
					var field = parent.GetType().GetField(fieldOrPropName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
					if (field != null)
					{
						field.SetValue(parent, change.Value);
					}
					else
					{
						var prop = parent.GetType().GetProperty(fieldOrPropName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
						if (prop != null)
						{
							prop.SetValue(parent, change.Value);
						}
					}
				}
			}
			return obj;
		}
		private object GetParentObject(string[] parts, object obj)
		{
			for (int i = 1; i < parts.Length - 1; i++)
			{
				if (int.TryParse(parts[i], out var index) && obj is IList list)
				{
					obj = list[index];
				}
				else
				{
					var field = obj.GetType().GetField(parts[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
					if (field != null)
					{
						obj = field.GetValue(obj);
					}
					else
					{
						var prop = obj.GetType().GetProperty(parts[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
						if (prop != null)
						{
							obj = prop.GetValue(obj);
						}
					}
				}
			}

			return obj;
		}
	}
}
