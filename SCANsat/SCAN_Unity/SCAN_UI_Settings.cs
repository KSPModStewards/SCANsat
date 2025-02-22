#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_UI_Settings - UI control object for SCANsat settings window
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SCANsat.Unity.Interfaces;
using SCANsat.Unity.Unity;
using SCANsat.SCAN_Data;
using SCANsat.SCAN_Toolbar;
using SCANsat.SCAN_UI.UI_Framework;
using KSP.UI;
using KSP.UI.Screens;
using KSP.Localization;
using KSPAssets.KSPedia;

namespace SCANsat.SCAN_Unity
{
	public class SCAN_UI_Settings : ISCAN_Settings
	{
		private static ApplicationLauncherButton dummyButton;

		private bool _isVisible;
		private bool _inputLock;
		private string _sensorCount = "";
		private Vector2 _position;
		private const string controlLock = "SCANsatSettings";

		private string _currentData = "All Data";
		private SCANtype _currentDataType = SCANtype.Everything;

		private SCAN_Settings uiElement;

		private SCAN_UI_Color colorInterface;

		private static SCAN_UI_Settings instance;

		public static SCAN_UI_Settings Instance
		{
			get { return instance; }
		}

		public SCAN_UI_Settings()
		{
			instance = this;

			colorInterface = new SCAN_UI_Color();
		}

		public int Page
		{
			get
			{
				if (uiElement != null)
				{
					return uiElement.Page;
				}

				return 0;
			}
		}

		public bool IsCurrentResource(string body, string resource)
		{
			if (uiElement != null)
			{
				return uiElement.IsCurrentResourceActive(body, resource);
			}

			return false;
		}

		public void OnDestroy()
		{
			if (uiElement != null)
			{
				uiElement.gameObject.SetActive(false);
				MonoBehaviour.Destroy(uiElement.gameObject);
			}
		}

		public void Update()
		{
			if (!_isVisible)
			{
				return;
			}

			_sensorCount = string.Format("Vessels: {0} Sensors: {1} Passes: {2}"
				, SCANcontroller.controller.ActiveVessels
				, SCANcontroller.controller.ActiveSensors
				, SCANcontroller.controller.ActualPasses);
		}

		public void Open(int page = 0, bool savePosition = false, bool forceResource = false)
		{
			if (uiElement != null)
			{
				uiElement.gameObject.SetActive(false);
				MonoBehaviour.DestroyImmediate(uiElement.gameObject);
			}

			uiElement = GameObject.Instantiate(SCAN_UI_Loader.SettingsPrefab).GetComponent<SCAN_Settings>();

			if (uiElement == null)
			{
				return;
			}

			uiElement.transform.SetParent(UIMasterController.Instance.dialogCanvas.transform, false);

			uiElement.setSettings(this, page, forceResource);

			if (!savePosition)
			{
				_position = new Vector2(0, 100);
			}

			uiElement.SetPosition(_position);

			_isVisible = true;

			if (HighLogic.LoadedSceneIsFlight && SCAN_Settings_Config.Instance.StockToolbar && SCAN_Settings_Config.Instance.ToolbarMenu)
			{
				if (SCANappLauncher.Instance != null && SCANappLauncher.Instance.UIElement != null)
				{
					SCANappLauncher.Instance.UIElement.SetSettingsToggle(true);
				}
			}
		}

		public void Close()
		{
			_isVisible = false;

			if (uiElement == null)
			{
				return;
			}

			uiElement.FadeOut();

			if (HighLogic.LoadedSceneIsFlight && SCAN_Settings_Config.Instance.StockToolbar && SCAN_Settings_Config.Instance.ToolbarMenu)
			{
				if (SCANappLauncher.Instance != null && SCANappLauncher.Instance.UIElement != null)
				{
					SCANappLauncher.Instance.UIElement.SetSettingsToggle(false);
				}
			}

			if (_inputLock)
			{
				InputLockManager.RemoveControlLock(controlLock);
			}

			uiElement = null;
		}

		public string Version
		{
			get { return SCANmainMenuLoader.SCANsatVersion; }
		}

