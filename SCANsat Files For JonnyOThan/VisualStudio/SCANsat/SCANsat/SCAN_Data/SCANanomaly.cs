#region license
/*
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 * 
 * SCANanomaly - stores info on anomalies and their locations
 *
 * Copyright (c)2013 damny;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
*/
#endregion

using System;

namespace SCANsat.SCAN_Data
{
	public class SCANanomaly
	{
		internal SCANanomaly(string s, double lon, double lat, PQSMod m)
		{
			name = s;
			longitude = lon;
			latitude = lat;
			known = false;
			mod = m;
		}

		private bool known;
		private bool detail;
		private string name;
		private double longitude;
		private double latitude;
		private PQSMod mod;

		public bool Known
		{
			get { return known; }
			internal set { known = value; }
		}

		public bool Detail
		{
			get { return detail; }
			internal set { detail = value; }
		}

		public string Name
		{
			get { return name; }
		}

		public double Longitude
		{
			get { return longitude; }
		}

		public double Latitude
		{
			get { return latitude; }
		}

		public PQSMod Mod
		{
			get { return mod; }
		}
	}
}
