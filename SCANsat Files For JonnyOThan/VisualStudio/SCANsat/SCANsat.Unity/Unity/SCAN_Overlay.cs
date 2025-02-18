#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_Overlay - Script for controlling the planetary overlay UI
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using SCANsat.Unity.Interfaces;

namespace SCANsat.Unity.Unity
{
	public class SCAN_Overlay : CanvasFader, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerDownHandler
	{
		[SerializeField]
		private TextHandler m_Version = null;
		[SerializeField]
		private TextHandler m_BiomeText = null;
		[SerializeField]
		private TextHandler m_TerrainText = null;
		[SerializeField]
		private SCAN_Toggle m_OverlayToggle = null;
		[SerializeField]
		private GameObject m_RefreshButton = null;
		[SerializeField]
		private GameObject m_ResourcePrefab = null;
		[SerializeField]
		private Transform m_ResourceTransform = null;
		[SerializeField]
		private Color m_ActiveColor = Color.white;
		[SerializeField]
		private Color m_NormalColor = Color.white;
		[SerializeField]
		private GameObject m_TooltipPrefab = null;

		private ISCAN_Overlay overInterface;
		private bool loaded;
		private RectTransform rect;
		private Vector2 mouseStart;
		private Vector3 windowStart;

		private bool tooltipOn;
		private SCAN_Tooltip _tooltip;

		private List<SCAN_ResourceOverlay> resources = new List<SCAN_ResourceOverlay>();
		
		public Color ActiveColor
		{
			get { return m_ActiveColor; }
		}

		public Color NormalColor
		{
			get { return m_NormalColor; }
		}

		public ISCAN_Overlay OverlayInterface
		{
			get { return overInterface; }
		}

		protected override void Awake()
		{
			base.Awake();

			rect = GetComponent<RectTransform>();

			Alpha(0);
		}

		private void Update()
		{
			if (overInterface == null || !overInterface.IsVisible)
				return;

			overInterface.Update();

			if (overInterface.OverlayTooltip)
			{
				if (!tooltipOn)
				{
					if (_tooltip != null)
						CloseTooltip();

					tooltipOn = true;
					OpenTooltip();
				}
				else if (_tooltip != null)
					_tooltip.UpdateText(overInterface.TooltipText);
					
			}
			else if (tooltipOn)
			{
				tooltipOn = false;
				CloseTooltip();
			}
		}

		private void OpenTooltip()
		{
			if (m_TooltipPrefab == null || overInterface.TooltipCanvas == null)
				return;

			_tooltip = Instantiate(m_TooltipPrefab).GetComponent<SCAN_Tooltip>();

			if (_tooltip == null)
				return;

			_tooltip.transform.SetParent(overInterface.TooltipCanvas.transform, false);
			_tooltip.transform.SetAsLastSibling();

			_tooltip.Setup(overInterface.TooltipCanvas, "0°0'0\"N 0°0'0\"W", overInterface.Scale);
		}

		private void CloseTooltip()
		{
			if (_tooltip == null)
				return;

			_tooltip.gameObject.SetActive(false);
			Destroy(_tooltip.gameObject);
			_tooltip = null;
		}

		public void SetOverlay(ISCAN_Overlay over)
		{
			if (over == null)
				return;

			overInterface = over;

			if (m_Version != null)
				m_Version.OnTextUpdate.Invoke(over.Version);

			if (m_OverlayToggle != null)
				m_OverlayToggle.isOn = over.DrawOverlay;

			if (over.DrawBiome)
			{
				if (m_BiomeText != null)
				{
					m_BiomeText.OnColorUpdate.Invoke(m_ActiveColor);
					m_BiomeText.SetNormalColor(m_ActiveColor);
				}
			}
			else if (over.DrawTerrain)
			{
				if (m_TerrainText != null)
				{
					m_TerrainText.OnColorUpdate.Invoke(m_ActiveColor);
					m_TerrainText.SetNormalColor(m_ActiveColor);
				}
			}

			if (m_RefreshButton != null)
				m_RefreshButton.SetActive(over.DrawOverlay);

			CreateResources(over.Resources);

			SetScale(over.Scale);

			SetPosition(over.Position);

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
			if (overInterface != null)
				overInterface.IsVisible = false;
		}

		public void ProcessTooltips()
		{
			if (overInterface == null)
				return;

			TooltipHandler[] handlers = gameObject.GetComponentsInChildren<TooltipHandler>(true);

			if (handlers == null)
				return;

			for (int j = 0; j < handlers.Length; j++)
				ProcessTooltip(handlers[j], overInterface.WindowTooltips, overInterface.TooltipCanvas, overInterface.Scale);
		}

