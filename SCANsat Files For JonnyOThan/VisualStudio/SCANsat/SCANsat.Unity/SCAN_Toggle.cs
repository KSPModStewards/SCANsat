#region license
/*  [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_Toggle - Script to replace the hover sprite for the active toggle state
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
using UnityEngine.EventSystems;

namespace SCANsat.Unity
{
	public class SCAN_Toggle : Toggle, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
	{
		private Sprite normalImage;
		public Sprite HoverCheckmark;
		private bool inToggle;

		protected override void Awake()
		{
			base.Awake();

			normalImage = ((Image)graphic).sprite;
		}

		new public void OnPointerClick(PointerEventData eventData)
		{
			base.OnPointerClick(eventData);

			if (!isOn)
				return;

			if (inToggle)
				((Image)graphic).sprite = HoverCheckmark;
		}

		new public void OnPointerEnter(PointerEventData eventData)
		{
			base.OnPointerEnter(eventData);

			inToggle = true;

			if (!isOn)
				return;

			((Image)graphic).sprite = HoverCheckmark;
		}

		new public void OnPointerExit(PointerEventData eventData)
		{
			base.OnPointerExit(eventData);

			inToggle = false;

			if (!isOn)
				return;

			((Image)graphic).sprite = normalImage;
		}
	}
}
