#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_SettingsResource - Script for controlling the resource settings page
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
	public class SCAN_SettingsResource : SettingsPage
	{
		[SerializeField]
		private SCAN_Toggle m_BiomeLockToggle = null;
		[SerializeField]
		private SCAN_Toggle m_NarrowBandToggle = null;
		[SerializeField]
		private SCAN_Toggle m_InstantScanToggle = null;
		[SerializeField]
		private SCAN_Toggle m_DisableStockToggle = null;
		[SerializeField]
		private GameObject m_StockThresholdObject = null;
		[SerializeField]
		private SCAN_Toggle m_StockThresholdToggle = null;
		[SerializeField]
		private TextHandler m_StockThresholdValue = null;
		[SerializeField]
		private InputHandler m_ThresholdInput = null;
		[SerializeField]
		private SCAN_Toggle m_OverlayTooltipToggle = null;
		[SerializeField]
		private TextHandler m_MapInterpolation = null;
		[SerializeField]
		private TextHandler m_MapHeight = null;
		[SerializeField]
		private TextHandler m_CoverageTransparency = null;
		[SerializeField]
		private TextHandler m_BiomeMapHeight = null;

		private bool loaded;
		private bool ignoreWarning;
		private ISCAN_Settings settings;

		private void Update()
		{
			if (settings == null)
				return;

			if (settings.LockInput)
			{
				if (m_ThresholdInput != null && !m_ThresholdInput.IsFocused)
					settings.LockInput = false;
			}
		}

		public void setup(ISCAN_Settings set)
		{
			if (set == null)
				return;

			settings = set;

			if (m_BiomeLockToggle != null)
				m_BiomeLockToggle.isOn = set.BiomeLock;

			if (m_NarrowBandToggle != null)
				m_NarrowBandToggle.isOn = set.NarrowBand;

			if (m_DisableStockToggle != null)
				m_DisableStockToggle.isOn = set.DisableStock;

			if (m_InstantScanToggle != null)
			{
				m_InstantScanToggle.isOn = set.InstantScan;
				m_InstantScanToggle.gameObject.SetActive(!set.DisableStock);
			}

			if (m_StockThresholdObject != null)
				m_StockThresholdObject.gameObject.SetActive(set.DisableStock);

			if (m_StockThresholdToggle != null)
				m_StockThresholdToggle.isOn = set.StockThreshold;

			if (m_StockThresholdValue != null)
				m_StockThresholdValue.OnTextUpdate.Invoke("Stock Scan Threshold: " + set.StockThresholdValue.ToString("P0"));

			if (set.DisableStock)
			{
				if (m_InstantScanToggle != null)
					m_InstantScanToggle.gameObject.SetActive(false);
			}
			else
			{
				if (m_StockThresholdObject != null)
					m_StockThresholdObject.gameObject.SetActive(false);
			}

			if (m_OverlayTooltipToggle != null)
				m_OverlayTooltipToggle.isOn = set.OverlayTooltips;

			if (m_MapInterpolation != null)
				m_MapInterpolation.OnTextUpdate.Invoke(set.Interpolation.ToString());

			if (m_MapHeight != null)
				m_MapHeight.OnTextUpdate.Invoke(set.MapHeight.ToString());

			if (m_CoverageTransparency != null)
				m_CoverageTransparency.OnTextUpdate.Invoke(set.Transparency.ToString("P0"));

			if (m_BiomeMapHeight != null)
				m_BiomeMapHeight.OnTextUpdate.Invoke(set.BiomeMapHeight.ToString());


			loaded = true;
		}

		public override void OnPointerDown(PointerEventData eventData)
		{
			if (SCAN_Settings.Instance == null)
				return;

			if (SCAN_Settings.Instance.WarningPopup != null)
			{
				RectTransform r = SCAN_Settings.Instance.WarningPopup.GetComponent<RectTransform>();

				if (r != null)
				{
					if (!RectTransformUtility.RectangleContainsScreenPoint(r, eventData.position, eventData.pressEventCamera))
					{
						SCAN_Settings.Instance.WarningPopup.FadeOut();
						SCAN_Settings.Instance.WarningPopup = null;
					}
				}
			}
		}

		public void BimomeLock(bool isOn)
		{
			if (!loaded || settings == null)
				return;

			settings.BiomeLock = isOn;
		}

		public void RequireNarrowBand(bool isOn)
		{
			if (!loaded || settings == null)
				return;

			settings.NarrowBand = isOn;
		}

		public void DisableStock(bool isOn)
		{
			if (!loaded || settings == null)
				return;

			if (isOn && !settings.ModuleManager && !ignoreWarning)
			{
				if (SCAN_Settings.Instance == null)
					return;

				if (SCAN_Settings.Instance.WarningPopup != null)
				{
					SCAN_Settings.Instance.WarningPopup.FadeOut(true);
					SCAN_Settings.Instance.WarningPopup = null;
				}

				if (SCAN_Settings.Instance.PopupPrefab == null)
					return;

				SCAN_Settings.Instance.WarningPopup = Instantiate(SCAN_Settings.Instance.PopupPrefab).GetComponent<SCAN_Popup>();

				if (SCAN_Settings.Instance.WarningPopup == null)
					return;

				SCAN_Settings.Instance.WarningPopup.transform.SetParent(transform, false);

				SCAN_Settings.Instance.WarningPopup.Setup(settings.ModuleManagerWarning);

				SCAN_Settings.Instance.WarningPopup.OnSelectUpdate.AddListener(ConfirmStockDisable);

				return;
			}

			settings.DisableStock = isOn;

			if (m_InstantScanToggle != null)
				m_InstantScanToggle.gameObject.SetActive(!isOn);

			if (m_StockThresholdObject != null)
				m_StockThresholdObject.gameObject.SetActive(isOn);
		}

		private void ConfirmStockDisable()
		{
			if (settings == null)
				return;

			ignoreWarning = true;

			settings.DisableStock = true;

			if (m_InstantScanToggle != null)
				m_InstantScanToggle.gameObject.SetActive(false);

			if (m_StockThresholdObject != null)
				m_StockThresholdObject.gameObject.SetActive(true);
		}

		public void InstantScan(bool isOn)
		{
			if (!loaded || settings == null)
				return;

			settings.InstantScan = isOn;
		}

		public void StockTreshold(bool isOn)
		{
			if (!loaded || settings == null)
				return;

			settings.LockInput = false;

			settings.StockThreshold = isOn;
		}

		public void SetThreshold()
		{
			if (m_ThresholdInput == null || settings == null)
				return;

			settings.LockInput = false;

			float value = settings.StockThresholdValue;

			if (float.TryParse(m_ThresholdInput.Text, out value))
			{
				value /= 100;

				if (value < 0)
					value = 0;
				else if (value > 1)
					value = 1;

				m_ThresholdInput.OnTextUpdate.Invoke((value * 100).ToString("N0"));

				settings.StockThresholdValue = value;

				if (m_StockThresholdValue != null)
					m_StockThresholdValue.OnTextUpdate.Invoke("Stock Scan Threshold: " + value.ToString("P0"));
			}
		}

		public void OnInputClick(BaseEventData eventData)
		{
			if (!(eventData is PointerEventData) || settings == null)
				return;

			if (((PointerEventData)eventData).button != PointerEventData.InputButton.Left)
				return;

			settings.LockInput = true;
		}

		public void OverlayTooltip(bool isOn)
		{
			if (!loaded || settings == null)
				return;

			settings.OverlayTooltips = isOn;
		}

		public void InterpolationDown()
		{
			if (settings == null)
				return;

			settings.Interpolation = Math.Max(2, settings.Interpolation / 2);

			if (m_MapInterpolation != null)
				m_MapInterpolation.OnTextUpdate.Invoke(settings.Interpolation.ToString());
		}

		public void InterpolationUp()
		{
			if (settings == null)
				return;

			settings.Interpolation = Math.Min(32, settings.Interpolation * 2);

			if (m_MapInterpolation != null)
				m_MapInterpolation.OnTextUpdate.Invoke(settings.Interpolation.ToString());
		}

		public void MapHeightDown()
		{
			if (settings == null)
				return;

			settings.MapHeight = Math.Max(64, settings.MapHeight / 2);

			if (m_MapHeight != null)
				m_MapHeight.OnTextUpdate.Invoke(settings.MapHeight.ToString());
		}

		public void MapHeightUp()
		{
			if (settings == null)
				return;

			settings.MapHeight = Math.Min(1024, settings.MapHeight * 2);

			if (m_MapHeight != null)
				m_MapHeight.OnTextUpdate.Invoke(settings.MapHeight.ToString());
		}

		public void TransparencyDown()
		{
			if (settings == null)
				return;

			settings.Transparency = Mathf.Max(0f, settings.Transparency - 0.1f);

			if (m_CoverageTransparency != null)
				m_CoverageTransparency.OnTextUpdate.Invoke(settings.Transparency.ToString("P0"));
		}

		public void TransparencyUp()
		{
			if (settings == null)
				return;

			settings.Transparency = Mathf.Min(1f, settings.Transparency + 0.1f);

			if (m_CoverageTransparency != null)
				m_CoverageTransparency.OnTextUpdate.Invoke(settings.Transparency.ToString("P0"));
		}

		public void BiomeMapHeightDown()
		{
			if (settings == null)
				return;

			settings.BiomeMapHeight = Math.Max(256, settings.BiomeMapHeight / 2);

			if (m_BiomeMapHeight != null)
				m_BiomeMapHeight.OnTextUpdate.Invoke(settings.BiomeMapHeight.ToString());
		}

		public void BiomeMapHeightUp()
		{
			if (settings == null)
				return;

			settings.BiomeMapHeight = Math.Min(1024, settings.BiomeMapHeight * 2);

			if (m_BiomeMapHeight != null)
				m_BiomeMapHeight.OnTextUpdate.Invoke(settings.BiomeMapHeight.ToString());
		}

	}
}
