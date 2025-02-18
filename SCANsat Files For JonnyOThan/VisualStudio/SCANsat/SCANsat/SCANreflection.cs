#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANreflection - assigns reflection methods at startup
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using System.Reflection;
using FinePrint;
using FinePrint.Contracts.Parameters;
using FinePrint.Utilities;
using UnityEngine;

namespace SCANsat
{
	static class SCANreflection
	{
		private static bool FinePrintFlightBandRun = false;
		private static bool FinePrintStationaryWaypointRun = false;

		private static FieldInfo _FinePrintFlightBand;
		private static FieldInfo _FinePrintStationaryWaypoint;

		internal static Waypoint FinePrintStationaryWaypointObject(StationaryPointParameter p)
		{
			Waypoint w = null;
			try
			{
				w = (Waypoint)_FinePrintStationaryWaypoint.GetValue(p);
			}
			catch (Exception e)
			{
				SCANUtil.SCANlog("Error in detecting FinePrint Stationary Waypoint object: {0}", e);
			}

			return w;
		}

		internal static FlightBand FinePrintFlightBandValue(SurveyWaypointParameter p)
		{
			FlightBand b = FlightBand.NONE;
			try
			{
				b = (FlightBand)_FinePrintFlightBand.GetValue(p);
			}
			catch (Exception e)
			{
				SCANUtil.SCANlog("Error in detecting FinePrint FlightBand object: {0}", e);
			}

			return b;
		}

		internal static bool FinePrintStationaryWaypointReflection()
		{
			if (_FinePrintStationaryWaypoint != null)
				return true;

			if (FinePrintStationaryWaypointRun)
				return false;

			FinePrintStationaryWaypointRun = true;

			try
			{
				Type sType = typeof(StationaryPointParameter);

				var field = sType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

				_FinePrintStationaryWaypoint = field[0];

				if (_FinePrintStationaryWaypoint == null)
				{
					SCANUtil.SCANlog("FinePrint Stationary Waypoint Field Not Found");
					return false;
				}

				SCANUtil.SCANlog("FinePrint Stationary Waypoint Field Assigned");

				return _FinePrintStationaryWaypoint != null;
			}
			catch (Exception e)
			{
				SCANUtil.SCANlog("Error in assigning FinePrint Stationary Waypoint method: {0}", e);
			}

			return false;
		}

		internal static bool FinePrintFlightBandReflection()
		{
			if (_FinePrintFlightBand != null)
				return true;

			if (FinePrintFlightBandRun)
				return false;

			FinePrintFlightBandRun = true;

			try
			{
				Type sType = typeof(SurveyWaypointParameter);

				var field = sType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

				_FinePrintFlightBand = field[2];

				if (_FinePrintFlightBand == null)
				{
					SCANUtil.SCANlog("FinePrint FlightBand Field Not Found");
					return false;
				}

				SCANUtil.SCANlog("FinePrint FlightBand Field Assigned");

				return _FinePrintFlightBand != null;
			}
			catch (Exception e)
			{
				SCANUtil.SCANlog("Error in assigning FinePrint FlightBand method: {0}", e);
			}

			return false;
		}

	}
}
