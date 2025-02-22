#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_BigMap - Script for controlling the big map UI
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
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SCANsat.Unity.Interfaces;

namespace SCANsat.Unity.Unity
{
	public class SCAN_BigMap : CanvasFader, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerDownHandler
	{
		[SerializeField]
		private TextHandler m_Version = null;
		[SerializeField]
		private TextHandler m_Title = null;
		[SerializeField]
		private Transform m_Projection = null;
		[SerializeField]
		private Transform m_MapType = null;
		[SerializeField]
		private Transform m_Resources = null;
		[SerializeField]
		private Transform m_CelestialBody = null;
		[SerializeField]
		private ToggleGroup m_DropDownToggles = null;
		[SerializeField]
		private GameObject m_DropDownPrefab = null;
		[SerializeField]
		private GameObject m_OrbitObject = null;
		[SerializeField]
		private GameObject m_WaypointObject = null;
		[SerializeField]
		private SCAN_Toggle m_ColorToggle = null;
		[SerializeField]
		private SCAN_Toggle m_TerminatorToggle = null;
		[SerializeField]
		private SCAN_Toggle m_GridToggle = null;
		[SerializeField]
		private SCAN_Toggle m_OrbitToggle = null;
		[SerializeField]
		private SCAN_Toggle m_WaypointToggle = null;
		[SerializeField]
		private SCAN_Toggle m_AnomalyToggle = null;
		[SerializeField]
		private SCAN_Toggle m_FlagToggle = null;
		[SerializeField]
		private SCAN_Toggle m_LegendToggle = null;
		[SerializeField]
		private SCAN_Toggle m_ResourcesToggle = null;
		[SerializeField]
		private TextHandler m_ReadoutText = null;
		[SerializeField]
		private RawImage m_MapImage = null;
		[SerializeField]
		private RawImage m_GridMap = null;
		[SerializeField]
		private RawImage m_EQMap = null;
		[SerializeField]
		private LayoutElement m_MapLayout = null;
		[SerializeField]
		private GameObject m_MapLabelPrefab = null;
		[SerializeField]
		private GameObject m_LegendObject = null;
		[SerializeField]
		private RawImage m_LegendImage = null;
		[SerializeField]
		private TextHandler m_LegendLabelOne = null;
		[SerializeField]
		private TextHandler m_LegendLabelTwo = null;
		[SerializeField]
		private TextHandler m_LegendLabelThree = null;
		[SerializeField]
		private GameObject m_WaypointBar = null;
		[SerializeField]
		private GameObject m_MechJebButton = null;
		[SerializeField]
		private InputHandler m_WaypointInput = null;
		[SerializeField]
		private GameObject m_SmallMapButton = null;
		[SerializeField]
		private GameObject m_ZoomMapButton = null;
		[SerializeField]
		private GameObject m_OverlayButton = null;
		[SerializeField]
		private GameObject m_InstrumentsButton = null;
		[SerializeField]
		private GameObject m_SettingsButton = null;
		[SerializeField]
		private GameObject m_NorthSouthMarkers = null;
		[SerializeField]
		private GameObject m_TooltipPrefab = null;
		[SerializeField]
		private GameObject m_ResourceBar = null;
		[SerializeField]
		private RawImage m_ResourceLegendImage = null;
		[SerializeField]
		private TextHandler m_ResourceLegendLabelOne = null;
		[SerializeField]
		private TextHandler m_ResourceLegendLabelTwo = null;
		[SerializeField]
		private Slider m_LoSlider = null;
		[SerializeField]
		private Slider m_HiSlider = null;
		[SerializeField]
		private Slider m_MultiSlider = null;
		[SerializeField]
		private Slider m_LoVisSlider = null;
		[SerializeField]
		private Slider m_HiVisSlider = null;
		[SerializeField]
		private Slider m_AnomalySlider = null;
		[SerializeField]
		private Slider m_LoResSlider = null;
		[SerializeField]
		private Slider m_HiResSlider = null;
		[SerializeField]
		private TextHandler m_LoText = null;
		[SerializeField]
		private TextHandler m_HiText = null;
		[SerializeField]
		private TextHandler m_MultiText = null;
		[SerializeField]
		private TextHandler m_LoVisText = null;
		[SerializeField]
		private TextHandler m_HiVisText = null;
		[SerializeField]
		private TextHandler m_AnomalyText = null;
		[SerializeField]
		private TextHandler m_LoResText = null;
		[SerializeField]
		private TextHandler m_HiResText = null;
		[SerializeField]
		private Color m_StatusCoverage = new Color(0.35f, 0.7f, 0.9f);
		[SerializeField]
		private Color m_StatusGrey = new Color(1f, 1f, 1f);
		[SerializeField]
		private Color m_StatusOrange = new Color(0.9f, 0.6f, 0);
		[SerializeField]
		private Color m_StatusGreen = new Color(0, 0.6f, 0.5f);

		private ISCAN_BigMap bigInterface;
		private bool loaded;
		private RectTransform rect;
		private Vector2 mouseStart;
		private Vector3 windowStart;
		private bool inMap;
		private Vector2 rectPos = new Vector2();

		private bool waypointSelecting;
		private string waypoint;

		private float lastStatusTimer;
		private const float StatusTimerValue = 1;

		private List<SCAN_SimpleLabel> orbitLabels = new List<SCAN_SimpleLabel>();
		private List<SCAN_MapLabel> orbitIconLabels = new List<SCAN_MapLabel>();
		private List<SCAN_MapLabel> anomalyLabels = new List<SCAN_MapLabel>();
		private List<SCAN_MapLabel> waypointLabels = new List<SCAN_MapLabel>();
		private List<SCAN_MapLabel> flagLabels = new List<SCAN_MapLabel>();
		private SCAN_MapLabel vesselLabel;
		private SCAN_MapLabel tempWaypointLabel;
		private SCAN_MapLabel hoverWaypointLabel;

		private SCAN_DropDown dropDown;

		private bool tooltipOn;
		private SCAN_Tooltip _tooltip;

		private bool statusTooltipOn;
		private byte statusTooltipType;
		private SCAN_Tooltip _statusTooltip;

		private const float MAXMAPWIDTH = 8200;

		protected override void Awake()
		{
			base.Awake();

			rect = GetComponent<RectTransform>();

			Alpha(0);
		}

