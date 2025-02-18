#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_ColorAltimetry - Script for controlling the altimetry color management UI
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
	public class SCAN_ColorAltimetry : SettingsPage
	{
		[SerializeField]
		private ToggleGroup m_DropDownToggles = null;
		[SerializeField]
		private GameObject m_PalettePrefab = null;
		[SerializeField]
		private Transform m_PaletteGrid = null;
		[SerializeField]
		private Transform m_PaletteSelection = null;
		[SerializeField]
		private TextHandler m_PaletteName = null;
		[SerializeField]
		private RawImage m_PaletteOld = null;
		[SerializeField]
		private RawImage m_PalettePreview = null;
		[SerializeField]
		private Transform m_PlanetSelection = null;
		[SerializeField]
		private TextHandler m_PlanetName = null;
		[SerializeField]
		private Slider m_MinSlider = null;
		[SerializeField]
		private Slider m_MaxSlider = null;
		[SerializeField]
		private Slider m_ClampSlider = null;
		[SerializeField]
		private Slider m_SizeSlider = null;
		[SerializeField]
		private GameObject m_ClampObject = null;
		[SerializeField]
		private GameObject m_SizeObject = null;
		[SerializeField]
		private TextHandler m_MinSliderLabelOne = null;
		[SerializeField]
		private TextHandler m_MinSliderLabelTwo = null;
		[SerializeField]
		private TextHandler m_MaxSliderLabelOne = null;
		[SerializeField]
		private TextHandler m_MaxSliderLabelTwo = null;
		[SerializeField]
		private TextHandler m_ClampSliderLabelOne = null;
		[SerializeField]
		private TextHandler m_ClampSliderLabelTwo = null;
		[SerializeField]
		private TextHandler m_SizeSliderLabelOne = null;
		[SerializeField]
		private TextHandler m_SizeSliderLabelTwo = null;
		[SerializeField]
		private TextHandler m_MinText = null;
		[SerializeField]
		private TextHandler m_MaxText = null;
		[SerializeField]
		private TextHandler m_ClampText = null;
		[SerializeField]
		private TextHandler m_SizeText = null;
		[SerializeField]
		private InputHandler m_MinInputField = null;
		[SerializeField]
		private InputHandler m_MaxInputField = null;
		[SerializeField]
		private InputHandler m_ClampInputField = null;
		[SerializeField]
		private Toggle m_ClampToggle = null;
		[SerializeField]
		private Toggle m_ReverseToggle = null;
		[SerializeField]
		private Toggle m_DiscreteToggle = null;
		
		private bool loaded;

		private ISCAN_Color colorInterface;
		private ISCAN_Settings settingsInterface;

		private List<SCAN_PaletteButton> paletteButtons = new List<SCAN_PaletteButton>();
		
		private void Awake()
		{
			if (m_MinInputField != null)
				m_MinInputField.OnValueChange.AddListener(new UnityAction<string>(OnMinInputChange));

			if (m_MaxInputField != null)
				m_MaxInputField.OnValueChange.AddListener(new UnityAction<string>(OnMaxInputChange));

			if (m_ClampInputField != null)
				m_ClampInputField.OnValueChange.AddListener(new UnityAction<string>(OnClampInputChange));
		}

		private void Update()
		{
			if (settingsInterface == null)
				return;

			if (settingsInterface.LockInput)
			{
				if (m_MinInputField != null && !m_MinInputField.IsFocused
					&& m_MaxInputField != null && !m_MaxInputField.IsFocused
					&& m_ClampInputField != null && !m_ClampInputField.IsFocused)
					settingsInterface.LockInput = false;
			}
		}
	
		public void SetTerrain(ISCAN_Color color, ISCAN_Settings settings)
		{
			if (color == null || settings == null)
				return;

			colorInterface = color;
			settingsInterface = settings;

			color.Refresh();

			SetUI();
		}

		private void SetUI()
		{
			if (colorInterface == null)
				return;

			SetMinSlider();

			SetMaxSlider();

			if (m_MinSlider != null)
				m_MinSlider.value = colorInterface.TerrainCurrentMin;

			if (m_MaxSlider != null)
				m_MaxSlider.value = colorInterface.TerrainCurrentMax;

			if (colorInterface.TerrainClampOn && m_ClampSlider != null)
				m_ClampSlider.value = colorInterface.TerrainClamp;

			if (m_ClampToggle != null)
				m_ClampToggle.isOn = colorInterface.TerrainClampOn;

			if (m_ReverseToggle != null)
				m_ReverseToggle.isOn = colorInterface.TerrainReverse;

			if (m_DiscreteToggle != null)
				m_DiscreteToggle.isOn = colorInterface.TerrainDiscrete;

			if (m_PaletteName != null)
				m_PaletteName.OnTextUpdate.Invoke(colorInterface.TerrainPaletteStyle);

			if (m_PlanetName != null)
				m_PlanetName.OnTextUpdate.Invoke(colorInterface.TerrainPlanet);

			if (m_PaletteName != null)
				m_PaletteName.OnTextUpdate.Invoke(colorInterface.TerrainPaletteStyle);

			CreatePalettes(colorInterface.TerrainPalettes);

			SetPalettePreviews();

			SetSizeSlider();

			loaded = true;
		}

		public void OnInputClick(BaseEventData eventData)
		{
			if (!(eventData is PointerEventData) || settingsInterface == null)
				return;

			if (((PointerEventData)eventData).button != PointerEventData.InputButton.Left)
				return;

			settingsInterface.LockInput = true;
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
						
						if (m_DropDownToggles != null)
							m_DropDownToggles.SetAllTogglesOff();
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

		public void PaletteStyleDropDown(bool isOn)
		{
			if (SCAN_Settings.Instance.DropDown != null)
			{
				SCAN_Settings.Instance.DropDown.FadeOut(true);
				SCAN_Settings.Instance.DropDown = null;
			}

			if (!isOn)
				return;

			if (m_PaletteSelection == null || SCAN_Settings.Instance.DropDownPrefab == null || colorInterface == null)
				return;

			SCAN_Settings.Instance.DropDown = Instantiate(SCAN_Settings.Instance.DropDownPrefab).GetComponent<SCAN_DropDown>();

			if (SCAN_Settings.Instance.DropDown == null)
				return;

			SCAN_Settings.Instance.DropDown.transform.SetParent(m_PaletteSelection, false);

			SCAN_Settings.Instance.DropDown.Setup(colorInterface.PaletteStyleNames, colorInterface.TerrainPaletteStyle);

			SCAN_Settings.Instance.DropDown.OnSelectUpdate.AddListener(new UnityEngine.Events.UnityAction<string>(PaletteStyle));
		}

		public void PaletteStyle(string style)
		{
			if (m_PaletteName != null)
				m_PaletteName.OnTextUpdate.Invoke(style);

			SCAN_Settings.Instance.DropDown.FadeOut(true);
			SCAN_Settings.Instance.DropDown = null;

			if (m_DropDownToggles != null)
				m_DropDownToggles.SetAllTogglesOff();

			if (colorInterface == null)
				return;

			colorInterface.TerrainPaletteStyle = style;

			ClearPalettes();

			CreatePalettes(colorInterface.TerrainPalettes);

			SetPalettePreviews();

			SetSizeSlider();
		}

		private void ClearPalettes()
		{
			for (int i = paletteButtons.Count - 1; i >= 0; i--)
			{
				SCAN_PaletteButton button = paletteButtons[i];

				button.gameObject.SetActive(false);
				DestroyImmediate(button.gameObject);
			}

			paletteButtons.Clear();
		}

		private void CreatePalettes(IList<KeyValuePair<string, Texture2D>> palettes)
		{
			if (colorInterface == null || m_PaletteGrid == null || m_PalettePrefab == null || palettes == null)
				return;

			for (int i = 0; i < palettes.Count; i++)
			{
				KeyValuePair<string, Texture2D> palette = palettes[i];

				CreatePalette(palette);
			}
		}

		private void CreatePalette(KeyValuePair<string, Texture2D> palette)
		{
			SCAN_PaletteButton button = Instantiate(m_PalettePrefab).GetComponent<SCAN_PaletteButton>();

			if (button == null)
				return;

			button.transform.SetParent(m_PaletteGrid, false);

			button.setup(palette.Value, palette.Key, this);

			paletteButtons.Add(button);
		}

		public void SetPalette(string palette)
		{
			if (string.IsNullOrEmpty(palette) || colorInterface == null)
				return;

			colorInterface.TerrainPalette = palette;

			SetPalettePreviews();

			SetSizeSlider();
		}

		private void SetPalettePreviews()
		{
			if (colorInterface == null)
				return;

			if (m_PaletteOld != null)
				m_PaletteOld.texture = colorInterface.TerrainPaletteOld;

			if (m_PalettePreview != null)
				m_PalettePreview.texture = colorInterface.TerrainPaletteNew;
		}

		public void PlanetDropDown(bool isOn)
		{
			if (SCAN_Settings.Instance.DropDown != null)
			{
				SCAN_Settings.Instance.DropDown.FadeOut(true);
				SCAN_Settings.Instance.DropDown = null;
			}

			if (!isOn)
				return;

			if (m_PlanetSelection == null || SCAN_Settings.Instance.DropDownPrefab == null || colorInterface == null)
				return;

			SCAN_Settings.Instance.DropDown = Instantiate(SCAN_Settings.Instance.DropDownPrefab).GetComponent<SCAN_DropDown>();

			if (SCAN_Settings.Instance.DropDown == null)
				return;

			SCAN_Settings.Instance.DropDown.transform.SetParent(m_PlanetSelection, false);

			SCAN_Settings.Instance.DropDown.Setup(colorInterface.CelestialBodies, colorInterface.TerrainPlanet);

			SCAN_Settings.Instance.DropDown.OnSelectUpdate.AddListener(new UnityEngine.Events.UnityAction<string>(Planet));
		}

		public void Planet(string planet)
		{
			SCAN_Settings.Instance.DropDown.FadeOut(true);
			SCAN_Settings.Instance.DropDown = null;

			if (m_DropDownToggles != null)
				m_DropDownToggles.SetAllTogglesOff();

			if (colorInterface == null)
				return;

			colorInterface.TerrainPlanet = planet;

			loaded = false;

			ClearPalettes();

			SetUI();
		}

		public void OnMinChange(float value)
		{
			if (colorInterface == null)
				return;

			float max = (((int)(colorInterface.TerrainCurrentMax * 100)) / 100) - 200;

			if (value > max)
				value = max;
			else if (value < colorInterface.TerrainGlobalMin)
				value = colorInterface.TerrainGlobalMin;

			colorInterface.TerrainCurrentMin = value;

			SetMaxSlider();

			if (m_MinText != null)
				m_MinText.OnTextUpdate.Invoke(string.Format("Min: {0:N0}m", value));

			if (colorInterface.TerrainClampOn)
				SetClamp();
		}

		public void OnMinInputChange(string input)
		{
			if (m_MinSlider == null)
				return;

			float min = SCAN_ColorControl.ParseInput(input, m_MinSlider.value, m_MinSlider.minValue, m_MinSlider.maxValue, 0);

			if (min != m_MinSlider.value)
				m_MinSlider.value = min;
		}

		public void OnMaxChange(float value)
		{
			if (colorInterface == null)
				return;

			float min = (((int)(colorInterface.TerrainCurrentMin * 100)) / 100) + 200;

			if (value < min)
				value = min;
			else if (value > colorInterface.TerrainGlobalMax)
				value = colorInterface.TerrainGlobalMax;

			colorInterface.TerrainCurrentMax = value;

			SetMinSlider();

			if (m_MaxText != null)
				m_MaxText.OnTextUpdate.Invoke(string.Format("Max: {0:N0}m", value));

			if (colorInterface.TerrainClampOn)
				SetClamp();
		}

		public void OnMaxInputChange(string input)
		{
			if (m_MaxSlider == null)
				return;

			float max = SCAN_ColorControl.ParseInput(input, m_MaxSlider.value, m_MaxSlider.minValue, m_MaxSlider.maxValue, 0);

			if (max != m_MaxSlider.value)
				m_MaxSlider.value = max;
		}

		public void OnClampChange(float value)
		{
			if (colorInterface == null)
				return;

			if (m_ClampText != null)
				m_ClampText.OnTextUpdate.Invoke(string.Format("Clamp: {0:N0}m", value));

			if (!loaded)
				return;

			colorInterface.TerrainClamp = value;
		}

		public void OnClampInputChange(string input)
		{
			if (m_ClampSlider == null)
				return;

			float clamp = SCAN_ColorControl.ParseInput(input, m_ClampSlider.value, m_ClampSlider.minValue, m_ClampSlider.maxValue, 0);

			if (clamp != m_ClampSlider.value)
				m_ClampSlider.value = clamp;
		}

		public void ClampToggle(bool isOn)
		{
			if (m_ClampObject != null)
				m_ClampObject.SetActive(isOn);

			if (isOn)
				SetClamp();

			if (!loaded || colorInterface == null)
				return;

			colorInterface.TerrainClampOn = isOn;

			SetPalettePreviews();
		}

		private void SetMinSlider()
		{
			if (colorInterface == null || m_MinSlider == null)
				return;

			float min = (((int)(colorInterface.TerrainGlobalMin * 100)) / 100);

			float max = (((int)(colorInterface.TerrainCurrentMax * 100)) / 100) - 200;

			m_MinSlider.minValue = min;
			m_MinSlider.maxValue = max;

			if (m_MinSliderLabelOne != null)
				m_MinSliderLabelOne.OnTextUpdate.Invoke(string.Format("|\n{0:N0}m", min));

			if (m_MinSliderLabelTwo != null)
				m_MinSliderLabelTwo.OnTextUpdate.Invoke(string.Format("|\n{0:N0}m", max));
		}

		private void SetMaxSlider()
		{
			if (colorInterface == null || m_MaxSlider == null)
				return;

			float min = (((int)(colorInterface.TerrainCurrentMin * 100)) / 100) + 200;

			float max = (((int)(colorInterface.TerrainGlobalMax * 100)) / 100);

			m_MaxSlider.minValue = min;
			m_MaxSlider.maxValue = max;

			if (m_MaxSliderLabelOne != null)
				m_MaxSliderLabelOne.OnTextUpdate.Invoke(string.Format("|\n{0:N0}m", min));

			if (m_MaxSliderLabelTwo != null)
				m_MaxSliderLabelTwo.OnTextUpdate.Invoke(string.Format("|\n{0:N0}m", max));
		}

		private void SetClamp()
		{
			if (colorInterface == null || m_ClampSlider == null)
				return;

			float min = (((int)(colorInterface.TerrainCurrentMin * 100)) / 100) + 100;

			float max = (((int)(colorInterface.TerrainCurrentMax * 100)) / 100) - 100;

			m_ClampSlider.minValue = min;
			m_ClampSlider.maxValue = max;

			if (m_ClampSliderLabelOne != null)
				m_ClampSliderLabelOne.OnTextUpdate.Invoke(string.Format("|\n{0:N0}m", min));

			if (m_ClampSliderLabelTwo != null)
				m_ClampSliderLabelTwo.OnTextUpdate.Invoke(string.Format("|\n{0:N0}m", max));

			float clamp = colorInterface.TerrainClamp;

			if (clamp <= min)
				clamp = min;
			else if (clamp >= max)
				clamp = max;

			m_ClampSlider.value = clamp;
		}

		private void SetSizeSlider()
		{
			if (colorInterface == null || m_SizeSlider == null)
				return;

			if (m_SizeObject != null)
				m_SizeObject.SetActive(colorInterface.TerrainHasSize);

			if (!colorInterface.TerrainHasSize)
				return;

			int min = colorInterface.TerrainSizeMin;
			int max = colorInterface.TerrainSizeMax;

			m_SizeSlider.minValue = min;
			m_SizeSlider.maxValue = max;

			if (m_SizeSliderLabelOne != null)
				m_SizeSliderLabelOne.OnTextUpdate.Invoke(string.Format("|\n{0}", min));

			if (m_SizeSliderLabelTwo != null)
				m_SizeSliderLabelTwo.OnTextUpdate.Invoke(string.Format("|\n{0}", max));

			int size = colorInterface.TerrainSize;

			if (size < min)
				size = min;
			else if (size > max)
				size = max;

			m_SizeSlider.value = size;
		}

		public void OnSizeChange(float value)
		{
			if (m_SizeText != null)
				m_SizeText.OnTextUpdate.Invoke(string.Format("Size: {0}", (int)value));

			if (!loaded || colorInterface == null)
				return;

			colorInterface.TerrainSize = (int)value;
			
			ClearPalettes();

			CreatePalettes(colorInterface.TerrainPalettes);

			SetPalettePreviews();

			SetSizeSlider();
		}

		public void ReverseToggle(bool isOn)
		{
			if (!loaded || colorInterface == null)
				return;

			colorInterface.TerrainReverse = isOn;

			SetPalettePreviews();
		}

		public void DiscreteToggle(bool isOn)
		{
			if (!loaded || colorInterface == null)
				return;

			colorInterface.TerrainDiscrete = isOn;

			SetPalettePreviews();
		}

		public void Apply()
		{
			if (colorInterface == null)
				return;

			if (settingsInterface != null)
				settingsInterface.LockInput = false;

			colorInterface.TerrainApply();
		}

		public void Default()
		{
			if (colorInterface == null)
				return;

			if (settingsInterface != null)
				settingsInterface.LockInput = false;

			colorInterface.TerrainDefault();

			loaded = false;

			ClearPalettes();

			SetUI();
		}

		public void SaveToConfig()
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

			SCAN_Settings.Instance.WarningPopup.Setup(settingsInterface.SaveToConfig);

			SCAN_Settings.Instance.WarningPopup.OnSelectUpdate.AddListener(ConfirmSaveToConfig);
		}

		private void ConfirmSaveToConfig()
		{
			if (colorInterface == null)
				return;

			if (settingsInterface != null)
				settingsInterface.LockInput = false;

			colorInterface.TerrainSaveToConfig();
		}
	}
}
