#region license
/*
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANmapProfile - what a map does differently because of who owns it
 *
 * Copyright (c)2013 damny;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
*/
#endregion

namespace SCANsat.SCAN_Map
{
	/// <summary>
	/// The fixed part of a SCANmap's behaviour: everything it does differently because of WHO owns it
	/// (the big map, the zoom map, an RPM page, the small map, the planet overlay), decided once from
	/// the mapSource and read by SCANmap wherever it would otherwise ask "which source am I". What the
	/// owner changes per pass - mode, colour toggle, terminator, resource layer, the overlay's
	/// selection-dependent alpha and base - stays on SCANmap's properties.
	/// </summary>
	internal sealed class SCANmapProfile
	{
		internal enum UnscannedFill
		{
			Settings,   // the UnscannedColor setting at UnscannedTransparency (the map windows)
			Grey,       // opaque palette.Grey, the classic small map's base for uncovered pixels
			Clear,      // transparent, so a planet overlay leaves uncovered terrain showing through
		}

		private enum BiomeStyle
		{
			BigMap,     // the big map's own border / stock-colour settings
			Window,     // the zoom map's border setting with the big map's stock-colour setting (zoom map, RPM)
			SmallMap,   // the small map's own settings, no elevation underlay
			Flat,       // plain stock colours, no borders, no underlay (the planet overlay)
		}

		/// <summary>true: the three data caches (elevation, biome index, resource grid) are geographic
		/// over the whole globe (setWidth) and survive projection changes and re-centring; false: pixel
		/// space over the current window (setSize), dropped when the window moves, zooms or changes
		/// projection. Sets the shader's _PixelSpace, its one addressing rule.</summary>
		internal bool GeographicCache { get; private set; }

		/// <summary>false: no timed reveal; the pass is complete when the build is, and nothing is shown
		/// mid-build (the planet overlay hands its texture over once).</summary>
		internal bool Sweep { get; private set; }

		/// <summary>false: Biome samples no elevation for its underlay, and the shader keeps it grey.</summary>
		internal bool BiomeUnderlay { get; private set; }

		/// <summary>true: columns in the planet's ScaledSpace UV layout, u = 0 at 90 E and longitude
		/// decreasing with u - the inverse of a map window's, and what the CPU overlays' fixLon did per
		/// column. Any texture the PLANET displays must use it.</summary>
		internal bool PlanetUV { get; private set; }

		/// <summary>true: the palette range is fitted to the window's own samples by a pre-pass before
		/// the first row is revealed (zoom map, RPM - what their calcTerrainLimits did).</summary>
		internal bool AutoRange { get; private set; }

		/// <summary>true: a 360x180 rectangular pass reads the body's prebuilt height grid
		/// (SCANdata.HeightMapValue) instead of sampling PQS, as the classic small map and terrain
		/// overlay did. SCANmap still requires the grid to be built and the size to match.</summary>
		internal bool HeightGrid { get; private set; }

		/// <summary>true: the shader draws the small map's dotted 30-degree graticule over unscanned
		/// pixels (the big map's graticule is a separate texture, SCANmap.renderGrid).</summary>
		internal bool GridDots { get; private set; }

		internal UnscannedFill Unscanned { get; private set; }

		/// <summary>true: the resource cache is sized by the ResourceMapHeight setting and re-synced to
		/// it on every reset; false: it is the map's own size (the zoom map).</summary>
		internal bool ResourceGridFromSettings { get; private set; }

		/// <summary>true: the resource cache is interpolated with hard edges (the zoom map).</summary>
		internal bool ResourceHardEdges { get; private set; }

		private BiomeStyle biomeStyle;

		private SCANmapProfile()
		{
		}

		internal static SCANmapProfile For(mapSource source)
		{
			switch (source)
			{
				case mapSource.BigMap:
					return new SCANmapProfile
					{
						GeographicCache = true,
						Sweep = true,
						BiomeUnderlay = true,
						PlanetUV = false,
						AutoRange = false,
						HeightGrid = false,
						GridDots = false,
						Unscanned = UnscannedFill.Settings,
						ResourceGridFromSettings = true,
						ResourceHardEdges = false,
						biomeStyle = BiomeStyle.BigMap,
					};

				case mapSource.ZoomMap:
					return new SCANmapProfile
					{
						GeographicCache = false,
						Sweep = true,
						BiomeUnderlay = true,
						PlanetUV = false,
						AutoRange = true,
						HeightGrid = false,
						GridDots = false,
						Unscanned = UnscannedFill.Settings,
						ResourceGridFromSettings = false,
						ResourceHardEdges = true,
						biomeStyle = BiomeStyle.Window,
					};

				case mapSource.RPM:
					return new SCANmapProfile
					{
						GeographicCache = false,
						Sweep = true,
						BiomeUnderlay = true,
						PlanetUV = false,
						AutoRange = true,
						HeightGrid = false,
						GridDots = false,
						Unscanned = UnscannedFill.Settings,
						ResourceGridFromSettings = true,
						ResourceHardEdges = false,
						biomeStyle = BiomeStyle.Window,
					};

				case mapSource.Data:   // the small map
					return new SCANmapProfile
					{
						GeographicCache = false,
						Sweep = true,
						BiomeUnderlay = false,   // its biome transparency is 0, so an underlay would be sampled and never seen
						PlanetUV = false,
						AutoRange = false,
						HeightGrid = true,
						GridDots = true,
						Unscanned = UnscannedFill.Grey,
						ResourceGridFromSettings = true,
						ResourceHardEdges = false,
						biomeStyle = BiomeStyle.SmallMap,
					};

				case mapSource.Overlay:
					return new SCANmapProfile
					{
						GeographicCache = true,
						Sweep = false,
						BiomeUnderlay = false,
						PlanetUV = true,
						AutoRange = false,
						HeightGrid = true,
						GridDots = false,
						Unscanned = UnscannedFill.Clear,
						ResourceGridFromSettings = true,
						ResourceHardEdges = false,
						biomeStyle = BiomeStyle.Flat,
					};

				default:
					throw new System.ArgumentOutOfRangeException("source", source, "no SCANmapProfile for this map source");
			}
		}

		/// <summary>
		/// The biome colouring toggles for a composite: the border and stock-colour switches come from
		/// the owning window's own settings block, and the blend toward the elevation underlay is the
		/// BiomeTransparency setting where a source draws an underlay at all.
		/// </summary>
		internal void BiomeToggles(bool colorMap, out bool border, out bool stock, out float transparency)
		{
			SCAN_Settings_Config s = SCAN_Settings_Config.Instance;
			transparency = s.BiomeTransparency;

			switch (biomeStyle)
			{
				case BiomeStyle.BigMap:
					border = s.BigMapBiomeBorder;
					stock = s.BigMapStockBiomes && colorMap;
					break;
				case BiomeStyle.SmallMap:
					border = s.SmallMapBiomeBorder;
					stock = s.SmallMapStockBiomes;
					transparency = 0f;
					break;
				case BiomeStyle.Flat:
					border = false;
					stock = true;
					transparency = 0f;
					break;
				default:
					border = s.ZoomMapBiomeBorder;
					stock = s.BigMapStockBiomes && colorMap;
					break;
			}
		}
	}
}
