#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_Toolbar - Script for controlling the toolbar menu UI
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using SCANsat.Unity.Interfaces;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SCANsat.Unity.Unity
{
	public class SCAN_Toolbar : CanvasFader, IPointerEnterHandler, IPointerExitHandler
	{
		[SerializeField]
		private Toggle m_MainMapToggle = null;
		[SerializeField]
		private Toggle m_BigMapToggle = null;
		[SerializeField]
		private Toggle m_ZoomMapToggle = null;
		[SerializeField]
		private Toggle m_OverlayToggle = null;
		[SerializeField]
		private Toggle m_InstrumentsToggle = null;
		[SerializeField]
		private Toggle m_SettingsToggle = null;

		private bool loaded;
		private ISCAN_Toolbar toolbarInterface;

		protected override void Awake()
		{
			base.Awake();

			Alpha(0);
		}

		public void Setup(ISCAN_Toolbar toolbar)
		{
			if (toolbar == null)
				return;

			toolbarInterface = toolbar;

			if (m_MainMapToggle != null)
				m_MainMapToggle.isOn = toolbar.MainMap;

			if (m_BigMapToggle != null)
				m_BigMapToggle.isOn = toolbar.BigMap;

			if (m_ZoomMapToggle != null)
				m_ZoomMapToggle.isOn = toolbar.ZoomMap;

			if (m_OverlayToggle != null)
				m_OverlayToggle.isOn = toolbar.Overlay;

			if (m_InstrumentsToggle != null)
				m_InstrumentsToggle.isOn = toolbar.Instruments;

			if (m_SettingsToggle != null)
				m_SettingsToggle.isOn = toolbar.Settings;

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

		public void ProcessTooltips()
		{
			if (toolbarInterface == null)
				return;

			TooltipHandler[] handlers = gameObject.GetComponentsInChildren<TooltipHandler>(true);

			if (handlers == null)
				return;

			for (int j = 0; j < handlers.Length; j++)
				ProcessTooltip(handlers[j], toolbarInterface.TooltipsOn, toolbarInterface.TooltipCanvas, toolbarInterface.Scale);
		}

		private void ProcessTooltip(TooltipHandler handler, bool isOn, Canvas c, float scale)
		{
			if (handler == null)
				return;

			handler.IsActive = isOn && !handler.HelpTip;
			handler._Canvas = c;
			handler.Scale = scale;
		}

		public void SetMainMapToggle(bool isOn)
		{
			if (m_MainMapToggle == null)
				return;

			loaded = false;

			m_MainMapToggle.isOn = isOn;

			loaded = true;
		}

		public void SetBigMapToggle(bool isOn)
		{
			if (m_BigMapToggle == null)
				return;

			loaded = false;

			m_BigMapToggle.isOn = isOn;

			loaded = true;
		}

		public void SetZoomMapToggle(bool isOn)
		{
			if (m_ZoomMapToggle == null)
				return;

			loaded = false;

			m_ZoomMapToggle.isOn = isOn;

			loaded = true;
		}

		public void SetOverlayToggle(bool isOn)
		{
			if (m_OverlayToggle == null)
				return;

			loaded = false;

			m_OverlayToggle.isOn = isOn;

			loaded = true;
		}

		public void SetInstrumentToggle(bool isOn)
		{
			if (m_InstrumentsToggle == null)
				return;

			loaded = false;

			m_InstrumentsToggle.isOn = isOn;

			loaded = true;
		}

		public void SetSettingsToggle(bool isOn)
		{
			if (m_SettingsToggle == null)
				return;

			loaded = false;

			m_SettingsToggle.isOn = isOn;

			loaded = true;
		}

		public void ToggleMainMap(bool isOn)
		{
			if (!loaded || toolbarInterface == null)
				return;

			toolbarInterface.MainMap = isOn;
		}

		public void ToggleBigMap(bool isOn)
		{
			if (!loaded || toolbarInterface == null)
				return;

			toolbarInterface.BigMap = isOn;
		}

		public void ToggleZoomMap(bool isOn)
		{
			if (!loaded || toolbarInterface == null)
				return;

			toolbarInterface.ZoomMap = isOn;
		}

		public void ToggleOverlay(bool isOn)
		{
			if (!loaded || toolbarInterface == null)
				return;

			toolbarInterface.Overlay = isOn;
		}

		public void ToggleInstruments(bool isOn)
		{
			if (!loaded || toolbarInterface == null)
				return;

			toolbarInterface.Instruments = isOn;
		}

		public void ToggleSettings(bool isOn)
		{
			if (!loaded || toolbarInterface == null)
				return;

			toolbarInterface.Settings = isOn;
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (toolbarInterface != null)
				toolbarInterface.InMenu = true;
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			if (toolbarInterface != null)
				toolbarInterface.InMenu = false;
		}
	}
}
