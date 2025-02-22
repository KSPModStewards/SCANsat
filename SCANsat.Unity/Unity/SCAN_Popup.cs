#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_Popup - Script for controlling the warning popup windows
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace SCANsat.Unity.Unity
{
	public class SCAN_Popup : CanvasFader
	{
		public class OnSelectEvent : UnityEvent { }

		[SerializeField]
		private TextHandler m_WarningText = null;

		private OnSelectEvent _onSelectUpdate = new OnSelectEvent();

		public OnSelectEvent OnSelectUpdate
		{
			get { return _onSelectUpdate; }
		}

		protected override void Awake()
		{
			base.Awake();

			Alpha(0);
		}

		public void Setup(string text)
		{
			if (m_WarningText != null)
			{
				m_WarningText.OnTextUpdate.Invoke(text);
			}

			FadeIn();
		}

		private void FadeIn()
		{
			Fade(1, true);
		}

		public void FadeOut(bool fast = false)
		{
			Fade(0, fast, Close, false);
		}

		private void Close()
		{
			gameObject.SetActive(false);
			DestroyImmediate(gameObject);
		}

		public void Confirm()
		{
			_onSelectUpdate.Invoke();
		}
	}
}
