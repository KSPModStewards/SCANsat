#region license
/*
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 * 
 * SCANappLauncher - A class to initialize and destroy the stock app launcher button
 *
 * Copyright (c)2013 damny;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
*/
#endregion

using System.Collections;
using SCANsat.SCAN_Platform;
using SCANsat.SCAN_UI;
using SCANsat.SCAN_UI.UI_Framework;
using SCANsat.SCAN_Unity;
using SCANsat.Unity.Unity;
using SCANsat.Unity.Interfaces;
using KSP.UI;
using KSP.UI.Screens;
using UnityEngine;

namespace SCANsat.SCAN_Toolbar
{
	public class SCANappLauncher : MonoBehaviour, ISCAN_Toolbar
	{
		private ApplicationLauncherButton SCANappLauncherButton = null;
		private bool _inMenu;
		private bool _sticky;
		private bool _hovering;
		private SCANsat.Unity.Unity.SCAN_Toolbar uiElement;
		private static SCANappLauncher instance;

		public static SCANappLauncher Instance
		{
			get { return instance; }
		}

		public SCANsat.Unity.Unity.SCAN_Toolbar UIElement
		{
			get { return uiElement; }
		}

		public ApplicationLauncherButton SCANAppButton
		{
			get { return SCANappLauncherButton; }
		}

		public Canvas TooltipCanvas
		{
			get { return UIMasterController.Instance.tooltipCanvas; }
		}

		public bool TooltipsOn
		{
			get { return SCAN_Settings_Config.Instance.WindowTooltips; }
		}

		public bool IsVisible
		{
			get { return _sticky && uiElement != null; }
		}

		public float Scale
		{
			get { return GameSettings.UI_SCALE_APPS; }
		}

		public void ProcessTooltips()
		{
			if (uiElement != null)
				uiElement.ProcessTooltips();
		}

		private void Start()
		{
			instance = this;

			setupToolbar();

			GameEvents.OnGameSettingsApplied.Add(settingsApplied);
		}

		public void ToggleToolbarType()
		{
			removeButton(HighLogic.LoadedScene);

			StartCoroutine(addButton());
		}

		private void OnDestroy()
		{
			GameEvents.onGUIApplicationLauncherUnreadifying.Remove(removeButton);

			if (SCANappLauncherButton != null)
				removeButton(HighLogic.LoadedScene);

			GameEvents.OnGameSettingsApplied.Remove(settingsApplied);
		}

		private void settingsApplied()
		{
			if (!_sticky || uiElement == null)
				return;

			uiElement.gameObject.SetActive(false);
			DestroyImmediate(uiElement.gameObject);

			OpenMenu();
		}

		private void setupToolbar()
		{
			StartCoroutine(addButton());
		}

		IEnumerator addButton()
		{
			while (!ApplicationLauncher.Ready)
				yield return null;

			if (HighLogic.LoadedScene == GameScenes.FLIGHT)
			{
				if (SCAN_Settings_Config.Instance.ToolbarMenu)
				{
					SCANappLauncherButton = ApplicationLauncher.Instance.AddModApplication(OnTrue, OnFalse, HoverMenuIn, HoverMenuOut, null, null, ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW, SCAN_UI_Loader.SmallMapAppIcon.texture);
					ApplicationLauncher.Instance.EnableMutuallyExclusive(SCANappLauncherButton);
				}
				else
				{
					SCANappLauncherButton = ApplicationLauncher.Instance.AddModApplication(toggleFlight, toggleFlight, null, null, null, null, ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW, SCAN_UI_Loader.SmallMapAppIcon.texture);

					if (SCAN_UI_MainMap.Instance.IsVisible)
						SCANappLauncherButton.SetTrue(false);
				}
			}
			else if (HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION)
			{
				SCANappLauncherButton = ApplicationLauncher.Instance.AddModApplication(toggleKSC, toggleKSC, null, null, null, null, ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.TRACKSTATION, SCAN_UI_Loader.BigMapAppIcon.texture);
			}

			GameEvents.onGUIApplicationLauncherUnreadifying.Add(removeButton);
		}

		private void removeButton(GameScenes scene)
		{
			if (uiElement != null)
			{
				uiElement.gameObject.SetActive(false);
				Destroy(uiElement.gameObject);
			}

			if (SCANappLauncherButton != null)
			{
				ApplicationLauncher.Instance.RemoveModApplication(SCANappLauncherButton);
				SCANappLauncherButton = null;
			}
		}

