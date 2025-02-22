#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANresourceGlobal - Serializable object for storing information about a resource
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

using SCANsat.SCAN_Platform;

namespace SCANsat.SCAN_Data
{
	public class SCANresourceGlobal : SCAN_ConfigNodeStorage
	{
		[Persistent]
		private string name;
		[Persistent]
		private Color lowResourceColor;
		[Persistent]
		private Color highResourceColor;
		[Persistent]
		private float resourceTransparency = 20;
		[Persistent]
		private float defaultMinValue = 0.001f;
		[Persistent]
		private float defaultMaxValue = 10f;
		[Persistent]
		private List<SCANresourceBody> Resource_Planetary_Config = new List<SCANresourceBody>();

		private DictionaryValueList<string, SCANresourceBody> masterBodyConfigs = new DictionaryValueList<string, SCANresourceBody>();

		private Color defaultLowColor;
		private Color defaultHighColor;
		private Color32 lowColor32;
		private Color32 highColor32;
		private float defaultTrans;
		private string displayName;

		private bool defaultZero;

		private SCANresourceBody currentBody;

		internal SCANresourceGlobal(string resource, string display, float trans, float defMin, float defMax, Color minC, Color maxC)
		{
			name = resource;
			displayName = display;
			resourceTransparency = trans;
			lowResourceColor = minC;
			highResourceColor = maxC;
			lowColor32 = (Color32)lowResourceColor;
			highColor32 = (Color32)highResourceColor;
			defaultMinValue = defMin;
			defaultMaxValue = defMax;

			setDefaultValues();
		}

		public SCANresourceGlobal()
		{
		}

		internal SCANresourceGlobal(SCANresourceGlobal copy)
		{
			name = copy.name;
			displayName = copy.displayName;
			resourceTransparency = copy.resourceTransparency;
			lowResourceColor = copy.lowResourceColor;
			highResourceColor = copy.highResourceColor;
			lowColor32 = copy.lowColor32;
			highColor32 = copy.highColor32;
			masterBodyConfigs = copyBodyConfigs(copy);
			defaultLowColor = copy.defaultLowColor;
			defaultHighColor = copy.defaultHighColor;
			defaultTrans = copy.defaultTrans;
			defaultMinValue = copy.defaultMinValue;
			defaultMaxValue = copy.defaultMaxValue;
		}

		private DictionaryValueList<string, SCANresourceBody> copyBodyConfigs(SCANresourceGlobal c)
		{
			DictionaryValueList<string, SCANresourceBody> newCopy = new DictionaryValueList<string, SCANresourceBody>();
			int l = c.masterBodyConfigs.Count;

			for (int i = 0; i < l; i++)
			{
				SCANresourceBody r = c.masterBodyConfigs.At(i);
				SCANresourceBody newR = new SCANresourceBody(r);
				if (!newCopy.Contains(newR.BodyName))
				{
					newCopy.Add(newR.BodyName, newR);
				}
			}

			return newCopy;
		}

		public override void OnDecodeFromConfigNode()
		{
			lowColor32 = (Color32)lowResourceColor;
			highColor32 = (Color32)highResourceColor;

			setDefaultValues();

			//SCANUtil.SCANlog("Loading SCANsat global resource config settings: {0}", name);
			try
			{
				int l = Resource_Planetary_Config.Count;

				for (int i = 0; i < l; i++)
				{
					SCANresourceBody r = Resource_Planetary_Config[i];

					if (r == null)
					{
						continue;
					}

					//SCANUtil.SCANlog("Loading SCANsat body resource config settings: {0} - {1}", name, r.BodyName);
					if (!masterBodyConfigs.Contains(r.BodyName))
					{
						masterBodyConfigs.Add(r.BodyName, r);
					}
				}
			}
			catch (Exception e)
			{
				SCANUtil.SCANlog("Error while loading SCANsat body resource config settings: {0}", e);
			}
		}

