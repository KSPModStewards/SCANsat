#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_ColorBiome - Script for controlling the biome color management UI
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
using SCANsat.Unity.HSVPicker.UI;

namespace SCANsat.Unity.Unity
{
	public class SCAN_ColorBiome : SettingsPage
	{
		[SerializeField]
		private SCAN_ColorPicker m_ColorPicker = null;
		[SerializeField]
		private Toggle m_BigMapColor = null;
		[SerializeField]
		private Toggle m_BigMapBorder = null;
		[SerializeField]
		private Toggle m_ZoomMapBorder = null;
		[SerializeField]
		private Toggle m_SmallMapColor = null;
		[SerializeField]
		private Toggle m_SmallMapBorder = null;
		[SerializeField]
		private Slider m_TransparencySlider = null;
		[SerializeField]
		private TextHandler m_TransparencyText = null;
		[SerializeField]
		private InputHandler m_TransInputField = null;

		private bool loaded;

		private ISCAN_Color colorInterface;
		private ISCAN_Settings settingsInterface;

		private void Awake()
		{
			if (m_TransInputField != null)
			{
				m_TransInputField.OnValueChange.AddListener(new UnityAction<string>(OnTransparencyInputChange));
			}
		}

		private void Update()
		{
			if (settingsInterface == null)
			{
				return;
			}

			if (settingsInterface.LockInput)
			{
				if (m_TransInputField != null && !m_TransInputField.IsFocused
					&& m_ColorPicker != null && !m_ColorPicker.AnyInputActive)
				{
					settingsInterface.LockInput = false;
				}
			}
		}

		public void SetBiome(ISCAN_Color color, ISCAN_Settings settings)
		{
			if (color == null)
			{
				return;
			}

			colorInterface = color;
			settingsInterface = settings;

			color.Refresh();

			if (m_BigMapColor != null)
			{
				m_BigMapColor.isOn = color.BiomeBigMapStockColor;
			}

			if (m_BigMapBorder != null)
			{
				m_BigMapBorder.isOn = color.BiomeBigMapWhiteBorder;
			}

			if (m_ZoomMapBorder != null)
			{
				m_ZoomMapBorder.isOn = color.BiomeZoomMapWhiteBorder;
			}

			if (m_SmallMapColor != null)
			{
				m_SmallMapColor.isOn = color.BiomeSmallMapStockColor;
			}

			if (m_SmallMapBorder != null)
			{
				m_SmallMapBorder.isOn = color.BiomeSmallMapWhiteBorder;
			}

			if (m_TransparencySlider != null)
			{
				m_TransparencySlider.value = color.BiomeTransparency;
			}

			if (m_ColorPicker != null)
			{
				m_ColorPicker.Setup(color.BiomeColorOne, color.BiomeColorTwo, true);
			}

			loaded = true;
		}

		public void OnInputClick(BaseEventData eventData)
		{
			if (!(eventData is PointerEventData) || settingsInterface == null)
			{
				return;
			}

			if (((PointerEventData)eventData).button != PointerEventData.InputButton.Left)
			{
				return;
			}

			settingsInterface.LockInput = true;
		}

		public void BigMapColor(bool isOn)
		{
			if (!loaded || colorInterface == null)
			{
				return;
			}

			colorInterface.BiomeBigMapStockColor = isOn;
		}

		public void BigMapBorder(bool isOn)
		{
			if (!loaded || colorInterface == null)
			{
				return;
			}

			colorInterface.BiomeBigMapWhiteBorder = isOn;
		}

		public void ZoomMapBorder(bool isOn)
		{
			if (!loaded || colorInterface == null)
			{
				return;
			}

			colorInterface.BiomeZoomMapWhiteBorder = isOn;
		}

		public void SmallMapColor(bool isOn)
		{
			if (!loaded || colorInterface == null)
			{
				return;
			}

			colorInterface.BiomeSmallMapStockColor = isOn;
		}

		public void SmallMapBorder(bool isOn)
		{
			if (!loaded || colorInterface == null)
			{
				return;
			}

			colorInterface.BiomeSmallMapWhiteBorder = isOn;
		}

		public void Transparency(float value)
		{
			if (m_TransparencyText != null)
			{
				m_TransparencyText.OnTextUpdate.Invoke(string.Format("Terrain Transparency: {0}%", value.ToString("N0")));
			}

			if (!loaded || colorInterface == null)
			{
				return;
			}

			colorInterface.BiomeTransparency = value;
		}

		public void OnTransparencyInputChange(string input)
		{
			if (m_TransparencySlider == null)
			{
				return;
			}

			float tran = SCAN_ColorControl.ParseInput(input, m_TransparencySlider.value, m_TransparencySlider.minValue, m_TransparencySlider.maxValue, 0);

			if (tran != m_TransparencySlider.value)
			{
				m_TransparencySlider.value = tran;
			}
		}

		public void Apply()
		{
			if (colorInterface == null || m_ColorPicker == null)
			{
				return;
			}

			colorInterface.BiomeApply(m_ColorPicker.GetColorOne, m_ColorPicker.GetColorTwo);

			if (m_ColorPicker != null)
			{
				m_ColorPicker.Setup(m_ColorPicker.GetColorOne, m_ColorPicker.GetColorTwo, false);
			}
		}

		public void Default()
		{
			if (colorInterface == null)
			{
				return;
			}

			colorInterface.BiomeDefault();

			if (m_ColorPicker != null)
			{
				m_ColorPicker.Setup(colorInterface.BiomeColorOne, colorInterface.BiomeColorTwo, false);
			}

			loaded = false;

			if (m_BigMapColor != null)
			{
				m_BigMapColor.isOn = colorInterface.BiomeBigMapStockColor;
			}

			if (m_BigMapBorder != null)
			{
				m_BigMapBorder.isOn = colorInterface.BiomeBigMapWhiteBorder;
			}

			if (m_SmallMapColor != null)
			{
				m_SmallMapColor.isOn = colorInterface.BiomeSmallMapStockColor;
			}

			if (m_SmallMapBorder != null)
			{
				m_SmallMapBorder.isOn = colorInterface.BiomeSmallMapWhiteBorder;
			}

			if (m_TransparencySlider != null)
			{
				m_TransparencySlider.value = colorInterface.BiomeTransparency;
			}

			loaded = true;
		}
	}
}
