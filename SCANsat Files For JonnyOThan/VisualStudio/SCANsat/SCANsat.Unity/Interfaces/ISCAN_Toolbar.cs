#region license
/*  [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * ISCAN_Toolbar - Interface for transfer of toolbar menu information
 * 
 * Copyright (c)2013 damny;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 DMagic
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using UnityEngine;

namespace SCANsat.Unity.Interfaces
{
	public interface ISCAN_Toolbar
	{
		Canvas TooltipCanvas { get; }

		bool TooltipsOn { get; }

		bool MainMap { get; set; }

		bool BigMap { get; set; }

		bool ZoomMap { get; set; }

		bool Overlay { get; set; }

		bool Instruments { get; set; }

		bool Settings { get; set; }

		bool InMenu { get; set; }

		float Scale { get; }
	}
}
