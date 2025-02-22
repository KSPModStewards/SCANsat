#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_TMP_InputField - An extension of TMP_InputField for updating certain elements of the input field
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using SCANsat.Unity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

namespace SCANsat.SCAN_Unity
{
	public class SCAN_TMP_InputField : TMP_InputField
	{
		private InputHandler _handler;

		new private void Awake()
		{
			base.Awake();

			_handler = GetComponent<InputHandler>();

			onValueChanged.AddListener(new UnityAction<string>(valueChanged));

			_handler.OnTextUpdate.AddListener(new UnityAction<string>(UpdateText));
		}

		private void Update()
		{
			if (_handler != null)
			{
				_handler.IsFocused = isFocused;
			}
		}

		private void valueChanged(string s)
		{
			if (_handler == null)
			{
				return;
			}

			_handler.Text = s;

			_handler.OnValueChange.Invoke(s);
		}

		private void UpdateText(string t)
		{
			text = t;
		}
	}
}