		private void Update()
		{
			if (bigInterface == null || !bigInterface.IsVisible)
			{
				return;
			}

			if (bigInterface.LockInput)
			{
				if (m_WaypointInput != null && !m_WaypointInput.IsFocused)
				{
					bigInterface.LockInput = false;
				}
			}

			bigInterface.Update();

			if (vesselLabel != null)
			{
				vesselLabel.UpdatePosition(bigInterface.VesselPosition());
			}

			if (inMap && m_MapImage != null && m_ReadoutText != null)
			{
				RectTransformUtility.ScreenPointToLocalPointInRectangle(m_MapImage.rectTransform, Input.mousePosition, bigInterface.MainCanvas.worldCamera, out rectPos);

				m_ReadoutText.OnTextUpdate.Invoke(bigInterface.MapInfo(rectPos));

				SetStatusText(bigInterface.ScanStatus, false);

				if (waypointSelecting)
				{
					if (hoverWaypointLabel != null)
					{
						Vector2 mapPos = new Vector2(rectPos.x, rectPos.y + bigInterface.Size.y);

						hoverWaypointLabel.UpdateActive(true);

						hoverWaypointLabel.UpdatePosition(mapPos);
					}
				}
			}
			else if (waypointSelecting)
			{
				if (hoverWaypointLabel != null)
				{
					hoverWaypointLabel.UpdateActive(false);
				}

				SetStatusText(0, true);
			}
			else
			{
				SetStatusText(0, true);
			}

			if (tooltipOn)
			{
				RectTransformUtility.ScreenPointToLocalPointInRectangle(m_LegendImage.rectTransform, Input.mousePosition, bigInterface.MainCanvas.worldCamera, out rectPos);

				float halfWidth = m_LegendImage.rectTransform.rect.width / 2;

				float legendXPos = (rectPos.x + halfWidth) / m_LegendImage.rectTransform.rect.width;

				if (_tooltip != null)
				{
					_tooltip.UpdateText(bigInterface.TooltipText(legendXPos));
				}
			}

			if (statusTooltipOn)
			{
				if (_statusTooltip != null)
				{
					string status = "0%";

					switch (statusTooltipType)
					{
						case 0:
							status = (bigInterface.LoAltScan / 100).ToString("P1");
							break;
						case 1:
							status = (bigInterface.HiAltScan / 100).ToString("P1");
							break;
						case 2:
							status = (bigInterface.MultiScan / 100).ToString("P1");
							break;
						case 3:
							status = (bigInterface.LoVisScan / 100).ToString("P1");
							break;
						case 4:
							status = (bigInterface.HiVisScan / 100).ToString("P1");
							break;
						case 5:
							status = (bigInterface.AnomalyScan / 100).ToString("P1");
							break;
						case 6:
							status = (bigInterface.LoResScan / 100).ToString("P1");
							break;
						case 7:
							status = (bigInterface.HiResScan / 100).ToString("P1");
							break;
					}

					_statusTooltip.UpdateText(status);
				}
			}

			if (bigInterface.OrbitToggle && bigInterface.ShowOrbit)
			{
				for (int i = orbitLabels.Count - 1; i >= 0; i--)
				{
					SCAN_SimpleLabel label = orbitLabels[i];

					label.UpdateIcon(bigInterface.OrbitInfo(i));
				}

				for (int i = orbitIconLabels.Count - 1; i >= 0; i--)
				{
					SCAN_MapLabel label = orbitIconLabels[i];

					label.UpdatePositionActivation(bigInterface.OrbitIconInfo(label.StringID));
				}
			}

			if (Time.time > lastStatusTimer + StatusTimerValue)
			{
				lastStatusTimer = Time.time;

				SetStatusSliders();
			}
		}

		private void SetStatusText(byte status, bool grey)
		{
			if (m_LoText != null)
			{
				m_LoText.OnColorUpdate.Invoke(grey ? m_StatusGrey : (status & 1 << 0) != 0 ? m_StatusGreen : m_StatusOrange);
			}

			if (m_HiText != null)
			{
				m_HiText.OnColorUpdate.Invoke(grey ? m_StatusGrey : (status & 1 << 1) != 0 ? m_StatusGreen : m_StatusOrange);
			}

			if (m_MultiText != null)
			{
				m_MultiText.OnColorUpdate.Invoke(grey ? m_StatusGrey : (status & 1 << 2) != 0 ? m_StatusGreen : m_StatusOrange);
			}

			if (m_LoVisText != null)
			{
				m_LoVisText.OnColorUpdate.Invoke(grey ? m_StatusGrey : (status & 1 << 3) != 0 ? m_StatusGreen : m_StatusOrange);
			}

			if (m_HiVisText != null)
			{
				m_HiVisText.OnColorUpdate.Invoke(grey ? m_StatusGrey : (status & 1 << 4) != 0 ? m_StatusGreen : m_StatusOrange);
			}

			if (m_AnomalyText != null)
			{
				m_AnomalyText.OnColorUpdate.Invoke(grey ? m_StatusGrey : (status & 1 << 5) != 0 ? m_StatusGreen : m_StatusOrange);
			}

			if (m_LoResText != null)
			{
				m_LoResText.OnColorUpdate.Invoke(grey ? m_StatusGrey : (status & 1 << 6) != 0 ? m_StatusGreen : m_StatusOrange);
			}

			if (m_HiResText != null)
			{
				m_HiResText.OnColorUpdate.Invoke(grey ? m_StatusGrey : (status & 1 << 7) != 0 ? m_StatusGreen : m_StatusOrange);
			}
		}

		private void SetStatusSliders()
		{
			if (m_LoSlider != null)
			{
				m_LoSlider.value = bigInterface.LoAltScan;
			}

			if (m_HiSlider != null)
			{
				m_HiSlider.value = bigInterface.HiAltScan;
			}

			if (m_MultiSlider != null)
			{
				m_MultiSlider.value = bigInterface.MultiScan;
			}

			if (m_LoVisSlider != null)
			{
				m_LoVisSlider.value = bigInterface.LoVisScan;
			}

			if (m_HiVisSlider != null)
			{
				m_HiVisSlider.value = bigInterface.HiVisScan;
			}

			if (m_AnomalySlider != null)
			{
				m_AnomalySlider.value = bigInterface.AnomalyScan;
			}

			if (m_LoResSlider != null)
			{
				m_LoResSlider.value = bigInterface.LoResScan;
			}

			if (m_HiResSlider != null)
			{
				m_HiResSlider.value = bigInterface.HiResScan;
			}
		}

