using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPUtil
{
	/// <summary>
	/// Enum consisting of all the directions on a 16-point compass rose.
	/// </summary>
	public enum CompassDirection
	{
		N = 0,
		NNE = 1,
		NE = 2,
		ENE = 3,
		E = 4,
		ESE = 5,
		SE = 6,
		SSE = 7,
		S = 8,
		SSW = 9,
		SW = 10,
		WSW = 11,
		W = 12,
		WNW = 13,
		NW = 14,
		NNW = 15
	}
	/// <summary>
	/// Computes compass-related things.
	/// </summary>
	public static class Compass
	{
		/// <summary>
		/// Returns the CompassDirection enum value that best describes the absolute bearing (degrees clockwise from True North).
		/// </summary>
		/// <param name="absoluteBearingDegrees">Degrees absolute bearing (0 = north, 90 = east, 180 = south, 270 = west)</param>
		/// <returns></returns>
		public static CompassDirection GetCompassDirection(int absoluteBearingDegrees)
		{
			absoluteBearingDegrees = absoluteBearingDegrees % 360;
			if (absoluteBearingDegrees < 0)
				absoluteBearingDegrees = 360 + absoluteBearingDegrees;
			int idx = (int)Math.Round(absoluteBearingDegrees / 22.5);
			if (idx < 0 || idx > 15)
				idx = 0;
			return (CompassDirection)idx;
		}
	}
}
