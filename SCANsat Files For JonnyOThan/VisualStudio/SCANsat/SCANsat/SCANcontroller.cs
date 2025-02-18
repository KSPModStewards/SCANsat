#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANcontroller - scenario module that handles all scanning
 * 
 * Copyright (c)2013 damny;
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Contracts;
using FinePrint.Contracts;
using FinePrint.Utilities;
using SCANsat.SCAN_UI;
using SCANsat.SCAN_UI.UI_Framework;
using SCANsat.SCAN_Unity;
using SCANsat.SCAN_Data;
using SCANsat.SCAN_Map;
using SCANsat.SCAN_PartModules;
using SCANsat.SCAN_Platform;
using SCANsat.SCAN_Platform.Extensions.ConfigNodes;
using SCANsat.SCAN_Platform.Palettes;
using SCANsat.SCAN_Platform.Palettes.ColorBrewer;
using SCANsat.SCAN_Platform.Palettes.FixedColors;
using SCANsat.SCAN_Toolbar;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANpalette;

namespace SCANsat
{
	[KSPScenario(ScenarioCreationOptions.AddToAllGames | ScenarioCreationOptions.AddToExistingGames, GameScenes.FLIGHT, GameScenes.SPACECENTER, GameScenes.TRACKSTATION)]
	public class SCANcontroller : ScenarioModule
	{
		public static SCANcontroller controller
		{
			get { return instance; }
		}

		private static int minScanAlt = 5000;
		private static int maxScanAlt = 500000;
		private static int bestScanAlt = 250000;

		[KSPField(isPersistant = true)]
		public bool mainMapVisible = false;
		[KSPField(isPersistant = true)]
		public bool mainMapColor = true;
		[KSPField(isPersistant = true)]
		public bool mainMapTerminator = false;
		[KSPField(isPersistant = true)]
		public bool mainMapBiome = false;
		[KSPField(isPersistant = true)]
		public bool mainMapMinimized = false;
		[KSPField(isPersistant = true)]
		public bool bigMapVisible = false;
		[KSPField(isPersistant = true)]
		public bool bigMapColor = true;
		[KSPField(isPersistant = true)]
		public bool bigMapTerminator = false;
		[KSPField(isPersistant = true)]
		public bool bigMapGrid = true;
		[KSPField(isPersistant = true)]
		public bool bigMapOrbit = true;
		[KSPField(isPersistant = true)]
		public bool bigMapWaypoint = true;
		[KSPField(isPersistant = true)]
		public bool bigMapAnomaly = true;
		[KSPField(isPersistant = true)]
		public bool bigMapFlag = true;
		[KSPField(isPersistant = true)]
		public bool bigMapLegend = true;
		[KSPField(isPersistant = true)]
		public bool bigMapResourceOn = false;
		[KSPField(isPersistant = true)]
		public string bigMapProjection = "Rectangular";
		[KSPField(isPersistant = true)]
		public string bigMapType = "Altimetry";
		[KSPField(isPersistant = true)]
		public string bigMapResource = "Ore";
		[KSPField(isPersistant = true)]
		public string bigMapBody = "Kerbin";
		[KSPField(isPersistant = true)]
		public bool zoomMapVesselLock = false;
		[KSPField(isPersistant = true)]
		public bool zoomMapColor = true;
		[KSPField(isPersistant = true)]
		public bool zoomMapTerminator = false;
		[KSPField(isPersistant = true)]
		public bool zoomMapOrbit = true;
		[KSPField(isPersistant = true)]
		public bool zoomMapIcons = true;
		[KSPField(isPersistant = true)]
		public bool zoomMapLegend = true;
		[KSPField(isPersistant = true)]
		public bool zoomMapResourceOn = false;
		[KSPField(isPersistant = true)]
		public string zoomMapType = "Altimetry";
		[KSPField(isPersistant = true)]
		public string zoomMapResource = "Ore";
		[KSPField(isPersistant = true)]
		public int zoomMapState = 0;
		[KSPField(isPersistant = true)]
		public int overlaySelection = 0;
		[KSPField(isPersistant = true)]
		public string overlayResource = "Ore";
		
		public Color32 lowBiomeColor32 = new Color(0, 0.46f, 0.02345098f, 1);
		public Color32 highBiomeColor32 = new Color(0.7f, 0.2388235f, 0, 1);
		public Color32 lowSlopeColorOne32 = new Color(0.004705883f, 0.6f, 0.3788235f, 1);
		public Color32 highSlopeColorOne32 = new Color(0.9764706f, 1, 0.4627451f, 1);
		public Color32 lowSlopeColorTwo32 = new Color(0.9764706f, 1, 0.4627451f, 1);
		public Color32 highSlopeColorTwo32 = new Color(0.94f, 0.2727843f, 0.007372549f, 1);

		/* Available resources for overlays; loaded from SCANsat configs; only loaded once */
		private static DictionaryValueList<string, SCANresourceGlobal> masterResourceNodes = new DictionaryValueList<string, SCANresourceGlobal>();

		/* Resource types loaded from SCANsat configs; only needs to be loaded once */
		private static Dictionary<string, SCANresourceType> resourceTypes = new Dictionary<string, SCANresourceType>();

		/* Terrain height and color option containers loaded from SCANsat configs; only needs to be loaded once */
		private static Dictionary<string, SCANterrainConfig> masterTerrainNodes = new Dictionary<string, SCANterrainConfig>();

		/* List of resources currently loaded from resource addons */
		private static List<string> loadedResources = new List<string>();

		/* Primary SCANsat vessel dictionary; loaded every time */
		public DictionaryValueList<Guid, SCANvessel> knownVessels = new DictionaryValueList<Guid, SCANvessel>();

		/* Primary SCANdata dictionary; loaded every time*/
		private DictionaryValueList<string, SCANdata> body_data = new DictionaryValueList<string, SCANdata>();

		/* MechJeb Landing Target Integration */
		private bool mechjebLoaded;
		private SCANwaypoint landingTarget;

		/* Kopernicus On Demand Loading Data */
		private List<CelestialBody> dataBodies = new List<CelestialBody>();
		private CelestialBody bigMapBodyPQS;
		private CelestialBody zoomMapBody;
		private PQSMod KopernicusOnDemand;
		
		private SCAN_UI_MainMap _mainMap;
		private SCAN_UI_Instruments _instruments;
		private SCAN_UI_BigMap _bigMap;
		private SCAN_UI_ZoomMap _zoomMap;
		private SCAN_UI_Overlay _overlay;
		private SCAN_UI_Settings _settings;
		
		/* App launcher object */
		internal SCANappLauncher appLauncher;

		/* Used in case the loading process is interupted somehow */
		private bool loaded = false;

		/* Used to make sure all contracts are loaded */
		private bool contractsLoaded = false;

		/* Used as holder for vessel id's while loading */
		private List<Guid> tempIDs = new List<Guid>();

		private bool heightMapsBuilt = false;

		private static SCANcontroller instance;

		#region Public Accessors

		public SCANdata getData(string bodyName)
		{
			if (body_data.Contains(bodyName))
				return body_data[bodyName];

			return null;
		}

		public SCANdata getData(int index)
		{
			if (body_data.Count > index)
				return body_data.At(index);
			else
				SCANUtil.SCANdebugLog("SCANdata dictionary index out of range; something went wrong here...");

			return null;
		}

		public List<SCANdata> GetAllData
		{
			get { return body_data.Values.ToList(); }
		}

		public int GetDataCount
		{
			get { return body_data.Count; }
		}

		/* Use this method to protect against duplicate dictionary keys */
		public void addToBodyData (CelestialBody b, SCANdata data)
		{
			if (!body_data.Contains(b.name))
				body_data.Add(b.name, data);
			else
				Debug.LogError("[SCANsat] Warning: SCANdata Dictionary Already Contains Key of This Type");
		}