		private Vector2 ScreenPosition(RectTransform r, Canvas canvas)
		{
			Vector3[] corners = new Vector3[4];
			Vector2 pos = new Vector2();

			r.GetWorldCorners(corners);

			if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
			{
				pos = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
			}
			else
			{
				pos = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[0]);
			}

			pos.y = Screen.height - pos.y - r.sizeDelta.y;

			return pos;
		}

		public void setMap(ISCAN_BigMap map)
		{
			if (map == null)
			{
				return;
			}

			bigInterface = map;

			if (m_Version != null)
			{
				m_Version.OnTextUpdate.Invoke(map.Version);
			}

			if (m_ColorToggle != null)
			{
				m_ColorToggle.isOn = map.ColorToggle;
			}

			if (m_TerminatorToggle != null)
			{
				m_TerminatorToggle.isOn = map.TerminatorToggle;
			}

			if (m_GridToggle != null)
			{
				m_GridToggle.isOn = map.GridToggle;
			}

			if (m_OrbitToggle != null)
			{
				m_OrbitToggle.isOn = map.OrbitToggle;
			}

			if (m_WaypointToggle != null)
			{
				m_WaypointToggle.isOn = map.WaypointToggle;
			}

			if (m_AnomalyToggle != null)
			{
				m_AnomalyToggle.isOn = map.AnomalyToggle;
			}

			if (m_FlagToggle != null)
			{
				m_FlagToggle.isOn = map.FlagToggle;
			}

			if (m_LegendToggle != null)
			{
				m_LegendToggle.isOn = map.LegendToggle;
			}

			if (m_ResourcesToggle != null)
			{
				m_ResourcesToggle.isOn = map.ResourceToggle;
			}

			if (m_ResourceBar != null)
			{
				m_ResourceBar.SetActive(map.ResourceToggle);
			}

			if (!map.OrbitAvailable && m_OrbitObject != null)
			{
				m_OrbitObject.SetActive(false);
			}

			if (!map.ShowWaypoint && m_WaypointObject != null)
			{
				m_WaypointObject.SetActive(false);
			}

			if (!map.ShowResource && m_Resources != null)
			{
				m_Resources.gameObject.SetActive(false);
			}

			SetLegend(map.LegendToggle);

			if (map.ResourceToggle)
			{
				SetResourceLegend();
			}

			SetButtons(map.CurrentScene);

			SetScale(map.Scale);

			SetPosition(map.Position);

			SetSize(map.Size);

			SetIcons();

			SetNorthSouth();

			ProcessTooltips();

			FadeIn();

			loaded = true;
		}

		public void FadeIn()
		{
			Fade(1, true);
		}

		public void FadeOut()
		{
			Fade(0, false, Kill, false);
		}

		private void Kill()
		{
			gameObject.SetActive(false);
			Destroy(gameObject);
		}

		public void Close()
		{
			if (bigInterface != null)
			{
				bigInterface.IsVisible = false;
			}
		}

		public void ProcessTooltips()
		{
			if (bigInterface == null)
			{
				return;
			}

			TooltipHandler[] handlers = gameObject.GetComponentsInChildren<TooltipHandler>(true);

			if (handlers == null)
			{
				return;
			}

			for (int j = 0; j < handlers.Length; j++)
			{
				ProcessTooltip(handlers[j], bigInterface.TooltipsOn, bigInterface.TooltipCanvas, bigInterface.Scale);
			}
		}

		private void ProcessTooltip(TooltipHandler handler, bool isOn, Canvas c, float scale)
		{
			if (handler == null)
			{
				return;
			}

			handler.IsActive = isOn && !handler.HelpTip;
			handler._Canvas = c;
			handler.Scale = scale;
		}

		private void SetNorthSouth()
		{
			if (bigInterface == null || m_NorthSouthMarkers == null)
			{
				return;
			}

			if (bigInterface.CurrentProjection == "Polar")
			{
				m_NorthSouthMarkers.SetActive(true);
			}
			else
			{
				m_NorthSouthMarkers.SetActive(false);
			}
		}

		private void SetButtons(int i)
		{
			switch (i)
			{
				case -1:
					if (m_SmallMapButton != null)
					{
						m_SmallMapButton.SetActive(false);
					}

					if (m_ZoomMapButton != null)
					{
						m_ZoomMapButton.SetActive(false);
					}

					if (m_OverlayButton != null)
					{
						m_OverlayButton.SetActive(false);
					}

					if (m_InstrumentsButton != null)
					{
						m_InstrumentsButton.SetActive(false);
					}

					if (m_SettingsButton != null)
					{
						m_SettingsButton.SetActive(false);
					}

					break;
				case 1:
					if (m_SmallMapButton != null)
					{
						m_SmallMapButton.SetActive(false);
					}

					if (m_InstrumentsButton != null)
					{
						m_InstrumentsButton.SetActive(false);
					}

					if (m_ZoomMapButton != null)
					{
						m_ZoomMapButton.SetActive(false);
					}

					break;
				case 2:
					if (m_SmallMapButton != null)
					{
						m_SmallMapButton.SetActive(false);
					}

					if (m_OverlayButton != null)
					{
						m_OverlayButton.SetActive(false);
					}

					if (m_InstrumentsButton != null)
					{
						m_InstrumentsButton.SetActive(false);
					}

					if (m_ZoomMapButton != null)
					{
						m_ZoomMapButton.SetActive(false);
					}

					break;
			}
		}

		public void SetScale(float scale)
		{
			rect.localScale = Vector3.one * scale;
		}

		public void SetPosition(Vector2 pos)
		{
			if (rect == null)
			{
				return;
			}

			rect.anchoredPosition = new Vector3(pos.x, pos.y, 0);
		}

		public void SetSize(Vector2 size)
		{
			if (m_MapLayout == null)
			{
				return;
			}

			if (size.x + 8 < m_MapLayout.minWidth)
			{
				size.x = m_MapLayout.minWidth - 8;
			}
			else if (size.x > 8192)
			{
				size.x = 8192;
			}

			if (size.x % 2 != 0)
			{
				size.x += 1;
			}

			m_MapLayout.preferredWidth = size.x + 8;
			m_MapLayout.preferredHeight = m_MapLayout.preferredWidth / 2 + 8;
		}

		public void SetLegends(bool isOn)
		{
			SetLegend(isOn);

			if (bigInterface.ResourceToggle)
			{
				SetResourceLegend();
			}
		}

