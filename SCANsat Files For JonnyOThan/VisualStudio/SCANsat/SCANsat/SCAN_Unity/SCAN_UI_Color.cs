#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCAN_UI_Color - UI control object for SCANsat color management window
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SCANsat.Unity.Interfaces;
using SCANsat.Unity.Unity;
using SCANsat.SCAN_Data;
using SCANsat.SCAN_Map;
using SCANsat.SCAN_Platform.Palettes;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANpalette;

namespace SCANsat.SCAN_Unity
{
	public class SCAN_UI_Color : ISCAN_Color
	{
		private string _resourceCurrent;
		private string _resourcePlanet;
		private string _terrainPlanet;
		private string _terrainPalette;
		private string _terrainPaletteStyle;

		private bool _biomeBigMapStockColor;
		private bool _biomeBigMapWhiteBorder;
		private bool _biomeZoomMapWhiteBorder;
		private bool _biomeSmallMapStockColor;
		private bool _biomeSmallMapWhiteBorder;
		private bool _terrainClampOn;
		private bool _terrainReverse;
		private bool _terrainDiscrete;

		private float _biomeTransparency;
		private float _slopeCutoff;
		private float _resourceMin;
		private float _resourceMax;
		private float _resourceTransparency;
		private float _terrainCurrentMin;
		private float _terrainCurrentMax;
		private float _terrainClamp;

		private int _terrainSize;

		private SCANterrainConfig currentTerrain;
		private Palette currentPalette;

		private SCANresourceGlobal currentResource;
		private List<SCANresourceGlobal> loadedResources;

		public SCAN_UI_Color()
		{
			if (HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION)
				_resourcePlanet = Planetarium.fetch.Home.bodyName;
			else if (HighLogic.LoadedSceneIsFlight)
				_resourcePlanet = FlightGlobals.currentMainBody.bodyName;

			_terrainPlanet = _resourcePlanet;
		}

		public void Refresh()
		{
			_biomeBigMapStockColor = SCAN_Settings_Config.Instance.BigMapStockBiomes;
			_biomeBigMapWhiteBorder = SCAN_Settings_Config.Instance.BigMapBiomeBorder;
			_biomeZoomMapWhiteBorder = SCAN_Settings_Config.Instance.ZoomMapBiomeBorder;
			_biomeSmallMapStockColor = SCAN_Settings_Config.Instance.SmallMapStockBiomes;
			_biomeSmallMapWhiteBorder = SCAN_Settings_Config.Instance.SmallMapBiomeBorder;
			_biomeTransparency = SCAN_Settings_Config.Instance.BiomeTransparency * 100;

			_slopeCutoff = SCAN_Settings_Config.Instance.SlopeCutoff;

			loadedResources = SCANcontroller.setLoadedResourceList();
			currentResource = new SCANresourceGlobal(loadedResources[0]);

			if (currentResource != null)
			{
				currentResource.CurrentBodyConfig(_resourcePlanet);
				_resourceCurrent = currentResource.DisplayName;
				_resourceMin = currentResource.CurrentBody.MinValue;
				_resourceMax = currentResource.CurrentBody.MaxValue;
				_resourceTransparency = currentResource.Transparency;
			}

			currentTerrain = SCANcontroller.getTerrainNode(_terrainPlanet);

			if (currentTerrain != null)
			{
				currentPalette = currentTerrain.ColorPal;

				palette.CurrentPalettes = palette.setCurrentPalettesType(currentPalette.kind, currentPalette.size);

				_terrainPalette = currentTerrain.ColorPal.name;
				_terrainPaletteStyle = currentTerrain.ColorPal.kind.ToString();

				_terrainCurrentMin = currentTerrain.MinTerrain;
				_terrainCurrentMax = currentTerrain.MaxTerrain;
				_terrainClampOn = currentTerrain.ClampTerrain != null;
				_terrainClamp = currentTerrain.ClampTerrain == null ? 0 : (float)currentTerrain.ClampTerrain;
				_terrainDiscrete = currentTerrain.PalDis;
				_terrainReverse = currentTerrain.PalRev;
				_terrainSize = currentTerrain.PalSize;
			}
		}

