#region license
/*
 *  [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANtoolbar -	optional integration with Blizzy's toolvar
 *
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 *
 * Created by David to allow the SCANsat plugin to function through the toolbar interface
 */
#endregion

using System.IO;
using UnityEngine;
using SCANsat.SCAN_Platform;
using SCANsat.SCAN_Unity;

namespace SCANsat.SCAN_Toolbar
{
	[KSPAddon(KSPAddon.Startup.AllGameScenes, false)]
	public class SCANtoolbar : MonoBehaviour
	{
		private IButton SCANButton;
		private IButton MapButton;
		private IButton SmallButton;
		private IButton OverlayButton;
		private IButton KSCButton;
		private IButton ZoomButton;

		public SCANtoolbar()
		{
			if (!ToolbarManager.ToolbarAvailable)
			{
				Destroy(gameObject); // bail if we don't have a toolbar
				return;
			}

			if (HighLogic.LoadedSceneIsFlight)
			{
				SCANButton = ToolbarManager.Instance.add("SCANsat", "UIMenu");
				MapButton = ToolbarManager.Instance.add("SCANsat", "BigMap");
				SmallButton = ToolbarManager.Instance.add("SCANsat", "SmallMap");
				OverlayButton = ToolbarManager.Instance.add("SCANsat", "Overlay");
				ZoomButton = ToolbarManager.Instance.add("SCANsat", "ZoomMap");

				//Fall back to some default toolbar icons if someone deletes the SCANsat icons or puts them in the wrong folder
				if (File.Exists(Path.Combine(new DirectoryInfo(KSPUtil.ApplicationRootPath).FullName, "GameData/SCANsat/Icons/SCANsat_Icon.png").Replace("\\", "/")))
					SCANButton.TexturePath = "SCANsat/Icons/SCANsat_Icon"; // S.C.A.N
				else
					SCANButton.TexturePath = "000_Toolbar/toolbar-dropdown";
				if (File.Exists(Path.Combine(new DirectoryInfo(KSPUtil.ApplicationRootPath).FullName, "GameData/SCANsat/Icons/SCANsat_Map_Icon.png").Replace("\\", "/")))
					MapButton.TexturePath = "SCANsat/Icons/SCANsat_Map_Icon"; // from in-game biome map of Kerbin
				else
					MapButton.TexturePath = "000_Toolbar/move-cursor";
				if (File.Exists(Path.Combine(new DirectoryInfo(KSPUtil.ApplicationRootPath).FullName, "GameData/SCANsat/Icons/SCANsat_SmallMap_Icon.png").Replace("\\", "/")))
					SmallButton.TexturePath = "SCANsat/Icons/SCANsat_SmallMap_Icon"; // from unity, edited by DG
				else
					SmallButton.TexturePath = "000_Toolbar/resize-cursor";
				if (File.Exists(Path.Combine(new DirectoryInfo(KSPUtil.ApplicationRootPath).FullName, "GameData/SCANsat/Icons/SCANsat_OverlayToolbar_Icon.png").Replace("\\", "/")))
					OverlayButton.TexturePath = "SCANsat/Icons/SCANsat_OverlayToolbar_Icon";
				else
					OverlayButton.TexturePath = "000_Toolbar/resize-cursor";
				if (File.Exists(Path.Combine(new DirectoryInfo(KSPUtil.ApplicationRootPath).FullName, "GameData/SCANsat/Icons/SCANsat_ZoomToolbar_Icon.png").Replace("\\", "/")))
					ZoomButton.TexturePath = "SCANsat/Icons/SCANsat_ZoomToolbar_Icon";
				else
					ZoomButton.TexturePath = "000_Toolbar/resize-cursor";

				SCANButton.ToolTip = "SCANsat";
				MapButton.ToolTip = "SCANsat Big Map";
				SmallButton.ToolTip = "SCANsat Small Map";
				OverlayButton.ToolTip = "SCANsat Overlay Controller";
				ZoomButton.ToolTip = "SCANsat Zoom Map";

				SCANButton.OnClick += (e) =>
					{
						if (SCANcontroller.controller != null)
						{
							toggleMenu(SCANButton);
						}
					};
				MapButton.OnClick += (e) =>
					{
						if (SCAN_UI_BigMap.Instance != null)
						{
							if (SCAN_UI_BigMap.Instance.IsVisible)
								SCAN_UI_BigMap.Instance.Close();
							else
								SCAN_UI_BigMap.Instance.Open();
						}
					};
				SmallButton.OnClick += (e) =>
					{
						if (SCAN_UI_MainMap.Instance != null)
						{
							if (SCAN_UI_MainMap.Instance.IsVisible)
								SCAN_UI_MainMap.Instance.Close();
							else
								SCAN_UI_MainMap.Instance.Open();
						}
					};
				OverlayButton.OnClick += (e) =>
					{
						if (SCAN_UI_Overlay.Instance != null)
						{
							if (SCAN_UI_Overlay.Instance.IsVisible)
								SCAN_UI_Overlay.Instance.Close();
							else
								SCAN_UI_Overlay.Instance.Open();
						}
					};
				ZoomButton.OnClick += (e) =>
					{
						if (SCAN_UI_ZoomMap.Instance != null)
						{
							if (SCAN_UI_ZoomMap.Instance.IsVisible)
								SCAN_UI_ZoomMap.Instance.Close();
							else
								SCAN_UI_ZoomMap.Instance.Open(true);
						}
					};
			}
			else if (HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION)
			{
				KSCButton = ToolbarManager.Instance.add("SCANsat", "KSCMap");

				if (File.Exists(Path.Combine(new DirectoryInfo(KSPUtil.ApplicationRootPath).FullName, "GameData/SCANsat/Icons/SCANsat_Map_Icon.png").Replace("\\", "/")))
					KSCButton.TexturePath = "SCANsat/Icons/SCANsat_Map_Icon"; // from in-game biome map of Kerbin
				else
					KSCButton.TexturePath = "000_Toolbar/move-cursor";

				KSCButton.ToolTip = "SCANsat KSC Map";

				KSCButton.OnClick += (e) =>
					{
						if (SCAN_UI_BigMap.Instance != null)
						{
							if (SCAN_UI_BigMap.Instance.IsVisible)
								SCAN_UI_BigMap.Instance.Close();
							else
								SCAN_UI_BigMap.Instance.Open();
						}
					};
			}
			else
				Destroy(gameObject);
		}

