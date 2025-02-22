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

			int blockSize = (int)Math.Truncate(256 / (count * 1d));

			int total = 0;

			for (int i = 0; i < count; i++)
			{
				for (int j = 0; j < blockSize; j++)
				{
					if (total >= 256)
					{
						break;
					}

					int current = i * blockSize + j;

					if (stock && color)
					{
						pix[current] = biomes[i].mapColor;
					}
					else if (color)
					{
						pix[current] = palette.lerp(SCANcontroller.controller.lowBiomeColor32, SCANcontroller.controller.highBiomeColor32, (float)((i * 1f) / (count * 1f)));
					}
					else
					{
						pix[current] = palette.lerp(palette.Black, palette.White, (float)(i * 1f) / (count * 1f));
					}

					total++;
				}
			}

			int remaining = 256 - count * blockSize;
			int backCount = remaining;

			for (int i = 0; i < remaining; i++)
			{
				if (total > 256 || backCount <= 0)
				{
					break;
				}

				if (SCAN_Settings_Config.Instance.BigMapStockBiomes && color)
				{
					pix[256 - backCount] = biomes[count - 1].mapColor;
				}
				else if (color)
				{
					pix[256 - backCount] = palette.lerp(SCANcontroller.controller.lowBiomeColor32, SCANcontroller.controller.highBiomeColor32, (float)(((count - 1) * 1f) / (count * 1f)));
				}
				else
				{
					pix[256 - backCount] = palette.lerp(palette.Black, palette.White, (float)(((count - 1) * 1f) / (count * 1f)));
				}

				backCount--;

				total++;
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
	}
}
