#region license
/*
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 * 
 * SCANmapType - All SCANsat map types
 *
 * Copyright (c)2013 damny;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
*/
#endregion

using System;

namespace SCANsat.SCAN_Map
{
	public class SCANmapType
	{
		public static string[] mapTypeNames = getMapTypeNames();

		private static string[] getMapTypeNames()
		{
			mapType[] v = (mapType[])Enum.GetValues(typeof(mapType));
			string[] r = new string[v.Length];
			for (int i = 0; i < v.Length; i++)
			{
				r[i] = v[i].ToString();
			}

			return r;
		}
	}

	public enum mapType
	{
		Altimetry = 0,
		Slope = 1,
		Biome = 2,
		Visual = 3,
	}

	public enum mapSource
	{
		Data = 0,
		BigMap = 1,
		ZoomMap = 2,
		RPM = 3,
		Overlay = 4,
	}
}
