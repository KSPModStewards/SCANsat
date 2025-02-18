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

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using SCANsat.SCAN_Toolbar;
using SCANsat.Unity.Interfaces;
using SCANsat.Unity;
using SCANsat.Unity.Unity;
using SCANsat.SCAN_Data;
using SCANsat.SCAN_UI.UI_Framework;
using KSP.UI;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANpalette;

namespace SCANsat.SCAN_Unity
{
	public class SCAN_UI_MainMap : ISCAN_MainMap
	{
		private bool _isVisible;
		
		private Vessel v;
		private SCANdata data;
		private SCANtype sensors;
		private Texture2D map_small;
		private Color32[] cols_height_map_small;
		private Color32[] biomeCache;
		private bool biomeBuilding;
		private double[] biomeIndex = new double[360];
		private int scanline;
		private int scanstep;
		private int updateInterval = 60;
		private int lastUpdate;
		private bool flip;

		double sunLonCenter;
		double sunLatCenter;
		double gamma;

		private SCAN_MainMap uiElement;
		
		private static SCAN_UI_MainMap instance;

		public static SCAN_UI_MainMap Instance
		{
			get { return instance; }
		}

		public SCAN_UI_MainMap()
		{
			instance = this;

			map_small = new Texture2D(360, 180, TextureFormat.ARGB32, false);
			cols_height_map_small = new Color32[360];
			biomeCache = new Color32[360 * 180];

			if (palette.small_redline == null)
			{
				palette.small_redline = new Color32[360];
				for (int i = 0; i < 360; i++)
					palette.small_redline[i] = palette.Red;
			}

			GameEvents.onVesselSOIChanged.Add(soiChange);
			GameEvents.onVesselChange.Add(vesselChange);
			GameEvents.onVesselWasModified.Add(vesselChange);

			if (SCANcontroller.controller.mainMapVisible)
				Open();
		}

		public void OnDestroy()
		{
			if (uiElement != null)
			{
				uiElement.gameObject.SetActive(false);
				MonoBehaviour.Destroy(uiElement.gameObject);
			}

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
				uiElement.RefreshVessels();
		}

		private void vesselChange(Vessel V)
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
				uiElement.RefreshVessels();
		}

		public void SetScale(float scale)
		{
			if (uiElement != null)
				uiElement.SetScale(scale);
		}

		public void ProcessTooltips()
		{
			if (uiElement != null)
				uiElement.ProcessTooltips();
		}