		private void SetLegend(bool isOn)
		{
			if (m_LegendObject == null)
			{
				return;
			}

			if (bigInterface.LegendAvailable)
			{
				m_LegendObject.SetActive(isOn);
			}
			else
			{
				m_LegendObject.SetActive(false);
				return;
			}

			if (!isOn)
			{
				return;
			}

			if (m_LegendImage != null)
			{
				m_LegendImage.texture = bigInterface.LegendImage;
			}

			if (bigInterface.CurrentMapType == "Biome")
			{
				if (m_LegendLabelOne != null)
				{
					m_LegendLabelOne.gameObject.SetActive(false);
				}

				if (m_LegendLabelTwo != null)
				{
					m_LegendLabelTwo.gameObject.SetActive(false);
				}

				if (m_LegendLabelThree != null)
				{
					m_LegendLabelThree.gameObject.SetActive(false);
				}
			}
			else
			{
				IList<string> labels = bigInterface.LegendLabels;

				if (labels == null || labels.Count != 3)
				{
					return;
				}

				if (m_LegendLabelOne != null)
				{
					m_LegendLabelOne.gameObject.SetActive(true);
					m_LegendLabelOne.OnTextUpdate.Invoke(labels[0]);
				}

				if (m_LegendLabelTwo != null)
				{
					m_LegendLabelTwo.gameObject.SetActive(true);
					m_LegendLabelTwo.OnTextUpdate.Invoke(labels[1]);
				}

				if (m_LegendLabelThree != null)
				{
					m_LegendLabelThree.gameObject.SetActive(true);
					m_LegendLabelThree.OnTextUpdate.Invoke(labels[2]);
				}
			}
		}

		public void SetResourceLegend()
		{
			if (m_ResourceLegendImage != null)
			{
				m_ResourceLegendImage.texture = bigInterface.ResourceLegendImage;
			}

			Vector2 res = bigInterface.ResourceLegendLabels;

			if (m_ResourceLegendLabelOne != null)
			{
				m_ResourceLegendLabelOne.OnTextUpdate.Invoke(res.x.ToString(res.x < 0.1 ? "P1" : "P0"));
			}

			if (m_ResourceLegendLabelTwo != null)
			{
				m_ResourceLegendLabelTwo.OnTextUpdate.Invoke(res.y.ToString(res.y < 0.1 ? "P1" : "P0"));
			}
		}

		private void SetFlagIcons(IList<MapLabelInfo> flags)
		{
			if (flags == null)
			{
				return;
			}

			if (bigInterface == null || m_MapLabelPrefab == null || m_MapImage == null)
			{
				return;
			}

			for (int i = 0; i < flags.Count; i++)
			{
				MapLabelInfo info = flags[i];

				createFlag(info);
			}
		}

		private void createFlag(MapLabelInfo info)
		{
			SCAN_MapLabel mapLabel = Instantiate(m_MapLabelPrefab).GetComponent<SCAN_MapLabel>();

			if (mapLabel == null)
			{
				return;
			}

			mapLabel.transform.SetParent(m_MapImage.transform, false);

			mapLabel.Setup(info);

			flagLabels.Add(mapLabel);
		}

		private void SetAnomalyIcons(Dictionary<string, MapLabelInfo> anomalies)
		{
			if (anomalies == null)
			{
				return;
			}

			if (bigInterface == null || m_MapLabelPrefab == null || m_MapImage == null)
			{
				return;
			}

			for (int i = 0; i < anomalies.Count; i++)
			{
				string id = anomalies.ElementAt(i).Key;

				MapLabelInfo info;

				if (!anomalies.TryGetValue(id, out info))
				{
					continue;
				}

				createAnomaly(id, info);
			}
		}

		private void createAnomaly(string id, MapLabelInfo info)
		{
			SCAN_MapLabel mapLabel = Instantiate(m_MapLabelPrefab).GetComponent<SCAN_MapLabel>();

			if (mapLabel == null)
			{
				return;
			}

			mapLabel.transform.SetParent(m_MapImage.transform, false);

			mapLabel.Setup(id, info);

			anomalyLabels.Add(mapLabel);
		}

		private void SetWaypointIcons(Dictionary<int, MapLabelInfo> waypoints)
		{
			if (waypoints == null)
			{
				return;
			}

			if (bigInterface == null || m_MapLabelPrefab == null || m_MapImage == null)
			{
				return;
			}

			for (int i = 0; i < waypoints.Count; i++)
			{
				int id = waypoints.ElementAt(i).Key;

				MapLabelInfo info;

				if (!waypoints.TryGetValue(id, out info))
				{
					continue;
				}

				createWaypoint(id, info);
			}
		}

		private SCAN_MapLabel createWaypoint(int id, MapLabelInfo info, bool temp = false)
		{
			SCAN_MapLabel mapLabel = Instantiate(m_MapLabelPrefab).GetComponent<SCAN_MapLabel>();

			if (mapLabel == null)
			{
				return null;
			}

			mapLabel.transform.SetParent(m_MapImage.transform, false);

			mapLabel.Setup(id, info);

			if (!temp)
			{
				waypointLabels.Add(mapLabel);
			}

			return mapLabel;
		}

		private void SetVesselIcon(KeyValuePair<Guid, MapLabelInfo> vessel)
		{
			if (vessel.Value.label == "null")
			{
				return;
			}

			if (bigInterface == null || m_MapLabelPrefab == null || m_MapImage == null)
			{
				return;
			}

			SCAN_MapLabel mapLabel = Instantiate(m_MapLabelPrefab).GetComponent<SCAN_MapLabel>();

			if (mapLabel == null)
			{
				return;
			}

			mapLabel.transform.SetParent(m_MapImage.transform, false);

			mapLabel.Setup(vessel.Key, vessel.Value);

			vesselLabel = mapLabel;
		}

		private void SetOrbitIcons(int count)
		{
			if (bigInterface == null || m_MapImage == null)
			{
				return;
			}

			for (int i = 0; i < count; i++)
			{
				SimpleLabelInfo info = bigInterface.OrbitInfo(i);

				CreateOrbitIcon(info);
			}
		}

		private void CreateOrbitIcon(SimpleLabelInfo info)
		{
			GameObject labelObj = new GameObject("SCAN_SimpleLabel");

			SCAN_SimpleLabel label = labelObj.AddComponent<SCAN_SimpleLabel>();

			if (label == null)
			{
				return;
			}

			label.transform.SetParent(m_MapImage.transform, false);

			label.Setup(info);

			orbitLabels.Add(label);
		}

