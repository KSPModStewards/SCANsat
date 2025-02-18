#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_UI_Loader - Script for loading in all UI objects
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SCANsat.Unity;
using SCANsat.Unity.Unity;
using SCANsat.SCAN_Platform.Palettes;
using KSP.UI;
using KSP.UI.Screens;
using KSP.Localization;
using TMPro;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANpalette;

namespace SCANsat.SCAN_Unity
{
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	public class SCAN_UI_Loader : MonoBehaviour
	{
		private const string prefabAssetName = "scan_prefabs.scan";
		private const string unitySkinAssetName = "scan_unity_skin.scan";
		private const string iconAssetName = "scan_icons.scan";
		private const string shadersAssetName = "scan_shaders";
		private const string winShaderName = "-windows.scan";
		private const string linuxShaderName = "-linux.scan";
		private const string macShaderName = "-macosx.scan";

		private static bool loaded;
		private static bool skinLoaded;
		private static bool prefabsLoaded;
		private static bool spritesLoaded;
		private static bool iconsLoaded;
		private static bool shadersLoaded;
		private static bool tmpProcessed;
		private static bool tmpInputProcessed;
		private static bool tooltipsProcessed;
		private static bool prefabsProcessed;
		private static bool clearLoaded;
		private static bool toggleLoaded;
		private static bool appLoaded;

		private static UISkinDef _unitySkinDef;

		private static Sprite _podIcon;
		private static Sprite _planeIcon;
		private static Sprite _probeIcon;
		private static Sprite _debrisIcon;
		private static Sprite _baseIcon;
		private static Sprite _stationIcon;
		private static Sprite _landerIcon;
		private static Sprite _roverIcon;
		private static Sprite _relayIcon;
		private static Sprite _asteroidIcon;
		private static Sprite _evaIcon;
		private static Sprite _planetIcon;
		private static Sprite _mysteryIcon;
		private static Sprite _flagIcon;
		private static Sprite _apMarker;
		private static Sprite _peMarker;
		private static Sprite _maneuverMarker;
		private static Sprite _encounterMarker;
		private static Sprite _exitMarker;
		private static Sprite _anomalyIcon;
		private static Sprite _waypointIcon;

		private static Sprite _clearSprite;
		private static Sprite _toggleNormal;
		private static Sprite _toggleHover;
		private static Sprite _toggleActive;
		private static Sprite _toggleOn;
		private static Sprite _toggleOnHover;
		private static Sprite _toggleOnActive;
		private static Sprite _unityToggleOnHover;

		private static Sprite _appButtonNormal;
		private static Sprite _appButtonHover;
		private static Sprite _appButtonActive;
		private static Sprite _appButtonDisabled;
		private static Sprite _appBackground;

		private static Sprite _kspTooltipBackground;
		private static Sprite _unityTooltipBackground;

		private static Sprite _smallMapAppIcon;
		private static Sprite _bigMapAppIcon;

		private static Sprite _mechJebIcon;

		private static Shader _edgeDetectShader;
		private static Shader _greyScaleShader;

		private static GameObject[] loadedPrefabs;

		private static GameObject _mainMapPrefab;
		private static GameObject _bigMapPrefab;
		private static GameObject _zoomMapPrefab;
		private static GameObject _instrumentsPrefab;
		private static GameObject _overlayPrefab;
		private static GameObject _settingsPrefab;
		private static GameObject _toolbarPrefab;
		private static GameObject _tooltipPrefab;

		public static GameObject MainMapPrefab
		{
			get { return _mainMapPrefab; }
		}

		public static GameObject BigMapPrefab
		{
			get { return _bigMapPrefab; }
		}

		public static GameObject ZoomMapPrefab
		{
			get { return _zoomMapPrefab; }
		}

		public static GameObject InstrumentsPrefab
		{
			get { return _instrumentsPrefab; }
		}

		public static GameObject OverlayPrefab
		{
			get { return _overlayPrefab; }
		}

		public static GameObject SettingsPrefab
		{
			get { return _settingsPrefab; }
		}

		public static GameObject ToolbarPrefab
		{
			get { return _toolbarPrefab; }
		}

		public static UISkinDef UnitySkinDef
		{
			get { return _unitySkinDef; }
		}

		public static Sprite PodIcon
		{
			get { return _podIcon; }
		}

		public static Sprite PlaneIcon
		{
			get { return _planeIcon; }
		}

		public static Sprite ProbeIcon
		{
			get { return _probeIcon; }
		}

		public static Sprite DebrisIcon
		{
			get { return _debrisIcon; }
		}

		public static Sprite BaseIcon
		{
			get { return _baseIcon; }
		}

		public static Sprite StationIcon
		{
			get { return _stationIcon; }
		}

		public static Sprite LanderIcon
		{
			get { return _landerIcon; }
		}

		public static Sprite RoverIcon
		{
			get { return _roverIcon; }
		}

		public static Sprite RelayIcon
		{
			get { return _relayIcon; }
		}

		public static Sprite AsteroidIcon
		{
			get { return _asteroidIcon; }
		}

		public static Sprite EVAIcon
		{
			get { return _evaIcon; }
		}

		public static Sprite PlanetIcon
		{
			get { return _planetIcon; }
		}

		public static Sprite MysteryIcon
		{
			get { return _mysteryIcon; }
		}

		public static Sprite FlagIcon
		{
			get { return _flagIcon; }
		}

		public static Sprite APMarker
		{
			get { return _apMarker; }
		}

		public static Sprite PEMarker
		{
			get { return _peMarker; }
		}

		public static Sprite ManeuverMarker
		{
			get { return _maneuverMarker; }
		}

		public static Sprite EncounterMarker
		{
			get { return _encounterMarker; }
		}

		public static Sprite ExitMarker
		{
			get { return _exitMarker; }
		}

		public static Sprite AnomalyIcon
		{
			get { return _anomalyIcon; }
		}

		public static Sprite WaypointIcon
		{
			get { return _waypointIcon; }
		}

		public static Sprite VesselIcon(VesselType type)
		{
			switch(type)
			{
				case VesselType.Base:
					return _baseIcon;
				case VesselType.Debris:
					return _debrisIcon;
				case VesselType.EVA:
					return _evaIcon;
				case VesselType.Flag:
					return _flagIcon;
				case VesselType.Lander:
					return _landerIcon;
				case VesselType.Plane:
					return _planeIcon;
				case VesselType.Probe:
					return _probeIcon;
				case VesselType.Relay:
					return _relayIcon;
				case VesselType.Rover:
					return _roverIcon;
				case VesselType.Ship:
					return _podIcon;
				case VesselType.SpaceObject:
					return _asteroidIcon;
				case VesselType.Station:
					return _stationIcon;
				case VesselType.Unknown:
					return _mysteryIcon;
				default:
					return _mysteryIcon;
			}
		}

		public static Sprite SmallMapAppIcon
		{
			get { return _smallMapAppIcon; }
		}

		public static Sprite BigMapAppIcon
		{
			get { return _bigMapAppIcon; }
		}

		public static Sprite MechJebIcon
		{
			get { return _mechJebIcon; }
		}

		public static Shader EdgeDetectShader
		{
			get {return _edgeDetectShader;}
		}

		public static Shader GreyScaleShader
		{
			get { return _greyScaleShader; }
		}

		public static void ResetUIStyle()
		{
			if (loadedPrefabs != null)
				processUIPrefabs();
		}

		private static string path;

		private void Awake()
		{
			if (loaded)
			{
				Destroy(gameObject);
				return;
			}

			path = KSPUtil.ApplicationRootPath + "GameData/SCANsat/Resources/";

			SCANUtil.SCANlog("Processing SCANsat asset bundles...");

			StartCoroutine(loadResources());
		}

		private IEnumerator loadResources()
		{
			while (ApplicationLauncher.Instance == null)
				yield return null;

			while (SCAN_Settings_Config.Instance == null)
				yield return null;

			if (!shadersLoaded)
				loadShaders();

			if (!spritesLoaded)
				loadTextures();

			if (!skinLoaded)
				loadUnitySkin();

			if (!iconsLoaded)
				loadIcons();

			if (!prefabsLoaded)
				loadPrefabBundle();

			palette.CurrentPalettes = palette.setCurrentPalettesType(Palette.Kind.Diverging, 7);

			if (shadersLoaded && spritesLoaded && skinLoaded && iconsLoaded && prefabsLoaded)
				SCANUtil.SCANlog("All SCANsat asset bundles loaded");
			else
				SCANUtil.SCANlog("Error in loading SCANsat asset bundles\nSome UI elements may be non-functional");

			loaded = true;
		}

		private static void loadShaders()
		{
			string shaderPath;

			if (Application.platform == RuntimePlatform.WindowsPlayer && SystemInfo.graphicsDeviceVersion.StartsWith("OpenGL"))
				shaderPath = shadersAssetName + linuxShaderName;
			else if (Application.platform == RuntimePlatform.WindowsPlayer)
				shaderPath = shadersAssetName + winShaderName;
			else if (Application.platform == RuntimePlatform.LinuxPlayer)
				shaderPath = shadersAssetName + linuxShaderName;
			else
				shaderPath = shadersAssetName + macShaderName;

			AssetBundle shaders = AssetBundle.LoadFromFile(path + shaderPath);

			if (shaders == null)
				return;

			Shader[] loadedShaders = shaders.LoadAllAssets<Shader>();

			if (loadedShaders == null)
				return;

			for (int i = loadedShaders.Length - 1; i >= 0; i--)
			{
				Shader s = loadedShaders[i];

				if (s.name == "Hidden/Edge Detect X")
					_edgeDetectShader = s;
				else if (s.name == "Hidden/Grayscale Effect")
					_greyScaleShader = s;
			}

			SCANUtil.SCANlog("Shader asset bundle loaded; using platform bundle: {0}", shaderPath);

			//shaders.Unload(false);

			shadersLoaded = true;
		}

		private static void loadTextures()
		{
			if (!clearLoaded)
			{
				Texture2D clear = new Texture2D(1, 1);
				clear.SetPixel(1, 1, palette.clear);
				clear.Apply();

				_clearSprite = Sprite.Create(clear, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));

				clearLoaded = true;
			}

			if (!toggleLoaded)
			{
				GUISkin skin = HighLogic.Skin;

				if (skin == null)
					return;

				_toggleNormal = Sprite.Create(skin.toggle.normal.background, new Rect(16, 16, skin.toggle.normal.background.width - 32, skin.toggle.normal.background.height - 32), new Vector2(0.5f, 0.5f));
				_toggleHover = Sprite.Create(skin.toggle.hover.background, new Rect(16, 16, skin.toggle.hover.background.width - 32, skin.toggle.hover.background.height - 32), new Vector2(0.5f, 0.5f));
				_toggleActive = Sprite.Create(skin.toggle.active.background, new Rect(16, 16, skin.toggle.active.background.width - 32, skin.toggle.active.background.height - 32), new Vector2(0.5f, 0.5f));
				_toggleOn = Sprite.Create(skin.toggle.onNormal.background, new Rect(16, 16, skin.toggle.onNormal.background.width - 32, skin.toggle.onNormal.background.height - 32), new Vector2(0.5f, 0.5f));
				_toggleOnHover = Sprite.Create(skin.toggle.onHover.background, new Rect(16, 16, skin.toggle.onHover.background.width - 32, skin.toggle.onHover.background.height - 32), new Vector2(0.5f, 0.5f));
				_toggleOnActive = Sprite.Create(skin.toggle.onActive.background, new Rect(16, 16, skin.toggle.onActive.background.width - 32, skin.toggle.onActive.background.height - 32), new Vector2(0.5f, 0.5f));
				
				toggleLoaded = true;
			}

			if (!appLoaded)
			{
				if (ApplicationLauncher.Instance != null)
				{
					UIRadioButton button = ApplicationLauncher.Instance.listItemPrefab.toggleButton;

					_appButtonNormal = button.stateFalse.normal;
					_appButtonHover = button.stateTrue.highlight;
					_appButtonActive = button.stateTrue.pressed;
					_appButtonDisabled = button.stateTrue.disabled;

					Image background = ApplicationLauncher.Instance.listItemPrefab.GetComponent<Image>();

					if (background != null)
						_appBackground = background.sprite;

					appLoaded = true;
				}
			}

			if (clearLoaded && toggleLoaded && appLoaded)
				spritesLoaded = true;
		}

