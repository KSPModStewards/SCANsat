#region license
/*
 * This extension is from MechJeb; Used with permission from r4m0n: https://github.com/MuMech/MechJeb2/blob/master/MechJeb2/OrbitExtensions.cs
*/
#endregion

using System;
using System.Collections.Generic;

namespace SCANsat
{

	public static class OrbitExtensions
	{
		//Returns whether a has an ascending node with b. This can be false
		//if a is hyperbolic and the would-be ascending node is within the opening
		//angle of the hyperbola.
		public static bool AscendingNodeExists(this Orbit a, Orbit b)
		{
			return Math.Abs(JUtil.ClampDegrees180(a.AscendingNodeTrueAnomaly(b))) <= a.MaximumTrueAnomaly();
		}
		//Gives the true anomaly (in a's orbit) at which a crosses its ascending node
		//with b's orbit.
		//The returned value is always between 0 and 360.
		public static double AscendingNodeTrueAnomaly(this Orbit a, Orbit b)
		{
			Vector3d vectorToAN = Vector3d.Cross(a.SwappedOrbitNormal(), b.SwappedOrbitNormal());
			return a.TrueAnomalyFromVector(vectorToAN);

		}
		//For hyperbolic orbits, the true anomaly only takes on values in the range
		// -M < true anomaly < +M for some M. This function computes M.
		public static double MaximumTrueAnomaly(this Orbit o)
		{
			if (o.eccentricity < 1)
				return 180;
			return 180 / Math.PI * Math.Acos(-1 / o.eccentricity);
		}
		//normalized vector perpendicular to the orbital plane
		//convention: as you look down along the orbit normal, the satellite revolves counterclockwise
		public static Vector3d SwappedOrbitNormal(this Orbit o)
		{
			return -(o.GetOrbitNormal().xzy).normalized;
		}

