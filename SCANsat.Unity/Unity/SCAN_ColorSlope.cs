#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_ColorSlope - Script for controlling the slope color management UI
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
	public class SCAN_ColorSlope : SettingsPage
	{
		[SerializeField]
		private SCAN_ColorPicker m_ColorPickerOne = null;
		[SerializeField]
		private SCAN_ColorPicker m_ColorPickerTwo = null;
		[SerializeField]
		private Slider m_CutoffSlider = null;
		[SerializeField]
		private TextHandler m_CutoffText = null;
		[SerializeField]
		private InputHandler m_CutoffInputField = null;

		private bool loaded;

		private ISCAN_Color colorInterface;
		private ISCAN_Settings settingsInterface;

		private void Awake()
		{
			if (m_CutoffInputField != null)
			{
				m_CutoffInputField.OnValueChange.AddListener(new UnityAction<string>(OnCutoffInputChange));
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
				if (m_CutoffInputField != null && !m_CutoffInputField.IsFocused
					&& m_ColorPickerOne != null && !m_ColorPickerOne.AnyInputActive
					&& m_ColorPickerTwo != null && !m_ColorPickerTwo.AnyInputActive)
				{
					settingsInterface.LockInput = false;
				}
			}
		}

		public void SetSlope(ISCAN_Color color, ISCAN_Settings settings)
		{
			if (color == null)
			{
				return;
			}

			colorInterface = color;
			settingsInterface = settings;

			color.Refresh();

			if (m_CutoffSlider != null)
			{
				m_CutoffSlider.value = color.SlopeCutoff;
			}

			if (m_ColorPickerOne != null)
			{
				m_ColorPickerOne.Setup(color.SlopeColorOneLo, color.SlopeColorOneHi, true);
			}

			if (m_ColorPickerTwo != null)
			{
				m_ColorPickerTwo.Setup(color.SlopeColorTwoLo, color.SlopeColorTwoHi, true);
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

		public void Cutoff(float value)
		{
			if (m_CutoffText != null)
			{
				m_CutoffText.OnTextUpdate.Invoke(string.Format("Slope Cutoff: {0}", value.ToString("N1")));
			}

			if (!loaded || colorInterface == null)
			{
				return;
			}

			colorInterface.SlopeCutoff = value;
		}

		public void OnCutoffInputChange(string input)
		{
			if (m_CutoffSlider == null)
			{
				return;
			}

			float cutoff = SCAN_ColorControl.ParseInput(input, m_CutoffSlider.value, m_CutoffSlider.minValue, m_CutoffSlider.maxValue, 1);

			if (cutoff != m_CutoffSlider.value)
			{
				m_CutoffSlider.value = cutoff;
			}
		}

		public void Apply()
		{
			if (colorInterface == null || m_ColorPickerOne == null || m_ColorPickerTwo == null)
			{
				return;
			}

			colorInterface.SlopeApply(m_ColorPickerOne.GetColorOne, m_ColorPickerOne.GetColorTwo, m_ColorPickerTwo.GetColorOne, m_ColorPickerTwo.GetColorTwo);

			if (m_ColorPickerOne != null)
			{
				m_ColorPickerOne.Setup(m_ColorPickerOne.GetColorOne, m_ColorPickerOne.GetColorTwo, false);
			}

			if (m_ColorPickerTwo != null)
			{
				m_ColorPickerTwo.Setup(m_ColorPickerTwo.GetColorOne, m_ColorPickerTwo.GetColorTwo, false);
			}
		}

		public void Default()
		{
			if (colorInterface == null)
			{
				return;
			}

			colorInterface.SlopeDefault();

			if (m_ColorPickerOne != null)
			{
				m_ColorPickerOne.Setup(colorInterface.SlopeColorOneLo, colorInterface.SlopeColorOneHi, false);
			}

			if (m_ColorPickerTwo != null)
			{
				m_ColorPickerTwo.Setup(colorInterface.SlopeColorTwoLo, colorInterface.SlopeColorTwoHi, false);
			}
		}
	}
}
