#region license
/*  [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_DoubleButton - Script for handling double click buttons
 * 
 * Copyright (c)2013 damny;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 DMagic
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SCANsat.Unity
{
	public class SCAN_DoubleButton : Button, IPointerClickHandler
	{
		private int clickCount;

		new public void OnPointerClick(PointerEventData eventData)
		{
			if (eventData.button != PointerEventData.InputButton.Left)
				return;

			if (clickCount > 0)
				clickCount++;
			else
			{
				clickCount = 1;
				StartCoroutine(clickWait());
			}
		}

		private IEnumerator clickWait()
		{
			yield return new WaitForSeconds(0.4f);

			if (clickCount > 1)
				doubleClick();

			clickCount = 0;
		}

		private void doubleClick()
		{
			if (!IsActive() || !IsInteractable())
				return;

			onClick.Invoke();
		}

	}
}