		private void toggleMenu(IButton menu)
		{
			if (menu.Drawable == null)
				createMenu(menu);
			else
				destroyMenu(menu);
		}

		private void createMenu(IButton menu)
		{
			if (!ToolbarManager.ToolbarAvailable) return; // bail if we don't have a toolbar

			PopupMenuDrawable list = new PopupMenuDrawable();

			IButton smallMap = list.AddOption("Small Map");
			IButton instrument = list.AddOption("Instruments");
			IButton bigMap = list.AddOption("Big Map");
			IButton zoomMap = list.AddOption("Zoom Map");
			IButton settings = list.AddOption("Settings");
			IButton resource = list.AddOption("Planetary Overlay");

			smallMap.OnClick += (e2) =>
				{
					if (SCAN_UI_MainMap.Instance != null)
					{
						if (SCAN_UI_MainMap.Instance.IsVisible)
							SCAN_UI_MainMap.Instance.Close();
						else
							SCAN_UI_MainMap.Instance.Open();
					}
				};
			instrument.OnClick += (e2) =>
				{
					if (SCAN_UI_Instruments.Instance != null)
					{
						if (SCAN_UI_Instruments.Instance.IsVisible)
							SCAN_UI_Instruments.Instance.Close();
						else
							SCAN_UI_Instruments.Instance.Open();
					}
				};
			bigMap.OnClick += (e2) =>
				{
					if (SCAN_UI_BigMap.Instance != null)
					{
						if (SCAN_UI_BigMap.Instance.IsVisible)
							SCAN_UI_BigMap.Instance.Close();
						else
							SCAN_UI_BigMap.Instance.Open();
					}
				};
			zoomMap.OnClick += (e2) =>
				{
					if (SCAN_UI_ZoomMap.Instance != null)
					{
						if (SCAN_UI_ZoomMap.Instance.IsVisible)
							SCAN_UI_ZoomMap.Instance.Close();
						else
							SCAN_UI_ZoomMap.Instance.Open(true);
					}
				};
			settings.OnClick += (e2) =>
				{
					if (SCAN_UI_Settings.Instance != null)
					{
						if (SCAN_UI_Settings.Instance.IsVisible)
							SCAN_UI_Settings.Instance.Close();
						else
							SCAN_UI_Settings.Instance.Open();
					}
				};
			resource.OnClick += (e2) =>
				{
					if (SCAN_UI_Overlay.Instance != null)
					{
						if (SCAN_UI_Overlay.Instance.IsVisible)
							SCAN_UI_Overlay.Instance.Close();
						else
							SCAN_UI_Overlay.Instance.Open();
					}
				};
			list.OnAnyOptionClicked += () => destroyMenu(menu);
			menu.Drawable = list;
		}

		private void destroyMenu(IButton menu)
		{
			((PopupMenuDrawable)menu.Drawable).Destroy();
			menu.Drawable = null;
		}

		internal void OnDestroy()
		{
			if (SCANButton != null)
				SCANButton.Destroy();
			if (MapButton != null)
				MapButton.Destroy();
			if (SmallButton != null)
				SmallButton.Destroy();
			if (KSCButton != null)
				KSCButton.Destroy();
			if (OverlayButton != null)
				OverlayButton.Destroy();
			if (ZoomButton != null)
				ZoomButton.Destroy();
		}

	}
}