		private void SetOrbitMapIcons(Dictionary<string, MapLabelInfo> orbitLabels)
		{
			if (orbitLabels == null)
			{
				return;
			}

			if (bigInterface == null || m_MapLabelPrefab == null || m_MapImage == null)
			{
				return;
			}

			for (int i = 0; i < orbitLabels.Count; i++)
			{
				string id = orbitLabels.ElementAt(i).Key;

				MapLabelInfo info;

				if (!orbitLabels.TryGetValue(id, out info))
				{
					continue;
				}

				CreateOrbitMapIcon(id, info);
			}
		}

		private void CreateOrbitMapIcon(string id, MapLabelInfo info)
		{
			SCAN_MapLabel mapLabel = Instantiate(m_MapLabelPrefab).GetComponent<SCAN_MapLabel>();

			if (mapLabel == null)
			{
				return;
			}

			mapLabel.transform.SetParent(m_MapImage.transform, false);

			mapLabel.Setup(id, info);

			orbitIconLabels.Add(mapLabel);
		}

		private void ClearIcons()
		{
			for (int i = waypointLabels.Count - 1; i >= 0; i--)
			{
				SCAN_MapLabel m = waypointLabels[i];

				m.gameObject.SetActive(false);
				Destroy(m.gameObject);
			}

			for (int i = anomalyLabels.Count - 1; i >= 0; i--)
			{
				SCAN_MapLabel m = anomalyLabels[i];

				m.gameObject.SetActive(false);
				Destroy(m.gameObject);
			}

			for (int i = flagLabels.Count - 1; i >= 0; i--)
			{
				SCAN_MapLabel m = flagLabels[i];

				m.gameObject.SetActive(false);
				Destroy(m.gameObject);
			}

			for (int i = orbitLabels.Count - 1; i >= 0; i--)
			{
				SCAN_SimpleLabel s = orbitLabels[i];

				s.gameObject.SetActive(false);
				Destroy(s.gameObject);
			}

			for (int i = orbitIconLabels.Count - 1; i >= 0; i--)
			{
				SCAN_MapLabel m = orbitIconLabels[i];

				m.gameObject.SetActive(false);
				Destroy(m.gameObject);
			}

			if (vesselLabel != null)
			{
				vesselLabel.gameObject.SetActive(false);
				Destroy(vesselLabel.gameObject);
			}

			DestroyWaypoint(tempWaypointLabel);

			flagLabels.Clear();
			anomalyLabels.Clear();
			waypointLabels.Clear();
			orbitLabels.Clear();
			orbitIconLabels.Clear();
			vesselLabel = null;
		}

		public void RefreshIcons()
		{
			ClearIcons();

			SetIcons();
		}

		private void SetIcons()
		{
			if (bigInterface == null)
			{
				return;
			}

			if (bigInterface.FlagToggle)
			{
				SetFlagIcons(bigInterface.FlagInfoList);
			}

			if (bigInterface.AnomalyToggle)
			{
				SetAnomalyIcons(bigInterface.AnomalyInfoList);
			}

			if (bigInterface.WaypointToggle && bigInterface.ShowWaypoint)
			{
				SetWaypointIcons(bigInterface.WaypointInfoList);
			}

			if (bigInterface.OrbitToggle && bigInterface.ShowOrbit)
			{
				SetOrbitIcons(bigInterface.OrbitSteps);
				SetOrbitMapIcons(bigInterface.OrbitLabelList);
			}

			SetVesselIcon(bigInterface.VesselInfo);
		}

		public void OnEnterLoStatus(BaseEventData eventData)
		{
			if (_statusTooltip != null)
			{
				CloseStatusTooltip();
			}

			statusTooltipOn = true;
			statusTooltipType = 0;
			OpenStatusTooltip();
		}

		public void OnExitLoStatus(BaseEventData eventData)
		{
			statusTooltipOn = false;
			CloseStatusTooltip();
		}

		public void OnEnterHiStatus(BaseEventData eventData)
		{
			if (_statusTooltip != null)
			{
				CloseStatusTooltip();
			}

			statusTooltipOn = true;
			statusTooltipType = 1;
			OpenStatusTooltip();
		}

		public void OnExitHiStatus(BaseEventData eventData)
		{
			statusTooltipOn = false;
			CloseStatusTooltip();
		}

		public void OnEnterMultiStatus(BaseEventData eventData)
		{
			if (_statusTooltip != null)
			{
				CloseStatusTooltip();
			}

			statusTooltipOn = true;
			statusTooltipType = 2;
			OpenStatusTooltip();
		}

		public void OnExitMultiStatus(BaseEventData eventData)
		{
			statusTooltipOn = false;
			CloseStatusTooltip();
		}

		public void OnEnterLoVisStatus(BaseEventData eventData)
		{
			if (_statusTooltip != null)
			{
				CloseStatusTooltip();
			}

			statusTooltipOn = true;
			statusTooltipType = 3;
			OpenStatusTooltip();
		}

		public void OnExitLoVisStatus(BaseEventData eventData)
		{
			statusTooltipOn = false;
			CloseStatusTooltip();
		}

		public void OnEnterHiVisStatus(BaseEventData eventData)
		{
			if (_statusTooltip != null)
			{
				CloseStatusTooltip();
			}

			statusTooltipOn = true;
			statusTooltipType = 4;
			OpenStatusTooltip();
		}

		public void OnExitHiVisStatus(BaseEventData eventData)
		{
			statusTooltipOn = false;
			CloseStatusTooltip();
		}

		public void OnEnterAnomalyStatus(BaseEventData eventData)
		{
			if (_statusTooltip != null)
			{
				CloseStatusTooltip();
			}

			statusTooltipOn = true;
			statusTooltipType = 5;
			OpenStatusTooltip();
		}

		public void OnExitAnomalyStatus(BaseEventData eventData)
		{
			statusTooltipOn = false;
			CloseStatusTooltip();
		}

		public void OnEnterLoResStatus(BaseEventData eventData)
		{
			if (_statusTooltip != null)
			{
				CloseStatusTooltip();
			}

			statusTooltipOn = true;
			statusTooltipType = 6;
			OpenStatusTooltip();
		}

		public void OnExitLoResStatus(BaseEventData eventData)
		{
			statusTooltipOn = false;
			CloseStatusTooltip();
		}

		public void OnEnterHiResStatus(BaseEventData eventData)
		{
			if (_statusTooltip != null)
			{
				CloseStatusTooltip();
			}

			statusTooltipOn = true;
			statusTooltipType = 7;
			OpenStatusTooltip();
		}