		public static List<SCANterrainConfig> EncodeTerrainConfigs
		{
			get
			{
				try
				{
					return masterTerrainNodes.Values.ToList();
				}
				catch (Exception e)
				{
					SCANUtil.SCANlog("Error while saving SCANsat altimetry config data: {0}", e);
				}

				return new List<SCANterrainConfig>();
			}
		}

		public static void setMasterTerrainNodes (List<SCANterrainConfig> terrainConfigs)
		{
			masterTerrainNodes.Clear();
			try
			{
				masterTerrainNodes = terrainConfigs.ToDictionary(a => a.Name, a => a);
			}
			catch (Exception e)
			{
				SCANUtil.SCANlog("Error while loading SCANsat terrain config settings: {0}", e);
			}
		}

		public static void checkLoadedTerrainNodes()
		{
			for (int i = 0; i < FlightGlobals.Bodies.Count; i++)
			{
				CelestialBody b = FlightGlobals.Bodies[i];

				if (b == null)
					continue;

				if (getTerrainNode(b.name) == null)
				{
					float? clamp = null;
					if (b.ocean)
						clamp = 0;

					float newMax;

					try
					{
						newMax = ((float)CelestialUtilities.GetHighestPeak(b)).Mathf_Round(-2);
					}
					catch (Exception e)
					{
						SCANUtil.SCANlog("Error in calculating Max Height for {0}; using default value/n{1}", b.displayName, e);
						newMax = SCANconfigLoader.SCANNode.DefaultMaxHeightRange;
					}

					SCANUtil.SCANlog("Generating new SCANsat Terrain Config for [{0}] - Max Height: [{1:F0}m]", b.displayName, newMax);

					addToTerrainConfigData(b.name, new SCANterrainConfig(SCANconfigLoader.SCANNode.DefaultMinHeightRange, newMax, clamp, SCANUtil.paletteLoader(SCANconfigLoader.SCANNode.DefaultPalette, 7), 7, false, false, b));
				}
			}
		}

		public static SCANterrainConfig getTerrainNode(string name)
		{
			if (masterTerrainNodes.ContainsKey(name))
				return masterTerrainNodes[name];
			else
				SCANUtil.SCANlog("SCANsat terrain config [{0}] cannot be found in master terrain storage list", name);

			return null;
		}

		public static void updateTerrainConfig (SCANterrainConfig t)
		{
			SCANterrainConfig update = getTerrainNode(t.Name);
			if (update != null)
			{
				update.MinTerrain = t.MinTerrain;
				update.MaxTerrain = t.MaxTerrain;
				update.ClampTerrain = t.ClampTerrain;
				update.PalSize = t.PalSize;
				update.PalRev = t.PalRev;
				update.PalDis = t.PalDis;
				update.ColorPal = t.ColorPal;
			}
		}

		public static void addToTerrainConfigData (string name, SCANterrainConfig data)
		{
			if (!masterTerrainNodes.ContainsKey(name))
				masterTerrainNodes.Add(name, data);
			else
				Debug.LogError("[SCANsat] Warning: SCANterrain Data Dictionary Already Contains Key Of This Type");
		}

		public static int MasterResourceCount
		{
			get { return loadedResources.Count; }
		}

		public static List<SCANresourceGlobal> EncodeResourceConfigs
		{
			get
			{
				try
				{
					return masterResourceNodes.Values.ToList();
				}
				catch (Exception e)
				{
					SCANUtil.SCANlog("Error while saving SCANsat resource config data: {0}", e);
				}

				return new List<SCANresourceGlobal>();
			}
		}

		public static void setMasterResourceNodes (List<SCANresourceGlobal> resourceConfigs)
		{
			masterResourceNodes.Clear();

			try
			{
				int l = resourceConfigs.Count;

				for (int i = 0; i < l; i++)
				{
					SCANresourceGlobal r = resourceConfigs[i];

					if (r == null)
						continue;

					if (!masterResourceNodes.Contains(r.Name))
						masterResourceNodes.Add(r.Name, r);
				}
			}
			catch (Exception e)
			{
				SCANUtil.SCANlog("Error while loading SCANsat resource config settings: {0}", e);
			}
		}

		public static SCANresourceGlobal getResourceNode (string resourceName, bool warn = false)
		{
			if (masterResourceNodes.Contains(resourceName))
				return masterResourceNodes[resourceName];
			else if (warn)
				SCANUtil.SCANlog("SCANsat resource [{0}] cannot be found in master resource storage list", resourceName);

			return null;
		}

		public static SCANresourceGlobal GetFirstResource
		{
			get
			{
				if (masterResourceNodes.Count > 0)
					return masterResourceNodes.At(0);
				else
					SCANUtil.SCANlog("SCANsat resource storage list is empty; something probably went wrong here...");

				return null;
			}
		}

		public static void updateSCANresource (SCANresourceGlobal r, bool all)
		{
			SCANresourceGlobal update = getResourceNode(r.Name, true);
			if (update != null)
			{
				update.MinColor = r.MinColor;
				update.MaxColor = r.MaxColor;
				update.Transparency = r.Transparency;
				if (all)
				{
					for (int i = 0; i < update.getBodyCount; i++)
					{
						SCANresourceBody b = update.getBodyConfig(i);
						if (b != null)
						{
							SCANresourceBody bNew = r.getBodyConfig(b.BodyName);
							if (bNew != null)
							{
								b.MinValue = bNew.MinValue;
								b.MaxValue = bNew.MaxValue;
							}
						}
					}
				}
				else
				{
					SCANresourceBody b = update.getBodyConfig(r.CurrentBody.BodyName);
					if (b != null)
					{
						b.MinValue = r.CurrentBody.MinValue;
						b.MaxValue = r.CurrentBody.MaxValue;
					}
				}
			}
		}

		public static void addToResourceData (string name, SCANresourceGlobal res)
		{
			if (!masterResourceNodes.Contains(name))
			{
				masterResourceNodes.Add(name, res);
			}
			else
				Debug.LogError(string.Format("[SCANsat] Warning: SCANResource Dictionary Already Contains Key of This Type: Resource: {0}", name));
		}

		public static SCANresourceType getResourceType (string name, bool warn = true)
		{
			if (resourceTypes.ContainsKey(name))
				return resourceTypes[name];
			else if (warn)
				SCANUtil.SCANlog("SCANsat resource type [{0}] cannot be found in master resource type storage list", name);

			return null;
		}

		public static void addToResourceTypes (string name, SCANresourceType type)
		{
			if (!resourceTypes.ContainsKey(name))
			{
				resourceTypes.Add(name, type);
			}
			else
				Debug.LogError(string.Format("[SCANsat] Warning: SCANResourceType Dictionary Already Contains Key of This Name: SCAN Resource Type: {0}", name));
		}

		public static void addToLoadedResourceNames (string name)
		{
			if (!loadedResources.Contains(name))
				loadedResources.Add(name);
			else
				Debug.LogError(string.Format("[SCANsat] Warning: Loaded Resource List Already Contains Resource Of Name: {0}", name));
		}

		public static List<SCANresourceGlobal> setLoadedResourceList()
		{
			List<SCANresourceGlobal> rList = new List<SCANresourceGlobal>();
			SCANresourceGlobal ore = null;

			int l = loadedResources.Count;

			for (int i = 0; i < l; i++)
			{
				string r = loadedResources[i];

				if (string.IsNullOrEmpty(r))
					continue;

				if (masterResourceNodes.Contains(r))
				{
					if (r != "Ore")
						rList.Add(masterResourceNodes[r]);
					else
						ore = masterResourceNodes[r];
				}
			}

			if (ore != null)
				rList.Insert(0, ore);

			return rList;
		}

		public static List<SCANresourceGlobal> resources()
		{
			return masterResourceNodes.Values.ToList();
		}

		public List<SCANvessel> Known_Vessels
		{
			get { return knownVessels.Values.ToList(); }
		}

		public int ActiveSensors
		{
			get { return activeSensors; }
		}

		public int ActiveVessels
		{
			get { return activeVessels; }
		}

		public int ActualPasses
		{
			get { return actualPasses; }
		}

