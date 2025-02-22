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
using System;
using SCANsat.SCAN_Data;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANcolorUtil;
using Contracts.Parameters;

namespace SCANsat
{
	public static class SCANconfigLoader
	{
		private static bool globalResource = false;
		private static bool initialized = false;

		private const string configFile = "SCANsat/Resources/SCANcolors";
		private const string configNodeName = "SCAN_Color_Config";

		private const string paletteFile = "SCANsat/Resources/SCANpalettes";
		private const string paletteNodeName = "SCAN_Palette_Config";

		private static SCAN_Color_Config SCANnode;
		private static SCAN_Palette_Config SCANpalettes;

		public static SCAN_Color_Config SCANNode
		{
			get { return SCANnode; }
		}

		public static SCAN_Palette_Config SCANPalettes
		{
			get { return SCANpalettes; }
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
			SCANpalettes = new SCAN_Palette_Config(paletteFile, paletteNodeName);
			SCANnode = new SCAN_Color_Config(configFile, configNodeName);

			SCANcontroller.checkLoadedTerrainNodes();

			loadResources();
		}

		private static void loadResources()
		{
			foreach (var rs in ResourceCache.Instance.GlobalResources)
			{
				if ((HarvestTypes)rs.ResourceType != HarvestTypes.Planetary)
				{
					continue;
				}

				SCANresourceGlobal currentGlobal = SCANcontroller.getResourceNode(rs.ResourceName);

				PartResourceDefinition pr = PartResourceLibrary.Instance.GetDefinition(rs.ResourceName);

				if (currentGlobal == null)
				{
					SCANcontroller.addToResourceData(rs.ResourceName, new SCANresourceGlobal(rs.ResourceName, pr == null || string.IsNullOrEmpty(pr.displayName) ? rs.ResourceName : pr.displayName, 20, rs.Distribution.MinAbundance, rs.Distribution.MaxAbundance, palette.magenta, palette.cb_orange));//, t));
					currentGlobal = SCANcontroller.getResourceNode(rs.ResourceName, true);
				}

				currentGlobal.DisplayName = pr == null || string.IsNullOrEmpty(pr.displayName) ? rs.ResourceName : pr.displayName;
				currentGlobal.DefaultZero = rs.Distribution.PresenceChance <= 0;

				if (rs.Distribution.MinAbundance < currentGlobal.DefaultMinValue)
				{
					currentGlobal.DefaultMinValue = rs.Distribution.MinAbundance;
				}

				if (rs.Distribution.MaxAbundance > currentGlobal.DefaultMaxValue)
				{
					currentGlobal.DefaultMaxValue = rs.Distribution.MaxAbundance;
				}

				foreach (CelestialBody body in FlightGlobals.Bodies)
				{
					SCANresourceBody newBody = currentGlobal.getBodyConfig(body.bodyName, false);

					if (newBody == null)
					{
						currentGlobal.addToBodyConfigs(body.bodyName, new SCANresourceBody(rs.ResourceName, body, currentGlobal.DefaultMinValue, currentGlobal.DefaultMaxValue, currentGlobal.DefaultZero), false);
					}
					else
					{
						newBody.DefaultZero = currentGlobal.DefaultZero;
					}
				}

				SCANcontroller.addToLoadedResourceNames(rs.ResourceName);
			}

			foreach (var rsBody in ResourceCache.Instance.PlanetaryResources)
			{
				if ((HarvestTypes)rsBody.ResourceType != HarvestTypes.Planetary)
				{
					continue;
				}

				SCANresourceGlobal currentGlobal = SCANcontroller.getResourceNode(rsBody.ResourceName);

				if (currentGlobal == null)
				{
					//SCANUtil.SCANlog("Adding new global resource from planetary config: {0}", rsBody.ResourceName);
					PartResourceDefinition pr = PartResourceLibrary.Instance.GetDefinition(rsBody.ResourceName);
					SCANcontroller.addToResourceData(rsBody.ResourceName, new SCANresourceGlobal(rsBody.ResourceName, pr == null || string.IsNullOrEmpty(pr.displayName) ? rsBody.ResourceName : pr.displayName, 20, 1f, 10f, palette.magenta, palette.cb_orange));//, t));
					currentGlobal = SCANcontroller.getResourceNode(rsBody.ResourceName, true);

					currentGlobal.DefaultZero = true;

					foreach (CelestialBody body in FlightGlobals.Bodies)
					{
						SCANresourceBody newBody = currentGlobal.getBodyConfig(body.bodyName, false);

						if (newBody == null)
						{
							currentGlobal.addToBodyConfigs(body.bodyName, new SCANresourceBody(rsBody.ResourceName, body, 1f, 10f, true), false);
						}
						else
						{
							newBody.DefaultZero = true;
						}
					}
				}

				SCANresourceBody currentBody = currentGlobal.getBodyConfig(rsBody.PlanetName, false);

				if (currentBody == null)
				{
					CelestialBody body = FlightGlobals.Bodies.FirstOrDefault(a => a.bodyName == rsBody.PlanetName);
					if (body == null)
					{
						continue;
					}

					currentGlobal.addToBodyConfigs(rsBody.PlanetName, new SCANresourceBody(rsBody.ResourceName, body, rsBody.Distribution.MinAbundance, rsBody.Distribution.MaxAbundance, rsBody.Distribution.PresenceChance <= 0), false);
					currentBody = currentGlobal.getBodyConfig(rsBody.PlanetName, false);
				}

				if (rsBody.Distribution.MinAbundance < currentBody.DefaultMinValue)
				{
					currentBody.DefaultMinValue = rsBody.Distribution.MinAbundance;
				}

				if (rsBody.Distribution.MaxAbundance > currentBody.DefaultMaxValue)
				{
					currentBody.DefaultMaxValue = rsBody.Distribution.MaxAbundance;
				}

				if (rsBody.Distribution.PresenceChance <= 0)
				{
					currentBody.DefaultZero = true;
				}
				else
				{
					currentBody.DefaultZero = false;
				}

				SCANcontroller.addToLoadedResourceNames(rsBody.ResourceName, false);
			}

			foreach (CelestialBody body in FlightGlobals.Bodies)
			{
				foreach (var rsBiome in ResourceCache.Instance.BiomeResources)
				{
					if ((HarvestTypes)rsBiome.ResourceType != HarvestTypes.Planetary)
					{
						continue;
					}

					if (body.bodyName != rsBiome.PlanetName)
					{
						continue;
					}

					SCANresourceGlobal currentGlobal = SCANcontroller.getResourceNode(rsBiome.ResourceName);

					if (currentGlobal == null)
					{
						//SCANUtil.SCANlog("Adding biome resource node global config: {0}", rsBiome.ResourceName);
						PartResourceDefinition pr = PartResourceLibrary.Instance.GetDefinition(rsBiome.ResourceName);
						SCANcontroller.addToResourceData(rsBiome.ResourceName, new SCANresourceGlobal(rsBiome.ResourceName, pr == null || string.IsNullOrEmpty(pr.displayName) ? rsBiome.ResourceName : pr.displayName, 20, 1f, 10, palette.magenta, palette.cb_orange));//, t));
						currentGlobal = SCANcontroller.getResourceNode(rsBiome.ResourceName, true);

						currentGlobal.DefaultZero = true;

						foreach (CelestialBody globalBody in FlightGlobals.Bodies)
						{
							SCANresourceBody newBody = currentGlobal.getBodyConfig(globalBody.bodyName, false);

							if (newBody == null)
							{
								currentGlobal.addToBodyConfigs(globalBody.bodyName, new SCANresourceBody(rsBiome.ResourceName, globalBody, 1, 10f, true), false);
							}
							else
							{
								newBody.DefaultZero = true;
							}
						}
					}

					SCANresourceBody currentBody = currentGlobal.getBodyConfig(body.bodyName, false);

					if (currentBody == null)
					{
						currentGlobal.addToBodyConfigs(rsBiome.PlanetName, new SCANresourceBody(rsBiome.ResourceName, body, 1, 10f, true), false);
						currentBody = currentGlobal.getBodyConfig(rsBiome.PlanetName, false);
					}

					if (rsBiome.Distribution.MinAbundance < currentBody.DefaultMinValue)
					{
						currentBody.DefaultMinValue = rsBiome.Distribution.MinAbundance;
					}

					if (rsBiome.Distribution.MaxAbundance > currentBody.DefaultMaxValue)
					{
						currentBody.DefaultMaxValue = rsBiome.Distribution.MaxAbundance;
					}

					if (rsBiome.Distribution.PresenceChance > 0)
					{
						currentBody.DefaultZero = false;
					}

					SCANcontroller.addToLoadedResourceNames(rsBiome.ResourceName, false);
				}
			}

			foreach (SCANresourceGlobal global in SCANcontroller.setLoadedResourceList())
			{
				if (global == null)
				{
					continue;
				}

				foreach (CelestialBody body in FlightGlobals.Bodies)
				{
					SCANresourceBody newBody = global.getBodyConfig(body.bodyName, false);

					if (newBody == null)
					{
						global.addToBodyConfigs(body.bodyName, new SCANresourceBody(global.Name, body, global.DefaultMinValue, global.DefaultMaxValue, true), false);
					}
				}
			}

			if (SCANcontroller.MasterResourceCount == 0)
			{
				globalResource = false;
			}
			else
			{
				globalResource = true;
			}
		}
	}
}