		public string SensorCount
		{
			get { return _sensorCount; }
		}

		public string DataResetCurrent
		{
			get { return Localizer.Format("#autoLOC_SCANsat_Warning_DataResetCurrent", _currentDataType, getTargetBody().displayName); }
		}

		public string DataResetAll
		{
			get { return Localizer.Format("#autoLOC_SCANsat_Warning_DataResetAll", _currentDataType); }
		}

		public string StockResourceResetCurrent
		{
			get { return Localizer.Format("#autoLOC_SCANsat_Warning_StockResourceResetCurrent", getTargetBody().displayName); }
		}

		public string StockResourceResetAll
		{
			get { return Localizer.Format("#autoLOC_SCANsat_Warning_StockResourceResetAll"); }
		}

		public string WarningMapFillCurrent
		{
			get { return Localizer.Format("#autoLOC_SCANsat_Warning_MapFillCurrent", _currentData, getTargetBody().displayName); }
		}

		public string WarningMapFillAll
		{
			get { return Localizer.Format("#autoLOC_SCANsat_Warning_MapFillAll", _currentData); }
		}

		public string ModuleManagerWarning
		{
			get { return Localizer.Format("#autoLOC_SCANsat_Warning_ModuleManagerResource"); }
		}

		public string CurrentBody
		{
			get { return getTargetBody().displayName.LocalizeBodyName(); }
		}

		public string SaveToConfig
		{
			get { return Localizer.Format("#autoLOC_SCANsat_Warning_SaveToConfig"); }
		}

		public string CurrentMapData
		{
			get { return _currentData; }
			set
			{
				_currentData = value;

				if (value == "All Data")
				{
					_currentDataType = SCANtype.Everything;
				}
				//else if (value == "SCAN Data Types")
				//	_currentDataType = SCANtype.Everything_SCAN;
				//else if (value == "All Resource Types")
				//{
				//                _currentDataType = SCANtype.ResourceHiRes;
				//	//_currentDataType = 0;

				//	//List<SCANresourceGlobal> resources = SCANcontroller.setLoadedResourceList();

				//	//for (int i = 0; i < resources.Count; i++)
				//	//	_currentDataType |= resources[i].SType;
				//}
				else
				{
					try
					{
						_currentDataType = (SCANtype)Enum.Parse(typeof(SCANtype), value);
					}
					catch (Exception e)
					{
						SCANUtil.SCANlog("Error in parsing map fill type value: {0} - Setting fill type to Everything:\n{1}", value, e);
						_currentData = "All Data";
						_currentDataType = SCANtype.Everything;
					}
				}
			}
		}

		public int MapGenSpeed
		{
			get { return SCAN_Settings_Config.Instance.MapGenerationSpeed; }
			set { SCAN_Settings_Config.Instance.MapGenerationSpeed = value; }
		}

		public int TimeWarp
		{
			get { return SCAN_Settings_Config.Instance.TimeWarpResolution; }
			set { SCAN_Settings_Config.Instance.TimeWarpResolution = value; }
		}

		public int MapWidth
		{
			get { return SCAN_Settings_Config.Instance.BigMapWidth; }
			set
			{
				SCAN_Settings_Config.Instance.BigMapWidth = value;

				if (SCAN_UI_BigMap.Instance != null && SCAN_UI_BigMap.Instance.IsVisible)
				{
					SCAN_UI_BigMap.Instance.Size = new Vector2(value, value / 2);

					SCAN_UI_BigMap.Instance.SetMapSize();

					SCAN_UI_BigMap.Instance.RefreshIcons();
				}
			}
		}

		public int Interpolation
		{
			get { return SCAN_Settings_Config.Instance.Interpolation; }
			set { SCAN_Settings_Config.Instance.Interpolation = value; }
		}

		public int MapHeight
		{
			get { return SCAN_Settings_Config.Instance.ResourceMapHeight; }
			set { SCAN_Settings_Config.Instance.ResourceMapHeight = value; }
		}

		public int BiomeMapHeight
		{
			get { return SCAN_Settings_Config.Instance.BiomeMapHeight; }
			set { SCAN_Settings_Config.Instance.BiomeMapHeight = value; }
		}