		private static void loadUnitySkin()
		{
			_unitySkinDef = new UISkinDef();

			SkinInit(_unitySkinDef);

			AssetBundle skin = AssetBundle.LoadFromFile(path + unitySkinAssetName);

			if (skin == null)
				return;

			Sprite[] skinSprites = skin.LoadAllAssets<Sprite>();

			if (skinSprites == null)
				return;

			for (int i = skinSprites.Length - 1; i >= 0; i--)
			{
				Sprite s = skinSprites[i];

				if (s.name == "window")
					_unitySkinDef.window.normal.background = s;
				else if (s.name == "box")
					_unitySkinDef.box.normal.background = s;
				else if (s.name == "button")
					_unitySkinDef.button.normal.background = s;
				else if (s.name == "button hover")
					_unitySkinDef.button.highlight.background = s;
				else if (s.name == "button on")
					_unitySkinDef.button.active.background = s;
				else if (s.name == "toggle_border")
					_unitySkinDef.toggle.normal.background = s;
				else if (s.name == "SCAN_Toggle_Hover_Border")
					_unitySkinDef.toggle.highlight.background = s;
				else if (s.name == "toggle active_border")
					_unitySkinDef.toggle.active.background = s;
				else if (s.name == "SCAN_Toggle_Border")
					_unitySkinDef.toggle.disabled.background = s;
				else if (s.name == "SCAN_Toggle_On_Hover_Border")
					_unityToggleOnHover = s;
				else if (s.name == "textfield")
					_unitySkinDef.textField.normal.background = s;
				else if (s.name == "textfield hover")
					_unitySkinDef.textField.highlight.background = s;
				else if (s.name == "textfield on")
					_unitySkinDef.textField.active.background = s;
				else if (s.name == "horizontal scrollbar")
					_unitySkinDef.horizontalScrollbar.normal.background = s;
				else if (s.name == "horizontal scrollbar thumb")
					_unitySkinDef.horizontalScrollbarThumb.normal.background = s;
				else if (s.name == "vertical scrollbar")
					_unitySkinDef.verticalScrollbar.normal.background = s;
				else if (s.name == "vertical scrollbar thumb")
					_unitySkinDef.verticalScrollbarThumb.normal.background = s;
				else if (s.name == "horizontalslider")
					_unitySkinDef.horizontalSlider.normal.background = s;
				else if (s.name == "slider thumb")
					_unitySkinDef.horizontalSliderThumb.normal.background = s;
				else if (s.name == "slider thumb hover")
					_unitySkinDef.horizontalSliderThumb.highlight.background = s;
				else if (s.name == "slider thumb active")
					_unitySkinDef.horizontalSliderThumb.active.background = s;
				else if (s.name == "verticalslider")
					_unitySkinDef.verticalSlider.normal.background = s;
				else if (s.name == "slider thumb")
					_unitySkinDef.verticalSliderThumb.normal.background = s;
				else if (s.name == "slider thumb hover")
					_unitySkinDef.verticalSliderThumb.highlight.background = s;
				else if (s.name == "slider thumb active")
					_unitySkinDef.verticalSliderThumb.active.background = s;
				else if (s.name == "tooltip")
					_unityTooltipBackground = s;
			}

			SCANUtil.SCANlog("Unity skin asset bundle loaded");

			//images.Unload(false);

			skinLoaded = true;
		}

