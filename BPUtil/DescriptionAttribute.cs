using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace BPUtil
{
	/// <summary>
	/// Helper methods for <see cref="System.ComponentModel.DescriptionAttribute"/>.
	/// </summary>
	public static class DescriptionHelper
	{
		/// <summary>
		/// Get the descriptions for each field or property in the given type.  Returns a dictionary keyed on field/propery name (case-sensitive), where the value is the description string.  If no description string is available, the field/property will not be in the dictionary.
		/// </summary>
		/// <typeparam name="T">Type for which to get descriptions of fields and properties.</typeparam>
		/// <returns>Returns a dictionary keyed on field/propery name (case-sensitive), where the value is the description string.  If no description string is available, the field/property will not be in the dictionary.</returns>
		public static Dictionary<string, string> GetDescriptions<T>()
		{
			Dictionary<string, string> dict = new Dictionary<string, string>();
			Type type = typeof(T);
			foreach (FieldInfo field in type.GetFields())
			{
				DescriptionAttribute attr = field.GetCustomAttribute<DescriptionAttribute>();
				if (attr != null)
					dict[field.Name] = attr.Description;
			}
			foreach (PropertyInfo prop in type.GetProperties())
			{
				DescriptionAttribute attr = prop.GetCustomAttribute<DescriptionAttribute>();
				if (attr != null)
					dict[prop.Name] = attr.Description;
			}
			return dict;
		}
	}
}
