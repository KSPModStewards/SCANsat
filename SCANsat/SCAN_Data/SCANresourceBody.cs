#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANvresourceBody - Serializable object for storing information about resource density on a given Celestial Body
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System.CodeDom;
using System.Linq;
using SCANsat.SCAN_Platform;

namespace SCANsat.SCAN_Data
{
	public class SCANresourceBody : SCAN_ConfigNodeStorage
	{
		[Persistent]
		private string resourceName;
		[Persistent]
		private string bodyName;
		[Persistent]
		private int index;
		[Persistent]
		private float lowResourceCutoff = 0.001f;
		[Persistent]
		private float highResourceCutoff = 10f;

		private CelestialBody body;
		private float defaultMinValue, defaultMaxValue;
		private float fraction;
		private bool defaultZero;

		internal SCANresourceBody(string rName, CelestialBody Body, float min, float max, bool zero)
		{
			resourceName = rName;
			body = Body;
			bodyName = body.bodyName;
			index = body.flightGlobalsIndex;
			lowResourceCutoff = defaultMinValue = min;
			highResourceCutoff = defaultMaxValue = max;
			defaultZero = zero;
		}

		public SCANresourceBody()
		{
		}

		internal SCANresourceBody(SCANresourceBody copy)
		{
			resourceName = copy.resourceName;
			bodyName = copy.bodyName;
			index = copy.index;
			lowResourceCutoff = copy.lowResourceCutoff;
			highResourceCutoff = copy.highResourceCutoff;
			body = copy.body;
			defaultMinValue = copy.defaultMinValue;
			defaultMaxValue = copy.defaultMaxValue;
		}

		public override void OnDecodeFromConfigNode()
		{
			defaultMinValue = lowResourceCutoff;
			defaultMaxValue = highResourceCutoff;

			body = FlightGlobals.Bodies.FirstOrDefault(b => b.flightGlobalsIndex == index);
		}

		public string BodyName
		{
			get { return bodyName; }
		}

		public string ResourceName
		{
			get { return resourceName; }
		}

		public CelestialBody Body
		{
			get { return body; }
		}

		public int Index
		{
			get { return index; }
		}

		public float MinValue
		{
			get { return lowResourceCutoff; }
			internal set
			{
				if (value < 0)
				{
					lowResourceCutoff = 0;
				}
				else if (value >= highResourceCutoff)
				{
					lowResourceCutoff = highResourceCutoff - 1;
				}
				else if (value >= 100)
				{
					lowResourceCutoff = 99;
				}
				else
				{
					lowResourceCutoff = value;
				}
			}
		}

		public float MaxValue
		{
			get { return highResourceCutoff; }
			internal set
			{
				if (value <= lowResourceCutoff)
				{
					highResourceCutoff = lowResourceCutoff + 1;
				}
				else if (value <= 0)
				{
					highResourceCutoff = 1;
				}
				else if (value > 100)
				{
					highResourceCutoff = 100;
				}
				else
				{
					highResourceCutoff = value;
				}
			}
		}

		public float DefaultMinValue
		{
			get { return defaultMinValue; }
			internal set
			{
				if (value < 0)
				{
					defaultMinValue = 0;
				}
				else if (value >= defaultMaxValue)
				{
					defaultMinValue = defaultMaxValue - 1;
				}
				else if (value >= 100)
				{
					defaultMinValue = 99;
				}
				else
				{
					defaultMinValue = value;
				}
			}
		}

		public float DefaultMaxValue
		{
			get { return defaultMaxValue; }
			internal set
			{
				if (value <= defaultMinValue)
				{
					defaultMaxValue = defaultMinValue + 1;
				}
				else if (value <= 0)
				{
					defaultMaxValue = 1;
				}
				else if (value > 100)
				{
					defaultMaxValue = 100;
				}
				else
				{
					defaultMaxValue = value;
				}
			}
		}

		public bool DefaultZero
		{
			get { return defaultZero; }
			set { defaultZero = value; }
		}

		public float Fraction
		{
			get { return fraction; }
			internal set { fraction = value; }
		}
	}
}
