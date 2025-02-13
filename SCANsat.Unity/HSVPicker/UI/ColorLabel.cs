using UnityEngine;
using System;
using SCANsat.Unity.HSVPicker.Enum;

namespace SCANsat.Unity.HSVPicker.UI
{
	[RequireComponent(typeof(TextHandler))]
	public class ColorLabel : MonoBehaviour
	{
		public ColorPicker picker;
		public Enum.ColorValues type;

		private TextHandler label;

		private void Awake()
		{
			label = GetComponent<TextHandler>();
		}

		private void OnEnable()
		{
			if (Application.isPlaying && picker != null)
			{
				picker.onValueChanged.AddListener(ColorChanged);
				picker.onHSVChanged.AddListener(HSVChanged);
			}
		}

		private void OnDestroy()
		{
			if (picker != null)
			{
				picker.onValueChanged.RemoveListener(ColorChanged);
				picker.onHSVChanged.RemoveListener(HSVChanged);
			}
		}

		private void ColorChanged(Color color)
		{
			UpdateValue();
		}

		private void HSVChanged(float hue, float sateration, float value)
		{
			UpdateValue();
		}

		private void UpdateValue()
		{
			if (picker == null)
			{
				label.OnTextUpdate.Invoke("-");
			}
			else
			{
				float valueOne = 0;
				float valueTwo = 0;
				float valueThree = 0;


				if (type == Enum.ColorValues.R)
				{
					valueOne = picker.GetValue(ColorValues.R) * 255;
					valueTwo = picker.GetValue(ColorValues.G) * 255;
					valueThree = picker.GetValue(ColorValues.B) * 255;

					label.OnTextUpdate.Invoke(string.Format("{0:N0},{1:N0},{2:N0}", Mathf.FloorToInt(valueOne), Mathf.FloorToInt(valueTwo), Mathf.FloorToInt(valueThree)));
				}
				else if (type == Enum.ColorValues.Hue)
				{
					valueOne = picker.GetValue(ColorValues.Hue) * 360;
					valueTwo = picker.GetValue(ColorValues.Saturation) * 255;
					valueThree = picker.GetValue(ColorValues.Value) * 255;

					label.OnTextUpdate.Invoke(string.Format("{0:N0},{1:N0},{2:N0}", Mathf.FloorToInt(valueOne), Mathf.FloorToInt(valueTwo), Mathf.FloorToInt(valueThree)));
				}
				else if (type == Enum.ColorValues.Hex)
				{
					valueOne = picker.GetValue(ColorValues.R) * 255;
					valueTwo = picker.GetValue(ColorValues.G) * 255;
					valueThree = picker.GetValue(ColorValues.B) * 255;

					label.OnTextUpdate.Invoke(string.Format("#{0:X2}{1:X2}{2:X2}", Mathf.FloorToInt(valueOne), Mathf.FloorToInt(valueTwo), Mathf.FloorToInt(valueThree)));
				}
			}
		}
	}
}