		private static void SkinInit (UISkinDef skin)
		{
			skin.window = new UIStyle();
			skin.box = new UIStyle();
			skin.button = new UIStyle();
			skin.toggle = new UIStyle();
			skin.textField = new UIStyle();
			skin.horizontalScrollbar = new UIStyle();
			skin.horizontalScrollbarThumb = new UIStyle();
			skin.verticalScrollbar = new UIStyle();
			skin.verticalScrollbarThumb = new UIStyle();
			skin.horizontalSlider = new UIStyle();
			skin.horizontalSliderThumb = new UIStyle();
			skin.verticalSlider = new UIStyle();
			skin.verticalSliderThumb = new UIStyle();

			skin.window.normal = new UIStyleState();
			skin.box.normal = new UIStyleState();
			skin.button.normal = new UIStyleState();
			skin.button.highlight = new UIStyleState();
			skin.button.active = new UIStyleState();
			skin.toggle.normal = new UIStyleState();
			skin.toggle.highlight = new UIStyleState();
			skin.toggle.active = new UIStyleState();
			skin.toggle.disabled = new UIStyleState();
			skin.textField.normal = new UIStyleState();
			skin.textField.highlight = new UIStyleState();
			skin.textField.active = new UIStyleState();
			skin.horizontalScrollbar.normal = new UIStyleState();
			skin.horizontalScrollbarThumb.normal = new UIStyleState();
			skin.verticalScrollbar.normal = new UIStyleState();
			skin.verticalScrollbarThumb.normal = new UIStyleState();
			skin.horizontalSlider.normal = new UIStyleState();
			skin.horizontalSliderThumb.normal = new UIStyleState();
			skin.horizontalSliderThumb.highlight = new UIStyleState();
			skin.horizontalSliderThumb.active = new UIStyleState();
			skin.verticalSlider.normal = new UIStyleState();
			skin.verticalSliderThumb.normal = new UIStyleState();
			skin.verticalSliderThumb.highlight = new UIStyleState();
			skin.verticalSliderThumb.active = new UIStyleState();
		}

