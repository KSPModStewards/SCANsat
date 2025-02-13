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
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using Contracts;
using FinePrint.Contracts;
using FinePrint.Utilities;
using SCANsat.SCAN_UI.UI_Framework;
using SCANsat.SCAN_Unity;
using SCANsat.SCAN_Data;
using SCANsat.SCAN_Map;
using SCANsat.SCAN_Platform.Extensions.ConfigNodes;
using SCANsat.SCAN_Palettes;
using SCANsat.SCAN_Toolbar;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANcolorUtil;
using FinePrint.Contracts.Parameters;
using UnityEngine.Profiling;

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
        public bool storageUpgraded = false;
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
        public int zoomMapRefresh = 0;
        [KSPField(isPersistant = true)]
        public bool zoomMapZoomPersist = false;
        [KSPField(isPersistant = true)]
        public float zoomMapZoom = 10f;
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
        private CelestialBody zoomMapBodyPQS;
        private PQSMod KopernicusOnDemand;
        private MonoBehaviour kopernicusScaledSpaceLoader;
        private CelestialBody bigMapBodyVisual;
        private CelestialBody zoomMapBodyVisual;

        /* Visual Map Texture Data */
        private Dictionary<CelestialBody, Texture2D> readableScaledSpaceMaps = new Dictionary<CelestialBody, Texture2D>();
        private Dictionary<CelestialBody, Texture2D> readableScaledSpaceNormalMaps = new Dictionary<CelestialBody, Texture2D>();
        private CelestialBody bigMapBodyScaledSpace;
        private CelestialBody zoomMapBodyScaledSpace;

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

        /* Is the Breaking Ground Expansion installed */
        private bool serenityLoaded = false;

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
        public void addToBodyData(CelestialBody b, SCANdata data)
        {
            if (!body_data.Contains(b.bodyName))
                body_data.Add(b.bodyName, data);
            else
                UnityEngine.Debug.LogError("[SCANsat] Warning: SCANdata Dictionary Already Contains Key of This Type");
        }

        public Texture2D getVisualMapTexture(CelestialBody b)
        {
            if (!SCAN_Settings_Config.Instance.VisibleMapsActive)
                return null;

            if (readableScaledSpaceMaps.ContainsKey(b))
                return readableScaledSpaceMaps[b];

            return null;
        }

        public Texture2D getVisualMapNormalTexture(CelestialBody b)
        {
            if (readableScaledSpaceNormalMaps.ContainsKey(b))
                return readableScaledSpaceNormalMaps[b];

            return null;
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

        public static void setMasterTerrainNodes(List<SCANterrainConfig> terrainConfigs)
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

                if (getTerrainNode(b.bodyName) == null)
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
                        SCANUtil.SCANlog("Error in calculating Max Height for {0}; using default value/n{1}", b.bodyName, e);
                        newMax = SCANconfigLoader.SCANNode.DefaultMaxHeightRange;
                    }

                    SCANUtil.SCANlog("Generating new SCANsat Terrain Config for [{0}] - Max Height: [{1:F0}m]", b.bodyName, newMax);

                    addToTerrainConfigData(b.bodyName, new SCANterrainConfig(SCANconfigLoader.SCANNode.DefaultMinHeightRange, newMax, clamp, SCANUtil.PaletteLoader(SCANconfigLoader.SCANNode.DefaultPalette, 7), 7, false, false, b));
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

        public static void updateTerrainConfig(SCANterrainConfig t)
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

        public static void addToTerrainConfigData(string name, SCANterrainConfig data)
        {
            if (!masterTerrainNodes.ContainsKey(name))
                masterTerrainNodes.Add(name, data);
            else
                UnityEngine.Debug.LogError("[SCANsat] Warning: SCANterrain Data Dictionary Already Contains Key Of This Type");
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

        public static void setMasterResourceNodes(List<SCANresourceGlobal> resourceConfigs)
        {
            masterResourceNodes.Clear();

            try
            {
                int l = resourceConfigs.Count;

                for (int i = 0; i < l; i++)
                {
                    SCANresourceGlobal r = resourceConfigs[i];

                    if (r == null || string.IsNullOrEmpty(r.Name))
                        continue;
                    //SCANUtil.SCANlog("Loading SCANsat resource config settings: {0}", r.Name);
                    if (!masterResourceNodes.Contains(r.Name))
                        masterResourceNodes.Add(r.Name, r);
                }
            }
            catch (Exception e)
            {
                SCANUtil.SCANlog("Error while loading SCANsat resource config settings: {0}", e);
            }
        }

        public static SCANresourceGlobal getResourceNode(string resourceName, bool warn = false)
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

        public static void updateSCANresource(SCANresourceGlobal r, bool all)
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

        public static void addToResourceData(string name, SCANresourceGlobal res)
        {
            if (!masterResourceNodes.Contains(name))
            {
                masterResourceNodes.Add(name, res);
            }
            else
                UnityEngine.Debug.LogError(string.Format("[SCANsat] Warning: SCANResource Dictionary Already Contains Key of This Type: Resource: {0}", name));
        }

        public static void addToLoadedResourceNames(string name, bool warn = true)
        {
            if (!loadedResources.Contains(name))
                loadedResources.Add(name);
            else if (warn)
                UnityEngine.Debug.LogError(string.Format("[SCANsat] Warning: Loaded Resource List Already Contains Resource Of Name: {0}", name));
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

        public bool SerenityLoaded
        {
            get { return serenityLoaded; }
        }

        public class OnMJTargetSet : UnityEvent<Vector2d, CelestialBody> { }

        public OnMJTargetSet MJTargetSet = new OnMJTargetSet();

        #endregion

        #region save/load

        public override void OnLoad(ConfigNode node)
        {
            if (storageUpgraded)
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
                            bool require_light = node_sensor.parse("require_light", (bool)false);

                            registerSensorTemp(id, (SCANtype)sensor, fov, min_alt, max_alt, best_alt, require_light);

                            tempIDs.Add(id);
                        }
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
                        body = FlightGlobals.Bodies.FirstOrDefault(b => b.bodyName == body_name);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError(string.Format("[SCANsat] Error in loading Celestial Body [{0}]...\n{1}", body_name, e));
                        continue;
                    }

                    if (body != null)
                    {
                        SCANdata data = getData(body.bodyName);
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

                            if (storageUpgraded)
                            {
                                data.shortDeserialize(mapdata);
                            }
                            else
                            {
                                data.ConvertStorage(mapdata);
                            }

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
                                paletteName = data.TerrainConfig.DefaultPalette.Name;

                            SCANPalette dataPalette = SCANUtil.PaletteLoader(paletteName, pSize);

                            if (dataPalette.Hash == SCAN_Palette_Config.DefaultPalette.GetPalette(0).Hash)
                            {
                                paletteName = "Default";
                                pSize = 7;
                            }

                            SCANterrainConfig dataTerrainConfig = getTerrainNode(body.bodyName);

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
                foreach (ConfigNode node_resource_type in node_resources.GetNodes("ResourceType"))
                {
                    if (node_resource_type != null)
                    {
                        loadCustomResourceValues(node_resource_type);
                    }
                }
            }
            loaded = true;
            storageUpgraded = true;
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
                foreach (SCANsensor sensor in sv.sensors)
                {
                    ConfigNode node_sensor = new ConfigNode("Sensor");
                    node_sensor.AddValue("type", (int)sensor.sensor);
                    node_sensor.AddValue("fov", sensor.fov);
                    node_sensor.AddValue("min_alt", sensor.min_alt);
                    node_sensor.AddValue("max_alt", sensor.max_alt);
                    node_sensor.AddValue("best_alt", sensor.best_alt);
                    node_sensor.AddValue("require_light", sensor.requireLight);
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
                    node_body.AddValue("PaletteName", body_scan.TerrainConfig.ColorPal.Name);
                    node_body.AddValue("PaletteSize", body_scan.TerrainConfig.PalSize);
                    node_body.AddValue("PaletteReverse", body_scan.TerrainConfig.PalRev);
                    node_body.AddValue("PaletteDiscrete", body_scan.TerrainConfig.PalDis);
                    node_body.AddValue("Map", body_scan.shortSerialize());
                    node_progress.AddNode(node_body);
                }
                node.AddNode(node_progress);
            }
            if (masterResourceNodes.Count > 0)
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

            serenityLoaded = Expansions.ExpansionsLoader.IsExpansionInstalled("Serenity");

            if (SCAN_Settings_Config.Instance == null)
                return;

            SCANUtil.fillLocalizedNames();

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
                finishRegistration(tempIDs[i]);
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
                if (!body_data.Contains(FlightGlobals.currentMainBody.bodyName))
                    body_data.Add(FlightGlobals.currentMainBody.bodyName, new SCANdata(FlightGlobals.currentMainBody));
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
                if (!body_data.Contains(Planetarium.fetch.Home.bodyName))
                    body_data.Add(Planetarium.fetch.Home.bodyName, new SCANdata(Planetarium.fetch.Home));
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

            for (int i = body_data.Count - 1; i >= 0; i--)
            {
                body_data.At(i).updateCoverage();
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

            if (SCANUtil.GetCoverage((short)SCANtype.ResourceHiRes, body) >= 100)
                return;

            SCANdata data = getData(body.bodyName);

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

            SCANdata data = getData(body.bodyName);

            if (data == null)
                return;

            if (SCANUtil.getCoveragePercentage(data, SCANtype.ResourceLoRes) > (SCAN_Settings_Config.Instance.StockTreshold * 100) || SCANUtil.getCoveragePercentage(data, SCANtype.ResourceHiRes) > (SCAN_Settings_Config.Instance.StockTreshold * 100))
            {
                SCANUtil.SCANlog("SCANsat resource scanning for {0} meets threshold value [{1:P0}]\nConducting stock orbital resource scan...", body.bodyName, SCAN_Settings_Config.Instance.StockTreshold);
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

            if (SCANUtil.GetCoverage((short)SCANtype.ResourceHiRes, b) >= 100)
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

                    if (zoomMapBodyPQS != null && zoomMapBodyPQS == b)
                        return;
                    break;
                case mapSource.BigMap:
                    if (bigMapBodyPQS != null && bigMapBodyPQS == b)
                        return;

                    bigMapBodyPQS = b;

                    if (zoomMapBodyPQS != null && zoomMapBodyPQS == b)
                        return;

                    if (dataBodies.Contains(b))
                        return;
                    break;

                case mapSource.ZoomMap:
                    if (zoomMapBodyPQS != null && zoomMapBodyPQS == b)
                        return;

                    zoomMapBodyPQS = b;

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

            SCANUtil.SCANlog("Loading Kopernicus On Demand PQSMod For {0}", b.bodyName);
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

                    if (zoomMapBodyPQS != null && zoomMapBodyPQS == b)
                        return;
                    break;
                case mapSource.BigMap:
                    bigMapBodyPQS = null;

                    if (zoomMapBodyPQS != null && zoomMapBodyPQS == b)
                        return;

                    if (dataBodies.Contains(b))
                        return;
                    break;

                case mapSource.ZoomMap:
                    zoomMapBodyPQS = null;

                    if (bigMapBodyPQS != null && bigMapBodyPQS == b)
                        return;

                    if (dataBodies.Contains(b))
                        return;
                    break;
                case mapSource.RPM:
                    return;
            }

            bool setInactive = false;

            switch (HighLogic.LoadedScene)
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

            SCANUtil.SCANlog("Unloading Kopernicus On Demand PQSMod For {0}", b.bodyName);
        }

        internal void loadOnDemandScaledSpace(CelestialBody b, mapSource s)
        {
            if (!SCANmainMenuLoader.KopernicusLoaded)
                return;

            if (!SCAN_Settings_Config.Instance.VisibleMapsActive)
                return;

            if (b == null)
                return;

            if (b.scaledBody == null)
                return;

            switch(s)
            {
                case mapSource.BigMap:
                    if (bigMapBodyVisual != null && bigMapBodyVisual == b)
                        return;

                    bigMapBodyVisual = b;

                    if (zoomMapBodyVisual != null && zoomMapBodyVisual == b)
                        return;

                    break;
                case mapSource.ZoomMap:
                    if (zoomMapBodyVisual != null && zoomMapBodyVisual == b)
                        return;

                    zoomMapBodyVisual = b;

                    if (bigMapBodyVisual != null && bigMapBodyVisual == b)
                        return;
                    break;
            }

            kopernicusScaledSpaceLoader = b.scaledBody.GetComponents<MonoBehaviour>().Where(p => p.GetType().Name == SCANreflection.KOPERNICUSONDEMANDTYPE).FirstOrDefault();

            if (kopernicusScaledSpaceLoader == null)
                return;

            SCANreflection.LoadOnDemand(kopernicusScaledSpaceLoader);

            kopernicusScaledSpaceLoader = null;

            SCANUtil.SCANlog("Loading Kopernicus On Demand Scaled Space Map For {0}", b.bodyName);
        }

        internal void unloadOnDemandScaledSpace(CelestialBody b, mapSource s)
        {
            if (!SCANmainMenuLoader.KopernicusLoaded)
                return;

            if (!SCAN_Settings_Config.Instance.VisibleMapsActive)
                return;

            if (b == null)
                return;

            if (b.scaledBody == null)
                return;

            switch(s)
            {
                case mapSource.BigMap:
                    bigMapBodyVisual = null;

                    if (zoomMapBodyVisual != null && zoomMapBodyVisual == b)
                        return;
                    break;
                case mapSource.ZoomMap:
                    zoomMapBodyVisual = null;

                    if (bigMapBodyVisual != null && bigMapBodyVisual == b)
                        return;
                    break;
            }

            bool setInactive = false;

            switch (HighLogic.LoadedScene)
            {
                case GameScenes.SPACECENTER:
                    setInactive = true;
                    break;
                case GameScenes.TRACKSTATION:
                    setInactive = !InCurrentBodyFamily(b);
                    break;
                case GameScenes.FLIGHT:
                    setInactive = !InCurrentBodyFamily(b);
                    break;
            }

            if (!setInactive)
                return;

            kopernicusScaledSpaceLoader = b.scaledBody.GetComponents<MonoBehaviour>().Where(p => p.GetType().Name == SCANreflection.KOPERNICUSONDEMANDTYPE).FirstOrDefault();

            if (kopernicusScaledSpaceLoader == null)
                return;

            SCANreflection.UnloadOnDemand(kopernicusScaledSpaceLoader);

            kopernicusScaledSpaceLoader = null;

            SCANUtil.SCANlog("Unloading Kopernicus On Demand Scaled Space Map For {0}", b.bodyName);
        }

        private bool InCurrentBodyFamily(CelestialBody b)
        {
            switch (HighLogic.LoadedScene)
            {
                case GameScenes.TRACKSTATION:
                    MapObject mo = PlanetariumCamera.fetch.target;

                    CelestialBody tgt = null;

                    if (mo.vessel != null)
                    {
                        tgt = mo.vessel.mainBody;
                    }
                    else if (mo.celestialBody != null)
                    {
                        tgt = mo.celestialBody;
                    }

                    if (tgt == null)
                        return false;

                    if (b == tgt)
                        return true;

                    if (tgt.HasChild(b))
                        return true;

                    if (tgt.HasParent(b))
                        return true;

                    break;
                case GameScenes.FLIGHT:
                    if (b == FlightGlobals.currentMainBody)
                        return true;

                    if (FlightGlobals.currentMainBody.HasChild(b))
                        return true;

                    if (FlightGlobals.currentMainBody.HasParent(b))
                        return true;
                    break;
            }

            return false;
        }

        internal void LoadVisualMapTexture(CelestialBody b, mapSource s)
        {
            if (!SCAN_Settings_Config.Instance.VisibleMapsActive)
                return;

            if (b == null)
                return;

            if (b.scaledBody == null)
                return;

            MeshRenderer scaledMesh = b.scaledBody.GetComponent<MeshRenderer>();

            if (scaledMesh == null)
                return;

            if (!readableScaledSpaceMaps.ContainsKey(b) || readableScaledSpaceMaps[b] == null)
            {
                if (scaledMesh.sharedMaterial.shader.name == "Terrain/Gas Giant")
                {
                    if (scaledMesh.sharedMaterial.HasProperty("_DetailCloudPatternTexture"))
                        readableScaledSpaceMaps.Add(b, readableTexture(scaledMesh.sharedMaterial.GetTexture("_DetailCloudPatternTexture"), scaledMesh.sharedMaterial, true));
                }
                //else if (scaledMesh.sharedMaterial.shader.name == "Emissive Multi Ramp Sunspots")
                //{
                //    if (scaledMesh.sharedMaterial.HasProperty("_MainTex"))
                //        readableScaledSpaceMaps.Add(b, readableTexture(scaledMesh.sharedMaterial.GetTexture("_MainTex"), scaledMesh.sharedMaterial, true));
                //}
                else
                {
                    if (scaledMesh.sharedMaterial.HasProperty("_MainTex"))
                    {
                        readableScaledSpaceMaps.Add(b, readableTexture(scaledMesh.sharedMaterial.GetTexture("_MainTex"), scaledMesh.sharedMaterial, true));
                    }
                    else if (scaledMesh.sharedMaterial.HasProperty("_ColorMap"))
                    {
                        readableScaledSpaceMaps.Add(b, readableTexture(scaledMesh.sharedMaterial.GetTexture("_ColorMap"), scaledMesh.sharedMaterial, true));
                    }
                }
            }

            if (!readableScaledSpaceNormalMaps.ContainsKey(b) || readableScaledSpaceNormalMaps[b] == null)
            {
                if (scaledMesh.sharedMaterial.shader.name == "Terrain/Gas Giant")
                {
                    if (scaledMesh.sharedMaterial.HasProperty("_NormalMap"))
                        readableScaledSpaceNormalMaps.Add(b, readableTexture(scaledMesh.sharedMaterial.GetTexture("_NormalMap"), scaledMesh.sharedMaterial, false));
                }
                else
                {
                    if (scaledMesh.sharedMaterial.HasProperty("_BumpMap"))
                    {
                        readableScaledSpaceNormalMaps.Add(b, readableTexture(scaledMesh.sharedMaterial.GetTexture("_BumpMap"), scaledMesh.sharedMaterial, false));
                    }
                    else if (scaledMesh.sharedMaterial.HasProperty("_NormalMap"))
                    {
                        readableScaledSpaceNormalMaps.Add(b, readableTexture(scaledMesh.sharedMaterial.GetTexture("_NormalMap"), scaledMesh.sharedMaterial, false));
                    }
                }
            }

            switch (s)
            {
                case mapSource.BigMap:
                    bigMapBodyScaledSpace = b;
                    break;
                case mapSource.ZoomMap:
                    zoomMapBodyScaledSpace = b;
                    break;
            }
        }

        internal void UnloadVisualMapTexture(CelestialBody b, mapSource s)
        {
            if (!SCAN_Settings_Config.Instance.VisibleMapsActive)
                return;

            if (b == null)
                return;

            switch (s)
            {
                case mapSource.BigMap:
                    bigMapBodyScaledSpace = null;

                    if (zoomMapBodyScaledSpace != null && zoomMapBodyScaledSpace == b)
                        return;
                    break;
                case mapSource.ZoomMap:
                    zoomMapBodyScaledSpace = null;

                    if (bigMapBodyScaledSpace != null && bigMapBodyScaledSpace == b)
                        return;
                    break;
            }

            if (readableScaledSpaceMaps.ContainsKey(b))
            {
                GameObject.Destroy(readableScaledSpaceMaps[b]);
                readableScaledSpaceMaps[b] = null;
                readableScaledSpaceMaps.Remove(b);
            }

            if (readableScaledSpaceNormalMaps.ContainsKey(b))
            {
                GameObject.Destroy(readableScaledSpaceNormalMaps[b]);
                readableScaledSpaceNormalMaps[b] = null;
                readableScaledSpaceNormalMaps.Remove(b);
            }
        }

        private Texture2D readableTexture(Texture tex, Material mat, bool useMat)
        {
            if (tex == null)
                return null;

            Texture2D readable = new Texture2D(tex.width, tex.height);

            var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, 1);

            if (useMat)
                Graphics.Blit(tex, rt, mat);
            else
                Graphics.Blit(tex, rt);

            RenderTexture.active = rt;

            readable.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            rt = null;

            readable.Apply();

            tex = null;

            return readable;
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

            SCANdata d = getData(b.bodyName);

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

                //Color col;

                double groundWidth = getFOV(sv, body);//, out col);

                if (groundWidth < 1)
                    return;

                double surfaceScale = (2 * Math.PI * body.Radius) / 360;

                groundWidth *= surfaceScale;

                SCANuiUtil.drawGroundTrackTris(body, sv.vessel, groundWidth, sv.trackColor);
            }
            else
            {
                double surfaceScale = (2 * Math.PI * body.Radius) / 360;

                for (int j = knownVessels.Count - 1; j >= 0; j--)
                {
                    SCANvessel sv = knownVessels.At(j);

                    if (sv == null)
                        continue;

                    if (sv.vessel.mainBody != body)
                        continue;

                    Vessel.Situations sit = sv.vessel.loaded ? sv.vessel.situation : sv.vessel.protoVessel.situation;

                    if (sit == Vessel.Situations.LANDED || sit == Vessel.Situations.PRELAUNCH || sit == Vessel.Situations.SPLASHED)
                        continue;

                    //Color col;

                    double groundWidth = getFOV(sv, body);//, out col);

                    if (groundWidth < 1)
                        continue;

                    groundWidth *= surfaceScale;

                    SCANuiUtil.drawGroundTrackTris(body, sv.vessel, groundWidth, sv.trackColor);
                }
            }
        }

        private double getFOV(SCANvessel v, CelestialBody b)//, out Color c)
        {
            //c = palette.xkcd_DarkGreenAlpha;
            double maxFOV = 0;
            double alt = v.vessel.altitude;
            double soi_radius = b.sphereOfInfluence - b.Radius;
            double surfscale = Planetarium.fetch.Home.Radius / b.Radius;
            if (surfscale < 1)
                surfscale = 1;
            surfscale = Math.Sqrt(surfscale);

            for (int j = v.sensors.Count - 1; j >= 0; j--)
            {
                SCANsensor s = v.sensors[j];

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

        private Color32 getScanTypeColor(SCANtype s)
        {
            float r = 0;
            float g = 0;
            float b = 0;
            float a = 0;

            int count = 0;

            for (int i = 0; i < 31; i++)
            {
                if ((1 << i & (int)s) != 0)
                {
                    Color32 c = typeColor(i);

                    r += (c.r * c.r);
                    g += (c.g * c.g);
                    b += (c.b * c.b);
                    a += (c.a * c.a);

                    count++;
                }
            }

            r /= count;
            g /= count;
            b /= count;
            a /= count;

            Color32 col = new Color32((byte)Mathf.Sqrt(r), (byte)Mathf.Sqrt(g), (byte)Mathf.Sqrt(b), (byte)Mathf.Sqrt(a));

            return col;
        }

        private Color32 typeColor(int i)
        {
            switch(i)
            {
                case 0:
                    return SCAN_Settings_Config.Instance.LoResAltimetryTrackColor;
                case 1:
                    return SCAN_Settings_Config.Instance.HiResAltimetryTrackColor;
                case 2:
                    return SCAN_Settings_Config.Instance.LoResVisualTrackColor;
                case 3:
                    return SCAN_Settings_Config.Instance.BiomeTrackColor;
                case 4:
                    return SCAN_Settings_Config.Instance.AnomalyTrackColor;
                case 5:
                    return SCAN_Settings_Config.Instance.AnomalyDetailTrackColor;
                case 6:
                    return SCAN_Settings_Config.Instance.HiResVisualTrackColor;
                case 7:
                    return SCAN_Settings_Config.Instance.FuzzyResourceTrackColor;
                case 8:
                    return SCAN_Settings_Config.Instance.HiResResourceTrackColor;
                default:

                    return palette.XKCD_DarkGreenAlpha;
            }
        }

        private void removeVessel(Vessel v)
        {
            if (isVesselKnown(v))
            {
                unregisterVessel(v);
            }
        }

        private void addVessel(Vessel v)
        {
            foreach (SCANsat.SCAN_PartModules.SCANsat s in v.FindPartModulesImplementing<SCANsat.SCAN_PartModules.SCANsat>())
            {
                if (s.scanningNow)
                    registerSensor(v.id, (SCANtype)s.sensorType, s.fov, s.min_alt, s.max_alt, s.best_alt, s.requireLight);
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
            if (c.GetType() == typeof(SurveyContract))
            {
                SurveyContract s = c as SurveyContract;

                CelestialBody b = s.targetBody;

                SCANdata data = getData(b.bodyName);

                if (data == null)
                    return;

                data.addSurveyWaypoints(b, s);
            }
            else if (c.GetType() == typeof(SatelliteContract))
            {
                SatelliteContract s = c as SatelliteContract;

                SpecificOrbitParameter orbit = s.GetParameter<SpecificOrbitParameter>();

                if (orbit == null)
                    return;

                CelestialBody b = orbit.TargetBody;

                SCANdata data = getData(b.bodyName);

                if (data == null)
                    return;

                data.addStationaryWaypoints(b, s);
            }

            if (_bigMap.IsVisible && _bigMap.WaypointToggle)
                _bigMap.RefreshIcons();

            if (_zoomMap.IsVisible && _zoomMap.IconsToggle)
                _zoomMap.RefreshIcons();
        }

        private void SOIChange(GameEvents.HostedFromToAction<Vessel, CelestialBody> VC)
        {
            if (!body_data.Contains(VC.to.bodyName))
                body_data.Add(VC.to.bodyName, new SCANdata(VC.to));
        }

        private void setNewTerrainConfigValues(SCANterrainConfig terrain, float min, float max, float? clamp, SCANPalette c, int size, bool reverse, bool discrete)
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
                            UnityEngine.Debug.LogError("[SCANsat] Error in loading Celestial Body...\n" + e);
                            return;
                        }

                        if (b != null)
                        {
                            SCANresourceBody res = r.getBodyConfig(b.bodyName, false);
                            if (res != null)
                            {
                                if (!float.TryParse(sB[1], out min))
                                    min = res.DefaultMinValue;
                                if (!float.TryParse(sB[2], out max))
                                    max = res.DefaultMaxValue;
                                res.MinValue = min;
                                res.MaxValue = max;
                            }
                            //else
                            //SCANUtil.SCANlog("No resources found assigned for Celestial Body: {0}, skipping...", b.bodyName);
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

            public Color32 trackColor;

            public bool inRange;
            public bool bestRange;
            public bool inDarkness;

            public bool requireLight;

            public SCANsensor() { }
        }

        public class SCANvessel
        {
            public Guid id;
            public Vessel vessel;

            public Color32 trackColor;

            public List<SCANsensor> sensors = new List<SCANsensor>();
            
            public CelestialBody body;
            public double latitude, longitude;
            public int frame;
            public double lastUT;
        }

        private void registerSensorTemp(Guid id, SCANtype sensors, double _fov, double _min_alt, double _max_alt, double _best_alt, bool _require_light)
        {
            if (id == null)
                return;

            if (!knownVessels.Contains(id))
                knownVessels[id] = new SCANvessel();

            SCANvessel sv = knownVessels[id];
            sv.id = id;

            if (_max_alt <= 0)
            {
                _min_alt = 5000;
                _max_alt = 500000;
                _best_alt = 200000;
                _fov = 5;
            }

            foreach (SCANtype sensorType in Enum.GetValues(typeof(SCANtype)))
            {
                if (SCANUtil.countBits((int)sensorType) != 1)
                    continue;

                if ((sensorType & sensors) == SCANtype.Nothing)
                    continue;
                
                bool flag = true;

                for (int i = sv.sensors.Count - 1; i >= 0; i--)
                {
                    SCANsensor sen = sv.sensors[i];

                    if (sen.min_alt == _min_alt && sen.max_alt == _max_alt
                        && sen.best_alt == _best_alt && sen.fov == _fov
                        && sen.requireLight == _require_light)
                    {
                        SCANtype t = sen.sensor | sensorType;

                        sv.sensors[i] = new SCANsensor()
                        {
                            min_alt = _min_alt,
                            max_alt = _max_alt,
                            best_alt = _best_alt,
                            fov = _fov,
                            sensor = t,
                            requireLight = _require_light,
                            trackColor = getScanTypeColor(t),
                            //sensorType = (int)sv.sensors[i].sensor,
                        };

                        flag = false;

                        break;
                    }
                }

                if (flag)
                {
                    sv.sensors.Add(new SCANsensor()
                    {
                        min_alt = _min_alt,
                        max_alt = _max_alt,
                        best_alt = _best_alt,
                        fov = _fov,
                        requireLight = _require_light,
                        sensor = sensorType,
                        trackColor = getScanTypeColor(sensorType),
                    });
                }
            }

            sv.trackColor = palette.combineColors(sv.sensors.Select(s => s.trackColor).ToArray());
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
                UnityEngine.Debug.LogError("[SCANsat] Something went wrong while trying to load this SCANsat vessel; moving on the next vessel... \n" + e);
            }

            if (sv.vessel == null)
                knownVessels.Remove(id);
        }

        internal void registerSensor(Vessel v, SCANtype sensors, double fov, double min_alt, double max_alt, double best_alt, bool _require_light)
        {
            registerSensor(v.id, sensors, fov, min_alt, max_alt, best_alt, _require_light);
            knownVessels[v.id].vessel = v;
            knownVessels[v.id].latitude = SCANUtil.fixLatShift(v.latitude);
            knownVessels[v.id].longitude = SCANUtil.fixLonShift(v.longitude);
        }

        private void registerSensor(Guid id, SCANtype sensors, double _fov, double _min_alt, double _max_alt, double _best_alt, bool _require_light)
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
                UnityEngine.Debug.LogError("[SCANsat] Something went wrong while trying to load this SCANsat vessel; moving on the next vessel... \n" + e);
            }
            if (sv.vessel == null)
            {
                knownVessels.Remove(id);
                return;
            }

            if (_max_alt <= 0)
            {
                _min_alt = 5000;
                _max_alt = 500000;
                _best_alt = 200000;
                _fov = 5;
            }

            foreach (SCANtype sensorType in Enum.GetValues(typeof(SCANtype)))
            {
                if (SCANUtil.countBits((int)sensorType) != 1)
                    continue;
                if ((sensorType & sensors) == SCANtype.Nothing)
                    continue;

                bool flag = true;

                for (int i = sv.sensors.Count - 1; i >= 0; i--)
                {
                    SCANsensor sen = sv.sensors[i];

                    if (sen.min_alt == _min_alt && sen.max_alt == _max_alt
                        && sen.best_alt == _best_alt && sen.fov == _fov
                        && sen.requireLight == _require_light)
                    {
                        SCANtype t = sen.sensor | sensorType;

                        sv.sensors[i] = new SCANsensor()
                        {
                            min_alt = _min_alt,
                            max_alt = _max_alt,
                            best_alt = _best_alt,
                            fov = _fov,
                            sensor = t,
                            requireLight = _require_light,
                            trackColor = getScanTypeColor(t),
                        };

                        flag = false;

                        break;
                    }
                }

                if (flag)
                {
                    sv.sensors.Add(new SCANsensor()
                    {
                        min_alt = _min_alt,
                        max_alt = _max_alt,
                        best_alt = _best_alt,
                        fov = _fov,
                        sensor = sensorType,
                        requireLight = _require_light,
                        trackColor = getScanTypeColor(sensorType),
                    });
                }
            }

            sv.trackColor = palette.combineColors(sv.sensors.Select(s => s.trackColor).ToArray());
        }

        internal void unregisterSensor(Vessel v, SCANtype sensors, double _fov, double _min_alt, double _max_alt, double _best_alt, bool require_light)
        {
            if (!knownVessels.Contains(v.id))
                return;

            SCANvessel sv = knownVessels[v.id];
            sv.id = v.id;
            sv.vessel = v;

            if (_max_alt <= 0)
            {
                _min_alt = 5000;
                _max_alt = 500000;
                _best_alt = 200000;
                _fov = 5;
            }

            foreach (SCANtype sensor in Enum.GetValues(typeof(SCANtype)))
            {
                if ((sensors & sensor) == SCANtype.Nothing)
                    continue;

                for (int i = sv.sensors.Count - 1; i >= 0; i--)
                {
                    SCANsensor sen = sv.sensors[i];

                    if ((sen.sensor & sensor) != SCANtype.Nothing)
                    {
                        if (sen.min_alt == _min_alt && sen.max_alt == _max_alt
                            && sen.best_alt == _best_alt && sen.fov == _fov
                            && sen.requireLight == require_light)
                        {
                            SCANtype t = sen.sensor ^ sensor;

                            sv.sensors[i] = new SCANsensor()
                            {
                                min_alt = sen.min_alt,
                                max_alt = sen.max_alt,
                                best_alt = sen.best_alt,
                                fov = sen.fov,
                                sensor = t,
                                requireLight = require_light,
                                trackColor = getScanTypeColor(t),
                            };
                        }
                    }

                    if (sv.sensors[i].sensor == SCANtype.Nothing)
                        sv.sensors.RemoveAt(i);
                }
            }

            sv.trackColor = palette.combineColors(sv.sensors.Select(s => s.trackColor).ToArray());

            if (sv.sensors.Count == 0)
            {
                knownVessels.Remove(v.id);
                //SCANUtil.SCANdebugLog("Unregister Vessel");
            }
        }

        public void unregisterVessel(Vessel v)
        {
            if (!knownVessels.Contains(v.id))
                return;

            knownVessels.Remove(v.id);
        }

        internal bool isVesselKnown(Guid id, SCANtype sensor)
        {
            if (!knownVessels.Contains(id))
                return false;

            SCANtype all = SCANtype.Nothing;
            
            for (int i = knownVessels[id].sensors.Count - 1; i >= 0; i--)
                all |= knownVessels[id].sensors[i].sensor;

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

            for (int i = knownVessels[v.id].sensors.Count - 1; i >= 0; i--)
            {
                if ((knownVessels[v.id].sensors[i].sensor & sensor) != SCANtype.Nothing)
                    return knownVessels[v.id].sensors[i];
            }

            return null;
        }

        internal SCANtype activeSensorsOnVessel(Guid id, bool daylight)
        {
            if (!knownVessels.Contains(id))
                return SCANtype.Nothing;

            SCANtype sensors = SCANtype.Nothing;

            for (int i = knownVessels[id].sensors.Count - 1; i >= 0; i--)
            {
                if (daylight)
                {
                    SCANsensor sen = knownVessels[id].sensors[i];

                    if (sen.requireLight)
                    {
                        Vessel ves = knownVessels[id].vessel;
                        if (SCANUtil.InDarkness(ves.orbit.getPositionAtUT(Planetarium.GetUniversalTime()), ves.mainBody.position, SCANUtil.LocalSun(ves.mainBody).position))
                        {
                            continue;
                        }
                    }
                }

                sensors |= knownVessels[id].sensors[i].sensor;
            }
            return sensors;
        }

        //private System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
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
            if (body_data.Count > 0)
            {
                body_data.At(i).updateCoverage();    //Update coverage for planets one at a time, rather than all together
                i++;
                if (i >= body_data.Count) i = 0;
            }
            int l = knownVessels.Count;

            SCANvessel vessel = null;
            SCANdata data = null;

            //watch.Reset();
            //watch.Start();

            for (int j = 0; j < l; j++)
            {
                //Profiler.BeginSample("SCAN Vessel scan");
                vessel = knownVessels.At(j);

                data = SCANUtil.getData(vessel.vessel.mainBody);

                if (data == null)
                    continue;

                if (!data.Disabled)
                {
                    if (isVesselKnown(vessel.vessel))
                    {
                        doScanPass(vessel, vessel.vessel, data, scan_UT, scan_UT, vessel.lastUT);
                        ++currentActiveVessel;
                        currentActiveSensor += knownVessels[vessel.vessel.id].sensors.Count;
                    }
                }

                vessel.body = vessel.vessel.mainBody;
                vessel.frame = Time.frameCount;
                vessel.lastUT = scan_UT;
                vessel.latitude = SCANUtil.fixLatShift(vessel.vessel.latitude);
                vessel.longitude = SCANUtil.fixLonShift(vessel.vessel.longitude);
                //Profiler.EndSample();
            }

            //watch.Stop();

            //SCANUtil.SCANlog("SCAN Time: {0} - Passes: {1}", watch.ElapsedMilliseconds.ToString(), actualPasses.ToString());
            activeVessels = currentActiveVessel;
            activeSensors = currentActiveSensor;
        }

        private int actualPasses;
        private static Queue<double> scanQueue;
        private void doScanPass(SCANvessel vessel, Vessel v, SCANdata data, double UT, double startUT, double lastUT)
        {
            //SCANUtil.SCANlog("Start New Scan Pass");
            CelestialBody sun = SCANUtil.LocalSun(v.mainBody);
            double soi_radius = v.mainBody.sphereOfInfluence - v.mainBody.Radius;
            double alt = v.altitude;
            double llat = SCANUtil.fixLat(v.latitude);
            double llon = SCANUtil.fixLon(v.longitude);
            int lat = (int)Math.Floor(llat);
            int lon = (int)Math.Floor(llon);
            double res = 0;
            Orbit o = v.orbit;
            bool uncovered;
            bool darkness = SCANUtil.InDarkness(o.getPositionAtUT(UT), v.mainBody.position, sun.position);
            short sensorType;

            double surfscale = Planetarium.fetch.Home.Radius / v.mainBody.Radius;

            if (surfscale < 1)
                surfscale = 1;

            surfscale = Math.Sqrt(surfscale);

            if (scanQueue == null)
                scanQueue = new Queue<double>();

            if (scanQueue.Count != 0)
                scanQueue.Clear();

            loop: // don't look at me like that, I just unrolled the recursion
            if (res > 0)
            {
                if (double.IsNaN(UT))
                    goto dequeue;
                
                if (double.IsNaN(o.getObtAtUT(UT)))
                    goto dequeue;

                Vector3d pos = o.getPositionAtUT(UT);
                double rotation = 0;

                if (v.mainBody.rotates)
                    rotation = (360 * ((UT - scan_UT) / v.mainBody.rotationPeriod)) % 360;

                alt = v.mainBody.GetAltitude(pos);
                lat = SCANUtil.fixLatInt(v.mainBody.GetLatitude(pos));
                lon = SCANUtil.fixLonInt(v.mainBody.GetLongitude(pos) - rotation);
                darkness = SCANUtil.InDarkness(pos, v.mainBody.position, sun.position);

                if (alt < 0)
                    alt = 0;
            }
            else
            {
                alt = v.radarAltitude;
                if (alt < 0)
                    alt = v.altitude;
            }

            if (Math.Abs((lat * 1d) - llat) < 1 && Math.Abs((lon * 1d) - llon) < 1 && res > 0)
                goto dequeue;

            actualPasses++;

            uncovered = res <= 0;
            //SCANUtil.SCANlog("Begin scan pass: {0}", vessel.sensors.Count);
            for (int j = vessel.sensors.Count - 1; j >= 0; j--)
            {
                SCANsensor sensor = vessel.sensors[j];

                sensorType = (short)sensor.sensor;

                if (res <= 0)
                {
                    if (data.getCoverage(sensor.sensor) > 0)
                        uncovered = false;
                }

                sensor.inDarkness = sensor.requireLight;

                if (sensor.requireLight && darkness)
                {
                    //SCANUtil.SCANlog("Sensor {0} fail light check...", sensorType);
                    continue;
                }

                sensor.inDarkness = false;
                sensor.inRange = false;
                sensor.bestRange = false;

                double ma = Math.Max(0, Math.Min(sensor.min_alt, soi_radius - 100000));

                if (alt < ma)
                    continue;

                if (alt > Math.Min(sensor.max_alt, soi_radius))
                    continue;

                //Profiler.BeginSample("SCAN Sensor Check");

                sensor.inRange = true;

                double fov = sensor.fov;
                double ba = Math.Min(sensor.best_alt, soi_radius);

                if (alt < ba)
                    fov = (alt / ba) * fov;
                else
                    sensor.bestRange = true;
                
                fov *= surfscale;

                if (fov > 20)
                    fov = 20;

                int f = (int)Math.Truncate(fov);
                int f1 = f + (int)Math.Round(fov - f);

                int w = f;
                double fovW = fov;

                if (lat < 180)
                {
                    fovW = fov * (1 / SCANUtil.cosLookUp[lat]);
                    
                    if (fovW > 120)
                        fovW = 120;

                    w = (int)Math.Truncate(fovW);
                }

                int w1 = w + (int)Math.Round(fovW - w);

                for (int x = -w; x <= w1; ++x)
                {
                    int clampLon = lon + x;

                    if (clampLon > 359 || clampLon < 0)
                    {
                        clampLon = (clampLon + 360) % 360;
                    }

                    for (int y = -f; y <= f1; ++y)
                    {
                        int clampLat = lat + y;
                        int clampLonAgain = clampLon;

                        if (clampLat > 179)
                        {
                            clampLat = 359 - clampLat;
                            clampLonAgain += 180;

                            clampLat = (clampLat + 180) % 180;

                            if (clampLonAgain > 359 || clampLonAgain < 0)
                            {
                                clampLonAgain = (clampLonAgain + 360) % 360;
                            }
                        }
                        else if (clampLat < 0)
                        {
                            clampLat = 0 - clampLat;
                            clampLonAgain += 180;

                            clampLat = (clampLat + 180) % 180;

                            if (clampLonAgain > 359 || clampLonAgain < 0)
                            {
                                clampLonAgain = (clampLonAgain + 360) % 360;
                            }
                        }
                        
                        data.coverage[clampLonAgain, clampLat] |= sensorType;
                    }
                }
                //Profiler.EndSample();
            }
            if (uncovered)
                return;

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
