#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_UI_MainMap - UI control object for SCANsat main map
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using KSP.UI;
using SCANsat.SCAN_Data;
using SCANsat.SCAN_Map;
using SCANsat.SCAN_Toolbar;
using SCANsat.SCAN_UI.UI_Framework;
using SCANsat.Unity;
using SCANsat.Unity.Interfaces;
using SCANsat.Unity.Unity;
using System;
using System.Collections.Generic;
using UnityEngine;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANcolorUtil;

namespace SCANsat.SCAN_Unity
{
	public class SCAN_UI_MainMap : ISCAN_MainMap
	{
		private bool _isVisible;

		private Vessel v;
		private SCANdata data;
		private SCANtype sensors;
		private SCANmap visualMap;      // the small map: one 360x180 rectangular SCANmap, GPU-composited in every display mode (see pumpMap)
		private Texture shownTexture;   // what the RawImage currently points at
		private int updateInterval = 60;
		private int lastUpdate;
		private bool flip;

		private SCAN_MainMap uiElement;

		private static SCAN_UI_MainMap instance;

		public static SCAN_UI_MainMap Instance
		{
			get { return instance; }
		}

		public SCAN_UI_MainMap()
		{
			instance = this;

			GameEvents.onVesselSOIChanged.Add(soiChange);
			GameEvents.onVesselChange.Add(vesselChange);
			GameEvents.onVesselWasModified.Add(vesselChange);

			if (SCANcontroller.controller.mainMapVisible)
			{
				Open();
			}
		}

		public void OnDestroy()
		{
			if (uiElement != null)
			{
				uiElement.gameObject.SetActive(false);
				MonoBehaviour.Destroy(uiElement.gameObject);
			}

			destroyVisualMap();

			GameEvents.onVesselSOIChanged.Remove(soiChange);
			GameEvents.onVesselChange.Remove(vesselChange);
			GameEvents.onVesselWasModified.Remove(vesselChange);
		}

		private void soiChange(GameEvents.HostedFromToAction<Vessel, CelestialBody> VC)
		{
			v = FlightGlobals.ActiveVessel;

			data = SCANUtil.getData(v.mainBody);

			if (data == null)
			{
				data = new SCANdata(v.mainBody);
				SCANcontroller.controller.addToBodyData(v.mainBody, data);
			}

			resetImages();

			if (uiElement != null)
			{
				uiElement.RefreshVessels();
			}
		}

		bool vesselChanged = false;

		private void vesselChange(Vessel V)
		{
			v = FlightGlobals.ActiveVessel;
			vesselChanged = true;
		}

		private void RefreshAfterVesselChanged()
		{
			vesselChanged = false;
			
			// TODO: make this smarter about not doing work if it doesn't need to

			data = SCANUtil.getData(v.mainBody);

			if (data == null)
			{
				data = new SCANdata(v.mainBody);
				SCANcontroller.controller.addToBodyData(v.mainBody, data);
			}

			resetImages();

			if (uiElement != null)
			{
				uiElement.RefreshVessels();
			}
		}

		public void SetScale(float scale)
		{
			if (uiElement != null)
			{
				uiElement.SetScale(scale);
			}
		}

		public void ProcessTooltips()
		{
			if (uiElement != null)
			{
				uiElement.ProcessTooltips();
			}
		}

