#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_UI_Overlay - UI control object for SCANsat planetary overlay window
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
using UnityEngine.EventSystems;
using SCANsat.SCAN_Toolbar;
using SCANsat.Unity.Interfaces;
using SCANsat.Unity.Unity;
using SCANsat.SCAN_Data;
using SCANsat.SCAN_Map;
using SCANsat.SCAN_UI.UI_Framework;
using KSP.UI;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANcolorUtil;
using Log = KSPCommunityLib.Logging.Log;

namespace SCANsat.SCAN_Unity
{
	public class SCAN_UI_Overlay : ISCAN_Overlay
	{
		private bool _isVisible;
		private bool _overlayOn;

		private CelestialBody body;
		private SCANdata data;
		private SCANresourceGlobal currentResource;
		private List<SCANresourceGlobal> resources;

		private double degreeOffset;
		private bool bodyBiome, bodyPQS;

		private int timer;

		private StringBuilder tooltipText = new StringBuilder();
		private string tooltipString = string.Empty;
		private bool tooltipActive;

		private Texture2D mapOverlay;

		private Texture2D resourceLegend;
		private const int RESOURCELEGENDWIDTH = 156;

		private SCAN_Overlay uiElement;

		private static SCAN_UI_Overlay instance;

		public static SCAN_UI_Overlay Instance
		{
			get { return instance; }
		}

		public SCAN_UI_Overlay()
		{
			instance = this;

			resources = SCANcontroller.setLoadedResourceList();

			setBody(HighLogic.LoadedSceneIsFlight ? FlightGlobals.currentMainBody : Planetarium.fetch.Home);
		}

		public void OnDestroy()
		{
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

			if (mapOverlay != null)
			{
				GameObject.Destroy(mapOverlay);
				mapOverlay = null;
			}

			destroyOverlayMap();

			removeOverlay(true);
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
			tooltipActive = false;

			if ((MapView.MapIsEnabled && HighLogic.LoadedSceneIsFlight && FlightGlobals.ready) || HighLogic.LoadedScene == GameScenes.TRACKSTATION)
			{
				CelestialBody mapBody = SCANUtil.getTargetBody(MapView.MapCamera.target);

				if (mapBody == null)
				{
					return;
				}

				if (mapBody != body)
				{
					setBody(mapBody);
				}

				if (SCAN_Settings_Config.Instance.OverlayTooltips && _overlayOn)
				{
					SCANUtil.SCANCoordinates coords = SCANUtil.GetMouseCoordinates(body);

					if (coords != null)
					{
						tooltipActive = true;

						PointerEventData pe = new PointerEventData(EventSystem.current);
						pe.position = Input.mousePosition;
						List<RaycastResult> hits = new List<RaycastResult>();

						EventSystem.current.RaycastAll(pe, hits);

						for (int i = hits.Count - 1; i >= 0; i--)
						{
							RaycastResult r = hits[i];

							GameObject go = r.gameObject;

							if (go.layer == 5)
							{
								tooltipActive = false;
								break;
							}
						}

						if (tooltipActive)
						{
							MouseOverTooltip(coords);
						}
					}
				}

			}
			else if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready)
			{
				if (body != FlightGlobals.currentMainBody)
				{
					setBody(FlightGlobals.currentMainBody);
				}
			}
		}

