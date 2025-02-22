#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_Settings - Script for controlling the settings UI
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
	public class SCAN_Settings : CanvasFader, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerDownHandler
	{
		[SerializeField]
		private TextHandler m_Version = null;
		[SerializeField]
		private Transform m_ContentTransform = null;
		[SerializeField]
		private Toggle m_GeneralToggle = null;
		[SerializeField]
		private Toggle m_BackgroundToggle = null;
		[SerializeField]
		private Toggle m_ResourceToggle = null;
		[SerializeField]
		private Toggle m_DataToggle = null;
		[SerializeField]
		private Toggle m_ColorToggle = null;
		[SerializeField]
		private Toggle m_HelpTips = null;
		[SerializeField]
		private GameObject m_GeneralPrefab = null;
		[SerializeField]
		private GameObject m_BackgroundPrefab = null;
		[SerializeField]
		private GameObject m_ResourcePrefab = null;
		[SerializeField]
		private GameObject m_DataPrefab = null;
		[SerializeField]
		private GameObject m_ColorPrefab = null;
		[SerializeField]
		private GameObject m_PopupPrefab = null;
		[SerializeField]
		private GameObject m_DropDownPrefab = null;

		private ISCAN_Settings settingsInterface;
		private RectTransform rect;
		private Vector2 mouseStart;
		private Vector3 windowStart;
		private int _page;
		private bool _forceResource;

		private SettingsPage CurrentPage;
		private SCAN_Popup warningPopup;
		private SCAN_DropDown dropDown;

		private static SCAN_Settings instance;

		public static SCAN_Settings Instance
		{
			get { return instance; }
		}

		public int Page
		{
			get { return _page; }
		}

		public GameObject PopupPrefab
		{
			get { return m_PopupPrefab; }
		}

		public GameObject DropDownPrefab
		{
			get { return m_DropDownPrefab; }
		}

		public SCAN_Popup WarningPopup
		{
			get { return warningPopup; }
			set { warningPopup = value; }
		}

		public SCAN_DropDown DropDown
		{
			get { return dropDown; }
			set { dropDown = value; }
		}

		public bool IsCurrentResourceActive(string body, string resource)
		{
			if (CurrentPage != null)
			{
				if (CurrentPage is SCAN_ColorControl)
				{
					return ((SCAN_ColorControl)CurrentPage).IsResourcePage(body, resource);
				}
			}

			return false;
		}

		public void ClearWarningsAndDropDown()
		{
			if (dropDown != null)
			{
				dropDown.gameObject.SetActive(false);
				DestroyImmediate(dropDown.gameObject);
				dropDown = null;
			}

			if (warningPopup != null)
			{
				warningPopup.gameObject.SetActive(false);
				DestroyImmediate(warningPopup.gameObject);
				warningPopup = null;
			}
		}

		protected override void Awake()
		{
			base.Awake();

			instance = this;

			rect = GetComponent<RectTransform>();

			Alpha(0);
		}

		private void Update()
		{
			if (settingsInterface == null || !settingsInterface.IsVisible)
			{
				return;
			}

			settingsInterface.Update();
		}

		public void setSettings(ISCAN_Settings settings, int page, bool resource)
		{
			if (settings == null)
			{
				return;
			}

			settingsInterface = settings;

			if (m_Version != null)
			{
				m_Version.OnTextUpdate.Invoke(settings.Version);
			}

			_page = page;
			_forceResource = resource;

			switch (page)
			{
				case 0:
					if (m_GeneralToggle != null)
					{
						m_GeneralToggle.isOn = true;
					}

					break;
				case 1:
					if (m_BackgroundToggle != null)
					{
						m_BackgroundToggle.isOn = true;
					}

					break;
				case 2:
					if (m_ResourceToggle != null)
					{
						m_ResourceToggle.isOn = true;
					}

					break;
				case 3:
					if (m_DataToggle != null)
					{
						m_DataToggle.isOn = true;
					}

					break;
				case 4:
					if (m_ColorToggle != null)
					{
						m_ColorToggle.isOn = true;
					}

					break;
				default:
					if (m_GeneralToggle != null)
					{
						m_GeneralToggle.isOn = true;
					}

					break;
			}

			SetScale(settings.UIScale);

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
			if (settingsInterface != null)
			{
				settingsInterface.IsVisible = false;
			}
		}

		public void ProcessTooltips()
		{
			if (settingsInterface == null)
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
				ProcessTooltip(handlers[j], settingsInterface.WindowTooltips, settingsInterface.TooltipCanvas, settingsInterface.UIScale);
			}

			if (m_HelpTips != null)
			{
				ProcessHelpTooltips(m_HelpTips.isOn);
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

			((SettingsPage)CurrentPage).OnPointerDown(eventData);
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

			if (settingsInterface == null)
			{
				return;
			}

			settingsInterface.ClampToScreen(rect);
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			if (rect == null || settingsInterface == null)
			{
				return;
			}

			settingsInterface.Position = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y);
		}

		public void KSPedia(bool isOn)
		{
			if (settingsInterface != null)
			{
				settingsInterface.OpenKSPedia(isOn);
			}
		}

		public void HelpTips(bool isOn)
		{
			ProcessHelpTooltips(isOn);
		}

		public void ProcessHelpTooltips(bool isOn)
		{
			if (settingsInterface == null)
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
				ProcessHelpTooltip(handlers[j], isOn, settingsInterface.TooltipCanvas);
			}
		}

		private void ProcessHelpTooltip(TooltipHandler handler, bool isOn, Canvas c)
		{
			if (handler == null)
			{
				return;
			}

			handler.IsActive = isOn && handler.HelpTip;
			handler._Canvas = c;
		}

		public void GeneralSettings(bool isOn)
		{
			if (!isOn)
			{
				return;
			}

			if (CurrentPage != null)
			{
				CurrentPage.gameObject.SetActive(false);
				DestroyImmediate(CurrentPage.gameObject);
			}

			if (m_GeneralPrefab == null || m_ContentTransform == null || settingsInterface == null)
			{
				return;
			}

			if (settingsInterface.LockInput)
			{
				settingsInterface.LockInput = false;
			}

			CurrentPage = Instantiate(m_GeneralPrefab).GetComponent<SettingsPage>();

			if (CurrentPage == null)
			{
				return;
			}

			_page = 0;

			CurrentPage.transform.SetParent(m_ContentTransform, false);

			((SCAN_SettingsGeneral)CurrentPage).setup(settingsInterface);

			ProcessTooltips();

			ClearWarningsAndDropDown();
		}

		public void BackgroundSettings(bool isOn)
		{
			if (!isOn)
			{
				return;
			}

			if (CurrentPage != null)
			{
				CurrentPage.gameObject.SetActive(false);
				DestroyImmediate(CurrentPage.gameObject);
			}

			if (m_BackgroundPrefab == null || m_ContentTransform == null || settingsInterface == null)
			{
				return;
			}

			if (settingsInterface.LockInput)
			{
				settingsInterface.LockInput = false;
			}

			CurrentPage = Instantiate(m_BackgroundPrefab).GetComponent<SettingsPage>();

			if (CurrentPage == null)
			{
				return;
			}

			_page = 1;

			CurrentPage.transform.SetParent(m_ContentTransform, false);

			((SCAN_SettingsBackground)CurrentPage).setup(settingsInterface);

			ProcessTooltips();

			ClearWarningsAndDropDown();
		}

		public void ResourceSettings(bool isOn)
		{
			if (!isOn)
			{
				return;
			}

			if (CurrentPage != null)
			{
				CurrentPage.gameObject.SetActive(false);
				DestroyImmediate(CurrentPage.gameObject);
			}

			if (m_ResourcePrefab == null || m_ContentTransform == null || settingsInterface == null)
			{
				return;
			}

			if (settingsInterface.LockInput)
			{
				settingsInterface.LockInput = false;
			}

			CurrentPage = Instantiate(m_ResourcePrefab).GetComponent<SettingsPage>();

			if (CurrentPage == null)
			{
				return;
			}

			_page = 2;

			CurrentPage.transform.SetParent(m_ContentTransform, false);

			((SCAN_SettingsResource)CurrentPage).setup(settingsInterface);

			ProcessTooltips();

			ClearWarningsAndDropDown();
		}

		public void DataSettings(bool isOn)
		{
			if (!isOn)
			{
				return;
			}

			if (CurrentPage != null)
			{
				CurrentPage.gameObject.SetActive(false);
				DestroyImmediate(CurrentPage.gameObject);
			}

			if (m_DataPrefab == null || m_ContentTransform == null || settingsInterface == null)
			{
				return;
			}

			if (settingsInterface.LockInput)
			{
				settingsInterface.LockInput = false;
			}

			CurrentPage = Instantiate(m_DataPrefab).GetComponent<SettingsPage>();

			if (CurrentPage == null)
			{
				return;
			}

			_page = 3;

			CurrentPage.transform.SetParent(m_ContentTransform, false);

			((SCAN_SettingsData)CurrentPage).setup(settingsInterface);

			ProcessTooltips();

			ClearWarningsAndDropDown();
		}

		public void ColorSettings(bool isOn)
		{
			if (!isOn)
			{
				return;
			}

			if (CurrentPage != null)
			{
				CurrentPage.gameObject.SetActive(false);
				DestroyImmediate(CurrentPage.gameObject);
			}

			if (m_ColorPrefab == null || m_ContentTransform == null || settingsInterface == null)
			{
				return;
			}

			if (settingsInterface.LockInput)
			{
				settingsInterface.LockInput = false;
			}

			CurrentPage = Instantiate(m_ColorPrefab).GetComponent<SettingsPage>();

			if (CurrentPage == null)
			{
				return;
			}

			_page = 4;

			CurrentPage.transform.SetParent(m_ContentTransform, false);

			((SCAN_ColorControl)CurrentPage).setup(settingsInterface, settingsInterface.ColorInterface, _forceResource);

			ProcessTooltips();

			ClearWarningsAndDropDown();
		}

	}
}