		private static void loadIcons()
		{
			AssetBundle icons = AssetBundle.LoadFromFile(path + iconAssetName);

			if (icons == null)
				return;

			Sprite[] iconSprites = icons.LoadAllAssets<Sprite>();

			if (iconSprites == null)
				return;

			for (int i = iconSprites.Length - 1; i >= 0; i--)
			{
				Sprite s = iconSprites[i];

				if (s.name == "PodIcon")
					_podIcon = s;
				else if (s.name == "PlaneIcon")
					_planeIcon = s;
				else if (s.name == "ProbeIcon")
					_probeIcon = s;
				else if (s.name == "DebrisIcon")
					_debrisIcon = s;
				else if (s.name == "StationIcon")
					_stationIcon = s;
				else if (s.name == "LanderIcon")
					_landerIcon = s;
				else if (s.name == "RoverIcon")
					_roverIcon = s;
				else if (s.name == "RelayIcon")
					_relayIcon = s;
				else if (s.name == "AsteroidIcon")
					_asteroidIcon = s;
				else if (s.name == "EVAIcon")
					_evaIcon = s;
				else if (s.name == "BaseIcon")
					_baseIcon = s;
				else if (s.name == "PlanetIcon")
					_planetIcon = s;
				else if (s.name == "MysteryIcon")
					_mysteryIcon = s;
				else if (s.name == "FlagIcon")
					_flagIcon = s;
				else if (s.name == "APMarker")
					_apMarker = s;
				else if (s.name == "PEMarker")
					_peMarker = s;
				else if (s.name == "ManeuverMarker")
					_maneuverMarker = s;
				else if (s.name == "EncounterMarker")
					_encounterMarker = s;
				else if (s.name == "ExitMarker")
					_exitMarker = s;
				else if (s.name == "AnomalyIconOutline")
					_anomalyIcon = s;
				else if (s.name == "SCAN_WayPointIcon_Outline")
					_waypointIcon = s;
				else if (s.name == "SCANsat_AppLauncherSmall_Icon")
					_smallMapAppIcon = s;
				else if (s.name == "SCANsat_AppLauncherLarge_Icon")
					_bigMapAppIcon = s;
				else if (s.name == "SCAN_MechJebIcon")
					_mechJebIcon = s;
				else if (s.name == "KSP_Tooltip")
					_kspTooltipBackground = s;
			}

			SCANUtil.SCANlog("Icon asset bundle loaded");

			iconsLoaded = true;
		}