		private void MouseOverTooltip(SCANUtil.SCANCoordinates coords)
		{
			if (timer < 5)
			{
				timer++;
				return;
			}

			timer = 0;

			tooltipText.Length = 0;

			coords.ToDMS(tooltipText);

			if (body.pqsController != null)
			{
				if (SCANUtil.isCovered(coords.longitude, coords.latitude, data, SCANtype.Altimetry))
				{
					bool hires = SCANUtil.isCovered(coords.longitude, coords.latitude, data, SCANtype.AltimetryHiRes);

					tooltipText.AppendLine();
					tooltipText.AppendFormat(string.Format("Terrain: {0}", SCANuiUtil.getMouseOverElevation(coords.longitude, coords.latitude, data, 0, hires)));

					if (hires)
					{
						tooltipText.AppendLine();
						tooltipText.AppendFormat(string.Format("Slope: {0}°", SCANUtil.slope(SCANUtil.getElevation(body, coords.longitude, coords.latitude), body, coords.longitude, coords.latitude, degreeOffset).ToString("F1")));
					}
				}
			}

			if (body.BiomeMap != null)
			{
				if (SCANUtil.isCovered(coords.longitude, coords.latitude, data, SCANtype.Biome))
				{
					tooltipText.AppendLine();
					tooltipText.AppendFormat(string.Format("Biome: {0}", SCANUtil.getBiomeDisplayName(body, coords.longitude, coords.latitude)));
				}
			}

			bool resources = false;
			bool fuzzy = false;

			if (SCANUtil.isCovered(coords.longitude, coords.latitude, data, SCANtype.ResourceHiRes))
			{
				resources = true;
			}
			else if (SCANUtil.isCovered(coords.longitude, coords.latitude, data, SCANtype.ResourceLoRes))
			{
				resources = true;
				fuzzy = true;
			}

			if (resources)
			{
				tooltipText.AppendLine();
				tooltipText.Append(SCANuiUtil.getResourceAbundance(body, coords.latitude, coords.longitude, fuzzy, currentResource));
			}

			tooltipString = tooltipText.ToString();
		}

		public void Open()
		{
			if (uiElement != null)
			{
				uiElement.gameObject.SetActive(false);
				MonoBehaviour.DestroyImmediate(uiElement.gameObject);
			}

			uiElement = GameObject.Instantiate(SCAN_UI_Loader.OverlayPrefab).GetComponent<SCAN_Overlay>();

			if (uiElement == null)
			{
				return;
			}

			uiElement.transform.SetParent(UIMasterController.Instance.dialogCanvas.transform, false);

			uiElement.SetOverlay(this);

			tooltipString = string.Empty;

			_isVisible = true;

			if (HighLogic.LoadedSceneIsFlight && SCAN_Settings_Config.Instance.StockToolbar && SCAN_Settings_Config.Instance.ToolbarMenu)
			{
				if (SCANappLauncher.Instance != null && SCANappLauncher.Instance.UIElement != null)
				{
					SCANappLauncher.Instance.UIElement.SetOverlayToggle(true);
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
					SCANappLauncher.Instance.UIElement.SetOverlayToggle(false);
				}
			}

			uiElement = null;
		}

		public string Version
		{
			get { return SCANmainMenuLoader.SCANsatVersion; }
		}

		public string CurrentResource
		{
			get { return currentResource == null ? "" : currentResource.DisplayName; }
		}

