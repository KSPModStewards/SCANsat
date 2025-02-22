using System;

namespace SCANsat.SCAN_Data
{
	public class SCANROC
	{

		private ROC roc;
		private double longitude;
		private double latitude;
		private string name;
		private int id;
		private bool known;
		private bool scanned;

		public SCANROC(ROC r, string n, double lon, double lat, bool k, bool s)
		{
			roc = r;

			id = r.rocID;
			name = n;
			longitude = lon;
			latitude = lat;
			known = k;
			scanned = s;
		}

		public double Longitude
		{
			get { return longitude; }
		}

		public double Latitude
		{
			get { return latitude; }
		}

		public string Name
		{
			get { return name; }
		}

		public int ID
		{
			get { return id; }
		}

		public bool Known
		{
			get { return known; }
		}

		public bool Scanned
		{
			get { return scanned; }
		}

		public ROC Roc
		{
			get { return roc; }
		}
	}
}
