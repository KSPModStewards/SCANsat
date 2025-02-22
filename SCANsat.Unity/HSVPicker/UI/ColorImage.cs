using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace SCANsat.Unity.HSVPicker.UI
{
	[RequireComponent(typeof(Image))]
	public class ColorImage : MonoBehaviour
	{
		public ColorPicker picker;
		private bool _isActive;

		private Image image;

		public bool IsActive
		{
			get { return _isActive; }
			set { _isActive = value; }
		}

		public Color CurrentColor
		{
			get { return image.color; }
		}

		private void Awake()
		{
			image = GetComponent<Image>();
			picker.onValueChanged.AddListener(ColorChanged);
		}

		private void OnDestroy()
		{
			picker.onValueChanged.RemoveListener(ColorChanged);
		}

		private void ColorChanged(Color newColor)
		{
			if (_isActive)
			{
				image.color = newColor;
			}
		}

		public void SetColor(Color newColor)
		{
			image.color = newColor;
		}
	}
}
