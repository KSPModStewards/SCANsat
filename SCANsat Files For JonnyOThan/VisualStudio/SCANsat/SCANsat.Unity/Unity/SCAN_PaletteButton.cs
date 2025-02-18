#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_PaletteButton - Script for controlling the color palette button elements
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using UnityEngine;
using UnityEngine.UI;

namespace SCANsat.Unity.Unity
{
	public class SCAN_PaletteButton : MonoBehaviour
	{
		[SerializeField]
		private RawImage m_Palette = null;

		private string paletteName;
		private SCAN_ColorAltimetry color;

		public void setup(Texture2D tex, string palette, SCAN_ColorAltimetry c)
		{
			if (m_Palette == null || c == null)
				return;

			m_Palette.texture = tex;
			paletteName = palette;
			color = c;
		}

		public void Select()
		{
			if (color == null)
				return;

			color.SetPalette(paletteName);
		}
	}
}
