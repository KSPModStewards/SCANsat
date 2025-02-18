#region license
/*  [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SimpleLabelInfo - Data object for simple map icon
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
	public class SimpleLabelInfo
	{
		public Sprite image;
		public int width;
		public Vector2 pos;
		public Color color;
		public bool show;

		public SimpleLabelInfo(int w, Sprite img)
		{
			image = img;
			width = w;
		}
	}
}