		private void setDefaultValues()
		{
			defaultLowColor = lowResourceColor;
			defaultHighColor = highResourceColor;
			defaultTrans = resourceTransparency;
		}

		public override void OnEncodeToConfigNode()
		{
			try
			{
				Resource_Planetary_Config = masterBodyConfigs.Values.ToList();
			}
			catch (Exception e)
			{
				SCANUtil.SCANlog("Error while saving SCANsat resource config data: {0}", e);
			}
		}

		public void addToBodyConfigs(string s, SCANresourceBody r, bool warn)
		{
			//SCANUtil.SCANlog("Adding SCANsat body resource config data: {0} - {1}", name, s);
			if (!masterBodyConfigs.Contains(s))
			{
				masterBodyConfigs.Add(s, r);
			}
			else if (warn)
			{
				Debug.LogError(string.Format("[SCANsat] Warning: SCANresource Dictionary Already Contains Key Of This Type: [{0}] For Body: [{1}]", r.ResourceName, s));
			}
		}

		public void updateBodyConfig(SCANresourceBody b)
		{
			SCANresourceBody update = getBodyConfig(b.BodyName);
			if (update != null)
			{
				update.MinValue = b.MinValue;
				update.MaxValue = b.MaxValue;
			}
		}

		public string Name
		{
			get { return name; }
		}

		public string DisplayName
		{
			get
			{
				if (string.IsNullOrEmpty(displayName))
				{
					return name;
				}

				return displayName;
			}
			set { displayName = value; }
		}

		public float Transparency
		{
			get { return resourceTransparency; }
			internal set
			{
				if (value < 0)
				{
					resourceTransparency = 0;
				}
				else if (value > 80)
				{
					resourceTransparency = 80;
				}
				else
				{
					resourceTransparency = value;
				}
			}
		}

		public bool DefaultZero
		{
			get { return defaultZero; }
			set { defaultZero = value; }
		}

		public Color MinColor
		{
			get { return lowResourceColor; }
			internal set
			{
				lowResourceColor = value;
				lowColor32 = (Color32)value;
			}
		}

		public Color MaxColor
		{
			get { return highResourceColor; }
			internal set
			{
				highResourceColor = value;
				highColor32 = (Color32)value;
			}
		}

		public Color32 MinColor32
		{
			get { return lowColor32; }
		}

		public Color32 MaxColor32
		{
			get { return highColor32; }
		}

		public float DefaultMinValue
		{
			get { return defaultMinValue; }
			internal set
			{
				if (value >= 0 && value < defaultMaxValue && value <= 100)
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
				if (value >= 0 && value > defaultMinValue && value <= 100)
				{
					defaultMaxValue = value;
				}
			}
		}

		public int getBodyCount
		{
			get { return masterBodyConfigs.Count; }
		}

		public SCANresourceBody getBodyConfig(string body, bool warn = true)
		{
			if (masterBodyConfigs.Contains(body))
			{
				return masterBodyConfigs[body];
			}
			else if (warn)
			{
				SCANUtil.SCANlog("SCANsat resource celestial body config: [{0}] is empty; something probably went wrong here", body);
			}

			return null;
		}

		public SCANresourceBody getBodyConfig(int i)
		{
			if (masterBodyConfigs.Count > i)
			{
				return masterBodyConfigs.At(i);
			}
			else
			{
				SCANUtil.SCANlog("SCANsat resource celestial body config is empty; something probably went wrong here");
			}

			return null;
		}

		public void CurrentBodyConfig(string body)
		{
			if (masterBodyConfigs.Contains(body))
			{
				currentBody = masterBodyConfigs[body];
			}
			else
			{
				currentBody = masterBodyConfigs.At(0);
			}
		}

		public SCANresourceBody CurrentBody
		{
			get { return currentBody; }
		}

		public Color DefaultLowColor
		{
			get { return defaultLowColor; }
		}

		public Color DefaultHighColor
		{
			get { return defaultHighColor; }
		}

		public float DefaultTrans
		{
			get { return defaultTrans; }
		}
	}
}