		public float Transparency
		{
			get { return SCAN_Settings_Config.Instance.CoverageTransparency; }
			set { SCAN_Settings_Config.Instance.CoverageTransparency = value; }
		}

		public float StockThresholdValue
		{
			get { return SCAN_Settings_Config.Instance.StockTreshold; }
			set { SCAN_Settings_Config.Instance.StockTreshold = value; }
		}

		public float UIScale
		{
			get { return SCAN_Settings_Config.Instance.UIScale; }
			set
			{
				SCAN_Settings_Config.Instance.UIScale = value;

				uiElement.SetScale(value);

				if (SCAN_UI_BigMap.Instance != null && SCAN_UI_BigMap.Instance.IsVisible)
				{
					SCAN_UI_BigMap.Instance.SetScale(value);
				}

				if (SCAN_UI_MainMap.Instance != null && SCAN_UI_MainMap.Instance.IsVisible)
				{
					SCAN_UI_MainMap.Instance.SetScale(value);
				}

				if (SCAN_UI_ZoomMap.Instance != null && SCAN_UI_ZoomMap.Instance.IsVisible)
				{
					SCAN_UI_ZoomMap.Instance.SetScale(value);
				}

				if (SCAN_UI_Instruments.Instance != null && SCAN_UI_Instruments.Instance.IsVisible)
				{
					SCAN_UI_Instruments.Instance.SetScale(value);
				}

				if (SCAN_UI_Overlay.Instance != null && SCAN_UI_Overlay.Instance.IsVisible)
				{
					SCAN_UI_Overlay.Instance.SetScale(value);
				}
			}
		}

		public bool IsVisible
		{
			get { return _isVisible; }
			set
			{
				_isVisible = value;

				if (!value)
				{
					Close();
				}
			}
		}

		public bool BackgroundScanning
		{
			get { return SCAN_Settings_Config.Instance.BackgroundScanning; }
			set { SCAN_Settings_Config.Instance.BackgroundScanning = value; }
		}

		public bool GroundTracks
		{
			get { return SCAN_Settings_Config.Instance.ShowGroundTracks; }
			set { SCAN_Settings_Config.Instance.ShowGroundTracks = value; }
		}

		public bool ActiveGround
		{
			get { return SCAN_Settings_Config.Instance.GroundTracksActiveOnly; }
			set { SCAN_Settings_Config.Instance.GroundTracksActiveOnly = value; }
		}

		public bool OverlayTooltips
		{
			get { return SCAN_Settings_Config.Instance.OverlayTooltips; }
			set { SCAN_Settings_Config.Instance.OverlayTooltips = value; }
		}

		public bool WindowTooltips
		{
			get { return SCAN_Settings_Config.Instance.WindowTooltips; }
			set
			{
				SCAN_Settings_Config.Instance.WindowTooltips = value;

				SCAN_UI_Loader.ToggleTooltips(value);

				if (SCAN_UI_BigMap.Instance != null && SCAN_UI_BigMap.Instance.IsVisible)
				{
					SCAN_UI_BigMap.Instance.ProcessTooltips();
				}

				if (SCAN_UI_MainMap.Instance != null && SCAN_UI_MainMap.Instance.IsVisible)
				{
					SCAN_UI_MainMap.Instance.ProcessTooltips();
				}

				if (SCAN_UI_Instruments.Instance != null && SCAN_UI_Instruments.Instance.IsVisible)
				{
					SCAN_UI_Instruments.Instance.ProcessTooltips();
				}

				if (SCAN_UI_Overlay.Instance != null && SCAN_UI_Overlay.Instance.IsVisible)
				{
					SCAN_UI_Overlay.Instance.ProcessTooltips();
				}

				if (SCAN_Settings_Config.Instance.StockToolbar && SCAN_Settings_Config.Instance.ToolbarMenu
					&& SCANappLauncher.Instance != null && SCANappLauncher.Instance.IsVisible)
				{
					SCANappLauncher.Instance.ProcessTooltips();
				}
			}
		}

