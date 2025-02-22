#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_Instruments - Script for controlling the instruments window UI
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
using UnityEngine.EventSystems;
using SCANsat.Unity.Interfaces;

namespace SCANsat.Unity.Unity
{
	public class SCAN_Instruments : CanvasFader, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerDownHandler
	{
		[SerializeField]
		private TextHandler m_Version = null;
		[SerializeField]
		private TextHandler m_ReadoutText = null;
		[SerializeField]
		private RectTransform m_ResourceButtons = null;
		[SerializeField]
		private GameObject m_AnomalyButtons = null;
		[SerializeField]
		private RawImage m_AnomalyImage = null;
		[SerializeField]
		private TextHandler m_AnomalyPrintText = null;
		[SerializeField]
		private TextHandler m_AnomalyNameText = null;
		[SerializeField]
		private GameObject m_AnomalyObject = null;
		[SerializeField]
		private Shader m_EdgeDetectShader = null;
		[SerializeField]
		private Shader m_GrayScaleShader = null;

		private ISCAN_Instruments insInterface;
		private RectTransform rect;
		private Vector2 mouseStart;
		private Vector3 windowStart;

		//public Shader EdgeDetectShader
		//{
		//	get { return m_EdgeDetectShader; }
		//}

		//public Shader GrayScaleShader
		//{
		//	get { return m_GrayScaleShader; }
		//}

		protected override void Awake()
		{
			base.Awake();

			rect = GetComponent<RectTransform>();

			//string s;

			//if (m_EdgeDetectShader != null)
			//	s = m_EdgeDetectShader.name;

			//if (m_GrayScaleShader != null)
			//	s = m_GrayScaleShader.name;

			Alpha(0);
		}

		private void Update()
		{
			if (insInterface == null || !insInterface.IsVisible)
			{
				return;
			}

			insInterface.Update();
		}

		public void SetInstruments(ISCAN_Instruments ins)
		{
			if (ins == null)
			{
				return;
			}

			insInterface = ins;

			if (m_Version != null)
			{
				m_Version.OnTextUpdate.Invoke(ins.Version);
			}

			if (!ins.ResourceButtons && m_ResourceButtons != null)
			{
				m_ResourceButtons.gameObject.SetActive(false);
			}

			if (!ins.AnomalyButtons && m_AnomalyButtons != null)
			{
				m_AnomalyButtons.SetActive(false);
			}

			SetScale(ins.Scale);

			SetPosition(ins.Position);

			ProcessTooltips();

			FadeIn();
		}

		public void FadeIn()
		{
			Fade(1, true);
		}

		public void FadeOut()
		{
			Fade(0, false, Kill, false);
		}

		private void Kill()
		{
			gameObject.SetActive(false);
			Destroy(gameObject);
		}

		public void Close()
		{
			if (insInterface != null)
			{
				insInterface.IsVisible = false;
			}
		}

		public void ProcessTooltips()
		{
			if (insInterface == null)
			{
				return;
			}

			TooltipHandler[] handlers = gameObject.GetComponentsInChildren<TooltipHandler>(true);

			if (handlers == null)
			{
				return;
			}

			for (int j = 0; j < handlers.Length; j++)
			{
				ProcessTooltip(handlers[j], insInterface.TooltipsOn, insInterface.TooltipCanvas, insInterface.Scale);
			}
		}

		private void ProcessTooltip(TooltipHandler handler, bool isOn, Canvas c, float scale)
		{
			if (handler == null)
			{
				return;
			}

			handler.IsActive = isOn && !handler.HelpTip;
			handler._Canvas = c;
			handler.Scale = scale;
		}

		public void SetResourceButtons(int lines)
		{
			if (insInterface == null || m_ResourceButtons == null)
			{
				return;
			}

			float y = -1 * lines * 24;

			if (insInterface.ResourceButtons)
			{
				m_ResourceButtons.anchoredPosition3D = new Vector3(m_ResourceButtons.anchoredPosition.x, y, 0);
			}
		}

		public void SetAnomalyButtons()
		{
			if (insInterface == null || m_AnomalyButtons == null)
			{
				return;
			}

			if (!insInterface.AnomalyButtons)
			{
				if (m_AnomalyButtons.activeSelf)
				{
					m_AnomalyButtons.SetActive(false);
				}
			}
			else
			{
				if (!m_AnomalyButtons.activeSelf)
				{
					m_AnomalyButtons.SetActive(true);
				}
			}
		}

		public void SetScale(float scale)
		{
			rect.localScale = Vector3.one * scale;
		}

		public void SetPosition(Vector2 pos)
		{
			if (rect == null)
			{
				return;
			}

			rect.anchoredPosition = new Vector3(pos.x, pos.y, 0);
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			transform.SetAsLastSibling();
		}

		public void OnBeginDrag(PointerEventData eventData)
		{
			if (rect == null)
			{
				return;
			}

			mouseStart = eventData.position;
			windowStart = rect.position;
		}

		public void OnDrag(PointerEventData eventData)
		{
			if (rect == null)
			{
				return;
			}

			rect.position = windowStart + (Vector3)(eventData.position - mouseStart);

			if (insInterface == null)
			{
				return;
			}

			insInterface.ClampToScreen(rect);
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			if (rect == null || insInterface == null)
			{
				return;
			}

			insInterface.Position = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y);
		}

		public void UpdateText(string s)
		{
			if (m_ReadoutText == null)
			{
				return;
			}

			m_ReadoutText.OnTextUpdate.Invoke(s);
		}

		public void UpdateAnomaly(Texture tex)
		{
			if (m_AnomalyImage == null)
			{
				return;
			}

			m_AnomalyImage.texture = tex;
		}

		public void UpdateAnomalyText(string s)
		{
			if (m_AnomalyPrintText == null)
			{
				return;
			}

			m_AnomalyPrintText.OnTextUpdate.Invoke(s);
		}

		public void UpdateAnomalyName(string s)
		{
			if (m_AnomalyNameText == null)
			{
				return;
			}

			m_AnomalyNameText.OnTextUpdate.Invoke(s);
		}

		public void PreviousResource()
		{
			if (insInterface == null)
			{
				return;
			}

			insInterface.PreviousResource();
		}

		public void NextResource()
		{
			if (insInterface == null)
			{
				return;
			}

			insInterface.NextResource();
		}

		public void NextAnomaly()
		{
			if (insInterface == null)
			{
				return;
			}

			insInterface.NextAnomaly();
		}

		public void SetDetailState(bool isOn)
		{
			if (m_AnomalyObject == null)
			{
				return;
			}

			if (isOn && !m_AnomalyObject.activeSelf)
			{
				m_AnomalyObject.SetActive(true);
			}
			else if (!isOn && m_AnomalyObject.activeSelf)
			{
				m_AnomalyObject.SetActive(false);
			}
		}

		public void OnMouseEnterAnomaly(BaseEventData eventData)
		{
			if (!(eventData is PointerEventData) || insInterface == null)
			{
				return;
			}

			insInterface.MouseAnomaly = true;
		}

		public void OnMouseExitAnomaly(BaseEventData eventData)
		{
			if (!(eventData is PointerEventData) || insInterface == null)
			{
				return;
			}

			insInterface.MouseAnomaly = false;
		}
	}
}
