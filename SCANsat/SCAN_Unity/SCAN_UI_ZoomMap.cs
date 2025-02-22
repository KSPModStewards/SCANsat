#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_UI_ZoomMap - UI control object for SCANsat zoom map
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using SCANsat.SCAN_Toolbar;
using SCANsat.Unity.Interfaces;
using SCANsat.Unity;
using SCANsat.Unity.Unity;
using SCANsat.SCAN_Data;
using SCANsat.SCAN_Map;
using SCANsat.SCAN_UI.UI_Framework;
using KSP.UI;
using KSP.Localization;
using FinePrint;
using FinePrint.Utilities;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANcolorUtil;

namespace SCANsat.SCAN_Unity
{
	public class SCAN_UI_ZoomMap : ISCAN_ZoomMap
	{

		private bool _isVisible;

		private static SCANmap spotmap;
		private static CelestialBody body;
		private SCANdata data;
		private Vessel vessel;
		private bool updateMap;
		private bool rebuilding;
		private bool narrowBand;
		private StringBuilder infoString = new StringBuilder();
		private System.Random gen;
		private bool _inputLock;
		private const string controlLock = "SCANsatZoom";

		private bool initialized;

		private float terrainMin;
		private float terrainMax;

		private float resourceMin;
		private float resourceMax;

		private SCANresourceGlobal currentResource;
		private List<SCANresourceGlobal> resources;

		private List<CBAttributeMapSO.MapAttribute> biomes = new List<CBAttributeMapSO.MapAttribute>();

		private List<Vessel> mapFlags = new List<Vessel>();

		private SCAN_ZoomMap uiElement;

		private Texture2D resourceLegend;
		private const int RESOURCELEGENDWIDTH = 90;

		private const int orbitSteps = 80;
		private const float minZoom = 2;
		private const float maxZoom = 1000;
		private const float DefaultMapScale = 10f;
		private List<SimpleLabelInfo> orbitLabels = new List<SimpleLabelInfo>();
		private Dictionary<string, MapLabelInfo> orbitMapLabels = new Dictionary<string, MapLabelInfo>();
		private const string Aplabel = "Ap";
		private const string Pelabel = "Pe";
		private const string Escapelabel = "Escape";
		private const string Encounterlabel = "Encounter";
		private const string Manlabel = "Man";
		private const string ManAplabel = "ManAp";
		private const string ManPelabel = "ManPe";
		private const string ManEscapelabel = "ManEscape";
		private const string ManEncounterlabel = "ManEncounter";

		private static SCAN_UI_ZoomMap instance;

		public static SCAN_UI_ZoomMap Instance
		{
			get { return instance; }
		}

		public static CelestialBody Body
		{
			get { return body; }
		}

		public SCAN_UI_ZoomMap()
		{
			instance = this;

			resources = SCANcontroller.setLoadedResourceList();

			GameEvents.onVesselChange.Add(vesselChange);
			GameEvents.onVesselWasModified.Add(vesselChange);
			GameEvents.onVesselSOIChanged.Add(soiChange);

			gen = new System.Random(Environment.TickCount.GetHashCode());

			initializeMap();
		}

		private void vesselChange(Vessel V)
		{
			vessel = FlightGlobals.ActiveVessel;

			if (!_isVisible || uiElement == null)
			{
				return;
			}

			uiElement.RefreshIcons();
		}

		private void soiChange(GameEvents.HostedFromToAction<Vessel, CelestialBody> action)
		{
			if (!_isVisible || uiElement == null)
			{
				return;
			}

			uiElement.RefreshIcons();

			updateMap = true;
		}

		public void Open(bool v, double lat = 0, double lon = 0, SCANmap m = null)
		{
			if (uiElement != null)
			{
				uiElement.gameObject.SetActive(false);
				MonoBehaviour.DestroyImmediate(uiElement.gameObject);
			}

			uiElement = GameObject.Instantiate(SCAN_UI_Loader.ZoomMapPrefab).GetComponent<SCAN_ZoomMap>();

			if (uiElement == null)
			{
				return;
			}

			uiElement.transform.SetParent(UIMasterController.Instance.dialogCanvas.transform, false);

			vessel = FlightGlobals.ActiveVessel;

			if (v || VesselLock)
			{
				setToVessel();
			}
			else
			{
				setToPosition(lat, lon, m);
			}

			if (OrbitToggle && ShowOrbit)
			{
				Orbit o = vessel.orbit;

				orbitLabels.Clear();

				for (int i = 0; i < orbitSteps * 3; i++)
				{
					orbitLabels.Add(new SimpleLabelInfo(10, SCAN_UI_Loader.PlanetIcon));
				}

				if (!vessel.LandedOrSplashed)
				{
					UpdateOrbitIcons(o);
				}
			}

			uiElement.setMap(this);

			updateMap = true;

			_isVisible = true;

			if (HighLogic.LoadedSceneIsFlight && SCAN_Settings_Config.Instance.StockToolbar && SCAN_Settings_Config.Instance.ToolbarMenu)
			{
				if (SCANappLauncher.Instance != null && SCANappLauncher.Instance.UIElement != null)
				{
					SCANappLauncher.Instance.UIElement.SetZoomMapToggle(true);
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
					SCANappLauncher.Instance.UIElement.SetZoomMapToggle(false);
				}
			}

			uiElement = null;
		}

		private void initializeMap()
		{
			if (HighLogic.LoadedSceneIsFlight)
			{
				vessel = FlightGlobals.ActiveVessel;
			}

			if (body == null)
			{
				if (vessel == null)
				{
					body = FlightGlobals.Bodies[1];
				}
				else
				{
					body = vessel.mainBody;
				}
			}

			data = SCANUtil.getData(body);

			if (data == null)
			{
				data = new SCANdata(body);
				SCANcontroller.controller.addToBodyData(body, data);
			}

			if (spotmap == null)
			{
				spotmap = new SCANmap(body, false, mapSource.ZoomMap);

				mapType t = mapType.Altimetry;

				try
				{
					t = (mapType)Enum.Parse(typeof(mapType), SCANcontroller.controller.zoomMapType, true);
				}
				catch (Exception e)
				{
					SCANUtil.SCANlog("Error in parsing map projection and/or type\n{0}", e);

					t = mapType.Altimetry;
				}

				spotmap.Projection = MapProjection.Orthographic;
				spotmap.MType = t;
				spotmap.ResourceActive = SCANcontroller.controller.zoomMapResourceOn;
				spotmap.ColorMap = SCANcontroller.controller.zoomMapColor;

				if (SCAN_Settings_Config.Instance.ZoomMapSize.x % 2 != 0)
				{
					SCAN_Settings_Config.Instance.ZoomMapSize.x += 1;
				}

				if (SCAN_Settings_Config.Instance.ZoomMapSize.y % 2 != 0)
				{
					SCAN_Settings_Config.Instance.ZoomMapSize.y += 1;
				}

				if (SCAN_Settings_Config.Instance.ZoomMapSize.x % 4 != 0)
				{
					SCAN_Settings_Config.Instance.ZoomMapSize.x += 2;
				}

				if (SCAN_Settings_Config.Instance.ZoomMapSize.y % 4 != 0)
				{
					SCAN_Settings_Config.Instance.ZoomMapSize.y += 2;
				}

				spotmap.setSize(SCAN_Settings_Config.Instance.ZoomMapSize);
			}

			spotmap.setBody(body);

			currentResource = AssignResource(SCANcontroller.controller.zoomMapResource);

			if (currentResource != null)
			{
				spotmap.Resource = currentResource;
			}

			AddOrbitMapLabels();
		}

