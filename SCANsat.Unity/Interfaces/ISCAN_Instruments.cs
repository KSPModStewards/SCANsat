#region license
/*  [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * ISCAN_Instruments - Interface for transfer of instruments window information
 * 
 * Copyright (c)2013 damny;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 DMagic
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using UnityEngine;

namespace SCANsat.Unity.Interfaces
{
	public interface ISCAN_Instruments
	{
		string Version { get; }

		string Readout { get; }

		string ResourceName { get; }

		string TypingText { get; }

		string AnomalyText { get; }

		bool IsVisible { get; set; }

		bool ResourceButtons { get; }

		bool AnomalyButtons { get; }

		bool MouseAnomaly { get; set; }

		bool TooltipsOn { get; }

		float Scale { get; }

		Canvas TooltipCanvas { get; }

		Texture AnomalyCamera { get; }

		Vector2 Position { get; set; }

		void ClampToScreen(RectTransform rect);

		void NextResource();

		void PreviousResource();

		void NextAnomaly();

		void Update();
	}
}