		public string ResourcePlanet
		{
			get { return _resourcePlanet; }
			set
			{
				string body = SCANUtil.bodyFromDisplayName(value);

				_resourcePlanet = body;

				if (currentResource != null)
				{
					currentResource.CurrentBodyConfig(body);

					_resourceMin = currentResource.CurrentBody.MinValue;
					_resourceMax = currentResource.CurrentBody.MaxValue;
				}
			}
		}

		public string ResourceCurrent
		{
			get { return _resourceCurrent; }
			set
			{
				_resourceCurrent = value;

				if (currentResource.DisplayName != value)
				{
					for (int i = loadedResources.Count - 1; i >= 0; i--)
					{
						SCANresourceGlobal res = loadedResources[i];

						if (res.DisplayName != value)
							continue;

						currentResource = res;
						break;
					}

					if (currentResource == null)
						currentResource = SCANcontroller.GetFirstResource;

					if (currentResource != null)
					{
						currentResource.CurrentBodyConfig(_resourcePlanet);

						_resourceCurrent = currentResource.DisplayName;
						_resourceMin = currentResource.CurrentBody.MinValue;
						_resourceMax = currentResource.CurrentBody.MaxValue;
						_resourceTransparency = currentResource.Transparency;
					}
				}
			}
		}

		public string TerrainPlanet
		{
			get { return _terrainPlanet; }
			set
			{
				string body = SCANUtil.bodyFromDisplayName(value);

				_terrainPlanet = body;

				currentTerrain = SCANcontroller.getTerrainNode(body);

				if (currentTerrain != null)
				{
					currentPalette = currentTerrain.ColorPal;

					palette.CurrentPalettes = palette.setCurrentPalettesType(currentPalette.kind, currentPalette.size);

					_terrainPalette = currentTerrain.ColorPal.name;
					_terrainPaletteStyle = currentTerrain.ColorPal.kind.ToString();

					_terrainCurrentMin = currentTerrain.MinTerrain;
					_terrainCurrentMax = currentTerrain.MaxTerrain;
					_terrainClampOn = currentTerrain.ClampTerrain != null;
					_terrainClamp = currentTerrain.ClampTerrain == null ? 0 : (float)currentTerrain.ClampTerrain;
					_terrainDiscrete = currentTerrain.PalDis;
					_terrainReverse = currentTerrain.PalRev;
					_terrainSize = currentTerrain.PalSize;
				}
			}
		}

		public string TerrainPalette
		{
			get { return _terrainPalette; }
			set
			{
				_terrainPalette = value;

				for (int i = palette.CurrentPalettes.Length - 1; i >= 0; i--)
				{
					Palette p = palette.CurrentPalettes.availablePalettes[i];

					if (p.name != value)
						continue;

					currentPalette = p;
					break;
				}
			}
		}

		public string TerrainPaletteStyle
		{
			get { return _terrainPaletteStyle; }
			set
			{
				_terrainPaletteStyle = value;

				Palette.Kind kind = Palette.Kind.Diverging;
				
				try
				{
					kind = (Palette.Kind)Enum.Parse(typeof(Palette.Kind), value);
				}
				catch (Exception e)
				{
					SCANUtil.SCANlog("Error in palette style type\n{0}", e);
				}

				palette.CurrentPalettes = palette.setCurrentPalettesType(kind, _terrainSize);

				currentPalette = palette.CurrentPalettes.availablePalettes[0];

				_terrainPalette = currentPalette.name;
			}
		}

		public bool BiomeBigMapStockColor
		{
			get { return _biomeBigMapStockColor; }
			set { _biomeBigMapStockColor = value; }
		}

		public bool BiomeBigMapWhiteBorder
		{
			get { return _biomeBigMapWhiteBorder; }
			set { _biomeBigMapWhiteBorder = value; }
		}

		public bool BiomeZoomMapWhiteBorder
		{
			get { return _biomeZoomMapWhiteBorder; }
			set { _biomeZoomMapWhiteBorder = value; }
		}

		public bool BiomeSmallMapStockColor
		{
			get { return _biomeSmallMapStockColor; }
			set { _biomeSmallMapStockColor = value; }
		}

