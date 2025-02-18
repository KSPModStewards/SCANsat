#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_BackgroundElement - Script for controlling background scanning toggle elements
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SCANsat.Unity.Interfaces;

namespace SCANsat.Unity.Unity
{
	public class SCAN_BackgroundElement : MonoBehaviour
	{
		[SerializeField]
		private TextHandler m_BodyText = null;
		[SerializeField]
		private SCAN_Toggle m_BodyToggle = null;

		private bool loaded;
		private string bodyName;
		private ISCAN_Settings settingsInterface;

		public string BodyName
		{
			get { return bodyName; }
		}

		public void Setup(string body, bool active, ISCAN_Settings settings)
		{
			if (settings == null)
				return;

			bodyName = body;
			settingsInterface = settings;

			if (m_BodyToggle != null)
				m_BodyToggle.isOn = active;

			if (m_BodyText != null)
				m_BodyText.OnTextUpdate.Invoke(string.Format("{0} (0%)", body));

			loaded = true;
		}

		public void UpdateText(double amount)
		{
			if (m_BodyText == null)
				return;

			m_BodyText.OnTextUpdate.Invoke(string.Format("{0} ({1:P0})", bodyName, amount));
		}

		public void ToggleBody(bool isOn)
		{
			if (!loaded || settingsInterface == null)
				return;

			settingsInterface.ToggleBody(bodyName);
		}
	}
}
