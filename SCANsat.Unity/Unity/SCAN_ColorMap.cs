
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using SCANsat.Unity.Interfaces;

namespace SCANsat.Unity.Unity
{
	public class SCAN_ColorMap : SettingsPage
	{
		[SerializeField]
		private SCAN_ColorPicker m_ColorPicker = null;
		[SerializeField]
		private Toggle m_MapVignette = null;
		[SerializeField]
		private Slider m_UnscannedSlider = null;
		[SerializeField]
		private TextHandler m_UnscannedText = null;
		[SerializeField]
		private InputHandler m_UnscannedInputField = null;
		[SerializeField]
		private Slider m_TransparencySlider = null;
		[SerializeField]
		private TextHandler m_TransparencyText = null;
		[SerializeField]
		private InputHandler m_TransInputField = null;


		[SerializeField]
		private TextHandler m_MapWidth = null;
		[SerializeField]
		private InputHandler m_MapWidthInputHandler = null;
		[SerializeField]
		private Toggle m_MapWidthToggle = null;
		[SerializeField]
		private Toggle m_PixelFiltering = null;
		[SerializeField]
		private Toggle m_NormalMap = null;
		[SerializeField]
		private Toggle m_ColorMap = null;
		[SerializeField]
		private Slider m_NormalSlider = null;
		[SerializeField]
		private TextHandler m_NormalText = null;
		[SerializeField]
		private Slider m_LuminanceSlider = null;
		[SerializeField]
		private TextHandler m_LuminanceText = null;


		private bool loaded;

		private ISCAN_Color colorInterface;
		private ISCAN_Settings settingsInterface;

		private void Awake()
		{
			if (m_TransInputField != null)
			{
				m_TransInputField.OnValueChange.AddListener(new UnityAction<string>(OnTransparencyInputChange));
			}

			if (m_UnscannedInputField != null)
			{
				m_UnscannedInputField.OnValueChange.AddListener(new UnityAction<string>(OnUnscannedTransparencyInputChange));
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
					&& m_UnscannedInputField != null && !m_UnscannedInputField.IsFocused
					&& m_MapWidthInputHandler != null && !m_MapWidthInputHandler.IsFocused
					&& m_ColorPicker != null && !m_ColorPicker.AnyInputActive)
				{
					settingsInterface.LockInput = false;
				}
			}
		}