		public bool ContractsLoaded
		{
			get { return contractsLoaded; }
		}

		public bool MechJebLoaded
		{
			get { return mechjebLoaded; }
			set { mechjebLoaded = value; }
		}

		public SCANwaypoint LandingTarget
		{
			get { return landingTarget; }
			set { landingTarget = value; }
		}

		public class OnMJTargetSet : UnityEvent<Vector2d, CelestialBody> { }

		public OnMJTargetSet MJTargetSet = new OnMJTargetSet();

		#endregion

		#region save/load

		public override void OnLoad(ConfigNode node)
		{
			ConfigNode node_vessels = node.GetNode("Scanners");
			if (node_vessels != null)
			{
				SCANUtil.SCANlog("SCANsat Controller: Loading {0} known vessels", node_vessels.CountNodes);
				foreach (ConfigNode node_vessel in node_vessels.GetNodes("Vessel"))
				{
					Guid id = node_vessel.parse("guid", new Guid());

					if (id == new Guid())
					{
						SCANUtil.SCANlog("Something Went Wrong Loading This SCAN Vessel; Moving On To The Next");
						continue;
					}

					foreach (ConfigNode node_sensor in node_vessel.GetNodes("Sensor"))
					{
						int sensor = node_sensor.parse("type", (int)0);
						double fov = node_sensor.parse("fov", 3d);
						double min_alt = node_sensor.parse("min_alt", (double)minScanAlt);
						double max_alt = node_sensor.parse("max_alt", (double)maxScanAlt);
						double best_alt = node_sensor.parse("best_alt", (double)bestScanAlt);

						registerSensorTemp(id, (SCANtype)sensor, fov, min_alt, max_alt, best_alt);

						tempIDs.Add(id);
					}
				}
			}

			ConfigNode node_progress = node.GetNode("Progress");
			if (node_progress != null)
			{
				foreach (ConfigNode node_body in node_progress.GetNodes("Body"))
				{
					string body_name = node_body.parse("Name", "");

					if (string.IsNullOrEmpty(body_name))
					{
						SCANUtil.SCANlog("SCANsat Controller: Error while loading Celestial Body data; skipping value...");
						continue;
					}

					SCANUtil.SCANlog("SCANsat Controller: Loading map for {0}", body_name);

					CelestialBody body;
					try
					{
						body = FlightGlobals.Bodies.FirstOrDefault(b => b.name == body_name);
					}
					catch (Exception e)
					{
						Debug.LogError(string.Format("[SCANsat] Error in loading Celestial Body [{0}]...\n{1}", body_name, e));
						continue;
					}

					if (body != null)
					{
						SCANdata data = getData(body.name);
						if (data == null)
							data = new SCANdata(body);
						if (!body_data.Contains(body_name))
							body_data.Add(body_name, data);
						else
							body_data[body_name] = data;

						try
						{
							string mapdata = node_body.parse("Map", "");
							
							if (string.IsNullOrEmpty(mapdata))
							{
								SCANUtil.SCANlog("SCANsat Controller: Error while loading Celestial Body map data; skipping value...");
								continue;
							}
							
							data.integerDeserialize(mapdata);
						}
						catch (Exception e)
						{
							SCANUtil.SCANlog("Something Went Wrong Loading Scanning Data; Resetting Coverage: {0}", e);
							data.reset();
							// fail somewhat gracefully; don't make the save unloadable 
						}

						try
						{
							if (SCANmainMenuLoader.MechJebLoaded && SCAN_Settings_Config.Instance.MechJebTarget && SCAN_Settings_Config.Instance.MechJebTargetLoad)
							{
								string targetName = node_body.parse("LandingTarget", "");

								if (!string.IsNullOrEmpty(targetName))
									loadWaypoint(targetName, body);
							}

							data.Disabled = node_body.parse("Disabled", false);

							float min = node_body.parse("MinHeightRange", data.TerrainConfig.DefaultMinHeight);
							float max = node_body.parse("MaxHeightRange", data.TerrainConfig.DefaultMaxHeight);
							float? clampState = node_body.parse("ClampHeight", (float?)null);

							int pSize = node_body.parse("PaletteSize", data.TerrainConfig.DefaultPaletteSize);
							bool pRev = node_body.parse("PaletteReverse", data.TerrainConfig.DefaultReverse);
							bool pDis = node_body.parse("PaletteDiscrete", data.TerrainConfig.DefaultDiscrete);

							string paletteName = node_body.parse("PaletteName", "");

							if (string.IsNullOrEmpty(paletteName))
								paletteName = data.TerrainConfig.DefaultPalette.name;

							Palette dataPalette = SCANUtil.paletteLoader(paletteName, pSize);

							if (dataPalette.hash == PaletteLoader.defaultPalette.hash)
							{
								paletteName = "Default";
								pSize = 7;
							}

							SCANterrainConfig dataTerrainConfig = getTerrainNode(body.name);

							if (dataTerrainConfig == null)
								dataTerrainConfig = new SCANterrainConfig(min, max, clampState, dataPalette, pSize, pRev, pDis, body);
							else
								setNewTerrainConfigValues(dataTerrainConfig, min, max, clampState, dataPalette, pSize, pRev, pDis);

							data.TerrainConfig = dataTerrainConfig;
						}
						catch (Exception e)
						{
							SCANUtil.SCANlog("Error Loading SCANdata; Reverting To Default Settings: {0}", e);
						}
					}
				}
			}

			ConfigNode node_resources = node.GetNode("SCANResources");
			if (node_resources != null)
			{
				foreach(ConfigNode node_resource_type in node_resources.GetNodes("ResourceType"))
				{
					if (node_resource_type != null)
					{
						loadCustomResourceValues(node_resource_type);
					}
				}
			}
			loaded = true;
		}

		public override void OnSave(ConfigNode node)
		{
			int l = knownVessels.Count;
			ConfigNode node_vessels = new ConfigNode("Scanners");

			for (int i = 0; i < l; i++)
			{
				SCANvessel sv = knownVessels.At(i);
				ConfigNode node_vessel = new ConfigNode("Vessel");
				node_vessel.AddValue("guid", sv.id.ToString());
				if (sv.vessel != null)
					node_vessel.AddValue("name", sv.vessel.vesselName);
				foreach (SCANsensor sensor in sv.sensors.Values)
				{
					ConfigNode node_sensor = new ConfigNode("Sensor");
					node_sensor.AddValue("type", (int)sensor.sensor);
					node_sensor.AddValue("fov", sensor.fov);
					node_sensor.AddValue("min_alt", sensor.min_alt);
					node_sensor.AddValue("max_alt", sensor.max_alt);
					node_sensor.AddValue("best_alt", sensor.best_alt);
					node_vessel.AddNode(node_sensor);
				}
				node_vessels.AddNode(node_vessel);
			}
			node.AddNode(node_vessels);
			if (body_data != null)
			{
				ConfigNode node_progress = new ConfigNode("Progress");
				foreach (string body_name in body_data.Keys)
				{
					ConfigNode node_body = new ConfigNode("Body");
					SCANdata body_scan = body_data[body_name];
					node_body.AddValue("Name", body_name);
					node_body.AddValue("Disabled", body_scan.Disabled);
					if (SCANmainMenuLoader.MechJebLoaded && SCAN_Settings_Config.Instance.MechJebTarget && SCAN_Settings_Config.Instance.MechJebTargetLoad)
					{
						SCANwaypoint w = body_scan.Waypoints.FirstOrDefault(a => a.LandingTarget);
						if (w != null)
							node_body.AddValue("LandingTarget", string.Format("{0:N4},{1:N4}", w.Latitude, w.Longitude));
					}
					node_body.AddValue("MinHeightRange", body_scan.TerrainConfig.MinTerrain / body_scan.TerrainConfig.MinHeightMultiplier);
					node_body.AddValue("MaxHeightRange", body_scan.TerrainConfig.MaxTerrain / body_scan.TerrainConfig.MaxHeightMultiplier);
					if (body_scan.TerrainConfig.ClampTerrain != null)
						node_body.AddValue("ClampHeight", body_scan.TerrainConfig.ClampTerrain / body_scan.TerrainConfig.ClampHeightMultiplier);
					node_body.AddValue("PaletteName", body_scan.TerrainConfig.ColorPal.name);
					node_body.AddValue("PaletteSize", body_scan.TerrainConfig.PalSize);
					node_body.AddValue("PaletteReverse", body_scan.TerrainConfig.PalRev);
					node_body.AddValue("PaletteDiscrete", body_scan.TerrainConfig.PalDis);
					node_body.AddValue("Map", body_scan.integerSerialize());
					node_progress.AddNode(node_body);
				}
				node.AddNode(node_progress);
			}
			if (resourceTypes.Count > 0 && masterResourceNodes.Count > 0)
			{
				ConfigNode node_resources = new ConfigNode("SCANResources");
				foreach (SCANresourceGlobal r in masterResourceNodes.Values)
				{
					if (r != null)
					{
						ConfigNode node_resource_type = new ConfigNode("ResourceType");
						node_resource_type.AddValue("Resource", r.Name);
						node_resource_type.AddValue("MinColor", ConfigNode.WriteColor(r.MinColor));
						node_resource_type.AddValue("MaxColor", ConfigNode.WriteColor(r.MaxColor));
						node_resource_type.AddValue("Transparency", r.Transparency);

						string rMinMax = saveResources(r);
						node_resource_type.AddValue("MinMaxValues", rMinMax);
						node_resources.AddNode(node_resource_type);
					}
				}
				node.AddNode(node_resources);
			}
		}

