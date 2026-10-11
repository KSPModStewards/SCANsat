#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_SettingsGeneral - Script for controlling the general settings page
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
	public class SCAN_SettingsGeneral : SettingsPage
	{
		[SerializeField]
		private SCAN_Toggle m_GroundTrackToggle = null;
		[SerializeField]
		private SCAN_Toggle m_GroundTrackActiveToggle = null;
		[SerializeField]
		private SCAN_Toggle m_WindowTooltipToggle = null;
		[SerializeField]
		private SCAN_Toggle m_LegendTooltipToggle = null;
		[SerializeField]
		private SCAN_Toggle m_StockToolbarToggle = null;
		[SerializeField]
		private SCAN_Toggle m_ToolbarMenuToggle = null;
		[SerializeField]
		private SCAN_Toggle m_StockUIToggle = null;
		[SerializeField]
		private SCAN_Toggle m_MechJebToggle = null;
		[SerializeField]
		private SCAN_Toggle m_MechJebLoadToggle = null;
		[SerializeField]
		private SCAN_Toggle m_DaylightCheckToggle = null;
		[SerializeField]
		private GameObject m_MechJebBar = null;
		[SerializeField]
		private TextHandler m_UIScale = null;
		[SerializeField]
		private TextHandler m_MapBudget = null;
		[SerializeField]
		private Slider m_MapBudgetSlider = null;
		[SerializeField]
		private TextHandler m_Scanline = null;
		[SerializeField]
		private Slider m_ScanlineSlider = null;
		[SerializeField]
		private Slider m_UIScaleSlider = null;

		// Both map sliders are four-stop indices into these tables. The settings store the value itself,
		// not the index, so a settings file edited by hand keeps whatever it says and the slider shows
		// the nearest stop; the labels always report the real value.
		private static readonly float[] budgets = new float[] { 2, 4, 8, 16 };
		private static readonly float[] scanlineSpeeds = new float[] { 0.5f, 1f, 2f, 0f };   // 0: Instant, no reveal

		private bool loaded;
		private ISCAN_Settings settings;

		public void setup(ISCAN_Settings set)
		{
			if (set == null)
			{
				return;
			}

			settings = set;

			if (m_GroundTrackToggle != null)
			{
				m_GroundTrackToggle.isOn = set.GroundTracks;
			}

			if (m_GroundTrackActiveToggle != null)
			{
				m_GroundTrackActiveToggle.isOn = set.ActiveGround;
				m_GroundTrackActiveToggle.gameObject.SetActive(set.GroundTracks);
			}

			if (m_WindowTooltipToggle != null)
			{
				m_WindowTooltipToggle.isOn = set.WindowTooltips;
			}

			if (m_LegendTooltipToggle != null)
			{
				m_LegendTooltipToggle.isOn = set.LegendTooltips;
			}

			if (m_StockToolbarToggle != null)
			{
				m_StockToolbarToggle.isOn = set.StockToolbar;
			}

			if (m_MechJebBar != null)
			{
				m_MechJebBar.SetActive(set.MechJebAvailable);

				if (m_MechJebToggle != null)
				{
					m_MechJebToggle.isOn = set.MechJebTarget;
				}

				if (m_MechJebLoadToggle != null)
				{
					m_MechJebLoadToggle.isOn = set.MechJebLoad;
					m_MechJebLoadToggle.gameObject.SetActive(set.MechJebTarget);
				}
			}

			if (m_DaylightCheckToggle != null)
			{
				m_DaylightCheckToggle.isOn = set.DaylightCheck;
			}

			if (m_ToolbarMenuToggle != null)
			{
				m_ToolbarMenuToggle.isOn = set.ToolbarMenu;
				m_ToolbarMenuToggle.gameObject.SetActive(set.StockToolbar);
			}

			if (m_StockUIToggle != null)
			{
				m_StockUIToggle.isOn = set.StockUIStyle;
			}

			if (m_MapBudgetSlider != null)
			{
				m_MapBudgetSlider.value = nearestStop(budgets, set.MapGenBudget);
			}

			if (m_MapBudget != null)
			{
				m_MapBudget.OnTextUpdate.Invoke(budgetLabel(set.MapGenBudget));
			}

			if (m_ScanlineSlider != null)
			{
				m_ScanlineSlider.value = nearestStop(scanlineSpeeds, set.ScanlineSpeed);
			}

			if (m_Scanline != null)
			{
				m_Scanline.OnTextUpdate.Invoke(scanlineLabel(set.ScanlineSpeed));
			}

			if (m_UIScale != null)
			{
				m_UIScale.OnTextUpdate.Invoke("UI Scale: " + set.UIScale.ToString("P0"));
			}

			if (m_UIScaleSlider != null)
			{
				m_UIScaleSlider.value = set.UIScale * 100;
			}

			loaded = true;
		}

		public void GroundTrack(bool isOn)
		{
			if (!loaded || settings == null)
			{
				return;
			}

			settings.GroundTracks = isOn;

			if (m_GroundTrackActiveToggle != null)
			{
				m_GroundTrackActiveToggle.gameObject.SetActive(isOn);
			}
		}

		public void ActiveTrackOnly(bool isOn)
		{
			if (!loaded || settings == null)
			{
				return;
			}

			settings.ActiveGround = isOn;
		}

		public void WindowTooltip(bool isOn)
		{
			if (!loaded || settings == null)
			{
				return;
			}

			settings.WindowTooltips = isOn;
		}

		public void LegendTooltip(bool isOn)
		{
			if (!loaded || settings == null)
			{
				return;
			}

			settings.LegendTooltips = isOn;
		}

		public void StockToolbar(bool isOn)
		{
			if (!loaded || settings == null)
			{
				return;
			}

			settings.StockToolbar = isOn;

			if (m_ToolbarMenuToggle != null)
			{
				m_ToolbarMenuToggle.gameObject.SetActive(isOn);
			}
		}

		public void ToolbarMenu(bool isOn)
		{
			if (!loaded || settings == null)
			{
				return;
			}

			settings.ToolbarMenu = isOn;
		}

		public void StockUIStlye(bool isOn)
		{
			if (!loaded || settings == null)
			{
				return;
			}

			settings.StockUIStyle = isOn;
		}

		public void DaylightCheck(bool isOn)
		{
			if (!loaded || settings == null)
			{
				return;
			}

			settings.DaylightCheck = isOn;
		}

		public void MechJebTargetSelection(bool isOn)
		{
			if (!loaded || settings == null)
			{
				return;
			}

			settings.MechJebTarget = isOn;

			if (m_MechJebLoadToggle != null)
			{
				m_MechJebLoadToggle.gameObject.SetActive(isOn);
			}
		}

		public void MechJebLoadTarget(bool isOn)
		{
			if (!loaded || settings == null)
			{
				return;
			}

			settings.MechJebLoad = isOn;
		}

		public void MapBudgetSlider(float index)
		{
			if (!loaded || settings == null)
			{
				return;
			}

			int ms = (int)stopValue(budgets, index);

			settings.MapGenBudget = ms;

			if (m_MapBudget != null)
			{
				m_MapBudget.OnTextUpdate.Invoke(budgetLabel(ms));
			}
		}

		public void ScanlineSlider(float index)
		{
			if (!loaded || settings == null)
			{
				return;
			}

			float speed = stopValue(scanlineSpeeds, index);

			settings.ScanlineSpeed = speed;

			if (m_Scanline != null)
			{
				m_Scanline.OnTextUpdate.Invoke(scanlineLabel(speed));
			}
		}

		private static float stopValue(float[] stops, float index)
		{
			return stops[Mathf.Clamp(Mathf.RoundToInt(index), 0, stops.Length - 1)];
		}

		private static int nearestStop(float[] stops, float value)
		{
			int index = 0;

			for (int i = 1; i < stops.Length; i++)
			{
				if (Mathf.Abs(stops[i] - value) < Mathf.Abs(stops[index] - value))
				{
					index = i;
				}
			}

			return index;
		}

		private static string budgetLabel(int ms)
		{
			return string.Format("Map Generation Budget: {0} ms/frame", ms);
		}

		private static string scanlineLabel(float speed)
		{
			if (speed <= 0)
			{
				return "Scanline Speed: Instant";
			}

			return string.Format("Scanline Speed: {0:0.##}x ({1:0.0} s)", speed, 1 / speed);
		}

		public void UISlider(float scale)
		{
			if (!loaded || m_UIScale == null)
			{
				return;
			}

			m_UIScale.OnTextUpdate.Invoke("UI Scale: " + (scale / 100).ToString("P0"));
		}

		public void SetUIScale()
		{
			if (settings == null || m_UIScaleSlider == null)
			{
				return;
			}

			settings.UIScale = m_UIScaleSlider.value / 100;
		}

		public void ResetWindows()
		{
			if (settings == null)
			{
				return;
			}

			settings.ResetWindows();
		}
	}
}
