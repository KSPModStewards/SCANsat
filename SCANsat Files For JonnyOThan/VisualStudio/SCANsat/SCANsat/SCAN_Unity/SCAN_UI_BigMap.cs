#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_UI_BigMap - UI control object for SCANsat big map
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using System.Collections;
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
using Contracts;
using KSP.UI;
using FinePrint;
using FinePrint.Utilities;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANpalette;

namespace SCANsat.SCAN_Unity
{
	public class SCAN_UI_BigMap : ISCAN_BigMap
	{
		private bool _isVisible;

		private static SCANmap bigmap;
		private static CelestialBody body;
		private SCANdata data;
		private Vessel vessel;
		private bool updateMap;
		private StringBuilder infoString;
		private System.Random gen;
		private bool _inputLock;
		private const string controlLock = "SCANsatBig";

		private SCANresourceGlobal currentResource;
		private List<SCANresourceGlobal> resources;

		private List<Vessel> mapFlags = new List<Vessel>();

		private SCAN_BigMap uiElement;

		private const int orbitSteps = 80;
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
		
		private static Texture2D eqMap;
		private static bool[] eq_an;
		private static bool[] eq_dn;
		private static Color32[] eq_pix;
		private const int eq_height = 20;
		private const int eq_count = 100;
		private const int eq_update = 4;
		private int eq_timer;
		private Color32 c_dn = palette.CB_orange;
		private Color32 c_an = palette.CB_skyBlue;
		private Texture2D clearMap;
		private bool clearMapSet;

		private Texture2D gridMap;

		private static SCAN_UI_BigMap instance;

		public static SCAN_UI_BigMap Instance
		{
			get { return instance; }
		}

		public static CelestialBody Body
		{
			get { return body; }
		}

		public SCAN_UI_BigMap()
		{
			instance = this;

			resources = SCANcontroller.setLoadedResourceList();

			GameEvents.onVesselChange.Add(vesselChange);
			GameEvents.onVesselWasModified.Add(vesselChange);
			GameEvents.onVesselSOIChanged.Add(soiChange);

			gen = new System.Random(Environment.TickCount.GetHashCode());

			initializeMap();

			if (HighLogic.LoadedSceneIsFlight && SCANcontroller.controller.bigMapVisible)
			{
				if (WaypointToggle)
					SCANcontroller.controller.StartCoroutine(WaitForWaypoints());
				else
					Open();
			}
		}

		private IEnumerator WaitForWaypoints()
		{
			while (!ContractSystem.loaded)
				yield return null;

			Open();
		}

		private void vesselChange(Vessel V)
		{
			vessel = FlightGlobals.ActiveVessel;

			if (!_isVisible)
				return;

			RefreshIcons();
		}

		private void soiChange(GameEvents.HostedFromToAction<Vessel, CelestialBody> action)
		{
			if (!_isVisible)
				return;

			RefreshIcons();

			updateMap = true;
		}