		public void OnExitHiResStatus(BaseEventData eventData)
		{
			statusTooltipOn = false;
			CloseStatusTooltip();
		}

		private void OpenStatusTooltip()
		{
			if (m_TooltipPrefab == null || bigInterface.TooltipCanvas == null)
			{
				return;
			}

			_statusTooltip = Instantiate(m_TooltipPrefab).GetComponent<SCAN_Tooltip>();

			if (_statusTooltip == null)
			{
				return;
			}

			_statusTooltip.transform.SetParent(bigInterface.TooltipCanvas.transform, false);
			_statusTooltip.transform.SetAsLastSibling();

			_statusTooltip.Setup(bigInterface.TooltipCanvas, "_", bigInterface.Scale);
		}

		private void CloseStatusTooltip()
		{
			if (_statusTooltip == null)
			{
				return;
			}

			_statusTooltip.gameObject.SetActive(false);
			Destroy(_statusTooltip.gameObject);
			_statusTooltip = null;
		}

		public void OnEnterLegend(BaseEventData eventData)
		{
			if (bigInterface == null || !bigInterface.LegendToggle || !bigInterface.LegendTooltips)
			{
				return;
			}

			if (_tooltip != null)
			{
				CloseTooltip();
			}

			tooltipOn = true;
			OpenTooltip();
		}

		public void OnExitLegend(BaseEventData eventData)
		{
			if (bigInterface == null || !bigInterface.LegendToggle || !bigInterface.LegendTooltips)
			{
				return;
			}

			tooltipOn = false;
			CloseTooltip();
		}

		private void OpenTooltip()
		{
			if (m_TooltipPrefab == null || bigInterface.TooltipCanvas == null)
			{
				return;
			}

			_tooltip = Instantiate(m_TooltipPrefab).GetComponent<SCAN_Tooltip>();

			if (_tooltip == null)
			{
				return;
			}

			_tooltip.transform.SetParent(bigInterface.TooltipCanvas.transform, false);
			_tooltip.transform.SetAsLastSibling();

			_tooltip.Setup(bigInterface.TooltipCanvas, "_", bigInterface.Scale);
		}

		private void CloseTooltip()
		{
			if (_tooltip == null)
			{
				return;
			}

			_tooltip.gameObject.SetActive(false);
			Destroy(_tooltip.gameObject);
			_tooltip = null;
		}

		public void OnEnterMap(BaseEventData eventData)
		{
			inMap = true;
		}

		public void OnExitMap(BaseEventData eventData)
		{
			inMap = false;

			if (m_ReadoutText != null)
			{
				m_ReadoutText.OnTextUpdate.Invoke("");
			}
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			if (rect == null)
			{
				return;
			}

			mouseStart = eventData.position;
			windowStart = rect.position;
		}

