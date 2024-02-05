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
	/// <para>This class scans all public instance-level fields and properties to produce a map of the object.  Members inheriting from IList are supported and their elements will be scanned into the ObjectFieldMap.</para>
	/// <para>ObjectFieldMap is used by ObjectChangeReplay and ObjectThreeWayMerge classes.</para>
	/// </summary>
	public class ObjectFieldMap
	{
		/// <summary>
		/// <para>The keys are strings which identify the field or property or IList index which contains the value.  Examples: "Name", "Child.Name", "Children[0].Name".</para>
		/// <para>All values are strings or primitive value types or null.</para>
		/// </summary>
		private SortedList<string, object> map;

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

			map = new SortedList<string, object>();
			Scan(obj, null);
		}
		private void Scan(object obj, string path)
		{
			Type objType = obj?.GetType();
			if (obj == null || (objType.IsValueType && objType.IsPrimitive) || objType == typeof(string))
				map[path] = obj;
			else if (obj is IList list)
			{
				for (int i = 0; i < list.Count; i++)
				{
					Scan(list[i], StringUtil.DeNullify(path) + "[" + i + "]");
				}
			}
			else
			{
				// Scan each field
				foreach (FieldInfo field in objType.GetFields(BindingFlags.Public | BindingFlags.Instance))
				{
					object v = field.GetValue(obj);
					Scan(v, ConcatPath(path, field.Name));
				}
				// Scan each property
				foreach (PropertyInfo prop in objType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
				{
					object v = prop.GetValue(obj);
					Scan(v, ConcatPath(path, prop.Name));
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
		/// Returns a string describing the hierarchy of changes which are stored in this object.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			foreach (KeyValuePair<string, object> item in map)
			{
				if (sb.Length > 0)
					sb.AppendLine();
				sb.Append(item.Key);
				sb.Append(" = ");
				if (item.Value == null)
					sb.Append("null");
				else if (item.Value is string)
					sb.Append('"').Append(item.Value).Append('"');
				else if (item.Value is char)
					sb.Append('\'').Append(item.Value).Append('\'');
				else
					sb.Append(item.Value.ToString());
			}
			return sb.ToString();
		}
	}
}
