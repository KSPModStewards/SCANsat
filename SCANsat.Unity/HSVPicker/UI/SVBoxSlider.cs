using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using SCANsat.Unity.HSVPicker.Utility;

namespace SCANsat.Unity.HSVPicker.UI
{
	[RequireComponent(typeof(BoxSlider), typeof(RawImage)), ExecuteInEditMode()]
	public class SVBoxSlider : MonoBehaviour
	{
		public ColorPicker picker;

		private BoxSlider slider;
		private RawImage image;

		private float lastH = -1;
		private bool listen = true;

		public RectTransform rectTransform
		{
			get
			{
				return transform as RectTransform;
			}
		}

		private void Awake()
		{
			slider = GetComponent<BoxSlider>();
			image = GetComponent<RawImage>();

			RegenerateSVTexture();
		}

		private void OnEnable()
		{
			if (Application.isPlaying)
            {
                if (picker != null)
                    picker.onHSVChanged.AddListener(HSVChanged);
                if (slider != null)
                    slider.onValueChanged.AddListener(SliderChanged);
            }
        }

		private void OnDisable()
		{
			if (picker != null)
				picker.onHSVChanged.RemoveListener(HSVChanged);
            if (slider != null)
                slider.onValueChanged.RemoveListener(SliderChanged);
        }

		private void OnDestroy()
		{
			if (image.texture != null)
				DestroyImmediate(image.texture);
		}

#if UNITY_EDITOR
    private void OnValidate()
    {
        image = GetComponent<RawImage>();
        RegenerateSVTexture();
    }
#endif

		private void SliderChanged(float saturation, float value)
		{
			if (listen)
			{
				picker.AssignColor(ColorValues.Saturation, saturation);
				picker.AssignColor(ColorValues.Value, value);
			}
			listen = true;
		}

		private void HSVChanged(float h, float s, float v)
		{
			if (lastH != h)
			{
				lastH = h;
				RegenerateSVTexture();
			}

			if (s != slider.normalizedValue)
			{
				listen = false;
				slider.normalizedValue = s;
			}

			if (v != slider.normalizedValueY)
			{
				listen = false;
				slider.normalizedValueY = v;
			}
		}

		private void RegenerateSVTexture()
		{
			double h = picker != null ? picker.H * 360 : 0;

			if (image.texture != null)
				DestroyImmediate(image.texture);

			Texture2D texture = new Texture2D(100, 100);
			texture.hideFlags = HideFlags.DontSave;

			for (int s = 0; s < 100; s++)
			{
				Color32[] colors = new Color32[100];
				for (int v = 0; v < 100; v++)
				{
					colors[v] = HSVUtil.ConvertHsvToRgb(h, (float)s / 100, (float)v / 100, 1);
				}
				texture.SetPixels32(s, 0, 1, 100, colors);
			}
			texture.Apply();

			image.texture = texture;
		}
	}
}
