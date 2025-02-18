#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_TextMeshPro - An extension of TextMeshProUGUI for updating certain elements of the text
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using SCANsat.Unity;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace SCANsat.SCAN_Unity
{
	public class SCAN_TextMeshPro : TextMeshProUGUI
	{
		private TextHandler _handler;

		new private void Awake()
		{
			m_isAlignmentEnumConverted = true;
			
			base.Awake();

			_handler = GetComponent<TextHandler>();

			if (_handler == null)
				return;

			_handler.SetNormalColor(color);

			_handler.OnColorUpdate.AddListener(new UnityAction<Color>(UpdateColor));
			_handler.OnTextUpdate.AddListener(new UnityAction<string>(UpdateText));
			_handler.OnFontChange.AddListener(new UnityAction<int>(UpdateFontSize));
		}

		public void Setup(TextHandler h)
		{
			_handler = h;

			_handler.OnColorUpdate.AddListener(new UnityAction<Color>(UpdateColor));
			_handler.OnTextUpdate.AddListener(new UnityAction<string>(UpdateText));
			_handler.OnFontChange.AddListener(new UnityAction<int>(UpdateFontSize));
		}

		private void UpdateColor(Color c)
		{
			color = c;
		}

		private void UpdateText(string t)
		{
			text = t;

			_handler.PreferredSize = new Vector2(preferredWidth, preferredHeight);
		}

		private void UpdateFontSize(int i)
		{
			fontSize += i;
		}
	}
}