		private void ProcessTooltip(TooltipHandler handler, bool isOn, Canvas c, float scale)
		{
			if (handler == null)
				return;

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
				return;

			rect.anchoredPosition = new Vector3(pos.x, pos.y, 0);
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			transform.SetAsLastSibling();
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			if (rect == null)
				return;

			mouseStart = eventData.position;
			windowStart = rect.position;
		}

		public void OnDrag(PointerEventData eventData)
		{
			if (rect == null)
				return;

			rect.position = windowStart + (Vector3)(eventData.position - mouseStart);

			if (overInterface == null)
				return;

			overInterface.ClampToScreen(rect);
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			if (rect == null || overInterface == null)
				return;

			overInterface.Position = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y);
		}

		private void CreateResources(IList<string> resources)
		{
			if (resources == null)
				return;

			if (m_ResourcePrefab == null || m_ResourceTransform == null)
				return;

			for (int i = 0; i < resources.Count; i++)
			{
				string s = resources[i];

				CreateResource(s);
			}
		}

		private void CreateResource(string resource)
		{
			SCAN_ResourceOverlay res = Instantiate(m_ResourcePrefab).GetComponent<SCAN_ResourceOverlay>();

			if (res == null)
				return;

			res.transform.SetParent(m_ResourceTransform, false);

			res.SetResource(resource, this, overInterface.CurrentResource == resource);

			resources.Add(res);
		}

		public void SetResource(string resource, bool isOn)
		{
			if (overInterface == null)
				return;

			overInterface.SetResource(resource, isOn);

			if (isOn)
			{
				if (m_RefreshButton != null)
					m_RefreshButton.SetActive(true);

				loaded = false;

				if (m_OverlayToggle != null)
					m_OverlayToggle.isOn = true;

				loaded = true;
			}
			else
			{
				if (overInterface.DrawResource && overInterface.CurrentResource == resource)
				{
					if (m_RefreshButton != null)
						m_RefreshButton.SetActive(false);

					loaded = false;

					if (m_OverlayToggle != null)
						m_OverlayToggle.isOn = false;

					loaded = true;
				}
			}

			InactivateOthers();
		}

		public void DrawBiome()
		{
			if (!loaded || overInterface == null)
				return;

			overInterface.DrawBiome = overInterface.DrawBiome ? !overInterface.DrawOverlay : true;

			if (m_BiomeText != null)
				m_BiomeText.SetNormalColor(m_ActiveColor);

			if (m_RefreshButton != null)
				m_RefreshButton.SetActive(overInterface.DrawOverlay);

			if (m_OverlayToggle != null)
			{
				loaded = false;
				m_OverlayToggle.isOn = overInterface.DrawOverlay;
				loaded = true;
			}

			InactivateOthers();
		}

		public void DrawTerrain()
		{
			if (!loaded || overInterface == null)
				return;

			overInterface.DrawTerrain = overInterface.DrawTerrain ? !overInterface.DrawOverlay : true;

			if (m_TerrainText != null)
				m_TerrainText.SetNormalColor(m_ActiveColor);

			if (m_RefreshButton != null)
				m_RefreshButton.SetActive(overInterface.DrawOverlay);
			
			if (m_OverlayToggle != null)
			{
				loaded = false;
				m_OverlayToggle.isOn = overInterface.DrawOverlay;
				loaded = true;
			}
						
			InactivateOthers();
		}

		private void InactivateOthers()
		{
			if (overInterface == null)
				return;

			if (m_BiomeText != null && !overInterface.DrawBiome)
			{
				m_BiomeText.OnColorUpdate.Invoke(m_NormalColor);
				m_BiomeText.SetNormalColor(m_NormalColor);
			}

			if (m_TerrainText != null && !overInterface.DrawTerrain)
			{
				m_TerrainText.OnColorUpdate.Invoke(m_NormalColor);
				m_TerrainText.SetNormalColor(m_NormalColor);
			}

			for (int i = resources.Count - 1; i >= 0; i--)
			{
				SCAN_ResourceOverlay resource = resources[i];

				if (resource == null)
					continue;

				if (!overInterface.DrawResource)
					resource.Inactivate();
				else if (resource.Resource != overInterface.CurrentResource)
					resource.Inactivate();
			}
		}
		
		public void DrawOverlay(bool isOn)
		{
			if (!loaded || overInterface == null)
				return;

			overInterface.DrawOverlay = isOn;

			if (m_RefreshButton != null)
				m_RefreshButton.SetActive(isOn);
		}

		public void Refresh()
		{
			if (overInterface == null)
				return;

			overInterface.Refresh();
		}

		public void Settings()
		{
			if (overInterface == null)
				return;

			overInterface.OpenSettings();
		}
	}
}