		public void Update()
		{
			if (!_isVisible || data == null)
				return;

			sensors = SCANcontroller.controller.activeSensorsOnVessel(v.id);

			if (!SCANcontroller.controller.mainMapBiome)
			{
				if (SCAN_Settings_Config.Instance.MapGenerationSpeed > 1)
					drawPartialMap(sensors, false);

				drawPartialMap(sensors, true);
			}
			else
			{
				if (SCAN_Settings_Config.Instance.MapGenerationSpeed > 1)
					drawBiomeMap(sensors, false);
					
				drawBiomeMap(sensors, true);
			}

			lastUpdate++;

			if (uiElement == null)
				return;

			if (lastUpdate < updateInterval)
				return;

			lastUpdate = 0;
			flip = !flip;

			SCANcontroller.SCANsensor s;

			s = SCANcontroller.controller.getSensorStatus(v, SCANtype.AltimetryLoRes);
			if (s == null)
				uiElement.UpdateLoColor(palette.grey);
			else if (!s.inRange)
				uiElement.UpdateLoColor(palette.c_bad);
			else if (!s.bestRange && flip)
				uiElement.UpdateLoColor(palette.c_bad);
			else
				uiElement.UpdateLoColor(palette.c_good);

			s = SCANcontroller.controller.getSensorStatus(v, SCANtype.AltimetryHiRes);
			if (s == null)
				uiElement.UpdateHiColor(palette.grey);
			else if (!s.inRange)
				uiElement.UpdateHiColor(palette.c_bad);
			else if (!s.bestRange && flip)
				uiElement.UpdateHiColor(palette.c_bad);
			else
				uiElement.UpdateHiColor(palette.c_good);

			s = SCANcontroller.controller.getSensorStatus(v, SCANtype.Biome);
			if (s == null)
				uiElement.UpdateMultiColor(palette.grey);
			else if (!s.inRange)
				uiElement.UpdateMultiColor(palette.c_bad);
			else if (!s.bestRange && flip)
				uiElement.UpdateMultiColor(palette.c_bad);
			else
				uiElement.UpdateMultiColor(palette.c_good);

			if (ResourcesOn)
			{
				s = SCANcontroller.controller.getSensorStatus(v, SCANtype.FuzzyResources);
				if (s == null)
					uiElement.UpdateM700Color(palette.grey);
				else if (!s.inRange)
					uiElement.UpdateM700Color(palette.c_bad);
				else if (!s.bestRange && flip)
					uiElement.UpdateM700Color(palette.c_bad);
				else
					uiElement.UpdateM700Color(palette.c_good);

				s = SCANcontroller.controller.getSensorStatus(v, SCANtype.Ore);
				if (s == null)
					uiElement.UpdateOreColor(palette.grey);
				else if (!s.inRange)
					uiElement.UpdateOreColor(palette.c_bad);
				else if (!s.bestRange && flip)
					uiElement.UpdateOreColor(palette.c_bad);
				else
					uiElement.UpdateOreColor(palette.c_good);
			}

			if (sensors != SCANtype.Nothing)
				uiElement.UpdatePercentage(string.Format("{0:N1}%", SCANUtil.getCoveragePercentage(data, sensors)));
			else
				uiElement.UpdatePercentage("0%");
		}

		public string Version
		{
			get { return SCANmainMenuLoader.SCANsatVersion; }
		}

		public void Open()
		{
			v = FlightGlobals.ActiveVessel;

			data = SCANUtil.getData(v.mainBody);

			if (data == null)
			{
				data = new SCANdata(v.mainBody);
				SCANcontroller.controller.addToBodyData(v.mainBody, data);
			}

			uiElement = GameObject.Instantiate(SCAN_UI_Loader.MainMapPrefab).GetComponent<SCAN_MainMap>();

			if (uiElement == null)
				return;

			uiElement.transform.SetParent(UIMasterController.Instance.dialogCanvas.transform, false);

			uiElement.setMap(this);

			resetImages();

			uiElement.UpdateMapTexture(map_small);

			_isVisible = true;
			SCANcontroller.controller.mainMapVisible = true;

			if (HighLogic.LoadedSceneIsFlight && SCAN_Settings_Config.Instance.StockToolbar)
			{
				if (SCAN_Settings_Config.Instance.ToolbarMenu)
				{
					if (SCANappLauncher.Instance != null && SCANappLauncher.Instance.UIElement != null)
						SCANappLauncher.Instance.UIElement.SetMainMapToggle(true);
				}
				else
				{
					if (SCANappLauncher.Instance != null && SCANappLauncher.Instance.SCANAppButton != null)
						SCANappLauncher.Instance.SCANAppButton.SetTrue(false);
				}
			}
		}