		#endregion

		public override void OnAwake()
		{
			instance = this;

			if (SCAN_Settings_Config.Instance == null)
				return;

			lowBiomeColor32 = SCAN_Settings_Config.Instance.LowBiomeColor;
			highBiomeColor32 = SCAN_Settings_Config.Instance.HighBiomeColor;
			lowSlopeColorOne32 = SCAN_Settings_Config.Instance.BottomLowSlopeColor;
			highSlopeColorOne32 = SCAN_Settings_Config.Instance.BottomHighSlopeColor;
			lowSlopeColorTwo32 = SCAN_Settings_Config.Instance.TopLowSlopeColor;
			highSlopeColorTwo32 = SCAN_Settings_Config.Instance.TopHighSlopeColor;
		}

		private void Start()
		{
			for (int i = tempIDs.Count - 1; i >= 0; i--)
			{
				Guid id = tempIDs[i];

				finishRegistration(id);
			}

			GameEvents.OnScienceRecieved.Add(watcher);
			GameEvents.OnOrbitalSurveyCompleted.Add(onSurvey);
			GameEvents.onVesselSOIChanged.Add(SOIChange);
			GameEvents.onVesselCreate.Add(newVesselCheck);
			GameEvents.onPartCouple.Add(dockingEventCheck);
			GameEvents.Contract.onContractsLoaded.Add(contractsCheck);
			GameEvents.Contract.onParameterChange.Add(onParamChange);

			if (HighLogic.LoadedSceneIsFlight)
			{
				if (!body_data.Contains(FlightGlobals.currentMainBody.name))
					body_data.Add(FlightGlobals.currentMainBody.name, new SCANdata(FlightGlobals.currentMainBody));
				try
				{
					_mainMap = new SCAN_UI_MainMap();
					_bigMap = new SCAN_UI_BigMap();
					_zoomMap = new SCAN_UI_ZoomMap();
					_instruments = new SCAN_UI_Instruments();
					_overlay = new SCAN_UI_Overlay();
					_settings = new SCAN_UI_Settings();
				}
				catch (Exception e)
				{
					SCANUtil.SCANlog("Something Went Wrong Initializing UI Objects:\n{0}", e);
				}

				StartCoroutine(WaitForScienceUpdate());
			}
			else if (HighLogic.LoadedSceneHasPlanetarium)
			{
				if (!body_data.Contains(Planetarium.fetch.Home.name))
					body_data.Add(Planetarium.fetch.Home.name, new SCANdata(Planetarium.fetch.Home));
				try
				{
					_bigMap = new SCAN_UI_BigMap();
					_settings = new SCAN_UI_Settings();
					_zoomMap = new SCAN_UI_ZoomMap();

					if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)
					{
						_overlay = new SCAN_UI_Overlay();
					}
				}
				catch (Exception e)
				{
					SCANUtil.SCANlog("Something Went Wrong Initializing UI Objects:\n{0}", e);
				}
			}
			if (SCAN_Settings_Config.Instance.StockToolbar)
				appLauncher = gameObject.AddComponent<SCANappLauncher>();

			if (SCAN_Settings_Config.Instance.DisableStockResource && SCAN_Settings_Config.Instance.UseStockTreshold)
			{
				for (int i = FlightGlobals.Bodies.Count - 1; i >= 0; i--)
				{					
					CelestialBody b = FlightGlobals.Bodies[i];

					checkResourceScanStatus(b);
				}
			}
			else if (!SCAN_Settings_Config.Instance.DisableStockResource && SCAN_Settings_Config.Instance.InstantScan)
			{
				for (int i = FlightGlobals.Bodies.Count - 1; i >= 0; i--)
				{
					CelestialBody b = FlightGlobals.Bodies[i];

					checkStockResourceScanStatus(b);
				}
			}
		}

		private void Update()
		{
			if (SCAN_Settings_Config.Instance.BackgroundScanning && loaded)
				scanFromAllVessels();

			if (!heightMapsBuilt)
				checkHeightMapStatus();
		}

		private IEnumerator WaitForScienceUpdate()
		{
			while (!FlightGlobals.ready || FlightGlobals.ActiveVessel == null)
				yield return null;

			SCANUtil.UpdateAllVesselData(FlightGlobals.ActiveVessel);
		}

		public void checkStockResourceScanStatus(CelestialBody body)
		{
			if (SCAN_Settings_Config.Instance.DisableStockResource || !SCAN_Settings_Config.Instance.InstantScan)
				return;

			if (body == null)
				return;

			if (!ResourceMap.Instance.IsPlanetScanned(body.flightGlobalsIndex))
				return;

			if (SCANUtil.GetCoverage((int)SCANtype.AllResources, body) >= 100)
				return;

			SCANdata data = getData(body.name);

			if (data == null)
				return;

			data.fillResourceMap();
		}

		public void checkResourceScanStatus(CelestialBody body)
		{
			if (!SCAN_Settings_Config.Instance.UseStockTreshold)
				return;

			if (body == null)
				return;

			if (ResourceMap.Instance.IsPlanetScanned(body.flightGlobalsIndex))
				return;

			SCANdata data = getData(body.name);

			if (data == null)
				return;

			if (SCANUtil.getCoveragePercentage(data, SCANtype.FuzzyResources) > (SCAN_Settings_Config.Instance.StockTreshold * 100))
			{
				SCANUtil.SCANlog("SCANsat resource scanning for {0} meets threshold value [{1:P0}]\nConducting stock orbital resource scan...", body.displayName, SCAN_Settings_Config.Instance.StockTreshold);
				ResourceMap.Instance.UnlockPlanet(body.flightGlobalsIndex);
			}
		}

		private int dataStep, dataStart;
		private bool currentlyBuilding;
		private SCANdata buildingData;

		private void checkHeightMapStatus()
		{
			if (!currentlyBuilding)
			{
				for (int i = 0; i < body_data.Count; i++)
				{
					buildingData = getData(i);

					if (buildingData == null)
						continue;

					if (buildingData.Built)
						continue;

					if (buildingData.MapBuilding || buildingData.OverlayBuilding)
						continue;

					buildingData.ControllerBuilding = true;
					currentlyBuilding = true;

					return;
				}
			}
			else
			{
				if (buildingData == null)
				{
					currentlyBuilding = false;
					return;
				}

				if (buildingData.Built)
				{
					currentlyBuilding = false;
					buildingData.ControllerBuilding = false;
					return;
				}

				if (buildingData.ControllerBuilding)
				{
					buildingData.generateHeightMap(ref dataStep, ref dataStart, 120);
					return;
				}
			}

			SCANUtil.SCANlog("All Height Maps Generated");

			buildingData = null;
			heightMapsBuilt = true;
		}

