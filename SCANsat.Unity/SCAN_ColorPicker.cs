#region license
/*  [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * CanvasFader - Monobehaviour for making smooth fade in and fade out for UI windows
 * 
 * Copyright (c)2013 damny;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 DMagic
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SCANsat.Unity.HSVPicker.UI;

namespace SCANsat.Unity
{
	public class SCAN_ColorPicker : MonoBehaviour
	{
		[SerializeField]
		private HSVPicker.UI.ColorImage m_ColorOne = null;
		[SerializeField]
		private HSVPicker.UI.ColorImage m_ColorTwo = null;
		[SerializeField]
		private Image m_OldColorOne = null;
		[SerializeField]
		private Image m_OldColorTwo = null;
		[SerializeField]
		private InputHandler m_RInputField = null;
		[SerializeField]
		private InputHandler m_GInputField = null;
		[SerializeField]
		private InputHandler m_BInputField = null;

		private ColorPicker picker;

		private bool anyInputActive;

		public Color GetColorOne
		{
			get { return m_ColorOne.CurrentColor; }
		}

		public Color GetColorTwo
		{
			get { return m_ColorTwo.CurrentColor; }
		}

		public bool AnyInputActive
		{
			get { return anyInputActive; }
		}

		private void Awake()
		{
			picker = GetComponent<ColorPicker>();
		}

		private void Update()
		{
			anyInputActive = m_RInputField.IsFocused || m_GInputField.IsFocused || m_RInputField.IsFocused;
		}

		public void Setup(Color one, Color two, bool reset)
		{
			if (picker != null && reset)
				picker.CurrentColor = one;

			if (m_ColorOne != null)
			{
				m_ColorOne.SetColor(one);

				if (reset)
					m_ColorOne.IsActive = true;
			}

			if (m_ColorTwo != null)
				m_ColorTwo.SetColor(two);

			if (m_OldColorOne != null)
				m_OldColorOne.color = one;

			if (m_OldColorTwo != null)
				m_OldColorTwo.color = two;

			if (m_RInputField != null)
				m_RInputField.OnTextUpdate.Invoke("");

			if (m_GInputField != null)
				m_GInputField.OnTextUpdate.Invoke("");

			if (m_BInputField != null)
				m_BInputField.OnTextUpdate.Invoke("");
		}

		public void ColorOne(bool isOn)
		{
			if (m_ColorOne == null)
				return;

			m_ColorOne.IsActive = isOn;

			if (isOn)
			{
				picker.CurrentColor = m_ColorOne.CurrentColor;

				if (m_RInputField != null)
					m_RInputField.OnTextUpdate.Invoke("");

				if (m_GInputField != null)
					m_GInputField.OnTextUpdate.Invoke("");

				if (m_BInputField != null)
					m_BInputField.OnTextUpdate.Invoke("");
			}
		}

		public void ColorTwo(bool isOn)
		{
			if (m_ColorTwo == null)
				return;

			m_ColorTwo.IsActive = isOn;

			if (isOn)
			{
				picker.CurrentColor = m_ColorTwo.CurrentColor;

				if (m_RInputField != null)
					m_RInputField.OnTextUpdate.Invoke("");

				if (m_GInputField != null)
					m_GInputField.OnTextUpdate.Invoke("");

				if (m_BInputField != null)
					m_BInputField.OnTextUpdate.Invoke("");
			}
		}

	}
}
