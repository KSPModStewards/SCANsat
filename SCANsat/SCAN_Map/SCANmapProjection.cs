#region license
/*
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 * 
 * SCANmapProjection - SCANsat projection types
 *
 * Copyright (c)2013 damny;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
*/
#endregion

using System;

namespace SCANsat.SCAN_Map
{
	public class SCANmapProjection
	{
		public static string[] projectionNames = getProjectionNames();

		private static string[] getProjectionNames()
		{
			MapProjection[] v = (MapProjection[])Enum.GetValues(typeof(MapProjection));
			string[] r = new string[v.Length];
			for (int i = 0; i < v.Length; ++i)
			{
				r[i] = v[i].ToString();
			}

			return r;
		}
	}

	public enum MapProjection
	{
		Rectangular = 0,
		KavrayskiyVII = 1,
		Polar = 2,
		Orthographic = 3,
	}
}