		public void SetMap(ISCAN_Color color, ISCAN_Settings settings)
		{
			if (color == null)
			{
				return;
			}

			colorInterface = color;
			settingsInterface = settings;

			color.Refresh();

			if (m_MapVignette != null)
			{
				m_MapVignette.isOn = color.MapVignette;
			}

			if (m_UnscannedSlider != null)
			{
				m_UnscannedSlider.value = color.UnscannedTransparency;
			}

			if (m_TransparencySlider != null)
			{
				m_TransparencySlider.value = color.BackgroundTransparency;
			}

			if (m_MapWidth != null)
			{
				m_MapWidth.OnTextUpdate.Invoke("Map Width: " + color.MapWidth.ToString());
			}

			if (m_MapWidthToggle != null)
			{
				m_MapWidthToggle.isOn = color.UseMapWidth;
			}

			if (m_PixelFiltering != null)
			{
				m_PixelFiltering.isOn = color.PixelFiltering;
			}

			if (m_NormalMap != null)
			{
				m_NormalMap.isOn = color.NormalMap;
			}

			if (m_ColorMap != null)
			{
				m_ColorMap.isOn = color.ColorMap;
			}

			if (m_NormalSlider != null)
			{
				m_NormalSlider.value = color.NormalOpacity;
			}

			if (m_LuminanceSlider != null)
			{
				m_LuminanceSlider.value = color.LuminanceReduction;
			}

			if (m_ColorPicker != null)
			{
				m_ColorPicker.Setup(color.MapBackgroundColor, color.UnscannedColor, true);
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

		public void MapVignette(bool isOn)
		{
			if (!loaded || colorInterface == null)
			{
				return;
			}

			colorInterface.MapVignette = isOn;
		}

		public void UnscannedTrans(float value)
		{
			if (m_UnscannedText != null)
			{
				m_UnscannedText.OnTextUpdate.Invoke(string.Format("Unscanned Transparency: {0}%", value.ToString("N0")));
			}

			if (!loaded || colorInterface == null)
			{
				return;
			}

			colorInterface.UnscannedTransparency = value;
		}

		public void Transparency(float value)
		{
			if (m_TransparencyText != null)
			{
				m_TransparencyText.OnTextUpdate.Invoke(string.Format("Background Transparency: {0}%", value.ToString("N0")));
			}

			if (!loaded || colorInterface == null)
			{
				return;
			}

			colorInterface.BackgroundTransparency = value;
		}


		public void SetMapWidth()
		{
			if (colorInterface == null || m_MapWidthInputHandler == null)
			{
				return;
			}

			settingsInterface.LockInput = false;

			int width = colorInterface.MapWidth;

			if (int.TryParse(m_MapWidthInputHandler.Text, out width))
			{
				if (width % 2 != 0)
				{
					width += 1;
				}

				if (width > 8192)
				{
					width = 8192;
				}
				else if (width < 256)
				{
					width = 256;
				}

				m_MapWidthInputHandler.OnTextUpdate.Invoke(width.ToString());

				colorInterface.MapWidth = width;

				if (m_MapWidth != null)
				{
					m_MapWidth.OnTextUpdate.Invoke("Map Width: " + width.ToString());
				}
			}
		}

		public void MapWidthToggle(bool isOn)
		{
			if (!loaded || colorInterface == null)
			{
				return;
			}

			colorInterface.UseMapWidth = isOn;
		}

		public void PixelFilterToggle(bool isOn)
		{
			if (!loaded || colorInterface == null)
			{
				return;
			}

			colorInterface.PixelFiltering = isOn;
		}

		public void NormalMapToggle(bool isOn)
		{
			if (!loaded || colorInterface == null)
			{
				return;
			}

			colorInterface.NormalMap = isOn;
		}

		public void ColorMapToggle(bool isOn)
		{
			if (!loaded || colorInterface == null)
			{
				return;
			}

			colorInterface.ColorMap = isOn;
		}

		public void NormalOpacity(float value)
		{
			if (m_NormalText != null)
			{
				m_NormalText.OnTextUpdate.Invoke(string.Format("Normal Opacity: {0}%", value.ToString("N0")));
			}

			if (!loaded || colorInterface == null)
			{
				return;
			}

			colorInterface.NormalOpacity = value;
		}

		public void LuminanceReduction(float value)
		{
			if (m_LuminanceText != null)
			{
				m_LuminanceText.OnTextUpdate.Invoke(string.Format("Luminance Reduction: {0}%", value.ToString("N0")));
			}

			if (!loaded || colorInterface == null)
			{
				return;
			}

			colorInterface.LuminanceReduction = value;
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

		public void OnUnscannedTransparencyInputChange(string input)
		{
			if (m_UnscannedSlider == null)
			{
				return;
			}

			float tran = SCAN_ColorControl.ParseInput(input, m_UnscannedSlider.value, m_UnscannedSlider.minValue, m_UnscannedSlider.maxValue, 0);

			if (tran != m_UnscannedSlider.value)
			{
				m_UnscannedSlider.value = tran;
			}
		}

		public void Apply()
		{
			if (colorInterface == null || m_ColorPicker == null)
			{
				return;
			}

			colorInterface.MapApply(m_ColorPicker.GetColorOne, m_ColorPicker.GetColorTwo);

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

			colorInterface.MapDefault();

			if (m_ColorPicker != null)
			{
				m_ColorPicker.Setup(colorInterface.MapBackgroundColor, colorInterface.UnscannedColor, false);
			}

			loaded = false;

			if (m_UnscannedSlider != null)
			{
				m_UnscannedSlider.value = colorInterface.UnscannedTransparency;
			}

			if (m_TransparencySlider != null)
			{
				m_TransparencySlider.value = colorInterface.BackgroundTransparency;
			}

			loaded = true;
		}
	}
}
