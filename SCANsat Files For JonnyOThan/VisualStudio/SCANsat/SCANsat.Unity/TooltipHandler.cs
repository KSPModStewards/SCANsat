#region license
/*  [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * TooltipHandler - Script to control tooltip activation
 * 
 * Copyright (c)2013 damny;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 DMagic
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using System.Collections.Generic;
using SCANsat.Unity.Unity;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SCANsat.Unity
{

	public class TooltipHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
	{
		[SerializeField, TextArea(2, 10)]
		private string m_TooltipName = "";
		[SerializeField]
		private bool m_IsActive = true;
		[SerializeField]
		private bool m_HelpTip = false;
		[SerializeField]
		private GameObject _prefab = null;
		[SerializeField]
		private string _tooltipText = "";

		private Canvas _canvas;
		private SCAN_Tooltip _tooltip;
		private float _scale;

		public string TooltipName
		{
			get { return m_TooltipName; }
		}

		public Canvas _Canvas
		{
			set { _canvas = value; }
		}

		public GameObject Prefab
		{
			set { _prefab = value; }
		}

		public float Scale
		{
			set { _scale = value; }
		}

		public bool IsActive
		{
			set { m_IsActive = value; }
		}

		public bool HelpTip
		{
			get { return m_HelpTip; }
		}

		public string TooltipText
		{
			set { _tooltipText = value; }
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (!m_IsActive)
				return;

			OpenTooltip();
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			if (!m_IsActive)
				return;

			CloseTooltip();
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (!m_IsActive)
				return;

			CloseTooltip();
		}

		private void OpenTooltip()
		{
			if (_prefab == null || _canvas == null)
				return;

			_tooltip = Instantiate(_prefab).GetComponent<SCAN_Tooltip>();

			if (_tooltip == null)
				return;

			_tooltip.transform.SetParent(_canvas.transform, false);
			_tooltip.transform.SetAsLastSibling();

			_tooltip.Setup(_canvas, _tooltipText, _scale);
		}

		private void CloseTooltip()
		{
			if (_tooltip == null)
				return;

			_tooltip.gameObject.SetActive(false);
			Destroy(_tooltip.gameObject);
		}
	}
}
