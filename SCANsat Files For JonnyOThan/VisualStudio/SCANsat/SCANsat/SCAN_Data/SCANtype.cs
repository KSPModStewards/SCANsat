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
	public enum SCANtype : int
	{
		Nothing = 0, 		    // no data (MapTraq)
		AltimetryLoRes = 1 << 0,  // low resolution altimetry (limited zoom)
		AltimetryHiRes = 1 << 1,  // high resolution altimetry (unlimited zoom)
		Altimetry = (1 << 2) - 1, 	        // both (setting) or either (testing) altimetry
		SCANsat_1 = 1 << 2,		// Unused, reserved for future SCANsat scanner
		Biome = 1 << 3,		    // biome data
		Anomaly = 1 << 4,		    // anomalies (position of anomaly)
		AnomalyDetail = 1 << 5,	// anomaly detail (name of anomaly, etc.)
		Kethane = 1 << 6,         // Kethane
		MetallicOre = 1 << 7,             // CRP Ore
		Ore = 1 << 8,				//Stock Ore
		SolarWind = 1 << 9,       // SolarWind - He-3 - KSPI
		Uraninite = 1 << 10,        // Uranium - CRP
		Monazite = 1 << 11,        // Monazite - Thorium - KSPI
		Alumina = 1 << 12,        // Alumina - CRP - KSPI
		Water = 1 << 13,          // Water - CRP
		Aquifer = 1 << 14,        // Aquifer - CRP
		Minerals = 1 << 15,       // Minerals - CRP
		Substrate = 1 << 16,      // Substrate - CRP
		MetalOre = 1 << 17,          // Metal Ore - EPL
		Karbonite = 1 << 18,    // Karbonite - CRP
		FuzzyResources = 1 << 19,         // Low Detail Resource
		Hydrates = 1 << 20,		// Hydrates - CRP
		Gypsum = 1 << 21,		// Gypsum - CRP
		RareMetals = 1 << 22, // Exotic Minerals - CRP
		ExoticMinerals = 1 << 23,			// Dirt - CRP
		Dirt = 1 << 24,	// Rare Metals - CRP
		Borate = 1 << 25,		// Borate - KSPI
		GeoEnergy = 1 << 26,	// Geo Energy - Pathfinder
		SaltWater = 1 << 27,	// Salt Water - KSPI
		Silicates = 1 << 28,	// Silicates - KSPI

		Everything_SCAN = (1 << 6) - 1,	// All default SCANsat scanners
		Science = 524299,				// All science collection types
		AllResources = 2147483584,		// All resource types
		DefinedResources = 536346496,		// All defined resource types
		MKSResources = 301048960,			// All standard MKS/USI resources
		KSPIResourece = 437272064,					// All KSPI standard resources
		Everything = Int32.MaxValue      // All scanner types
	}
}