		public bool BiomeSmallMapWhiteBorder
		{
			get { return _biomeSmallMapWhiteBorder; }
			set { _biomeSmallMapWhiteBorder = value; }
		}

		public bool TerrainClampOn
		{
			get { return _terrainClampOn; }
			set { _terrainClampOn = value; }
		}

		public bool TerrainReverse
		{
			get { return _terrainReverse; }
			set { _terrainReverse = value; }
		}

		public bool TerrainDiscrete
		{
			get { return _terrainDiscrete; }
			set { _terrainDiscrete = value; }
		}

		public bool TerrainHasSize
		{
			get { return currentPalette.kind != Palette.Kind.Fixed; }
		}

		public float BiomeTransparency
		{
			get { return _biomeTransparency; }
			set { _biomeTransparency = value; }
		}

		public float SlopeCutoff
		{
			get { return _slopeCutoff; }
			set { _slopeCutoff = value; }
		}

		public float ResourceMin
		{
			get { return _resourceMin; }
			set { _resourceMin = value; }
		}

		public float ResourceMax
		{
			get { return _resourceMax; }
			set { _resourceMax = value; }
		}

		public float ResourceTransparency
		{
			get { return _resourceTransparency; }
			set { _resourceTransparency = value; }
		}

		public float TerrainCurrentMin
		{
			get { return _terrainCurrentMin; }
			set { _terrainCurrentMin = value; }
		}

		public float TerrainGlobalMin
		{
			get { return currentTerrain.DefaultMinHeight - SCANconfigLoader.SCANNode.RangeBelowMinHeight; }
		}

		public float TerrainCurrentMax
		{
			get { return _terrainCurrentMax; }
			set { _terrainCurrentMax = value; }
		}

		public float TerrainGlobalMax
		{
			get { return currentTerrain.DefaultMaxHeight + SCANconfigLoader.SCANNode.RangeAboveMaxHeight; }
		}

		public float TerrainClamp
		{
			get { return _terrainClamp; }
			set { _terrainClamp = value; }
		}

		public int TerrainSize
		{
			get { return _terrainSize; }
			set
			{
				_terrainSize = value;

				palette.CurrentPalettes = palette.setCurrentPalettesType(currentPalette.kind, _terrainSize);

				for (int i = palette.CurrentPalettes.Length - 1; i >= 0; i--)
				{
					Palette p = palette.CurrentPalettes.availablePalettes[i];

					if (p.name != currentPalette.name)
						continue;

					currentPalette = p;
					break;
				}
			}
		}

		public int TerrainSizeMin
		{
			get { return 3; }
		}

		public int TerrainSizeMax
		{
			get
			{
				switch(currentTerrain.ColorPal.kind)
				{
					case Palette.Kind.Diverging:
						return 11;
					case Palette.Kind.Qualitative:
						return 12;
					case Palette.Kind.Sequential:
						return 9;
				}

				return 12;
			}
		}

		public Color BiomeColorOne
		{
			get { return SCAN_Settings_Config.Instance.LowBiomeColor; }
		}

		public Color BiomeColorTwo
		{
			get { return SCAN_Settings_Config.Instance.HighBiomeColor; }
		}

		public Color SlopeColorOneLo
		{
			get { return SCAN_Settings_Config.Instance.BottomLowSlopeColor; }
		}

		public Color SlopeColorOneHi
		{
			get { return SCAN_Settings_Config.Instance.BottomHighSlopeColor; }
		}

		public Color SlopeColorTwoLo
		{
			get { return SCAN_Settings_Config.Instance.TopLowSlopeColor; }
		}

		public Color SlopeColorTwoHi
		{
			get { return SCAN_Settings_Config.Instance.TopHighSlopeColor; }
		}

		public Color ResourceColorOne
		{
			get { return currentResource.MinColor; }
		}

		public Color ResourceColorTwo
		{
			get { return currentResource.MaxColor; }
		}

		public Texture2D TerrainPaletteOld
		{
			get { return SCANmapLegend.getStaticLegend(currentTerrain); }
		}