		public bool LegendTooltips
		{
			get { return SCAN_Settings_Config.Instance.LegendTooltips; }
			set { SCAN_Settings_Config.Instance.LegendTooltips = value; }
		}

		public bool StockToolbar
		{
			get { return SCAN_Settings_Config.Instance.StockToolbar; }
			set
			{
				SCAN_Settings_Config.Instance.StockToolbar = value;

				if (value)
				{
					SCANcontroller.controller.appLauncher = SCANcontroller.controller.gameObject.AddComponent<SCANappLauncher>();
				}
				else
				{
					MonoBehaviour.Destroy(SCANcontroller.controller.appLauncher);
					SCANcontroller.controller.appLauncher = null;
				}
			}
		}

		public bool ToolbarMenu
		{
			get { return SCAN_Settings_Config.Instance.ToolbarMenu; }
			set
			{
				SCAN_Settings_Config.Instance.ToolbarMenu = value;

				if (SCANappLauncher.Instance != null)
				{
					SCANappLauncher.Instance.ToggleToolbarType();
				}
			}
		}

		public bool StockUIStyle
		{
			get { return SCAN_Settings_Config.Instance.StockUIStyle; }
			set
			{
				SCAN_Settings_Config.Instance.StockUIStyle = value;

				SCAN_UI_Loader.ResetUIStyle();

				if (SCAN_UI_BigMap.Instance != null && SCAN_UI_BigMap.Instance.IsVisible)
				{
					SCAN_UI_BigMap.Instance.Close();
					SCAN_UI_BigMap.Instance.Open();
				}

				if (SCAN_UI_MainMap.Instance != null && SCAN_UI_MainMap.Instance.IsVisible && HighLogic.LoadedSceneIsFlight)
				{
					SCAN_UI_MainMap.Instance.Close();
					SCAN_UI_MainMap.Instance.Open();
				}

				if (SCAN_UI_ZoomMap.Instance != null && SCAN_UI_ZoomMap.Instance.IsVisible)
				{
					SCAN_UI_ZoomMap.Instance.Close();
					SCAN_UI_ZoomMap.Instance.Open(true);
				}

				if (SCAN_UI_Instruments.Instance != null && SCAN_UI_Instruments.Instance.IsVisible && HighLogic.LoadedSceneIsFlight)
				{
					SCAN_UI_Instruments.Instance.Close();
					SCAN_UI_Instruments.Instance.Open();
				}

				if (SCAN_UI_Overlay.Instance != null && SCAN_UI_Overlay.Instance.IsVisible && HighLogic.LoadedScene != GameScenes.SPACECENTER)
				{
					SCAN_UI_Overlay.Instance.Close();
					SCAN_UI_Overlay.Instance.Open();
				}

				Close();
				Open(0, true);
			}
		}

		public bool MechJebTarget
		{
			get { return SCAN_Settings_Config.Instance.MechJebTarget; }
			set { SCAN_Settings_Config.Instance.MechJebTarget = value; }
		}

		public bool MechJebLoad
		{
			get { return SCAN_Settings_Config.Instance.MechJebTargetLoad; }
			set { SCAN_Settings_Config.Instance.MechJebTargetLoad = value; }
		}

		public bool MechJebAvailable
		{
			get { return SCANmainMenuLoader.MechJebLoaded; }
		}

		public bool DaylightCheck
		{
			get { return SCAN_Settings_Config.Instance.DaylightCheck; }
			set { SCAN_Settings_Config.Instance.DaylightCheck = value; }
		}

		public bool BiomeLock
		{
			get { return SCAN_Settings_Config.Instance.BiomeLock; }
			set { SCAN_Settings_Config.Instance.BiomeLock = value; }
		}

		public bool NarrowBand
		{
			get { return SCAN_Settings_Config.Instance.RequireNarrowBand; }
			set { SCAN_Settings_Config.Instance.RequireNarrowBand = value; }
		}

