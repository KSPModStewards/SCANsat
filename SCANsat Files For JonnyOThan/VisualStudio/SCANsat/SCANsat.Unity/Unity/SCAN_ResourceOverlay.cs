#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_ResourceOverlay - Script for controlling each resource toggle for the planetary overlay window
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

namespace SCANsat.Unity.Unity
{
	public class SCAN_ResourceOverlay : MonoBehaviour
	{
		[SerializeField]
		private TextHandler m_ResourceName = null;

		private SCAN_Overlay parent;
		private string resource;
		private bool active;

		public string Resource
		{
			get { return resource; }
		}

		public void SetResource(string name, SCAN_Overlay p, bool isOn)
		{
			if (p == null)
				return;

			parent = p;
			resource = name;

			if (m_ResourceName != null)
				m_ResourceName.OnTextUpdate.Invoke(name);

			if (isOn && p.OverlayInterface.DrawResource)
			{
				m_ResourceName.OnColorUpdate.Invoke(p.ActiveColor);
				m_ResourceName.SetNormalColor(p.ActiveColor);
			}
		}

		public void DrawResource()
		{
			if (parent == null)
				return;

			active = !active;

			parent.SetResource(resource, active);

			if (m_ResourceName != null)
				m_ResourceName.SetNormalColor(parent.ActiveColor);
		}

		public void Inactivate()
		{
			active = false;

			if (m_ResourceName != null)
			{
				m_ResourceName.OnColorUpdate.Invoke(parent.NormalColor);
				m_ResourceName.SetNormalColor(parent.NormalColor);
			}
		}
	}
}