		public Texture2D TerrainPaletteNew
		{
			get
			{
				Color32[] c = currentPalette.colors;

				if (_terrainReverse)
					c = currentPalette.colorsReverse;

				return SCANmapLegend.getStaticLegend(_terrainCurrentMax, _terrainCurrentMin, _terrainCurrentMax - _terrainCurrentMin, _terrainClampOn ? (float?)_terrainClamp : null, _terrainDiscrete, c);
			}
		}

		public IList<KeyValuePair<string, Texture2D>> TerrainPalettes
		{
			get
			{
				List<KeyValuePair<string, Texture2D>> values = new List<KeyValuePair<string, Texture2D>>();

				for (int i = 0; i < palette.CurrentPalettes.Length; i++)
				{
					Palette p = palette.CurrentPalettes.availablePalettes[i];

					values.Add(new KeyValuePair<string, Texture2D>(p.name, palette.CurrentPalettes.paletteSwatch[i]));
				}

				return values;
			}
		}

		public IList<string> Resources
		{
			get { return new List<string>(loadedResources.Select(r => r.DisplayName)); }
		}

		public IList<string> CelestialBodies
		{
			get { return new List<string>(FlightGlobals.Bodies.Select(d => d.displayName)); }
		}

		public IList<string> PaletteStyleNames
		{
			get { return new List<string>(Palette.kindNames); }
		}

		public void BiomeApply(Color one, Color two)
		{
			SCAN_Settings_Config.Instance.LowBiomeColor = one;
			SCAN_Settings_Config.Instance.HighBiomeColor = two;
			SCANcontroller.controller.lowBiomeColor32 = one;
			SCANcontroller.controller.highBiomeColor32 = two;

			SCAN_Settings_Config.Instance.BigMapStockBiomes = _biomeBigMapStockColor;
			SCAN_Settings_Config.Instance.BigMapBiomeBorder = _biomeBigMapWhiteBorder;
			SCAN_Settings_Config.Instance.ZoomMapBiomeBorder = _biomeZoomMapWhiteBorder;
			SCAN_Settings_Config.Instance.SmallMapStockBiomes = _biomeSmallMapStockColor;
			SCAN_Settings_Config.Instance.SmallMapBiomeBorder = _biomeSmallMapWhiteBorder;
			SCAN_Settings_Config.Instance.BiomeTransparency = _biomeTransparency / 100;

			if (SCAN_UI_BigMap.Instance != null && SCAN_UI_BigMap.Instance.IsVisible && SCAN_UI_BigMap.Instance.CurrentMapType == "Biome")
				SCAN_UI_BigMap.Instance.RefreshMap();

			if (SCAN_UI_MainMap.Instance != null && SCAN_UI_MainMap.Instance.IsVisible && SCAN_UI_MainMap.Instance.MapType)
				SCAN_UI_MainMap.Instance.resetImages();

			if (SCAN_UI_ZoomMap.Instance != null && SCAN_UI_ZoomMap.Instance.IsVisible && SCAN_UI_ZoomMap.Instance.CurrentMapType == "Biome")
				SCAN_UI_ZoomMap.Instance.RefreshMap();
		}

		public void BiomeDefault()
		{
			SCAN_Settings_Config.Instance.LowBiomeColor = palette.xkcd_CamoGreen;
			SCANcontroller.controller.lowBiomeColor32 = palette.xkcd_CamoGreen;
			SCAN_Settings_Config.Instance.HighBiomeColor = palette.xkcd_Marigold;
			SCANcontroller.controller.highBiomeColor32 = palette.xkcd_Marigold;

			SCAN_Settings_Config.Instance.BigMapStockBiomes = true;
			SCAN_Settings_Config.Instance.BigMapBiomeBorder = true;
			SCAN_Settings_Config.Instance.ZoomMapBiomeBorder = true;
			SCAN_Settings_Config.Instance.SmallMapStockBiomes = true;
			SCAN_Settings_Config.Instance.SmallMapBiomeBorder = false;
			SCAN_Settings_Config.Instance.BiomeTransparency = 0.4f;

			_biomeBigMapStockColor = true;
			_biomeBigMapWhiteBorder = true;
			_biomeZoomMapWhiteBorder = true;
			_biomeSmallMapStockColor = true;
			_biomeSmallMapWhiteBorder = false;
			_biomeTransparency = 40;

			if (SCAN_UI_BigMap.Instance != null && SCAN_UI_BigMap.Instance.IsVisible && SCAN_UI_BigMap.Instance.CurrentMapType == "Biome")
				SCAN_UI_BigMap.Instance.RefreshMap();

			if (SCAN_UI_MainMap.Instance != null && SCAN_UI_MainMap.Instance.IsVisible && SCAN_UI_MainMap.Instance.MapType)
				SCAN_UI_MainMap.Instance.resetImages();

			if (SCAN_UI_ZoomMap.Instance != null && SCAN_UI_ZoomMap.Instance.IsVisible && SCAN_UI_ZoomMap.Instance.CurrentMapType == "Biome")
				SCAN_UI_ZoomMap.Instance.RefreshMap();
		}

