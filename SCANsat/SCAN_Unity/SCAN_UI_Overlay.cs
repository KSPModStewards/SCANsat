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
using System.Threading;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using SCANsat.SCAN_Toolbar;
using SCANsat.Unity.Interfaces;
using SCANsat.Unity.Unity;
using SCANsat.SCAN_Data;
using SCANsat.SCAN_UI.UI_Framework;
using KSP.UI;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANcolorUtil;

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

		private bool mapGenerating;
		private double degreeOffset;
		private int mapStep, mapStart;
		private bool bodyBiome, bodyPQS;

		private int timer;
		//These are read/written on multiple threads; we use volatile to ensure that cached values are not used when reading the value
		private volatile bool threadRunning, threadFinished;
		private volatile bool terrainGenerated;

		private StringBuilder tooltipText = new StringBuilder();
		private bool tooltipActive;

		private Texture2D mapOverlay;
		private Color32[] resourcePixels;
		private Color32[] biomePixels;
		private Color32[] terrainPixels;
		private float[,] abundanceValues;
		private float[,] terrainValues;

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

			tooltipText.Append(coords.ToDMS());

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

			tooltipText = SCANStringBuilderCache.Acquire();

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
			get { return tooltipText.SCANToStringAndRelease(); }
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

			terrainGenerated = false;

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

		private void refreshMap(int i, bool remove = true)
		{
			if (remove)
			{
				removeOverlay();
			}

			if (mapGenerating)
			{
				return;
			}

			if (threadRunning)
			{
				return;
			}

			_overlayOn = true;

			switch (i)
			{
				case 0:
					body.SetResourceMap(SCANuiUtil.drawBiomeMap(ref mapOverlay, ref biomePixels, data, SCAN_Settings_Config.Instance.CoverageTransparency, SCAN_Settings_Config.Instance.BiomeMapHeight));
					break;
				case 1:
					SCANcontroller.controller.StartCoroutine(setTerrainMap());
					break;
				case 2:
					SCANcontroller.controller.StartCoroutine(setOverlayMap());
					break;
				default:
					break;
			}
		}

		private IEnumerator setOverlayMap()
		{
			int timer = 0;

			mapGenerating = true;

			SCANuiUtil.generateOverlayResourceValues(ref abundanceValues, SCAN_Settings_Config.Instance.ResourceMapHeight, data, currentResource, SCAN_Settings_Config.Instance.Interpolation);

			SCANdata copy = new SCANdata(data);
			SCANresourceGlobal resourceCopy = new SCANresourceGlobal(currentResource);
			resourceCopy.CurrentBodyConfig(body.bodyName);

			Thread t = new Thread(() => resourceThreadRun(SCAN_Settings_Config.Instance.ResourceMapHeight, SCAN_Settings_Config.Instance.Interpolation, SCAN_Settings_Config.Instance.CoverageTransparency, new System.Random(ResourceScenario.Instance.gameSettings.Seed), copy, resourceCopy));
			threadRunning = true;
			threadFinished = false;
			t.Start();

			while (threadRunning && timer < 1000)
			{
				timer++;
				yield return null;
			}

			mapGenerating = false;
			copy = null;
			resourceCopy = null;

			if (timer >= 1000)
			{
				Debug.LogError("[SCANsat] Something went wrong when drawing the SCANsat resource map overlay...");
				t.Abort();
				threadRunning = false;
				yield break;
			}

			if (!threadFinished)
			{
				Debug.LogError("[SCANsat] Something went wrong when drawing the SCANsat resource map overlay...");
				yield break;
			}

			if (mapOverlay == null || mapOverlay.height != SCAN_Settings_Config.Instance.ResourceMapHeight)
			{
				mapOverlay = new Texture2D(SCAN_Settings_Config.Instance.ResourceMapHeight * 2, SCAN_Settings_Config.Instance.ResourceMapHeight, TextureFormat.ARGB32, true);
			}

			mapOverlay.SetPixels32(resourcePixels);
			mapOverlay.Apply();

			body.SetResourceMap(mapOverlay);
		}

		private void resourceThreadRun(int height, int step, float transparent, System.Random r, SCANdata copyData, SCANresourceGlobal copyResource)
		{
			try
			{
				SCANuiUtil.generateOverlayResourcePixels(ref resourcePixels, ref abundanceValues, height, copyData, copyResource, r, step, transparent);
				threadFinished = true;
			}
			catch
			{
				threadFinished = false;
			}
			finally
			{
				threadRunning = false;
			}
		}

		private IEnumerator setTerrainMap()
		{
			if (data.Body.pqsController == null)
			{
				yield break;
			}

			int timer = 0;

			while (!data.Built && timer < 2000)
			{
				mapGenerating = true;
				if (!data.ControllerBuilding && !data.MapBuilding)
				{
					if (!data.OverlayBuilding)
					{
						mapStep = 0;
						mapStart = 0;
					}

					data.OverlayBuilding = true;
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

			Thread t = new Thread(() => terrainThreadRun(copy, index));
			threadFinished = false;
			threadRunning = true;
			t.Start();

			while (threadRunning && timer < 1000)
			{
				timer++;
				yield return null;
			}

			mapGenerating = false;
			copy = null;

			if (timer >= 1000)
			{
				Debug.LogError("[SCANsat] Something went wrong when drawing the SCANsat terrain map overlay...");
				t.Abort();
				threadRunning = false;
				yield break;
			}

			if (!threadFinished)
			{
				Debug.LogError("[SCANsat] Something went wrong when drawing the SCANsat terrain map overlay...");
				yield break;
			}

			if (mapOverlay == null)
			{
				mapOverlay = new Texture2D(1440, 720, TextureFormat.ARGB32, true);
			}

			mapOverlay.SetPixels32(terrainPixels);
			mapOverlay.Apply();

			body.SetResourceMap(mapOverlay);
		}

		private void terrainThreadRun(SCANdata copyData, int i)
		{
			try
			{
				if (!terrainGenerated)
				{
					SCANuiUtil.generateTerrainArray(ref terrainValues, 720, 4, copyData, i);
					terrainGenerated = true;
				}

				SCANuiUtil.drawTerrainMap(ref terrainPixels, ref terrainValues, copyData, 720, 4);

				threadFinished = true;
			}
			catch
			{
				threadFinished = false;
			}
			finally
			{
				threadRunning = false;
			}
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
