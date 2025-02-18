#region license
/*  [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * ISCAN_Overlay - Interface for transfer of overlay window information
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
	public interface ISCAN_Overlay
	{
		string Version { get; }

		string CurrentResource { get; }

		string TooltipText { get; }

		bool IsVisible { get; set; }

		bool OverlayTooltip { get; }

		bool DrawOverlay { get; set; }

		bool DrawBiome { get; set; }

		bool DrawTerrain { get; set; }

		bool DrawResource { get; }

		bool WindowTooltips { get; }

		float Scale { get; }

		Canvas TooltipCanvas { get; }

		IList<string> Resources { get; }

		Vector2 Position { get; set; }

		void ClampToScreen(RectTransform rect);

		void SetResource(string resource, bool isOn);

		void Refresh();

		void OpenSettings();

		void Update();
	}
}
