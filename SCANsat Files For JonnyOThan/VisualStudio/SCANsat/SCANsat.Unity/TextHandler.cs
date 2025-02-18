#region license
/*  [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * TextHandler - Script for handling Text object replacement with Text Mesh Pro
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
	public class TextHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IScrollHandler
	{
		[SerializeField]
		private bool m_Outline = false;
		[SerializeField]
		private float m_OutlineWidth = 0;
		[SerializeField]
		private bool m_Highlighter = false;
		[SerializeField]
		private Color m_HighlightColor = Color.white;

		public class OnTextEvent : UnityEvent<string> { }

		public class OnColorEvent : UnityEvent<Color> { }

		public class OnFontEvent : UnityEvent<int> { }

		private OnTextEvent _onTextUpdate = new OnTextEvent();
		private OnColorEvent _onColorUpdate = new OnColorEvent();
		private OnFontEvent _onFontChange = new OnFontEvent();

		private Vector2 _preferredSize = new Vector2();
		private Color _normalColor = Color.white;
		private ScrollRect scroller;

		public void SetNormalColor(Color c)
		{
			_normalColor = c;
		}

		public void SetScroller(ScrollRect s)
		{
			scroller = s;
		}

		public bool Outline
		{
			get { return m_Outline; }
		}

		public float OutlineWidth
		{
			get { return m_OutlineWidth; }
		}

		public Vector2 PreferredSize
		{
			get { return _preferredSize; }
			set { _preferredSize = value; }
		}

		public UnityEvent<string> OnTextUpdate
		{
			get { return _onTextUpdate; }
		}

		public UnityEvent<Color> OnColorUpdate
		{
			get { return _onColorUpdate; }
		}

		public UnityEvent<int> OnFontChange
		{
			get { return _onFontChange; }
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (!m_Highlighter)
				return;

			OnColorUpdate.Invoke(m_HighlightColor);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			if (!m_Highlighter)
				return;

			OnColorUpdate.Invoke(_normalColor);
		}

		public void OnScroll(PointerEventData eventData)
		{
			if (scroller == null)
				return;

			scroller.OnScroll(eventData);
		}
	}
}
