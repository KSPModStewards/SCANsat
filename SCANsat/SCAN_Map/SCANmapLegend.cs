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

		public Texture2D getLegend(bool color, SCANterrainConfig terrain)
		{
			if (legend != null && legendMin == terrain.MinTerrain && legendMax == terrain.MaxTerrain && legendScheme == color && terrain.ColorPal.Hash == dataPalette.Hash)
			{
				return legend;
			}

			body = null;

			legend = new Texture2D(256, 1, TextureFormat.RGB24, false);
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

			legend = new Texture2D(256, 1, TextureFormat.RGB24, false);
			legendMin = min;
			legendMin = max;
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

			legend = new Texture2D(256, 1, TextureFormat.RGB24, false);
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
			Texture2D t = new Texture2D(256, 1, TextureFormat.RGB24, false);
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
			Texture2D t = new Texture2D(128, 1, TextureFormat.RGB24, false);
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
			int digits = (int)Math.Floor(Math.Log10(terrainMax - terrainMin));
			int round = (int)Math.Pow(10, digits - 1);

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