		public void Update()
		{
			if (vesselChanged)
			{
				RefreshAfterVesselChanged();
			}

			if (!_isVisible || data == null)
			{
				return;
			}

			sensors = SCANcontroller.controller.activeSensorsOnVessel(v.id, false);

			pumpMap(SCANcontroller.controller.mainMapDisplayMode);

			lastUpdate++;

			if (uiElement == null)
			{
				return;
			}

			if (lastUpdate < updateInterval)
			{
				return;
			}

			lastUpdate = 0;
			flip = !flip;

			SCANcontroller.SCANsensor s;

			s = SCANcontroller.controller.getSensorStatus(v, SCANtype.AltimetryLoRes);
			if (s == null)
			{
				uiElement.UpdateLoColor(palette.grey);
			}
			else if (s.inDarkness)
			{
				uiElement.UpdateLoColor(palette.c_bad);
			}
			else if (!s.inRange)
			{
				uiElement.UpdateLoColor(palette.c_bad);
			}
			else if (!s.bestRange && flip)
			{
				uiElement.UpdateLoColor(palette.c_bad);
			}
			else
			{
				uiElement.UpdateLoColor(palette.c_good);
			}

			s = SCANcontroller.controller.getSensorStatus(v, SCANtype.AltimetryHiRes);
			if (s == null)
			{
				uiElement.UpdateHiColor(palette.grey);
			}
			else if (s.inDarkness)
			{
				uiElement.UpdateHiColor(palette.c_bad);
			}
			else if (!s.inRange)
			{
				uiElement.UpdateHiColor(palette.c_bad);
			}
			else if (!s.bestRange && flip)
			{
				uiElement.UpdateHiColor(palette.c_bad);
			}
			else
			{
				uiElement.UpdateHiColor(palette.c_good);
			}

			s = SCANcontroller.controller.getSensorStatus(v, SCANtype.Biome);
			if (s == null)
			{
				uiElement.UpdateMultiColor(palette.grey);
			}
			else if (s.inDarkness)
			{
				uiElement.UpdateMultiColor(palette.c_bad);
			}
			else if (!s.inRange)
			{
				uiElement.UpdateMultiColor(palette.c_bad);
			}
			else if (!s.bestRange && flip)
			{
				uiElement.UpdateMultiColor(palette.c_bad);
			}
			else
			{
				uiElement.UpdateMultiColor(palette.c_good);
			}

			s = SCANcontroller.controller.getSensorStatus(v, SCANtype.VisualLoRes);
			if (s == null)
			{
				uiElement.UpdateVisLoColor(palette.grey);
			}
			else if (s.inDarkness)
			{
				uiElement.UpdateVisHiColor(palette.c_bad);
			}
			else if (!s.inRange)
			{
				uiElement.UpdateVisLoColor(palette.c_bad);
			}
			else if (!s.bestRange && flip)
			{
				uiElement.UpdateVisLoColor(palette.c_bad);
			}
			else
			{
				uiElement.UpdateVisLoColor(palette.c_good);
			}

			s = SCANcontroller.controller.getSensorStatus(v, SCANtype.VisualHiRes);
			if (s == null)
			{
				uiElement.UpdateVisHiColor(palette.grey);
			}
			else if (s.inDarkness)
			{
				uiElement.UpdateVisHiColor(palette.c_bad);
			}
			else if (!s.inRange)
			{
				uiElement.UpdateVisHiColor(palette.c_bad);
			}
			else if (!s.bestRange && flip)
			{
				uiElement.UpdateVisHiColor(palette.c_bad);
			}
			else
			{
				uiElement.UpdateVisHiColor(palette.c_good);
			}

			//         if (ResourcesOn)
			//{
			s = SCANcontroller.controller.getSensorStatus(v, SCANtype.ResourceLoRes);
			if (s == null)
			{
				uiElement.UpdateM700Color(palette.grey);
			}
			else if (s.inDarkness)
			{
				uiElement.UpdateM700Color(palette.c_bad);
			}
			else if (!s.inRange)
			{
				uiElement.UpdateM700Color(palette.c_bad);
			}
			else if (!s.bestRange && flip)
			{
				uiElement.UpdateM700Color(palette.c_bad);
			}
			else
			{
				uiElement.UpdateM700Color(palette.c_good);
			}

			s = SCANcontroller.controller.getSensorStatus(v, SCANtype.ResourceHiRes);
			if (s == null)
			{
				uiElement.UpdateOreColor(palette.grey);
			}
			else if (s.inDarkness)
			{
				uiElement.UpdateOreColor(palette.c_bad);
			}
			else if (!s.inRange)
			{
				uiElement.UpdateOreColor(palette.c_bad);
			}
			else if (!s.bestRange && flip)
			{
				uiElement.UpdateOreColor(palette.c_bad);
			}
			else
			{
				uiElement.UpdateOreColor(palette.c_good);
			}
			//}

			if (sensors != SCANtype.Nothing)
			{
				uiElement.UpdatePercentage(string.Format("{0}%", SCANUtil.getCoveragePercentage(data, sensors).ToString("N1")));
			}
			else
			{
				uiElement.UpdatePercentage("0%");
			}
		}