		public bool InstantScan
		{
			get { return SCAN_Settings_Config.Instance.InstantScan; }
			set
			{
				SCAN_Settings_Config.Instance.InstantScan = value;

				if (SCAN_UI_MainMap.Instance != null && SCAN_UI_MainMap.Instance.IsVisible)
				{
					SCAN_UI_MainMap.Instance.Close();
					SCAN_UI_MainMap.Instance.Open();
				}

			}
		}

		public bool DisableStock
		{
			get { return SCAN_Settings_Config.Instance.DisableStockResource; }
			set
			{
				SCAN_Settings_Config.Instance.DisableStockResource = value;

				if (SCAN_UI_MainMap.Instance != null && SCAN_UI_MainMap.Instance.IsVisible)
				{
					SCAN_UI_MainMap.Instance.Close();
					SCAN_UI_MainMap.Instance.Open();
				}

			}
		}

		public bool StockThreshold
		{
			get { return SCAN_Settings_Config.Instance.UseStockTreshold; }
			set { SCAN_Settings_Config.Instance.UseStockTreshold = value; }
		}

		public bool HideZeroResources
		{
			get { return SCAN_Settings_Config.Instance.HideZeroResources; }
			set { SCAN_Settings_Config.Instance.HideZeroResources = value; }
		}

		public bool GreyScale
		{
			get { return SCAN_Settings_Config.Instance.TrueGreyScale; }
			set { SCAN_Settings_Config.Instance.TrueGreyScale = value; }
		}

		public bool ExportCSV
		{
			get { return SCAN_Settings_Config.Instance.ExportCSV; }
			set { SCAN_Settings_Config.Instance.ExportCSV = value; }
		}

		public bool ShowStockReset
		{
			get { return SCAN_Settings_Config.Instance.DisableStockResource || !SCAN_Settings_Config.Instance.InstantScan; }
		}

		public bool ShowMapFill
		{
			get { return SCAN_Settings_Config.Instance.CheatMapFill; }
		}

		public bool LockInput
		{
			get { return _inputLock; }
			set
			{
				_inputLock = value;

				if (_inputLock)
				{
					InputLockManager.SetControlLock(controlLock);
				}
				else
				{
					InputLockManager.RemoveControlLock(controlLock);
				}
			}
		}

		public bool ModuleManager
		{
			get { return SCANmainMenuLoader.MMLoaded; }
		}

		public Canvas TooltipCanvas
		{
			get { return UIMasterController.Instance.tooltipCanvas; }
		}

		public Vector2 Position
		{
			set { _position = value; }
		}

		public IList<string> BackgroundBodies
		{
			get
			{
				var bodies = FlightGlobals.Bodies.Where(b => b.referenceBody == Planetarium.fetch.Sun && b.referenceBody != b);

				var orderedBodies = bodies.OrderBy(b => b.orbit.semiMajorAxis).ToList();

				List<string> bodyList = SCANUtil.RecursiveCelestialBodies(orderedBodies);

				bool missingHome = true;

				for (int i = bodyList.Count - 1; i >= 0; i--)
				{
					string b = bodyList[i];

					if (b == FlightGlobals.GetHomeBody().displayName.LocalizeBodyName())
					{
						missingHome = false;
						break;
					}
				}

				if (missingHome)
				{
					List<string> missingHomeBodies = SCANUtil.RecursiveCelestialBodies(new List<CelestialBody>() { FlightGlobals.GetHomeBody() });

					bodyList.InsertRange(0, missingHomeBodies);
				}

				SCANdata sun = SCANcontroller.controller.getData(Planetarium.fetch.Sun.bodyName);

				if (sun != null)
				{
					bodyList.Add(sun.Body.displayName.LocalizeBodyName());
				}

				return bodyList;
			}
		}

		public IList<string> MapDataTypes
		{
			get
			{
				List<int> availableTypes = new List<int>() { 0, 1, 3, 4, 5, 2, 6, 7, 8 };

				List<string> types = new List<string>() { "All Data" };

				for (int i = 0; i < availableTypes.Count; i++)
				{
					types.Add(((SCANtype)(1 << availableTypes[i])).ToString());
				}

				return types;
			}
		}

		public ISCAN_Color ColorInterface
		{
			get { return colorInterface; }
		}

