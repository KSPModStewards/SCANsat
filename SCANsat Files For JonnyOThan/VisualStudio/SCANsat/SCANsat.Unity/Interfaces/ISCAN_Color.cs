#region license
/*  [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * ISCAN_Color - Interface for transfer of color management information
 * 
 * Copyright (c)2013 damny;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 DMagic
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SCANsat.Unity.Interfaces
{
	public interface ISCAN_Color
	{
		string ResourcePlanet { get; set; }

		string ResourceCurrent { get; set; }

		string TerrainPlanet { get; set; }

		string TerrainPalette { get; set; }

		string TerrainPaletteStyle { get; set; }

		bool BiomeBigMapStockColor { get; set; }

		bool BiomeBigMapWhiteBorder { get; set; }

		bool BiomeSmallMapStockColor { get; set; }

		bool BiomeSmallMapWhiteBorder { get; set; }

		bool BiomeZoomMapWhiteBorder { get; set; }

		bool TerrainClampOn { get; set; }

		bool TerrainReverse { get; set; }

		bool TerrainDiscrete { get; set; }

		bool TerrainHasSize { get; }

		float BiomeTransparency { get; set; }

		float SlopeCutoff { get; set; }

		float ResourceMin { get; set; }

		float ResourceMax { get; set; }

		float ResourceTransparency { get; set; }

		float TerrainCurrentMin { get; set; }

		float TerrainGlobalMin { get; }

		float TerrainCurrentMax { get; set; }

		float TerrainGlobalMax { get; }

		float TerrainClamp { get; set; }

		int TerrainSize { get; set; }

		int TerrainSizeMin { get; }

		int TerrainSizeMax { get; }

		Color BiomeColorOne { get; }

		Color BiomeColorTwo { get; }

		Color SlopeColorOneLo { get; }

		Color SlopeColorOneHi { get; }

		Color SlopeColorTwoLo { get; }

		Color SlopeColorTwoHi { get; }

		Color ResourceColorOne { get; }

		Color ResourceColorTwo { get; }

		Texture2D TerrainPaletteOld { get; }

		Texture2D TerrainPaletteNew { get; }

		IList<KeyValuePair<string, Texture2D>> TerrainPalettes { get; }

		IList<string> Resources { get; }

		IList<string> CelestialBodies { get; }

		IList<string> PaletteStyleNames { get; }

		void BiomeApply(Color one, Color two);

		void BiomeDefault();

		void SlopeApply(Color oneLow, Color oneHigh, Color twoLow, Color twoHigh);

		void SlopeDefault();

		void ResourceApply(Color one, Color two);

		void ResourceApplyToAll(Color one, Color two);

		void ResourceDefault();

		void ResourceDefaultToAll();

		void ResourceSaveToConfig(Color one, Color two);

		void TerrainApply();

		void TerrainDefault();

		void TerrainSaveToConfig();

		void Refresh();
	}
}