		private void OnDestroy()
		{
			GameEvents.OnScienceRecieved.Remove(watcher);
			GameEvents.OnOrbitalSurveyCompleted.Remove(onSurvey);
			GameEvents.onVesselSOIChanged.Remove(SOIChange);
			GameEvents.onVesselCreate.Remove(newVesselCheck);
			GameEvents.onPartCouple.Remove(dockingEventCheck);
			GameEvents.Contract.onContractsLoaded.Remove(contractsCheck);
			GameEvents.Contract.onParameterChange.Remove(onParamChange);

			if (appLauncher != null)
				Destroy(appLauncher);

			if (_mainMap != null)
			{
				_mainMap.OnDestroy();
				_mainMap = null;
			}
			if (_bigMap != null)
			{
				_bigMap.OnDestroy();
				_bigMap = null;
			}
			if (_instruments != null)
			{
				_instruments.OnDestroy();
				_instruments = null;
			}
			if (_overlay != null)
			{
				_overlay.OnDestroy();
				_overlay = null;
			}
			if (_settings != null)
			{
				_settings.OnDestroy();
				_settings = null;
			}
			if (_zoomMap != null)
			{
				_zoomMap.OnDestroy();
				_settings = null;
			}

			if (SCAN_Settings_Config.Instance != null)
				SCAN_Settings_Config.Instance.Save();

			if (!heightMapsBuilt)
			{
				for (int i = dataBodies.Count - 1; i >= 0; i--)
				{
					CelestialBody b = dataBodies[i];

					unloadPQS(b);
				}
			}
		}

		private void watcher(float sci, ScienceSubject sub, ProtoVessel v, bool b)
		{
			if (!HighLogic.LoadedSceneIsFlight)
				return;

			if (FlightGlobals.ActiveVessel == null)
				return;

			if (sub == null)
				return;

			if (!sub.id.StartsWith("SCAN"))
				return;

			SCANUtil.UpdateVesselData(FlightGlobals.ActiveVessel, sub);
		}

		private void onSurvey(Vessel v, CelestialBody b)
		{
			if (!SCAN_Settings_Config.Instance.InstantScan || SCAN_Settings_Config.Instance.DisableStockResource)
				return;

			if (b == null)
				return;

			if (SCANUtil.GetCoverage((int)SCANtype.AllResources, b) >= 100)
				return;

			SCANdata data = SCANUtil.getData(b);

			if (data == null)
			{
				data = new SCANdata(b);
				addToBodyData(b, data);
			}

			data.fillResourceMap();
		}

		internal void loadPQS(CelestialBody b, mapSource s = mapSource.Data)
		{
			if (!SCANmainMenuLoader.KopernicusLoaded)
				return;

			if (b == null)
				return;

			switch (s)
			{
				case mapSource.Data:
					if (dataBodies.Contains(b))
						return;

					dataBodies.Add(b);

					if (bigMapBodyPQS != null && bigMapBodyPQS == b)
						return;

					if (zoomMapBody != null && zoomMapBody == b)
						return;
					break;
				case mapSource.BigMap:
					if (bigMapBodyPQS != null && bigMapBodyPQS == b)
						return;

					bigMapBodyPQS = b;

					if (zoomMapBody != null && zoomMapBody == b)
						return;

					if (dataBodies.Contains(b))
						return;
					break;

				case mapSource.ZoomMap:
					if (zoomMapBody != null && zoomMapBody == b)
						return;

					zoomMapBody = b;

					if (bigMapBodyPQS != null && bigMapBodyPQS == b)
						return;

					if (dataBodies.Contains(b))
						return;
					break;
				case mapSource.RPM:
					return;
			}

			KopernicusOnDemand = b.GetComponentsInChildren<PQSMod>(true).Where(p => p.GetType().Name == "PQSMod_OnDemandHandler").FirstOrDefault();

			if (KopernicusOnDemand == null)
				return;

			KopernicusOnDemand.OnQuadPreBuild(null);

			KopernicusOnDemand = null;

			SCANUtil.SCANlog("Loading Kopernicus On Demand PQSMod For {0}", b.displayName);
		}

		internal void unloadPQS(CelestialBody b, mapSource s = mapSource.Data)
		{
			if (!SCANmainMenuLoader.KopernicusLoaded)
				return;

			if (b == null)
				return;

			switch (s)
			{
				case mapSource.Data:
					if (dataBodies.Contains(b))
						dataBodies.RemoveAll(a => a == b);

					if (bigMapBodyPQS != null && bigMapBodyPQS == b)
						return;

					if (zoomMapBody != null && zoomMapBody == b)
						return;
					break;
				case mapSource.BigMap:
					bigMapBodyPQS = null;

					if (zoomMapBody != null && zoomMapBody == b)
						return;

					if (dataBodies.Contains(b))
						return;
					break;

				case mapSource.ZoomMap:
					zoomMapBody = null;

					if (bigMapBodyPQS != null && bigMapBodyPQS == b)
						return;

					if (dataBodies.Contains(b))
						return;
					break;
				case mapSource.RPM:
					return;
			}

			bool setInactive = false;

			switch(HighLogic.LoadedScene)
			{
				case GameScenes.SPACECENTER:
					if (b != Planetarium.fetch.Home)
						setInactive = true;
					break;
				case GameScenes.TRACKSTATION:
					setInactive = true;
					break;
				case GameScenes.FLIGHT:
					if (b != FlightGlobals.currentMainBody)
						setInactive = true;
					break;
			}

			if (!setInactive)
				return;

			KopernicusOnDemand = b.GetComponentsInChildren<PQSMod>(true).Where(p => p.GetType().Name == "PQSMod_OnDemandHandler").FirstOrDefault();

			if (KopernicusOnDemand == null)
				return;

			KopernicusOnDemand.OnSphereInactive();

			KopernicusOnDemand = null;

			SCANUtil.SCANlog("Unloading Kopernicus On Demand PQSMod For {0}", b.displayName);
		}

