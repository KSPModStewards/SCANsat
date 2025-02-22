#region license
/*
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 * 
 * SCANtype - Enum for SCANsat scanner types
 *
 * Copyright (c)2013 damny;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
*/
#endregion

using System;

namespace SCANsat.SCAN_Data
{
	public enum SCANtype : short
	{
		Nothing = 0,                    // no data (MapTraq)
		AltimetryLoRes = 1 << 0,        // low resolution altimetry (limited zoom)
		AltimetryHiRes = 1 << 1,        // high resolution altimetry (unlimited zoom)
		Altimetry = (1 << 2) - 1,       // both (setting) or either (testing) altimetry
		VisualLoRes = 1 << 2,           // Visual low resolution
		Biome = 1 << 3,                 // biome data
		Anomaly = 1 << 4,               // anomalies (position of anomaly)
		AnomalyDetail = 1 << 5,         // anomaly detail (name of anomaly, etc.)
		VisualHiRes = 1 << 6,           // Visual high resolution
		ResourceLoRes = 1 << 7,         // Low detail resource
		ResourceHiRes = 1 << 8,         // High detail resource

		Everything_SCAN = (1 << 9) - 1, // All default SCANsat scanners
		Science = 143,                  // All science collection types
		Everything = Int16.MaxValue     // All scanner types
	}
}
