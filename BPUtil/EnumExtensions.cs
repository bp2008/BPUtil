using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	public static class EnumExtensions
	{
		/// <summary>
		/// Gets this enum value as a string, preferring to use the optional value provided by <see cref="DescriptionAttribute"/> if available.  For some of our enums, the Description attribute contains a string from our database so ToDescriptionString() returns the string as it is found in the database.
		/// </summary>
		/// <param name="val">Enum value which may or may not have a Description annotation.</param>
		/// <returns></returns>
		public static string ToDescriptionString(this Enum val)
		{
			DescriptionAttribute[] attributes = (DescriptionAttribute[])val
			   .GetType()
			   .GetField(val.ToString())
			   ?.GetCustomAttributes(typeof(DescriptionAttribute), false);
			return attributes != null && attributes.Length > 0 ? attributes[0].Description : val.ToString();
		}
		/// <summary>
		/// Gets the value of the <see cref="DescriptionAttribute"/> assigned to this enum value, or null if none is assigned.
		/// </summary>
		/// <param name="val">Enum value which may or may not have a Description annotation.</param>
		/// <returns></returns>
		public static string GetDescriptionString(this Enum val)
		{
			DescriptionAttribute[] attributes = (DescriptionAttribute[])val
			   .GetType()
			   .GetField(val.ToString())
			   ?.GetCustomAttributes(typeof(DescriptionAttribute), false);
			return attributes != null && attributes.Length > 0 ? attributes[0].Description : null;
		}
		/// <summary>
		/// Gets a collection containing the custom attributes of the specified type that are declared on the specified enum value.
		/// 
		/// </summary>
		/// <param name="val">Enum value which may or may not have annotations of type <typeparamref name="T"/>.</param>
		/// <returns></returns>
		public static IEnumerable<T> GetCustomAttributes<T>(this Enum val)
		{
			FieldInfo fieldInfo = val.GetType().GetField(val.ToString());
			if (fieldInfo == null)
				return Enumerable.Empty<T>();
			else
				return fieldInfo.GetCustomAttributes(typeof(T), false).Cast<T>();
		}
		/// <summary>
		/// <para>Finds the enum value matching the specified string, returning true if found.  Searches the Description attributes first, then falls back to using standard <c>Enum.TryParse()</c>.</para>
		/// <para>This operation is case-sensitive.</para>
		/// </summary>
		/// <typeparam name="T">Type to match.</typeparam>
		/// <param name="description">Description string to match.</param>
		/// <param name="enumValue">(Output) the matched enum value.</param>
		/// <returns>True if the match is successful, false otherwise.</returns>
		public static bool TryParseWithDescription<T>(string description, out T enumValue) where T : struct, Enum
		{
			return TryParseWithDescription(description, false, out enumValue);
		}
		/// <summary>
		/// Finds the enum value matching the specified string, returning true if found.  Searches the Description attributes first, then falls back to using standard <c>Enum.TryParse()</c>.
		/// </summary>
		/// <typeparam name="T">Type to match.</typeparam>
		/// <param name="description">Description string to match.</param>
		/// <param name="ignoreCase">true to ignore case; false to regard case.</param>
		/// <param name="enumValue">(Output) the matched enum value.</param>
		/// <returns>True if the match is successful, false otherwise.</returns>
		public static bool TryParseWithDescription<T>(string description, bool ignoreCase, out T enumValue) where T : struct, Enum
		{
			if (description == null)
				throw new ArgumentNullException(nameof(description));
			if (ignoreCase)
			{
				foreach (T val in Enum.GetValues(typeof(T)))
				{
					if (description.IEquals(val.GetDescriptionString()))
					{
						enumValue = val;
						return true;
					}
				}
			}
			else
			{
				foreach (T val in Enum.GetValues(typeof(T)))
				{
					if (description.Equals(val.GetDescriptionString()))
					{
						enumValue = val;
						return true;
					}
				}
			}
			return Enum.TryParse(description, ignoreCase, out enumValue);
		}

		/// <summary>
		/// <para>Converts the string representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.</para>
		/// <para>This operation is case-sensitive.</para>
		/// <para>If the given string is invalid for the given enumeration type, an exception is thrown.</para>
		/// </summary>
		/// <typeparam name="T">An enumeration type.</typeparam>
		/// <param name="value">A string containing the name or value to convert.</param>
		/// <returns>An object of type <typeparamref name="T"/> whose value is represented by <paramref name="value"/>.</returns>
		public static T Parse<T>(string value) where T : struct, Enum
		{
			return (T)Enum.Parse(typeof(T), value);
		}
		/// <summary>
		/// <para>Converts the string representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.  A parameter specifies whether the operation is case-insensitive.</para>
		/// <para>If the given string is invalid for the given enumeration type, an exception is thrown.</para>
		/// </summary>
		/// <typeparam name="T">An enumeration type.</typeparam>
		/// <param name="value">A string containing the name or value to convert.</param>
		/// <param name="ignoreCase">true to ignore case; false to regard case.</param>
		/// <returns>An object of type <typeparamref name="T"/> whose value is represented by <paramref name="value"/>.</returns>
		public static T Parse<T>(string value, bool ignoreCase) where T : struct, Enum
		{
			return (T)Enum.Parse(typeof(T), value, ignoreCase);
		}
		/// <summary>
		/// <para>Converts the string representation of the Description attribute, name, or numeric value of one or more enumerated constants to an equivalent enumerated object.  Searches the Description attributes first, then falls back to using standard <c>Enum.Parse()</c>.</para>
		/// <para>This operation is case-sensitive.</para>
		/// <para>If the given string is invalid for the given enumeration type, an exception is thrown.</para>
		/// </summary>
		/// <typeparam name="T">An enumeration type.</typeparam>
		/// <param name="value">A string containing the name or value to convert.</param>
		/// <returns>An object of type <typeparamref name="T"/> whose value is represented by <paramref name="value"/>.</returns>
		public static T ParseWithDescription<T>(string value) where T : struct, Enum
		{
			return ParseWithDescription<T>(value, false);
		}
		/// <summary>
		/// <para>Converts the string representation of the Description attribute, name, or numeric value of one or more enumerated constants to an equivalent enumerated object.  A parameter specifies whether the operation is case-insensitive.  Searches the Description attributes first, then falls back to using standard <c>Enum.Parse()</c>.</para>
		/// <para>The given <paramref name="value"/> is tested against all enum descriptions first.</para>
		/// <para>If the given string is invalid for the given enumeration type, an exception is thrown.</para>
		/// </summary>
		/// <typeparam name="T">An enumeration type.</typeparam>
		/// <param name="value">A string containing the name or value to convert.</param>
		/// <param name="ignoreCase">true to ignore case; false to regard case.</param>
		/// <returns>An object of type <typeparamref name="T"/> whose value is represented by <paramref name="value"/>.</returns>
		public static T ParseWithDescription<T>(string value, bool ignoreCase) where T : struct, Enum
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			if (ignoreCase)
			{
				foreach (T val in Enum.GetValues(typeof(T)))
				{
					if (value.IEquals(val.GetDescriptionString()))
					{
						return val;
					}
				}
			}
			else
			{
				foreach (T val in Enum.GetValues(typeof(T)))
				{
					if (value.Equals(val.GetDescriptionString()))
					{
						return val;
					}
				}
			}
			return (T)Enum.Parse(typeof(T), value, ignoreCase);
		}
	}
}