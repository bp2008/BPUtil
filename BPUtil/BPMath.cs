using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	public static class BPMath
	{
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
