#region license
/*  [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * ISCAN_MainMap - Interface for transfer of main map information
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
	public interface ISCAN_MainMap
	{
		string Version { get; }

		bool IsVisible { get; set; }

		bool Color { get; set; }

		bool TerminatorToggle { get; set; }

		bool MapType { get; set; }

		bool Minimized { get; set; }

		bool TooltipsOn { get; }

		bool MapGenerating { get; }

		bool ResourcesOn { get; }

		float Scale { get; }

		Canvas TooltipCanvas { get; }

		Vector2 Position { get; set; }

		Dictionary<Guid, MapLabelInfo> VesselInfoList { get; }

		void ClampToScreen(RectTransform rect);

		void OpenBigMap();

		void OpenInstruments();

		void OpenZoomMap();

		void OpenSettings();

		void OpenOverlay();

		void ChangeToVessel(Guid id);

		string VesselInfo(Guid id);

		Sprite VesselType(Guid id);

		Vector2 VesselPosition(Guid id);

		void Update();
	}
}