		private static void loadPrefabBundle()
		{
			AssetBundle prefabs = AssetBundle.LoadFromFile(path + prefabAssetName);

			if (prefabs == null)
				return;

			loadedPrefabs = prefabs.LoadAllAssets<GameObject>();

			if (loadedPrefabs == null)
				return;

			if (!tmpProcessed)
				processTMPPrefabs();

			if (!tooltipsProcessed && _tooltipPrefab != null)
				processTooltips();

			if (!prefabsProcessed)
				processUIPrefabs();

			if (tmpProcessed && tooltipsProcessed && prefabsProcessed)
				SCANUtil.SCANlog("UI prefab bundle loaded and processed");
			else
				SCANUtil.SCANlog("Error in processing UI prefab bundle\nSome UI elements may be affected or non-functional");

			//prefabs.Unload(false);

			prefabsLoaded = true;
		}

		private static void processTMPPrefabs()
		{
			//foreach (var r in Resources.FindObjectsOfTypeAll<Material>())
			//{
			//	SCANUtil.SCANlog("Material: {0}", r.name);
			//}

			//foreach (var f in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
			//{
			//	SCANUtil.SCANlog("TMP Font: {0}", f.name);
			//}

			//foreach (var f in Resources.FindObjectsOfTypeAll<Font>())
			//{
			//	SCANUtil.SCANlog("Font: {0}", f.name);
			//}

			for (int i = loadedPrefabs.Length - 1; i >= 0; i--)
			{
				GameObject o = loadedPrefabs[i];

				if (o.name == "SCAN_MainMap")
					_mainMapPrefab = o;
				else if (o.name == "SCAN_BigMap")
					_bigMapPrefab = o;
				else if (o.name == "SCAN_ZoomMap")
					_zoomMapPrefab = o;
				else if (o.name == "SCAN_Instruments")
					_instrumentsPrefab = o;
				else if (o.name == "SCAN_Overlay")
					_overlayPrefab = o;
				else if (o.name == "SCAN_Settings")
					_settingsPrefab = o;
				else if (o.name == "SCAN_Toolbar")
					_toolbarPrefab = o;
				else if (o.name == "SCAN_Tooltip")
					_tooltipPrefab = o;

				if (o != null)
				{
					processTMP(o);

					if (!tmpInputProcessed)
						processInputFields(o);
				}
			}

			tmpProcessed = true;
			tmpInputProcessed = true;
		}