		private void toggleFlight()
		{
			if (SCAN_UI_MainMap.Instance.IsVisible)
				SCAN_UI_MainMap.Instance.Close();
			else
				SCAN_UI_MainMap.Instance.Open();
		}

		private void toggleKSC()
		{
			if (SCAN_UI_BigMap.Instance.IsVisible)
				SCAN_UI_BigMap.Instance.Close();
			else
				SCAN_UI_BigMap.Instance.Open();
		}

		private void OnTrue()
		{
			_sticky = true;

			if (uiElement == null)
				OpenMenu();
		}

		private void OnFalse()
		{
			_sticky = false;

			CloseMenu();

			uiElement = null;
		}

		private void HoverMenuIn()
		{
			_hovering = true;

			if (_sticky || uiElement != null)
				return;

			OpenMenu();
		}

		private void HoverMenuOut()
		{
			_hovering = false;

			if (!_sticky)
				StartCoroutine(HoverOutWait());
		}

		private IEnumerator HoverOutWait()
		{
			int timer = 0;

			while (timer < 2)
			{
				timer++;
				yield return null;
			}

			if (!_inMenu)
				CloseMenu();
		}

		public Vector3 GetAnchor()
		{
			if (SCANappLauncherButton == null)
				return Vector3.zero;

			Vector3 anchor = SCANappLauncherButton.GetAnchor();

			anchor.x -= 3;
			anchor.y += 20;

			return anchor;
		}

		private void OpenMenu()
		{
			if (SCAN_UI_Loader.ToolbarPrefab == null)
				return;

			uiElement = (Instantiate(SCAN_UI_Loader.ToolbarPrefab, GetAnchor(), Quaternion.identity) as GameObject).GetComponent<SCANsat.Unity.Unity.SCAN_Toolbar>();
			
			if (uiElement == null)
				return;

			uiElement.transform.SetParent(UIMasterController.Instance.appCanvas.transform);

			uiElement.Setup(this);
		}

		private void CloseMenu()
		{
			if (uiElement != null)
				uiElement.FadeOut();
		}

		private IEnumerator MenuHoverOutWait()
		{
			int timer = 0;

			while (timer < 2)
			{
				timer++;
				yield return null;
			}

			if (!_hovering && !_sticky)
				CloseMenu();
		}

		public bool InMenu
		{
			get { return _inMenu; }
			set
			{
				_inMenu = value;

				if (!value)
					StartCoroutine(MenuHoverOutWait());
			}
		}

		public bool MainMap
		{
			get { return SCAN_UI_MainMap.Instance.IsVisible; }
			set
			{
				if (value)
					SCAN_UI_MainMap.Instance.Open();
				else
					SCAN_UI_MainMap.Instance.Close();
			}
		}

		public bool BigMap
		{
			get { return SCAN_UI_BigMap.Instance.IsVisible; }
			set
			{
				if (value)
					SCAN_UI_BigMap.Instance.Open();
				else
					SCAN_UI_BigMap.Instance.Close();
			}
		}

		public bool ZoomMap
		{
			get { return SCAN_UI_ZoomMap.Instance.IsVisible; }
			set
			{
				if (value)
					SCAN_UI_ZoomMap.Instance.Open(true);
				else
					SCAN_UI_ZoomMap.Instance.Close();
			}
		}

		public bool Overlay
		{
			get { return SCAN_UI_Overlay.Instance.IsVisible; }
			set
			{
				if (value)
					SCAN_UI_Overlay.Instance.Open();
				else
					SCAN_UI_Overlay.Instance.Close();
			}
		}

		public bool Instruments
		{
			get { return SCAN_UI_Instruments.Instance.IsVisible; }
			set
			{
				if (value)
					SCAN_UI_Instruments.Instance.Open();
				else
					SCAN_UI_Instruments.Instance.Close();
			}
		}

		public bool Settings
		{
			get { return SCAN_UI_Settings.Instance.IsVisible; }
			set
			{
				if (value)
					SCAN_UI_Settings.Instance.Open();
				else
					SCAN_UI_Settings.Instance.Close();
			}
		}
	}
}
