#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_VesselInfo - Script for controlling vessel information readout buttons
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
	public class SCAN_VesselInfo : MonoBehaviour
	{
		[SerializeField]
		private TextHandler m_VesselText = null;

		private Guid _id;
		private ISCAN_MainMap mapInterface;
		private MapLabelInfo label;

		public Guid ID
		{
			get { return _id; }
		}

		public void SetVessel(Guid id, MapLabelInfo info, ISCAN_MainMap map)
		{
			if (map == null)
				return;

			_id = id;
			label = info;
			label.name = !string.IsNullOrEmpty(label.name) && label.name.Length > 26 ? label.name.Substring(0, 26) : label.name;
			mapInterface = map;

			if (m_VesselText == null)
				return;

			if (info.label != "1")
			{
				m_VesselText.SetNormalColor(Color.white);
				m_VesselText.OnColorUpdate.Invoke(Color.white);
			}

			m_VesselText.OnTextUpdate.Invoke(label.name);
		}

		public void UpdateText(string value)
		{
			if (m_VesselText == null)
				return;

			m_VesselText.OnTextUpdate.Invoke(string.Format("[{0}] {1}: {2}", label.label, label.name, value));
		}

		public void ChangeToVessel()
		{
			if (mapInterface == null)
				return;

			mapInterface.ChangeToVessel(_id);
		}

	}
}
