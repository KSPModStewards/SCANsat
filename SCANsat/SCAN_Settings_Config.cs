#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_Settings_Config - serializable object that stores settings in an external file
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANcolorUtil;

namespace SCANsat
{
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	public class SCAN_Settings_Config : MonoBehaviour
	{
		[Persistent]
		public bool BackgroundScanning = true;
		[Persistent]
		public int TimeWarpResolution = 12;
		[Persistent]
		public bool ShowGroundTracks = true;
		[Persistent]
		public bool GroundTracksActiveOnly = true;
		[Persistent]
		public bool MechJebTarget = false;
		[Persistent]
		public bool MechJebTargetLoad = false;
		[Persistent]
		public bool OverlayTooltips = true;
		[Persistent]
		public bool WindowTooltips = true;
		[Persistent]
		public bool LegendTooltips = true;
		[Persistent]
		public bool StockToolbar = true;
		[Persistent]
		public bool ToolbarMenu = true;
		[Persistent]
		public bool StockUIStyle = false;
		[Persistent]
		public int MapGenerationSpeed = 2;
		[Persistent]
		public float UIScale = 1;
		[Persistent]
		public bool BiomeLock = true;
		[Persistent]
		public bool RequireNarrowBand = true;
		[Persistent]
		public bool DisableStockResource = false;
		[Persistent]
		public bool InstantScan = true;
		[Persistent]
		public bool UseStockTreshold = true;
		[Persistent]
		public float StockTreshold = 0.9f;
		[Persistent]
		public bool HideZeroResources = true;
		[Persistent]
		public int Interpolation = 8;
		[Persistent]
		public int ResourceMapHeight = 256;
		[Persistent]
		public int BiomeMapHeight = 512;
		[Persistent]
		public float CoverageTransparency = 0.2f;
		[Persistent]
		public bool TrueGreyScale = false;
		[Persistent]
		public bool ExportCSV = false;
		[Persistent]
		public float UnscannedTransparency = 0.4f;
		[Persistent]
		public float BackgroundTransparency = 0.4f;
		[Persistent]
		public float BiomeTransparency = 0.4f;
		[Persistent]
		public bool MapVignette = false;
		[Persistent]
		public bool BigMapBiomeBorder = true;
		[Persistent]
		public bool BigMapStockBiomes = true;
		[Persistent]
		public bool ZoomMapBiomeBorder = true;
		[Persistent]
		public bool SmallMapBiomeBorder = false;
		[Persistent]
		public bool SmallMapStockBiomes = true;
		[Persistent]
		public bool DaylightCheck = true;
		[Persistent]
		public Color MapBackgroundColor = palette.grey;
		[Persistent]
		public Color UnscannedColor = palette.grey;
		[Persistent]
		public Color LowBiomeColor = palette.xkcd_CamoGreen;
		[Persistent]
		public Color HighBiomeColor = palette.xkcd_Marigold;
		[Persistent]
		public float SlopeCutoff = 1;
		[Persistent]
		public Color BottomLowSlopeColor = palette.xkcd_PukeGreen;
		[Persistent]
		public Color BottomHighSlopeColor = palette.xkcd_Lemon;
		[Persistent]
		public Color TopLowSlopeColor = palette.xkcd_Lemon;
		[Persistent]
		public Color TopHighSlopeColor = palette.xkcd_OrangeRed;
		[Persistent]
		public Color32 LoResAltimetryTrackColor = palette.cb_blue * new Vector4(1, 1, 1, 0.4f);
		[Persistent]
		public Color32 HiResAltimetryTrackColor = palette.cb_reddishPurple * new Vector4(1, 1, 1, 0.4f);
		[Persistent]
		public Color32 BiomeTrackColor = palette.cb_yellow * new Vector4(1, 1, 1, 0.4f);
		[Persistent]
		public Color32 AnomalyTrackColor = palette.cb_orange * new Vector4(1, 1, 1, 0.4f);
		[Persistent]
		public Color32 AnomalyDetailTrackColor = palette.xkcd_DarkGreenAlpha;
		[Persistent]
		public Color32 LoResVisualTrackColor = palette.xkcd_DarkGreenAlpha;
		[Persistent]
		public Color32 HiResVisualTrackColor = palette.xkcd_DarkGreenAlpha;
		[Persistent]
		public Color32 FuzzyResourceTrackColor = palette.cb_bluishGreen * new Vector4(1, 1, 1, 0.4f);
		[Persistent]
		public Color32 HiResResourceTrackColor = palette.xkcd_DarkGreenAlpha;
		[Persistent]
		public bool CheatMapFill = false;
		[Persistent]
		public int BigMapWidth = 720;
		[Persistent]
		public Vector2 ZoomMapSize = new Vector2(360, 240);
		[Persistent]
		public Vector2 BigMapPosition = new Vector2(400, -400);
		[Persistent]
		public Vector2 MainMapPosition = new Vector2(100, -200);
		[Persistent]
		public Vector2 ZoomMapPosition = new Vector2(400, -400);
		[Persistent]
		public Vector2 InstrumentsPosition = new Vector2(100, -500);
		[Persistent]
		public Vector2 OverlayPosition = new Vector2(600, -200);
		[Persistent]
		public bool VisibleMapsActive = true;

		private const string filePath = "GameData/SCANsat/PluginData/Settings.cfg";
		private const string directoryPath = "GameData/SCANsat/PluginData";
		private string fullPath;

		private static bool loaded;
		private static SCAN_Settings_Config instance;

		public static SCAN_Settings_Config Instance
		{
			get { return instance; }
		}

		private void Awake()
		{
			if (loaded)
			{
				Destroy(gameObject);
				return;
			}

			DontDestroyOnLoad(gameObject);

			loaded = true;

			instance = this;

			fullPath = Path.Combine(KSPUtil.ApplicationRootPath, filePath).Replace("\\", "/");

			if (Load())
			{
				SCANUtil.SCANlog("Settings file loaded");
			}
			else
			{
				if (Save())
				{
					SCANUtil.SCANlog("Settings file generated at:\n{0}", filePath);
				}
			}

			GameEvents.onGameStateSaved.Add(GameSaved);
		}

		private void GameSaved(Game g)
		{
			if (HighLogic.CurrentGame == g)
			{
				Save();
			}
		}

		public bool Load()
		{
			bool b = false;

			try
			{
				if (File.Exists(fullPath))
				{
					ConfigNode node = ConfigNode.Load(fullPath);
					ConfigNode unwrapped = node.GetNode(GetType().Name);
					ConfigNode.LoadObjectFromConfig(this, unwrapped);
					b = true;
				}
				else
				{
					SCANUtil.SCANlog("Settings file could not be found [{0}]", fullPath);
					b = false;
				}
			}
			catch (Exception e)
			{
				SCANUtil.SCANlog("Error while loading settings file from [{0}]\n{1}", fullPath, e);
				b = false;
			}

			return b;
		}

		public bool Save()
		{
			bool b = false;

			try
			{
				string dirPath = Path.Combine(KSPUtil.ApplicationRootPath, directoryPath).Replace("\\", "/");

				if (!Directory.Exists(dirPath))
				{
					Directory.CreateDirectory(dirPath);
					SCANUtil.SCANlog("Settings directory not found; generating PluginData directory at:\n{0}", dirPath);
				}

				ConfigNode node = AsConfigNode();
				ConfigNode wrapper = new ConfigNode(GetType().Name);
				wrapper.AddNode(node);
				wrapper.Save(fullPath);
				b = true;
			}
			catch (Exception e)
			{
				SCANUtil.SCANlog("Error while saving settings file from [{0}]\n{1}", fullPath, e);
				b = false;
			}

			return b;
		}

		private ConfigNode AsConfigNode()
		{
			try
			{
				ConfigNode node = new ConfigNode(GetType().Name);

				node = ConfigNode.CreateConfigFromObject(this, node);
				return node;
			}
			catch (Exception e)
			{
				SCANUtil.SCANlog("Failed to generate settings file node...\n{0}", e);
				return new ConfigNode(GetType().Name);
			}
		}
	}
}