		public string Version
		{
			get { return SCANmainMenuLoader.SCANsatVersion; }
		}

		public void Open()
		{
			if (uiElement != null)
			{
				uiElement.gameObject.SetActive(false);
				MonoBehaviour.DestroyImmediate(uiElement.gameObject);
			}

			v = FlightGlobals.ActiveVessel;

			data = SCANUtil.getData(v.mainBody);

			if (data == null)
			{
				data = new SCANdata(v.mainBody);
				SCANcontroller.controller.addToBodyData(v.mainBody, data);
			}

			uiElement = GameObject.Instantiate(SCAN_UI_Loader.MainMapPrefab).GetComponent<SCAN_MainMap>();

			if (uiElement == null)
			{
				return;
			}

			uiElement.transform.SetParent(UIMasterController.Instance.dialogCanvas.transform, false);

			uiElement.setMap(this);

			resetImages();

			shownTexture = null;   // the first Update re-points the RawImage at the map's RenderTexture

			_isVisible = true;
			SCANcontroller.controller.mainMapVisible = true;

			if (HighLogic.LoadedSceneIsFlight && SCAN_Settings_Config.Instance.StockToolbar)
			{
				if (SCAN_Settings_Config.Instance.ToolbarMenu)
				{
					if (SCANappLauncher.Instance != null && SCANappLauncher.Instance.UIElement != null)
					{
						SCANappLauncher.Instance.UIElement.SetMainMapToggle(true);
					}
				}
				else
				{
					if (SCANappLauncher.Instance != null && SCANappLauncher.Instance.SCANAppButton != null)
					{
						SCANappLauncher.Instance.SCANAppButton.SetTrue(false);
					}
				}
			}
		}

