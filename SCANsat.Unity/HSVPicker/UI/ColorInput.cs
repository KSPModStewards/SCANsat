using UnityEngine;
using UnityEngine.UI;
using System;

namespace SCANsat.Unity.HSVPicker.UI
{
	/// <summary>
	/// Displays one of the color values of aColorPicker
	/// </summary>
	public class ColorInput : MonoBehaviour
	{
		public ColorPicker hsvpicker;

		/// <summary>
		/// Which value this slider can edit.
		/// </summary>
		public ColorValues type;

		private InputHandler inputField;

		private void Awake()
		{
			inputField = GetComponent<InputHandler>();

			inputField.OnValueChange.AddListener(InputChanged);
		}

		private void OnDestroy()
		{
			inputField.OnValueChange.RemoveListener(InputChanged);
		}

		private void InputChanged(string input)
		{
			if (string.IsNullOrEmpty(input))
				return;

			float original = 0;

			switch(type)
			{
				case ColorValues.R:
					original = hsvpicker.R;
					break;
				case ColorValues.G:
					original = hsvpicker.G;
					break;
				case ColorValues.B:
					original = hsvpicker.B;
					break;
			}

			float f = original;

			if (!float.TryParse(input, out f))
				return;

			if (f < 0)
				return;
			else if (input.StartsWith("1."))
				f = 1;
			else if (f >= 1 && f <= 255)
			{
				f = Mathf.RoundToInt(f);

				f /= 255;
			}
			else if (f > 255)
				return;

			hsvpicker.AssignColor(type, f);
		}
	}
}