		private static void processTMP(GameObject obj)
		{
			TextHandler[] handlers = obj.GetComponentsInChildren<TextHandler>(true);

			if (handlers == null)
				return;

			for (int i = 0; i < handlers.Length; i++)
				TMProFromText(handlers[i]);
		}

		private static void TMProFromText(TextHandler handler)
		{
			if (handler == null)
				return;

			Text text = handler.GetComponent<Text>();

			if (text == null)
				return;

			string t = text.text;
			Color c = text.color;
			int i = text.fontSize;
			bool r = text.raycastTarget;
			FontStyles sty = TMPProUtil.FontStyle(text.fontStyle);
			TextAlignmentOptions align = TMPProUtil.TextAlignment(text.alignment);
			float spacing = text.lineSpacing;
			GameObject obj = text.gameObject;

			MonoBehaviour.DestroyImmediate(text);

			SCAN_TextMeshPro tmp = obj.AddComponent<SCAN_TextMeshPro>();

			tmp.text = t;
			tmp.color = c;
			tmp.fontSize = i;
			tmp.raycastTarget = r;
			tmp.alignment = align;
			tmp.fontStyle = sty;
			tmp.lineSpacing = spacing;

			tmp.font = UISkinManager.TMPFont; //Resources.Load("Fonts/Calibri SDF", typeof(TMP_FontAsset)) as TMP_FontAsset;
			if (handler.Outline)
			{
				tmp.fontSharedMaterial = Resources.Load("Fonts/Materials/Calibri Dropshadow Outline", typeof(Material)) as Material;

				//TMP throws an error if we try to set the outline variables before accessing the font material
				//Anything that calls the font property's Get method seems to be sufficient
				if (tmp.fontMaterial) { }

				tmp.outlineColor = palette.black;
				tmp.outlineWidth = handler.OutlineWidth;
			}
			else
				tmp.fontSharedMaterial = Resources.Load("Fonts/Materials/Calibri Dropshadow", typeof(Material)) as Material;
			
			tmp.enableWordWrapping = true;
			
			tmp.isOverlay = false;
			tmp.richText = true;
		}

		private static void processInputFields(GameObject obj)
		{
			InputHandler[] handlers = obj.GetComponentsInChildren<InputHandler>(true);

			if (handlers == null)
				return;

			for (int i = 0; i < handlers.Length; i++)
				TMPInputFromInput(handlers[i]);
		}

