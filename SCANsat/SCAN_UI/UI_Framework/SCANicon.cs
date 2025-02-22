#region license
/* 
 *  [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANsat - Static class to handle icon types
 * 
 * Copyright (c)2013 damny;
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 *
 */
#endregion
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

using palette = SCANsat.SCAN_UI.UI_Framework.SCANcolorUtil;
using Log = SCANsat.SCAN_Platform.Logging.ConsoleLogger;

namespace SCANsat.SCAN_UI.UI_Framework
{
	public class SCANicon
	{
		private static Rect pos_icon = new Rect(0, 0, 0, 0);
		private static Rect grid_pos;

		internal static void drawOrbitIconGL(int x, int y, OrbitIcon icon, Color c, Color shadow, Material iconMat, int size = 32 /*px*/, bool outline = false)
		{
			// PX [0..n]
			// ORIGIN: NorthWest
			pos_icon.x = x - (size / 2);
			pos_icon.y = y - (size / 2);
			pos_icon.width = size;
			pos_icon.height = size;

			// UV [0..1]
			// Origin: SouthWest
			grid_pos.width = 0.2f;
			grid_pos.height = 0.2f;
			grid_pos.x = 0.2f * ((int)icon % 5);
			grid_pos.y = 0.2f * (4 - (int)icon / 5);

			SCANuiUtil.drawMapIconGL(pos_icon, SCANmainMenuLoader.OrbitIconsMap, c, iconMat, shadow, outline, grid_pos, true);
		}

		public enum OrbitIcon : int
		{
			Pe = 0,
			Ap = 1,
			AN = 2,
			DN = 3,
			Plane = 4,
			Ship = 5,
			Debris = 6,
			Planet = 7,
			Mystery = 8,
			Relay = 9,
			Encounter = 10,
			Exit = 11,
			EVA = 12,
			Ball = 13,
			TargetTop = 15,
			TargetBottom = 16,
			ManeuverNode = 17,
			Station = 18,
			SpaceObject = 19,
			Rover = 20,
			Probe = 21,
			Base = 22,
			Lander = 23,
			Flag = 24,
		}
		internal static OrbitIcon orbitIconForVesselType(VesselType type)
		{
			switch (type)
			{
				case VesselType.Base:
					return OrbitIcon.Base;
				case VesselType.Debris:
					return OrbitIcon.Debris;
				case VesselType.EVA:
					return OrbitIcon.EVA;
				case VesselType.Flag:
					return OrbitIcon.Flag;
				case VesselType.Lander:
					return OrbitIcon.Lander;
				case VesselType.Plane:
					return OrbitIcon.Plane;
				case VesselType.Probe:
					return OrbitIcon.Probe;
				case VesselType.Relay:
					return OrbitIcon.Relay;
				case VesselType.Rover:
					return OrbitIcon.Rover;
				case VesselType.Ship:
					return OrbitIcon.Ship;
				case VesselType.Station:
					return OrbitIcon.Station;
				case VesselType.SpaceObject:
					return OrbitIcon.SpaceObject;
				case VesselType.Unknown:
					return OrbitIcon.Mystery;
				default:
					return OrbitIcon.Mystery;
			}
		}
	}
}

