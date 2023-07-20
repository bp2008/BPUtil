using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	public static class BPMath
	{
		/// <summary>
		/// Returns this object if it is within the specified min and max values, otherwise returns the nearest (min or max) value that was provided.
		/// </summary>
		/// <typeparam name="T">Type of object.</typeparam>
		/// <param name="v">This value.</param>
		/// <param name="min">Minimum value.</param>
		/// <param name="max">Maximum value.</param>
		/// <returns></returns>
		public static T Clamp<T>(this T v, T min, T max) where T : IComparable<T>
		{
			if (v.CompareTo(min) < 0)
				return min;
			else if (v.CompareTo(max) > 0)
				return max;
			else
				return v;
		}
	}
}