		public void SlopeApply(Color oneLow, Color oneHigh, Color twoLow, Color twoHigh)
		{
			SCAN_Settings_Config.Instance.BottomLowSlopeColor = oneLow;
			SCAN_Settings_Config.Instance.BottomHighSlopeColor = oneHigh;
			SCAN_Settings_Config.Instance.TopLowSlopeColor = twoLow;
			SCAN_Settings_Config.Instance.TopHighSlopeColor = twoHigh;
			SCANcontroller.controller.lowSlopeColorOne32 = oneLow;
			SCANcontroller.controller.highSlopeColorOne32 = oneHigh;
			SCANcontroller.controller.lowSlopeColorTwo32 = twoLow;
			SCANcontroller.controller.highSlopeColorTwo32 = twoHigh;

			SCAN_Settings_Config.Instance.SlopeCutoff = _slopeCutoff;

			if (SCAN_UI_BigMap.Instance != null && SCAN_UI_BigMap.Instance.IsVisible && SCAN_UI_BigMap.Instance.CurrentMapType == "Slope")
				SCAN_UI_BigMap.Instance.RefreshMap();

			if (SCAN_UI_ZoomMap.Instance != null && SCAN_UI_ZoomMap.Instance.IsVisible && SCAN_UI_ZoomMap.Instance.CurrentMapType == "Slope")
				SCAN_UI_ZoomMap.Instance.RefreshMap();
		}

		public void SlopeDefault()
		{
			SCAN_Settings_Config.Instance.BottomLowSlopeColor = palette.xkcd_PukeGreen;
			SCAN_Settings_Config.Instance.BottomHighSlopeColor = palette.xkcd_Lemon;
			SCAN_Settings_Config.Instance.TopLowSlopeColor = palette.xkcd_Lemon;
			SCAN_Settings_Config.Instance.TopHighSlopeColor = palette.xkcd_OrangeRed;
			SCANcontroller.controller.lowSlopeColorOne32 = palette.xkcd_PukeGreen;
			SCANcontroller.controller.highSlopeColorOne32 = palette.xkcd_Lemon;
			SCANcontroller.controller.lowSlopeColorTwo32 = palette.xkcd_Lemon;
			SCANcontroller.controller.highSlopeColorTwo32 = palette.xkcd_OrangeRed;

			SCAN_Settings_Config.Instance.SlopeCutoff = 1;
			
			if (SCAN_UI_BigMap.Instance != null && SCAN_UI_BigMap.Instance.IsVisible && SCAN_UI_BigMap.Instance.CurrentMapType == "Slope")
				SCAN_UI_BigMap.Instance.RefreshMap();

			if (SCAN_UI_ZoomMap.Instance != null && SCAN_UI_ZoomMap.Instance.IsVisible && SCAN_UI_ZoomMap.Instance.CurrentMapType == "Slope")
				SCAN_UI_ZoomMap.Instance.RefreshMap();
		}