		private static void TMPInputFromInput(InputHandler handler)
		{
			if (handler == null)
				return;

			InputField input = handler.GetComponent<InputField>();

			if (input == null)
				return;

			int limit = input.characterLimit;
			TMP_InputField.ContentType content = GetTMPContentType(input.contentType);
			float caretBlinkRate = input.caretBlinkRate;
			int caretWidth = input.caretWidth;
			Color selectionColor = input.selectionColor;
			GameObject obj = input.gameObject;

			RectTransform viewport = handler.GetComponentInChildren<RectMask2D>().rectTransform;
			SCAN_TextMeshPro placholder = handler.GetComponentsInChildren<SCAN_TextMeshPro>()[0];
			SCAN_TextMeshPro textComponent = handler.GetComponentsInChildren<SCAN_TextMeshPro>()[1];

			if (viewport == null || placholder == null || textComponent == null)
				return;

			MonoBehaviour.DestroyImmediate(input);

			SCAN_TMP_InputField tmp = obj.AddComponent<SCAN_TMP_InputField>();

			tmp.textViewport = viewport;
			tmp.placeholder = placholder;
			tmp.textComponent = textComponent;

			tmp.characterLimit = limit;
			tmp.contentType = content;
			tmp.caretBlinkRate = caretBlinkRate;
			tmp.caretWidth = caretWidth;
			tmp.selectionColor = selectionColor;

			tmp.readOnly = false;
			tmp.shouldHideMobileInput = false;

			tmp.fontAsset = UISkinManager.TMPFont;
		}

		private static TMP_InputField.ContentType GetTMPContentType(InputField.ContentType type)
		{
			switch(type)
			{
				case InputField.ContentType.Alphanumeric:
					return TMP_InputField.ContentType.Alphanumeric;
				case InputField.ContentType.Autocorrected:
					return TMP_InputField.ContentType.Autocorrected;
				case InputField.ContentType.Custom:
					return TMP_InputField.ContentType.Custom;
				case InputField.ContentType.DecimalNumber:
					return TMP_InputField.ContentType.DecimalNumber;
				case InputField.ContentType.EmailAddress:
					return TMP_InputField.ContentType.EmailAddress;
				case InputField.ContentType.IntegerNumber:
					return TMP_InputField.ContentType.IntegerNumber;
				case InputField.ContentType.Name:
					return TMP_InputField.ContentType.Name;
				case InputField.ContentType.Password:
					return TMP_InputField.ContentType.Password;
				case InputField.ContentType.Pin:
					return TMP_InputField.ContentType.Pin;
				case InputField.ContentType.Standard:
					return TMP_InputField.ContentType.Standard;
				default:
					return TMP_InputField.ContentType.Standard;
			}
		}
		
		private static void processTooltips()
		{
			for (int i = loadedPrefabs.Length - 1; i >= 0; i--)
			{
				GameObject o = loadedPrefabs[i];

				TooltipHandler[] handlers = o.GetComponentsInChildren<TooltipHandler>(true);

				if (handlers == null)
					return;

				for (int j = 0; j < handlers.Length; j++)
					processTooltip(handlers[j]);
			}

			tooltipsProcessed = true;
		}

		private static void processTooltip(TooltipHandler handler)
		{
			if (handler == null)
				return;

			handler.Prefab = _tooltipPrefab;
			handler.TooltipText = GetStringWithName(handler.TooltipName);

			toggleTooltip(handler, SCAN_Settings_Config.Instance.WindowTooltips);
		}

		private static string GetStringWithName(string tag)
		{
			return Localization.Format("#autoLOC_SCANsat_" + tag);
		}

		public static void ToggleTooltips(bool isOn)
		{
			for (int i = loadedPrefabs.Length - 1; i >= 0; i--)
			{
				GameObject o = loadedPrefabs[i];

				TooltipHandler[] handlers = o.GetComponentsInChildren<TooltipHandler>(true);

				if (handlers == null)
					return;

				for (int j = 0; j < handlers.Length; j++)
					toggleTooltip(handlers[j], isOn);
			}
		}

