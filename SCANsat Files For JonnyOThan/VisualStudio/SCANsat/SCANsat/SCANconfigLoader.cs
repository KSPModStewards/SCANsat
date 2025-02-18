#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANconfigLoader - Load the config file settings at startup
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System.Linq;
using SCANsat.SCAN_Data;
using SCANsat.SCAN_Platform;
using SCANsat.SCAN_Platform.Palettes;
using SCANsat.SCAN_Platform.Palettes.ColorBrewer;
using SCANsat.SCAN_Platform.Palettes.FixedColors;
using SCANsat.SCAN_UI.UI_Framework;
using UnityEngine;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANpalette;

namespace SCANsat
{
	public static class SCANconfigLoader
	{
		private static bool globalResource = false;
		private static bool initialized = false;

		private const string configFile = "SCANsat/Resources/SCANcolors";
		private const string configNodeName = "SCAN_Color_Config";

		private static SCAN_Color_Config SCANnode;

		public static SCAN_Color_Config SCANNode
		{
			get { return SCANnode; }
		}

		public static bool GlobalResource
		{
			get { return globalResource; }
		}

		public static bool Initialized
		{
			get { return initialized; }
		}

		internal static void configLoader()
		{
			loadSCANtypes();

			SCANnode = new SCAN_Color_Config(configFile, configNodeName);

			SCANcontroller.checkLoadedTerrainNodes();

			loadResources();
		}

		private static void loadSCANtypes()
		{
			foreach (ConfigNode node in GameDatabase.Instance.GetConfigNodes("SCANSAT_SENSOR"))
			{
				string name = "";
				int i = 0;
				if (node.HasValue("name"))
					name = node.GetValue("name");
				if (node.HasValue("SCANtype"))
					if (!int.TryParse(node.GetValue("SCANtype"), out i))
						continue;

				SCANcontroller.addToResourceTypes(name, new SCANresourceType(name, i));
			}
		}

		private static void loadResources()
		{
			foreach (var rs in ResourceCache.Instance.GlobalResources)
			{
				if ((HarvestTypes)rs.ResourceType != HarvestTypes.Planetary)
					continue;

				SCANresourceType t = OverlayResourceType(rs.ResourceName);

				if (t == null)
					continue;

				SCANresourceGlobal currentGlobal = SCANcontroller.getResourceNode(rs.ResourceName);

				PartResourceDefinition pr = PartResourceLibrary.Instance.GetDefinition(rs.ResourceName);

				if (currentGlobal == null)
				{
					SCANcontroller.addToResourceData(rs.ResourceName, new SCANresourceGlobal(rs.ResourceName, pr == null ? rs.ResourceName : pr.displayName, 20, rs.Distribution.MinAbundance, rs.Distribution.MaxAbundance, palette.magenta, palette.cb_orange, t));
					currentGlobal = SCANcontroller.getResourceNode(rs.ResourceName, true);
				}

				currentGlobal.DisplayName = pr == null ? rs.ResourceName : pr.displayName;

				if (rs.Distribution.MinAbundance > currentGlobal.DefaultMinValue)
					currentGlobal.DefaultMinValue = rs.Distribution.MinAbundance;

				if (rs.Distribution.MaxAbundance > currentGlobal.DefaultMaxValue)
					currentGlobal.DefaultMaxValue = rs.Distribution.MaxAbundance;

				foreach (CelestialBody body in FlightGlobals.Bodies)
				{
					SCANresourceBody newBody = currentGlobal.getBodyConfig(body.name, false);

					if (newBody == null)
						currentGlobal.addToBodyConfigs(body.name, new SCANresourceBody(rs.ResourceName, body, currentGlobal.DefaultMinValue, currentGlobal.DefaultMaxValue), false);
				}

				SCANcontroller.addToLoadedResourceNames(rs.ResourceName);
			}

			foreach (var rsBody in ResourceCache.Instance.PlanetaryResources)
			{
				if ((HarvestTypes)rsBody.ResourceType != HarvestTypes.Planetary)
					continue;

				SCANresourceGlobal currentGlobal = SCANcontroller.getResourceNode(rsBody.ResourceName);

				if (currentGlobal == null)
				{
					SCANresourceType t = OverlayResourceType(rsBody.ResourceName);

					if (t == null)
						continue;

					PartResourceDefinition pr = PartResourceLibrary.Instance.GetDefinition(rsBody.ResourceName);
					SCANcontroller.addToResourceData(rsBody.ResourceName, new SCANresourceGlobal(rsBody.ResourceName, pr == null ? rsBody.ResourceName : pr.displayName, 20, 0, 0.001f, palette.magenta, palette.cb_orange, t));
					currentGlobal = SCANcontroller.getResourceNode(rsBody.ResourceName, true);

					foreach (CelestialBody body in FlightGlobals.Bodies)
					{
						SCANresourceBody newBody = currentGlobal.getBodyConfig(body.name, false);

						if (newBody == null)
							currentGlobal.addToBodyConfigs(body.name, new SCANresourceBody(rsBody.ResourceName, body, 0, 0.001f), false);
					}

					SCANcontroller.addToLoadedResourceNames(rsBody.ResourceName);
				}

				SCANresourceBody currentBody = currentGlobal.getBodyConfig(rsBody.PlanetName, false);

				if (currentBody == null)
				{
					CelestialBody body = FlightGlobals.Bodies.FirstOrDefault(a => a.name == rsBody.PlanetName);
					if (body == null)
						continue;

					currentGlobal.addToBodyConfigs(rsBody.PlanetName, new SCANresourceBody(rsBody.ResourceName, body, rsBody.Distribution.MinAbundance, rsBody.Distribution.MaxAbundance), false);
					currentBody = currentGlobal.getBodyConfig(rsBody.PlanetName, false);
				}

				if (rsBody.Distribution.MinAbundance > currentBody.DefaultMinValue)
					currentBody.DefaultMinValue = rsBody.Distribution.MinAbundance;

				if (rsBody.Distribution.MaxAbundance > currentBody.DefaultMaxValue)
					currentBody.DefaultMaxValue = rsBody.Distribution.MaxAbundance;
			}

			if (SCANcontroller.MasterResourceCount == 0)
				globalResource = false;
			else
				globalResource = true;
		}

		private static SCANresourceType OverlayResourceType(string s)
		{
			return SCANcontroller.getResourceType(s, false);
		}
	}
}
