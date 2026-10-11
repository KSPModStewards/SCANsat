#region license
/*
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 * 
 * SCANmapLegend - Object to store data on map legend textures
 *
 * Copyright (c)2013 damny;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
*/
#endregion

using System;
using SCANsat.SCAN_Palettes;
using SCANsat.SCAN_Data;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANcolorUtil;

using UnityEngine;
using System.Collections.Generic;

namespace SCANsat.SCAN_Map
{
	public class SCANmapLegend
	{
		private Texture2D legend;
		private float legendMin, legendMax;
		private SCANPalette dataPalette;
		private bool legendScheme;
		private bool stockScheme;
		private CelestialBody body;

		public Texture2D Legend
		{
			get { return legend; }
			set { legend = value; }
		}

		// Legend bars are stretched far wider than their texel count. Clamp stops the bilinear
		// filter from bleeding the wrapped-around first texel into the last one (stray colours at
		// both ends); Point keeps discrete blocks (biomes, discrete palettes) crisp.
		private static Texture2D newLegendTexture(int width, FilterMode filter)
		{
			Texture2D t = new Texture2D(width, 1, TextureFormat.RGB24, false);
			t.wrapMode = TextureWrapMode.Clamp;
			t.filterMode = filter;
			return t;
		}

		public Texture2D getLegend(bool color, SCANterrainConfig terrain)
		{
			if (legend != null && legendMin == terrain.MinTerrain && legendMax == terrain.MaxTerrain && legendScheme == color && terrain.ColorPal.Hash == dataPalette.Hash)
			{
				return legend;
			}

			body = null;

			if (legend != null) UnityEngine.Object.Destroy(legend);
			legend = newLegendTexture(256, FilterMode.Bilinear);
			legendMin = terrain.MinTerrain;
			legendMax = terrain.MaxTerrain;
			legendScheme = color;
			dataPalette = terrain.ColorPal;
			Color32[] pix = new Color32[256];
			for (int x = 0; x < 256; ++x)
			{
				float val = (x * (legendMax - legendMin)) / 256f + legendMin;
				pix[x] = palette.heightToColor(val, color, terrain);
			}
			legend.SetPixels32(pix);
			legend.Apply();
			return legend;
		}

		public Texture2D getLegend(float min, float max, bool color, SCANterrainConfig terrain)
		{
			if (legend != null && legendMin == min && legendMax == max && legendScheme == color && terrain.ColorPal.Hash == dataPalette.Hash)
			{
				return legend;
			}

			if (legend != null) UnityEngine.Object.Destroy(legend);
			legend = newLegendTexture(256, FilterMode.Bilinear);
			legendMin = min;
			legendMax = max;
			legendScheme = color;
			dataPalette = terrain.ColorPal;
			Color32[] pix = new Color32[256];
			for (int x = 0; x < 256; ++x)
			{
				float val = (x * (max - min)) / 256f + min;
				pix[x] = palette.heightToColor(val, color, terrain, min, max, max - min, true);
			}
			legend.SetPixels32(pix);
			legend.Apply();
			return legend;
		}

		public Texture2D getLegend(SCANdata data, bool color, bool stock, CBAttributeMapSO.MapAttribute[] biomes, bool reset = false)
		{
			if (legend != null && legendScheme == color && stockScheme == stock && body == data.Body && !reset)
			{
				return legend;
			}

			dataPalette = new SCANPalette();

			if (legend != null) UnityEngine.Object.Destroy(legend);
			legend = newLegendTexture(256, FilterMode.Point);
			body = data.Body;
			legendScheme = color;
			stockScheme = stock;

			Color32[] pix = new Color32[256];

			int count = biomes.Length;
			for (int biome_idx = 0; biome_idx < count; biome_idx++)
			{
				int start = (int)Math.Round(biome_idx * 256 / (count * 1d));
				int end = (int)Math.Round((biome_idx + 1) * 256 / (count * 1d));

				for (int i = start; i < end; i++)
				{
					if (stock && color)
					{
						pix[i] = biomes[biome_idx].mapColor;
					}
					else if (color)
					{
						pix[i] = palette.lerp(SCANcontroller.controller.lowBiomeColor32, SCANcontroller.controller.highBiomeColor32, (float)((biome_idx * 1f) / (count * 1f)));
					}
					else
					{
						pix[i] = palette.lerp(palette.Black, palette.White, (float)(biome_idx * 1f) / (count * 1f));
					}
				}
			}

			legend.SetPixels32(pix);
			legend.Apply();
			return legend;
		}

		public static Texture2D getStaticLegend(SCANterrainConfig terrain)
		{
			Texture2D t = newLegendTexture(256, FilterMode.Bilinear);
			Color32[] pix = new Color32[256];
			for (int x = 0; x < 256; ++x)
			{
				float val = (x * (terrain.MaxTerrain - terrain.MinTerrain)) / 256f + terrain.MinTerrain;
				pix[x] = palette.heightToColor(val, true, terrain);
			}
			t.SetPixels32(pix);
			t.Apply();
			return t;
		}

		public static Texture2D getStaticLegend(float max, float min, float range, float? clamp, bool discrete, Color32[] c)
		{
			Texture2D t = newLegendTexture(128, discrete ? FilterMode.Point : FilterMode.Bilinear);
			Color32[] pix = new Color32[128];
			for (int x = 0; x < 128; x++)
			{
				float val = (x * (max - min)) / 128f + min;
				pix[x] = palette.heightToColor(val, max, min, range, clamp, discrete, c);
			}
			t.SetPixels32(pix);
			t.Apply();
			return t;
		}

		/// For a given min and max terrain height, generate a set of label strings to 2 significant figures precision between max and min. Used in Big and Zoom maps to display total range.
		/// </summary>
		/// <param name="terrainMin">Value in Minimum Label</param>
		/// <param name="terrainMax">Value in Maximum Label</param>
		/// <returns>IList<string> containing the three legend labels to display.</returns>
		public static IList<string> LegendLabels(double terrainMin, double terrainMax)
		{
			// The rounding step is 10^(digits-1), two significant figures of the range. That step is
			// 0 for any range narrower than 10 m - it rounds 10^-1 down to 0 - and Log10 of a range
			// of 0 (a flat body, or a config whose min and max were set equal) is -infinity, either
			// of which printed every label as "0". Below the width where a step means anything, round
			// to whole metres.
			double span = terrainMax - terrainMin;
			int round = 1;

			if (span > 0)
			{
				int digits = (int)Math.Floor(Math.Log10(span));

				if (digits > 1)
				{
					round = (int)Math.Pow(10, Math.Min(digits - 1, 9));
				}
			}

			string one = string.Format("|\n{0}", (((int)Math.Round(terrainMin / round)) * round).ToString("N0"));

			string two = string.Format("|\n{0}", (((int)Math.Round(((terrainMin + terrainMax) / 2) / round)) * round).ToString("N0"));

			string three = string.Format("|\n{0}", (((int)Math.Round(terrainMax / round)) * round).ToString("N0"));

			return new List<string>(3) { one, two, three };
		}

		/// <summary>
		/// For a given SCANterrainConfig, generate a set of label strings to 3 significant figures precision. Used in Big and Zoom maps to display total range.
		/// </summary>
		/// <param name="config">SCANterrainConfig instance for a celestial body</param>
		/// <returns>IList<string> containing the three legend labels to display.</returns>
		public static IList<string> LegendLabels(SCANterrainConfig config)
		{
			if (config == null)
			{
				return null;
			}

			return LegendLabels(config.MinTerrain, config.MaxTerrain);
		}
	}
}