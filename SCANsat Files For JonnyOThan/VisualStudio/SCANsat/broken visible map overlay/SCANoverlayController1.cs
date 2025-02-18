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
using SCANsat.SCAN_Data;
using SCANsat.SCAN_UI.UI_Framework;
using SCANsat.SCAN_Platform;
using UnityEngine;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANpalette;

namespace SCANsat.SCAN_UI
{
	class SCANoverlayController : SCAN_MBW
	{
		internal readonly static Rect defaultRect = new Rect(Screen.width - 280, 200, 300, 100);
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

		private int timer;
		private string tooltipText = "";

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
			WindowOptions = new GUILayoutOption[2] { GUILayout.Width(300), GUILayout.Height(100) };
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
					if (mapGenerating)
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

			if (GUILayout.Button("Biome Map", selection == (resources.Count) ? SCANskins.SCAN_labelLeftActive : SCANskins.SCAN_labelLeft))
			{
				if (mapGenerating)
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

			if (GUILayout.Button("Terrain Map", selection == (resources.Count + 1) ? SCANskins.SCAN_labelLeftActive : SCANskins.SCAN_labelLeft))
			{
				if (mapGenerating)
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

		private void resourceSettings(int id)
		{
			fillS();

			SCANcontroller.controller.planetaryOverlayTooltips = GUILayout.Toggle(SCANcontroller.controller.planetaryOverlayTooltips, "Tooltips", SCANskins.SCAN_settingsToggle);

			if (GUILayout.Button("Resource Settings"))
			{
				SCANcontroller.controller.resourceSettings.Visible = !SCANcontroller.controller.resourceSettings.Visible;
			}

			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Visual On"))
			{
				getMeshTexture();
				rescaleMap();

				mesh.material.SetTexture("_MainTex", (Texture)smallScaledMap);

				SCANUtil.SCANdebugLog("Map On");
			}

			if (GUILayout.Button("Visual Off"))
			{
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

			GUILayout.Label("Albedo:", SCANskins.SCAN_labelSmallLeft);

			fillS();

			if (GUILayout.Button("-", SCANskins.SCAN_buttonSmall, GUILayout.Width(18)))
			{
				body.albedo = Math.Max(0, body.albedo - 0.1);
				SCANUtil.SCANdebugLog("Albedo {0:F2}", body.albedo);
			}
			GUILayout.Label(body.albedo.ToString("F2"), SCANskins.SCAN_labelSmall, GUILayout.Width(36));
			if (GUILayout.Button("+", SCANskins.SCAN_buttonSmall, GUILayout.Width(18)))
			{
				body.albedo = Math.Min(1, body.albedo + 0.1);
				SCANUtil.SCANdebugLog("Albedo {0:F2}", body.albedo);
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

			GUILayout.Label("Emissivity:", SCANskins.SCAN_labelSmallLeft);

			fillS();

			if (GUILayout.Button("-", SCANskins.SCAN_buttonSmall, GUILayout.Width(18)))
			{
				body.emissivity = Math.Max(0, body.emissivity - 0.1);
				SCANUtil.SCANdebugLog("Emissivity {0:F2}", body.emissivity);
			}
			GUILayout.Label(body.emissivity.ToString("F2"), SCANskins.SCAN_labelSmall, GUILayout.Width(36));
			if (GUILayout.Button("+", SCANskins.SCAN_buttonSmall, GUILayout.Width(18)))
			{
				body.emissivity = Math.Min(1, body.emissivity + 0.1);
				SCANUtil.SCANdebugLog("Emissivity {0:F2}", body.emissivity);
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

			if (GUILayout.Button("Shadow Cast"))
			{
				mesh.castShadows = !mesh.castShadows;
				SCANUtil.SCANdebugLog("Shadow Cast {0}", mesh.castShadows);
			}

			if (GUILayout.Button("Shadow Receive"))
			{
				mesh.receiveShadows = !mesh.receiveShadows;
				SCANUtil.SCANdebugLog("Shadow Receive {0}", mesh.receiveShadows);
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

			if (GUILayout.Button("Multi"))
			{
				multi = !multi;
				SCANUtil.SCANdebugLog("Multi Set To {0}", multi);
			}

			if (GUILayout.Button("Reset Main"))
			{
				oldMainTex = mesh.material.GetTexture("_MainTex");
			}

			GUILayout.EndHorizontal();
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
		private int pass;
		private float alpha = 1;

		private void getMeshTexture()
		{
			newScaledMap = new Texture2D(oldMainTex.width, oldMainTex.height);

			var rt = RenderTexture.GetTemporary(oldMainTex.width, oldMainTex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, 1);

			blit(rt);

			RenderTexture.active = rt;

			newScaledMap.ReadPixels(new Rect(0, 0, oldMainTex.width, oldMainTex.height), 0, 0);

			RenderTexture.active = null;
			RenderTexture.ReleaseTemporary(rt);

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
			var pix = newScaledMap.GetPixels32();
			for (int i = 0; i < pix.Length; i++)
			{
				pix[i].a = (byte)(pix[i].a * alpha);
			}
			smallScaledMap = new Texture2D(newScaledMap.width, newScaledMap.height);
			smallScaledMap.SetPixels32(pix);
			smallScaledMap.Apply();

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

			if (selection == resources.Count)
				body.SetResourceMap(SCANuiUtil.drawBiomeMap(ref biomeOverlay, ref biomePixels, data, SCANcontroller.controller.overlayTransparency, SCANcontroller.controller.overlayMapHeight * 2));
			else if (selection == resources.Count + 1)
				StartCoroutine(setTerrainMap());
			else if (selection == resources.Count + 2)
				StartCoroutine(setSlopeMap());
			else
				body.SetResourceMap(SCANuiUtil.drawResourceTexture(ref mapOverlay, ref resourcePixels, ref abundanceValues, SCANcontroller.controller.overlayMapHeight, data, currentResource, SCANcontroller.controller.overlayInterpolation, SCANcontroller.controller.overlayTransparency));
		}

		private IEnumerator setTerrainMap()
		{
			if (data.Body.pqsController == null)
				yield return null;

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
				yield return null;

			if (!terrainGenerated)
			{
				SCANuiUtil.generateTerrainArray(ref terrainValues, 720, 4, data);
				terrainGenerated = true;
			}

			body.SetResourceMap(SCANuiUtil.drawTerrainMap(ref terrainOverlay, ref terrainPixels, ref terrainValues, data, 720, 4));
		}

		private IEnumerator setSlopeMap()
		{
			if (data.Body.pqsController == null)
				yield return null;

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
				yield return null;

			if (!terrainGenerated)
			{
				SCANuiUtil.generateTerrainArray(ref terrainValues, 720, 4, data);
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

			SCANUtil.SCANdebugLog("Access shader properties");

			SCANUtil.SCANdebugLog("Shader Passes = {0}", mesh.material.passCount);

			SCANUtil.SCANdebugLog("{0} albedo = {1}", body.bodyName, body.albedo);
			SCANUtil.SCANdebugLog("{0} emissivity = {1}", body.bodyName, body.emissivity);

			if (mesh.material.shader != null)
				SCANUtil.SCANdebugLog("Shader loaded");

			if (mesh.material.HasProperty("_MainTex"))
				SCANUtil.SCANdebugLog("Main Tex Present");
			if (mesh.material.HasProperty("_BumpMap"))
				SCANUtil.SCANdebugLog("Bump Map Present");
			if (mesh.material.HasProperty("_BackTex"))
				SCANUtil.SCANdebugLog("Back Tex Present");
			if (mesh.material.HasProperty("_BumpSpecMap"))
				SCANUtil.SCANdebugLog("Bump Map Spec Present");

			for (int i = 0; i < mesh.material.shaderKeywords.Length; i++)
			{
				SCANUtil.SCANdebugLog("Shader Keyword {0} = {1}", i, mesh.material.shaderKeywords[i]);
			}

			//int i = ShaderUtil.GetPropertyCount(mesh.material.shader);

			//for (int j = 0; j < i; j++ )
			//{
			//	string s = ShaderUtil.GetPropertyName(mesh.material.shader, j);
			//	ShaderUtil.ShaderPropertyType t = ShaderUtil.GetPropertyType(mesh.material.shader, j);
			//	SCANUtil.SCANdebugLog("Shader Property {0}: {1} ; Type {2}", j, s, t);
			//}

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
