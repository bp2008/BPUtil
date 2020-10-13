using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BPUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
	[TestClass]
	public class TestCompass
	{

		[TestMethod]
		public void TestGetCompassDirection_Wrapping()
		{
			Assert.AreEqual(CompassDirection.W, Compass.GetCompassDirection(-450));
			Assert.AreEqual(CompassDirection.N, Compass.GetCompassDirection(-360));
			Assert.AreEqual(CompassDirection.E, Compass.GetCompassDirection(-270));
			Assert.AreEqual(CompassDirection.S, Compass.GetCompassDirection(-180));
			Assert.AreEqual(CompassDirection.W, Compass.GetCompassDirection(-90));
			Assert.AreEqual(CompassDirection.N, Compass.GetCompassDirection(0));
			Assert.AreEqual(CompassDirection.E, Compass.GetCompassDirection(90));
			Assert.AreEqual(CompassDirection.S, Compass.GetCompassDirection(180));
			Assert.AreEqual(CompassDirection.W, Compass.GetCompassDirection(270));
			Assert.AreEqual(CompassDirection.N, Compass.GetCompassDirection(360));
			Assert.AreEqual(CompassDirection.E, Compass.GetCompassDirection(450));
			Assert.AreEqual(CompassDirection.S, Compass.GetCompassDirection(540));
			Assert.AreEqual(CompassDirection.W, Compass.GetCompassDirection(630));
			Assert.AreEqual(CompassDirection.N, Compass.GetCompassDirection(720));
			Assert.AreEqual(CompassDirection.E, Compass.GetCompassDirection(810));
		}
		[TestMethod]
		public void TestGetCompassDirection_Precision()
		{
			for (int i = 0; i < 360; i++)
			{
				CompassDirection d = Compass.GetCompassDirection(i);
				if (i < 12)
					Assert.AreEqual(CompassDirection.N, d);
				else if (i < 34)
					Assert.AreEqual(CompassDirection.NNE, d);
				else if (i < 57)
					Assert.AreEqual(CompassDirection.NE, d);
				else if (i < 79)
					Assert.AreEqual(CompassDirection.ENE, d);
				else if (i < 102)
					Assert.AreEqual(CompassDirection.E, d);
				else if (i < 124)
					Assert.AreEqual(CompassDirection.ESE, d);
				else if (i < 147)
					Assert.AreEqual(CompassDirection.SE, d);
				else if (i < 169)
					Assert.AreEqual(CompassDirection.SSE, d);
				else if (i < 192)
					Assert.AreEqual(CompassDirection.S, d);
				else if (i < 214)
					Assert.AreEqual(CompassDirection.SSW, d);
				else if (i < 237)
					Assert.AreEqual(CompassDirection.SW, d);
				else if (i < 259)
					Assert.AreEqual(CompassDirection.WSW, d);
				else if (i < 282)
					Assert.AreEqual(CompassDirection.W, d);
				else if (i < 304)
					Assert.AreEqual(CompassDirection.WNW, d);
				else if (i < 327)
					Assert.AreEqual(CompassDirection.NW, d);
				else if (i < 349)
					Assert.AreEqual(CompassDirection.NNW, d);
				else
					Assert.AreEqual(CompassDirection.N, d);
			}
		}
	}
}
