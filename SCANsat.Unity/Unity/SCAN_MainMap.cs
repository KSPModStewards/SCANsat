#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_MainMap - Script for controlling the main map UI
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
	public class SCAN_MainMap : CanvasFader, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerDownHandler
	{
		[SerializeField]
		private TextHandler m_Version = null;
		[SerializeField]
		private Toggle m_ColorToggle = null;
		[SerializeField]
		private Toggle m_TerminatorToggle = null;
		[SerializeField]
		private Toggle m_TypeToggle = null;
		[SerializeField]
		private Toggle m_MinimizeToggle = null;
		[SerializeField]
		private TextHandler m_MinimizeText = null;
		[SerializeField]
		private TextHandler m_TypeLabel = null;
		[SerializeField]
		private RawImage m_MainMap = null;
		[SerializeField]
		private TextHandler m_LoText = null;
		[SerializeField]
		private TextHandler m_HiText = null;
		[SerializeField]
		private TextHandler m_MultiText = null;
		[SerializeField]
		private TextHandler m_M700Text = null;
		[SerializeField]
		private TextHandler m_VisLoText = null;
		[SerializeField]
		private TextHandler m_VisHiText = null;
		[SerializeField]
		private TextHandler m_OreText = null;
		[SerializeField]
		private TextHandler m_PercentageText = null;
		[SerializeField]
		private GameObject m_VesselPrefab = null;
		[SerializeField]
		private Transform m_VesselTransform = null;
		[SerializeField]
		private GameObject m_MapPrefab = null;
		[SerializeField]
		private GameObject m_GeneratingText = null;

		private ISCAN_MainMap mapInterface;
		private bool loaded;
		private bool generating;
		private RectTransform rect;
		private Vector2 mouseStart;
		private Vector3 windowStart;
		private List<SCAN_VesselInfo> vessels = new List<SCAN_VesselInfo>();
		private List<SCAN_MapLabel> mapLabels = new List<SCAN_MapLabel>();

		protected override void Awake()
		{
			base.Awake();

			rect = GetComponent<RectTransform>();

			Alpha(0);
		}

		private void Update()
		{
			if (mapInterface == null || !mapInterface.IsVisible)
			{
				return;
			}

			mapInterface.Update();

			if (generating)
			{
				SetGeneratingText(mapInterface.MapGenerating);
			}

			if (!mapInterface.Minimized)
			{
				for (int i = vessels.Count - 1; i >= 0; i--)
				{
					SCAN_VesselInfo vessel = vessels[i];

					vessel.UpdateText(mapInterface.VesselInfo(vessel.ID));
				}
			}

			for (int i = mapLabels.Count - 1; i >= 0; i--)
			{
				SCAN_MapLabel mapLabel = mapLabels[i];

				mapLabel.UpdatePosition(mapInterface.VesselPosition(mapLabel.ID));
			}
		}

		public void setMap(ISCAN_MainMap map)
		{
			if (map == null)
			{
				return;
			}

			mapInterface = map;

			if (m_Version != null)
			{
				m_Version.OnTextUpdate.Invoke(map.Version);
			}

			if (m_ColorToggle != null)
			{
				m_ColorToggle.isOn = map.Color;
			}

			if (m_TerminatorToggle != null)
			{
				m_TerminatorToggle.isOn = map.TerminatorToggle;
			}

			if (m_TypeToggle != null)
			{
				m_TypeToggle.isOn = map.MapType;
			}

			if (m_MinimizeToggle != null)
			{
				m_MinimizeToggle.isOn = map.Minimized;
			}

			//if (m_M700Text != null && !map.ResourcesOn)
			//	m_M700Text.gameObject.SetActive(false);

			//if (m_OreText != null && !map.ResourcesOn)
			//	m_OreText.gameObject.SetActive(false);

			CreateVessels(map.VesselInfoList);

			SetScale(map.Scale);

			SetPosition(map.Position);

			if (!map.MapType)
			{
				SetGeneratingText(map.MapGenerating);
			}

			ProcessTooltips();

			if (m_VesselTransform != null)
			{
				m_VesselTransform.gameObject.SetActive(!map.Minimized);
			}

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
			if (mapInterface != null)
			{
				mapInterface.IsVisible = false;
			}
		}

		private void SetGeneratingText(bool isOn)
		{
			if (m_GeneratingText == null)
			{
				return;
			}

			generating = isOn;

			if (isOn && !m_GeneratingText.activeSelf)
			{
				m_GeneratingText.SetActive(true);
			}
			else if (!isOn && m_GeneratingText.activeSelf)
			{
				m_GeneratingText.SetActive(false);
			}
		}

		public void ProcessTooltips()
		{
			if (mapInterface == null)
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
				ProcessTooltip(handlers[j], mapInterface.TooltipsOn, mapInterface.TooltipCanvas, mapInterface.Scale);
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

		public void RefreshVesselTypes()
		{
			if (mapInterface == null)
			{
				return;
			}

			for (int i = mapLabels.Count - 1; i >= 0; i--)
			{
				SCAN_MapLabel mapLabel = mapLabels[i];

				mapLabel.UpdateImage(mapInterface.VesselType(mapLabel.ID));
			}
		}

		public void RefreshVessels()
		{
			for (int i = vessels.Count - 1; i >= 0; i--)
			{
				SCAN_VesselInfo v = vessels[i];

				v.gameObject.SetActive(false);
				Destroy(v.gameObject);
			}

			for (int i = mapLabels.Count - 1; i >= 0; i--)
			{
				SCAN_MapLabel m = mapLabels[i];

				m.gameObject.SetActive(false);
				Destroy(m.gameObject);
			}

			vessels.Clear();
			mapLabels.Clear();

			if (mapInterface != null)
			{
				CreateVessels(mapInterface.VesselInfoList);
			}
		}

		private void CreateVessels(Dictionary<Guid, MapLabelInfo> vessels)
		{
			if (vessels == null)
			{
				return;
			}

			if (mapInterface == null || m_VesselPrefab == null || m_MapPrefab == null || m_VesselTransform == null || m_MainMap == null)
			{
				return;
			}

			for (int i = 0; i < vessels.Count; i++)
			{
				Guid id = vessels.ElementAt(i).Key;

				MapLabelInfo label;

				if (!vessels.TryGetValue(id, out label))
				{
					continue;
				}

				CreateVessel(id, label);

				CreateMapLabel(id, label);
			}
		}

		private void CreateVessel(Guid id, MapLabelInfo info)
		{
			SCAN_VesselInfo vInfo = Instantiate(m_VesselPrefab).GetComponent<SCAN_VesselInfo>();

			if (vInfo == null)
			{
				return;
			}

			vInfo.transform.SetParent(m_VesselTransform, false);

			vInfo.SetVessel(id, info, mapInterface);

			vessels.Add(vInfo);
		}

		private void CreateMapLabel(Guid id, MapLabelInfo info)
		{
			SCAN_MapLabel mapLabel = Instantiate(m_MapPrefab).GetComponent<SCAN_MapLabel>();

			if (mapLabel == null)
			{
				return;
			}

			mapLabel.transform.SetParent(m_MainMap.transform, false);

			mapLabel.Setup(id, info);

			mapLabels.Add(mapLabel);
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			transform.SetAsLastSibling();
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

			if (mapInterface == null)
			{
				return;
			}

			mapInterface.ClampToScreen(rect);
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			if (rect == null || mapInterface == null)
			{
				return;
			}

			mapInterface.Position = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y);
		}

		public void UpdateLoColor(Color c)
		{
			if (m_LoText == null)
			{
				return;
			}

			m_LoText.OnColorUpdate.Invoke(c);
		}

		public void UpdateHiColor(Color c)
		{
			if (m_HiText == null)
			{
				return;
			}

			m_HiText.OnColorUpdate.Invoke(c);
		}

		public void UpdateMultiColor(Color c)
		{
			if (m_MultiText == null)
			{
				return;
			}

			m_MultiText.OnColorUpdate.Invoke(c);
		}

		public void UpdateM700Color(Color c)
		{
			if (m_M700Text == null)
			{
				return;
			}

			m_M700Text.OnColorUpdate.Invoke(c);
		}

		public void UpdateVisLoColor(Color c)
		{
			if (m_VisLoText == null)
			{
				return;
			}

			m_VisLoText.OnColorUpdate.Invoke(c);
		}

		public void UpdateVisHiColor(Color c)
		{
			if (m_VisHiText == null)
			{
				return;
			}

			m_VisHiText.OnColorUpdate.Invoke(c);
		}

		public void UpdateOreColor(Color c)
		{
			if (m_OreText == null)
			{
				return;
			}

			m_OreText.OnColorUpdate.Invoke(c);
		}

		public void UpdatePercentage(string text)
		{
			if (m_PercentageText == null)
			{
				return;
			}

			m_PercentageText.OnTextUpdate.Invoke(text);
		}

		public void UpdateMapTexture(Texture2D map)
		{
			if (m_MainMap == null)
			{
				return;
			}

			m_MainMap.texture = map;
		}

		public void ToggleColor(bool isOn)
		{
			if (!loaded || mapInterface == null)
			{
				return;
			}

			mapInterface.Color = isOn;
		}

		public void ToggleTerminator(bool isOn)
		{
			if (!loaded || mapInterface == null)
			{
				return;
			}

			mapInterface.TerminatorToggle = isOn;
		}

		public void ToggleType(bool isOn)
		{
			if (m_TypeLabel != null)
			{
				m_TypeLabel.OnTextUpdate.Invoke(isOn ? "Biome" : "Terrain");
			}

			if (!loaded || mapInterface == null)
			{
				return;
			}

			mapInterface.MapType = isOn;
		}

		public void ToggleSize(bool isOn)
		{
			if (m_MinimizeText != null)
			{
				m_MinimizeText.OnTextUpdate.Invoke(isOn ? "+" : "-");
			}

			if (!loaded || mapInterface == null)
			{
				return;
			}

			mapInterface.Minimized = isOn;

			if (m_VesselTransform != null)
			{
				m_VesselTransform.gameObject.SetActive(!isOn);
			}
		}

		public void OpenBigMap()
		{
			if (mapInterface == null)
			{
				return;
			}

			mapInterface.OpenBigMap();
		}

		public void OpenInstruments()
		{
			if (mapInterface == null)
			{
				return;
			}

			mapInterface.OpenInstruments();
		}

		public void OpenSettings()
		{
			if (mapInterface == null)
			{
				return;
			}

			mapInterface.OpenSettings();
		}

		public void OpenZoomMap()
		{
			if (mapInterface == null)
			{
				return;
			}

			mapInterface.OpenZoomMap();
		}

		public void OpenOverlay()
		{
			if (mapInterface == null)
			{
				return;
			}

			mapInterface.OpenOverlay();
		}
	}
}
