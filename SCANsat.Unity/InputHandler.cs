#region license
/*  [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * InputHandler - Script for handling Input object replacement with Text Mesh Pro
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
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace SCANsat.Unity
{
	public class InputHandler : MonoBehaviour
	{
		private string _text;
		private bool _isFocused;

		public class OnTextEvent : UnityEvent<string> { }
		public class OnValueChanged : UnityEvent<string> { }

		private OnTextEvent _onTextUpdate = new OnTextEvent();
		private OnValueChanged _onValueChanged = new OnValueChanged();

		public string Text
		{
			get { return _text; }
			set { _text = value; }
		}

		public bool IsFocused
		{
			get { return _isFocused; }
			set { _isFocused = value; }
		}

		public UnityEvent<string> OnTextUpdate
		{
			get { return _onTextUpdate; }
		}

		public UnityEvent<string> OnValueChange
		{
			get { return _onValueChanged; }
		}
	}
}
