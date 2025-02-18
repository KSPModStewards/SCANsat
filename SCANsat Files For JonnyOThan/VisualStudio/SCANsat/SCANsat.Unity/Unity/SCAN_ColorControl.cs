#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_ColorControl - Script for controlling the color management settings tab
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
	public class SCAN_ColorControl : SettingsPage
	{
		[SerializeField]
		private GameObject m_AltimetryPrefab = null;
		[SerializeField]
		private GameObject m_SlopePrefab = null;
		[SerializeField]
		private GameObject m_BiomePrefab = null;
		[SerializeField]
		private GameObject m_ResourcePrefab = null;
		[SerializeField]
		private Transform m_ContentTransform = null;

		private ISCAN_Settings settingsInterface;
		private ISCAN_Color colorInterface;

		private SettingsPage currentPage;

		public void setup(ISCAN_Settings settings, ISCAN_Color color)
		{
			if (settings == null || color == null)
				return;

			settingsInterface = settings;
			colorInterface = color;

			AltimetrySettings(true);
		}

		public override void OnPointerDown(PointerEventData eventData)
		{
			if (currentPage != null)
				currentPage.OnPointerDown(eventData);
		}

		public void AltimetrySettings(bool isOn)
		{
			if (!isOn)
				return;

			if (currentPage != null)
			{
				currentPage.gameObject.SetActive(false);
				DestroyImmediate(currentPage.gameObject);
			}

			if (m_ContentTransform == null || m_AltimetryPrefab == null || settingsInterface == null)
				return;

			if (settingsInterface.LockInput)
				settingsInterface.LockInput = false;

			currentPage = Instantiate(m_AltimetryPrefab).GetComponent<SettingsPage>();

			if (currentPage == null)
				return;

			currentPage.transform.SetParent(m_ContentTransform, false);

			((SCAN_ColorAltimetry)currentPage).SetTerrain(colorInterface, settingsInterface);

			if (SCAN_Settings.Instance != null)
				SCAN_Settings.Instance.ProcessTooltips();

			if (SCAN_Settings.Instance != null)
				SCAN_Settings.Instance.ClearWarningsAndDropDown();
		}

		public void SlopeSettings(bool isOn)
		{
			if (!isOn)
				return;

			if (currentPage != null)
			{
				currentPage.gameObject.SetActive(false);
				DestroyImmediate(currentPage.gameObject);
			}

			if (m_ContentTransform == null || m_SlopePrefab == null || settingsInterface == null)
				return;

			if (settingsInterface.LockInput)
				settingsInterface.LockInput = false;

			currentPage = Instantiate(m_SlopePrefab).GetComponent<SettingsPage>();

			if (currentPage == null)
				return;

			currentPage.transform.SetParent(m_ContentTransform, false);

			((SCAN_ColorSlope)currentPage).SetSlope(colorInterface, settingsInterface);

			if (SCAN_Settings.Instance != null)
				SCAN_Settings.Instance.ProcessTooltips();

			if (SCAN_Settings.Instance != null)
				SCAN_Settings.Instance.ClearWarningsAndDropDown();
		}

		public void BiomeSettings(bool isOn)
		{
			if (!isOn)
				return;

			if (currentPage != null)
			{
				currentPage.gameObject.SetActive(false);
				DestroyImmediate(currentPage.gameObject);
			}

			if (m_ContentTransform == null || m_BiomePrefab == null || settingsInterface == null)
				return;

			if (settingsInterface.LockInput)
				settingsInterface.LockInput = false;

			currentPage = Instantiate(m_BiomePrefab).GetComponent<SettingsPage>();

			if (currentPage == null)
				return;

			currentPage.transform.SetParent(m_ContentTransform, false);

			((SCAN_ColorBiome)currentPage).SetBiome(colorInterface, settingsInterface);

			if (SCAN_Settings.Instance != null)
				SCAN_Settings.Instance.ProcessTooltips();

			if (SCAN_Settings.Instance != null)
				SCAN_Settings.Instance.ClearWarningsAndDropDown();
		}

		public void ResourceSettings(bool isOn)
		{
			if (!isOn)
				return;

			if (currentPage != null)
			{
				currentPage.gameObject.SetActive(false);
				DestroyImmediate(currentPage.gameObject);
			}

			if (m_ContentTransform == null || m_ResourcePrefab == null || settingsInterface == null)
				return;

			if (settingsInterface.LockInput)
				settingsInterface.LockInput = false;

			currentPage = Instantiate(m_ResourcePrefab).GetComponent<SettingsPage>();

			if (currentPage == null)
				return;

			currentPage.transform.SetParent(m_ContentTransform, false);

			((SCAN_ColorResource)currentPage).SetResource(colorInterface, settingsInterface);

			if (SCAN_Settings.Instance != null)
				SCAN_Settings.Instance.ProcessTooltips();

			if (SCAN_Settings.Instance != null)
				SCAN_Settings.Instance.ClearWarningsAndDropDown();
		}

		public static float ParseInput(string input, float original, float min, float max, int digits)
		{
			float f = original;

			if (!float.TryParse(input, out f))
				return f;

			if (f < min)
				return original;
			else if (f > max)
				return original;

			f = (float)Math.Round(f, digits);

			return f;
		}

	}
}