		private void initializeMapCenter(double lat, double lon, CelestialBody b)
		{
			SCANdata dat = SCANUtil.getData(b);

			if (dat == null)
			{
				dat = new SCANdata(b);
			}

			data = dat;

			if (body != b)
			{
				body = data.Body;

				spotmap.setBody(body);
			}

			if (SCANconfigLoader.GlobalResource)
			{
				currentResource = AssignResource(SCANcontroller.controller.zoomMapResource);

				if (currentResource != null)
				{
					spotmap.Resource = currentResource;
				}
			}

			if (ResourceToggle && currentResource != null)
			{
				checkForScanners();
			}

			if (SCANcontroller.controller.zoomMapZoomPersist)
			{
				spotmap.MapScale = SCANcontroller.controller.zoomMapZoom;
			}
			else
			{
				spotmap.MapScale = DefaultMapScale;
			}

			spotmap.centerAround(lon, lat);

			calcTerrainLimits();

			mapType t = mapType.Altimetry;

			try
			{
				t = (mapType)Enum.Parse(typeof(mapType), SCANcontroller.controller.zoomMapType, true);
			}
			catch (Exception e)
			{
				SCANUtil.SCANlog("Error in parsing map projection and/or type\n{0}", e);

				t = mapType.Altimetry;
			}

			spotmap.ResourceActive = SCANcontroller.controller.zoomMapResourceOn;
			spotmap.ColorMap = SCANcontroller.controller.zoomMapColor;
			spotmap.Terminator = SCANcontroller.controller.zoomMapTerminator;

			rebuilding = true;

			spotmap.resetMap(t, false, ResourceToggle, narrowBand);
		}

		private void resetMap(double lat = 0, double lon = 0, bool withCenter = false)
		{
			if (VesselLock)
			{
				resetMapToVessel();
			}
			else
			{
				resetMap(withCenter, lat, lon);
			}

			SCANcontroller.controller.zoomMapZoom = (float)spotmap.MapScale;
		}

		public void resetMap(bool withCenter, double lat, double lon)
		{
			if (withCenter)
			{
				spotmap.centerAround(lon, lat);
			}
			else
			{
				spotmap.centerAround(spotmap.CenteredLong, spotmap.CenteredLat);
			}

			if (ResourceToggle)
			{
				checkForScanners();
			}

			calcTerrainLimits();

			rebuilding = true;

			spotmap.resetMap(ResourceToggle, narrowBand);
		}

		public void resetMapToVessel()
		{
			vessel = FlightGlobals.ActiveVessel;

			if (body != vessel.mainBody)
			{
				body = vessel.mainBody;

				SCANdata dat = SCANUtil.getData(body);

				if (dat == null)
				{
					dat = new SCANdata(body);
				}

				data = dat;
				body = data.Body;

				spotmap.setBody(body);
			}

			if (SCANconfigLoader.GlobalResource)
			{
				if (currentResource != null)
				{
					currentResource.CurrentBodyConfig(body.bodyName);
				}
			}

			resetMap(true, SCANUtil.fixLatShift(vessel.latitude), SCANUtil.fixLonShift(vessel.longitude));
		}

		protected void calcTerrainLimits()
		{
			bool terrainCalc = spotmap.MType == mapType.Altimetry || spotmap.MType == mapType.Biome;

			int w = spotmap.MapWidth / 4;
			int h = spotmap.MapHeight / 4;

			resourceMax = 0;
			resourceMin = 100;

			terrainMax = -200000;
			terrainMin = 100000;

			for (int i = 0; i < spotmap.MapHeight; i += 4)
			{
				for (int j = 0; j < spotmap.MapWidth; j += 4)
				{
					double lat = (i * 1.0f / spotmap.MapScale) - 90f + spotmap.Lat_Offset;
					double lon = (j * 1.0f / spotmap.MapScale) - 180f + spotmap.Lon_Offset;
					double la = lat, lo = lon;
					lat = spotmap.unprojectLatitude(lo, la);
					lon = spotmap.unprojectLongitude(lo, la);

					if (double.IsNaN(lon) || double.IsNaN(lat) || lon < -180 || lon >= 180 || lat < -90 && lat >= 90)
					{
						continue;
					}

					if (terrainCalc)
					{
						float terrain = (float)SCANUtil.getElevation(body, lon, lat);

						if (terrain < terrainMin)
						{
							terrainMin = terrain;
						}

						if (terrain > terrainMax)
						{
							terrainMax = terrain;
						}
					}

					if (currentResource != null)
					{
						float resource = SCANUtil.ResourceOverlay(lat, lon, currentResource.Name, body, SCAN_Settings_Config.Instance.BiomeLock) * 100f;

						if (resource < resourceMin)
						{
							resourceMin = resource;
						}

						if (resource > resourceMax)
						{
							resourceMax = resource;
						}
						//SCANUtil.SCANlog("Check Location: Lat: {3} x Long: {4}\nResource: {0} - Min: {1} - Max: {2}"
						//    , resource.ToString("F2"), resourceMin.ToString("F2"), resourceMax.ToString("F2"), lat.ToString("F2"), lon.ToString("F2") );
					}
					else
					{
						resourceMax = 100;
						resourceMin = 0;
					}
				}
			}

			if (terrainMin > terrainMax)
			{
				terrainMin = terrainMax - 1f;
			}

			if (terrainMin == terrainMax)
			{
				terrainMin = terrainMax - 1f;
			}

			if (currentResource != null && currentResource.CurrentBody != null && resourceMin < currentResource.CurrentBody.MinValue)
			{
				resourceMin = currentResource.CurrentBody.MinValue;
			}

			if (resourceMin >= resourceMax)
			{
				resourceMax = resourceMin + 1f;
			}

			if (resourceMin < 0)
			{
				resourceMin = 0;
			}

			if (currentResource != null && currentResource.CurrentBody != null && resourceMax > currentResource.CurrentBody.MaxValue)
			{
				resourceMax = currentResource.CurrentBody.MaxValue;
			}

			if (resourceMin >= resourceMax)
			{
				resourceMin = resourceMax - 1f;
			}

			spotmap.setCustomRange(terrainMin, terrainMax, resourceMin, resourceMax);
		}

		private void checkForScanners()
		{
			narrowBand = SCANuiUtil.narrowBandInOrbit(body, Math.Abs(spotmap.CenteredLat) - 5, currentResource);

			if (!narrowBand || currentResource == null)
			{
				spotmap.Resource = null;
			}
			else
			{
				spotmap.Resource = currentResource;
				spotmap.Resource.CurrentBodyConfig(body.bodyName);
			}
		}

		private void AddOrbitMapLabels()
		{
			orbitMapLabels.Add(Aplabel, new MapLabelInfo()
			{
				label = "",
				image = SCAN_UI_Loader.APMarker,
				pos = new Vector2(),
				baseColor = palette.cb_skyBlue,
				flash = false,
				width = 28,
				show = false
			});

			orbitMapLabels.Add(Pelabel, new MapLabelInfo()
			{
				label = "",
				image = SCAN_UI_Loader.PEMarker,
				pos = new Vector2(),
				baseColor = palette.cb_skyBlue,
				flash = false,
				width = 28,
				show = false
			});

			orbitMapLabels.Add(Escapelabel, new MapLabelInfo()
			{
				label = "",
				image = SCAN_UI_Loader.ExitMarker,
				pos = new Vector2(),
				baseColor = palette.cb_skyBlue,
				flash = false,
				width = 26,
				show = false
			});

			orbitMapLabels.Add(Encounterlabel, new MapLabelInfo()
			{
				label = "",
				image = SCAN_UI_Loader.EncounterMarker,
				pos = new Vector2(),
				baseColor = palette.cb_skyBlue,
				flash = false,
				width = 26,
				show = false
			});

			orbitMapLabels.Add(Manlabel, new MapLabelInfo()
			{
				label = "",
				image = SCAN_UI_Loader.ManeuverMarker,
				pos = new Vector2(),
				baseColor = palette.cb_reddishPurple,
				flash = false,
				width = 24,
				show = false
			});

			orbitMapLabels.Add(ManEscapelabel, new MapLabelInfo()
			{
				label = "",
				image = SCAN_UI_Loader.ExitMarker,
				pos = new Vector2(),
				baseColor = palette.cb_reddishPurple,
				flash = false,
				width = 26,
				show = false
			});

			orbitMapLabels.Add(ManEncounterlabel, new MapLabelInfo()
			{
				label = "",
				image = SCAN_UI_Loader.EncounterMarker,
				pos = new Vector2(),
				baseColor = palette.cb_reddishPurple,
				flash = false,
				width = 26,
				show = false
			});

			orbitMapLabels.Add(ManAplabel, new MapLabelInfo()
			{
				label = "",
				image = SCAN_UI_Loader.APMarker,
				pos = new Vector2(),
				baseColor = palette.cb_reddishPurple,
				flash = false,
				width = 28,
				show = false
			});

			orbitMapLabels.Add(ManPelabel, new MapLabelInfo()
			{
				label = "",
				image = SCAN_UI_Loader.PEMarker,
				pos = new Vector2(),
				baseColor = palette.cb_reddishPurple,
				flash = false,
				width = 28,
				show = false
			});
		}

