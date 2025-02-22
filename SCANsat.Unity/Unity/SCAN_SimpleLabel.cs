#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_SimpleLabel - Script for controlling simple map icons
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using UnityEngine;
using UnityEngine.UI;

namespace SCANsat.Unity.Unity
{
	public class SCAN_SimpleLabel : MonoBehaviour
	{
		private Image icon;
		private RectTransform rect;
		private SimpleLabelInfo info;

		public void Setup(SimpleLabelInfo label)
		{
			if (label == null)
			{
				return;
			}

			info = label;

			rect = gameObject.AddComponent<RectTransform>();
			rect.anchorMin = new Vector2(0, 0);
			rect.anchorMax = new Vector2(0, 0);
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.localScale = new Vector3(1, 1, 1);
			rect.localPosition = new Vector3(0, 0, 0);
			rect.anchoredPosition3D = new Vector3(label.pos.x, label.pos.y, 0);
			rect.sizeDelta = new Vector2(label.width, label.width);

			icon = gameObject.AddComponent<Image>();
			icon.sprite = label.image;
			icon.color = label.color;
			icon.raycastTarget = false;
			icon.type = Image.Type.Sliced;
		}

		public void UpdateIcon(SimpleLabelInfo label)
		{
			if (icon == null || rect == null || label == null)
			{
				return;
			}

			if (gameObject.activeSelf && !label.show)
			{
				gameObject.SetActive(false);
				return;
			}
			else if (!gameObject.activeSelf && label.show)
			{
				gameObject.SetActive(true);
			}

			if (icon.color != label.color)
			{
				icon.color = label.color;
			}

			rect.anchoredPosition = label.pos;
		}

	}
}