		public void Close()
		{
			_isVisible = false;
			SCANcontroller.controller.mainMapVisible = false;

			if (uiElement == null)
			{
				return;
			}

			uiElement.FadeOut();

			if (HighLogic.LoadedSceneIsFlight && SCAN_Settings_Config.Instance.StockToolbar)
			{
				if (SCAN_Settings_Config.Instance.ToolbarMenu)
				{
					if (SCANappLauncher.Instance != null && SCANappLauncher.Instance.UIElement != null)
					{
						SCANappLauncher.Instance.UIElement.SetMainMapToggle(false);
					}
				}
				else
				{
					if (SCANappLauncher.Instance != null && SCANappLauncher.Instance.SCANAppButton != null)
					{
						SCANappLauncher.Instance.SCANAppButton.SetFalse(false);
					}
				}
			}

			uiElement = null;
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

		public bool Color
		{
			get { return SCANcontroller.controller.mainMapColor; }
			set
			{
				SCANcontroller.controller.mainMapColor = value;

				resetImages();
			}
		}

		public bool TerminatorToggle
		{
			get { return SCANcontroller.controller.mainMapTerminator; }
			set
			{
				SCANcontroller.controller.mainMapTerminator = value;

				resetImages();
			}
		}

		public MainMapDisplayMode MapType
		{
			get { return SCANcontroller.controller.mainMapDisplayMode; }
			set
			{
				SCANcontroller.controller.mainMapDisplayMode = value;

				resetImages();
			}
		}

		public bool Minimized
		{
			get { return SCANcontroller.controller.mainMapMinimized; }
			set { SCANcontroller.controller.mainMapMinimized = value; }
		}

		public bool TooltipsOn
		{
			get { return SCAN_Settings_Config.Instance.WindowTooltips; }
		}

		public bool MapGenerating
		{
			get { return data == null ? false : !data.Built || data.MapBuilding || data.OverlayBuilding || data.ControllerBuilding; }
		}

		public bool ResourcesOn
		{
			get { return SCAN_Settings_Config.Instance.DisableStockResource || !SCAN_Settings_Config.Instance.InstantScan; }
		}

		public float Scale
		{
			get { return SCAN_Settings_Config.Instance.UIScale; }
		}

		public Canvas TooltipCanvas
		{
			get { return UIMasterController.Instance.tooltipCanvas; }
		}

		public Vector2 Position
		{
			get { return SCAN_Settings_Config.Instance.MainMapPosition; }
			set { SCAN_Settings_Config.Instance.MainMapPosition = value; }
		}

		public Sprite VesselType(Guid id)
		{
			SCANcontroller.SCANvessel v;

			if (!SCANcontroller.controller.knownVessels.TryGetValue(id, out v))
			{
				v = null;
			}

			Vessel sv;

			if (v == null)
			{
				if (FlightGlobals.ActiveVessel.id == id)
				{
					sv = FlightGlobals.ActiveVessel;
				}
				else
				{
					return SCAN_UI_Loader.MysteryIcon;
				}
			}
			else
			{
				sv = v.vessel;
			}

			return SCAN_UI_Loader.VesselIcon(sv.vesselType);
		}

		public Vector2 VesselPosition(Guid id)
		{
			SCANcontroller.SCANvessel v;

			if (!SCANcontroller.controller.knownVessels.TryGetValue(id, out v))
			{
				v = null;
			}

			Vessel sv;

			if (v == null)
			{
				if (FlightGlobals.ActiveVessel.id == id)
				{
					sv = FlightGlobals.ActiveVessel;
				}
				else
				{
					return new Vector2();
				}
			}
			else
			{
				sv = v.vessel;
			}

			double lon = SCANUtil.fixLon(sv.longitude);
			double lat = SCANUtil.fixLat(sv.latitude);

			return new Vector2((float)lon, (float)lat);
		}

		public Dictionary<Guid, MapLabelInfo> VesselInfoList
		{
			get
			{
				Dictionary<Guid, MapLabelInfo> vessels = new Dictionary<Guid, MapLabelInfo>();

				vessels.Add(v.id, new MapLabelInfo()
				{
					label = Minimized ? "" : "1",
					name = v.vesselName,
					image = VesselType(v.id),
					pos = VesselPosition(v.id),
					baseColor = Color ? palette.white : palette.cb_skyBlue,
					flashColor = palette.cb_yellow,
					flash = true,
					width = 18,
					show = true
				});

				int count = 2;

				for (int i = 0; i < SCANcontroller.controller.knownVessels.Count; i++)
				{
					SCANcontroller.SCANvessel sv = SCANcontroller.controller.knownVessels.At(i);

					if (sv.vessel == v)
					{
						continue;
					}

					if (sv.vessel.mainBody != v.mainBody)
					{
						continue;
					}

					vessels.Add(sv.vessel.id, new MapLabelInfo()
					{
						label = Minimized ? "" : count.ToString(),
						name = sv.vessel.vesselName,
						image = VesselType(sv.vessel.id),
						pos = VesselPosition(sv.vessel.id),
						baseColor = Color ? palette.white : palette.cb_skyBlue,
						flash = false,
						width = 18,
						show = true
					});

					count++;
				}

				return vessels;
			}
		}

		public void ClampToScreen(RectTransform rect)
		{
			UIMasterController.ClampToScreen(rect, Vector2.zero);
		}

		public void OpenBigMap()
		{
			if (SCAN_UI_BigMap.Instance.IsVisible)
			{
				SCAN_UI_BigMap.Instance.Close();
			}
			else
			{
				SCAN_UI_BigMap.Instance.Open();
			}
		}

		public void OpenZoomMap()
		{
			if (SCAN_UI_ZoomMap.Instance.IsVisible)
			{
				SCAN_UI_ZoomMap.Instance.Close();
			}
			else
			{
				SCAN_UI_ZoomMap.Instance.Open(true);
			}
		}

		public void OpenOverlay()
		{
			if (SCAN_UI_Overlay.Instance.IsVisible)
			{
				SCAN_UI_Overlay.Instance.Close();
			}
			else
			{
				SCAN_UI_Overlay.Instance.Open();
			}
		}

		public void OpenInstruments()
		{
			if (SCAN_UI_Instruments.Instance.IsVisible)
			{
				SCAN_UI_Instruments.Instance.Close();
			}
			else
			{
				SCAN_UI_Instruments.Instance.Open();
			}
		}

		public void OpenSettings()
		{
			if (SCAN_UI_Settings.Instance.IsVisible)
			{
				SCAN_UI_Settings.Instance.Close();
			}
			else
			{
				SCAN_UI_Settings.Instance.Open();
			}
		}

		public void ChangeToVessel(Guid id)
		{
			if (v == null || v.id == id)
			{
				return;
			}

			SCANcontroller.SCANvessel sv;

			if (!SCANcontroller.controller.knownVessels.TryGetValue(id, out sv))
			{
				sv = null;
			}

			if (sv == null)
			{
				return;
			}

			if (!HighLogic.CurrentGame.Parameters.Flight.CanSwitchVesselsFar)
			{
				return;
			}

			if (FlightGlobals.SetActiveVessel(sv.vessel))
			{
				if (MapView.MapIsEnabled)
				{
					MapView.ExitMapView();
				}

				FlightInputHandler.SetNeutralControls();
			}
		}

		public string VesselInfo(Guid id)
		{
			SCANcontroller.SCANvessel sv;

			if (!SCANcontroller.controller.knownVessels.TryGetValue(id, out sv))
			{
				sv = null;
			}

			Vessel ves;

			if (sv == null)
			{
				if (FlightGlobals.ActiveVessel.id == id)
				{
					ves = FlightGlobals.ActiveVessel;
				}
				else
				{
					return "";
				}
			}
			else
			{
				ves = sv.vessel;
			}

			float lon = (float)SCANUtil.fixLonShift(ves.longitude);
			float lat = (float)SCANUtil.fixLatShift(ves.latitude);

			string units = "";
			if (SCANcontroller.controller.mainMapDisplayMode == MainMapDisplayMode.Biome)
			{
				if (SCANUtil.isCovered(lon, lat, data, SCANtype.Biome))
				{
					units = string.Format("; {0}", SCANUtil.getBiomeDisplayName(data.Body, lon, lat));
				}
			}
			else
			{
				if (SCANUtil.isCovered(lon, lat, data, SCANtype.Altimetry))
				{
					if (SCANUtil.isCovered(lon, lat, data, SCANtype.AltimetryHiRes))
					{
						float alt = ves.heightFromTerrain;

						if (alt < 0)
						{
							alt = (float)ves.altitude;
						}

						units = string.Format("; {0}", SCANuiUtil.distanceString(alt, 100000, 100000000));
					}
					else
					{
						float alt = ves.heightFromTerrain;

						if (alt < 0)
						{
							alt = (float)ves.altitude;
						}

						alt = ((int)(alt / 500)) * 500;

						units = string.Format("; {0}", SCANuiUtil.distanceString(alt, 100000, 100000000));
					}
				}
			}

			return string.Format("({0}°,{1}°{2})", lat.ToString("F1"), lon.ToString("F1"), units);
		}

		/* The small map is one rectangular 360x180 SCANmap (mapSource.Data), composited on the GPU in
		   every display mode: Terrain is SCANmap's Altimetry (HiRes colour ramp, LoRes grey), fed from the
		   body's prebuilt 360x180 height map rather than PQS; Biome and Visual are SCANmap's own. The
		   shader adds the classic small-map details for this source: grey for uncovered pixels, the
		   dotted 30-degree graticule, no elevation underlay on biomes. */

		private static mapType displayModeToMapType(MainMapDisplayMode mode)
		{
			switch (mode)
			{
				case MainMapDisplayMode.Biome:
					return mapType.Biome;
				case MainMapDisplayMode.Visual:
					return mapType.Visual;
				default:
					return mapType.Altimetry;
			}
		}

		private void ensureVisualMap()
		{
			if (visualMap != null && visualMap.Body == v.mainBody)
			{
				return;
			}

			destroyVisualMap();

			visualMap = new SCANmap(v.mainBody, mapSource.Data);
			visualMap.setProjection(MapProjection.Rectangular);
			visualMap.setSize(360, 180);
			visualMap.MType = displayModeToMapType(SCANcontroller.controller.mainMapDisplayMode);
			visualMap.setBody(v.mainBody);
		}

		// Mode / colour / terminator / body changed: re-sync the map and start a fresh pass.
		private void resetVisualMap()
		{
			ensureVisualMap();

			visualMap.ColorMap = Color;
			visualMap.Terminator = TerminatorToggle;
			visualMap.resetMap(displayModeToMapType(SCANcontroller.controller.mainMapDisplayMode), false, false);
		}

		private void pumpMap(MainMapDisplayMode display)
		{
			// Terrain draws from the body's 360x180 height map. Until the controller has built it, pump
			// the build from here as the classic small map did, and draw nothing meanwhile.
			if (display == MainMapDisplayMode.Terrain && !data.Built)
			{
				// Someone else owns the build - unless nobody has pumped it for a few frames, in which case
				// that owner is gone (a window closed, an overlay build abandoned, a body swapped under it)
				// and the build is ours to finish, from the row it stopped on.
				if ((data.ControllerBuilding || data.OverlayBuilding) && !data.BuildStalled)
				{
					return;
				}

				data.takeOverBuild();
				data.MapBuilding = true;
				SCANcontroller.pumpHeightMap(data);
				return;
			}

			if (visualMap == null)
			{
				resetVisualMap();
			}

			// The small map is a live scanning display: when a pass completes, start another so newly
			// scanned coverage shows up, under a redline like the classic small map's. A GPU pass is one
			// composite per frame for the length of the timed sweep, so this is cheap.
			if (visualMap.isMapComplete())
			{
				visualMap.resetMap(false, false);
			}

			// The CPU small map dimmed every pixel not covered by ALL of the active vessel's sensors;
			// `sensors` is already refreshed once per Update (line 160), as it was there.
			visualMap.SensorMask = sensors;

			visualMap.getPartialMap();

			showTexture(visualMap.DisplayTexture);
		}

		// The RawImage only needs re-pointing when the texture object changes (CPU Texture2D <->
		// GPU RenderTexture, or a re-created RenderTexture); in-place updates show through.
		private void showTexture(Texture t)
		{
			if (uiElement == null || t == null || ReferenceEquals(t, shownTexture))
			{
				return;
			}

			shownTexture = t;
			uiElement.UpdateMapTexture(t);
		}

		private void destroyVisualMap()
		{
			if (visualMap == null)
			{
				return;
			}

			// Release this source's claim on the body's Visual textures, then the RT/material.
			SCANcontroller.controller.UnloadVisualMapTexture(visualMap.Body, mapSource.Data);
			visualMap.Destroy();
			visualMap = null;
		}

		internal void resetImages()
		{
			resetVisualMap();
		}

		public void ResetPosition()
		{
			SCAN_Settings_Config.Instance.MainMapPosition = new Vector2(100, -200);

			if (uiElement != null)
			{
				uiElement.SetPosition(SCAN_Settings_Config.Instance.MainMapPosition);
			}
		}
	}
}
