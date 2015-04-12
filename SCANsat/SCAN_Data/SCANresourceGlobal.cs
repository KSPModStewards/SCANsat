﻿using System;
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

		private Dictionary<string, SCANresourceBody> masterBodyConfigs = new Dictionary<string,SCANresourceBody>();

		private SCANtype sType;
		private SCANresourceType resourceType;
		private SCANresource_Source source;

		private Color defaultLowColor;
		private Color defaultHighColor;
		private float defaultTrans;

		private SCANresourceBody currentBody;

		internal SCANresourceGlobal(string resource, float trans, float defMin, float defMax, Color minC, Color maxC, SCANresourceType t, int S)
		{
			name = resource;
			resourceTransparency = trans;
			lowResourceColor = minC;
			highResourceColor = maxC;
			defaultMinValue = defMin;
			defaultMaxValue = defMax;
			resourceType = t;
			sType = resourceType.Type;
			source = (SCANresource_Source)S;

			setDefaultValues();
		}

		public SCANresourceGlobal()
		{
		}

		internal SCANresourceGlobal(SCANresourceGlobal copy)
		{
			SCANUtil.SCANdebugLog("Preparing New Resource Copy");
			name = copy.name;
			resourceTransparency = copy.resourceTransparency;
			lowResourceColor = copy.lowResourceColor;
			highResourceColor = copy.highResourceColor;
			sType = copy.sType;
			resourceType = copy.resourceType;
			source = copy.source;
			masterBodyConfigs = copyBodyConfigs(copy);
			defaultLowColor = copy.defaultLowColor;
			defaultHighColor = copy.defaultHighColor;
			defaultTrans = copy.defaultTrans;
			defaultMinValue = copy.defaultMinValue;
			defaultMaxValue = copy.defaultMaxValue;
		}

		private Dictionary<string, SCANresourceBody> copyBodyConfigs(SCANresourceGlobal c)
		{
			SCANUtil.SCANdebugLog("Preparing New Body Config Database");
			Dictionary<string, SCANresourceBody> newCopy = new Dictionary<string, SCANresourceBody>();
			foreach (SCANresourceBody r in c.masterBodyConfigs.Values)
			{
				SCANUtil.SCANdebugLog("Copying SCAN Body Resource Config: [{0}]", r.BodyName);
				SCANresourceBody newR = new SCANresourceBody(r);
				if (!newCopy.ContainsKey(newR.BodyName))
					newCopy.Add(newR.BodyName, newR);
			}

			return newCopy;
		}

		public override void OnDecodeFromConfigNode()
		{
			resourceType = SCANcontroller.getResourceType(name);
			sType = resourceType.Type;
			source = (SCANresource_Source)2;

			setDefaultValues();

			SCANUtil.SCANdebugLog("Resource Global Decode");
			SCANUtil.SCANdebugLog("-------->Resource Name           =>   {0}", name);
			SCANUtil.SCANdebugLog("-------->Resource Transparency   =>   {0}", resourceTransparency);
			SCANUtil.SCANdebugLog("-------->Low Resource Color      =>   {0}", lowResourceColor);
			SCANUtil.SCANdebugLog("-------->High Resource Color     =>   {0}", highResourceColor);
			SCANUtil.SCANdebugLog("-------->Resource Type           =>   {0}", resourceType.Name);
			SCANUtil.SCANdebugLog("-------->SCAN Type               =>   {0}", sType);
			SCANUtil.SCANdebugLog("-------->SCAN Resource Source    =>   {0}", source);
			try
			{
				masterBodyConfigs = Resource_Planetary_Config.ToDictionary(a => a.BodyName, a => a);
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
				SCANUtil.SCANdebugLog("Saving Master Resource Node");
				Resource_Planetary_Config = masterBodyConfigs.Values.ToList();
			}
			catch (Exception e)
			{
				SCANUtil.SCANlog("Error while saving SCANsat altimetry config data: {0}", e);
			}
		}

		public void addToBodyConfigs(string s, SCANresourceBody r, bool warn)
		{
			if (!masterBodyConfigs.ContainsKey(s))
				masterBodyConfigs.Add(s, r);
			else if (warn)
				Debug.LogError(string.Format("[SCANsat] Warning: SCANresource Dictionary Already Contains Key Of This Type: [{0}] For Body: [{1}]", r.ResourceName, s));
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

		public void logValues(string s)
		{
			SCANUtil.SCANdebugLog("Log SCAN Global Resource Values: {0}", s);
			SCANUtil.SCANdebugLog("-------->Resource Name           =>   {0}", name);
			SCANUtil.SCANdebugLog("-------->Resource Transparency   =>   {0}", resourceTransparency);
			SCANUtil.SCANdebugLog("-------->Low Resource Color      =>   {0}", lowResourceColor);
			SCANUtil.SCANdebugLog("-------->High Resource Color     =>   {0}", highResourceColor);
			SCANUtil.SCANdebugLog("-------->Resource Type           =>   {0}", resourceType.Name);
			SCANUtil.SCANdebugLog("-------->SCAN Type               =>   {0}", sType);
			SCANUtil.SCANdebugLog("-------->SCAN Resource Source    =>   {0}", source);
		}

		public string Name
		{
			get { return name; }
		}

		public float Transparency
		{
			get { return resourceTransparency; }
			internal set
			{
				if (value < 0)
					resourceTransparency = 0;
				else if (value > 80)
					resourceTransparency = 80;
				else
					resourceTransparency = value;
			}
		}

		public Color MinColor
		{
			get { return lowResourceColor; }
			internal set { lowResourceColor = value; }
		}

		public Color MaxColor
		{
			get { return highResourceColor; }
			internal set { highResourceColor = value; }
		}

		public float DefaultMinValue
		{
			get { return defaultMinValue; }
		}

		public float DefaultMaxValue
		{
			get { return defaultMaxValue; }
		}

		public SCANtype SType
		{
			get { return sType; }
		}

		public SCANresourceType ResourceType
		{
			get { return resourceType; }
		}

		public SCANresource_Source Source
		{
			get { return source; }
		}

		public int getBodyCount
		{
			get { return masterBodyConfigs.Count; }
		}

		public SCANresourceBody getBodyConfig (string body)
		{
			if (masterBodyConfigs.ContainsKey(body))
				return masterBodyConfigs[body];
			else
				SCANUtil.SCANlog("SCANsat resource celestial body config is empty; something probably went wrong here");

			return null;
		}

		public SCANresourceBody getBodyConfig (int i)
		{
			if (masterBodyConfigs.Count >= i)
				return masterBodyConfigs.ElementAt(i).Value;
			else
				SCANUtil.SCANlog("SCANsat resource celestial body config is empty; something probably went wrong here");

			return null;
		}

		public void CurrentBodyConfig(string body)
		{
			if (masterBodyConfigs.ContainsKey(body))
				currentBody = masterBodyConfigs[body];
			else
				currentBody = masterBodyConfigs.ElementAt(0).Value;
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