		public void Open()
		{
			uiElement = GameObject.Instantiate(SCAN_UI_Loader.BigMapPrefab).GetComponent<SCAN_BigMap>();

			if (uiElement == null)
				return;

			uiElement.transform.SetParent(UIMasterController.Instance.dialogCanvas.transform, false);

			if (OrbitToggle && ShowOrbit)
			{
				Orbit o = vessel.orbit;

				orbitLabels.Clear();

				for (int i = 0; i < orbitSteps * 3; i++)
				{
					orbitLabels.Add(new SimpleLabelInfo(10, SCAN_UI_Loader.PlanetIcon));
				}

				if (!vessel.LandedOrSplashed)
					UpdateOrbitIcons(o);
			}

			uiElement.setMap(this);

			SetGridLines();

			SetTitle();

			uiElement.UpdateEQMapTexture(clearMap);
			clearMapSet = true;

			updateMap = true;

			_isVisible = true;

			if (HighLogic.LoadedSceneIsFlight)
				SCANcontroller.controller.bigMapVisible = true;

			if (SCAN_Settings_Config.Instance.StockToolbar)
			{
				if (HighLogic.LoadedSceneIsFlight)
				{
					if (SCAN_Settings_Config.Instance.ToolbarMenu)
					{
						if (SCANappLauncher.Instance != null && SCANappLauncher.Instance.UIElement != null)
							SCANappLauncher.Instance.UIElement.SetBigMapToggle(false);
					}
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

			if (uiElement == null)
				return;

			uiElement.FadeOut();

			if (SCAN_Settings_Config.Instance.StockToolbar)
			{
				if (HighLogic.LoadedSceneIsFlight)
				{
					if (SCAN_Settings_Config.Instance.ToolbarMenu)
					{
						if (SCANappLauncher.Instance != null && SCANappLauncher.Instance.UIElement != null)
							SCANappLauncher.Instance.UIElement.SetBigMapToggle(false);
					}
				}
				else
				{
					if (SCANappLauncher.Instance != null && SCANappLauncher.Instance.SCANAppButton != null)
						SCANappLauncher.Instance.SCANAppButton.SetFalse(false);
				}
			}

			if (HighLogic.LoadedSceneIsFlight)
				SCANcontroller.controller.bigMapVisible = false;
		}

		public void RefreshIcons()
		{
			if (!_isVisible || uiElement == null)
				return;

			uiElement.RefreshIcons();
		}

		private void initializeMap()
		{
			if (HighLogic.LoadedSceneIsFlight)
				vessel = FlightGlobals.ActiveVessel;

			if (body == null)
			{
				for (int i = FlightGlobals.Bodies.Count - 1; i >= 0; i--)
				{
					CelestialBody b = FlightGlobals.Bodies[i];

					if (b.bodyName != SCANcontroller.controller.bigMapBody)
						continue;

					body = b;
					break;
				}

				if (body == null)
				{
					if (vessel == null)
						body = FlightGlobals.Bodies[1];
					else
						body = vessel.mainBody;
				}
			}

			data = SCANUtil.getData(body);

			if (data == null)
			{
				data = new SCANdata(body);
				SCANcontroller.controller.addToBodyData(body, data);
			}

			if (bigmap == null)
			{				
				bigmap = new SCANmap(body, true, mapSource.BigMap);

				MapProjection p = MapProjection.Rectangular;
				mapType t = mapType.Altimetry;

				try
				{
					p = (MapProjection)Enum.Parse(typeof(MapProjection), SCANcontroller.controller.bigMapProjection, true);
					t = (mapType)Enum.Parse(typeof(mapType), SCANcontroller.controller.bigMapType, true);
				}
				catch (Exception e)
				{
					SCANUtil.SCANlog("Error in parsing map projection and/or type\n{0}", e);
					
					p = MapProjection.Rectangular;
					t = mapType.Altimetry;
				}

				bigmap.Projection = p;
				bigmap.MType = t;
				bigmap.ResourceActive = SCANcontroller.controller.bigMapResourceOn;
				bigmap.ColorMap = SCANcontroller.controller.bigMapColor;
				bigmap.Terminator = SCANcontroller.controller.bigMapTerminator;

				if (SCAN_Settings_Config.Instance.BigMapWidth % 2 != 0)
					SCAN_Settings_Config.Instance.BigMapWidth += 1;

				bigmap.setWidth(SCAN_Settings_Config.Instance.BigMapWidth);
			}

			bigmap.setBody(body);

			currentResource = AssignResource(SCANcontroller.controller.bigMapResource);

			if (currentResource != null)
				bigmap.Resource = currentResource;

			if (eqMap == null)
				RefreshEQMap();

			AddOrbitMapLabels();

			clearMap = new Texture2D(1, 1, TextureFormat.ARGB32, false);

			clearMap.SetPixel(0, 0, palette.clear);
			clearMap.Apply();
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
			GameEvents.onVesselChange.Remove(vesselChange);
			GameEvents.onVesselWasModified.Remove(vesselChange);
			GameEvents.onVesselSOIChanged.Remove(soiChange);

			if (uiElement != null)
			{
				uiElement.gameObject.SetActive(false);
				MonoBehaviour.Destroy(uiElement.gameObject);
			}

			SCANcontroller.controller.unloadPQS(bigmap.Body, mapSource.BigMap);
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
			if (!_isVisible || data == null || bigmap == null)
				return;
			
			if (uiElement == null)
				return;

			if (!bigmap.isMapComplete())
			{
				if (SCAN_Settings_Config.Instance.MapGenerationSpeed > 2)
					bigmap.getPartialMap(false);

				if (SCAN_Settings_Config.Instance.MapGenerationSpeed > 1)
					bigmap.getPartialMap(false);

				bigmap.getPartialMap(true);
			}

			if (OrbitToggle && ShowOrbit)
			{
				if (vessel != null && vessel.mainBody == body && !vessel.LandedOrSplashed)
				{
					Orbit o = vessel.orbit;

					UpdateOrbitIcons(o);

					if (o.PeA < 0)
					{
						if (!clearMapSet)
						{
							clearMapSet = true;
							uiElement.UpdateEQMapTexture(clearMap);
						}
					}
					else if (clearMapSet)
					{
						clearMapSet = false;
						if (bigmap.Projection != MapProjection.Polar)
							uiElement.UpdateEQMapTexture(eqMap);
					}

					if (eq_timer >= eq_update)
						UpdateEQMap(o);

					eq_timer++;

					if (eq_timer > eq_update)
						eq_timer = 0;
				}
			}

			if (updateMap)
			{
				updateMap = false;
				uiElement.UpdateMapTexture(bigmap.Map);

				if (OrbitToggle && ShowOrbit && bigmap.Projection != MapProjection.Polar && vessel.orbit.PeA > 0)
					uiElement.UpdateEQMapTexture(eqMap);
				else
					uiElement.UpdateEQMapTexture(clearMap);
			}
		}

		private void UpdateOrbitIcons(Orbit o)
		{
			double startUT = Planetarium.GetUniversalTime();
			double UT = startUT;
			Color col;

			for (int i = 0; i < orbitSteps * 2; i++)
			{
				SimpleLabelInfo info = orbitLabels[i];

				if (info == null)
					continue;

				int k = i - orbitSteps;

				if (k < 0)
					UT = startUT - (orbitSteps + k) * (o.period / orbitSteps);
				else
					UT = startUT + k * o.period * (1f / orbitSteps);

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
					rotation = (360 * ((UT - startUT) / body.rotationPeriod)) % 360;

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

				double lon = (bigmap.projectLongitude(lo, la) + 180) % 360;
				double lat = (bigmap.projectLatitude(lo, la) + 90) % 180;

				lon = bigmap.scaleLongitude(lon);
				lat = bigmap.scaleLatitude(lat);

				if (lat < 0 || lon < 0 || lat > 180 || lon > 360)
				{
					info.show = false;
					continue;
				}

				lon = lon * bigmap.MapWidth / 360;
				lat = lat * bigmap.MapHeight / 180;

				if (k < 0)
					col = palette.cb_orange;
				else
				{
					if (body.atmosphere && body.atmosphereDepth >= alt)
						col = palette.cb_reddishPurple;
					else
						col = palette.cb_skyBlue;
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
				Ap.show = false;

			orbitMapLabels[Aplabel] = Ap;

			MapLabelInfo Pe = orbitMapLabels[Pelabel];

			if (o.PeA > 0 && mapPosAtT(o, o.timeToPe, startUT, out labelPos))
			{
				Pe.show = true;
				Pe.pos = labelPos;
				Pe.label = o.PeA.ToString("N0");
			}
			else
				Pe.show = false;

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
							ManAp.show = false;

						orbitMapLabels[ManAplabel] = ManAp;

						MapLabelInfo ManPe = orbitMapLabels[ManPelabel];

						if (next.timeToPe > 0 && n.UT + next.timeToPe < next.EndUT && mapPosAtT(next, n.UT - startUT + next.timeToPe, startUT, out labelPos))
						{
							ManPe.show = true;
							ManPe.pos = labelPos;
						}
						else
							ManPe.show = false;

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

		private void UpdateEQMap(Orbit o)
		{
			if (o.PeA < 0)
				return;

			if (bigmap.Projection == MapProjection.Polar)
				return;

			for (int i = eq_an.Length - 1; i >= 0; i--)
			{
				eq_an[i] = false;
				eq_dn[i] = false;
			}

			double startUT = Planetarium.GetUniversalTime();

			double TAAN = 360f - o.argumentOfPeriapsis;	// true anomaly at ascending node
			double TADN = (TAAN + 180) % 360;			// true anomaly at descending node
			double MAAN = meanForTrue(TAAN, o.eccentricity);
			double MADN = meanForTrue(TADN, o.eccentricity);
			double tAN = (((MAAN - o.meanAnomaly * Mathf.Rad2Deg + 360) % 360) / 360f * o.period + startUT);
			double tDN = (((MADN - o.meanAnomaly * Mathf.Rad2Deg + 360) % 360) / 360f * o.period + startUT);

			for (int i = 0; i < 100; i++)
			{
				double UTAN = tAN + o.period * i;
				double UTDN = tDN + o.period * i;

				if (double.IsNaN(UTAN) || double.IsNaN(UTDN))
					continue;

				Vector3d pAN = o.getPositionAtUT(UTAN);
				Vector3d pDN = o.getPositionAtUT(UTDN);

				double rotAN = 0;
				double rotDN = 0;

				if (body.rotates)
				{
					rotAN = ((360 * ((UTAN - startUT) / body.rotationPeriod)) % 360);
					rotDN = ((360 * ((UTDN - startUT) / body.rotationPeriod)) % 360);
				}

				double loAN = body.GetLongitude(pAN) - rotAN;
				double loDN = body.GetLongitude(pDN) - rotDN;

				int lonAN = (int)(((bigmap.projectLongitude(loAN, 0) + 180) % 360) * eq_an.Length / 360f);
				int lonDN = (int)(((bigmap.projectLongitude(loDN, 0) + 180) % 360) * eq_dn.Length / 360f);

				if (lonAN >= 0 && lonAN < eq_an.Length)
					eq_an[lonAN] = true;
				if (lonDN >= 0 && lonDN < eq_dn.Length)
					eq_dn[lonDN] = true;
			}

			for (int y = 0; y < eq_height; y++)
			{
				bool down = y < eq_height / 2;
				Color32 lc = palette.Clear;

				for (int x = 0; x < eq_an.Length; x++)
				{
					bool cross;
					Color32 c = palette.Clear;

					if (down)
						cross = eq_dn[x];
					else
						cross = eq_an[x];

					if (cross)
					{
						if (y == 0 || y == eq_height - 1)
							c = palette.Black;
						else
						{
							if (lc.r == palette.Clear.r)
								eq_pix[y * eq_an.Length + x - 1] = palette.Black;

							c = down ? c_dn : c_an;
						}
					}
					else
					{
						if (lc.r != palette.Clear.r && lc.r != palette.Black.r)
							c = palette.Black;
					}

					eq_pix[y * eq_an.Length + x] = c;
					lc = c;
				}
			}

			eqMap.SetPixels32(eq_pix);
			eqMap.Apply();
		}

		private double meanForTrue(double TA, double e)
		{
			TA = TA * Mathf.Deg2Rad;

			double EA = Math.Acos((e + Math.Cos(TA)) / (1 + e * Math.Cos(TA)));

			if (TA > Math.PI)
				EA = 2 * Math.PI - EA;

			double MA = EA - e * Math.Sin(EA);

			// the mean anomaly isn't really an angle, but I'm a simple person
			return MA * Mathf.Rad2Deg;
		}

		private bool mapPosAtT(Orbit o, double dT, double startUT, out Vector2 labelPos)
		{
			labelPos = new Vector2();

			double UT = startUT + dT;

			if (double.IsNaN(UT))
				return false;

			try
			{
				if (double.IsNaN(o.getObtAtUT(UT)))
					return false;

				Vector3d pos = o.getPositionAtUT(UT);
				double rotation = 0;

				if (body.rotates)
					rotation = (360 * (dT / vessel.mainBody.rotationPeriod)) % 360;

				double lo = (body.GetLongitude(pos) - rotation);
				double la = (body.GetLatitude(pos));

				double lon = (bigmap.projectLongitude(lo, la) + 180) % 360;
				double lat = (bigmap.projectLatitude(lo, la) + 90) % 180;

				lat = bigmap.scaleLatitude(lat);
				lon = bigmap.scaleLongitude(lon);

				if (lat < 0 || lon < 0 || lat > 180 || lon > 360)
					return false;

				lon = lon * bigmap.MapWidth / 360;
				lat = lat * bigmap.MapHeight / 180;

				labelPos = new Vector2((float)lon, (float)lat);

				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private void RefreshEQMap()
		{
			eqMap = new Texture2D(bigmap.MapWidth, eq_height, TextureFormat.ARGB32, false);
			eq_an = new bool[bigmap.MapWidth];
			eq_dn = new bool[bigmap.MapWidth];
			eq_pix = new Color32[bigmap.MapWidth * eq_height];

			for (int i = eq_pix.Length - 1; i >= 0; i--)
			{
				eq_pix[i] = palette.Clear;
			}

			eqMap.SetPixels32(eq_pix);
			eqMap.Apply();
		}

		private void SetGridLines()
		{
			if (uiElement == null)
				return;

			if (!GridToggle)
				uiElement.UpdateGridTexture(clearMap);
			else
			{
				GenerateGridMap();

				uiElement.UpdateGridTexture(gridMap);
			}
		}

		private void GenerateGridMap()
		{
			gridMap = new Texture2D(bigmap.MapWidth, bigmap.MapHeight, TextureFormat.ARGB32, false);

			Color32[] pix = gridMap.GetPixels32();

			for (int i = pix.Length - 1; i >= 0; i--)
				pix[i] = palette.Clear;

			int x, y;
			for (double lat = -90; lat < 90; lat+=2)
			{
				for (double lon = -180; lon < 180; lon +=2)
				{
					if (lat % 30 == 0 || lon % 30 == 0)
					{
						x = (int)(bigmap.MapScale * ((bigmap.projectLongitude(lon, lat) + 180) % 360));
						y = (int)(bigmap.MapScale * ((bigmap.projectLatitude(lon, lat) + 90) % 180));

						pix[y * bigmap.MapWidth + x] = palette.White;

						if (x < bigmap.MapWidth - 1)
							pix[(y * bigmap.MapWidth) + (x + 1)] = palette.Black;

						if (x > 0)
							pix[(y * bigmap.MapWidth) + (x - 1)] = palette.Black;

						if (y < bigmap.MapHeight - 1)
							pix[((y + 1) * bigmap.MapWidth) + x] = palette.Black;

						if (y > 0)
							pix[((y - 1) * bigmap.MapWidth) + x] = palette.Black;
					}
				}
			}

			gridMap.SetPixels32(pix);

			gridMap.Apply();
		}

		public void SetMapSize()
		{
			if (!_isVisible || uiElement == null)
				return;

			uiElement.SetSize(Size);
		}

		private void SetTitle()
		{
			if (uiElement == null || bigmap == null)
				return;

			uiElement.UpdateTitle(string.Format("S.C.A.N. {0} Map of {1}", bigmap.MType, body.displayName));
		}

		public string Version
		{
			get { return SCANmainMenuLoader.SCANsatVersion; }
		}

		public string CurrentProjection
		{
			get { return SCANcontroller.controller.bigMapProjection; }
			set
			{
				MapProjection p;

				try
				{
					p = (MapProjection)Enum.Parse(typeof(MapProjection), value, true);

					SCANcontroller.controller.bigMapProjection = value;
					bigmap.Projection = p;

					SetGridLines();

					bigmap.resetMap(SCANcontroller.controller.bigMapResourceOn);

					updateMap = true;
				}
				catch (Exception e)
				{
					SCANUtil.SCANlog("Error in parsing map projection type\n{0}", e);
				}
			}
		}

		public string CurrentMapType
		{
			get { return SCANcontroller.controller.bigMapType; }
			set
			{
				mapType t;

				try
				{
					t = (mapType)Enum.Parse(typeof(mapType), value, true);

					SCANcontroller.controller.bigMapType = value;
					bigmap.MType = t;
					bigmap.resetMap(SCANcontroller.controller.bigMapResourceOn);
				}
				catch (Exception e)
				{
					SCANUtil.SCANlog("Error in parsing map type\n{0}", e);
				}

				SetTitle();
			}
		}

		public string CurrentResource
		{
			get { return SCANUtil.displayNameFromResource(SCANcontroller.controller.bigMapResource); }
			set
			{
				SCANcontroller.controller.bigMapResource = SCANUtil.resourceFromDisplayName(value);

				currentResource = AssignResource(SCANcontroller.controller.bigMapResource);

				if (currentResource == null)
					bigmap.Resource = null;
				else
					bigmap.Resource = currentResource;
			}
		}

		private SCANresourceGlobal AssignResource(string resource)
		{
			SCANresourceGlobal r = currentResource;

			if (r == null || r.DisplayName != resource)
			{
				for (int i = resources.Count - 1; i >= 0; i--)
				{
					SCANresourceGlobal res = resources[i];

					if (res.DisplayName != resource)
						continue;

					r = res;
					break;
				}
			}

			if (r == null)
				r = SCANcontroller.GetFirstResource;

			if (r != null)
				r.CurrentBodyConfig(body.name);

			return r;
		}

		public string CurrentCelestialBody
		{
			get { return SCANUtil.displayNameFromBodyName(SCANcontroller.controller.bigMapBody); }
			set
			{
				string b = SCANUtil.bodyFromDisplayName(value);

				SCANdata bodyData = SCANUtil.getData(b);

				if (bodyData != null)
				{
					data = bodyData;
					body = data.Body;
					bigmap.setBody(body);

					if (OrbitToggle && ShowOrbit)
					{
						Orbit o = vessel.orbit;

						orbitLabels.Clear();

						for (int i = 0; i < orbitSteps * 3; i++)
						{
							orbitLabels.Add(new SimpleLabelInfo(10, SCAN_UI_Loader.PlanetIcon));
						}

						if (!vessel.LandedOrSplashed)
							UpdateOrbitIcons(o);
					}

					bigmap.resetMap(SCANcontroller.controller.bigMapResourceOn);

					SCANcontroller.controller.bigMapBody = b;

					if (currentResource != null)
						currentResource.CurrentBodyConfig(body.bodyName);

					updateMap = true;
				}

				SetTitle();
			}
		}

		public string RandomWaypoint
		{
			get { return StringUtilities.GenerateSiteName(gen.Next(), body, false, true); }
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

		public bool ColorToggle
		{
			get { return SCANcontroller.controller.bigMapColor; }
			set
			{
				SCANcontroller.controller.bigMapColor = value;

				bigmap.ColorMap = value;
				bigmap.resetMap(SCANcontroller.controller.bigMapResourceOn);
			}
		}

		public bool TerminatorToggle
		{
			get { return SCANcontroller.controller.bigMapTerminator; }
			set
			{
				SCANcontroller.controller.bigMapTerminator = value;

				bigmap.Terminator = value;
				bigmap.resetMap(SCANcontroller.controller.bigMapResourceOn);
			}
		}

		public bool GridToggle
		{
			get { return SCANcontroller.controller.bigMapGrid; }
			set
			{
				SCANcontroller.controller.bigMapGrid = value;

				SetGridLines();
			}
		}

		public bool OrbitToggle
		{
			get { return SCANcontroller.controller.bigMapOrbit; }
			set
			{
				SCANcontroller.controller.bigMapOrbit = value;

				if (value && ShowOrbit)
				{
					Orbit o = vessel.orbit;

					orbitLabels.Clear();

					for (int i = 0; i < orbitSteps * 3; i++)
					{
						orbitLabels.Add(new SimpleLabelInfo(10, SCAN_UI_Loader.PlanetIcon));
					}

					if (!vessel.LandedOrSplashed)
						UpdateOrbitIcons(o);
				}

				updateMap = true;	
			}
		}

		public bool WaypointToggle
		{
			get { return SCANcontroller.controller.bigMapWaypoint; }
			set
			{
				SCANcontroller.controller.bigMapWaypoint = value;

			}
		}

		public bool AnomalyToggle
		{
			get { return SCANcontroller.controller.bigMapAnomaly; }
			set
			{
				SCANcontroller.controller.bigMapAnomaly = value;

			}
		}

		public bool FlagToggle
		{
			get { return SCANcontroller.controller.bigMapFlag; }
			set
			{
				SCANcontroller.controller.bigMapFlag = value;

			}
		}

		public bool LegendToggle
		{
			get { return SCANcontroller.controller.bigMapLegend; }
			set { SCANcontroller.controller.bigMapLegend = value;}
		}

		public bool LegendAvailable
		{
			get
			{
				switch (bigmap.MType)
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
			get { return SCANcontroller.controller.bigMapResourceOn; }
			set
			{
				SCANcontroller.controller.bigMapResourceOn = value;

				bigmap.resetMap(SCANcontroller.controller.bigMapResourceOn);
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

		public bool MechJebAvailable
		{
			get { return SCANmainMenuLoader.MechJebLoaded && SCAN_Settings_Config.Instance.MechJebTarget && SCANcontroller.controller.MechJebLoaded; }
		}

		public bool LockInput
		{
			get { return _inputLock; }
			set
			{
				_inputLock = value;

				if (_inputLock)
					InputLockManager.SetControlLock(controlLock);
				else
					InputLockManager.RemoveControlLock(controlLock);
			}
		}

		public bool TooltipsOn
		{
			get { return SCAN_Settings_Config.Instance.WindowTooltips; }
		}

		public bool LegendTooltips
		{
			get { return SCAN_Settings_Config.Instance.LegendTooltips; }
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
			get { return SCAN_Settings_Config.Instance.BigMapPosition; }
			set { SCAN_Settings_Config.Instance.BigMapPosition = value; }
		}

		public Vector2 Size
		{
			get
			{
				float width = SCAN_Settings_Config.Instance.BigMapWidth;
				float height = width / 2;

				return new Vector2(width, height);
			}
			set
			{
				SCAN_Settings_Config.Instance.BigMapWidth = (int)value.x;

				bigmap.setWidth(SCAN_Settings_Config.Instance.BigMapWidth);

				SetGridLines();

				RefreshEQMap();

				updateMap = true;
			}
		}

		public Texture2D LegendImage
		{
			get
			{
				if (bigmap.MapLegend == null)
					bigmap.MapLegend = new SCANmapLegend();

				if (data == null)
					return null;

				switch (bigmap.MType)
				{
					case mapType.Altimetry:
						return bigmap.MapLegend.getLegend(SCANcontroller.controller.zoomMapColor, data.TerrainConfig);
					case mapType.Biome:
						if (body != null && body.BiomeMap != null && body.BiomeMap.Attributes != null)
							return bigmap.MapLegend.getLegend(data, SCANcontroller.controller.zoomMapColor, SCAN_Settings_Config.Instance.BigMapStockBiomes, body.BiomeMap.Attributes);
						else
							return null;
				}

				return null;
			}
		}

		public IList<string> Projections
		{
			get { return new List<string>(3) { "Rectangular", "KavrayskiyVII", "Polar" }; }
		}

		public IList<string> MapTypes
		{
			get { return new List<string>(3) { "Altimetry", "Slope", "Biome" }; }
		}

		public IList<string> Resources
		{
			get { return new List<string>(resources.Select(r => r.DisplayName)); }
		}

		public IList<string> CelestialBodies
		{
			get { return new List<string>(SCANcontroller.controller.GetAllData.Select(d => d.Body.displayName)); }
		}

		public IList<string> LegendLabels
		{
			get
			{
				if (data == null)
					return null;

				string one = string.Format("|\n{0:N0}", (((int)(data.TerrainConfig.MinTerrain / 100)) * 100));

				string two = string.Format("|\n{0:N0}", (((int)((data.TerrainConfig.MinTerrain + (data.TerrainConfig.TerrainRange / 2)) / 100)) * 100));

				string three = string.Format("|\n{0:N0}", (((int)(data.TerrainConfig.MaxTerrain / 100)) * 100));

				return new List<string>(3) { one, two, three };
			}
		}

		public SimpleLabelInfo OrbitInfo(int index)
		{
			if (index < 0 || index >= orbitLabels.Count)
				return null;

			return orbitLabels[index];
		}

		public MapLabelInfo OrbitIconInfo(string id)
		{
			MapLabelInfo info;

			if (OrbitLabelList.TryGetValue(id, out info))
				return info;

			return new MapLabelInfo();
		}

		public Vector2 VesselPosition()
		{
			if (vessel == null)
				return new Vector2();

			return VesselPosition(vessel);
		}

		public Vector2 VesselPosition(Guid id)
		{
			Vessel v = null;

			for (int i = FlightGlobals.Vessels.Count - 1; i >= 0; i--)
			{
				v = FlightGlobals.Vessels[i];

				if (v.id == id)
					break;
			}

			return VesselPosition(v);
		}

		public Vector2 VesselPosition(Vessel v)
		{
			if (v == null)
				return new Vector2();

			double lat = SCANUtil.fixLat(bigmap.projectLatitude(v.longitude, v.latitude));
			double lon = SCANUtil.fixLon(bigmap.projectLongitude(v.longitude, v.latitude));
			lat = bigmap.scaleLatitude(lat);
			lon = bigmap.scaleLongitude(lon);

			lon = lon * bigmap.MapWidth / 360;
			lat = lat * bigmap.MapHeight / 180;

			return new Vector2((float)lon, (float)lat);
		}

		public Vector2 MapPosition(double lat, double lon)
		{
			double Lat = SCANUtil.fixLat(bigmap.projectLatitude(lon, lat));
			double Lon = SCANUtil.fixLon(bigmap.projectLongitude(lon, lat));
			Lat = bigmap.scaleLatitude(Lat);
			Lon = bigmap.scaleLongitude(Lon);

			Lon = Lon * bigmap.MapWidth / 360;
			Lat = Lat * bigmap.MapHeight / 180;

			return new Vector2((float)Lon, (float)Lat);
		}

		public Dictionary<string, MapLabelInfo> OrbitLabelList
		{
			get { return orbitMapLabels; }
		}

		public Dictionary<Guid, MapLabelInfo> FlagInfoList
		{
			get
			{
				Dictionary<Guid, MapLabelInfo> vessels = new Dictionary<Guid, MapLabelInfo>();
				mapFlags.Clear();

				for (int i = FlightGlobals.Vessels.Count - 1; i >= 0; i--)
				{
					Vessel v = FlightGlobals.Vessels[i];

					if (v == null)
						continue;

					if (v.vesselType != VesselType.Flag)
						continue;

					if (v.mainBody != body)
						continue;

					mapFlags.Add(v);

					vessels.Add(v.id, new MapLabelInfo()
					{
						label = "",
						image = SCAN_UI_Loader.FlagIcon,
						pos = VesselPosition(v.id),
						baseColor = ColorToggle ? palette.cb_yellow : palette.cb_skyBlue,
						flash = false,
						width = 32,
						show = true
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
							continue;

						if (!a.Known)
							continue;

						anomalies.Add(a.Name, new MapLabelInfo()
							{
								label = "",
								image = SCAN_UI_Loader.AnomalyIcon,
								pos = MapPosition(a.Latitude, a.Longitude),
								baseColor = ColorToggle ? palette.cb_yellow : palette.cb_skyBlue,
								flash = false,
								width = 20,
								alignBottom = 8,
								show = true
							});
					}
				}

				return anomalies;
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
							continue;

						Vector2 wayPos = MapPosition(w.Latitude, w.Longitude);

						if (w.LandingTarget)
						{
							waypoints.Add(w.Seed, new MapLabelInfo()
							{
								label = "",
								image = SCAN_UI_Loader.MechJebIcon,
								pos = wayPos,
								baseColor = palette.red,
								flash = false,
								width = 20,
								alignBottom = 0,
								show = true
							});
						}
						else
						{
							waypoints.Add(w.Seed, new MapLabelInfo()
							{
								label = "",
								image = SCAN_UI_Loader.WaypointIcon,
								pos = wayPos,
								baseColor = palette.white,
								flash = false,
								width = 20,
								alignBottom = 10,
								show = true
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
					return new KeyValuePair<Guid,MapLabelInfo>(new Guid(), new MapLabelInfo() { label = "null" });

				return new KeyValuePair<Guid,MapLabelInfo>(vessel.id, new MapLabelInfo()
				{
					label = "",
					image = SCAN_UI_Loader.VesselIcon(vessel.vesselType),
					pos = VesselPosition(vessel),
					baseColor = ColorToggle ? palette.white : palette.cb_skyBlue,
					flashColor = palette.cb_yellow,
					flash = true,
					width = 28,
					show = true
				});
			}
		}

		private Vector2d MousePosition(Vector2 pos)
		{
			float mx = pos.x;
			float my = pos.y * -1f;

			double mlo = (mx * 360 / bigmap.MapWidth) - 180;
			double mla = 90 - (my * 180 / bigmap.MapHeight);

			double mlon = bigmap.unprojectLongitude(mlo, mla);
			double mlat = bigmap.unprojectLatitude(mlo, mla);

			return new Vector2d(mlon, mlat);
		}

		public string MapInfo(Vector2 mapPos)
		{
			Vector2d pos = MousePosition(mapPos);

			double mlon = pos.x;
			double mlat = pos.y;

			if (mlon >= -180 && mlon <= 180 && mlat >= -90 && mlat <= 90)
				return mouseOverInfo(mlon, mlat);
			else
				return "";
		}

		private string mouseOverInfo(double lon, double lat)
		{
			infoString = StringBuilderCache.Acquire();

			bool altimetry = SCANUtil.isCovered(lon, lat, data, SCANtype.Altimetry);
			bool hires = SCANUtil.isCovered(lon, lat, data, SCANtype.AltimetryHiRes);

			if (SCANUtil.isCovered(lon, lat, data, SCANtype.AltimetryLoRes))
			{
				if (body.pqsController == null)
					infoString.Append(palette.coloredNoQuote(palette.c_bad, "LO "));
				else
					infoString.Append(palette.coloredNoQuote(palette.c_good, "LO "));
			}
			else
				infoString.Append(palette.coloredNoQuote(palette.grey, "LO "));

			if (hires)
			{
				if (body.pqsController == null)
					infoString.Append(palette.coloredNoQuote(palette.c_bad, "HI "));
				else
					infoString.Append(palette.coloredNoQuote(palette.c_good, "HI "));
			}
			else
				infoString.Append(palette.coloredNoQuote(palette.grey, "HI "));

			if (SCANUtil.isCovered(lon, lat, data, SCANtype.Biome))
			{
				if (body.BiomeMap == null)
					infoString.Append(palette.coloredNoQuote(palette.c_bad, "MULTI"));
				else
					infoString.Append(palette.coloredNoQuote(palette.c_good, "MULTI"));
			}
			else
				infoString.Append(palette.coloredNoQuote(palette.grey, "MULTI"));

			if (altimetry)
			{
				infoString.AppendFormat(" Terrain Height: {0}", SCANuiUtil.getMouseOverElevation(lon, lat, data, 2, hires));

				if (hires)
				{
					double circum = body.Radius * 2 * Math.PI;
					double eqDistancePerDegree = circum / 360;
					double degreeOffset = 5 / eqDistancePerDegree;

					infoString.AppendFormat(" Slope: {0:F1}°", SCANUtil.slope(SCANUtil.getElevation(body, lon, lat), body, lon, lat, degreeOffset));
				}
			}

			if (SCANUtil.isCovered(lon, lat, data, SCANtype.Biome))
				infoString.AppendFormat(" Biome: {0}", SCANUtil.getBiomeDisplayName(body, lon, lat));

			if (bigmap.ResourceActive && SCANconfigLoader.GlobalResource && bigmap.Resource != null)
			{
				bool resources = false;
				bool fuzzy = false;

				if (SCANUtil.isCovered(lon, lat, data, bigmap.Resource.SType))
				{
					resources = true;
				}
				else if (SCANUtil.isCovered(lon, lat, data, SCANtype.FuzzyResources))
				{
					resources = true;
					fuzzy = true;
				}

				if (resources)
					infoString.AppendFormat(" {0}", SCANuiUtil.getResourceAbundance(bigmap.Body, lat, lon, fuzzy, bigmap.Resource));
			}
			
			infoString.AppendLine();
			infoString.AppendFormat("{0} (lat: {1:F2}° lon: {2:F2}°)", SCANuiUtil.toDMS(lat, lon), lat, lon);

			double range = ContractDefs.Survey.MaximumTriggerRange;

			if (SCANcontroller.controller.bigMapWaypoint)
			{
				for (int i = data.Waypoints.Count - 1; i >= 0; i--)
				{
					SCANwaypoint p = data.Waypoints[i];

					if (!p.LandingTarget)
					{
						if (p.Root != null)
						{
							if (p.Root.ContractState != Contracts.Contract.State.Active)
								continue;
						}
						if (p.Param != null)
						{
							if (p.Param.State != Contracts.ParameterState.Incomplete)
								continue;
						}

						if (SCANUtil.waypointDistance(lat, lon, 1000, p.Latitude, p.Longitude, 1000, body) <= range)
						{
							infoString.AppendFormat(" Waypoint: {0}", p.Name);
							break;
						}
					}
					else if (SCANUtil.waypointDistance(lat, lon, 1000, p.Latitude, p.Longitude, 1000, body) <= range)
					{
						infoString.AppendFormat(" MechJeb Target: {0}", SCANuiUtil.toDMS(p.Latitude, p.Longitude, 0));
						break;
					}
				}
			}

			if (SCANcontroller.controller.bigMapAnomaly)
			{
				for (int i = data.Anomalies.Length - 1; i >= 0; i--)
				{
					SCANanomaly a = data.Anomalies[i];

					if (a.Known)
					{
						if (SCANUtil.mapLabelDistance(lat, lon, a.Latitude, a.Longitude, body) <= range)
						{
							if (a.Detail)
							{
								infoString.Append(" Anomaly: ");
								infoString.Append(a.Name);
							}
							else
							{
								infoString.Append(" Anomaly: Unknown");
							}
							break;
						}
					}
				}
			}

			if (SCANcontroller.controller.bigMapFlag)
			{
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

			return infoString.ToStringAndRelease();
		}

		public string TooltipText(float xPos)
		{
			switch (bigmap.MType)
			{
				case mapType.Biome:
					if (body.BiomeMap == null || body.BiomeMap.Attributes == null)
						return "";

					int count = body.BiomeMap.Attributes.Length;

					int blockSize = (int)Math.Truncate(256 / (count * 1d));

					int current = (int)Math.Truncate((xPos * 256) / (blockSize * 1d));

					if (current >= count)
						current = count - 1;
					else if (current < 0)
						current = 0;

					return body.BiomeMap.Attributes[current].displayname;
				case mapType.Altimetry:
					float terrain = xPos * data.TerrainConfig.TerrainRange + data.TerrainConfig.MinTerrain;

					return string.Format("{0:N0}m", terrain);
			}

			return "";
		}

		public void SetWaypoint(string id, Vector2 pos)
		{
			if (string.IsNullOrEmpty(id))
				id = RandomWaypoint;

			pos.y -= bigmap.MapHeight;

			Vector2d mapPos = MousePosition(pos);

			if (mapPos.x < -180 || mapPos.x > 180 || mapPos.y < -90 || mapPos.y > 90)
				return;

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
			pos.y -= bigmap.MapHeight;

			Vector2d mapPos = MousePosition(pos);

			if (mapPos.x < -180 || mapPos.x > 180 || mapPos.y < -90 || mapPos.y > 90)
				return;

			SCANcontroller.controller.MJTargetSet.Invoke(mapPos, body);

			SCANwaypoint w = new SCANwaypoint(mapPos.y, mapPos.x, "MechJeb Landing Target");
			data.addToWaypoints(w);
		}

		public void ClickMap(Vector2 pos)
		{
			Vector2d mapPos = MousePosition(pos);

			if (SCANcontroller.controller.zoomMapVesselLock)
				return;

			if (SCAN_UI_ZoomMap.Instance == null)
				return;

			if (SCAN_UI_ZoomMap.Instance.IsVisible)
				SCAN_UI_ZoomMap.Instance.Close();

			SCAN_UI_ZoomMap.Instance.Open(false, mapPos.y, mapPos.x, bigmap);
		}

		public void RefreshMap()
		{
			bigmap.resetMap(SCANcontroller.controller.bigMapResourceOn);

			uiElement.SetLegend(LegendToggle);
		}

		public void OpenMainMap()
		{
			if (SCAN_UI_MainMap.Instance.IsVisible)
				SCAN_UI_MainMap.Instance.Close();
			else
				SCAN_UI_MainMap.Instance.Open();
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

		public void ExportMap()
		{
			if (bigmap.isMapComplete())
				bigmap.exportPNG();
		}

		public void setMapWidth(int width)
		{
			if (bigmap == null)
				return;

			bigmap.setWidth(width);
			SCAN_Settings_Config.Instance.BigMapWidth = bigmap.MapWidth;
		}

		public void ResetPosition()
		{
			SCAN_Settings_Config.Instance.BigMapPosition = new Vector2(400, -400);

			if (uiElement != null)
				uiElement.SetPosition(SCAN_Settings_Config.Instance.BigMapPosition);
		}

	}
}