		private void OnGUI()
		{
			if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedScene == GameScenes.TRACKSTATION)
				drawTarget();
		}

		private void drawTarget()
		{
			if (!MapView.MapIsEnabled)
				return;

			CelestialBody b = SCANUtil.getTargetBody(MapView.MapCamera.target);

			if (b == null)
				return;

			SCANdata d = getData(b.name);

			if (d == null)
				return;

			if (SCAN_Settings_Config.Instance.ShowGroundTracks && HighLogic.LoadedSceneIsFlight && !d.Disabled && SCAN_Settings_Config.Instance.BackgroundScanning)
				drawGroundTracks(b);

			return;
		}

		private void drawGroundTracks(CelestialBody body)
		{
			if (SCAN_Settings_Config.Instance.GroundTracksActiveOnly)
			{
				Vessel v = FlightGlobals.ActiveVessel;

				if (v.mainBody != body)
					return;

				if (v.situation == Vessel.Situations.LANDED || v.situation == Vessel.Situations.PRELAUNCH || v.situation == Vessel.Situations.SPLASHED)
					return;

				if (!isVesselKnown(v))
					return;

				SCANvessel sv = knownVessels[v.id];

				if (sv == null)
					return;

				Color col;

				double groundWidth = getFOV(sv, body, out col);

				if (groundWidth < 1)
					return;

				double surfaceScale = (2 * Math.PI * body.Radius) / 360;

				groundWidth *= surfaceScale;

				SCANuiUtil.drawGroundTrackTris(body, sv.vessel, groundWidth, col);
			}
			else
			{
				double surfaceScale = (2 * Math.PI * body.Radius) / 360;

				for (int j = knownVessels.Count - 1; j >= 0 ; j--)
				{			 
					SCANvessel sv = knownVessels.At(j);

					if (sv == null)
						continue;

					if (sv.vessel.mainBody != body)
						continue;

					Vessel.Situations sit = sv.vessel.loaded ? sv.vessel.situation : sv.vessel.protoVessel.situation;

					if (sit == Vessel.Situations.LANDED || sit == Vessel.Situations.PRELAUNCH || sit == Vessel.Situations.SPLASHED)
						continue;

					Color col;

					double groundWidth = getFOV(sv, body, out col);

					if (groundWidth < 1)
						continue;

					groundWidth *= surfaceScale;

					SCANuiUtil.drawGroundTrackTris(body, sv.vessel, groundWidth, col);
				}
			}
		}

		private double getFOV(SCANvessel v, CelestialBody b, out Color c)
		{
			c = palette.xkcd_DarkGreenAlpha;
			double maxFOV = 0;
			double alt = v.vessel.altitude;
			double soi_radius = b.sphereOfInfluence - b.Radius;
			double surfscale = Planetarium.fetch.Home.Radius / b.Radius;
			if (surfscale < 1)
				surfscale = 1;
			surfscale = Math.Sqrt(surfscale);

			for (int j = v.sensors.Count - 1; j >= 0; j--)
			{
				SCANsensor s = v.sensors.At(j);

				if (alt < s.min_alt)
					continue;
				if (alt > Math.Min(s.max_alt, soi_radius))
					continue;

				double fov = s.fov;
				double ba = Math.Min(s.best_alt, soi_radius);
				if (alt < ba)
				{
					fov = (alt / ba) * fov;
				}

				fov *= surfscale;
				if (fov > 20)
					fov = 20;

				if (fov > maxFOV)
					maxFOV = fov;
			}

			return maxFOV;
		}

		private void removeVessel(Vessel v)
		{
			if (isVesselKnown(v))
			{
				foreach (SCANtype t in Enum.GetValues(typeof(SCANtype)))
					unregisterSensor(v, t);
			}
		}

		private void addVessel(Vessel v)
		{
			foreach (SCANsat.SCAN_PartModules.SCANsat s in v.FindPartModulesImplementing<SCANsat.SCAN_PartModules.SCANsat>())
			{
				if (s.scanningNow)
					registerSensor(v.id, (SCANtype)s.sensorType, s.fov, s.min_alt, s.max_alt, s.best_alt);
			}
		}

		private void dockingEventCheck(GameEvents.FromToAction<Part, Part> Parts)
		{
			StartCoroutine(dockingCheckCoRoutine(Parts.to.vessel, Parts.from.vessel));
		}

		IEnumerator dockingCheckCoRoutine(Vessel to, Vessel from)
		{
			int timer = 0;

			while (timer < 45)
			{
				timer++;
				yield return null;
			}

			if (from != null)
			{
				removeVessel(from);
			}

			if (to != null)
			{
				removeVessel(to);
			}

			addVessel(FlightGlobals.ActiveVessel);
		}

		private void newVesselCheck(Vessel v)
		{
			if (v.loaded)
			{
				Vessel newVessel = null;

				if (v.Parts.Count > 1)
					newVessel = v;
				else
					newVessel = null;

				Vessel oldVessel = FlightGlobals.ActiveVessel;

				StartCoroutine(newVesselCoRoutine(newVessel, oldVessel));
			}
		}

		IEnumerator newVesselCoRoutine(Vessel newV, Vessel oldV)
		{
			int timer = 0;

			while (timer < 45)
			{
				timer++;
				yield return null;
			}

			if (newV != null)
			{
				removeVessel(newV);
				addVessel(newV);
			}

			if (oldV != null)
			{
				removeVessel(oldV);
				addVessel(oldV);
			}
		}

		private void contractsCheck()
		{
			contractsLoaded = true;
		}

		private void onParamChange(Contract c, ContractParameter p)
		{
			if (c.GetType() != typeof(SurveyContract))
				return;

			SurveyContract s = c as SurveyContract;

			CelestialBody b = s.targetBody;

			SCANdata data = getData(b.name);

			if (data == null)
				return;

			data.addSurveyWaypoints(b, s);

			if (_bigMap.IsVisible && _bigMap.WaypointToggle)
				_bigMap.RefreshIcons();
		}

		private void SOIChange(GameEvents.HostedFromToAction<Vessel, CelestialBody> VC)
		{
			if (!body_data.Contains(VC.to.name))
				body_data.Add(VC.to.name, new SCANdata(VC.to));
		}

		private void setNewTerrainConfigValues(SCANterrainConfig terrain, float min, float max, float? clamp, Palette c, int size, bool reverse, bool discrete)
		{
			terrain.MinTerrain = min * terrain.MinHeightMultiplier;
			terrain.MaxTerrain = max * terrain.MaxHeightMultiplier;
			terrain.ClampTerrain = clamp * terrain.ClampHeightMultiplier;
			terrain.ColorPal = c;
			terrain.PalSize = size;
			terrain.PalRev = reverse;
			terrain.PalDis = discrete;
		}

		private string saveResources(SCANresourceGlobal resource)
		{
			List<string> sL = new List<string>();
			for (int j = 0; j < resource.getBodyCount; j++)
			{
				SCANresourceBody bodyRes = resource.getBodyConfig(j);
				if (bodyRes != null)
				{
					string a = string.Format("{0}|{1:F3}|{2:F3}", bodyRes.Index, bodyRes.MinValue, bodyRes.MaxValue);
					sL.Add(a);
				}
			}

			return string.Join(",", sL.ToArray());
		}

		private void loadCustomResourceValues(ConfigNode node)
		{
			SCANresourceGlobal r;

			string resource = node.parse("Resource", "");

			if (string.IsNullOrEmpty(resource))
				return;

			if (masterResourceNodes.Contains(resource))
				r = masterResourceNodes[resource];
			else
				return;

			Color lowColor = node.parse("MinColor", r.DefaultLowColor);
			Color highColor = node.parse("MaxColor", r.DefaultHighColor);
			float transparent = node.parse("Transparency", r.DefaultTrans);

			r.MinColor = lowColor;
			r.MaxColor = highColor;
			r.Transparency = transparent;

			string s = node.parse("MinMaxValues", "");

			if (!string.IsNullOrEmpty(s))
			{
				string[] sA = s.Split(',');
				for (int i = 0; i < sA.Length; i++)
				{
					string[] sB = sA[i].Split('|');
					try
					{
						int j = 0;
						float min = 0;
						float max = 0;
						if (!int.TryParse(sB[0], out j))
							continue;

						CelestialBody b = null;

						try
						{
							b = FlightGlobals.Bodies.FirstOrDefault(a => a.flightGlobalsIndex == j);
						}
						catch (Exception e)
						{
							Debug.LogError("[SCANsat] Error in loading Celestial Body...\n" + e);
							return;
						}

						if (b != null)
						{
							SCANresourceBody res = r.getBodyConfig(b.name);
							if (res != null)
							{
								if (!float.TryParse(sB[1], out min))
									min = res.DefaultMinValue;
								if (!float.TryParse(sB[2], out max))
									max = res.DefaultMaxValue;
								res.MinValue = min;
								res.MaxValue = max;
							}
							else
								SCANUtil.SCANlog("No resources found assigned for Celestial Body: {0}, skipping...", b.name);
						}
						else
							SCANUtil.SCANlog("No Celestial Body found matching this saved resource value: {0}, skipping...", j);
					}
					catch (Exception e)
					{
						SCANUtil.SCANlog("Something Went Wrong While Loading Custom Resource Settings; Reverting To Default Values: {0}", e);
					}
				}
			}
		}

		private void loadWaypoint(string s, CelestialBody b)
		{
			if (!HighLogic.LoadedSceneIsFlight)
				return;

			StartCoroutine(WaitForWaypoint(s, b));
		}

		private IEnumerator WaitForWaypoint(string s, CelestialBody b)
		{
			while (!FlightGlobals.ready || FlightGlobals.ActiveVessel == null)
				yield return null;

			int timer = 0;

			while (timer < 5)
			{
				timer++;
				yield return null;
			}

			if (!mechjebLoaded || b != FlightGlobals.currentMainBody)
				yield break;

			SCANwaypoint w = null;
			string[] a = s.Split(',');
			double lat = 0;
			double lon = 0;

			if (!double.TryParse(a[0], out lat))
				yield break;
			if (!double.TryParse(a[1], out lon))
				yield break;

			w = new SCANwaypoint(lat, lon, "MechJeb Landing Target");

			MJTargetSet.Invoke(new Vector2d(lon, lat), b);

			SCANdata d = getData(b.bodyName);

			if (d != null)
				d.addToWaypoints(w);
		}

		public class SCANsensor
		{
			public SCANtype sensor;
			public double fov;
			public double min_alt, max_alt, best_alt;

			public bool inRange;
			public bool bestRange;

			public SCANsensor() { }

			public SCANsensor(SCANtype t, double f, double min, double max, double best)
			{
				sensor = t;
				fov = f;
				min_alt = min;
				max_alt = max;
				best_alt = best;
			}
		}

		public class SCANvessel
		{
			public Guid id;
			public Vessel vessel;
			public DictionaryValueList<SCANtype, SCANsensor> sensors = new DictionaryValueList<SCANtype, SCANsensor>();

			public CelestialBody body;
			public double latitude, longitude;
			public int frame;
			public double lastUT;
		}

		private void registerSensorTemp(Guid id, SCANtype sensors, double fov, double min_alt, double max_alt, double best_alt)
		{
			if (id == null)
				return;
			if (!knownVessels.Contains(id))
				knownVessels[id] = new SCANvessel();
			SCANvessel sv = knownVessels[id];
			sv.id = id;
			foreach (SCANtype sensor in Enum.GetValues(typeof(SCANtype)))
			{
				if (SCANUtil.countBits((int)sensor) != 1)
					continue;
				if ((sensor & sensors) == SCANtype.Nothing)
					continue;
				double this_fov = fov;
				double this_min_alt = min_alt;
				double this_max_alt = max_alt;
				double this_best_alt = best_alt;
				if (this_max_alt <= 0)
				{
					this_min_alt = 5000;
					this_max_alt = 500000;
					this_best_alt = 200000;
					this_fov = 5;
					if ((sensor & SCANtype.AltimetryHiRes) != SCANtype.Nothing) this_fov = 3;
					if ((sensor & SCANtype.AnomalyDetail) != SCANtype.Nothing)
					{
						this_min_alt = 0;
						this_max_alt = 2000;
						this_best_alt = 0;
						this_fov = 1;
					}
				}
				if (!sv.sensors.Contains(sensor))
					sv.sensors[sensor] = new SCANsensor(sensor, this_fov, this_min_alt, this_max_alt, this_best_alt);
				else
				{
					SCANsensor s = sv.sensors[sensor];
					s.sensor = sensor;
					if (this_fov > s.fov)
						s.fov = this_fov;
					if (this_min_alt < s.min_alt)
						s.min_alt = this_min_alt;
					if (this_max_alt > s.max_alt)
						s.max_alt = this_max_alt;
					s.best_alt = this_best_alt;
				}
			}
		}

		private void finishRegistration(Guid id)
		{
			if (!knownVessels.Contains(id))
				return;

			SCANvessel sv = knownVessels[id];

			try
			{
				sv.vessel = FlightGlobals.Vessels.FirstOrDefault(a => a.id == id);
			}
			catch (Exception e)
			{
				Debug.LogError("[SCANsat] Something went wrong while trying to load this SCANsat vessel; moving on the next vessel... \n" + e);
			}

			if (sv.vessel == null)
				knownVessels.Remove(id);
		}

		internal void registerSensor(Vessel v, SCANtype sensors, double fov, double min_alt, double max_alt, double best_alt)
		{
			registerSensor(v.id, sensors, fov, min_alt, max_alt, best_alt);
			knownVessels[v.id].vessel = v;
			knownVessels[v.id].latitude = SCANUtil.fixLatShift(v.latitude);
			knownVessels[v.id].longitude = SCANUtil.fixLonShift(v.longitude);
		}

		private void registerSensor(Guid id, SCANtype sensors, double fov, double min_alt, double max_alt, double best_alt)
		{
			if (id == null)
				return;
			if (!knownVessels.Contains(id))
				knownVessels[id] = new SCANvessel();
			SCANvessel sv = knownVessels[id];
			sv.id = id;
			try
			{
				sv.vessel = FlightGlobals.Vessels.FirstOrDefault(a => a.id == id);
			}
			catch (Exception e)
			{
				Debug.LogError("[SCANsat] Something went wrong while trying to load this SCANsat vessel; moving on the next vessel... \n" + e);
			}
			if (sv.vessel == null)
			{
				knownVessels.Remove(id);
				return;
			}
			foreach (SCANtype sensor in Enum.GetValues(typeof(SCANtype)))
			{
				if (SCANUtil.countBits((int)sensor) != 1)
					continue;
				if ((sensor & sensors) == SCANtype.Nothing)
					continue;
				double this_fov = fov;
				double this_min_alt = min_alt;
				double this_max_alt = max_alt;
				double this_best_alt = best_alt;
				if (this_max_alt <= 0)
				{
					this_min_alt = 5000;
					this_max_alt = 500000;
					this_best_alt = 200000;
					this_fov = 5;
					if ((sensor & SCANtype.AltimetryHiRes) != SCANtype.Nothing) this_fov = 3;
					if ((sensor & SCANtype.AnomalyDetail) != SCANtype.Nothing)
					{
						this_min_alt = 0;
						this_max_alt = 2000;
						this_best_alt = 0;
						this_fov = 1;
					}
				}
				if (!sv.sensors.Contains(sensor))
					sv.sensors[sensor] = new SCANsensor(sensor, this_fov, this_min_alt, this_max_alt, this_best_alt);
				else
				{
					SCANsensor s = sv.sensors[sensor];
					s.sensor = sensor;
					if (this_fov > s.fov)
						s.fov = this_fov;
					if (this_min_alt < s.min_alt)
						s.min_alt = this_min_alt;
					if (this_max_alt > s.max_alt)
						s.max_alt = this_max_alt;
					s.best_alt = this_best_alt;
				}
			}
		}

		internal void unregisterSensor(Vessel v, SCANtype sensors)
		{
			if (!knownVessels.Contains(v.id))
				return;

			SCANvessel sv = knownVessels[v.id];
			sv.id = v.id;
			sv.vessel = v;
			foreach (SCANtype sensor in Enum.GetValues(typeof(SCANtype)))
			{
				if ((sensors & sensor) == SCANtype.Nothing)
					continue;
				if (!sv.sensors.Contains(sensor))
					continue;

				sv.sensors.Remove(sensor);
			}
			if (sv.sensors.Count == 0)
			{
				knownVessels.Remove(v.id);
				SCANUtil.SCANdebugLog("Unregister Vessel");
			}
		}

		internal bool isVesselKnown(Guid id, SCANtype sensor)
		{
			if (!knownVessels.Contains(id))
				return false;

			SCANtype all = SCANtype.Nothing;

			SCANvessel sv = knownVessels[id];

			for (int i = sv.sensors.Count - 1; i >= 0; i--)
				all |= sv.sensors.At(i).sensor;

			return (all & sensor) != SCANtype.Nothing;
		}

		public bool isVesselKnown(Guid id)
		{
			if (!knownVessels.Contains(id))
				return false;

			return knownVessels[id].sensors.Count > 0;
		}

		public bool isVesselKnown(Vessel v)
		{
			if (v.vesselType == VesselType.Debris)
				return false;

			return isVesselKnown(v.id);
		}

		internal SCANsensor getSensorStatus(Vessel v, SCANtype sensor)
		{
			if (!knownVessels.Contains(v.id))
				return null;
			if (!knownVessels[v.id].sensors.Contains(sensor))
				return null;

			return knownVessels[v.id].sensors[sensor];
		}

		internal SCANtype activeSensorsOnVessel(Guid id)
		{
			if (!knownVessels.Contains(id))
				return SCANtype.Nothing;

			SCANtype sensors = SCANtype.Nothing;

			SCANvessel sv = knownVessels[id];

			for (int i = sv.sensors.Count - 1; i >= 0; i--)
				sensors |= sv.sensors.At(i).sensor;

			return sensors;
		}

		private int i = 0;
		private static int last_scan_frame;
		private static float last_scan_time;
		private static double scan_UT;
		private int activeSensors, activeVessels;
		private static int currentActiveSensor, currentActiveVessel;
		private void scanFromAllVessels()
		{
			if (Time.realtimeSinceStartup - last_scan_time < 1 && Time.realtimeSinceStartup > last_scan_time)
				return;
			if (last_scan_frame == Time.frameCount)
				return;

			last_scan_frame = Time.frameCount;
			last_scan_time = Time.realtimeSinceStartup;
			scan_UT = Planetarium.GetUniversalTime();
			currentActiveSensor = 0;
			currentActiveVessel = 0;
			actualPasses = 0;
			maxRes = 0;
			if (body_data.Count > 0)
			{
				body_data.At(i).updateCoverage();     //SCANUtil.getData(FlightGlobals.Bodies[i]); //Update coverage for planets one at a time, rather than all together
				i++;
				if (i >= body_data.Count) i = 0;
			}
			int l = knownVessels.Count;
			for (int j = 0; j < l; j++)
			{
				SCANvessel vessel = knownVessels.At(j);
				Vessel v = vessel.vessel;
				SCANdata data = SCANUtil.getData(v.mainBody);
				if (data == null)
					continue;

				if (!data.Disabled)
				{
					if (isVesselKnown(v))
					{
						doScanPass(vessel, scan_UT, scan_UT, vessel.lastUT, vessel.latitude, vessel.longitude);
						++currentActiveVessel;
						currentActiveSensor += knownVessels[v.id].sensors.Count;
					}
				}

				vessel.body = v.mainBody;
				vessel.frame = Time.frameCount;
				vessel.lastUT = scan_UT;
				vessel.latitude = SCANUtil.fixLatShift(v.latitude);
				vessel.longitude = SCANUtil.fixLonShift(v.longitude);
			}
			activeVessels = currentActiveVessel;
			activeSensors = currentActiveSensor;
		}

		private int actualPasses, maxRes;
		private static Queue<double> scanQueue;
		private void doScanPass(SCANvessel vessel, double UT, double startUT, double lastUT, double llat, double llon)
		{
			Vessel v = vessel.vessel;
			SCANdata data = SCANUtil.getData(v.mainBody);
			if (data == null)
				return;

			double soi_radius = v.mainBody.sphereOfInfluence - v.mainBody.Radius;
			double alt = v.altitude, lat = SCANUtil.fixLatShift(v.latitude), lon = SCANUtil.fixLonShift(v.longitude);
			double res = 0;
			Orbit o = v.orbit;
			bool uncovered;

			if (scanQueue == null)
				scanQueue = new Queue<double>();

			if (scanQueue.Count != 0)
				scanQueue.Clear();

		loop: // don't look at me like that, I just unrolled the recursion
			if (res > 0)
			{
				if (double.IsNaN(UT))
					goto dequeue;

				//if (o.ObT <= 0)
				//{
				//	SCANUtil.SCANlog("Dequeue {0}", 1);
				//	goto dequeue;
				//}

				if (double.IsNaN(o.getObtAtUT(UT)))
					goto dequeue;

				Vector3d pos = o.getPositionAtUT(UT);
				double rotation = 0;

				if (v.mainBody.rotates)
					rotation = (360 * ((UT - scan_UT) / v.mainBody.rotationPeriod)) % 360;

				alt = v.mainBody.GetAltitude(pos);
				lat = SCANUtil.fixLatShift(v.mainBody.GetLatitude(pos));
				lon = SCANUtil.fixLonShift(v.mainBody.GetLongitude(pos) - rotation);

				if (alt < 0)
					alt = 0;

				if (res > maxRes)
					maxRes = (int)res;
			}
			else
			{
				alt = v.radarAltitude;
				if (alt < 0)
					alt = v.altitude;
			}

			if (Math.Abs(lat - llat) < 1 && Math.Abs(lon - llon) < 1 && res > 0)
				goto dequeue;
			
			actualPasses++;

			uncovered = res <= 0;

			for (int j = vessel.sensors.Count - 1; j >= 0; j--)
			{
				SCANsensor sensor = vessel.sensors.At(j);

				if (res <= 0)
				{
					if (data.getCoverage(sensor.sensor) > 0)
						uncovered = false;
				}

				sensor.inRange = false;
				sensor.bestRange = false;

				if (alt < sensor.min_alt)
					continue;

				if (alt > Math.Min(sensor.max_alt, soi_radius))
					continue;

				sensor.inRange = true;

				double fov = sensor.fov;
				double ba = Math.Min(sensor.best_alt, soi_radius);

				if (alt < ba)
					fov = (alt / ba) * fov;
				else
					sensor.bestRange = true;

				double surfscale = Planetarium.fetch.Home.Radius / v.mainBody.Radius;

				if (surfscale < 1)
					surfscale = 1;

				surfscale = Math.Sqrt(surfscale);
				fov *= surfscale;

				if (fov > 20)
					fov = 20;

				int f = (int)Math.Truncate(fov);
				int f1 = f + (int)Math.Round(fov - f);

				int w = f;
				double fovW = fov;

				if (Math.Abs(lat) < 90)
				{
					fovW = fov * (1 / Math.Cos(Mathf.Deg2Rad * lat));

					if (fovW > 120)
						fovW = 120;

					w = (int)Math.Truncate(fovW);
				}

				int w1 = w + (int)Math.Round(fovW - w);

				for (int x = -w; x <= w1; ++x)
				{
					for (int y = -f; y <= f1; ++y)
					{
						double clampLon = lon + x;
						double clampLat = lat + y;
						if (clampLat > 89)
						{
							clampLat = 179 - clampLat;
							clampLon += 180;
						}
						if (clampLat < -90)
						{
							clampLat = -180 - clampLat;
							clampLon += 180;
						}

						SCANUtil.registerPass(clampLon, clampLat, data, sensor.sensor);
					}
				}
			}
			if (uncovered)
				return;
			/* 
			if(v.mainBody == FlightGlobals.currentMainBody) {
				if(res > 0) data.map_small.SetPixel((int)Math.Round(lon) + 180, (int)Math.Round(lat) + 90, palette.magenta);
				else data.map_small.SetPixel((int)Math.Round(lon) + 180, (int)Math.Round(lat) + 90, palette.yellow);
			}
			*/

			if (vessel.lastUT <= 0)
				return;
			if (vessel.frame <= 0)
				return;
			if (v.LandedOrSplashed)
				return;
			if (res >= SCAN_Settings_Config.Instance.TimeWarpResolution)
				goto dequeue;

			if (startUT > UT)
			{
				scanQueue.Enqueue((startUT + UT) / 2);
				scanQueue.Enqueue(startUT);
				scanQueue.Enqueue(UT);
				scanQueue.Enqueue(lat);
				scanQueue.Enqueue(lon);
				scanQueue.Enqueue(res + 1);
			}
			startUT = UT;
			UT = (lastUT + UT) / 2;
			llat = lat;
			llon = lon;
			res = res + 1;
			goto loop;

		dequeue:
			if (scanQueue.Count <= 0)
				return;
			UT = scanQueue.Dequeue();
			startUT = scanQueue.Dequeue();
			lastUT = scanQueue.Dequeue();
			llat = scanQueue.Dequeue();
			llon = scanQueue.Dequeue();
			res = scanQueue.Dequeue();
			goto loop;
		}

	}
}
