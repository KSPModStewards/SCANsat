#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN Palettes - class to hold a group of color palettes and information about them
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using UnityEngine;

namespace SCANsat.SCAN_Platform.Palettes
{
	public class _Palettes
	{
		internal Palette[] availablePalettes;
		internal Palette.Kind paletteType;
		internal Texture2D[] paletteSwatch;
		internal int size;

		internal int Length
		{
			get { return availablePalettes.Length; }
		}

		internal _Palettes (Palette[] p, Palette.Kind pK, int i)
		{
			availablePalettes = p;
			int j = 0;
			for (int d = 0; i < availablePalettes.Length; i++ )
			{
				availablePalettes[d].index = j;
				j++;
			}
			paletteType = pK;
			size = i;
			generateSwatches();
		}

		private Texture2D[] generateSwatches()
		{
			if (paletteSwatch == null)
			{
				paletteSwatch = new Texture2D[availablePalettes.Length];
				for (int i = 0; i < availablePalettes.Length; i++)
				{
					int k = 0;
					int m = 120;
					int paletteSize = availablePalettes[i].colors.Length;
					if (paletteSize == 11) m = 121;
					else if (paletteSize == 18) m = 126;
					else if (paletteSize == 9) m = 117;
					else if (paletteSize == 7) m = 119;
					else if (paletteSize == 34) m = 136;
					Texture2D t = new Texture2D(m, 1);
					Color[] pix = t.GetPixels();
					int sW = m / paletteSize;
					for (int j = 0; j < m; j++)
					{
						if (j % sW == 0)
							k++;
						pix[j] = availablePalettes[i].colors[k-1];
					}
					t.SetPixels(pix);
					t.Apply();
					paletteSwatch[i] = t;
				}
				return paletteSwatch;
			}
			else
				return paletteSwatch;
		}
	}
}