		private static void toggleTooltip(TooltipHandler handler, bool isOn)
		{
			if (handler == null)
				return;

			handler.IsActive = isOn && !handler.HelpTip;
		}

		private static void processUIPrefabs()
		{
			for (int i = loadedPrefabs.Length - 1; i >= 0; i--)
			{
				GameObject o = loadedPrefabs[i];

				if (o != null)
					processUIComponents(o);
			}

			prefabsProcessed = true;
		}

		private static void processUIComponents(GameObject obj)
		{
			SCAN_Style[] styles = obj.GetComponentsInChildren<SCAN_Style>(true);

			if (styles == null)
				return;

			for (int i = 0; i < styles.Length; i++)
				processComponents(styles[i]);
		}

		private static void processComponents(SCAN_Style style)
		{
			if (style == null)
				return;

			UISkinDef skin = UISkinManager.defaultSkin;

			Sprite KSPWindow = skin.window.normal.background;

			bool stock = SCAN_Settings_Config.Instance == null || SCAN_Settings_Config.Instance.StockUIStyle || _unitySkinDef == null;

			if (!stock)
				skin = _unitySkinDef;

			if (skin == null)
				return;

			switch (style.StlyeType)
			{
				case SCAN_Style.StyleTypes.Window:
					style.setImage(skin.window.normal.background);
					break;
				case SCAN_Style.StyleTypes.KSPWindow:
					style.setImage(_appBackground);
					break;
				case SCAN_Style.StyleTypes.Box:
					style.setImage(skin.box.normal.background);
					break;
				case SCAN_Style.StyleTypes.HiddenBox:
					style.setImage(stock ? skin.box.normal.background : _clearSprite);
					break;
				case SCAN_Style.StyleTypes.Button:
					style.setButton(skin.button.normal.background, skin.button.highlight.background, skin.button.active.background, skin.button.active.background);
					break;
				case SCAN_Style.StyleTypes.HiddenButton:
					style.setButton(_clearSprite, skin.button.highlight.background, skin.button.active.background, skin.button.active.background);
					break;
				case SCAN_Style.StyleTypes.AppButton:
					style.setToggleButton(_appButtonNormal, _appButtonHover, _appButtonActive, _appButtonDisabled, _appButtonHover);
					break;
				case SCAN_Style.StyleTypes.Toggle:
					if (stock)
						style.setToggle(_toggleNormal, _toggleHover, _toggleActive, _toggleActive, _toggleOn, _toggleOnHover);
					else
						style.setToggle(skin.toggle.normal.background, skin.toggle.highlight.background, skin.toggle.active.background, skin.toggle.active.background, skin.toggle.disabled.background, _unityToggleOnHover);
					break;
				case SCAN_Style.StyleTypes.ToggleButton:
					style.setToggleButton(skin.button.normal.background, skin.button.highlight.background, skin.button.active.background, skin.button.active.background, skin.button.active.background);
					break;
				case SCAN_Style.StyleTypes.KSPToggle:
					style.setToggle(_toggleNormal, _toggleHover, _toggleActive, _toggleActive, _toggleOn, _toggleOnHover);
					break;
				case SCAN_Style.StyleTypes.HorizontalSlider:
					style.setSlider(skin.horizontalSlider.normal.background, skin.horizontalSliderThumb.normal.background, skin.horizontalSliderThumb.highlight.background, skin.horizontalSliderThumb.active.background, skin.horizontalSliderThumb.active.background);
					break;
				case SCAN_Style.StyleTypes.VerticalScrollbar:
					style.setScrollbar(skin.verticalScrollbar.normal.background, skin.verticalScrollbarThumb.normal.background);
					break;
				case SCAN_Style.StyleTypes.Tooltip:
					style.setImage(stock ? _kspTooltipBackground : _unityTooltipBackground);
					break;
				case SCAN_Style.StyleTypes.VerticalSlider:
					style.setButton(skin.horizontalSliderThumb.normal.background, skin.horizontalSliderThumb.highlight.background, skin.horizontalSliderThumb.active.background, skin.horizontalSliderThumb.active.background);
					break;
				case SCAN_Style.StyleTypes.Popup:
					style.setImage(stock ? _kspTooltipBackground : _unityTooltipBackground);
					break;
				default:
					break;
			}
		}
	}
}
