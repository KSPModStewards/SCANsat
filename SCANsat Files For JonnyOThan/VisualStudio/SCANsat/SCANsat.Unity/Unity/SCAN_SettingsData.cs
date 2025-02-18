#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_SettingsData - Script for controlling the data management settings page
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
using UnityEngine.Events;
using UnityEngine.EventSystems;
using SCANsat.Unity.Interfaces;

namespace SCANsat.Unity.Unity
{
	public class SCAN_SettingsData : SettingsPage
	{
		[SerializeField]
		private SCAN_Toggle m_GreyScaleToggle = null;
		[SerializeField]
		private SCAN_Toggle m_CSVExportToggle = null;
		[SerializeField]
		private TextHandler m_MapWidth = null;
		[SerializeField]
		private TextHandler m_ResetCurrentText = null;
		[SerializeField]
		private TextHandler m_ResetCurrentStockText = null;
		[SerializeField]
		private TextHandler m_FillCurrentText = null;
		[SerializeField]
		private TextHandler m_TypeText = null;
		[SerializeField]
		private InputHandler m_MapWidthInputHandler = null;
		[SerializeField]
		private GameObject m_ResetStockResource = null;
		[SerializeField]
		private GameObject m_MapFill = null;
		[SerializeField]
		private Transform m_MapTypeOption = null;

		private bool loaded;
		private ISCAN_Settings settings;

		private void Update()
		{
			if (settings == null)
				return;

			if (settings.LockInput)
			{
				if (m_MapWidthInputHandler != null && !m_MapWidthInputHandler.IsFocused)
					settings.LockInput = false;
			}
		}

		public void setup(ISCAN_Settings set)
		{
			if (set == null)
				return;

			settings = set;

			if (m_GreyScaleToggle != null)
				m_GreyScaleToggle.isOn = set.GreyScale;

			if (m_CSVExportToggle != null)
				m_CSVExportToggle.isOn = set.ExportCSV;

			if (m_MapWidth != null)
				m_MapWidth.OnTextUpdate.Invoke("Map Width: " + set.MapWidth.ToString());

			if (!set.ShowStockReset)
			{
				if (m_ResetStockResource != null)
					m_ResetStockResource.SetActive(false);
			}

			if (!set.ShowMapFill && m_MapFill != null)
				m_MapFill.SetActive(false);

			SetButtonText();

			loaded = true;
		}

		public void SetButtonText()
		{
			if (settings == null)
				return;

			if (m_ResetCurrentText != null)
				m_ResetCurrentText.OnTextUpdate.Invoke("Reset Map of " + settings.CurrentBody);

			if (m_ResetCurrentStockText != null)
				m_ResetCurrentStockText.OnTextUpdate.Invoke("Reset stock resource data for " + settings.CurrentBody);

			if (m_FillCurrentText != null)
				m_FillCurrentText.OnTextUpdate.Invoke("Fill map of " + settings.CurrentBody);

			if (m_TypeText != null)
				m_TypeText.OnTextUpdate.Invoke(settings.CurrentMapData);
		}

		public void GreyScale(bool isOn)
		{
			if (!loaded || settings == null)
				return;

			settings.GreyScale = isOn;
		}

		public void ExportCSV(bool isOn)
		{
			if (!loaded || settings == null)
				return;

			settings.ExportCSV = isOn;
		}