		public void ResourceApply(Color one, Color two)
		{
			currentResource.CurrentBody.MinValue = _resourceMin;
			currentResource.CurrentBody.MaxValue = _resourceMax;
			currentResource.MinColor = one;
			currentResource.MaxColor = two;
			currentResource.Transparency = _resourceTransparency;

			SCANcontroller.updateSCANresource(currentResource, false);

			if (SCAN_UI_BigMap.Instance != null && SCAN_UI_BigMap.Instance.IsVisible && SCAN_UI_BigMap.Instance.ResourceToggle)
				SCAN_UI_BigMap.Instance.RefreshMap();

			if (SCAN_UI_ZoomMap.Instance != null && SCAN_UI_ZoomMap.Instance.IsVisible && SCAN_UI_ZoomMap.Instance.ResourceToggle)
				SCAN_UI_ZoomMap.Instance.RefreshMap();
		}

		public void ResourceApplyToAll(Color one, Color two)
		{
			currentResource.CurrentBody.MinValue = _resourceMin;
			currentResource.CurrentBody.MaxValue = _resourceMax;
			currentResource.MinColor = one;
			currentResource.MaxColor = two;
			currentResource.Transparency = _resourceTransparency;

			SCANcontroller.updateSCANresource(currentResource, true);

			if (SCAN_UI_BigMap.Instance != null && SCAN_UI_BigMap.Instance.IsVisible && SCAN_UI_BigMap.Instance.ResourceToggle)
				SCAN_UI_BigMap.Instance.RefreshMap();

			if (SCAN_UI_ZoomMap.Instance != null && SCAN_UI_ZoomMap.Instance.IsVisible && SCAN_UI_ZoomMap.Instance.ResourceToggle)
				SCAN_UI_ZoomMap.Instance.RefreshMap();
		}

		public void ResourceDefault()
		{
			currentResource.CurrentBody.MinValue = currentResource.CurrentBody.DefaultMinValue;
			currentResource.CurrentBody.MaxValue = currentResource.CurrentBody.DefaultMaxValue;
			currentResource.MinColor = currentResource.DefaultLowColor;
			currentResource.MaxColor = currentResource.DefaultHighColor;
			currentResource.Transparency = currentResource.DefaultTrans;

			SCANcontroller.updateSCANresource(currentResource, false);

			if (SCAN_UI_BigMap.Instance != null && SCAN_UI_BigMap.Instance.IsVisible && SCAN_UI_BigMap.Instance.ResourceToggle)
				SCAN_UI_BigMap.Instance.RefreshMap();
		}

		public void ResourceDefaultToAll()
		{
			currentResource.CurrentBody.MinValue = currentResource.CurrentBody.DefaultMinValue;
			currentResource.CurrentBody.MaxValue = currentResource.CurrentBody.DefaultMaxValue;
			currentResource.MinColor = currentResource.DefaultLowColor;
			currentResource.MaxColor = currentResource.DefaultHighColor;
			currentResource.Transparency = currentResource.DefaultTrans;

			SCANcontroller.updateSCANresource(currentResource, true);

			if (SCAN_UI_BigMap.Instance != null && SCAN_UI_BigMap.Instance.IsVisible && SCAN_UI_BigMap.Instance.ResourceToggle)
				SCAN_UI_BigMap.Instance.RefreshMap();

			if (SCAN_UI_ZoomMap.Instance != null && SCAN_UI_ZoomMap.Instance.IsVisible && SCAN_UI_ZoomMap.Instance.ResourceToggle)
				SCAN_UI_ZoomMap.Instance.RefreshMap();
		}

		public void ResourceSaveToConfig(Color one, Color two)
		{
			currentResource.CurrentBody.MinValue = _resourceMin;
			currentResource.CurrentBody.MaxValue = _resourceMax;
			currentResource.MinColor = one;
			currentResource.MaxColor = two;
			currentResource.Transparency = _resourceTransparency;

			SCANcontroller.updateSCANresource(currentResource, false);

			if (SCAN_UI_BigMap.Instance != null && SCAN_UI_BigMap.Instance.IsVisible && SCAN_UI_BigMap.Instance.ResourceToggle)
				SCAN_UI_BigMap.Instance.RefreshMap();

			if (SCAN_UI_ZoomMap.Instance != null && SCAN_UI_ZoomMap.Instance.IsVisible && SCAN_UI_ZoomMap.Instance.ResourceToggle)
				SCAN_UI_ZoomMap.Instance.RefreshMap();

			SCANconfigLoader.SCANNode.Save();
		}