		//Returns whether a has a descending node with b. This can be false
		//if a is hyperbolic and the would-be descending node is within the opening
		//angle of the hyperbola.
		public static bool DescendingNodeExists(this Orbit a, Orbit b)
		{
			return Math.Abs(JUtil.ClampDegrees180(a.DescendingNodeTrueAnomaly(b))) <= a.MaximumTrueAnomaly();
		}
		//Gives the true anomaly (in a's orbit) at which a crosses its descending node
		//with b's orbit.
		//The returned value is always between 0 and 360.
		public static double DescendingNodeTrueAnomaly(this Orbit a, Orbit b)
		{
			return JUtil.ClampDegrees360(a.AscendingNodeTrueAnomaly(b) + 180);
		}
		//Returns the next time at which a will cross its ascending node with b.
		//For elliptical orbits this is a time between UT and UT + a.period.
		//For hyperbolic orbits this can be any time, including a time in the past if
		//the ascending node is in the past.
		//NOTE: this function will throw an ArgumentException if a is a hyperbolic orbit and the "ascending node"
		//occurs at a true anomaly that a does not actually ever attain
		public static double TimeOfAscendingNode(this Orbit a, Orbit b, double UT)
		{
			return a.TimeOfTrueAnomaly(a.AscendingNodeTrueAnomaly(b), UT);
		}
		//Returns the next time at which a will cross its descending node with b.
		//For elliptical orbits this is a time between UT and UT + a.period.
		//For hyperbolic orbits this can be any time, including a time in the past if
		//the descending node is in the past.
		//NOTE: this function will throw an ArgumentException if a is a hyperbolic orbit and the "descending node"
		//occurs at a true anomaly that a does not actually ever attain
		public static double TimeOfDescendingNode(this Orbit a, Orbit b, double UT)
		{
			return a.TimeOfTrueAnomaly(a.DescendingNodeTrueAnomaly(b), UT);
		}
		//NOTE: this function can throw an ArgumentException, if o is a hyperbolic orbit with an eccentricity
		//large enough that it never attains the given true anomaly
		public static double TimeOfTrueAnomaly(this Orbit o, double trueAnomaly, double UT)
		{
			return o.UTAtMeanAnomaly(o.GetMeanAnomalyAtEccentricAnomaly(o.GetEccentricAnomalyAtTrueAnomaly(trueAnomaly)), UT);
		}
		//The next time at which the orbiting object will reach the given mean anomaly.
		//For elliptical orbits, this will be a time between UT and UT + o.period
		//For hyperbolic orbits, this can be any time, including a time in the past, if
		//the given mean anomaly occurred in the past
		public static double UTAtMeanAnomaly(this Orbit o, double meanAnomaly, double UT)
		{
			double currentMeanAnomaly = o.MeanAnomalyAtUT(UT);
			double meanDifference = meanAnomaly - currentMeanAnomaly;
			if (o.eccentricity < 1)
				meanDifference = JUtil.ClampRadiansTwoPi(meanDifference);
			return UT + meanDifference / o.MeanMotion();
		}
		//Converts an eccentric anomaly into a mean anomaly.
		//For an elliptical orbit, the returned value is between 0 and 2pi
		//For a hyperbolic orbit, the returned value is any number
		public static double GetMeanAnomalyAtEccentricAnomaly(this Orbit o, double E)
		{
			double e = o.eccentricity;
			if (e < 1)
			{ //elliptical orbits
				return JUtil.ClampRadiansTwoPi(E - (e * Math.Sin(E)));
			} //hyperbolic orbits
			return (e * Math.Sinh(E)) - E;
		}
		//Originally by Zool, revised by The_Duck
		//Converts a true anomaly into an eccentric anomaly.
		//For elliptical orbits this returns a value between 0 and 2pi
		//For hyperbolic orbits the returned value can be any number.
		//NOTE: For a hyperbolic orbit, if a true anomaly is requested that does not exist (a true anomaly
		//past the true anomaly of the asymptote) then an ArgumentException is thrown
		public static double GetEccentricAnomalyAtTrueAnomaly(this Orbit o, double trueAnomaly)
		{
			double e = o.eccentricity;
			trueAnomaly = JUtil.ClampDegrees360(trueAnomaly);
			trueAnomaly = trueAnomaly * (Math.PI / 180);

			if (e < 1)
			{ //elliptical orbits
				double cosE = (e + Math.Cos(trueAnomaly)) / (1 + e * Math.Cos(trueAnomaly));
				double sinE = Math.Sqrt(1 - (cosE * cosE));
				if (trueAnomaly > Math.PI)
					sinE *= -1;

				return JUtil.ClampRadiansTwoPi(Math.Atan2(sinE, cosE));
			}
			else
			{  //hyperbolic orbits
				double coshE = (e + Math.Cos(trueAnomaly)) / (1 + e * Math.Cos(trueAnomaly));
				if (coshE < 1)
					throw new ArgumentException("OrbitExtensions.GetEccentricAnomalyAtTrueAnomaly: True anomaly of " + trueAnomaly + " radians is not attained by orbit with eccentricity " + o.eccentricity);

				double E = JUtil.Acosh(coshE);
				if (trueAnomaly > Math.PI)
					E *= -1;

				return E;
			}
		}
		//The mean anomaly of the orbit.
		//For elliptical orbits, the value return is always between 0 and 2pi
		//For hyperbolic orbits, the value can be any number.
		public static double MeanAnomalyAtUT(this Orbit o, double UT)
		{
			double ret = o.meanAnomalyAtEpoch + o.MeanMotion() * (UT - o.epoch);
			if (o.eccentricity < 1)
				ret = JUtil.ClampRadiansTwoPi(ret);
			return ret;
		}
		//mean motion is rate of increase of the mean anomaly
		public static double MeanMotion(this Orbit o)
		{
			return Math.Sqrt(o.referenceBody.gravParameter / Math.Abs(Math.Pow(o.semiMajorAxis, 3)));
		}
		//Converts a direction, specified by a Vector3d, into a true anomaly.
		//The vector is projected into the orbital plane and then the true anomaly is
		//computed as the angle this vector makes with the vector pointing to the periapsis.
		//The returned value is always between 0 and 360.
		public static double TrueAnomalyFromVector(this Orbit o, Vector3d vec)
		{
			Vector3d projected = Vector3d.Exclude(o.SwappedOrbitNormal(), vec);
			Vector3d vectorToPe = o.eccVec.xzy;
			double angleFromPe = Math.Abs(Vector3d.Angle(vectorToPe, projected));

			//If the vector points to the infalling part of the orbit then we need to do 360 minus the
			//angle from Pe to get the true anomaly. Test this by taking the the cross product of the
			//orbit normal and vector to the periapsis. This gives a vector that points to center of the 
			//outgoing side of the orbit. If vectorToAN is more than 90 degrees from this vector, it occurs
			//during the infalling part of the orbit.
			if (Math.Abs(Vector3d.Angle(projected, Vector3d.Cross(o.SwappedOrbitNormal(), vectorToPe))) < 90)
			{
				return angleFromPe;
			}
			return 360 - angleFromPe;
		}
		//distance from the center of the planet
		public static double Radius(this Orbit o, double UT)
		{
			return o.SwappedRelativePositionAtUT(UT).magnitude;
		}
		//position relative to the primary
		public static Vector3d SwappedRelativePositionAtUT(this Orbit o, double UT)
		{
			return o.getRelativePositionAtUT(UT).xzy;
		}
	}
}