		public void Close()
		{
			_isVisible = false;
			SCANcontroller.controller.mainMapVisible = false;

			if (uiElement == null)
				return;

			uiElement.FadeOut();

			if (HighLogic.LoadedSceneIsFlight && SCAN_Settings_Config.Instance.StockToolbar)
			{
				if (SCAN_Settings_Config.Instance.ToolbarMenu)
				{
					if (SCANappLauncher.Instance != null && SCANappLauncher.Instance.UIElement != null)
						SCANappLauncher.Instance.UIElement.SetMainMapToggle(false);
				}
				else
				{
					if (SCANappLauncher.Instance != null && SCANappLauncher.Instance.SCANAppButton != null)
						SCANappLauncher.Instance.SCANAppButton.SetFalse(false);
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
					Close();
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

		public bool MapType
		{
			get { return SCANcontroller.controller.mainMapBiome; }
			set
			{
				SCANcontroller.controller.mainMapBiome = value;

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
				v = null;

			Vessel sv;

			if (v == null)
			{
				if (FlightGlobals.ActiveVessel.id == id)
					sv = FlightGlobals.ActiveVessel;
				else
					return SCAN_UI_Loader.MysteryIcon;
			}
			else
				sv = v.vessel;

			return SCAN_UI_Loader.VesselIcon(sv.vesselType);
		}

		public Vector2 VesselPosition(Guid id)
		{
			SCANcontroller.SCANvessel v;

			if (!SCANcontroller.controller.knownVessels.TryGetValue(id, out v))
				v = null;

			Vessel sv;

			if (v == null)
			{
				if (FlightGlobals.ActiveVessel.id == id)
					sv = FlightGlobals.ActiveVessel;
				else
					return new Vector2();
			}
			else
				sv = v.vessel;

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
						continue;

					if (sv.vessel.mainBody != v.mainBody)
						continue;

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
				SCAN_UI_BigMap.Instance.Close();
			else
				SCAN_UI_BigMap.Instance.Open();
		}

		public void OpenZoomMap()
		{
			if (SCAN_UI_ZoomMap.Instance.IsVisible)
				SCAN_UI_ZoomMap.Instance.Close();
			else
				SCAN_UI_ZoomMap.Instance.Open(true);
		}

		public void OpenOverlay()
		{
			if (SCAN_UI_Overlay.Instance.IsVisible)
				SCAN_UI_Overlay.Instance.Close();
			else
				SCAN_UI_Overlay.Instance.Open();
		}

		public void OpenInstruments()
		{
			if (SCAN_UI_Instruments.Instance.IsVisible)
				SCAN_UI_Instruments.Instance.Close();
			else
				SCAN_UI_Instruments.Instance.Open();
		}

		public void OpenSettings()
		{
			if (SCAN_UI_Settings.Instance.IsVisible)
				SCAN_UI_Settings.Instance.Close();
			else
				SCAN_UI_Settings.Instance.Open();
		}

		public void ChangeToVessel(Guid id)
		{
			if (v == null || v.id == id)
				return;

			SCANcontroller.SCANvessel sv;

			if (!SCANcontroller.controller.knownVessels.TryGetValue(id, out sv))
				sv = null;

			if (sv == null)
				return;

			if (!HighLogic.CurrentGame.Parameters.Flight.CanSwitchVesselsFar)
				return;

			if (FlightGlobals.SetActiveVessel(sv.vessel))
			{
				if (MapView.MapIsEnabled)
					MapView.ExitMapView();

				FlightInputHandler.SetNeutralControls();
			}
		}

		public string VesselInfo(Guid id)
		{
			SCANcontroller.SCANvessel sv;

			if (!SCANcontroller.controller.knownVessels.TryGetValue(id, out sv))
				sv = null;

			Vessel ves;

			if (sv == null)
			{
				if (FlightGlobals.ActiveVessel.id == id)
					ves = FlightGlobals.ActiveVessel;
				else
					return "";
			}
			else
				ves = sv.vessel;

			float lon = (float)SCANUtil.fixLonShift(ves.longitude);
			float lat = (float)SCANUtil.fixLatShift(ves.latitude);

			string units = "";
			if (SCANcontroller.controller.mainMapBiome)
			{
				if (SCANUtil.isCovered(lon, lat, data, SCANtype.Biome))
					units = string.Format("; {0}", SCANUtil.getBiomeDisplayName(data.Body, lon, lat));
			}
			else
			{
				if (SCANUtil.isCovered(lon, lat, data, SCANtype.Altimetry))
				{
					if (SCANUtil.isCovered(lon, lat, data, SCANtype.AltimetryHiRes))
					{
						float alt = ves.heightFromTerrain;

						if (alt < 0)
							alt = (float)ves.altitude;

						units = string.Format("; {0}", SCANuiUtil.distanceString(alt, 100000, 100000000));
					}
					else
					{
						float alt = ves.heightFromTerrain;

						if (alt < 0)
							alt = (float)ves.altitude;

						alt = ((int)(alt / 500)) * 500;

						units = string.Format("; {0}", SCANuiUtil.distanceString(alt, 100000, 100000000));
					}
				}
			}

			return string.Format("({0:F1}°,{1:F1}°{2})", lat, lon, units);
		}

		private void drawPartialMap(SCANtype type, bool apply)
		{
			bool pqsController = data.Body.pqsController != null;

			if (data.ControllerBuilding || data.OverlayBuilding)
				return;

			if (!data.Built)
			{
				if (!data.MapBuilding)
				{
					scanline = 0;
					scanstep = 0;
				}

				data.MapBuilding = true;
				data.generateHeightMap(ref scanline, ref scanstep, 360);
				return;
			}

			if (scanline == 0 && TerminatorToggle)
			{
				double sunLon = data.Body.GetLongitude(Planetarium.fetch.Sun.position, false);
				double sunLat = data.Body.GetLatitude(Planetarium.fetch.Sun.position, false);

				sunLatCenter = SCANUtil.fixLatShift(sunLat);

				if (sunLatCenter >= 0)
					sunLonCenter = SCANUtil.fixLonShift(sunLon + 90);
				else
					sunLonCenter = SCANUtil.fixLonShift(sunLon - 90);

				gamma = Math.Abs(sunLatCenter) < 0.55 ? 100 : Math.Tan(Mathf.Deg2Rad * (90 - Math.Abs(sunLatCenter)));
			}

			for (int ilon = 0; ilon < 360; ilon++)
			{
				if (!pqsController)
				{
					cols_height_map_small[ilon] = palette.lerp(palette.black, palette.white, UnityEngine.Random.value);
					continue;
				}

				Color32 c = palette.Grey;
				float val = data.HeightMapValue(data.Body.flightGlobalsIndex, ilon, scanline);
				if (SCANUtil.isCovered(ilon, scanline, data, SCANtype.Altimetry))
				{
					if (SCANUtil.isCovered(ilon, scanline, data, SCANtype.AltimetryHiRes))
						c = palette.heightToColor(val, Color, data.TerrainConfig);
					else
						c = palette.heightToColor(val, false, data.TerrainConfig);
				}
				else
				{
					if (scanline % 30 == 0 && ilon % 3 == 0)
					{
						c = palette.White;
					}
					else if (ilon % 30 == 0 && scanline % 3 == 0)
					{
						c = palette.White;
					}
				}

				if (TerminatorToggle)
				{
					double crossingLat = Math.Atan(gamma * Math.Sin(Mathf.Deg2Rad * (ilon - 180) - Mathf.Deg2Rad * sunLonCenter));

					if (sunLatCenter >= 0)
					{
						if (scanline - 90 < crossingLat * Mathf.Rad2Deg)
							c = palette.lerp(c, palette.Black, 0.5f);
					}
					else
					{
						if (scanline - 90 > crossingLat * Mathf.Rad2Deg)
							c = palette.lerp(c, palette.Black, 0.5f);
					}
				}
				else
				{
					if (type != SCANtype.Nothing)
					{
						if (!SCANUtil.isCoveredByAll(ilon, scanline, data, type))
							c = palette.lerp(c, palette.Black, 0.5f);
					}
				}

				cols_height_map_small[ilon] = c;
			}

			map_small.SetPixels32(0, scanline, 360, 1, cols_height_map_small);

			if (apply)
			{
				if (scanline < 179)
					map_small.SetPixels32(0, scanline + 1, 360, 1, palette.small_redline);
			}

			scanline++;

			if (apply || scanline >= 180)
				map_small.Apply();

			if (scanline >= 180)
				scanline = 0;
		}

		private void drawBiomeMap(SCANtype type, bool apply)
		{
			bool biomeMap = data.Body.BiomeMap != null;

			if (biomeBuilding)
				buildBiomeCache();

			if (scanline == 0 && TerminatorToggle)
			{
				double sunLon = data.Body.GetLongitude(Planetarium.fetch.Sun.position, false);
				double sunLat = data.Body.GetLatitude(Planetarium.fetch.Sun.position, false);

				sunLatCenter = SCANUtil.fixLatShift(sunLat);

				if (sunLatCenter >= 0)
					sunLonCenter = SCANUtil.fixLonShift(sunLon + 90);
				else
					sunLonCenter = SCANUtil.fixLonShift(sunLon - 90);

				gamma = Math.Abs(sunLatCenter) < 0.55 ? 100 : Math.Tan(Mathf.Deg2Rad * (90 - Math.Abs(sunLatCenter)));
			}

			for (int ilon = 0; ilon < 360; ilon++)
			{
				if (!biomeMap)
				{
					cols_height_map_small[ilon] = palette.lerp(palette.black, palette.white, UnityEngine.Random.value);
					continue;
				}

				Color32 c = biomeCache[scanline * 360 + ilon];
				if (!SCANUtil.isCovered(ilon, scanline, data, SCANtype.Biome))
				{
					c = palette.Grey;
				}

				if (TerminatorToggle)
				{
					double crossingLat = Math.Atan(gamma * Math.Sin(Mathf.Deg2Rad * (ilon - 180) - Mathf.Deg2Rad * sunLonCenter));

					if (sunLatCenter >= 0)
					{
						if (scanline - 90 < crossingLat * Mathf.Rad2Deg)
							c = palette.lerp(c, palette.Black, 0.5f);
					}
					else
					{
						if (scanline - 90 > crossingLat * Mathf.Rad2Deg)
							c = palette.lerp(c, palette.Black, 0.5f);
					}
				}
				else
				{
					if (type != SCANtype.Nothing)
					{
						if (!SCANUtil.isCoveredByAll(ilon, scanline, data, type))
							c = palette.lerp(c, palette.Black, 0.5f);
					}
				}

				cols_height_map_small[ilon] = c;
			}

			map_small.SetPixels32(0, scanline, 360, 1, cols_height_map_small);

			if (apply)
			{
				if (scanline < 179)
					map_small.SetPixels32(0, scanline + 1, 360, 1, palette.small_redline);
			}

			scanline++;

			if (apply || scanline >= 180)
				map_small.Apply();

			if (scanline >= 180)
				scanline = 0;
		}

		private void buildBiomeCache()
		{
			for (int i = 0; i < 360; i++)
			{
				double index = SCANUtil.getBiomeIndexFraction(data.Body, i - 180, scanline - 90);
				Color32 c = palette.Grey;

				if (SCAN_Settings_Config.Instance.SmallMapBiomeBorder && ((i > 0 && biomeIndex[i - 1] != index) || (scanline > 0 && biomeIndex[i] != index)))
					c = palette.White;
				else if (SCAN_Settings_Config.Instance.SmallMapStockBiomes)
					c = SCANUtil.getBiome(data.Body, i - 180, scanline - 90).mapColor;
				else
					c = palette.lerp(SCANcontroller.controller.lowBiomeColor32, SCANcontroller.controller.highBiomeColor32, (float)index);

				biomeCache[scanline * 360 + i] = c;

				biomeIndex[i] = index;
			}

			if (scanline >= 179)
				biomeBuilding = false;
		}

		internal void resetImages()
		{
			for (int y = 0; y < map_small.height; y++)
			{
				for (int x = 0; x < map_small.width; x++)
				{
					if ((x % 30 == 0 && y % 3 > 0) || (y % 30 == 0 && x % 3 > 0))
					{
						map_small.SetPixel(x, y, palette.white);
					}
					else
					{
						map_small.SetPixel(x, y, palette.grey);
					}
				}
			}

			map_small.Apply();

			if (SCANcontroller.controller.mainMapBiome)
			{
				biomeBuilding = true;
				scanline = 0;
			}
		}

		public void ResetPosition()
		{
			SCAN_Settings_Config.Instance.MainMapPosition = new Vector2(100, -200);

			if (uiElement != null)
				uiElement.SetPosition(SCAN_Settings_Config.Instance.MainMapPosition);
		}
	}
}
