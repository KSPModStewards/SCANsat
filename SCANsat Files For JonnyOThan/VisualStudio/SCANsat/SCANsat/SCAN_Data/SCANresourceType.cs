#region license
/*
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 * 
 * SCANresource - Stores a SCANsat sensor type for each resource
 *
 * Copyright (c)2013 damny;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
*/
#endregion

using UnityEngine;

namespace SCANsat.SCAN_Data
{
	public class SCANresourceType
	{
		private string name;
		private SCANtype type;

		internal SCANresourceType(string s, int i)
		{
			name = s;
			type = (SCANtype)i;
			if ((type & SCANtype.Everything_SCAN) != SCANtype.Nothing)
			{
				Debug.LogWarning("[SCANsat] Attempt To Override Default SCANsat Sensors; Resetting Resource Scanner Type [" + i + "] To 0");
				type = SCANtype.Nothing;
			}
			else if ((type & SCANtype.FuzzyResources) != SCANtype.Nothing)
			{
				Debug.LogWarning("[SCANsat] Attempt To Override M700 Resource Scanner; Resetting Resource Scanner Type [" + i + "] To 0");
				type = SCANtype.Nothing;
			}
		}

		public string Name
		{
			get { return name; }
		}

		public SCANtype Type
		{
			get { return type;}
		}

	}
}
