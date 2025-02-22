#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_SettingsBackground - Script for controlling the background settings page
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
	public class SCAN_SettingsBackground : SettingsPage
	{
		[SerializeField]
		private GameObject m_BodyInfoPrefab = null;
		[SerializeField]
		private Transform m_BodyInfoTransform = null;
		[SerializeField]
		private SCAN_Toggle m_ScanActiveToggle = null;
		[SerializeField]
		private Slider m_TimeWarpResolution = null;
		[SerializeField]
		private TextHandler m_SensorInfo = null;

		private bool loaded;
		private ISCAN_Settings settings;
		private List<SCAN_BackgroundElement> backgroundBodies = new List<SCAN_BackgroundElement>();

		private void Update()
		{
			if (settings == null || !settings.IsVisible)
			{
				return;
			}

			for (int i = backgroundBodies.Count - 1; i >= 0; i--)
			{
				SCAN_BackgroundElement background = backgroundBodies[i];

				background.UpdateText(settings.BodyPercentage(background.BodyName));
			}

			if (m_SensorInfo != null)
			{
				m_SensorInfo.OnTextUpdate.Invoke(settings.SensorCount);
			}
		}

		public void setup(ISCAN_Settings set)
		{
			if (set == null)
			{
				return;
			}

			settings = set;

			if (m_ScanActiveToggle != null)
			{
				m_ScanActiveToggle.isOn = set.BackgroundScanning;
			}

			if (m_TimeWarpResolution != null)
			{
				m_TimeWarpResolution.value = set.TimeWarp;
			}

			CreateBodySections(set.BackgroundBodies);

			loaded = true;
		}

		private void CreateBodySections(IList<string> bodies)
		{
			if (bodies == null || settings == null || m_BodyInfoPrefab == null || m_BodyInfoTransform == null)
			{
				return;
			}

			for (int i = 0; i < bodies.Count; i++)
			{
				string s = bodies[i];

				if (string.IsNullOrEmpty(s))
				{
					continue;
				}

				CreateBodySection(s);
			}
		}

		private void CreateBodySection(string body)
		{
			SCAN_BackgroundElement background = Instantiate(m_BodyInfoPrefab).GetComponent<SCAN_BackgroundElement>();

			if (background == null)
			{
				return;
			}

			background.transform.SetParent(m_BodyInfoTransform, false);

			background.Setup(body, settings.ToggleBodyActive(body), settings);

			backgroundBodies.Add(background);
		}

		public void ScanToggle(bool isOn)
		{
			if (!loaded || settings == null)
			{
				return;
			}

			settings.BackgroundScanning = isOn;
		}

		public void TimeWarpSlider(float value)
		{
			if (!loaded || settings == null)
			{
				return;
			}

			settings.TimeWarp = (int)value;
		}

		public void UpdateScanners(string s)
		{
			if (settings == null || m_SensorInfo == null)
			{
				return;
			}

			m_SensorInfo.OnTextUpdate.Invoke(s);
		}

	}
}