		public void TerrainApply()
		{
			currentTerrain.MinTerrain = _terrainCurrentMin;
			currentTerrain.MaxTerrain = _terrainCurrentMax;
			currentTerrain.ClampTerrain = _terrainClampOn ? (float?)_terrainClamp : null;
			currentTerrain.PalDis = _terrainDiscrete;
			currentTerrain.PalRev = _terrainReverse;
			currentTerrain.PalSize = _terrainSize;

			currentTerrain.ColorPal = currentPalette;

			SCANcontroller.updateTerrainConfig(currentTerrain);

			if (SCAN_UI_BigMap.Instance != null && SCAN_UI_BigMap.Instance.IsVisible && SCAN_UI_BigMap.Instance.CurrentMapType == "Altimetry")
				SCAN_UI_BigMap.Instance.RefreshMap();

			if (SCAN_UI_MainMap.Instance != null && SCAN_UI_MainMap.Instance.IsVisible && !SCAN_UI_MainMap.Instance.MapType)
				SCAN_UI_MainMap.Instance.resetImages();

			if (SCAN_UI_ZoomMap.Instance != null && SCAN_UI_ZoomMap.Instance.IsVisible && SCAN_UI_ZoomMap.Instance.CurrentMapType == "Altimetry")
				SCAN_UI_ZoomMap.Instance.RefreshMap();
		}

		public void TerrainDefault()
		{
			currentTerrain.MinTerrain = currentTerrain.DefaultMinHeight;
			currentTerrain.MaxTerrain = currentTerrain.DefaultMaxHeight;
			currentTerrain.ClampTerrain = currentTerrain.DefaultClampHeight;
			currentTerrain.ColorPal = currentTerrain.DefaultPalette;
			currentTerrain.PalRev = currentTerrain.DefaultReverse;
			currentTerrain.PalDis = currentTerrain.DefaultDiscrete;
			currentTerrain.PalSize = currentTerrain.DefaultPaletteSize;

			SCANcontroller.updateTerrainConfig(currentTerrain);

			Refresh();

			if (SCAN_UI_BigMap.Instance != null && SCAN_UI_BigMap.Instance.IsVisible && SCAN_UI_BigMap.Instance.CurrentMapType == "Altimetry")
				SCAN_UI_BigMap.Instance.RefreshMap();

			if (SCAN_UI_MainMap.Instance != null && SCAN_UI_MainMap.Instance.IsVisible && !SCAN_UI_MainMap.Instance.MapType)
				SCAN_UI_MainMap.Instance.resetImages();

			if (SCAN_UI_ZoomMap.Instance != null && SCAN_UI_ZoomMap.Instance.IsVisible && SCAN_UI_ZoomMap.Instance.CurrentMapType == "Altimetry")
				SCAN_UI_ZoomMap.Instance.RefreshMap();
		}

		public void TerrainSaveToConfig()
		{
			currentTerrain.MinTerrain = _terrainCurrentMin;
			currentTerrain.MaxTerrain = _terrainCurrentMax;
			currentTerrain.ClampTerrain = _terrainClampOn ? (float?)_terrainClamp : null;
			currentTerrain.PalDis = _terrainDiscrete;
			currentTerrain.PalRev = _terrainReverse;
			currentTerrain.PalSize = _terrainSize;

			SCANcontroller.updateTerrainConfig(currentTerrain);

			SCANconfigLoader.SCANNode.Save();

			if (SCAN_UI_BigMap.Instance != null && SCAN_UI_BigMap.Instance.IsVisible && SCAN_UI_BigMap.Instance.CurrentMapType == "Altimetry")
				SCAN_UI_BigMap.Instance.RefreshMap();

			if (SCAN_UI_MainMap.Instance != null && SCAN_UI_MainMap.Instance.IsVisible && !SCAN_UI_MainMap.Instance.MapType)
				SCAN_UI_MainMap.Instance.resetImages();

			if (SCAN_UI_ZoomMap.Instance != null && SCAN_UI_ZoomMap.Instance.IsVisible && SCAN_UI_ZoomMap.Instance.CurrentMapType == "Altimetry")
				SCAN_UI_ZoomMap.Instance.RefreshMap();
		}
	}
}