		public string TooltipText
		{
			get { return tooltipString; }
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

		public bool OverlayTooltip
		{
			get { return tooltipActive; }
		}

		public bool DrawOverlay
		{
			get { return _overlayOn; }
			set
			{
				if (value)
				{
					if (!_overlayOn)
					{
						refreshMap(SCANcontroller.controller.overlaySelection);
					}
				}
				else
				{
					removeOverlay();
				}
			}
		}

		public bool DrawBiome
		{
			get { return SCANcontroller.controller.overlaySelection == 0; }
			set
			{
				if (!value)
				{
					if (_overlayOn && SCANcontroller.controller.overlaySelection == 0)
					{
						removeOverlay();
					}

					return;
				}

				SCANcontroller.controller.overlaySelection = 0;

				refreshMap(0);
			}
		}

		public bool DrawTerrain
		{
			get { return SCANcontroller.controller.overlaySelection == 1; }
			set
			{
				if (!value)
				{
					if (_overlayOn && SCANcontroller.controller.overlaySelection == 1)
					{
						removeOverlay();
					}

					return;
				}

				SCANcontroller.controller.overlaySelection = 1;

				refreshMap(1);
			}
		}

		public bool DrawResource
		{
			get { return SCANcontroller.controller.overlaySelection == 2; }
		}

		public bool WindowTooltips
		{
			get { return SCAN_Settings_Config.Instance.WindowTooltips; }
		}

		public float Scale
		{
			get { return SCAN_Settings_Config.Instance.UIScale; }
		}

		public Canvas TooltipCanvas
		{
			get { return UIMasterController.Instance.tooltipCanvas; }
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
			get
			{
				if (currentResource != null)
				{
					SCANresourceBody resBody = currentResource.getBodyConfig(body.bodyName);

					if (resBody != null)
					{
						return new Vector2(resBody.MinValue / 100f, resBody.MaxValue / 100f);
					}

					return new Vector2(currentResource.DefaultMinValue / 100f, currentResource.DefaultMaxValue / 100f);
				}

				return new Vector2(0, 0.1f);
			}
		}

		public Vector2 Position
		{
			get { return SCAN_Settings_Config.Instance.OverlayPosition; }
			set { SCAN_Settings_Config.Instance.OverlayPosition = value; }
		}

		public void ClampToScreen(RectTransform rect)
		{
			UIMasterController.ClampToScreen(rect, Vector2.zero);
		}

		public void SetResource(string resource, bool isOn)
		{
			if (!isOn)
			{
				if (_overlayOn && SCANcontroller.controller.overlaySelection == 2 && currentResource != null && currentResource.DisplayName == resource)
				{
					removeOverlay();
				}

				return;
			}

			SCANcontroller.controller.overlaySelection = 2;

			if (currentResource.DisplayName != resource)
			{
				for (int i = resources.Count - 1; i >= 0; i--)
				{
					SCANresourceGlobal r = resources[i];

					if (r.DisplayName != resource)
					{
						continue;
					}

					currentResource = r;
					break;
				}
			}

			if (currentResource == null)
			{
				return;
			}

			SCANcontroller.controller.overlayResource = SCANUtil.resourceFromDisplayName(resource);

			refreshMap(2);
		}

		public void Refresh()
		{
			_overlayOn = true;

			refreshMap(SCANcontroller.controller.overlaySelection, true);

			if (SCANcontroller.controller.overlaySelection == 2)
			{
				uiElement.SetResourceLegend();
			}
		}

		public void OpenSettings()
		{
			if (SCAN_UI_Settings.Instance.IsVisible)
			{
				if (SCAN_UI_Settings.Instance.Page == 2)
				{
					SCAN_UI_Settings.Instance.Close();
				}
				else
				{
					SCAN_UI_Settings.Instance.Close();
					SCAN_UI_Settings.Instance.Open(2, true);
				}
			}
			else
			{
				SCAN_UI_Settings.Instance.Open(2);
			}
		}

		public void IncreaseResourceCutoff()
		{
			if (currentResource != null)
			{
				SCANresourceBody resBody = currentResource.getBodyConfig(body.bodyName);

				if (resBody != null)
				{
					float divisor = resBody.MaxValue / 10f;
					float min = resBody.MinValue;

					float current = min / divisor;
					float floor = Mathf.Floor(current);

					floor += 1;

					resBody.MinValue = floor * divisor;

					if (floor * divisor >= resBody.MaxValue)
					{
						resBody.MinValue = resBody.MaxValue - divisor;
					}
				}

				refreshMap(SCANcontroller.controller.overlaySelection, true);

				if (SCAN_UI_BigMap.Instance != null && SCAN_UI_BigMap.Instance.IsVisible && SCAN_UI_BigMap.Instance.ResourceToggle)
				{
					SCAN_UI_BigMap.Instance.RefreshMap();
				}

				if (SCAN_UI_ZoomMap.Instance != null && SCAN_UI_ZoomMap.Instance.IsVisible && SCAN_UI_ZoomMap.Instance.ResourceToggle)
				{
					SCAN_UI_ZoomMap.Instance.RefreshMap();
				}
			}
		}

		public void DecreaseResourceCutoff()
		{
			if (currentResource != null)
			{
				SCANresourceBody resBody = currentResource.getBodyConfig(body.bodyName);

				if (resBody != null)
				{
					float divisor = resBody.MaxValue / 10f;
					float min = resBody.MinValue;

					float current = min / divisor;
					float floor = Mathf.Floor(current);

					if (Mathf.FloorToInt(floor * 100) == Mathf.FloorToInt(current * 100))
					{
						floor -= 1;
					}

					resBody.MinValue = floor * divisor;

					if (floor * divisor >= resBody.MaxValue)
					{
						resBody.MinValue = resBody.MaxValue - divisor;
					}
				}

				refreshMap(SCANcontroller.controller.overlaySelection, true);

				if (SCAN_UI_BigMap.Instance != null && SCAN_UI_BigMap.Instance.IsVisible && SCAN_UI_BigMap.Instance.ResourceToggle)
				{
					SCAN_UI_BigMap.Instance.RefreshMap();
				}

				if (SCAN_UI_ZoomMap.Instance != null && SCAN_UI_ZoomMap.Instance.IsVisible && SCAN_UI_ZoomMap.Instance.ResourceToggle)
				{
					SCAN_UI_ZoomMap.Instance.RefreshMap();
				}
			}
		}

		public void OpenResourceSettings()
		{
			if (SCAN_UI_Settings.Instance.IsVisible)
			{
				if (SCAN_UI_Settings.Instance.Page == 4)
				{
					if (SCAN_UI_Settings.Instance.IsCurrentResource(body.bodyName, currentResource.DisplayName))
					{
						SCAN_UI_Settings.Instance.Close();
					}
					else
					{
						SCAN_UI_Settings.Instance.Close();
						ISCAN_Color col = SCAN_UI_Settings.Instance.ColorInterface;
						col.ResourcePlanet = body.bodyName;
						col.ResourceCurrent = currentResource.DisplayName;
						SCAN_UI_Settings.Instance.Open(4, true, true);
					}
				}
				else
				{
					SCAN_UI_Settings.Instance.Close();
					ISCAN_Color col = SCAN_UI_Settings.Instance.ColorInterface;
					col.ResourcePlanet = body.bodyName;
					col.ResourceCurrent = currentResource.DisplayName;
					SCAN_UI_Settings.Instance.Open(4, true, true);
				}
			}
			else
			{
				ISCAN_Color col = SCAN_UI_Settings.Instance.ColorInterface;
				col.ResourcePlanet = body.bodyName;
				col.ResourceCurrent = currentResource.DisplayName;
				SCAN_UI_Settings.Instance.Open(4, false, true);
			}
		}

		private void setBody(CelestialBody B)
		{
			body = B;

			data = SCANUtil.getData(body);
			if (data == null)
			{
				data = new SCANdata(body);
				SCANcontroller.controller.addToBodyData(body, data);
			}

			// The terrain overlay reads the body's height map; start it now so it is ready by the time
			// terrain is picked, instead of a few seconds of nothing while it builds on demand.
			SCANcontroller.controller.RequestHeightMap(data);

			if (currentResource == null)
			{
				if (resources.Count > 0)
				{
					for (int i = resources.Count - 1; i >= 0; i--)
					{
						SCANresourceGlobal r = resources[i];

						if (r.Name != SCANcontroller.controller.overlayResource)
						{
							continue;
						}

						currentResource = r;
						break;
					}

					if (currentResource == null)
					{
						currentResource = resources[0];
					}

					currentResource.CurrentBodyConfig(body.bodyName);
				}
			}
			else
			{
				currentResource.CurrentBodyConfig(body.bodyName);
			}

			bodyBiome = body.BiomeMap != null;
			bodyPQS = body.pqsController != null;

			if (_overlayOn)
			{
				refreshMap(SCANcontroller.controller.overlaySelection);
			}

			double circum = body.Radius * 2 * Math.PI;
			double eqDistancePerDegree = circum / 360;
			degreeOffset = 5 / eqDistancePerDegree;

			if (_isVisible)
			{
				Close();
				Open();
			}
		}

		private void removeOverlay(bool immediate = false)
		{
			_overlayOn = false;
			overlayBuild++;   // abandon a build in flight

			OverlayGenerator.Instance.ClearDisplay();

			if (mapOverlay != null)
			{
				MonoBehaviour.Destroy(mapOverlay);
			}

			mapOverlay = null;

			if (immediate)
			{
				try
				{
					body.scaledBody.GetComponentInChildren<ScaledSpaceFader>().r.material.SetTexture(Shader.PropertyToID("_ResourceMap"), null);
				}
				catch (Exception e)
				{
					SCANUtil.SCANlog("Error in destroying planetary map overlay:\n{0}", e);
				}
			}
		}

		public void refreshMap(float t, int height, int interp, int biomeHeight)
		{
			if (_overlayOn)
			{
				refreshMap(SCANcontroller.controller.overlaySelection);
			}
		}

		private SCANmap overlayMap;   // composites all three overlays on the GPU (mapSource.Overlay)
		private int overlayBuild;     // generation counter: a newer request or a removal abandons an in-flight build

		private void refreshMap(int i, bool remove = true)
		{
			if (remove)
			{
				removeOverlay();
			}

			if (i < 0 || i > 2)
			{
				return;
			}

			// A build already in flight is abandoned by the generation counter (removeOverlay bumped it,
			// and buildOverlay bumps it again), so a quick switch of overlay type starts the new one at once.
			_overlayOn = true;

			SCANcontroller.controller.StartCoroutine(buildOverlay(i));
		}

		// The overlay is composited by the same shader as the map windows, then read back into the
		// mipmapped Texture2D the planet has always been handed, through the same stock setter: nothing
		// on the material side changes. selection: 0 biome, 1 terrain, 2 resource.
		private IEnumerator buildOverlay(int selection)
		{
			int build = ++overlayBuild;
			int timer = 0;

			// Terrain draws from the body's 360x180 height map; pump its build from here if needed, as before.
			if (selection == 1)
			{
				if (data.Body.pqsController == null)
				{
					yield break;
				}

				while (!data.Built && timer < 2000 && build == overlayBuild)
				{
					// Someone else owns the build - unless nobody has pumped it for a few frames, in which
					// case that owner is gone and the build is ours to finish, from the row it stopped on.
					bool ownedElsewhere = (data.ControllerBuilding || data.MapBuilding) && !data.BuildStalled;

					if (!ownedElsewhere)
					{
						data.takeOverBuild();
						data.OverlayBuilding = true;
						SCANcontroller.pumpHeightMap(data);
					}

					timer++;
					yield return null;
				}

				if (timer >= 2000 || build != overlayBuild)
				{
					yield break;
				}
			}

			int outWidth, dataWidth;
			mapType mode;

			switch (selection)
			{
				case 0:   // biome: one lookup per output pixel, as the CPU overlay did
					outWidth = SCAN_Settings_Config.Instance.BiomeMapHeight * 2;
					dataWidth = outWidth;
					mode = mapType.Biome;
					break;
				case 1:   // terrain: the 360x180 height map, upsampled by the shader's bilinear fetch (the CPU overlay interpolated it x4)
					outWidth = 1440;
					dataWidth = 360;
					mode = mapType.Altimetry;
					break;
				default:  // resource: the resource layer alone, over clear
					outWidth = SCAN_Settings_Config.Instance.ResourceMapHeight * 2;
					dataWidth = outWidth;
					mode = mapType.Altimetry;
					break;
			}

			int outHeight = outWidth / 2;

			ensureOverlayMap(dataWidth);

			overlayMap.BaseNone = selection == 2;
			overlayMap.OutputAlpha = selection == 1 ? 0.9f : 1f;   // drawTerrainMap faded its colours 10 percent toward clear
			overlayMap.ResGreyBlend = SCAN_Settings_Config.Instance.CoverageTransparency;
			overlayMap.Resource = selection == 2 ? currentResource : null;
			overlayMap.ColorMap = true;
			overlayMap.Terminator = false;
			overlayMap.resetMap(mode, selection == 2 && currentResource != null);

			timer = 0;

			while (!overlayMap.isMapComplete() && timer < 20000)
			{
				if (build != overlayBuild)
				{
					yield break;   // a newer build has taken over
				}

				overlayMap.getPartialMap();
				timer++;
				yield return null;
			}

			if (build != overlayBuild)
			{
				yield break;
			}

			if (timer >= 20000 || !_overlayOn)
			{
				yield break;
			}

			RenderTexture rt = overlayMap.renderAt(outWidth, outHeight);

			if (rt == null)
			{
				Log.Error("Something went wrong when drawing the SCANsat planet overlay: the map did not render");
				yield break;
			}

			if (mapOverlay == null || mapOverlay.width != outWidth || mapOverlay.height != outHeight)
			{
				if (mapOverlay != null)
				{
					UnityEngine.Object.Destroy(mapOverlay);
				}

				mapOverlay = new Texture2D(outWidth, outHeight, TextureFormat.ARGB32, true);
				// A Texture2D defaults to Repeat on both axes, and the scaled-space sphere puts its pole
				// vertices at v = 0 and v = 1 exactly - where the bilinear fetch lands half a texel outside
				// the map and wraps, blending the north polar row with the south polar row over both caps.
				// Latitude has no wrap to honour, so clamp it; longitude does, so the seam keeps Repeat
				// (the same split SCANcontroller gives a body's ScaledSpace textures).
				mapOverlay.wrapModeU = TextureWrapMode.Repeat;
				mapOverlay.wrapModeV = TextureWrapMode.Clamp;
				// The sphere is edge-on at the poles, where the runaway longitude derivative drags an
				// isotropic mip far coarser than the latitude axis needs and rings the cap with bands.
				// Anisotropic sampling takes the mip from the short axis instead.
				mapOverlay.anisoLevel = 9;
			}

			RenderTexture prev = RenderTexture.active;
			RenderTexture.active = rt;
			mapOverlay.ReadPixels(new Rect(0, 0, outWidth, outHeight), 0, 0);
			mapOverlay.Apply(true);
			RenderTexture.active = prev;
			rt.Release();
			UnityEngine.Object.Destroy(rt);

			body.SetResourceMap(mapOverlay);
		}

		private void ensureOverlayMap(int width)
		{
			if (overlayMap != null && overlayMap.Body != body)
			{
				destroyOverlayMap();
			}

			if (overlayMap == null)
			{
				overlayMap = new SCANmap(body, mapSource.Overlay);
				overlayMap.setProjection(MapProjection.Rectangular);
				// No reset here: buildOverlay resets with the real mode straight after this returns. The
				// reset setWidth used to do ran with the previous selection's mType, and the overlay's
				// three widths meant a build reset twice every time the selection changed.
				overlayMap.setWidth(width, false);
				overlayMap.setBody(body);
			}
			else if (overlayMap.MapWidth != width)
			{
				overlayMap.setWidth(width, false);
			}
		}

		private void destroyOverlayMap()
		{
			if (overlayMap == null)
			{
				return;
			}

			overlayMap.Destroy();
			overlayMap = null;
		}
		public void ResetPosition()
		{
			SCAN_Settings_Config.Instance.OverlayPosition = new Vector2(600, -200);

			if (uiElement != null)
			{
				uiElement.SetPosition(SCAN_Settings_Config.Instance.OverlayPosition);
			}
		}

	}
}
