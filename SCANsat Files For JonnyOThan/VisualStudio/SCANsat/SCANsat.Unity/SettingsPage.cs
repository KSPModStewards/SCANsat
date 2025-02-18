#region license
/*  [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SettingsPage - Base behaviour for all settings window pages
 * 
 * Copyright (c)2013 damny;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 DMagic
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SCANsat.Unity
{
	public class SettingsPage : MonoBehaviour
	{
		public virtual void OnPointerDown(PointerEventData eventData) { }
	}
}
