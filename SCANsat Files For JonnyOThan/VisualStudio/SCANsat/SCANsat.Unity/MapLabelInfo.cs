#region license
/*  [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * MapLabelInfo - Data object for map icon and labels
 * 
 * Copyright (c)2013 damny;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 DMagic
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using UnityEngine;

namespace SCANsat.Unity
{
	public struct MapLabelInfo
	{
		public string label;
		public string name;
		public Sprite image;
		public Vector2 pos;
		public Color baseColor;
		public Color flashColor;
		public bool flash;
		public int width;
		public int alignBottom;
		public bool show;
	}
}