		public void SetMapWidth()
		{
			if (settings == null || m_MapWidthInputHandler == null)
				return;

			settings.LockInput = false;

			int width = settings.MapWidth;

			if (int.TryParse(m_MapWidthInputHandler.Text, out width))
			{
				if (width % 2 != 0)
					width += 1;

				if (width > 8192)
					width = 8192;
				else if (width < 560)
					width = 560;

				m_MapWidthInputHandler.OnTextUpdate.Invoke(width.ToString());

				settings.MapWidth = width;

				if (m_MapWidth != null)
					m_MapWidth.OnTextUpdate.Invoke("Map Width: " + width.ToString());
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

		public override void OnPointerDown(PointerEventData eventData)
		{
			if (SCAN_Settings.Instance == null)
				return;

			if (SCAN_Settings.Instance.DropDown != null)
			{
				RectTransform r = SCAN_Settings.Instance.DropDown.GetComponent<RectTransform>();

				if (r != null)
				{
					if (!RectTransformUtility.RectangleContainsScreenPoint(r, eventData.position, eventData.pressEventCamera))
					{
						SCAN_Settings.Instance.DropDown.FadeOut();
						SCAN_Settings.Instance.DropDown = null;
					}
				}
			}

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

		private void PopupPopup(string message, UnityAction callback)
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

			SCAN_Settings.Instance.WarningPopup.Setup(message);

			SCAN_Settings.Instance.WarningPopup.OnSelectUpdate.AddListener(callback);
		}

		public void ResetCurrentMap()
		{
			if (settings != null)
				PopupPopup(settings.DataResetCurrent, ConfirmResetCurrentMap);
		}

		private void ConfirmResetCurrentMap()
		{
			SCAN_Settings.Instance.WarningPopup.FadeOut(true);
			SCAN_Settings.Instance.WarningPopup = null;

			if (settings == null)
				return;

			settings.ResetCurrent();
		}

		public void ResetAllMaps()
		{
			if (settings != null)
				PopupPopup(settings.DataResetAll, ConfirmResetAllMaps);
		}

		private void ConfirmResetAllMaps()
		{
			SCAN_Settings.Instance.WarningPopup.FadeOut(true);
			SCAN_Settings.Instance.WarningPopup = null;

			if (settings == null)
				return;

			settings.ResetAll();
		}

		public void ResetStockResourceCurrent()
		{
			if (settings != null)
				PopupPopup(settings.StockResourceResetCurrent, ConfirmResetStockResourceCurrent);
		}

		private void ConfirmResetStockResourceCurrent()
		{
			SCAN_Settings.Instance.WarningPopup.FadeOut(true);
			SCAN_Settings.Instance.WarningPopup = null;

			if (settings == null)
				return;

			settings.ResetStockResourceCurrent();
		}

		public void ResetStockResourceAll()
		{
			if (settings != null)
				PopupPopup(settings.StockResourceResetAll, ConfirmResetStockResourceAll);
		}

		private void ConfirmResetStockResourceAll()
		{
			SCAN_Settings.Instance.WarningPopup.FadeOut(true);
			SCAN_Settings.Instance.WarningPopup = null;

			if (settings == null)
				return;

			settings.ResetStockResourceAll();
		}

		public void MapTypeDropDown(bool isOn)
		{
			if (SCAN_Settings.Instance.DropDown != null)
			{
				SCAN_Settings.Instance.DropDown.FadeOut(true);
				SCAN_Settings.Instance.DropDown = null;
			}

			if (!isOn)
				return;

			if (m_MapTypeOption == null || SCAN_Settings.Instance.DropDownPrefab == null || settings == null)
				return;

			SCAN_Settings.Instance.DropDown = Instantiate(SCAN_Settings.Instance.DropDownPrefab).GetComponent<SCAN_DropDown>();

			if (SCAN_Settings.Instance.DropDown == null)
				return;

			SCAN_Settings.Instance.DropDown.transform.SetParent(m_MapTypeOption, false);

			SCAN_Settings.Instance.DropDown.Setup(settings.MapDataTypes, settings.CurrentMapData, 12);

			SCAN_Settings.Instance.DropDown.OnSelectUpdate.AddListener(new UnityEngine.Events.UnityAction<string>(MapTypeOption));
		}

		public void MapTypeOption(string scanType)
		{
			if (m_TypeText != null)
				m_TypeText.OnTextUpdate.Invoke(scanType);

			SCAN_Settings.Instance.DropDown.FadeOut(true);
			SCAN_Settings.Instance.DropDown = null;

			if (settings == null)
				return;

			settings.CurrentMapData = scanType;
		}

		public void FillCurrentMap()
		{
			if (settings != null)
				PopupPopup(settings.WarningMapFillCurrent, ConfirmFillCurrentMap);
		}

		public void ConfirmFillCurrentMap()
		{
			SCAN_Settings.Instance.WarningPopup.FadeOut(true);
			SCAN_Settings.Instance.WarningPopup = null;

			if (settings == null)
				return;

			settings.FillCurrent();
		}

		public void FillAllMaps()
		{
			if (settings != null)
				PopupPopup(settings.WarningMapFillAll, ConfirmFillAllMaps);
		}

		public void ConfirmFillAllMaps()
		{
			SCAN_Settings.Instance.WarningPopup.FadeOut(true);
			SCAN_Settings.Instance.WarningPopup = null;

			if (settings == null)
				return;

			settings.FillAll();
		}
	}
}