		public void OnDestroy()
		{
			SCANcontroller.controller.unloadPQS(spotmap.Body, mapSource.BigMap);

			GameEvents.onVesselChange.Remove(vesselChange);
			GameEvents.onVesselWasModified.Remove(vesselChange);
			GameEvents.onVesselSOIChanged.Remove(soiChange);

			if (uiElement != null)
			{
				uiElement.gameObject.SetActive(false);
				MonoBehaviour.Destroy(uiElement.gameObject);
			}

			if (resourceLegend != null)
			{
				GameObject.Destroy(resourceLegend);
				resourceLegend = null;
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
			if (!_isVisible || data == null || spotmap == null)
			{
				return;
			}

			if (uiElement == null)
			{
				return;
			}

			if (!spotmap.isMapComplete())
			{
				if (SCAN_Settings_Config.Instance.MapGenerationSpeed > 2)
				{
					spotmap.getPartialMap(false);
					spotmap.getPartialMap(false);
				}

				if (SCAN_Settings_Config.Instance.MapGenerationSpeed > 1)
				{
					spotmap.getPartialMap(false);
				}

				spotmap.getPartialMap(true);
			}
			else if (rebuilding)
			{
				rebuilding = false;

				uiElement.ResetRefresh();
			}

			if (OrbitToggle && ShowOrbit)
			{
				if (vessel != null && vessel.mainBody == body && !vessel.LandedOrSplashed)
				{
					Orbit o = vessel.orbit;

					UpdateOrbitIcons(o);
				}
			}

			if (updateMap)
			{
				updateMap = false;
				uiElement.UpdateMapTexture(spotmap.Map);
			}
		}

		private void UpdateOrbitIcons(Orbit o)
		{
			double startUT = Planetarium.GetUniversalTime();
			double UT = startUT;
			Color col;

			for (int i = 0; i < orbitSteps * 2; i++)
			{
				if (i > orbitLabels.Count - 1)
				{
					break;
				}

				SimpleLabelInfo info = orbitLabels[i];

				if (info == null)
				{
					continue;
				}

				int k = i - orbitSteps;

				if (k < 0)
				{
					UT = startUT - (orbitSteps + k) * (o.period / orbitSteps);
				}
				else
				{
					UT = startUT + k * o.period * (1f / orbitSteps);
				}

				if (double.IsNaN(UT))
				{
					info.show = false;
					continue;
				}

				if (UT < o.StartUT && o.StartUT != startUT)
				{
					info.show = false;
					continue;
				}

				if (UT > o.EndUT)
				{
					info.show = false;
					continue;
				}

				if (double.IsNaN(o.getObtAtUT(UT)))
				{
					info.show = false;
					continue;
				}

				Vector3d pos = o.getPositionAtUT(UT);

				double rotation = 0;

				if (body.rotates)
				{
					rotation = (360 * ((UT - startUT) / body.rotationPeriod)) % 360;
				}

				double alt = body.GetAltitude(pos);

				if (alt < 0)
				{
					if (k < 0)
					{
						for (int j = k; j < 0; j++)
						{
							orbitLabels[j + orbitSteps].show = false;
						}

						i = orbitSteps;
						continue;
					}

					for (int j = k; j < orbitSteps; j++)
					{
						orbitLabels[j + orbitSteps].show = false;
					}

					break;
				}

				double lo = body.GetLongitude(pos) - rotation;
				double la = body.GetLatitude(pos);

				double lon = (spotmap.projectLongitude(lo, la) + 180) % 360;
				double lat = (spotmap.projectLatitude(lo, la) + 90) % 180;

				lon = spotmap.scaleLongitude(lon);
				lat = spotmap.scaleLatitude(lat);

				if (lat < 0 || lon < 0 || lat > 180 || lon > 360)
				{
					info.show = false;
					continue;
				}

				lon = lon * spotmap.MapWidth / 360;
				lat = lat * spotmap.MapHeight / 180;

				if (k < 0)
				{
					col = palette.cb_orange;
				}
				else
				{
					if (body.atmosphere && body.atmosphereDepth >= alt)
					{
						col = palette.cb_reddishPurple;
					}
					else
					{
						col = palette.cb_skyBlue;
					}
				}

				info.show = true;
				info.color = col;
				info.pos = new Vector2((float)lon, (float)lat);
			}

			MapLabelInfo Ap = orbitMapLabels[Aplabel];

			Vector2 labelPos;

			if (o.ApA > 0 && mapPosAtT(o, o.timeToAp, startUT, out labelPos))
			{
				Ap.show = true;
				Ap.pos = labelPos;
				Ap.label = o.ApA.ToString("N0");
			}
			else
			{
				Ap.show = false;
			}

			orbitMapLabels[Aplabel] = Ap;

			MapLabelInfo Pe = orbitMapLabels[Pelabel];

			if (o.PeA > 0 && mapPosAtT(o, o.timeToPe, startUT, out labelPos))
			{
				Pe.show = true;
				Pe.pos = labelPos;
				Pe.label = o.PeA.ToString("N0");
			}
			else
			{
				Pe.show = false;
			}

			orbitMapLabels[Pelabel] = Pe;

			if (o.patchEndTransition == Orbit.PatchTransitionType.ESCAPE && mapPosAtT(o, o.EndUT, startUT, out labelPos))
			{
				MapLabelInfo Esc = orbitMapLabels[Escapelabel];

				Esc.show = true;
				Esc.pos = labelPos;

				orbitMapLabels[Escapelabel] = Esc;
			}
			else if (o.patchEndTransition == Orbit.PatchTransitionType.ENCOUNTER && mapPosAtT(o, o.EndUT, startUT, out labelPos))
			{
				MapLabelInfo Enc = orbitMapLabels[Encounterlabel];

				Enc.show = true;
				Enc.pos = labelPos;

				orbitMapLabels[Encounterlabel] = Enc;
			}
			else
			{
				MapLabelInfo Esc = orbitMapLabels[Escapelabel];
				Esc.show = false;
				orbitMapLabels[Escapelabel] = Esc;

				MapLabelInfo Enc = orbitMapLabels[Encounterlabel];
				Enc.show = false;
				orbitMapLabels[Encounterlabel] = Enc;
			}

			if (vessel.patchedConicSolver != null)
			{
				if (vessel.patchedConicSolver.maneuverNodes.Count > 0)
				{
					ManeuverNode n = vessel.patchedConicSolver.maneuverNodes[0];

					if (n.patch == o && n.nextPatch != null && n.nextPatch.activePatch && n.UT > startUT - o.period && mapPosAtT(o, n.UT - startUT, startUT, out labelPos))
					{
						MapLabelInfo Man = orbitMapLabels[Manlabel];

						Man.show = true;
						Man.pos = labelPos;

						orbitMapLabels[Manlabel] = Man;

						Orbit next = n.nextPatch;

						for (int i = 0; i < orbitSteps; i++)
						{
							SimpleLabelInfo info = orbitLabels[orbitSteps * 2 + i];

							double T = n.UT - startUT + i * next.period / orbitSteps;

							if (T + startUT > next.EndUT)
							{
								for (int j = i; j < orbitSteps; j++)
								{
									orbitLabels[orbitSteps * 2 + j].show = false;
								}

								info.show = false;

								break;
							}

							if (mapPosAtT(next, T, startUT, out labelPos))
							{
								info.color = palette.cb_reddishPurple;
								info.show = true;
								info.pos = labelPos;
							}
							else
							{
								info.show = false;
								continue;
							}
						}

						if (next.patchEndTransition == Orbit.PatchTransitionType.ESCAPE)
						{
							MapLabelInfo ManEsc = orbitMapLabels[ManEscapelabel];

							ManEsc.show = true;
							ManEsc.pos = labelPos;

							orbitMapLabels[ManEscapelabel] = ManEsc;
						}
						else if (next.patchEndTransition == Orbit.PatchTransitionType.ENCOUNTER)
						{
							MapLabelInfo ManEnc = orbitMapLabels[ManEncounterlabel];

							ManEnc.show = true;
							ManEnc.pos = labelPos;

							orbitMapLabels[ManEncounterlabel] = ManEnc;
						}
						else
						{
							MapLabelInfo ManEsc = orbitMapLabels[ManEscapelabel];
							ManEsc.show = false;
							orbitMapLabels[ManEscapelabel] = ManEsc;

							MapLabelInfo ManEnc = orbitMapLabels[ManEncounterlabel];
							ManEnc.show = false;
							orbitMapLabels[ManEncounterlabel] = ManEnc;
						}

						MapLabelInfo ManAp = orbitMapLabels[ManAplabel];

						if (next.timeToAp > 0 && n.UT + next.timeToAp < next.EndUT && mapPosAtT(next, n.UT - startUT + next.timeToAp, startUT, out labelPos))
						{
							ManAp.show = true;
							ManAp.pos = labelPos;
						}
						else
						{
							ManAp.show = false;
						}

						orbitMapLabels[ManAplabel] = ManAp;

						MapLabelInfo ManPe = orbitMapLabels[ManPelabel];

						if (next.timeToPe > 0 && n.UT + next.timeToPe < next.EndUT && mapPosAtT(next, n.UT - startUT + next.timeToPe, startUT, out labelPos))
						{
							ManPe.show = true;
							ManPe.pos = labelPos;
						}
						else
						{
							ManPe.show = false;
						}

						orbitMapLabels[ManPelabel] = ManPe;

					}
					else
					{
						MapLabelInfo Man = orbitMapLabels[Manlabel];
						Man.show = false;
						orbitMapLabels[Manlabel] = Man;

						MapLabelInfo ManEsc = orbitMapLabels[ManEscapelabel];
						ManEsc.show = false;
						orbitMapLabels[ManEscapelabel] = ManEsc;

						MapLabelInfo ManEnc = orbitMapLabels[ManEncounterlabel];
						ManEnc.show = false;
						orbitMapLabels[ManEncounterlabel] = ManEnc;

						MapLabelInfo ManAp = orbitMapLabels[ManAplabel];
						ManAp.show = false;
						orbitMapLabels[ManAplabel] = ManAp;

						MapLabelInfo ManPe = orbitMapLabels[ManPelabel];
						ManPe.show = false;
						orbitMapLabels[ManPelabel] = ManPe;

						for (int i = 0; i < orbitSteps; i++)
						{
							SimpleLabelInfo info = orbitLabels[orbitSteps * 2 + i];
							info.show = false;
						}
					}

				}
				else
				{
					MapLabelInfo Man = orbitMapLabels[Manlabel];
					Man.show = false;
					orbitMapLabels[Manlabel] = Man;

					MapLabelInfo ManEsc = orbitMapLabels[ManEscapelabel];
					ManEsc.show = false;
					orbitMapLabels[ManEscapelabel] = ManEsc;

					MapLabelInfo ManEnc = orbitMapLabels[ManEncounterlabel];
					ManEnc.show = false;
					orbitMapLabels[ManEncounterlabel] = ManEnc;

					MapLabelInfo ManAp = orbitMapLabels[ManAplabel];
					ManAp.show = false;
					orbitMapLabels[ManAplabel] = ManAp;

					MapLabelInfo ManPe = orbitMapLabels[ManPelabel];
					ManPe.show = false;
					orbitMapLabels[ManPelabel] = ManPe;

					for (int i = 0; i < orbitSteps; i++)
					{
						SimpleLabelInfo info = orbitLabels[orbitSteps * 2 + i];
						info.show = false;
					}
				}

			}
		}

		private double meanForTrue(double TA, double e)
		{
			TA = TA * Mathf.Deg2Rad;

			double EA = Math.Acos((e + Math.Cos(TA)) / (1 + e * Math.Cos(TA)));

			if (TA > Math.PI)
			{
				EA = 2 * Math.PI - EA;
			}

			double MA = EA - e * Math.Sin(EA);

			// the mean anomaly isn't really an angle, but I'm a simple person
			return MA * Mathf.Rad2Deg;
		}

		private bool mapPosAtT(Orbit o, double dT, double startUT, out Vector2 labelPos)
		{
			labelPos = new Vector2();

			double UT = startUT + dT;

			if (double.IsNaN(UT))
			{
				return false;
			}

			try
			{
				if (double.IsNaN(o.getObtAtUT(UT)))
				{
					return false;
				}

				Vector3d pos = o.getPositionAtUT(UT);
				double rotation = 0;

				if (body.rotates)
				{
					rotation = (360 * (dT / vessel.mainBody.rotationPeriod)) % 360;
				}

				double lo = (body.GetLongitude(pos) - rotation);
				double la = (body.GetLatitude(pos));

				double lon = (spotmap.projectLongitude(lo, la) + 180) % 360;
				double lat = (spotmap.projectLatitude(lo, la) + 90) % 180;

				lat = spotmap.scaleLatitude(lat);
				lon = spotmap.scaleLongitude(lon);

				if (lat < 0 || lon < 0 || lat > 180 || lon > 360)
				{
					return false;
				}

				lon = lon * spotmap.MapWidth / 360;
				lat = lat * spotmap.MapHeight / 180;

				labelPos = new Vector2((float)lon, (float)lat);

				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public void RefreshIcons()
		{
			if (uiElement != null)
			{
				uiElement.RefreshIcons();
			}
		}

		public void setToPosition(double lat, double lon, SCANmap map)
		{
			SCANcontroller.controller.zoomMapType = map.MType.ToString();
			SCANcontroller.controller.zoomMapColor = map.ColorMap;
			SCANcontroller.controller.zoomMapResource = map.Resource.Name;
			SCANcontroller.controller.zoomMapResourceOn = map.ResourceActive;

			initializeMapCenter(SCANUtil.fixLatShift(lat), SCANUtil.fixLonShift(lon), map.Body);

			initialized = true;
		}

		public void setToVessel()
		{
			if (vessel == null)
			{
				initializeMapCenter(0, 0, FlightGlobals.GetHomeBody());
				return;
			}
			if (initialized && !VesselLock)
			{
				return;
			}

			initializeMapCenter(SCANUtil.fixLatShift(vessel.latitude), SCANUtil.fixLonShift(vessel.longitude), vessel.mainBody);

			initialized = true;
		}

		public string Version
		{
			get { return SCANmainMenuLoader.SCANsatVersion; }
		}

		public string CurrentMapType
		{
			get { return SCANcontroller.controller.zoomMapType; }
			set
			{
				mapType t;

				try
				{
					t = (mapType)Enum.Parse(typeof(mapType), value, true);

					SCANcontroller.controller.zoomMapType = value;
					spotmap.MType = t;
					resetMap();
				}
				catch (Exception e)
				{
					SCANUtil.SCANlog("Error in parsing map type\n{0}", e);
				}
			}
		}

		public string CurrentResource
		{
			get { return SCANUtil.displayNameFromResource(SCANcontroller.controller.zoomMapResource); }
			set
			{
				SCANcontroller.controller.zoomMapResource = SCANUtil.resourceFromDisplayName(value);

				currentResource = AssignResource(SCANcontroller.controller.zoomMapResource);

				if (currentResource == null)
				{
					spotmap.Resource = null;
				}
				else
				{
					spotmap.Resource = currentResource;
				}
			}
		}

		public string ZoomLevelText
		{
			get
			{
				double zoom = spotmap.MapScale;

				return (zoom > 999 ? zoom.ToString("N0") : zoom.ToString("N2")) + "X";
			}
		}

		public string MapCenterText
		{
			get { return SCANuiUtil.toDMS(spotmap.CenteredLat, spotmap.CenteredLong); }
		}

		public string RandomWaypoint
		{
			get { return StringUtilities.GenerateSiteName(gen.Next(), body, false, true); }
		}

		private SCANresourceGlobal AssignResource(string resource)
		{
			SCANresourceGlobal r = currentResource;

			if (r == null || r.Name != resource)
			{
				for (int i = resources.Count - 1; i >= 0; i--)
				{
					SCANresourceGlobal res = resources[i];

					if (res.Name != resource)
					{
						continue;
					}

					r = res;
					break;
				}
			}

			if (r == null)
			{
				r = SCANcontroller.GetFirstResource;
			}

			if (r != null)
			{
				r.CurrentBodyConfig(body.bodyName);
			}

			return r;
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

		public bool Rebuilding
		{
			get
			{
				if (spotmap == null)
				{
					return false;
				}

				return !spotmap.isMapComplete();
			}
		}

		public bool VesselLock
		{
			get { return SCANcontroller.controller.zoomMapVesselLock; }
			set
			{
				SCANcontroller.controller.zoomMapVesselLock = value;

				if (value)
				{
					VesselSync();
				}
			}
		}

		public bool ColorToggle
		{
			get { return SCANcontroller.controller.zoomMapColor; }
			set
			{
				SCANcontroller.controller.zoomMapColor = value;

				spotmap.ColorMap = value;
				resetMap();
			}
		}

		public bool TerminatorToggle
		{
			get { return SCANcontroller.controller.zoomMapTerminator; }
			set
			{
				SCANcontroller.controller.zoomMapTerminator = value;

				spotmap.Terminator = value;
				resetMap();
			}
		}

		public bool OrbitToggle
		{
			get { return SCANcontroller.controller.zoomMapOrbit; }
			set
			{
				SCANcontroller.controller.zoomMapOrbit = value;

				if (value && ShowOrbit)
				{
					Orbit o = vessel.orbit;

					orbitLabels.Clear();

					for (int i = 0; i < orbitSteps * 3; i++)
					{
						orbitLabels.Add(new SimpleLabelInfo(10, SCAN_UI_Loader.PlanetIcon));
					}

					if (!vessel.LandedOrSplashed)
					{
						UpdateOrbitIcons(o);
					}
				}

				updateMap = true;
			}
		}

		public bool IconsToggle
		{
			get { return SCANcontroller.controller.zoomMapIcons; }
			set { SCANcontroller.controller.zoomMapIcons = value; }
		}

		public bool LegendToggle
		{
			get { return SCANcontroller.controller.zoomMapLegend; }
			set { SCANcontroller.controller.zoomMapLegend = value; }
		}

		public bool LegendAvailable
		{
			get
			{
				if (WindowState != 0)
				{
					return false;
				}

				switch (spotmap.MType)
				{
					case mapType.Altimetry:
						return body.pqsController != null;
					case mapType.Biome:
						return body.BiomeMap != null && SCAN_Settings_Config.Instance.LegendTooltips;
				}

				return false;
			}
		}

		public bool ResourceToggle
		{
			get { return SCANcontroller.controller.zoomMapResourceOn; }
			set
			{
				SCANcontroller.controller.zoomMapResourceOn = value;

				resetMap();
			}
		}

		public bool OrbitAvailable
		{
			get { return HighLogic.LoadedSceneIsFlight; }
		}

		public bool ShowOrbit
		{
			get
			{
				return HighLogic.LoadedSceneIsFlight
				&& vessel != null
				&& body != null
				&& vessel.mainBody == body
				&& GameVariables.Instance.GetOrbitDisplayMode(
					ScenarioUpgradeableFacilities.GetFacilityLevel(
					SpaceCenterFacility.TrackingStation)
					) == GameVariables.OrbitDisplayMode.PatchedConics;
			}
		}

		public bool ShowWaypoint
		{
			get { return HighLogic.LoadedScene != GameScenes.SPACECENTER; }
		}

		public bool ShowResource
		{
			get { return SCANcontroller.MasterResourceCount > 1; }
		}

		public bool ShowVessel
		{
			get { return HighLogic.LoadedSceneIsFlight; }
		}

		public bool MechJebAvailable
		{
			get { return SCANmainMenuLoader.MechJebLoaded && SCAN_Settings_Config.Instance.MechJebTarget && SCANcontroller.controller.MechJebLoaded; }
		}

		public bool TooltipsOn
		{
			get { return SCAN_Settings_Config.Instance.WindowTooltips; }
		}

		public bool LegendTooltips
		{
			get { return SCAN_Settings_Config.Instance.LegendTooltips; }
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

		public bool ZoomPersist
		{
			get { return SCANcontroller.controller.zoomMapZoomPersist; }
			set { SCANcontroller.controller.zoomMapZoomPersist = value; }
		}

		public int OrbitSteps
		{
			get { return orbitSteps * 3; }
		}

		public int CurrentScene
		{
			get
			{
				switch (HighLogic.LoadedScene)
				{
					case GameScenes.FLIGHT:
						return 0;
					case GameScenes.TRACKSTATION:
						return 1;
					case GameScenes.SPACECENTER:
						return 2;
					default:
						return -1;
				}
			}
		}

		public int WindowState
		{
			get { return SCANcontroller.controller.zoomMapState; }
			set { SCANcontroller.controller.zoomMapState = value; }
		}

		public int MapRefresh
		{
			get { return SCANcontroller.controller.zoomMapRefresh; }
			set { SCANcontroller.controller.zoomMapRefresh = value; }
		}

		public float Scale
		{
			get { return SCAN_Settings_Config.Instance.UIScale; }
		}

		public Sprite WaypointSprite
		{
			get { return SCAN_UI_Loader.WaypointIcon; }
		}

		public Canvas MainCanvas
		{
			get { return UIMasterController.Instance.dialogCanvas; }
		}

		public Canvas TooltipCanvas
		{
			get { return UIMasterController.Instance.tooltipCanvas; }
		}

		public Vector2 Position
		{
			get { return SCAN_Settings_Config.Instance.ZoomMapPosition; }
			set { SCAN_Settings_Config.Instance.ZoomMapPosition = value; }
		}

		public Vector2 Size
		{
			get { return SCAN_Settings_Config.Instance.ZoomMapSize; }
			set
			{
				SCAN_Settings_Config.Instance.ZoomMapSize = value;

				double scale = spotmap.MapScale;
				spotmap.setSize(value);
				spotmap.MapScale = scale;

				resetMap();

				updateMap = true;
			}
		}

		public Texture2D LegendImage
		{
			get
			{
				if (spotmap.MapLegend == null)
				{
					spotmap.MapLegend = new SCANmapLegend();
				}

				if (data == null)
				{
					return null;
				}

				switch (spotmap.MType)
				{
					case mapType.Altimetry:
						return spotmap.MapLegend.getLegend(SCANcontroller.controller.zoomMapColor, data.TerrainConfig);
					case mapType.Biome:
						if (body != null && body.BiomeMap != null && body.BiomeMap.Attributes != null)
						{
							biomes = new List<CBAttributeMapSO.MapAttribute>();

							int w = spotmap.MapWidth / 4;
							int h = spotmap.MapHeight / 4;

							for (int i = 0; i < spotmap.MapHeight; i += 4)
							{
								for (int j = 0; j < spotmap.MapWidth; j += 4)
								{
									double lon = spotmap.Lon_Offset + (j * 1.0f / spotmap.MapScale) - 180;
									double lat = spotmap.Lat_Offset + (i * 1.0f / spotmap.MapScale) - 90;
									double la = lat, lo = lon;
									lat = spotmap.unprojectLatitude(lo, la);
									lon = spotmap.unprojectLongitude(lo, la);

									if (lon < -180 || lon >= 180 || lat < -90 && lat >= 90 || double.IsNaN(lon) || double.IsNaN(lat))
									{
										continue;
									}

									CBAttributeMapSO.MapAttribute biome = SCANUtil.getBiome(body, lon, lat);

									bool add = true;

									for (int b = biomes.Count - 1; b >= 0; b--)
									{
										if (biome != biomes[b])
										{
											continue;
										}

										add = false;
										break;
									}

									if (add)
									{
										biomes.Add(biome);
									}
								}
							}


							return spotmap.MapLegend.getLegend(data, SCANcontroller.controller.zoomMapColor, SCAN_Settings_Config.Instance.BigMapStockBiomes, biomes.ToArray(), true);
						}
						else
						{
							return null;
						}
				}

				return null;
			}
		}

		public IList<string> MapTypes
		{
			get { return new List<string>(3) { "Altimetry", "Slope", "Biome", "Visual" }; }
		}

		public IList<string> Resources
		{
			get
			{
				List<string> rList = new List<string>();

				bool threshold;

				if (!SCAN_Settings_Config.Instance.HideZeroResources)
				{
					threshold = SCANUtil.getCoveragePercentage(data, SCANtype.ResourceLoRes) > (SCAN_Settings_Config.Instance.StockTreshold * 100) || SCANUtil.getCoveragePercentage(data, SCANtype.ResourceHiRes) > (SCAN_Settings_Config.Instance.StockTreshold * 100);
				}
				else
				{
					threshold = true;
				}

				for (int i = 0; i < resources.Count; i++)
				{
					SCANresourceGlobal res = resources[i];

					if (threshold)
					{
						SCANresourceBody resBody = res.getBodyConfig(body.bodyName);

						if (resBody != null)
						{
							if (resBody.DefaultZero)
							{
								continue;
							}
						}
						else if (res.DefaultZero)
						{
							continue;
						}
					}

					rList.Add(res.DisplayName);
				}

				return rList;
			}
			//get { return new List<string>(resources.Select(r => r.DisplayName)); }
		}

		public IList<string> LegendLabels
		{
			get
			{
				if (data == null)
				{
					return null;
				}

				string one = string.Format("|\n{0}", (((int)(terrainMin / 100)) * 100).ToString("N0"));

				string two = string.Format("|\n{0}", (((int)((terrainMin + ((terrainMax - terrainMin) / 2)) / 100)) * 100).ToString("N0"));

				string three = string.Format("|\n{0}", (((int)(terrainMax / 100)) * 100).ToString("N0"));

				return new List<string>(3) { one, two, three };
			}
		}

		public SimpleLabelInfo OrbitInfo(int index)
		{
			if (index < 0 || index >= orbitLabels.Count)
			{
				return null;
			}

			return orbitLabels[index];
		}

		public MapLabelInfo OrbitIconInfo(string id)
		{
			MapLabelInfo info;

			if (OrbitLabelList.TryGetValue(id, out info))
			{
				return info;
			}

			return new MapLabelInfo();
		}

		public Vector2 VesselPosition()
		{
			if (vessel == null)
			{
				return new Vector2();
			}

			return VesselPosition(vessel);
		}

		public Vector2 VesselPosition(Guid id)
		{
			Vessel v = null;

			for (int i = FlightGlobals.Vessels.Count - 1; i >= 0; i--)
			{
				v = FlightGlobals.Vessels[i];

				if (v.id == id)
				{
					break;
				}
			}

			return VesselPosition(v);
		}

		public Vector2 VesselPosition(Vessel v)
		{
			if (v == null)
			{
				return new Vector2();
			}

			double lat = SCANUtil.fixLat(spotmap.projectLatitude(v.longitude, v.latitude));
			double lon = SCANUtil.fixLon(spotmap.projectLongitude(v.longitude, v.latitude));
			lat = spotmap.scaleLatitude(lat);
			lon = spotmap.scaleLongitude(lon);

			if (lat < 0 || lon < 0 || lat > 180 || lon > 360)
			{
				return new Vector2(-1, -1);
			}

			lon = lon * spotmap.MapWidth / 360;
			lat = lat * spotmap.MapHeight / 180;

			return new Vector2((float)lon, (float)lat);
		}

		public Vector2 MapPosition(double lat, double lon)
		{
			double Lat = SCANUtil.fixLat(spotmap.projectLatitude(lon, lat));
			double Lon = SCANUtil.fixLon(spotmap.projectLongitude(lon, lat));
			Lat = spotmap.scaleLatitude(Lat);
			Lon = spotmap.scaleLongitude(Lon);

			if (Lat < 0 || Lon < 0 || Lat > 180 || Lon > 360)
			{
				return new Vector2(-1, -1);
			}

			Lon = Lon * spotmap.MapWidth / 360;
			Lat = Lat * spotmap.MapHeight / 180;

			return new Vector2((float)Lon, (float)Lat);
		}

		public Texture2D ResourceLegendImage
		{
			get
			{
				if (resourceLegend == null)
				{
					resourceLegend = new Texture2D(RESOURCELEGENDWIDTH, 1, TextureFormat.RGB24, false);
				}

				if (currentResource == null)
				{
					return null;
				}

				Color32[] pix = new Color32[RESOURCELEGENDWIDTH];

				for (int i = 0; i < RESOURCELEGENDWIDTH; i++)
				{
					float val = (i * 1f) / (RESOURCELEGENDWIDTH * 1f);
					pix[i] = palette.lerp(currentResource.MinColor32, currentResource.MaxColor32, val);
				}

				resourceLegend.SetPixels32(pix);
				resourceLegend.Apply();

				return resourceLegend;
			}
		}

		public Vector2 ResourceLegendLabels
		{
			get { return new Vector2(resourceMin / 100f, resourceMax / 100f); }
		}

		public Dictionary<string, MapLabelInfo> OrbitLabelList
		{
			get { return orbitMapLabels; }
		}

		public IList<MapLabelInfo> FlagInfoList
		{
			get
			{
				List<MapLabelInfo> vessels = new List<MapLabelInfo>();
				mapFlags.Clear();

				for (int i = FlightGlobals.Vessels.Count - 1; i >= 0; i--)
				{
					Vessel v = FlightGlobals.Vessels[i];

					if (v == null)
					{
						continue;
					}

					if (v.vesselType != VesselType.Flag)
					{
						continue;
					}

					if (v.mainBody != body)
					{
						continue;
					}

					mapFlags.Add(v);

					Vector2 flagPos = VesselPosition(v.id);

					vessels.Add(new MapLabelInfo()
					{
						label = "",
						image = SCAN_UI_Loader.FlagIcon,
						pos = flagPos,
						baseColor = ColorToggle ? palette.cb_yellow : palette.cb_skyBlue,
						flash = false,
						width = 32,
						show = flagPos.x >= 0 && flagPos.y >= 0
					});
				}

				return vessels;
			}
		}

		public Dictionary<string, MapLabelInfo> AnomalyInfoList
		{
			get
			{
				Dictionary<string, MapLabelInfo> anomalies = new Dictionary<string, MapLabelInfo>();

				if (data != null)
				{
					for (int i = data.Anomalies.Length - 1; i >= 0; i--)
					{
						SCANanomaly a = data.Anomalies[i];

						if (a == null)
						{
							continue;
						}

						if (!a.Known)
						{
							continue;
						}

						if (anomalies.ContainsKey(a.Name))
						{
							continue;
						}

						Vector2 mapPos = MapPosition(a.Latitude, a.Longitude);

						anomalies.Add(a.Name, new MapLabelInfo()
						{
							label = "",
							image = SCAN_UI_Loader.AnomalyIcon,
							pos = mapPos,
							baseColor = a.Detail ? (ColorToggle ? palette.cb_yellow : palette.cb_skyBlue) : palette.xkcd_LightGrey,
							flash = false,
							width = 20,
							alignBottom = 8,
							show = mapPos.x >= 0 && mapPos.y >= 0
						});
					}
				}

				return anomalies;
			}
		}

		public IList<MapLabelInfo> ROCInfoList
		{
			get
			{
				List<MapLabelInfo> rocs = new List<MapLabelInfo>();

				if (data != null && SCANcontroller.controller.SerenityLoaded)
				{
					List<SCANROC> ROCS = data.ROCS(true);

					if (ROCS == null)
					{
						return null;
					}

					for (int i = ROCS.Count - 1; i >= 0; i--)
					{
						SCANROC r = ROCS[i];

						if (r == null)
						{
							continue;
						}

						if (!r.Known)
						{
							continue;
						}

						Vector2 mapPos = MapPosition(r.Latitude, r.Longitude);

						rocs.Add(new MapLabelInfo()
						{
							label = "",
							image = SCAN_UI_Loader.ROCIcon,
							pos = mapPos,
							baseColor = r.Known ? ColorToggle ? palette.cb_yellow : palette.cb_skyBlue : palette.xkcd_LightGrey,
							flash = false,
							width = 18,
							show = mapPos.x >= 0 && mapPos.y >= 0
						});
					}
				}

				return rocs;
			}
		}

		public Dictionary<int, MapLabelInfo> WaypointInfoList
		{
			get
			{
				Dictionary<int, MapLabelInfo> waypoints = new Dictionary<int, MapLabelInfo>();

				if (data != null)
				{
					for (int i = data.Waypoints.Count - 1; i >= 0; i--)
					{
						SCANwaypoint w = data.Waypoints[i];

						if (w == null)
						{
							continue;
						}

						Vector2 wayPos = MapPosition(w.Latitude, w.Longitude);

						if (w.LandingTarget)
						{
							if (waypoints.ContainsKey(w.Seed))
							{
								continue;
							}

							waypoints.Add(w.Seed, new MapLabelInfo()
							{
								label = "",
								image = SCAN_UI_Loader.MechJebIcon,
								pos = wayPos,
								baseColor = palette.red,
								flash = false,
								width = 20,
								alignBottom = 0,
								show = wayPos.x >= 0 && wayPos.y >= 0
							});
						}
						else
						{
							if (waypoints.ContainsKey(w.Seed))
							{
								continue;
							}

							waypoints.Add(w.Seed, new MapLabelInfo()
							{
								label = "",
								image = SCAN_UI_Loader.WaypointIcon,
								pos = wayPos,
								baseColor = palette.white,
								flash = false,
								width = 20,
								alignBottom = 10,
								show = wayPos.x >= 0 && wayPos.y >= 0
							});
						}
					}
				}

				return waypoints;
			}
		}

		public KeyValuePair<Guid, MapLabelInfo> VesselInfo
		{
			get
			{
				if (vessel == null || vessel.mainBody != body)
				{
					return new KeyValuePair<Guid, MapLabelInfo>(new Guid(), new MapLabelInfo() { label = "null" });
				}

				Vector2 vesPos = VesselPosition(vessel);

				return new KeyValuePair<Guid, MapLabelInfo>(vessel.id, new MapLabelInfo()
				{
					label = "",
					image = SCAN_UI_Loader.VesselIcon(vessel.vesselType),
					pos = vesPos,
					baseColor = ColorToggle ? palette.white : palette.cb_skyBlue,
					flashColor = palette.cb_yellow,
					flash = true,
					width = 28,
					show = vesPos.x >= 0 && vesPos.y >= 0
				});
			}
		}

		private Vector2d MousePosition(Vector2 pos)
		{
			float mx = pos.x;
			float my = pos.y * -1f;

			double mlo = spotmap.Lon_Offset + (mx / spotmap.MapScale) - 180;
			double mla = spotmap.Lat_Offset + (spotmap.MapHeight / spotmap.MapScale) - (my / spotmap.MapScale) - 90;

			double mlon = spotmap.unprojectLongitude(mlo, mla);
			double mlat = spotmap.unprojectLatitude(mlo, mla);

			return new Vector2d(mlon, mlat);
		}

		public void ClampToScreen(RectTransform rect)
		{
			UIMasterController.ClampToScreen(rect, Vector2.zero);
		}

		public string MapInfo(Vector2 mapPos)
		{
			Vector2d pos = MousePosition(mapPos);

			double mlon = pos.x;
			double mlat = pos.y;

			if (mlon >= -180 && mlon <= 180 && mlat >= -90 && mlat <= 90)
			{
				return mouseOverInfo(mlon, mlat);
			}
			else
			{
				return "";
			}
		}

		private string mouseOverInfo(double lon, double lat)
		{
			if (infoString == null)
			{
				infoString = new StringBuilder();
			}

			infoString.Length = 0;

			bool altimetry = SCANUtil.isCovered(lon, lat, data, SCANtype.Altimetry);
			bool hires = SCANUtil.isCovered(lon, lat, data, SCANtype.AltimetryHiRes);

			if (altimetry)
			{
				SCANuiUtil.getMouseOverElevation(infoString, lon, lat, data, 2, hires);
				infoString.Append(" ");

				if (hires)
				{
					double circum = body.Radius * 2 * Math.PI;
					double eqDistancePerDegree = circum / 360;
					double degreeOffset = 5 / eqDistancePerDegree;

					infoString.AppendFormat("{0}° ", SCANUtil.slope(SCANUtil.getElevation(body, lon, lat), body, lon, lat, degreeOffset).ToString("F1"));
				}
			}

			if (SCANUtil.isCovered(lon, lat, data, SCANtype.Biome))
			{
				if (body.BiomeMap != null)
				{
					SCANUtil.getBiomeDisplayName(infoString, body, lon, lat);
					infoString.Append(" ");
				}
			}

			if (SCANconfigLoader.GlobalResource && spotmap.Resource != null)
			{
				bool resources = false;
				bool fuzzy = false;

				if (SCANUtil.isCovered(lon, lat, data, SCANtype.ResourceHiRes))
				{
					resources = true;
				}
				else if (SCANUtil.isCovered(lon, lat, data, SCANtype.ResourceLoRes))
				{
					resources = true;
					fuzzy = true;
				}

				if (resources)
				{
					infoString.Append(SCANuiUtil.getResourceAbundance(spotmap.Body, lat, lon, fuzzy, spotmap.Resource));
				}
			}

			infoString.AppendLine();
			SCANuiUtil.toDMS(infoString, lat, lon);
			infoString.AppendFormat(" (lat: {0}° lon: {1}°)", lat.ToString("F2"), lon.ToString("F2"));

			if (SCANcontroller.controller.zoomMapIcons)
			{
				infoString.AppendLine();

				double range = (ContractDefs.Survey.MaximumTriggerRange * 10) / spotmap.MapScale;

				for (int i = data.Waypoints.Count - 1; i >= 0; i--)
				{
					SCANwaypoint p = data.Waypoints[i];

					if (!p.LandingTarget)
					{
						if (p.Root != null)
						{
							if (p.Root.ContractState != Contracts.Contract.State.Active)
							{
								continue;
							}
						}
						if (p.Param != null)
						{
							if (p.Param.State != Contracts.ParameterState.Incomplete)
							{
								continue;
							}
						}

						if (SCANUtil.waypointDistance(lat, lon, 1000, p.Latitude, p.Longitude, 1000, body) <= range)
						{
							infoString.AppendFormat("Way: {0} ", p.Name);
							break;
						}
					}
					else if (SCANUtil.waypointDistance(lat, lon, 1000, p.Latitude, p.Longitude, 1000, body) <= range)
					{
						infoString.Append("MJ: ");
						SCANuiUtil.toDMS(infoString, p.Latitude, p.Longitude);
						break;
					}
				}

				for (int i = data.Anomalies.Length - 1; i >= 0; i--)
				{
					SCANanomaly a = data.Anomalies[i];

					if (a.Known)
					{
						if (SCANUtil.mapLabelDistance(lat, lon, a.Latitude, a.Longitude, body) <= range)
						{
							if (a.Detail)
							{
								infoString.AppendFormat("?: {0} ", a.Name);
							}
							else
							{
								infoString.Append("?: Unknown ");
							}

							break;
						}
					}
				}

				if (SCANcontroller.controller.SerenityLoaded)
				{
					List<SCANROC> ROCS = data.ROCS(false);

					if (ROCS != null)
					{
						for (int i = ROCS.Count - 1; i >= 0; i--)
						{
							SCANROC r = ROCS[i];

							if (r.Known)
							{
								if (SCANUtil.mapLabelDistance(lat, lon, r.Latitude, r.Longitude, body) <= range)
								{
									if (r.Scanned)
									{
										infoString.AppendFormat("ROC: {0} ", r.Name);
									}
									else
									{
										infoString.Append("ROC: Unknown ");
									}

									break;
								}
							}
						}
					}
				}

				for (int i = mapFlags.Count - 1; i >= 0; i--)
				{
					Vessel flag = mapFlags[i];

					if (SCANUtil.mapLabelDistance(lat, lon, flag.latitude, flag.longitude, body) <= range)
					{
						infoString.Append(" Flag: ");
						infoString.Append(flag.vesselName);
						break;
					}
				}
			}

			return infoString.ToString();
		}

		public string TooltipText(float xPos)
		{
			switch (spotmap.MType)
			{
				case mapType.Biome:
					if (biomes.Count <= 0)
					{
						return "";
					}

					int count = biomes.Count;

					int blockSize = (int)Math.Truncate(256 / (count * 1d));

					int current = (int)Math.Truncate((xPos * 256) / (blockSize * 1d));

					if (current >= count)
					{
						current = count - 1;
					}
					else if (current < 0)
					{
						current = 0;
					}

					return Localizer.Format(biomes[current].displayname);
				case mapType.Altimetry:
					float terrain = xPos * (terrainMax - terrainMin) + terrainMin;

					return string.Format("{0}m", terrain.ToString("N0"));
			}

			return "";
		}

		public void RefreshMap()
		{
			resetMap();

			uiElement.SetLegends(LegendToggle);
		}

		public void VesselSync()
		{
			vessel = FlightGlobals.ActiveVessel;

			if (vessel == null)
			{
				return;
			}

			if (vessel.mainBody != body)
			{
				SCANdata dat = SCANUtil.getData(vessel.mainBody);
				if (dat == null)
				{
					dat = new SCANdata(vessel.mainBody);
				}

				data = dat;
				body = data.Body;

				spotmap.setBody(body);
			}

			resetMap(SCANUtil.fixLatShift(vessel.latitude), SCANUtil.fixLonShift(vessel.longitude), true);
		}

		public void MoveMap(int i)
		{
			if (VesselLock)
			{
				resetMapToVessel();
				return;
			}

			double div = 3;

			if (spotmap.MapScale < 4)
			{
				div = 5;
			}

			double w = (spotmap.MapWidth / spotmap.MapScale) / div;
			double h = (spotmap.MapHeight / spotmap.MapScale) / div;

			double lon = spotmap.CenteredLong;

			if (i == 2)
			{
				if (spotmap.CenteredLat + h > 90)
				{
					lon += 180;
					lon = SCANUtil.fixLonShift(lon);
				}
			}
			else if (i == 3)
			{
				if (spotmap.CenteredLat - h < -90)
				{
					lon += 180;
					lon = SCANUtil.fixLonShift(lon);
				}
			}

			switch (i)
			{
				case 0:
					w = spotmap.CenteredLong - w;
					if (w < -180)
					{
						w += 360;
					}

					resetMap(spotmap.CenteredLat, w, true);
					break;
				case 1:
					w = spotmap.CenteredLong + w;
					if (w > 180)
					{
						w -= 360;
					}

					resetMap(spotmap.CenteredLat, w, true);
					break;
				case 2:
					h = spotmap.CenteredLat + h;
					if (h > 90)
					{
						h = 180 - h;
					}

					resetMap(h, lon, true);
					break;
				case 3:
					h = spotmap.CenteredLat - h;
					if (h < -90)
					{
						h = -180 - h;
					}

					resetMap(h, lon, true);
					break;
			}
		}

		public void ZoomMap(bool zoom)
		{
			if (zoom)
			{
				spotmap.MapScale = spotmap.MapScale * 1.25f;
				if (spotmap.MapScale > maxZoom)
				{
					spotmap.MapScale = maxZoom;
				}

				resetMap();
			}
			else
			{
				spotmap.MapScale = spotmap.MapScale / 1.25f;
				if (spotmap.MapScale < minZoom)
				{
					spotmap.MapScale = minZoom;
				}

				resetMap();
			}
		}

		public void SetWaypoint(string id, Vector2 pos)
		{
			if (string.IsNullOrEmpty(id))
			{
				id = RandomWaypoint;
			}

			pos.y -= spotmap.MapHeight;

			Vector2d mapPos = MousePosition(pos);

			if (mapPos.x < -180 || mapPos.x > 180 || mapPos.y < -90 || mapPos.y > 90)
			{
				return;
			}

			Waypoint w = new Waypoint();

			w.name = id;
			w.isExplored = true;
			w.isNavigatable = true;
			w.isOnSurface = true;
			w.celestialName = body.GetName();
			w.latitude = mapPos.y;
			w.longitude = mapPos.x;
			w.seed = gen.Next(0, int.MaxValue);
			w.navigationId = new Guid();

			ScenarioCustomWaypoints.AddWaypoint(w);
		}

		public void SetMJWaypoint(Vector2 pos)
		{
			pos.y -= spotmap.MapHeight;

			Vector2d mapPos = MousePosition(pos);

			if (mapPos.x < -180 || mapPos.x > 180 || mapPos.y < -90 || mapPos.y > 90)
			{
				return;
			}

			SCANcontroller.controller.MJTargetSet.Invoke(mapPos, body);

			SCANwaypoint w = new SCANwaypoint(mapPos.y, mapPos.x, "MechJeb Landing Target");
			data.addToWaypoints(w);
		}

		public void ClickMap(int button, Vector2 pos)
		{
			Vector2d mapPos = MousePosition(pos);

			switch (button)
			{
				case 0:
					spotmap.MapScale = spotmap.MapScale / 1.25f;

					if (spotmap.MapScale < minZoom)
					{
						spotmap.MapScale = minZoom;
					}

					resetMap(mapPos.y, mapPos.x, true);
					break;
				case 1:
					if (GameSettings.MODIFIER_KEY.GetKey())
					{
						resetMap(mapPos.y, mapPos.x, true);
					}
					else
					{
						spotmap.MapScale = spotmap.MapScale * 1.25f;

						if (spotmap.MapScale > maxZoom)
						{
							spotmap.MapScale = maxZoom;
						}

						resetMap(mapPos.y, mapPos.x, true);
					}
					break;
				case 2:
					resetMap(mapPos.y, mapPos.x, true);
					break;
			}
		}

		public void ResetPosition()
		{
			SCAN_Settings_Config.Instance.ZoomMapPosition = new Vector2(400, -400);

			if (uiElement != null)
			{
				uiElement.SetPosition(SCAN_Settings_Config.Instance.ZoomMapPosition);
			}
		}
	}
}
