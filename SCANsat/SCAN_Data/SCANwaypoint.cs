#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANwaypoint - An object to store information about FinePrint waypoints
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using Contracts;
using FinePrint.Contracts.Parameters;
using FinePrint;
using FinePrint.Utilities;
using UnityEngine;

namespace SCANsat.SCAN_Data
{
	public class SCANwaypoint
	{
		internal SCANwaypoint(SurveyWaypointParameter p)
		{
			way = p.wp;
			if (way != null)
			{
				band = reflectFlightBand(p);
				root = p.Root;
				seed = way.uniqueSeed;
				param = p;
				name = way.name;
				Vector2d coords = SCANUtil.fixRetardCoordinates(new Vector2d(way.longitude, way.latitude));
				longitude = coords.x;
				latitude = coords.y;
				landingTarget = false;
			}
		}

		internal SCANwaypoint(StationaryPointParameter p)
		{
			way = reflectWaypoint(p);
			if (way != null)
			{
				band = FlightBand.NONE;
				root = p.Root;
				seed = way.uniqueSeed;
				param = p;
				name = way.name;
				Vector2d coords = SCANUtil.fixRetardCoordinates(new Vector2d(way.longitude, way.latitude));
				longitude = coords.x;
				latitude = coords.y;
				landingTarget = false;
			}
		}

		internal SCANwaypoint(Waypoint p)
		{
			way = p;
			band = FlightBand.NONE;
			root = p.contractReference;
			seed = way.uniqueSeed;
			param = null;
			name = way.name;
			Vector2d coords = SCANUtil.fixRetardCoordinates(new Vector2d(way.longitude, way.latitude));
			longitude = coords.x;
			latitude = coords.y;
			landingTarget = false;
		}

		public SCANwaypoint(double lat, double lon, string n)
		{
			way = null;
			band = FlightBand.NONE;
			seed = Random.Range(0, int.MaxValue);
			root = null;
			param = null;
			name = n;
			longitude = SCANUtil.fixLonShift(lon);
			latitude = SCANUtil.fixLatShift(lat);
			landingTarget = true;
		}

		private Waypoint way;
		private string name;
		private double longitude;
		private double latitude;
		private int seed;
		private FlightBand band;
		private Contract root;
		private ContractParameter param;
		private bool landingTarget;

		public Waypoint Way
		{
			get { return way; }
		}

		public string Name
		{
			get { return name; }
		}

		public int Seed
		{
			get { return seed; }
		}

		public Contract Root
		{
			get { return root; }
		}

		public ContractParameter Param
		{
			get { return param; }
		}

		public double Longitude
		{
			get { return longitude; }
		}

		public double Latitude
		{
			get { return latitude; }
		}

		public FlightBand Band
		{
			get { return band; }
		}

		public bool LandingTarget
		{
			get { return landingTarget; }
		}

		private Waypoint reflectWaypoint(StationaryPointParameter p)
		{
			if (SCANmainMenuLoader.FinePrintStationaryWaypoint)
			{
				return SCANreflection.FinePrintStationaryWaypointObject(p);
			}

			return null;
		}

		private FlightBand reflectFlightBand(SurveyWaypointParameter p)
		{
			if (SCANmainMenuLoader.FinePrintFlightBand)
			{
				return SCANreflection.FinePrintFlightBandValue(p);
			}

			return FlightBand.NONE;
		}
	}
}
