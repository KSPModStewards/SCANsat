#region license
/*  [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANoverlayController - Window to control the planetary overlay maps
 * 
 * Copyright (c)2013 damny;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 DMagic
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using SCANsat.SCAN_Data;
using SCANsat.SCAN_UI.UI_Framework;
using SCANsat.SCAN_Platform;
using UnityEngine;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANpalette;

namespace SCANsat.SCAN_UI
{
	class SCANoverlayController : SCAN_MBW
	{
		internal readonly static Rect defaultRect = new Rect(Screen.width - 280, 200, 175, 100);
		private static Rect sessionRect = defaultRect;
		private CelestialBody body;
		private SCANdata data;
		private SCANresourceGlobal currentResource;
		private List<SCANresourceGlobal> resources;
		private List<PResource.Resource> resourceFractions;
		private bool drawOverlay;
		private bool oldOverlay;
		private bool terrainGenerated;
		private bool mapGenerating;
		private int selection;
		private double degreeOffset;
		private bool enableUI = true;
		private int mapStep, mapStart;
		private bool bodyBiome, bodyPQS;

		private int timer;
		private bool threadRunning;
		private string tooltipText = "";

		private bool drawMap;
		private int mapInt;

		private Texture2D mapOverlay;
		private Texture2D biomeOverlay;
		private Texture2D terrainOverlay;
		private Color32[] resourcePixels;
		private Color32[] biomePixels;
		private Color32[] terrainPixels;
		private float[,] abundanceValues;
		private float[,] terrainValues;

		protected override void Awake()
		{
			WindowCaption = "  S.C.A.N. Overlay";
			WindowRect = sessionRect;
			WindowStyle = SCANskins.SCAN_window;
			WindowOptions = new GUILayoutOption[2] { GUILayout.Width(175), GUILayout.Height(100) };
			Visible = false;
			DragEnabled = true;
			ClampToScreenOffset = new RectOffset(-120, -120, -100, -100);

			SCAN_SkinsLibrary.SetCurrent("SCAN_Unity");
		}

		protected override void Start()
		{
			GameEvents.onShowUI.Add(showUI);
			GameEvents.onHideUI.Add(hideUI);

			resources = SCANcontroller.setLoadedResourceList();

			setBody(HighLogic.LoadedSceneIsFlight ? FlightGlobals.currentMainBody : Planetarium.fetch.Home);
		}

		protected override void Update()
		{
			if ((MapView.MapIsEnabled && HighLogic.LoadedSceneIsFlight && FlightGlobals.ready) || HighLogic.LoadedScene == GameScenes.TRACKSTATION)
			{
				CelestialBody mapBody = SCANUtil.getTargetBody(MapView.MapCamera.target);

				if (mapBody == null)
					return;

				if (mapBody != body)
					setBody(mapBody);
			}
			else if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready)
			{
				if (body != FlightGlobals.currentMainBody)
					setBody(FlightGlobals.currentMainBody);
			}
		}

		protected override void OnDestroy()
		{
			GameEvents.onShowUI.Remove(showUI);
			GameEvents.onHideUI.Remove(hideUI);
		}

		public bool DrawOverlay
		{
			get { return drawOverlay; }
		}

		protected override void DrawWindowPre(int id)
		{

		}

		protected override void DrawWindow(int id)
		{
			versionLabel(id);				/* Standard version label and close button */
			closeBox(id);

			drawResourceList(id);
			overlayToggle(id);
			overlayOptions(id);
			resourceSettings(id);
		}

		protected override void DrawWindowPost(int id)
		{
			if (oldOverlay != drawOverlay)
			{
				oldOverlay = drawOverlay;
				if (oldOverlay)
					refreshMap();
				else
					OverlayGenerator.Instance.ClearDisplay();
			}

			sessionRect = WindowRect;
		}

		protected override void OnGUIEvery()
		{
			if (enableUI)
				mouseOverToolTip();
		}

		//Draw the version label in the upper left corner
		private void versionLabel(int id)
		{
			Rect r = new Rect(4, 0, 50, 18);
			GUI.Label(r, SCANmainMenuLoader.SCANsatVersion, SCANskins.SCAN_whiteReadoutLabel);
		}

		//Draw the close button in the upper right corner
		private void closeBox(int id)
		{
			Rect r = new Rect(WindowRect.width - 20, 1, 18, 18);
			if (GUI.Button(r, SCANcontroller.controller.closeBox, SCANskins.SCAN_closeButton))
			{
				Visible = false;
			}
		}

		private void drawResourceList(int id)
		{
			for (int i = 0; i < resources.Count; i++)
			{
				SCANresourceGlobal r = resources[i];

				if (r == null)
					continue;

				if (GUILayout.Button(r.Name, selection == i ? SCANskins.SCAN_labelLeftActive : SCANskins.SCAN_labelLeft))
				{
					if (mapGenerating || threadRunning)
						return;

					OverlayGenerator.Instance.ClearDisplay();

					if (selection != i)
					{
						selection = i;
						currentResource = r;
						currentResource.CurrentBodyConfig(body.name);
						oldOverlay = drawOverlay = true;
						refreshMap();
						return;
					}

					if (drawOverlay)
					{
						oldOverlay = drawOverlay = false;
					}
					else
					{
						oldOverlay = drawOverlay = true;
						refreshMap();
					}
				}
			}

			if (bodyBiome)
			{
				if (GUILayout.Button("Biome Map", selection == (resources.Count) ? SCANskins.SCAN_labelLeftActive : SCANskins.SCAN_labelLeft))
				{
					if (mapGenerating || threadRunning)
						return;

					OverlayGenerator.Instance.ClearDisplay();

					if (selection != resources.Count)
					{
						selection = resources.Count;
						oldOverlay = drawOverlay = true;
						refreshMap();
						return;
					}

					if (drawOverlay)
					{
						oldOverlay = drawOverlay = false;
					}
					else
					{
						oldOverlay = drawOverlay = true;
						refreshMap();
					}
				}
			}

			if (bodyPQS)
			{
				if (GUILayout.Button("Terrain Map", selection == (resources.Count + 1) ? SCANskins.SCAN_labelLeftActive : SCANskins.SCAN_labelLeft))
				{
					if (mapGenerating || threadRunning)
						return;

					OverlayGenerator.Instance.ClearDisplay();

					if (selection != resources.Count + 1)
					{
						selection = resources.Count + 1;
						oldOverlay = drawOverlay = true;
						refreshMap();
						return;
					}

					if (drawOverlay)
					{
						oldOverlay = drawOverlay = false;
					}
					else
					{
						oldOverlay = drawOverlay = true;
						refreshMap();
					}
				}
			}

			//if (GUILayout.Button("Slope Map", selection == (resources.Count + 2) ? SCANskins.SCAN_labelLeftActive : SCANskins.SCAN_labelLeft))
			//{
			//	if (mapGenerating)
			//		return;

			//	OverlayGenerator.Instance.ClearDisplay();

			//	if (selection != resources.Count + 2)
			//	{
			//		selection = resources.Count + 2;
			//		oldOverlay = drawOverlay = true;
			//		refreshMap();
			//		return;
			//	}

			//	if (drawOverlay)
			//	{
			//		oldOverlay = drawOverlay = false;
			//	}
			//	else
			//	{
			//		oldOverlay = drawOverlay = true;
			//		refreshMap();
			//	}
			//}
		}

		private void overlayToggle(int id)
		{
			drawOverlay = GUILayout.Toggle(drawOverlay, "Draw Overlay", SCANskins.SCAN_settingsToggle);
		}

		private void overlayOptions(int id)
		{
			if (!drawOverlay)
				return;

			if (mapGenerating)
				return;

			if (GUILayout.Button("Refresh"))
				refreshMap();

			if (selection >= resources.Count)
				return;
		}

		Texture2D scaledMap;
		MeshRenderer shader;
		Texture mainTex;

		private void resourceSettings(int id)
		{
			fillS();

			SCANcontroller.controller.planetaryOverlayTooltips = GUILayout.Toggle(SCANcontroller.controller.planetaryOverlayTooltips, "Tooltips", SCANskins.SCAN_settingsToggle);

			if (GUILayout.Button("Resource Settings"))
			{
				SCANcontroller.controller.resourceSettings.Visible = !SCANcontroller.controller.resourceSettings.Visible;
			}

			if (GUILayout.Button("MeshRender On"))
			{
				drawMap = !drawMap;

				if (!drawMap)
				{
					//OverlayGenerator.Instance.ClearDisplay();
					Destroy(scaledMap);
					return;
				}

				if (shader == null)
					shader = body.scaledBody.GetComponent<MeshRenderer>();

				if (mainTex == null)
					mainTex = shader.material.GetTexture("_MainTex");

				if (scaledMap == null)
					scaledMap = new Texture2D(mainTex.width, mainTex.height, TextureFormat.RGB24, true);

				var rt = RenderTexture.GetTemporary(mainTex.width, mainTex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, 1);

				Graphics.Blit(mainTex, rt, shader.material);

				RenderTexture.active = rt;

				scaledMap.ReadPixels(new Rect(0, 0, scaledMap.width, scaledMap.height), 0, 0);
				scaledMap.Apply();

				RenderTexture.active = null;
				RenderTexture.ReleaseTemporary(rt);

				//TextureScale.Bilinear(scaledMap, 128, 64);

				body.SetResourceMap(scaledMap);

				//shader.material.SetTexture("_MainTex", (Texture)scaledMap);
				//shader.material.SetTexture("_BumpMap", null);
			}


			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Visual On"))
			{
				if (twice)
				{
					getMeshTexture();
					body.SetResourceMap(newScaledMap);
				}
				else
				{
					//Destroy(newScaledMap);

					getMeshTexture();

					OverlayGenerator.Instance.ClearDisplay();

					rescaleMap();

					mesh.material.SetTexture("_MainTex", (Texture)smallScaledMap);
					mesh.material.SetFloat("_Shininess", shiny);
				}

				SCANUtil.SCANdebugLog("Map On");
			}

			if (GUILayout.Button("Visual Off"))
			{
				if (twice)
				{
					Destroy(newScaledMap);
				}
				else
					mesh.material.SetTexture("_MainTex", oldMainTex);
				SCANUtil.SCANdebugLog("Map Off");
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Bump On"))
			{
				mesh.material.SetTexture("_BumpMap", oldBumpMap);
				SCANUtil.SCANdebugLog("Bump Map On");
			}

			if (GUILayout.Button("Bump Off"))
			{
				mesh.material.SetTexture("_BumpMap", null);
				SCANUtil.SCANdebugLog("Bump Map Off");
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

			GUILayout.Label("Visual Map Height:", SCANskins.SCAN_labelSmallLeft);

			fillS();

			if (GUILayout.Button("-", SCANskins.SCAN_buttonSmall, GUILayout.Width(18)))
			{
				visualHeight = Math.Max(64, visualHeight / 2);
				SCANUtil.SCANdebugLog("Height {0}", visualHeight);
			}
			GUILayout.Label(visualHeight.ToString(), SCANskins.SCAN_labelSmall, GUILayout.Width(36));
			if (GUILayout.Button("+", SCANskins.SCAN_buttonSmall, GUILayout.Width(18)))
			{
				visualHeight = Math.Min(512, visualHeight * 2);
				SCANUtil.SCANdebugLog("Height {0}", visualHeight);
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

			GUILayout.Label("Scale Type:", SCANskins.SCAN_labelSmallLeft);

			fillS();

			if (GUILayout.Button("-", SCANskins.SCAN_buttonSmall, GUILayout.Width(18)))
			{
				rescaleType = Math.Max(0, rescaleType - 1);
				SCANUtil.SCANdebugLog("Rescale Type {0}", rescaleType);
			}
			GUILayout.Label(rescaleType.ToString(), SCANskins.SCAN_labelSmall, GUILayout.Width(36));
			if (GUILayout.Button("+", SCANskins.SCAN_buttonSmall, GUILayout.Width(18)))
			{
				rescaleType = Math.Min(1, rescaleType + 1);
				SCANUtil.SCANdebugLog("Rescale Type {0}", rescaleType);
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

			GUILayout.Label("Alpha:", SCANskins.SCAN_labelSmallLeft);

			fillS();

			if (GUILayout.Button("-", SCANskins.SCAN_buttonSmall, GUILayout.Width(18)))
			{
				alpha = (float)Math.Max(0, alpha - 0.05);
				SCANUtil.SCANdebugLog("Alpha {0:P0}", alpha);
			}
			GUILayout.Label(alpha.ToString("P0"), SCANskins.SCAN_labelSmall, GUILayout.Width(36));
			if (GUILayout.Button("+", SCANskins.SCAN_buttonSmall, GUILayout.Width(18)))
			{
				alpha = (float)Math.Min(1, alpha + 0.05);
				SCANUtil.SCANdebugLog("Alpha {0:P0}", alpha);
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

			GUILayout.Label("Shiny:", SCANskins.SCAN_labelSmallLeft);

			fillS();

			if (GUILayout.Button("-", SCANskins.SCAN_buttonSmall, GUILayout.Width(18)))
			{
				shiny = (float)Math.Max(0, shiny - 0.05);
				SCANUtil.SCANdebugLog("shiny {0:P0}", shiny);
			}
			GUILayout.Label(shiny.ToString("P0"), SCANskins.SCAN_labelSmall, GUILayout.Width(36));
			if (GUILayout.Button("+", SCANskins.SCAN_buttonSmall, GUILayout.Width(18)))
			{
				shiny = (float)Math.Min(1, shiny + 0.05);
				SCANUtil.SCANdebugLog("shiny {0:P0}", shiny);
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

			GUILayout.Label("Pass:", SCANskins.SCAN_labelSmallLeft);

			fillS();

			if (GUILayout.Button("-", SCANskins.SCAN_buttonSmall, GUILayout.Width(18)))
			{
				pass = Math.Max(-1, pass - 1);
				SCANUtil.SCANdebugLog("Pass {0}", pass);
			}
			GUILayout.Label(pass.ToString(), SCANskins.SCAN_labelSmall, GUILayout.Width(36));
			if (GUILayout.Button("+", SCANskins.SCAN_buttonSmall, GUILayout.Width(18)))
			{
				pass = Math.Min(mesh.material.passCount - 1, pass + 1);
				SCANUtil.SCANdebugLog("Pass {0}", pass);
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Twice"))
			{
				twice = !twice;
				SCANUtil.SCANdebugLog("Twice Set To {0}", twice);
			}

			if (GUILayout.Button("Reset Main"))
			{
				mesh.material.SetTexture("_MainTex", oldMainTex);
			}

			GUILayout.EndHorizontal();

			//if (GUILayout.Button("Biome Summary"))
			//{
			//	foreach (ResourceCache.AbundanceSummary a in ResourceCache.Instance.AbundanceCache)
			//	{
			//		if (a.ResourceName == "Ore" && a.HarvestType == HarvestTypes.Planetary)
			//			SCANUtil.SCANlog("{0}: For {1} on Body {2} of scanner type {3}: Abundance = {4:P3}", a.ResourceName, a.BiomeName, a.BodyId, a.HarvestType, a.Abundance);
			//	}
			//}
		}

		private int visualHeight = 128;
		private int rescaleType = 0;
		private MeshRenderer mesh;
		private Texture oldMainTex;
		private Texture oldBumpMap;
		private Texture2D newScaledMap;
		private Texture2D smallScaledMap;
		private bool twice;
		private bool multi;
		private int pass = -1;
		private float alpha = 1;
		private float shiny;

		private void getMeshTexture()
		{
			//if (newScaledMap == null)
				newScaledMap = new Texture2D(oldMainTex.width, oldMainTex.height);

			var rt = RenderTexture.GetTemporary(oldMainTex.width, oldMainTex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, 1);

			blit(rt);

			RenderTexture.active = rt;

			newScaledMap.ReadPixels(new Rect(0, 0, oldMainTex.width, oldMainTex.height), 0, 0);

			RenderTexture.active = null;
			RenderTexture.ReleaseTemporary(rt);

			rt = null;

			newScaledMap.Apply();
		}

		private void blit(RenderTexture t)
		{
			if (!multi)
				Graphics.Blit(oldMainTex, t, mesh.material, pass);
			else
				Graphics.BlitMultiTap(oldMainTex, t, mesh.material);
		}

		private void rescaleMap()
		{
			smallScaledMap = newScaledMap;

			if (alpha < 0.95f)
			{
				var pix = smallScaledMap.GetPixels32();

				for (int i = 0; i < pix.Length; i++)
				{
					pix[i].a = (byte)(pix[i].a * alpha);
				}

				smallScaledMap.SetPixels32(pix);
				smallScaledMap.Apply();

				pix = null;
			}

			switch (rescaleType)
			{
				case 0:
					TextureScale.Bilinear(smallScaledMap, visualHeight * 2, visualHeight);
					break;
				case 1:
					TextureScale.Point(smallScaledMap, visualHeight * 2, visualHeight);
					break;
				default:
					break;
			}
		}

		private void mouseOverToolTip()
		{
			if (!drawOverlay)
				return;

			if (SCANcontroller.controller == null)
				return;

			if (!SCANcontroller.controller.planetaryOverlayTooltips)
				return;

			if ((MapView.MapIsEnabled && HighLogic.LoadedSceneIsFlight && FlightGlobals.ready) || HighLogic.LoadedScene == GameScenes.TRACKSTATION)
			{
				if (SCANUtil.MouseIsOverWindow())
					return;

				SCANUtil.SCANCoordinates coords = SCANUtil.GetMouseCoordinates(body);

				if (coords == null)
					return;

				if (timer < 5)
				{
					timer++;
					drawToolTipLabel();
					return;
				}

				timer = 0;
				tooltipText = "";

				tooltipText += coords.ToDMS();

				if (body.pqsController != null)
				{
					if (SCANUtil.isCovered(coords.longitude, coords.latitude, data, SCANtype.Altimetry))
					{
						double elevation = SCANUtil.getElevation(body, coords.longitude, coords.latitude);

						tooltipText += string.Format("\nTerrain: {0}", SCANuiUtil.getMouseOverElevation(coords.longitude, coords.latitude, data, 0));

						tooltipText += string.Format("\nSlope: {0:F1}°", SCANUtil.slope(elevation, body, coords.longitude, coords.latitude, degreeOffset));
					}
				}

				if (body.BiomeMap != null)
				{
					if (SCANUtil.isCovered(coords.longitude, coords.latitude, data, SCANtype.Biome))
						tooltipText += string.Format("\nBiome: {0}", SCANUtil.getBiomeName(body, coords.longitude, coords.latitude));
				}

				bool resources = false;
				bool fuzzy = false;

				if (SCANUtil.isCovered(coords.longitude, coords.latitude, data, currentResource.SType))
				{
					resources = true;
				}
				else if (SCANUtil.isCovered(coords.longitude, coords.latitude, data, SCANtype.FuzzyResources))
				{
					resources = true;
					fuzzy = true;
				}

				if (resources)
				{
					if (SCANcontroller.controller.needsNarrowBand)
					{
						bool coverage = false;
						bool scanner = false;

						foreach (Vessel vessel in FlightGlobals.Vessels)
						{
							if (vessel.protoVessel.protoPartSnapshots.Count <= 1)
								continue;

							if (vessel.vesselType == VesselType.Debris || vessel.vesselType == VesselType.Unknown || vessel.vesselType == VesselType.EVA || vessel.vesselType == VesselType.Flag)
								continue;

							if (vessel.mainBody != body)
								continue;

							if (vessel.situation != Vessel.Situations.ORBITING)
								continue;

							if (inc(vessel.orbit.inclination) < Math.Abs(coords.latitude))
							{
								coverage = true;
								continue;
							}

							var scanners = from pref in vessel.protoVessel.protoPartSnapshots
										   where pref.modules.Any(a => a.moduleName == "ModuleResourceScanner")
										   select pref;

							if (scanners.Count() == 0)
								continue;

							foreach (var p in scanners)
							{
								if (p.partInfo == null)
									continue;

								ConfigNode node = p.partInfo.partConfig;

								if (node == null)
									continue;

								var moduleNodes = from nodes in node.GetNodes("MODULE")
												  where nodes.GetValue("name") == "ModuleResourceScanner"
												  select nodes;

								foreach (ConfigNode moduleNode in moduleNodes)
								{
									if (moduleNode == null)
										continue;

									if (moduleNode.GetValue("ScannerType") != "0")
										continue;

									if (moduleNode.GetValue("ResourceName") != currentResource.Name)
										continue;

									if (moduleNode.HasValue("MaxAbundanceAltitude") && !vessel.Landed)
									{
										string alt = moduleNode.GetValue("MaxAbundanceAltitude");
										float f = 0;
										if (!float.TryParse(alt, out f))
											continue;

										if (f < vessel.altitude)
										{
											coverage = true;
											continue;
										}
									}

									coverage = false;
									scanner = true;
									break;
								}
								if (scanner)
									break;
							}
							if (scanner)
								break;
						}

						if (coverage)
							tooltipText += string.Format("\n{0}: No Coverage", currentResource.Name);
						else if (!scanner)
							tooltipText += string.Format("\n{0}: No Scanner", currentResource.Name);
						else
							resourceLabel(ref tooltipText, fuzzy, coords.latitude, coords.longitude);
					}
					else
						resourceLabel(ref tooltipText, fuzzy, coords.latitude, coords.longitude);
				}

				drawToolTipLabel();
			}
		}

		private void resourceLabel(ref string t, bool fuzz, double lat, double lon)
		{
			if (fuzz)
				t += string.Format("\n{0}: {1:P0}", currentResource.Name, SCANUtil.ResourceOverlay(lat, lon, currentResource.Name, body, SCANcontroller.controller.resourceBiomeLock));
			else
				t += string.Format("\n{0}: {1:P2}", currentResource.Name, SCANUtil.ResourceOverlay(lat, lon, currentResource.Name, body, SCANcontroller.controller.resourceBiomeLock));
		}

		private void drawToolTipLabel()
		{
			Vector2 size = SCANskins.SCAN_readoutLabelCenter.CalcSize(new GUIContent(tooltipText));

			float sizeX = size.x;
			if (sizeX < 160)
				sizeX = 160;
			else if (sizeX < 190)
				sizeX = 190;

			Rect r = new Rect(Event.current.mousePosition.x - (sizeX / 2), Event.current.mousePosition.y - (size.y + 16), sizeX + 10, size.y + 8);

			GUI.Box(r, "");

			r.x += 5;
			r.y += 4;
			r.width -= 10;
			r.height -= 8;

			SCANuiUtil.drawLabel(r, tooltipText, SCANskins.SCAN_readoutLabelCenter, true, SCANskins.SCAN_shadowReadoutLabelCenter);
		}

		public void refreshMap(float t, int height, int interp)
		{
			SCANcontroller.controller.overlayTransparency = t;
			SCANcontroller.controller.overlayMapHeight = height;
			SCANcontroller.controller.overlayInterpolation = interp;
			if (drawOverlay)
				refreshMap();
		}

		private void refreshMap()
		{
			if (mapGenerating)
				return;
			if (threadRunning)
				return;

			if (selection == resources.Count)
				body.SetResourceMap(SCANuiUtil.drawBiomeMap(ref biomeOverlay, ref biomePixels, data, SCANcontroller.controller.overlayTransparency, SCANcontroller.controller.overlayMapHeight * 2));
			else if (selection == resources.Count + 1)
				StartCoroutine(setTerrainMap());
			else if (selection == resources.Count + 2)
				StartCoroutine(setSlopeMap());
			else
				StartCoroutine(setOverlayMap());
				//body.SetResourceMap(SCANuiUtil.drawResourceTexture(ref mapOverlay, ref resourcePixels, ref abundanceValues, SCANcontroller.controller.overlayMapHeight, data, currentResource, SCANcontroller.controller.overlayInterpolation, SCANcontroller.controller.overlayTransparency));
		}

		private IEnumerator setOverlayMap()
		{
			int timer = 0;

			mapGenerating = true;

			SCANuiUtil.generateOverlayResourceValues(ref abundanceValues, SCANcontroller.controller.overlayMapHeight, data, currentResource, SCANcontroller.controller.overlayInterpolation);

			SCANdata copy = new SCANdata(data);

			SCANUtil.SCANlog("Starting Resource Thread");

			Thread t = new Thread( () => resourceThreadRun(SCANcontroller.controller.overlayMapHeight, SCANcontroller.controller.overlayInterpolation, SCANcontroller.controller.overlayTransparency, new System.Random(ResourceScenario.Instance.gameSettings.Seed), copy));
			t.Start();

			SCANUtil.SCANlog("Resource Thread Started...");

			while (threadRunning && timer < 1000)
			{
				SCANUtil.SCANlog("Resource Thread Running...");
				timer++;
				yield return null;
			}

			if (timer >= 1000)
			{
				t.Abort();
				threadRunning = false;
				mapGenerating = false;
				yield break;
			}

			mapGenerating = false;

			copy = null;

			SCANUtil.SCANlog("Resource Thread Finished; {0} Frames Used", timer);

			if (mapOverlay == null || mapOverlay.height != SCANcontroller.controller.overlayMapHeight)
				mapOverlay = new Texture2D(SCANcontroller.controller.overlayMapHeight * 2, SCANcontroller.controller.overlayMapHeight, TextureFormat.ARGB32, true);

			mapOverlay.SetPixels32(resourcePixels);
			mapOverlay.Apply();

			body.SetResourceMap(mapOverlay);
		}

		private void resourceThreadRun(int height, int step, float transparent, System.Random r, SCANdata copyData)
		{
			threadRunning = true;

			SCANuiUtil.generateOverlayResourcePixels(ref resourcePixels, ref abundanceValues, height, copyData, currentResource, r, step, transparent);

			threadRunning = false;
		}

		private IEnumerator setTerrainMap()
		{
			if (data.Body.pqsController == null)
				yield break;

			int timer = 0;

			while (!data.Built && timer < 2000)
			{
				mapGenerating = true;
				if (!data.Building)
				{
					data.ExternalBuilding = true;
					data.generateHeightMap(ref mapStep, ref mapStart, 360);
				}
				timer++;
				yield return null;
			}

			if (timer >= 2000)
			{
				mapGenerating = false;
				yield break;
			}

			timer = 0;

			SCANdata copy = new SCANdata(data);
			int index = data.Body.flightGlobalsIndex;

			SCANUtil.SCANlog("Starting Terrain Thread");

			Thread t = new Thread( () => terrainThreadRun(copy, index));
			t.Start();

			SCANUtil.SCANlog("Terrain Thread Started...");

			while (threadRunning && timer < 1000)
			{
				SCANUtil.SCANlog("Terrain Thread Running...");
				timer++;
				yield return null;
			}

			if (timer >= 1000)
			{
				t.Abort();
				threadRunning = false;
				mapGenerating = false;
				yield break;
			}

			mapGenerating = false;

			copy = null;

			SCANUtil.SCANlog("Terrain Thread Finished; {0} Frames Used", timer);

			if (terrainOverlay == null)
				terrainOverlay = new Texture2D(1440, 720, TextureFormat.ARGB32, true);

			terrainOverlay.SetPixels32(terrainPixels);
			terrainOverlay.Apply();

			body.SetResourceMap(terrainOverlay);
		}

		private void terrainThreadRun(SCANdata copyData, int i)
		{
			threadRunning = true;

			if (!terrainGenerated)
			{
				SCANuiUtil.generateTerrainArray(ref terrainValues, 720, 4, copyData, i);
				terrainGenerated = true;
			}

			SCANuiUtil.drawTerrainMap(ref terrainPixels, ref terrainValues, copyData, 720, 4);

			threadRunning = false;
		}

		private IEnumerator setSlopeMap()
		{
			if (data.Body.pqsController == null)
				yield break;

			int timer = 0;

			while (!data.Built && timer < 2000)
			{
				mapGenerating = true;
				if (!data.Building)
				{
					data.ExternalBuilding = true;
					data.generateHeightMap(ref mapStep, ref mapStart, 360);
				}
				timer++;
				yield return null;
			}

			mapGenerating = false;

			if (timer >= 2000)
				yield break;

			if (!terrainGenerated)
			{
				SCANuiUtil.generateTerrainArray(ref terrainValues, 720, 4, data, data.Body.flightGlobalsIndex);
				terrainGenerated = true;
			}

			body.SetResourceMap(SCANuiUtil.drawSlopeMap(ref terrainOverlay, ref terrainPixels, ref terrainValues, data, 720, 4));
		}

		private void setBody(CelestialBody B)
		{
			body = B;

			mesh = body.scaledBody.GetComponent<MeshRenderer>();
			oldMainTex = mesh.material.GetTexture("_MainTex");
			oldBumpMap = mesh.material.GetTexture("_BumpMap");

			if (mesh.material.HasProperty("_ResourceMap"))
				SCANUtil.SCANdebugLog("Resource Map Present");
			if (mesh.material.HasProperty("_SpecColor"))
				SCANUtil.SCANdebugLog("Spec Color Present");
			if (mesh.material.HasProperty("_Shininess"))
				SCANUtil.SCANdebugLog("Shiny Present");
			if (mesh.material.HasProperty("_Opacity"))
				SCANUtil.SCANdebugLog("Opacity Present");

			var c = mesh.material.GetColor("_SpecColor");
			SCANUtil.SCANdebugLog("Spec Color {0}", c);

			var s = mesh.material.GetFloat("_Shininess");
			SCANUtil.SCANdebugLog("Shiny {0:F3}", s);
			shiny = s;

			var o = mesh.material.GetFloat("_Opacity");
			SCANUtil.SCANdebugLog("Opacity {0:F3}", o);

			data = SCANUtil.getData(body);
			if (data == null)
			{
				data = new SCANdata(body);
				SCANcontroller.controller.addToBodyData(body, data);
			}

			if (currentResource == null)
			{
				if (resources.Count > 0)
				{
					currentResource = resources[0];
					currentResource.CurrentBodyConfig(body.name);
				}
			}
			else
			{
				currentResource.CurrentBodyConfig(body.name);
			}

			bodyBiome = body.BiomeMap != null;
			bodyPQS = body.pqsController != null;

			terrainGenerated = false;

			if (drawOverlay)
				refreshMap();

			double circum = body.Radius * 2 * Math.PI;
			double eqDistancePerDegree = circum / 360;
			degreeOffset = 5 / eqDistancePerDegree;

			//resourceFractions = ResourceMap.Instance.GetResourceItemList(HarvestTypes.Planetary, body);
			//if (resources.Count > 0)
			//{
			//	currentResource = resources[0];
			//	currentResource.CurrentBodyConfig(body.name);

			//	//foreach (SCANresourceGlobal r in resources)
			//	//{
			//	//	SCANresourceBody b = r.getBodyConfig(body.name, false);
			//	//	if (b != null)
			//	//	{
			//	//		b.Fraction = resourceFractions.FirstOrDefault(a => a.resourceName == r.Name).fraction;
			//	//	}
			//	//}
			//}
		}

		private double inc(double d)
		{
			if (d > 90)
				d = 180 - d;

			return d;
		}

		private void showUI()
		{
			enableUI = true;
		}

		private void hideUI()
		{
			enableUI = false;
		}
	}
}