		public void OpenKSPedia(bool isOn)
		{
			if (dummyButton == null)
			{
				dummyButton = UnityEngine.Object.Instantiate<ApplicationLauncherButton>(ApplicationLauncher.Instance.listItemPrefab);
			}

			if (isOn)
			{
				KSPediaSpawner.Show(dummyButton);

				try
				{
					KSPediaSpawner.Show("SCANsat_Header");
				}
				catch (Exception e)
				{
					SCANUtil.SCANlog("KSPedia Database not ready; can't load SCANsat page; loading first page");
				}
			}
			else
			{
				KSPediaSpawner.Hide();
			}
		}

		public void ClampToScreen(RectTransform rect)
		{
			UIMasterController.ClampToScreen(rect, Vector2.zero);
		}

		public void ResetCurrent()
		{
			CelestialBody thisBody = getTargetBody();

			SCANdata data = SCANUtil.getData(thisBody);

			if (data != null)
			{
				data.reset(_currentDataType);
			}
		}

		public void ResetAll()
		{
			foreach (SCANdata data in SCANcontroller.controller.GetAllData)
			{
				data.reset(_currentDataType);
			}
		}

		public void ResetStockResourceCurrent()
		{
			CelestialBody thisBody = getTargetBody();

			var resources = ResourceScenario.Instance.gameSettings.GetPlanetScanInfo();

			resources.RemoveAll(a => a.PlanetId == thisBody.flightGlobalsIndex);
		}

		public void ResetStockResourceAll()
		{
			ResourceScenario.Instance.gameSettings.GetPlanetScanInfo().Clear();
		}

		public void FillCurrent()
		{
			CelestialBody thisBody = getTargetBody();

			SCANdata data = SCANUtil.getData(thisBody);

			if (data == null)
			{
				data = new SCANdata(thisBody);
				SCANcontroller.controller.addToBodyData(thisBody, data);
			}

			data.fillMap(_currentDataType);
		}

		public void FillAll()
		{
			foreach (CelestialBody b in FlightGlobals.Bodies)
			{
				SCANdata data = SCANUtil.getData(b);

				if (data == null)
				{
					data = new SCANdata(b);
					SCANcontroller.controller.addToBodyData(b, data);
				}

				data.fillMap(_currentDataType);
			}
		}

		public void ResetWindows()
		{
			if (HighLogic.LoadedSceneIsFlight)
			{
				SCANuiUtil.resetMainMapPos();
				SCANuiUtil.resetBigMapPos();
				SCANuiUtil.resetInstUIPos();
				SCANuiUtil.resetOverlayControllerPos();
				SCANuiUtil.resetZoomMapPos();
			}
			else
			{
				SCANuiUtil.resetBigMapPos();
				SCANuiUtil.resetZoomMapPos();
				if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)
				{
					SCANuiUtil.resetOverlayControllerPos();
				}
			}
		}

		public void ToggleBody(string bodyName)
		{
			string body = SCANUtil.bodyFromDisplayName(bodyName);

			SCANdata data = SCANUtil.getData(body);

			if (data != null)
			{
				data.Disabled = !data.Disabled;
			}
		}

		public bool ToggleBodyActive(string bodyName)
		{
			string body = SCANUtil.bodyFromDisplayName(bodyName);

			SCANdata data = SCANUtil.getData(body);

			return data == null ? false : !data.Disabled;
		}

		public double BodyPercentage(string bodyName)
		{
			return SCANUtil.getCoveragePercentage(SCANUtil.getData(SCANUtil.bodyFromDisplayName(bodyName)), SCANtype.Nothing) / 100;
		}

		private CelestialBody getTargetBody()
		{
			switch (HighLogic.LoadedScene)
			{
				case GameScenes.FLIGHT:
					return FlightGlobals.currentMainBody;
				case GameScenes.SPACECENTER:
					return Planetarium.fetch.Home;
				case GameScenes.TRACKSTATION:
					return SCANUtil.getTargetBody(MapView.MapCamera.target);
				default:
					return null;
			}
		}
	}
}
