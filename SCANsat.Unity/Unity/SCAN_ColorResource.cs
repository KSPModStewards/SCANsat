#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_ColorResource - Script for controlling the resource color management UI
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
	public class SCAN_ColorResource : SettingsPage
	{
		[SerializeField]
		private SCAN_ColorPicker m_ColorPicker = null;
		[SerializeField]
		private Transform m_PlanetSelection = null;
		[SerializeField]
		private Transform m_ResourceSelection = null;
		[SerializeField]
		private ToggleGroup m_DropDownToggles = null;
		[SerializeField]
		private TextHandler m_PlanetName = null;
		[SerializeField]
		private TextHandler m_ResourceName = null;
		[SerializeField]
		private Slider m_MinSlider = null;
		[SerializeField]
		private Slider m_MaxSlider = null;
		[SerializeField]
		private Slider m_TransSlider = null;
		[SerializeField]
		private TextHandler m_MinSliderLabelTwo = null;
		[SerializeField]
		private TextHandler m_MaxSliderLabelOne = null;
		[SerializeField]
		private TextHandler m_MinText = null;
		[SerializeField]
		private TextHandler m_MaxText = null;
		[SerializeField]
		private TextHandler m_TransText = null;
		[SerializeField]
		private InputHandler m_MinInputField = null;
		[SerializeField]
		private InputHandler m_MaxInputField = null;
		[SerializeField]
		private InputHandler m_TransInputField = null;

		private ISCAN_Color colorInterface;
		private ISCAN_Settings settingsInterface;

		private void Awake()
		{
			if (m_MinInputField != null)
			{
				m_MinInputField.OnValueChange.AddListener(new UnityAction<string>(OnMinInputChange));
			}

			if (m_MaxInputField != null)
			{
				m_MaxInputField.OnValueChange.AddListener(new UnityAction<string>(OnMaxInputChange));
			}

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
				if (m_MinInputField != null && !m_MinInputField.IsFocused
					&& m_MaxInputField != null && !m_MaxInputField.IsFocused
					&& m_TransInputField != null && !m_TransInputField.IsFocused
					&& m_ColorPicker != null && !m_ColorPicker.AnyInputActive)
				{
					settingsInterface.LockInput = false;
				}
			}
		}

		public void SetResource(ISCAN_Color color, ISCAN_Settings settings)
		{
			if (color == null || settings == null)
			{
				return;
			}

			colorInterface = color;
			settingsInterface = settings;

			color.Refresh();

			SetUI();
		}

		private void SetUI(bool color = true)
		{
			if (colorInterface == null)
			{
				return;
			}

			SetMinSlider();

			SetMaxSlider();

			if (m_MinSlider != null)
			{
				m_MinSlider.value = colorInterface.ResourceMin;
			}

			if (m_MinText != null)
			{
				m_MinText.OnTextUpdate.Invoke(string.Format("Min: {0}%", colorInterface.ResourceMin.ToString("N2")));
			}

			if (m_MaxSlider != null)
			{
				m_MaxSlider.value = colorInterface.ResourceMax;
			}

			if (m_MaxText != null)
			{
				m_MaxText.OnTextUpdate.Invoke(string.Format("Max: {0}%", colorInterface.ResourceMax.ToString("N2")));
			}

			if (m_TransSlider != null)
			{
				m_TransSlider.value = colorInterface.ResourceTransparency;
			}

			if (m_ColorPicker != null && color)
			{
				m_ColorPicker.Setup(colorInterface.ResourceColorOne, colorInterface.ResourceColorTwo, true);
			}

			if (m_PlanetName != null)
			{
				m_PlanetName.OnTextUpdate.Invoke(colorInterface.ResourcePlanet);
			}

			if (m_ResourceName != null)
			{
				m_ResourceName.OnTextUpdate.Invoke(colorInterface.ResourceCurrent);
			}
		}

		public override void OnPointerDown(PointerEventData eventData)
		{
			if (SCAN_Settings.Instance == null)
			{
				return;
			}

			if (SCAN_Settings.Instance.DropDown != null)
			{
				RectTransform r = SCAN_Settings.Instance.DropDown.GetComponent<RectTransform>();

				if (r != null)
				{
					if (!RectTransformUtility.RectangleContainsScreenPoint(r, eventData.position, eventData.pressEventCamera))
					{
						SCAN_Settings.Instance.DropDown.FadeOut();
						SCAN_Settings.Instance.DropDown = null;

						if (m_DropDownToggles != null)
						{
							m_DropDownToggles.SetAllTogglesOff();
						}
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

		public void PlanetDropDown(bool isOn)
		{
			if (SCAN_Settings.Instance.DropDown != null)
			{
				SCAN_Settings.Instance.DropDown.FadeOut(true);
				SCAN_Settings.Instance.DropDown = null;
			}

			if (!isOn)
			{
				return;
			}

			if (m_PlanetSelection == null || SCAN_Settings.Instance.DropDownPrefab == null || colorInterface == null)
			{
				return;
			}

			SCAN_Settings.Instance.DropDown = Instantiate(SCAN_Settings.Instance.DropDownPrefab).GetComponent<SCAN_DropDown>();

			if (SCAN_Settings.Instance.DropDown == null)
			{
				return;
			}

			SCAN_Settings.Instance.DropDown.transform.SetParent(m_PlanetSelection, false);

			SCAN_Settings.Instance.DropDown.Setup(colorInterface.CelestialBodies, colorInterface.ResourcePlanet);

			SCAN_Settings.Instance.DropDown.OnSelectUpdate.AddListener(new UnityEngine.Events.UnityAction<string>(Planet));
		}

		public void Planet(string planet)
		{
			if (m_PlanetName != null)
			{
				m_PlanetName.OnTextUpdate.Invoke(planet);
			}

			SCAN_Settings.Instance.DropDown.FadeOut(true);
			SCAN_Settings.Instance.DropDown = null;

			if (m_DropDownToggles != null)
			{
				m_DropDownToggles.SetAllTogglesOff();
			}

			if (colorInterface == null)
			{
				return;
			}

			colorInterface.ResourcePlanet = planet;

			SetUI(false);
		}

		public void ResourceDropDown(bool isOn)
		{
			if (SCAN_Settings.Instance.DropDown != null)
			{
				SCAN_Settings.Instance.DropDown.FadeOut(true);
				SCAN_Settings.Instance.DropDown = null;
			}

			if (!isOn)
			{
				return;
			}

			if (m_ResourceSelection == null || SCAN_Settings.Instance.DropDownPrefab == null || colorInterface == null)
			{
				return;
			}

			SCAN_Settings.Instance.DropDown = Instantiate(SCAN_Settings.Instance.DropDownPrefab).GetComponent<SCAN_DropDown>();

			if (SCAN_Settings.Instance.DropDown == null)
			{
				return;
			}

			SCAN_Settings.Instance.DropDown.transform.SetParent(m_ResourceSelection, false);

			SCAN_Settings.Instance.DropDown.Setup(colorInterface.Resources, colorInterface.ResourceCurrent);

			SCAN_Settings.Instance.DropDown.OnSelectUpdate.AddListener(new UnityEngine.Events.UnityAction<string>(Resource));
		}

		public void Resource(string resource)
		{
			if (m_ResourceName != null)
			{
				m_ResourceName.OnTextUpdate.Invoke(resource);
			}

			SCAN_Settings.Instance.DropDown.FadeOut(true);
			SCAN_Settings.Instance.DropDown = null;

			if (m_DropDownToggles != null)
			{
				m_DropDownToggles.SetAllTogglesOff();
			}

			if (colorInterface == null)
			{
				return;
			}

			colorInterface.ResourceCurrent = resource;

			SetUI();
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

		public void OnMinChange(float value)
		{
			if (colorInterface == null)
			{
				return;
			}

			float max = colorInterface.ResourceMax - 1;

			if (value > max)
			{
				value = max;
			}
			else if (value < 0)
			{
				value = 0;
			}

			colorInterface.ResourceMin = value;

			SetMaxSlider();

			if (m_MinText != null)
			{
				m_MinText.OnTextUpdate.Invoke(string.Format("Min: {0}%", value.ToString("N2")));
			}
		}

		public void OnMinInputChange(string input)
		{
			if (m_MinSlider == null)
			{
				return;
			}

			float min = SCAN_ColorControl.ParseInput(input, m_MinSlider.value, m_MinSlider.minValue, m_MinSlider.maxValue, 2);

			if (min != m_MinSlider.value)
			{
				m_MinSlider.value = min;
			}
		}

		public void OnMaxChange(float value)
		{
			if (colorInterface == null)
			{
				return;
			}

			float min = colorInterface.ResourceMin + 1;

			if (value < min)
			{
				value = min;
			}
			else if (value > 100)
			{
				value = 100;
			}

			colorInterface.ResourceMax = value;

			SetMinSlider();

			if (m_MaxText != null)
			{
				m_MaxText.OnTextUpdate.Invoke(string.Format("Max: {0}%", value.ToString("N2")));
			}
		}

		public void OnMaxInputChange(string input)
		{
			if (m_MaxSlider == null)
			{
				return;
			}

			float max = SCAN_ColorControl.ParseInput(input, m_MaxSlider.value, m_MaxSlider.minValue, m_MaxSlider.maxValue, 2);

			if (max != m_MaxSlider.value)
			{
				m_MaxSlider.value = max;
			}
		}

		private void SetMinSlider()
		{
			if (colorInterface == null || m_MinSlider == null)
			{
				return;
			}

			float max = colorInterface.ResourceMax - 1;

			m_MinSlider.minValue = 0;
			m_MinSlider.maxValue = max;

			if (m_MinSliderLabelTwo != null)
			{
				m_MinSliderLabelTwo.OnTextUpdate.Invoke(string.Format("|\n{0}%", max.ToString("N1")));
			}
		}

		private void SetMaxSlider()
		{
			if (colorInterface == null || m_MaxSlider == null)
			{
				return;
			}

			float min = colorInterface.ResourceMin + 1;

			m_MaxSlider.minValue = min;
			m_MaxSlider.maxValue = 100;

			if (m_MaxSliderLabelOne != null)
			{
				m_MaxSliderLabelOne.OnTextUpdate.Invoke(string.Format("|\n{0}%", min.ToString("N1")));
			}
		}

		public void OnTransparencyChange(float value)
		{
			if (m_TransText != null)
			{
				m_TransText.OnTextUpdate.Invoke(string.Format("Trans: {0}%", value.ToString("N0")));
			}

			if (colorInterface == null)
			{
				return;
			}

			colorInterface.ResourceTransparency = value;
		}

		public void OnTransparencyInputChange(string input)
		{
			if (m_TransSlider == null)
			{
				return;
			}

			float tran = SCAN_ColorControl.ParseInput(input, m_TransSlider.value, m_TransSlider.minValue, m_TransSlider.maxValue, 0);

			if (tran != m_TransSlider.value)
			{
				m_TransSlider.value = tran;
			}
		}

		public void Apply()
		{
			if (colorInterface == null || m_ColorPicker == null)
			{
				return;
			}

			if (settingsInterface != null)
			{
				settingsInterface.LockInput = false;
			}

			colorInterface.ResourceApply(m_ColorPicker.GetColorOne, m_ColorPicker.GetColorTwo);

			if (m_ColorPicker != null)
			{
				m_ColorPicker.Setup(m_ColorPicker.GetColorOne, m_ColorPicker.GetColorTwo, false);
			}
		}

		public void ApplyToAll()
		{
			if (colorInterface == null || m_ColorPicker == null)
			{
				return;
			}

			if (settingsInterface != null)
			{
				settingsInterface.LockInput = false;
			}

			colorInterface.ResourceApplyToAll(m_ColorPicker.GetColorOne, m_ColorPicker.GetColorTwo);

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

			if (settingsInterface != null)
			{
				settingsInterface.LockInput = false;
			}

			colorInterface.ResourceDefault();

			if (m_ColorPicker != null)
			{
				m_ColorPicker.Setup(colorInterface.ResourceColorOne, colorInterface.ResourceColorTwo, false);
			}

			SetUI();
		}

		public void DefaultToAll()
		{
			if (colorInterface == null)
			{
				return;
			}

			if (settingsInterface != null)
			{
				settingsInterface.LockInput = false;
			}

			colorInterface.ResourceDefaultToAll();

			if (m_ColorPicker != null)
			{
				m_ColorPicker.Setup(colorInterface.ResourceColorOne, colorInterface.ResourceColorTwo, false);
			}

			SetUI();
		}

		public void SaveToConfig()
		{
			if (SCAN_Settings.Instance == null)
			{
				return;
			}

			if (SCAN_Settings.Instance.WarningPopup != null)
			{
				SCAN_Settings.Instance.WarningPopup.FadeOut(true);
				SCAN_Settings.Instance.WarningPopup = null;
			}

			if (SCAN_Settings.Instance.PopupPrefab == null)
			{
				return;
			}

			SCAN_Settings.Instance.WarningPopup = Instantiate(SCAN_Settings.Instance.PopupPrefab).GetComponent<SCAN_Popup>();

			if (SCAN_Settings.Instance.WarningPopup == null)
			{
				return;
			}

			SCAN_Settings.Instance.WarningPopup.transform.SetParent(transform, false);

			SCAN_Settings.Instance.WarningPopup.Setup(settingsInterface.SaveToConfig);

			SCAN_Settings.Instance.WarningPopup.OnSelectUpdate.AddListener(ConfirmSaveToConfig);
		}

		private void ConfirmSaveToConfig()
		{
			SCAN_Settings.Instance.WarningPopup.FadeOut(true);
			SCAN_Settings.Instance.WarningPopup = null;

			if (colorInterface == null || m_ColorPicker == null)
			{
				return;
			}

			if (settingsInterface != null)
			{
				settingsInterface.LockInput = false;
			}

			colorInterface.ResourceSaveToConfig(m_ColorPicker.GetColorOne, m_ColorPicker.GetColorTwo);

			if (m_ColorPicker != null)
			{
				m_ColorPicker.Setup(m_ColorPicker.GetColorOne, m_ColorPicker.GetColorTwo, false);
			}
		}
	}
}