		public void OnDrag(PointerEventData eventData)
		{
			if (rect == null)
			{
				return;
			}

			rect.position = windowStart + (Vector3)(eventData.position - mouseStart);
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			if (rect == null || bigInterface == null)
			{
				return;
			}

			bigInterface.Position = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y);
		}

		public void OnStartResize(BaseEventData eventData)
		{
			if (!(eventData is PointerEventData))
			{
				return;
			}

			ClearIcons();
		}

		public void OnResize(BaseEventData eventData)
		{
			if (m_MapLayout == null)
			{
				return;
			}

			if (!(eventData is PointerEventData))
			{
				return;
			}

			m_MapLayout.preferredWidth += ((PointerEventData)eventData).delta.x;

			if (m_MapLayout.preferredWidth < m_MapLayout.minWidth)
			{
				m_MapLayout.preferredWidth = m_MapLayout.minWidth;
			}
			else if (m_MapLayout.preferredWidth > MAXMAPWIDTH)
			{
				m_MapLayout.preferredWidth = MAXMAPWIDTH;
			}

			if (m_MapLayout.preferredWidth % 2 != 0)
			{
				m_MapLayout.preferredWidth += 1;
			}

			m_MapLayout.preferredHeight = m_MapLayout.preferredWidth / 2;
		}

		public void OnEndResize(BaseEventData eventData)
		{
			if (m_MapLayout == null || bigInterface == null)
			{
				return;
			}

			bigInterface.Size = new Vector2(m_MapLayout.preferredWidth - 8, m_MapLayout.preferredHeight - 8);

			SetIcons();
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			transform.SetAsLastSibling();

			if (dropDown == null)
			{
				return;
			}

			RectTransform r = dropDown.GetComponent<RectTransform>();

			if (r == null)
			{
				return;
			}

			if (RectTransformUtility.RectangleContainsScreenPoint(r, eventData.position, eventData.pressEventCamera))
			{
				return;
			}

			dropDown.FadeOut();
			dropDown = null;

			if (m_DropDownToggles != null)
			{
				m_DropDownToggles.SetAllTogglesOff();
			}
		}

		public void OnClickMap(BaseEventData eventData)
		{
			if (!inMap || bigInterface == null || m_MapImage == null || !(eventData is PointerEventData))
			{
				return;
			}

			OnPointerDown((PointerEventData)eventData);

			Vector2 pos;

			RectTransformUtility.ScreenPointToLocalPointInRectangle(m_MapImage.rectTransform, Input.mousePosition, bigInterface.MainCanvas.worldCamera, out pos);

			if (waypointSelecting)
			{
				DestroyWaypoint(tempWaypointLabel);

				SetWaypoint(pos);
			}
			else if (((PointerEventData)eventData).button == PointerEventData.InputButton.Right)
			{
				bigInterface.ClickMap(pos);
			}
		}

		private void SetWaypoint(Vector2 p)
		{
			Vector2 mapPos = new Vector2(p.x, p.y + bigInterface.Size.y);

			MapLabelInfo info = new MapLabelInfo()
			{
				label = "",
				image = bigInterface.WaypointSprite,
				pos = mapPos,
				baseColor = Color.white,
				flash = false,
				width = 20,
				alignBottom = 10,
				show = true
			};

			tempWaypointLabel = createWaypoint(0, info, true);
		}

		public void UpdateTitle(string text)
		{
			if (m_Title == null)
			{
				return;
			}

			m_Title.OnTextUpdate.Invoke(text);
		}

		public void UpdateMapTexture(Texture2D map)
		{
			if (m_MapImage == null)
			{
				return;
			}

			m_MapImage.texture = map;
		}

		public void UpdateGridTexture(Texture2D grid)
		{
			if (m_GridMap == null)
			{
				return;
			}

			m_GridMap.texture = grid;
		}

		public void UpdateEQMapTexture(Texture2D eq)
		{
			if (m_EQMap != null)
			{
				m_EQMap.texture = eq;
			}
		}

		public void ToggleProjectionSelection(bool isOn)
		{
			if (dropDown != null)
			{
				dropDown.FadeOut(true);
				dropDown = null;
			}

			if (!isOn)
			{
				return;
			}

			if (bigInterface == null || m_DropDownPrefab == null || m_Projection == null)
			{
				return;
			}

			dropDown = Instantiate(m_DropDownPrefab).GetComponent<SCAN_DropDown>();

			if (dropDown == null)
			{
				return;
			}

			dropDown.transform.SetParent(m_Projection, false);

			dropDown.Setup(bigInterface.Projections, bigInterface.CurrentProjection);

			dropDown.OnSelectUpdate.AddListener(new UnityEngine.Events.UnityAction<string>(SetProjection));
		}

		private void SetProjection(string selection)
		{
			if (bigInterface == null)
			{
				return;
			}

			bigInterface.CurrentProjection = selection;

			dropDown.FadeOut();
			dropDown = null;

			if (m_DropDownToggles != null)
			{
				m_DropDownToggles.SetAllTogglesOff();
			}

			RefreshIcons();

			SetNorthSouth();
		}

		public void ToggleTypeSelection(bool isOn)
		{
			if (dropDown != null)
			{
				dropDown.FadeOut(true);
				dropDown = null;
			}

			if (!isOn)
			{
				return;
			}

			if (bigInterface == null || m_DropDownPrefab == null || m_MapType == null)
			{
				return;
			}

			dropDown = Instantiate(m_DropDownPrefab).GetComponent<SCAN_DropDown>();

			if (dropDown == null)
			{
				return;
			}

			dropDown.transform.SetParent(m_MapType, false);

			dropDown.Setup(bigInterface.MapTypes, bigInterface.CurrentMapType);

			dropDown.OnSelectUpdate.AddListener(new UnityEngine.Events.UnityAction<string>(SetType));
		}

		private void SetType(string selection)
		{
			if (bigInterface == null)
			{
				return;
			}

			bigInterface.CurrentMapType = selection;

			dropDown.FadeOut();
			dropDown = null;

			if (m_DropDownToggles != null)
			{
				m_DropDownToggles.SetAllTogglesOff();
			}

			RefreshIcons();

			SetLegend(bigInterface.LegendToggle);
		}

		public void ToggleResourceSelection(bool isOn)
		{
			if (dropDown != null)
			{
				dropDown.FadeOut(true);
				dropDown = null;
			}

			if (!isOn)
			{
				return;
			}

			if (bigInterface == null || m_DropDownPrefab == null || m_Resources == null)
			{
				return;
			}

			dropDown = Instantiate(m_DropDownPrefab).GetComponent<SCAN_DropDown>();

			if (dropDown == null)
			{
				return;
			}

			dropDown.transform.SetParent(m_Resources, false);

			dropDown.Setup(bigInterface.Resources, bigInterface.CurrentResource);

			dropDown.OnSelectUpdate.AddListener(new UnityEngine.Events.UnityAction<string>(SetResource));
		}

		private void SetResource(string selection)
		{
			if (bigInterface == null)
			{
				return;
			}

			bigInterface.CurrentResource = selection;

			loaded = false;
			if (m_ResourcesToggle != null)
			{
				m_ResourcesToggle.isOn = true;
			}

			loaded = true;

			bigInterface.ResourceToggle = true;

			SetResourceLegend();

			dropDown.FadeOut();
			dropDown = null;

			if (m_DropDownToggles != null)
			{
				m_DropDownToggles.SetAllTogglesOff();
			}
		}

		public void ToggleCelestialBodySelection(bool isOn)
		{
			if (dropDown != null)
			{
				dropDown.FadeOut(true);
				dropDown = null;
			}

			if (!isOn)
			{
				return;
			}

			if (bigInterface == null || m_DropDownPrefab == null || m_CelestialBody == null)
			{
				return;
			}

			dropDown = Instantiate(m_DropDownPrefab).GetComponent<SCAN_DropDown>();

			if (dropDown == null)
			{
				return;
			}

			dropDown.transform.SetParent(m_CelestialBody, false);

			dropDown.Setup(bigInterface.CelestialBodies, bigInterface.CurrentCelestialBody);

			dropDown.OnSelectUpdate.AddListener(new UnityEngine.Events.UnityAction<string>(SetCelestialBody));
		}

		private void SetCelestialBody(string selection)
		{
			if (bigInterface == null)
			{
				return;
			}

			bigInterface.CurrentCelestialBody = selection;

			SetLegend(bigInterface.LegendToggle);

			if (bigInterface.ResourceToggle)
			{
				SetResourceLegend();
			}

			dropDown.FadeOut();
			dropDown = null;

			if (m_DropDownToggles != null)
			{
				m_DropDownToggles.SetAllTogglesOff();
			}

			RefreshIcons();
		}

		public void RefreshMap()
		{
			if (bigInterface == null)
			{
				return;
			}

			bigInterface.RefreshMap();

			RefreshIcons();
		}

		public void ToggleColor(bool isOn)
		{
			if (!loaded || bigInterface == null)
			{
				return;
			}

			bigInterface.ColorToggle = isOn;

			RefreshIcons();

			SetLegend(bigInterface.LegendToggle);
		}

		public void ToggleTerminator(bool isOn)
		{
			if (!loaded || bigInterface == null)
			{
				return;
			}

			bigInterface.TerminatorToggle = isOn;

			RefreshIcons();
		}

		public void ToggleGrid(bool isOn)
		{
			if (!loaded || bigInterface == null)
			{
				return;
			}

			bigInterface.GridToggle = isOn;
		}

		public void ToggleOrbit(bool isOn)
		{
			if (!loaded || bigInterface == null)
			{
				return;
			}

			bigInterface.OrbitToggle = isOn;

			RefreshIcons();
		}

		public void ToggleWaypoint(bool isOn)
		{
			if (!loaded || bigInterface == null)
			{
				return;
			}

			bigInterface.WaypointToggle = isOn;

			if (isOn && bigInterface.ShowWaypoint)
			{
				SetWaypointIcons(bigInterface.WaypointInfoList);
			}
			else
			{
				for (int i = waypointLabels.Count - 1; i >= 0; i--)
				{
					SCAN_MapLabel m = waypointLabels[i];

					m.gameObject.SetActive(false);
					Destroy(m.gameObject);
				}

				waypointLabels.Clear();
			}
		}

		public void ToggleAnomaly(bool isOn)
		{
			if (!loaded || bigInterface == null)
			{
				return;
			}

			bigInterface.AnomalyToggle = isOn;

			if (isOn)
			{
				SetAnomalyIcons(bigInterface.AnomalyInfoList);
			}
			else
			{
				for (int i = anomalyLabels.Count - 1; i >= 0; i--)
				{
					SCAN_MapLabel m = anomalyLabels[i];

					m.gameObject.SetActive(false);
					Destroy(m.gameObject);
				}

				anomalyLabels.Clear();
			}
		}

		public void ToggleFlag(bool isOn)
		{
			if (!loaded || bigInterface == null)
			{
				return;
			}

			bigInterface.FlagToggle = isOn;

			if (isOn)
			{
				SetFlagIcons(bigInterface.FlagInfoList);
			}
			else
			{
				for (int i = flagLabels.Count - 1; i >= 0; i--)
				{
					SCAN_MapLabel m = flagLabels[i];

					m.gameObject.SetActive(false);
					Destroy(m.gameObject);
				}

				flagLabels.Clear();
			}
		}

		public void ToggleLegend(bool isOn)
		{
			if (!loaded || bigInterface == null)
			{
				return;
			}

			bigInterface.LegendToggle = isOn;

			SetLegend(isOn);
		}

		public void ToggleResource(bool isOn)
		{
			if (!loaded || bigInterface == null)
			{
				return;
			}

			bigInterface.ResourceToggle = isOn;

			if (m_ResourceBar != null)
			{
				m_ResourceBar.SetActive(isOn);
			}

			RefreshIcons();

			if (isOn)
			{
				SetResourceLegend();
			}
		}

		public void ResourceCutoffMinus()
		{
			if (bigInterface == null)
			{
				return;
			}

			bigInterface.DecreaseResourceCutoff();

			SetResourceLegend();
		}

		public void ResourceCutoffPlus()
		{
			if (bigInterface == null)
			{
				return;
			}

			bigInterface.IncreaseResourceCutoff();

			SetResourceLegend();
		}

		public void OpenResourceSettings()
		{
			if (bigInterface == null)
			{
				return;
			}

			bigInterface.OpenResourceSettings();
		}

		public void OpenSmallMap()
		{
			if (bigInterface == null)
			{
				return;
			}

			bigInterface.OpenMainMap();
		}

		public void OpenInstruments()
		{
			if (bigInterface == null)
			{
				return;
			}

			bigInterface.OpenInstruments();
		}

		public void OpenSettings()
		{
			if (bigInterface == null)
			{
				return;
			}

			bigInterface.OpenSettings();
		}

		public void OpenZoomMap()
		{
			if (bigInterface == null)
			{
				return;
			}

			bigInterface.OpenZoomMap();
		}

		public void OpenOverlay()
		{
			if (bigInterface == null)
			{
				return;
			}

			bigInterface.OpenOverlay();
		}

		public void ExportMap()
		{
			if (bigInterface == null)
			{
				return;
			}

			bigInterface.ExportMap();
		}

		public void GenerateWaypoint()
		{
			waypointSelecting = !waypointSelecting;

			DestroyWaypoint(tempWaypointLabel);
			DestroyWaypoint(hoverWaypointLabel);

			if (bigInterface == null)
			{
				return;
			}

			bigInterface.LockInput = false;

			if (m_WaypointBar != null)
			{
				m_WaypointBar.SetActive(waypointSelecting);
			}

			if (waypointSelecting)
			{
				HoverWaypoint();

				if (m_MechJebButton != null)
				{
					m_MechJebButton.SetActive(bigInterface.MechJebAvailable);
				}

				if (m_WaypointInput != null)
				{
					if (string.IsNullOrEmpty(waypoint))
					{
						m_WaypointInput.OnTextUpdate.Invoke(bigInterface.RandomWaypoint);
					}
					else
					{
						m_WaypointInput.OnTextUpdate.Invoke(waypoint);
					}
				}
			}
		}

		private void HoverWaypoint()
		{
			MapLabelInfo info = new MapLabelInfo()
			{
				label = "",
				image = bigInterface.WaypointSprite,
				pos = new Vector2(),
				baseColor = Color.white,
				flashColor = Color.red,
				flash = true,
				width = 20,
				alignBottom = 10,
				show = false
			};

			hoverWaypointLabel = createWaypoint(0, info, true);
		}

		public void OnInputClick(BaseEventData eventData)
		{
			if (!(eventData is PointerEventData) || bigInterface == null)
			{
				return;
			}

			if (((PointerEventData)eventData).button != PointerEventData.InputButton.Left)
			{
				return;
			}

			bigInterface.LockInput = true;
		}

		public void RefreshWaypoint()
		{
			DestroyWaypoint(tempWaypointLabel);

			if (bigInterface == null || m_WaypointInput == null)
			{
				return;
			}

			m_WaypointInput.OnTextUpdate.Invoke(bigInterface.RandomWaypoint);

			waypoint = "";
		}

		public void SetWaypoint()
		{
			if (bigInterface == null || m_WaypointInput == null)
			{
				return;
			}

			bigInterface.LockInput = false;

			waypoint = "";

			if (tempWaypointLabel != null)
			{
				bigInterface.SetWaypoint(m_WaypointInput.Text, tempWaypointLabel.Info.pos);
			}

			GenerateWaypoint();

			RefreshIcons();

			waypointSelecting = false;
		}

		public void CancelWaypoint()
		{
			if (bigInterface != null)
			{
				bigInterface.LockInput = false;
			}

			GenerateWaypoint();

			RefreshIcons();

			waypointSelecting = false;
		}

		public void MechJebLanding()
		{
			if (bigInterface != null)
			{
				bigInterface.LockInput = false;
			}

			waypoint = "";

			if (tempWaypointLabel != null)
			{
				bigInterface.SetMJWaypoint(tempWaypointLabel.Info.pos);
			}

			GenerateWaypoint();

			RefreshIcons();

			waypointSelecting = false;
		}

		private void DestroyWaypoint(SCAN_MapLabel waypoint)
		{
			if (waypoint != null)
			{
				waypoint.gameObject.SetActive(false);
				Destroy(waypoint.gameObject);
				waypoint = null;
			}
		}
	}
}
