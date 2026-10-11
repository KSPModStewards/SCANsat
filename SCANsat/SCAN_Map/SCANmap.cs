#region license
/*
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 * 
 * SCANmap - makes maps from data
 *
 * Copyright (c)2013 damny;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
*/
#endregion

using System;
using System.Collections.Generic;
using UnityEngine;
using SCANsat.SCAN_Data;
using SCANsat.SCAN_UI.UI_Framework;
using SCANsat.SCAN_Unity;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANcolorUtil;

namespace SCANsat.SCAN_Map
{
	public class SCANmap
	{
		internal SCANmap(CelestialBody Body, mapSource s)
		{
			body = Body;
			mSource = s;
			profile = SCANmapProfile.For(s);
			if (profile.AutoRange)
			{
				useCustomRange = true;   // the pre-pass fills the values in before the first row is revealed
			}
			pqs = body.pqsController != null;
			biomeMap = body.BiomeMap != null;
			data = SCANUtil.getData(body);
			if (data == null)
			{
				data = new SCANdata(body);
				SCANcontroller.controller.addToBodyData(body, data);
			}
		}

		#region Public Accessors

		public double MapScale
		{
			get { return mapscale; }
			internal set
			{
				if (!profile.GeographicCache && mapscale != value)
					clearWindowCaches();   // pixel-space caches: a new zoom level is a new window
				mapscale = value;
				resourceMapScale = mapscale * mapwidth / resourceMapWidth;
			}
		}

		public double Lon_Offset
		{
			get { return lon_offset; }
		}

		public double Lat_Offset
		{
			get { return lat_offset; }
		}

		public double CenteredLong
		{
			get { return centeredLong; }
		}

		public double CenteredLat
		{
			get { return centeredLat; }
		}

		public int MapWidth
		{
			get { return mapwidth; }
		}

		public int MapHeight
		{
			get { return mapheight; }
		}

		public mapType MType
		{
			get { return mType; }
			set { mType = value; }
		}

		public bool ColorMap
		{
			get { return colorMap; }
			set { colorMap = value; }
		}

		public bool Terminator
		{
			get { return terminator; }
			set { terminator = value; }
		}

		// The active vessel's scanner bitmask, for the small map's "not covered by your active sensors"
		// dimming (SCANUtil.isCoveredByAll). Nothing = off; only mapSource.Data ever sets it.
		public SCANtype SensorMask
		{
			get { return sensorMask; }
			set { sensorMask = value; }
		}

		public mapSource MSource
		{
			get { return mSource; }
		}

		// The texture to display: the RenderTexture the map was composited into, which RawImage.texture
		// and Graphics.Blit both accept. Null until a render has happened. There is no Texture2D copy;
		// PNG export re-composites (renderVisualExport) and SCANmapExporter reads that back.
		public Texture DisplayTexture
		{
			get { return gpuRendered && visualRenderTex != null ? visualRenderTex : null; }
		}

		public bool GpuRendered
		{
			get { return gpuRendered; }
		}

		public RenderTexture VisualRenderTexture
		{
			get { return visualRenderTex; }
		}

		public CelestialBody Body
		{
			get { return body; }
		}

		public SCANresourceGlobal Resource
		{
			get { return resource; }
			set { resource = value; }
		}

		public bool ResourceActive
		{
			get { return resourceActive; }
			set { resourceActive = value; }
		}

		public SCANmapLegend MapLegend
		{
			get { return mapLegend; }
			internal set { mapLegend = value; }
		}

		public MapProjection Projection
		{
			get { return projection; }
			set { projection = value; }
		}

		internal float[,] Big_HeightMap
		{
			get { return big_heightmap; }
		}

		public bool UseCustomRange
		{
			get { return useCustomRange; }
		}

		public float CustomMin
		{
			get { return customMin; }
		}

		public float CustomMax
		{
			get { return customMax; }
		}

		public float CustomRange
		{
			get { return customRange; }
		}

		public float CustomResourceMin
		{
			get { return customResourceMin; }
		}

		public float CustomResourceMax
		{
			get { return customResourceMax; }
		}

		// What the planet overlay changes per selection (see the fields). Everything a map does
		// differently because of its source alone is in the profile.
		internal bool BaseNone
		{
			get { return baseNone; }
			set { baseNone = value; }
		}

		internal float OutputAlpha
		{
			get { return outputAlpha; }
			set { outputAlpha = value; }
		}

		internal float ResGreyBlend
		{
			get { return resGreyBlend; }
			set { resGreyBlend = value; }
		}

		#endregion

		#region Big Map methods and fields

		/* MAP: Big Map height map caching */
		private float[,] big_heightmap;
		// Geographic grids for bodies this map is not currently showing. A body switch used to zero the
		// cache, so coming back re-sampled every covered pixel from PQS - about 1.3 s of CPU on a
		// scanned RSS body at 1440 wide, five seconds of wall clock under the per-frame build budget.
		// Park them instead: terrain does not change at runtime, and zero still means "unsampled", so a
		// grid handed back is never wrong, only incomplete - there is nothing to invalidate. setWidth
		// drops the lot, so every parked grid is always at the current width and the memory budget is
		// just a count of them.
		private readonly Dictionary<int, float[,]> parkedHeightmaps = new Dictionary<int, float[,]>();
		private readonly List<int> parkedOrder = new List<int>();   // least recently parked first
		// Parked grids outlive a scene change, as the live one does, so this is a standing cost once you
		// have browsed a few bodies. 32 MB is every body in an RSS system at the 720 default (about
		// 1 MB each), seven of them at 1440, and none at all at a width where one grid cannot fit.
		private const long ParkedHeightmapBudget = 32L << 20;
		private double centeredLong, centeredLat;

		// This pass reads the body's prebuilt 360x180 height map instead of sampling PQS - the same grid
		// and values the classic small map and terrain overlay drew from SCANdata.HeightMapValue. Only
		// for a source the profile allows it, at one pixel per degree, once the grid is built. Decided
		// once per pass in resetMap, so a pass never changes source part-way through.
		private bool heightGridPass;

		// Zero is "unsampled" in the cache, so a true 0 m is stored as -0.001, as terrainHeightToArray does.
		private void gridHeightToArray(int ilon, int ilat)
		{
			float alt = data.HeightMapValue(body.flightGlobalsIndex, ilon, ilat);
			if (alt == 0f)
			{
				alt = -0.001f;
			}

			big_heightmap[ilon, ilat] = alt;
		}

		private void terrainHeightToArray(double lon, double lat, int ilon, int ilat)
		{
			passHeightSamples++;
			float alt = 0f;
			alt = (float)SCANUtil.getElevation(body, lon, lat);
			if (alt == 0f)
			{
				alt = -0.001f;
			}

			big_heightmap[ilon, ilat] = alt;
		}

		/* MAP: Projection methods for converting planet coordinates to the rectangular texture */
		private MapProjection projection = MapProjection.Rectangular;

		internal void setProjection(MapProjection p)
		{
			if (projection == p)
			{
				return;
			}

			projection = p;
			// A window map's caches are sampled in projected pixel space; a geographic cache is the globe
			// whatever projection is shown, and stays.
			if (!profile.GeographicCache)
				clearWindowCaches();
		}

		internal double projectLongitude(double lon, double lat)
		{
			lon = (lon + 3600 + 180) % 360 - 180;
			lat = (lat + 1800 + 90) % 180 - 90;
			switch (projection)
			{
				case MapProjection.KavrayskiyVII:
					lon = Mathf.Deg2Rad * lon;
					lat = Mathf.Deg2Rad * lat;
					lon = (3.0f * lon / 2.0f / Math.PI) * Math.Sqrt(Math.PI * Math.PI / 3.0f - lat * lat);
					return Mathf.Rad2Deg * lon;
				case MapProjection.Polar:
					lon = Mathf.Deg2Rad * lon;
					lat = Mathf.Deg2Rad * lat;
					if (lat < 0)
					{
						lon = 1.3 * Math.Cos(lat) * Math.Sin(lon) - Math.PI / 2;
					}
					else
					{
						lon = 1.3 * Math.Cos(lat) * Math.Sin(lon) + Math.PI / 2;
					}
					return Mathf.Rad2Deg * lon;
				case MapProjection.Orthographic:
					lon = Mathf.Deg2Rad * lon;
					lat = Mathf.Deg2Rad * lat;
					double centerLon = Mathf.Deg2Rad * centeredLong;
					double centerLat = Mathf.Deg2Rad * centeredLat;

					if (Math.Sin(centerLat) * Math.Sin(lat) + Math.Cos(centerLat) * Math.Cos(lat) * Math.Cos(lon - centerLon) < 0)
					{
						return -200;
					}

					lon = 1.5 * Math.Cos(lat) * Math.Sin(lon - centerLon);

					return Mathf.Rad2Deg * lon;
				default:
					return lon;
			}
		}

		internal double projectLatitude(double lon, double lat)
		{
			lon = (lon + 3600 + 180) % 360 - 180;
			lat = (lat + 1800 + 90) % 180 - 90;
			switch (projection)
			{
				case MapProjection.Polar:
					lon = Mathf.Deg2Rad * lon;
					lat = Mathf.Deg2Rad * lat;
					if (lat < 0)
					{
						lat = 1.3 * Math.Cos(lat) * Math.Cos(lon);
					}
					else
					{
						lat = -1.3 * Math.Cos(lat) * Math.Cos(lon);
					}
					return Mathf.Rad2Deg * lat;
				case MapProjection.Orthographic:
					lon = Mathf.Deg2Rad * lon;
					lat = Mathf.Deg2Rad * lat;
					double centerLon = Mathf.Deg2Rad * centeredLong;
					double centerLat = Mathf.Deg2Rad * centeredLat;

					if (Math.Sin(centerLat) * Math.Sin(lat) + Math.Cos(centerLat) * Math.Cos(lat) * Math.Cos(lon - centerLon) < 0)
					{
						return -200;
					}

					lat = 1.5 * (Math.Cos(centerLat) * Math.Sin(lat) - Math.Sin(centerLat) * Math.Cos(lat) * Math.Cos(lon - centerLon));

					return Mathf.Rad2Deg * lat;
				default:
					return lat;
			}
		}

		internal double unprojectLongitude(double lon, double lat)
		{
			if (lat > 90)
			{
				lat = 180 - lat;
				lon += 180;
			}
			else if (lat < -90)
			{
				lat = -180 - lat;
				lon += 180;
			}
			lon = (lon + 3600 + 180) % 360 - 180;
			lat = (lat + 1800 + 90) % 180 - 90;
			switch (projection)
			{
				case MapProjection.KavrayskiyVII:
					lon = Mathf.Deg2Rad * lon;
					lat = Mathf.Deg2Rad * lat;
					lon = lon / Math.Sqrt(Mathf.PI * Math.PI / 3.0f - lat * lat) * 2.0f * Math.PI / 3.0f;
					return Mathf.Rad2Deg * lon;
				case MapProjection.Polar:
					lon = Mathf.Deg2Rad * lon;
					lat = Mathf.Deg2Rad * lat;
					double lat0 = Math.PI / 2;
					if (lon < 0)
					{
						lon += Math.PI / 2;
						lat0 = -Math.PI / 2;
					}
					else
					{
						lon -= Math.PI / 2;
					}
					lon /= 1.3;
					lat /= 1.3;
					double p = Math.Sqrt(lon * lon + lat * lat);
					double c = Math.Asin(p);
					lon = Math.Atan2((lon * Math.Sin(c)), (p * Math.Cos(lat0) * Math.Cos(c) - lat * Math.Sin(lat0) * Math.Sin(c)));
					lon = (Mathf.Rad2Deg * lon + 180) % 360 - 180;
					if (lon <= -180)
					{
						lon = -180;
					}

					return lon;
				case MapProjection.Orthographic:
					lon = Mathf.Deg2Rad * lon;
					lat = Mathf.Deg2Rad * lat;
					double centerLon = Mathf.Deg2Rad * centeredLong;
					double centerLat = Mathf.Deg2Rad * centeredLat;

					double p2 = Math.Sqrt(lon * lon + lat * lat);
					double c2 = Math.Asin(p2 / 1.5);

					if (Math.Cos(c2) < 0)
					{
						return 300;
					}

					lon = centerLon + Math.Atan2(lon * Math.Sin(c2), p2 * Math.Cos(c2) * Math.Cos(centerLat) - lat * Math.Sin(c2) * Math.Sin(centerLat));

					lon = (Mathf.Rad2Deg * lon + 180) % 360 - 180;

					if (lon <= -180)
					{
						lon += 360;
					}

					return lon;
				default:
					return lon;
			}
		}

		internal double unprojectLatitude(double lon, double lat)
		{
			if (lat > 90)
			{
				lat = 180 - lat;
				lon += 180;
			}
			else if (lat < -90)
			{
				lat = -180 - lat;
				lon += 180;
			}
			lon = (lon + 3600 + 180) % 360 - 180;
			lat = (lat + 1800 + 90) % 180 - 90;
			switch (projection)
			{
				case MapProjection.Polar:
					lon = Mathf.Deg2Rad * lon;
					lat = Mathf.Deg2Rad * lat;
					double lat0 = Math.PI / 2;
					if (lon < 0)
					{
						lon += Math.PI / 2;
						lat0 = -Math.PI / 2;
					}
					else
					{
						lon -= Math.PI / 2;
					}
					lon /= 1.3;
					lat /= 1.3;
					double p = Math.Sqrt(lon * lon + lat * lat);
					double c = Math.Asin(p);
					lat = Math.Asin(Math.Cos(c) * Math.Sin(lat0) + (lat * Math.Sin(c) * Math.Cos(lat0)) / (p));
					return Mathf.Rad2Deg * lat;
				case MapProjection.Orthographic:
					lon = Mathf.Deg2Rad * lon;
					lat = Mathf.Deg2Rad * lat;
					double centerLat = Mathf.Deg2Rad * centeredLat;

					double p2 = Math.Sqrt(lon * lon + lat * lat);
					double c2 = Math.Asin(p2 / 1.5);

					if (Math.Cos(c2) < 0)
					{
						return 300;
					}

					lat = Math.Asin(Math.Cos(c2) * Math.Sin(centerLat) + (lat * Math.Sin(c2) * Math.Cos(centerLat)) / p2);

					return Mathf.Rad2Deg * lat;
				default:
					return lat;
			}
		}

		/* MAP: scaling, centering (setting origin), translating, etc */
		private double mapscale, lon_offset, lat_offset;
		private int mapwidth, mapheight;
		private bool resourceActive;
		private float[,] resourceCache;
		private int resourceInterpolation = 4;
		private int resourceMapWidth = 4;
		private int resourceMapHeight = 2;
		private double resourceMapScale = 1;
		private bool randomEdges = true;
		private double[] biomeIndex;
		private int startLine;
		private int stopLine;
		double sunLonCenter;
		double sunLatCenter;
		double gamma;

		internal void setSize(Vector2 size)
		{
			setSize((int)size.x, (int)size.y);
		}

		internal void setSize(int w, int h, int interpolation = 2, int start = 0, int stop = 0)
		{
			if (w == 0)
			{
				w = 360 * (Screen.width / 360);
			}

			if (w > 360 * 4)
			{
				w = 360 * 4;
			}

			mapwidth = w;
			biomeIndex = new double[mapwidth];
			mapscale = mapwidth / 360f;
			if (h <= 0)
			{
				h = (int)(180 * mapscale);
			}

			mapheight = h;
			startLine = start;
			stopLine = stop == 0 ? mapheight - 1 : stop;
			// Window-map caches, pixel space (see prepRow): the GPU data path fills them per rendered
			// pixel, so the zoom map and RPM render Altimetry/Slope/Biome the way the big map does.
			// Sized by ensureModeCaches on the next reset, for the mode that actually reads them: a
			// window map that only ever shows Altimetry never allocates the biome index.
			big_heightmap = null;
			biome_indexmap = null;
			biomeRowCached = null;
			invalidateGpuDataCache();
			resourceMapWidth = mapwidth;
			resourceMapHeight = mapheight;
			resourceCache = null;   // buildResourceCache sizes it when a resource layer is actually on
			resourceInterpolation = interpolation;
			resourceMapScale = resourceMapWidth / 360f;
			randomEdges = false;
		}

		// reset: false for a caller that resets itself straight after with the real mode - the planet
		// overlay, which would otherwise reset twice per build, the first time with the previous
		// selection's mType and a coverageChecksum over 64,800 cells thrown away two lines later.
		internal void setWidth(int w, bool reset = true)
		{
			if (w == 0)
			{
				w = 360 * (int)(Screen.width / 360);
				if (w > 360 * 4)
				{
					w = 360 * 4;
				}
			}
			if (w < 360)
			{
				w = 360;
			}

			if (mapwidth == w)
			{
				return;
			}

			mapwidth = w;
			biomeIndex = new double[w];
			resourceMapHeight = SCAN_Settings_Config.Instance.ResourceMapHeight;
			resourceMapWidth = resourceMapHeight * 2;
			resourceInterpolation = SCAN_Settings_Config.Instance.Interpolation;
			resourceMapScale = resourceMapWidth / 360f;
			resourceCache = null;   // buildResourceCache sizes it when a resource layer is actually on
			randomEdges = true;
			mapscale = mapwidth / 360f;
			mapheight = (int)(w / 2);
			startLine = 0;
			stopLine = mapheight - 1;
			/* big map caching: sized per mode by ensureModeCaches on the reset below */
			big_heightmap = null;
			biome_indexmap = null;
			biomeRowCached = null;
			// Every parked grid is at the old width, so none of them can be handed back. Dropping them
			// here is also what lets the budget below be a count: all parked grids share one size.
			parkedHeightmaps.Clear();
			parkedOrder.Clear();
			// Just wiped big_heightmap/biome_indexmap. mapwidth is part of gpuConfigHash so
			// the stale claim can't match today, but the caches are empty either way - don't leave a
			// "data is complete" flag standing behind them.
			invalidateGpuDataCache();
			if (reset)
			{
				resetMap(resourceActive);
			}
		}

		// Free the Unity objects this map owns. Texture2D/RenderTexture/Material are NOT GC-managed
		// and are NOT auto-destroyed on scene load, so an orphaned SCANmap (the bigmap/spotmap
		// wrappers are re-created per scene) leaks them - SCANmap is a plain class with no finalizer.
		// The owning window's OnDestroy calls this. Safe to call more than once.
		internal void Destroy()
		{
			// Visual mode registers this body's textures with the controller (refreshVisualMapTexture) and
			// nothing releases them when the owner simply goes away - an IVA exit, a vessel switch, a scene
			// change. A stale claim also blocks every other map from freeing that body (the shared-body guard
			// in UnloadVisualMapTexture), so release this source's claim here too. Idempotent: the per-source
			// slot is nulled first, then the guard, then the dictionary lookup, so an owner that already
			// released (big map, zoom map, main map) just no-ops.
			if (SCANcontroller.controller != null && body != null)
			{
				SCANcontroller.controller.UnloadVisualMapTexture(body, mSource);
			}

			if (coverageFlags != null) { UnityEngine.Object.Destroy(coverageFlags); coverageFlags = null; }
			if (compositeMaterial != null) { UnityEngine.Object.Destroy(compositeMaterial); compositeMaterial = null; }
			if (visualRenderTex != null) { visualRenderTex.Release(); UnityEngine.Object.Destroy(visualRenderTex); visualRenderTex = null; }
			if (exporter != null) { UnityEngine.Object.Destroy(exporter.gameObject); exporter = null; }
			if (elevationTex != null) { UnityEngine.Object.Destroy(elevationTex); elevationTex = null; }
			if (biomeIndexTex != null) { UnityEngine.Object.Destroy(biomeIndexTex); biomeIndexTex = null; }
			if (resourceTex != null) { UnityEngine.Object.Destroy(resourceTex); resourceTex = null; }
			if (paletteLUT != null) { UnityEngine.Object.Destroy(paletteLUT); paletteLUT = null; }
			if (paletteGreyLUT != null) { UnityEngine.Object.Destroy(paletteGreyLUT); paletteGreyLUT = null; }
			if (biomeLUT != null) { UnityEngine.Object.Destroy(biomeLUT); biomeLUT = null; }

			// The GPU data cache just went away with those textures, so its bookkeeping must go too.
			// bigmap/spotmap are STATIC: the SCANmap outlives this Destroy and gets reused next scene.
			// Leaving dataTexKey stamped means resetMap's instant-recolour path matches on
			// the next open with the same body+mode+projection+zoom, skips the whole sample sweep, and
			// re-Blits from biomeIndexTex/elevationTex that are now null - a blank map until you switch
			// body (which changes the hash) and back.
			invalidateGpuDataCache();

			// The managed caches too. bigmap/spotmap being STATIC is exactly why: without this their
			// arrays stay resident for the whole session, across every scene change, for a window that
			// may never open again - 4 MB of biome index and half a MB of resource cache on a 1440x720
			// RSS map. The elevation caches are the exception - the live one and the parked ones alike:
			// they hold PQS samples, which cost seconds to take on RSS (8188f282), and prepRow's
			// "unsampled" check reuses them on the next pass even though the data textures are gone.
			// ParkedHeightmapBudget is what bounds the parked ones. The biome index is one stock biome-map
			// lookup per pixel to refill, and resetMap zeroes the resource cache on every pass anyway,
			// so neither is worth carrying. ensureModeCaches re-sizes whatever the next pass reads.
			biome_indexmap = null;
			biomeRowCached = null;
			resourceCache = null;
			gpuRowBuf = null;
		}

		// The data caches, sized for the mode that is about to run and no other. Altimetry and Slope
		// read the elevation cache; Biome reads the biome index, plus elevation only when it draws the
		// underlay (_HasElevation); the resource layer is the only reader of resourceCache, and
		// buildResourceCache sizes that one itself. Called from resetMap, so a pass always has what it
		// needs before willRenderGPU is asked. A body with no PQS or no biome map samples nothing
		// (gpuNoData - the shader draws static there), so it gets no cache at all.
		private void ensureModeCaches()
		{
			if (mapwidth <= 0 || mapheight <= 0)
			{
				return;
			}

			// baseNone: the resource-only planet overlay, which resetMap leaves with nothing to build.
			bool wantElev = !baseNone && pqs
				&& (mType == mapType.Altimetry || mType == mapType.Slope || (mType == mapType.Biome && profile.BiomeUnderlay));
			bool wantBiome = !baseNone && biomeMap && mType == mapType.Biome;

			if (wantElev && (big_heightmap == null || big_heightmap.GetLength(0) != mapwidth || big_heightmap.GetLength(1) != mapheight))
			{
				big_heightmap = new float[mapwidth, mapheight];
			}

			if (wantBiome)
			{
				if (biome_indexmap == null || biome_indexmap.GetLength(0) != mapwidth || biome_indexmap.GetLength(1) != mapheight)
				{
					biome_indexmap = new float[mapwidth, mapheight];
					biomeRowCached = new bool[mapheight];
				}
				else if (biomeRowCached == null || biomeRowCached.Length != mapheight)
				{
					biomeRowCached = new bool[mapheight];
				}
			}
		}

		/// <summary>
		/// Drop the claims that say the GPU already holds usable work, without touching the work itself.
		/// Anything that destroys or reallocates the data textures, the LUTs or the CPU caches behind
		/// them must call this, or resetMap will trust a cache that isn't there.
		///
		/// The resource grid and its texture are deliberately NOT dropped here, and a new cache belongs
		/// here only if its key cannot tell the difference. resourceGridKey says which FIELD was sampled
		/// - body, resource, window, grid size, biome locks - and a reset or a resize changes none of
		/// that, so a grid that still matches its key is still correct, and re-sampling it is the most
		/// expensive thing a pass can do (see buildResourceCache). The callers that really do invalidate
		/// the grid null resourceCache itself, which buildResourceCache catches by size.
		/// </summary>
		private void invalidateGpuDataCache()
		{
			dataTexKey.Invalidate();
			gpuRendered = false;
			gpuRecolorSweep = false;
			biomeLUTCount = 0;
			biomeLUTKey.Invalidate();
			paletteLUTKey.Invalidate();
			// Not a claim on anything, just this pass's memo of the resource key: the grid keeps its own.
			resourceConfigKey = 0;
		}

		internal void centerAround(double lon, double lat)
		{
			double oldLonOffset = lon_offset, oldLatOffset = lat_offset;
			double oldCenteredLong = centeredLong, oldCenteredLat = centeredLat;

			centeredLong = lon;
			centeredLat = lat;

			if (projection == MapProjection.Orthographic)
			{
				double lo = projectLongitude(lon, lat);
				double la = projectLatitude(lon, lat);
				lon_offset = 180 + lo - (mapwidth / mapscale) / 2;
				lat_offset = 90 + la - (mapheight / mapscale) / 2;
			}
			else
			{
				lon_offset = 180 + lon - (mapwidth / mapscale) / 2;
				lat_offset = 90 + lat - (mapheight / mapscale) / 2;
			}

			// A window map's caches are pixel space, so a moved window invalidates them. Same window
			// (the zoom map re-centres on every reset) keeps them, which is what makes a refresh warm.
			if (!profile.GeographicCache && (lon_offset != oldLonOffset || lat_offset != oldLatOffset || centeredLong != oldCenteredLong || centeredLat != oldCenteredLat))
				clearWindowCaches();
		}

		internal double scaleLatitude(double lat)
		{
			lat -= lat_offset;
			lat *= 180f / (mapheight / mapscale);
			return lat;
		}

		internal double scaleLongitude(double lon)
		{
			if (lon_offset < 0 && Math.Abs(lon_offset) < lon)
			{
				lon -= 360;
			}
			else if (lon_offset > 0 && Math.Abs(lon_offset) > lon)
			{
				lon += 360;
			}

			lon -= lon_offset;
			lon *= 360f / (mapwidth / mapscale);
			return lon;
		}

		private double unScaleLatitude(double lat)
		{
			lat -= lat_offset;
			lat += 90;
			lat *= mapscale;
			return lat;
		}

		private double unScaleLongitude(double lon)
		{
			lon -= lon_offset;
			lon += 180;
			lon *= mapscale;
			return lon;
		}

		private double fixUnscale(double value, int size)
		{
			if (value < 0)
			{
				value = 0;
			}
			else if (value >= (size - 0.5f))
			{
				value = size - 1;
			}

			return value;
		}

		/* MAP: internal state */
		private mapType mType;
		private mapSource mSource;
		private readonly SCANmapProfile profile;   // the fixed per-source behaviour; see SCANmapProfile
		private CelestialBody body = null; // all refs are below
		private SCANresourceGlobal resource;
		private SCANdata data;
		private SCANmapLegend mapLegend;
		private int mapstep; // all refs are below
		private bool pqs;
		private bool biomeMap;
		private float customMin;
		private float customMax;
		private float customRange;
		private float customResourceMin;
		private float customResourceMax;
		private bool useCustomRange;
		private bool colorMap;
		private bool terminator;
		private SCANtype sensorMask = SCANtype.Nothing;

		/* GPU Visual-mode compositing: renders the Visual map on the GPU sampling the body's
		   original ScaledSpace textures, so no readable CPU copy is needed. Dormant until the
		   composite shader is present in the bundle; otherwise the CPU renderer below runs. */
		private RenderTexture visualRenderTex;
		private Material compositeMaterial;
		private Texture2D coverageFlags;
		private Color32[] coverageFlagsBuf;   // reused CPU buffer for coverageFlags (no per-frame alloc)
		private bool coverageFlagsDirty = true;   // set each resetMap; rebuild the coverage texture once per pass, not per sweep row
		private bool gpuRendered;      // the render target exists at the map's size and DisplayTexture hands it out
			// GPU data textures for the non-Visual modes (all-modes port). Elevation/biome/resource
			// upload from the CPU caches (big_heightmap / biome_indexmap / resourceCache); a 1-D palette
			// LUT baked from heightToColor colourizes altimetry (legend parity). Shader branches _MapMode.
			private Texture2D elevationTex;
			private Texture2D biomeIndexTex;
			private Texture2D resourceTex;
			private Texture2D paletteLUT;
			private Texture2D paletteGreyLUT;   // LoRes-only altimetry grey ramp
			private Texture2D biomeLUT;         // stock biome mapColors
			private int biomeLUTCount;          // how many entries it holds; also the shader's _BiomeCount
			private CacheKey biomeLUTKey;       // the body and biome count it was baked for
			private float[,] biome_indexmap;
			private bool[] biomeRowCached;   // biome_indexmap row y is fully sampled for the current body / size / projection
			// Per-pass diagnostics, logged once when a GPU data pass completes.
			private int passHeightSamples;
			private int passBiomeSamples;
			private int passBuildFrames;
			private float passBuildStart = -1f;
			private float passBuildMs;
			private Color[] gpuRowBuf;
			private CacheKey resourceGridKey;   // the field resourceCache was sampled from
			private CacheKey resourceTexKey;    // the grid resourceTex was uploaded from
			private CacheKey paletteLUTKey;
			private CacheKey dataTexKey;        // the pass config elevationTex / biomeIndexTex were fully sampled for
			private int pendingDataKey;         // the key the build now in flight will stamp on dataTexKey when it finishes
			// resourceConfigHash walks the body's biomes under BiomeLock and the composite runs every
			// frame of the sweep, so the key is taken once a pass and both resource caches compare
			// against that. 0 = not taken yet this pass.
			private int resourceConfigKey;
			private bool gpuRecolorSweep;   // cosmetic re-sweep in progress: re-Blit cached data with a new LUT (no re-sample)
		// The GPU compositor draws the whole Visual map in one Blit; the scanline is a purely cosmetic
		// reveal (in the CPU path's row order) so it matches the CPU modes' look. Visual and the
		// recolour re-sweep have their end state on the first Blit, so their line is paced by wall-clock
		// time (the scanline setting) rather than by pump calls: a pass takes the same time on any map
		// size, frame rate or map generation budget. The data modes build row by row, so their line tracks
		// mapstep instead. passComplete gates isMapComplete so the pump keeps re-compositing until the
		// reveal finishes.
		private bool passComplete = true;   // the fully revealed composite happened, or nothing renders this map; true until a reset starts a pass, so an unsized map is never pumped
		private float sweepStart = -1f;   // realtimeSinceStartup at this pass's first composite; -1 until then
		private float sweepDuration = BaseSweepDuration;   // this pass's sweep length; taken when its clock starts
		private const float BaseSweepDuration = 1f;        // the sweep at 1x - the scanline setting scales this
		private float noiseSeed;          // per-pass seed for the shader's no-data static (re-rolled in resetMap like the CPU's Random.value per pass)

		// What the planet overlay changes per selection, through the properties above. The fixed
		// per-source switches are the profile's.
		private bool baseNone;               // true: no base layer, only the resource pass over clear (resource planet overlay)
		private float outputAlpha = 1f;      // final multiplier on the composite (terrain planet overlay: 0.9)
		private float resGreyBlend = 0.3f;   // resourceToColor32's Transparency argument for below-range cells
		private int rangeRow = -1;           // autoRange pre-pass cursor: -1 not started, >= mapheight done
		private float rangeMin, rangeMax;
		private int rangeSamples;

		/* MAP: nearly trivial functions */
		public void setBody(CelestialBody b)
		{
			SCANcontroller.controller.unloadPQS(body, mSource);
			SCANcontroller.controller.unloadOnDemandScaledSpace(body, mSource);

			bool bodyChanged = body != b;
			int outgoingBody = body != null ? body.flightGlobalsIndex : -1;   // `body` is reassigned below

			if (bodyChanged)
			{
				SCANcontroller.controller.UnloadVisualMapTexture(body, mSource);
				body = b;
				SCANcontroller.controller.loadOnDemandScaledSpace(body, mSource);
			}
			else
			{
				SCANcontroller.controller.loadOnDemandScaledSpace(body, mSource);
			}

			// Visual textures are registered per body and loaded lazily at the needed mip; only for that mode.
			refreshVisualMapTexture();

			data = SCANUtil.getData(body);

			SCANcontroller.controller.loadPQS(body, mSource);
			pqs = body.pqsController != null;
			biomeMap = body.BiomeMap != null;

			// The height and biome-index caches are per body: terrain and biomes do not change at
			// runtime, and new coverage is picked up per pixel by the "unsampled" checks. So they
			// survive same-body calls (the big map calls setBody on every open), and on a real body
			// change the height grid is parked rather than zeroed - see swapParkedHeightmap. The biome
			// index is not worth parking: one stock biome-map lookup per pixel to refill.
			if (profile.GeographicCache && bodyChanged)
			{
				swapParkedHeightmap(outgoingBody, body.flightGlobalsIndex);
				clearBiomeRowCache();
			}

			if (SCANconfigLoader.GlobalResource)
			{
				if (resource != null)
				{
					resource.CurrentBodyConfig(body.bodyName);
				}
			}
		}

		/// <summary>
		/// Visual mode's texture sources are per-body (SCANcontroller.mapTextureHandler): the
		/// SCANSAT_BODY_TEXTURES paths and the GPU textures loaded from them. Claim the body for this
		/// map source the first time this map shows Visual, and hold that claim while the map stays on
		/// the body - a mode toggle does NOT release it. Releasing on every non-Visual mode meant
		/// Visual -> Altimetry -> Visual destroyed the body's textures and read them again, and for a
		/// file with no mip chain (most RSS colour maps: 16384x8192 DXT5, one level) that is the whole
		/// 128 MiB off disk, about 85 ms on the main thread, per toggle - twice, with the normal map.
		/// The claim is released where the body really goes away: setBody on a body change, the window's
		/// Close, this map's Destroy, and the controller's own scene teardown. Nothing is held for a map
		/// that never shows Visual.
		/// </summary>
		private void refreshVisualMapTexture()
		{
			if (SCANcontroller.controller == null || body == null)
			{
				return;
			}

			if (mType == mapType.Visual)
			{
				SCANcontroller.controller.LoadVisualMapTexture_Renamed(body, mSource);
			}
		}

		public void setCustomRange(float min, float max, float rMin, float rMax)
		{
			useCustomRange = true;
			customMin = min;
			customMax = max;
			customRange = max - min;
			customResourceMin = rMin;
			customResourceMax = rMax;
		}

		// A pass holds "incomplete" until its cosmetic sweep finishes, so the pump keeps re-compositing
		// the advancing scanline; a map nothing renders completes on its first pump.
		internal bool isMapComplete()
		{
			return passComplete;
		}

		public void resetMap(bool resourceOn, bool setRes = true)
		{
			passComplete = false;
			gpuRecolorSweep = false;   // a recolour in flight ends here; the decision below is made afresh. Left set, a mode switch mid-sweep re-Blit the new mode from textures never built for it.
			sweepStart = -1f;
			rangeRow = -1;
			noiseSeed = UnityEngine.Random.value;
			passHeightSamples = 0;
			passBiomeSamples = 0;
			passBuildFrames = 0;
			passBuildStart = -1f;
			passBuildMs = 0f;
			resourceConfigKey = 0;       // new pass: take the resource key again (the field may have changed under us)
			coverageFlagsDirty = true;   // new pass: refresh the GPU coverage stencil once from live coverage
			resourceActive = resourceOn;
			heightGridPass = profile.HeightGrid && data != null && data.Built && mapwidth == 360 && mapheight == 180 && projection == MapProjection.Rectangular;
			ensureModeCaches();   // this pass's caches, and only this pass's
			if (SCANconfigLoader.GlobalResource && setRes)
			{ //Make sure that a resource is initialized if necessary
				if (resource != null && body != null)
				{
					resource.CurrentBodyConfig(body.bodyName);
				}

				resetResourceMap();
			}

			if (terminator)
			{
				double sunLon = body.GetLongitude(Planetarium.fetch.Sun.position, false);
				double sunLat = body.GetLatitude(Planetarium.fetch.Sun.position, false);

				sunLatCenter = SCANUtil.fixLatShift(sunLat);

				if (sunLatCenter >= 0)
				{
					sunLonCenter = SCANUtil.fixLonShift(sunLon + 90);
				}
				else
				{
					sunLonCenter = SCANUtil.fixLonShift(sunLon - 90);
				}

				gamma = Math.Abs(sunLatCenter) < 0.55 ? 100 : Math.Tan(Mathf.Deg2Rad * (90 - Math.Abs(sunLatCenter)));
			}

			// mType is set (via MType or the resetMap(mode,...) overload) before every call
			// here, so this covers map-type switches that don't go through setBody.
			refreshVisualMapTexture();

			// What this pass has to build. Visual has nothing: its end state is one composite of the body's
			// textures. A body without PQS or without a biome map has nothing either (the shader draws
			// its static), nor has the resource-only overlay (the composite builds its cache lazily). A
			// data mode whose cache is already fully sampled for this exact config - a colourisation-only
			// change (palette, clamp, terminator) leaves the config hash the same - re-Blits the cached
			// data with the rebuilt LUT under a new sweep, no re-sample. Everything else builds from row 0.
			mapstep = mapheight;
			bool dataMode = mType == mapType.Altimetry || mType == mapType.Slope || mType == mapType.Biome;

			if (dataMode && !gpuNoData() && !baseNone && willRenderGPU(mType))
			{
				// The config hash includes a coverage checksum. A cell scanned after its row was sampled
				// is in the next pass's stencil but not in the data (it uploads as 0 m and draws black),
				// so a build is stamped with the coverage it STARTED from, never the coverage at its end:
				// any growth during the pass fails this compare and forces a rebuild, which samples
				// exactly the new cells.
				int configHash = gpuConfigHash();

				// A resource layer used to force the full prep here, because resetResourceMap wiped the
				// abundance grid on every reset. It keeps the grid now (resourceConfigHash decides when to
				// re-sample), so a resource map takes this shortcut like any other: the composite's lazy
				// buildResourceCache finds the grid it already has.
				if (dataTexKey.Valid(configHash))
				{
					// dataTexKey stays the build's: a recolour adds no samples, so it must not claim
					// coverage that arrived during it.
					gpuRecolorSweep = true;
				}
				else
				{
					// A full data build starts now and overwrites the data textures row by row. Until it
					// completes (buildSlice stamps the key again) they hold a mix of passes, so a reset in
					// the meantime must not take the shortcut above.
					dataTexKey.Invalidate();
					pendingDataKey = configHash;
					mapstep = 0;
				}
			}
		}

		public void resetMap(mapType mode, bool resourceOn, bool setRes = true)
		{
			mType = mode;
			resetMap(resourceOn, setRes);
		}

		public void resetResourceMap()
		{
			if (profile.ResourceGridFromSettings)
			{
				if (SCAN_Settings_Config.Instance.ResourceMapHeight != resourceMapHeight)
				{
					resourceMapHeight = SCAN_Settings_Config.Instance.ResourceMapHeight;
					resourceMapWidth = resourceMapHeight * 2;
					resourceMapScale = resourceMapWidth / 360f;
					resourceCache = null;   // the new size is buildResourceCache's to allocate, if a layer asks
				}

				if (SCAN_Settings_Config.Instance.Interpolation != resourceInterpolation)
				{
					resourceInterpolation = SCAN_Settings_Config.Instance.Interpolation;
				}
			}

			// The grid itself is kept. Stock abundance is a static field: it does not change with scan
			// coverage (the shader masks the layer with the live coverage texture), with the cutoff, or with
			// the layer's colours - those are all shader uniforms. buildResourceCache re-samples only when
			// its config key says the grid is stale, and clears it itself when it does. Wiping it here made
			// every reset - a refresh, a cutoff nudge, a colour change - pay for a full re-sample.
		}

		/* MAP: export: PNG file */
		private SCANmapExporter exporter;

		internal void exportPNG()
		{
			if (exporter == null)
			{
				UnityEngine.GameObject obj = new GameObject();

				exporter = obj.gameObject.AddComponent<SCANmapExporter>();
			}

			if (exporter.Exporting)
			{
				return;
			}

			exporter.exportPNG(this, data);
		}

		#endregion

		#region Big Map Texture Generator

		// Pixels this map needs across 360 degrees of longitude at its current scale - what the Visual
		// source has to cover: the big map's width, or the zoom map's scale times 360.
		private int visualTargetWidth()
		{
			return Mathf.Max(1, Mathf.CeilToInt((float)(mapscale * 360.0)));
		}

		// True when the GPU compositor can render this map at all: the composite shader loaded, the map
		// has a body and data, and either Visual maps are switched on or the mode is one of the data
		// modes with a size. getPartialMap draws nothing when this is false.
		private bool willRenderGPU(mapType m)
		{
			if (SCAN_UI_Loader.VisualCompositeShader == null || body == null || data == null || SCANcontroller.controller == null)
				return false;
			switch (m)
			{
				// The setting disables Visual maps outright (the CPU path only honoured it by accident). A
				// body without a source texture still renders on the GPU: the shader draws unscanned (_HasSource).
				case mapType.Visual: return SCAN_Settings_Config.Instance.VisibleMapsActive;
				// The data modes need a size: geographic for the big map (setWidth), pixel space for a
				// window map (setSize). ensureModeCaches allocates the mode's own cache in resetMap, so an
				// array is not the test here - a lazily sized map would fail it and fall through to "no
				// renderer", a blank window. A body without PQS or without a biome map still renders on the
				// GPU: the shader draws the CPU renderers' black-white static there (_NoData).
				case mapType.Altimetry:
				case mapType.Slope:
				case mapType.Biome: return mapwidth > 0 && mapheight > 0;
				default: return false;
			}
		}

		// The render target, at the map's size, filled with the map background when created - as the CPU
		// path filled a fresh Texture2D. Creation and resize only: a pass overwrites rows under the
		// scanline and leaves the rest standing (the shader discards ahead of the sweep), which is how
		// every map behaved on the CPU. First thing in every pump, so DisplayTexture already hands the
		// target out on the frame the window consumes it - the first composite may be frames away (a
		// cold build, the range pre-pass) and the window is only re-pointed when its updateMap flag says.
		private void ensureRenderTex()
		{
			if (visualRenderTex == null || visualRenderTex.width != mapwidth || visualRenderTex.height != mapheight)
			{
				if (visualRenderTex != null)
				{
					// Release() frees the GPU surface but not the RT object; Destroy the wrapper too
					// or the old RenderTexture leaks until finalization on every resize.
					visualRenderTex.Release();
					UnityEngine.Object.Destroy(visualRenderTex);
				}

				visualRenderTex = new RenderTexture(mapwidth, mapheight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
				visualRenderTex.wrapMode = TextureWrapMode.Clamp;

				RenderTexture prev = RenderTexture.active;
				RenderTexture.active = visualRenderTex;
				Color bg = SCAN_Settings_Config.Instance.MapBackgroundColor;
				bg.a *= SCAN_Settings_Config.Instance.BackgroundTransparency;
				GL.Clear(false, true, bg);
				RenderTexture.active = prev;
			}

			gpuRendered = true;
		}

		// One composite of this map into its render target: every uniform from the map's current state,
		// the mode's data textures and LUTs (setModeUniforms), and the reveal. Rows ahead of the line
		// keep the previous pass (shader discard), the frontier is the redline. The line is paced by
		// wall-clock time (timedSweepFraction) but never runs ahead of the rows built: Visual and a
		// recolour have their end state at once (resetMap left nothing to build), so they always get
		// the full timed sweep; a data pass shows the line at the real sampling pace when that is
		// slower. The fully revealed composite completes the pass.
		private void composite()
		{
			Shader shader = SCAN_UI_Loader.VisualCompositeShader;

			SCANcontroller.controller.getVisualSource(body, visualTargetWidth(), out Texture colorTex, out Texture normalTex, out int normalYChannel, out Texture gasRamp, out _);

			if (compositeMaterial == null || compositeMaterial.shader != shader)
				compositeMaterial = new Material(shader);

			updateCoverageFlags();

			compositeMaterial.SetTexture("_ScaledColor", colorTex);
			compositeMaterial.SetTexture("_ScaledNormal", normalTex);
			compositeMaterial.SetTexture("_GasRamp", gasRamp);
			compositeMaterial.SetTexture("_CoverageFlags", coverageFlags);

			compositeMaterial.SetFloat("_MapWidth", mapwidth);
			compositeMaterial.SetFloat("_MapHeight", mapheight);
			compositeMaterial.SetFloat("_MapScale", (float)mapscale);
			compositeMaterial.SetFloat("_LonOffset", (float)lon_offset);
			compositeMaterial.SetFloat("_LatOffset", (float)lat_offset);
			compositeMaterial.SetFloat("_Projection", (float)(int)projection);
			compositeMaterial.SetFloat("_CenteredLon", (float)centeredLong);
			compositeMaterial.SetFloat("_CenteredLat", (float)centeredLat);
			compositeMaterial.SetFloat("_ColorMode", colorMap ? 1f : 0f);
			compositeMaterial.SetFloat("_HasNormal", normalTex != null ? 1f : 0f);
			compositeMaterial.SetFloat("_NormalYChannel", normalYChannel);
			// A gas giant hands over a cloud pattern rather than a colour map; the shader colours it
			// through the body's own gradient (SCANcontroller.GetVisualMapTexturesForBody).
			compositeMaterial.SetFloat("_GasGiant", gasRamp != null ? 1f : 0f);
			compositeMaterial.SetFloat("_Terminator", terminator ? 1f : 0f);
			compositeMaterial.SetFloat("_SunLonCenter", (float)sunLonCenter);
			compositeMaterial.SetFloat("_SunLatCenter", (float)sunLatCenter);
			compositeMaterial.SetFloat("_Gamma", (float)gamma);

			// Data-texture addressing, one rule for the three data textures (elevation, biome index,
			// resource - the shader's dataUVAt), and the classic-renderer details the shader reproduces. A
			// source with GeographicCache (the big map, the planet overlay) caches them over the globe, so
			// the shader reads them through the unprojected lon/lat and a projection change or a
			// re-centring keeps them; every other source caches its own window in pixel space, filled per
			// rendered pixel, and the shader reads them by pixel uv.
			compositeMaterial.SetFloat("_PixelSpace", profile.GeographicCache ? 0f : 1f);
			compositeMaterial.SetFloat("_RowMin", startLine);
			compositeMaterial.SetFloat("_RowMax", stopLine);
			compositeMaterial.SetFloat("_Grid", profile.GridDots ? 1f : 0f);
			compositeMaterial.SetFloat("_SensorMask", (short)sensorMask);   // 0 for every source but the small map
			compositeMaterial.SetFloat("_HasSource", colorTex != null ? 1f : 0f);
			compositeMaterial.SetFloat("_NoData", gpuNoData() ? 1f : 0f);
			compositeMaterial.SetFloat("_NoiseSeed", noiseSeed);
			compositeMaterial.SetFloat("_SweepBand", 2f);
			compositeMaterial.SetFloat("_BaseNone", baseNone ? 1f : 0f);
			compositeMaterial.SetFloat("_PlanetUV", profile.PlanetUV ? 1f : 0f);
			compositeMaterial.SetFloat("_OutputAlpha", outputAlpha);
			compositeMaterial.SetFloat("_ResGreyBlend", resGreyBlend);

			Color unscanned;
			switch (profile.Unscanned)
			{
				case SCANmapProfile.UnscannedFill.Grey:
					unscanned = palette.Grey;
					unscanned.a = 1f;
					break;
				case SCANmapProfile.UnscannedFill.Clear:
					unscanned = palette.Clear;
					break;
				default:
					unscanned = SCAN_Settings_Config.Instance.UnscannedColor;
					unscanned.a *= SCAN_Settings_Config.Instance.UnscannedTransparency;
					break;
			}
			compositeMaterial.SetColor("_UnscannedColor", unscanned);
			compositeMaterial.SetColor("_ClearColor", palette.Clear);
			Color greyCol = palette.Grey; greyCol.a = 1f;
			compositeMaterial.SetColor("_GreyColor", greyCol);

			// Mode select + the non-Visual data textures / LUTs / overlay uniforms.
			compositeMaterial.SetFloat("_MapMode", (float)(int)mType);
			setModeUniforms();

			// Re-compositing each frame with a new reveal fraction animates the RawImage - already pointed
			// at visualRenderTex - in place. A source without a sweep (the planet overlay) composites once,
			// fully revealed, when its build is done.
			float reveal = 1f;
			if (profile.Sweep && mapheight > 0)
				reveal = Mathf.Min(timedSweepFraction(), Mathf.Clamp01(mapstep / (float)mapheight));

			compositeMaterial.SetColor("_RedlineColor", palette.Red);
			compositeMaterial.SetFloat("_SweepY", reveal);

			Graphics.Blit(null, visualRenderTex, compositeMaterial);

			if (reveal >= 1f)
				passComplete = true;   // that Blit was the fully revealed one (no redline)
		}

		// The big map's graticule as a texture for the UI's grid layer, which sits above the map with
		// the layer's own colour and alpha exactly as before. Same pattern, same placement as the old
		// CPU GenerateGridMap: every 2 degrees along each 30-degree meridian and parallel, the lattice
		// point is projected with this map's own projection, truncated to a pixel, and that pixel is
		// white with a black pixel on each of its four sides - here as GL quads rasterised by the GPU
		// into a RenderTexture, then read back once into a Texture2D (reused when the size fits). The
		// caller re-renders on projection and size changes. Rectangular, Kavrayskiy and Polar only,
		// the projections the big map offers.
		internal Texture2D renderGrid(Texture2D reuse)
		{
			return renderGrid(reuse, mapwidth, mapheight);
		}

		// w x h is the texture size: the map's own size gives the classic texture; the map's on-screen
		// pixel size gives one texel per screen pixel, so the dots are not resampled by the UI's
		// scaling and come out uniform (the caller re-renders when the on-screen size changes).
		internal Texture2D renderGrid(Texture2D reuse, int w, int h)
		{
			if (w <= 0 || h <= 0)
			{
				return null;
			}

			Material mat = gridMaterial();

			if (mat == null)
			{
				return null;
			}

			double sx = w / 360.0;   // the big map covers the globe with no offset: pixels per projected degree
			double sy = h / 180.0;

			RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
			RenderTexture prev = RenderTexture.active;
			RenderTexture.active = rt;

			GL.Clear(false, true, palette.clear);
			GL.PushMatrix();
			GL.LoadPixelMatrix(0, w, 0, h);   // pixel (x, y), row 0 at the bottom like Texture2D.SetPixels
			mat.SetPass(0);
			GL.Begin(GL.QUADS);

			for (double lat = -90; lat < 90; lat += 2)
			{
				for (double lon = -180; lon < 180; lon += 2)
				{
					if (lat % 30 != 0 && lon % 30 != 0)
					{
						continue;
					}

					double px = sx * ((projectLongitude(lon, lat) + 180) % 360);
					double py = sy * ((projectLatitude(lon, lat) + 90) % 180);

					if (double.IsNaN(px) || double.IsNaN(py))
					{
						continue;
					}

					int x = (int)px;
					int y = (int)py;

					if (x < 0 || x >= w || y < 0 || y >= h)
					{
						continue;
					}

					gridPixel(x, y, palette.white);

					if (x < w - 1) gridPixel(x + 1, y, palette.black);
					if (x > 0) gridPixel(x - 1, y, palette.black);
					if (y < h - 1) gridPixel(x, y + 1, palette.black);
					if (y > 0) gridPixel(x, y - 1, palette.black);
				}
			}

			GL.End();
			GL.PopMatrix();

			Texture2D tex = reuse;

			if (tex == null || tex.width != w || tex.height != h)
			{
				if (tex != null)
					UnityEngine.Object.Destroy(tex);
				tex = new Texture2D(w, h, TextureFormat.ARGB32, false);
			}

			tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
			tex.Apply();
			RenderTexture.active = prev;
			RenderTexture.ReleaseTemporary(rt);

			return tex;
		}

		// Vertex-colour material for the grid quads. Unity's built-in Internal-Colored writes all four
		// channels; the KSP particle shader RPM uses for its trails masks alpha out, which left the
		// read-back grid texture fully transparent. Opaque overwrite, like the CPU's array writes.
		private static Material gridMat;

		private static Material gridMaterial()
		{
			if (gridMat != null)
			{
				return gridMat;
			}

			Shader s = Shader.Find("Hidden/Internal-Colored");

			if (s == null)
			{
				s = Shader.Find("UI/Default");
			}

			if (s == null)
			{
				return null;
			}

			gridMat = new Material(s);
			gridMat.hideFlags = HideFlags.HideAndDontSave;
			gridMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
			gridMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
			gridMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
			gridMat.SetInt("_ZWrite", 0);
			gridMat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
			return gridMat;
		}

		// One map pixel as a GL quad (pixel matrix: x, y in pixels).
		private static void gridPixel(int x, int y, Color c)
		{
			GL.Color(c);
			GL.Vertex3(x, y, 0f);
			GL.Vertex3(x + 1, y, 0f);
			GL.Vertex3(x + 1, y + 1, 0f);
			GL.Vertex3(x, y + 1, 0f);
		}

		// Wall-clock reveal fraction for a sweep whose end state is already rendered. The clock starts
		// at the pass's first composite, not at resetMap, so a map reset while hidden still plays its
		// sweep when shown. Real time: physics warp scales Time.deltaTime, and the UI runs while paused.
		private float timedSweepFraction()
		{
			float now = Time.realtimeSinceStartup;
			if (sweepStart < 0f)
			{
				sweepStart = now;
				sweepDuration = configuredSweepDuration();
			}

			if (sweepDuration <= 0f)
				return 1f;   // Instant: no reveal, the pass shows whatever it has

			return Mathf.Clamp01((now - sweepStart) / sweepDuration);
		}

		// The scanline setting is a speed multiplier on the one second sweep, 0 meaning Instant. Taken
		// once per pass rather than read per frame: moving the slider mid-sweep would otherwise rescale
		// the elapsed fraction and run the line back up over rows it had already revealed.
		private static float configuredSweepDuration()
		{
			float speed = SCAN_Settings_Config.Instance.ScanlineSpeed;
			return speed > 0f ? BaseSweepDuration / speed : 0f;
		}

		/// <summary>
		/// Render this Visual map again at up to <paramref name="width"/> pixels wide into a fresh
		/// RenderTexture (caller releases it), for PNG export. Only Visual is texture-backed, so only
		/// Visual gains anything from a larger render; the source mip is raised to match. Returns null
		/// when that does not apply or the request is not larger than the on-screen map.
		/// </summary>
		internal RenderTexture renderVisualExport(int width)
		{
			if (mType != mapType.Visual || !gpuRendered || compositeMaterial == null || body == null || SCANcontroller.controller == null)
			{
				return null;
			}

			int w = Mathf.Min(width, 16384);

			if (w <= mapwidth || mapwidth <= 0)
			{
				return null;
			}

			double k = (double)w / mapwidth;
			int h = Mathf.Max(1, Mathf.RoundToInt((float)(mapheight * k)));
			int sourceWidth = Mathf.CeilToInt((float)(mapscale * k * 360.0));

			if (!SCANcontroller.controller.getVisualSource(body, sourceWidth, out Texture colorTex, out Texture normalTex, out int normalYChannel, out Texture gasRamp, out _))
			{
				return null;
			}

			RenderTexture rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
			rt.wrapMode = TextureWrapMode.Clamp;
			rt.Create();

			// Same window, k times the pixels: scale the size and the pixels-per-degree together. Every
			// other uniform is still set from the last on-screen render; the next composite resets
			// all of them, so nothing here needs restoring.
			compositeMaterial.SetTexture("_ScaledColor", colorTex);
			compositeMaterial.SetTexture("_ScaledNormal", normalTex);
			compositeMaterial.SetTexture("_GasRamp", gasRamp);
			compositeMaterial.SetFloat("_HasNormal", normalTex != null ? 1f : 0f);
			compositeMaterial.SetFloat("_NormalYChannel", normalYChannel);
			compositeMaterial.SetFloat("_GasGiant", gasRamp != null ? 1f : 0f);
			setFramingUniforms(w, h, mapscale * k);

			Graphics.Blit(null, rt, compositeMaterial);

			SCANUtil.SCANlog("[{0}] Visual export rendered at {1}x{2} (on-screen {3}x{4})", body.bodyName, w, h, mapwidth, mapheight);
			return rt;
		}

		// Per-mode data textures + uniforms for composite. Visual's ScaledSpace textures are set by
		// the caller; here we upload the CPU-cache data for Altimetry/Slope/Biome + the resource overlay.
		private void setModeUniforms()
		{
			if (mType == mapType.Altimetry || mType == mapType.Slope)
			{
				compositeMaterial.SetTexture("_ElevationTex", elevationTex);   // uploaded incrementally per row in getPartialMap

				float tMin, tRange;
				if (useCustomRange) { tMin = customMin; tRange = customRange; }
				else { SCANterrainConfig tc = SCANUtil.getTerrainConfig(data); tMin = tc.MinTerrain; tRange = tc.MaxTerrain - tc.MinTerrain; }
				if (tRange <= 0f) tRange = 1f;
				compositeMaterial.SetFloat("_TerrainMin", tMin);
				compositeMaterial.SetFloat("_TerrainRange", tRange);

				if (mType == mapType.Altimetry)
				{
					buildPaletteLUT(tMin, tRange);
					compositeMaterial.SetTexture("_PaletteLUT", paletteLUT);
					compositeMaterial.SetTexture("_PaletteGreyLUT", paletteGreyLUT);
				}
				else
				{
					compositeMaterial.SetFloat("_SlopeCutoff", SCAN_Settings_Config.Instance.SlopeCutoff);
					compositeMaterial.SetColor("_SlopeLoColorOne", SCANcontroller.controller.lowSlopeColorOne32);
					compositeMaterial.SetColor("_SlopeHiColorOne", SCANcontroller.controller.highSlopeColorOne32);
					compositeMaterial.SetColor("_SlopeLoColorTwo", SCANcontroller.controller.lowSlopeColorTwo32);
					compositeMaterial.SetColor("_SlopeHiColorTwo", SCANcontroller.controller.highSlopeColorTwo32);
				}
			}
			else if (mType == mapType.Biome)
			{
				compositeMaterial.SetTexture("_BiomeIndexTex", biomeIndexTex);   // uploaded incrementally per row in getPartialMap
				compositeMaterial.SetColor("_LowBiomeColor", SCANcontroller.controller.lowBiomeColor32);
				compositeMaterial.SetColor("_HighBiomeColor", SCANcontroller.controller.highBiomeColor32);
				// Per-source toggles: the owning window's own settings block (see SCANmapProfile.BiomeToggles).
				profile.BiomeToggles(colorMap, out bool border, out bool stock, out float biomeTransparency);
				compositeMaterial.SetFloat("_BiomeTransparency", biomeTransparency);
				compositeMaterial.SetFloat("_BiomeBorder", border ? 1f : 0f);
				buildBiomeLUT();
				compositeMaterial.SetTexture("_BiomeLUT", biomeLUT);
				compositeMaterial.SetFloat("_BiomeCount", biomeLUTCount);
				compositeMaterial.SetFloat("_StockBiomes", stock ? 1f : 0f);
				// Elevation underlay: biome blends its colour with grey elevation by BiomeTransparency.
				// _HasElevation 0 (no PQS, or a source that samples no elevation at all) leaves the underlay
				// grey in the shader, which is what the CPU renderer did; the shader greys pixels with no
				// altimetry coverage too. The range is the window-fitted one wherever the map fits its own
				// (zoom map, RPM - their Altimetry already uses it), the body's terrain config otherwise.
				compositeMaterial.SetTexture("_ElevationTex", elevationTex);
				compositeMaterial.SetFloat("_HasElevation", (pqs && profile.BiomeUnderlay) ? 1f : 0f);
				float bMin, bRange;
				if (useCustomRange) { bMin = customMin; bRange = customRange; }
				else { SCANterrainConfig tc = SCANUtil.getTerrainConfig(data); bMin = tc.MinTerrain; bRange = tc.MaxTerrain - tc.MinTerrain; }
				if (bRange <= 0f) bRange = 1f;
				compositeMaterial.SetFloat("_TerrainMin", bMin);
				compositeMaterial.SetFloat("_TerrainRange", bRange);
			}

			bool resOn = resourceActive && SCANconfigLoader.GlobalResource && resource != null;
			compositeMaterial.SetFloat("_ResourceActive", resOn ? 1f : 0f);
			if (resOn)
			{
				// Both caches are keyed, so calling them every frame of the sweep is a key compare and a
				// return. The key itself is not free - under BiomeLock it walks the body's biomes - so it
				// is taken once a pass and handed to both.
				if (resourceConfigKey == 0)
					resourceConfigKey = resourceConfigHash();

				buildResourceCache(resourceConfigKey);
				uploadResourceTexture();
				compositeMaterial.SetTexture("_ResourceTex", resourceTex);
				float minR = useCustomRange ? customResourceMin : resource.CurrentBody.MinValue;
				float maxR = useCustomRange ? customResourceMax : resource.CurrentBody.MaxValue;
				compositeMaterial.SetColor("_ResMinColor", resource.MinColor32);
				compositeMaterial.SetColor("_ResMaxColor", resource.MaxColor32);
				compositeMaterial.SetFloat("_ResMinRange", minR);
				compositeMaterial.SetFloat("_ResMaxRange", maxR);
				compositeMaterial.SetFloat("_ResTransparency", resource.Transparency / 100f);
			}
		}

		/// <summary>
		/// One piece of cached GPU work and the config key it was built for. Valid(key) answers "is what
		/// I am holding still what you are asking for", Stamp(key) claims it once the work is done, and
		/// Invalidate() drops the claim without touching the work itself. Key 0 means "nothing valid
		/// here", so every key builder in this class ends with seal().
		///
		/// Not every cache here is one of these, and the two that are not are not oversights: the biome
		/// row flags are per row and the elevation grid uses a per-cell "unsampled" sentinel, because
		/// both are filled a row at a time and are useful half-built. These are all-or-nothing.
		/// </summary>
		private struct CacheKey
		{
			private int key;

			/// <summary>The key this was stamped with; 0 when nothing valid is held.</summary>
			internal int Key
			{
				get { return key; }
			}

			internal bool Valid(int wanted)
			{
				return key != 0 && key == wanted;
			}

			internal void Stamp(int wanted)
			{
				key = wanted;
			}

			internal void Invalidate()
			{
				key = 0;
			}
		}

		// Every config key below is folded the same way, so that none of them is quietly weaker than its
		// neighbours and a new one is written by copying an old one. seal keeps 0 for CacheKey's
		// "nothing valid here", so a key that happens to fold to zero is nudged off it.
		private static int mix(int h, int v)
		{
			return h * 31 + v;
		}

		private static int seal(int h)
		{
			return h == 0 ? 1 : h;
		}

		// Config key (body/mode/projection/size/offsets). If it matches the value cached when the data
		// finished sampling, the change was colourisation-only (palette/clamp/terminator) and we can
		// instant-recolour from the cached data textures instead of re-sweeping the whole map.
		private int gpuConfigHash()
		{
			int h = body != null ? body.flightGlobalsIndex : -1;
			h = mix(h, (int)mType);
			h = mix(h, (int)projection);
			h = mix(h, mapwidth);
			h = mix(h, mapheight);
			h = mix(h, mapscale.GetHashCode());   // a window map's zoom level
			h = mix(h, lon_offset.GetHashCode());
			h = mix(h, lat_offset.GetHashCode());
			h = mix(h, centeredLat.GetHashCode());
			h = mix(h, centeredLong.GetHashCode());
			// Coverage growth (live scanning) must defeat the instant-recolour shortcut: the cached data
			// textures hold no samples for newly covered pixels - they upload as 0 m, which the LoRes
			// grey ramp draws nearly black. A real pass samples just the new pixels. 64,800 shorts, per reset.
			h = mix(h, coverageChecksum());
			return seal(h);
		}

		/// <summary>
		/// Config key for resourceCache: everything generateResourceCache and the interpolation passes
		/// read. Deliberately NOT coverage - the shader masks the layer with the live coverage texture, so
		/// newly scanned ground reveals from the grid that is already there - and not the cutoff, the
		/// range or the layer's colours, which are shader uniforms the composite sets every frame.
		/// </summary>
		private int resourceConfigHash()
		{
			int h = body != null ? body.flightGlobalsIndex : -1;
			h = mix(h, resource != null && resource.Name != null ? resource.Name.GetHashCode() : 0);
			h = mix(h, SCAN_Settings_Config.Instance.BiomeLock ? 1 : 0);   // ResourceOverlay's CheckForLock
			h = mix(h, resourceMapWidth);
			h = mix(h, resourceMapHeight);
			h = mix(h, resourceInterpolation);
			// A window map's grid covers its own window, not the globe, unprojected per cell: it re-samples
			// when the window moves, zooms or changes projection. A geographic grid is the globe whatever
			// projection is shown.
			if (!profile.GeographicCache)
			{
				h = mix(h, (int)projection);
				h = mix(h, lon_offset.GetHashCode());
				h = mix(h, lat_offset.GetHashCode());
				h = mix(h, centeredLat.GetHashCode());
				h = mix(h, centeredLong.GetHashCode());
			}
			// The interpolated cells are noise: randomEdges picks the lerp per cell, the zoom map's hard
			// edges mirror where a globe wraps, and the sequence is seeded from the save's resource seed.
			h = mix(h, randomEdges ? 1 : 0);
			h = mix(h, (int)mSource);
			h = mix(h, ResourceScenario.Instance != null ? ResourceScenario.Instance.gameSettings.Seed : 0);

			// The one part of the field that play can change. With the lock on, stock hands back the biome's
			// average until a surface scan unlocks that biome, so landing a scanner has to re-sample - which
			// is what happened for free when every reset re-sampled. A few dozen lookups per reset.
			if (SCAN_Settings_Config.Instance.BiomeLock && ResourceMap.Instance != null
				&& body != null && body.BiomeMap != null && body.BiomeMap.Attributes != null)
			{
				CBAttributeMapSO.MapAttribute[] atts = body.BiomeMap.Attributes;

				for (int i = 0; i < atts.Length; i++)
				{
					if (atts[i] == null)
					{
						continue;
					}

					h = mix(h, ResourceMap.Instance.IsBiomeUnlocked(body.flightGlobalsIndex, atts[i].name) ? i + 1 : 0);
				}
			}

			return seal(h);
		}

		private int coverageChecksum()
		{
			if (data == null || data.Coverage == null)
			{
				return 0;
			}

			Int16[,] cov = data.Coverage;
			int sum = 0;

			for (int x = 0; x < 360; x++)
			{
				for (int y = 0; y < 180; y++)
				{
					sum = unchecked(sum + cov[x, y]);
				}
			}

			return sum;
		}

		// Ensure the mode's R-float data texture exists (cleared to 0). Rows are then staged by
		// stageDataRow as the prep fills the cache and uploaded once per frame (buildSlice).
		// A new Texture2D's contents are undefined, so the fresh texture is zeroed through its own raw
		// CPU buffer - GetRawTextureData<float> is a view over storage Unity has already allocated, not
		// a copy. The full-size Color[] mirror this replaced cost 16 bytes a pixel, 16 MB standing per
		// 1440x720 map, to clear each texture once.
		// point: no filtering - the biome index is a label, and a geographic cache is read at arbitrary
		// points under a non-rectangular projection, where a bilinear blend of two labels is a third biome.
		private void ensureDataTex(ref Texture2D tex, bool point = false)
		{
			if (tex != null && tex.width == mapwidth && tex.height == mapheight) return;
			if (tex != null) UnityEngine.Object.Destroy(tex);
			tex = new Texture2D(mapwidth, mapheight, TextureFormat.RFloat, false);
			tex.wrapMode = TextureWrapMode.Clamp;
			tex.filterMode = point ? FilterMode.Point : FilterMode.Bilinear;
			// TextureFormat.RFloat is one float per pixel, so this view is exactly mapwidth * mapheight
			// long. global:: because this namespace is SCANsat.SCAN_Map, where a bare "Unity" binds to
			// SCANsat.Unity rather than to UnityEngine's Unity.Collections.
			global::Unity.Collections.NativeArray<float> raw = tex.GetRawTextureData<float>();
			for (int i = 0; i < raw.Length; i++)
				raw[i] = 0f;
			tex.Apply(false);
		}

		// Stage one geographic row (src column y=row) into the data texture's CPU mirror. No Apply
		// here: Apply re-uploads the whole texture, so the caller does it once per frame.
		private void stageDataRow(Texture2D tex, float[,] src, int row)
		{
			if (tex == null || src == null || row < 0 || row >= mapheight) return;
			if (gpuRowBuf == null || gpuRowBuf.Length != mapwidth)
				gpuRowBuf = new Color[mapwidth];
			for (int x = 0; x < mapwidth; x++)
				gpuRowBuf[x] = new Color(src[x, row], 0f, 0f, 0f);
			tex.SetPixels(0, row, mapwidth, 1, gpuRowBuf);
		}

		// Build resourceCache (stock abundance). Nothing else builds it - a pass's first composite calls
		// this, whatever the mode - so this is where the cost lives. Sampling is the most expensive thing
		// left in a pass: the grid takes (2H/N)x(H/N) calls into stock GetAbundance, each carrying a
		// biome-map lookup, so at the settings' ceiling (map height 1024, interpolation 2) it is 524,288
		// of them - about a second, in one frame, with no way to spend it under the build budget. Hence
		// the config key: the grid outlives the reset that used to wipe it, and only a different body /
		// resource / window / grid actually re-samples. key is this pass's resourceConfigHash.
		private void buildResourceCache(int key)
		{
			bool sized = resourceCache != null && resourceCache.GetLength(0) == resourceMapWidth && resourceCache.GetLength(1) == resourceMapHeight;

			if (sized && resourceGridKey.Valid(key))
			{
				// The same field, already sampled. The auto-range fit is re-run because the windows' own UI
				// (setCustomRange) may have overwritten the range since; interpolation never rewrites the
				// sampled cells that fit reads, so a reused grid fits to exactly what a fresh one would.
				if (profile.AutoRange)
					applyAutoResourceRange();

				return;
			}

			// The one owner of this array's size: generateResourceCache writes into it and never
			// allocates, and nothing else in the class does now - a map with no resource layer keeps none.
			if (!sized)
			{
				resourceCache = new float[resourceMapWidth, resourceMapHeight];
			}
			else
			{
				// A grid whose width or height is not a whole number of steps has cells that neither the
				// sampling nor the interpolation passes write; they must not keep the old config's values.
				System.Array.Clear(resourceCache, 0, resourceCache.Length);
			}

			resourceGridKey.Invalidate();   // a throw mid-build must not leave a half-sampled grid stamped valid

			SCANuiUtil.generateResourceCache(ref resourceCache, resourceMapHeight, resourceMapWidth, resourceInterpolation, resourceMapScale, this, !profile.GeographicCache);
			if (profile.AutoRange)
				applyAutoResourceRange();   // from the sampled cells, before interpolation fills the gaps
			System.Random rr = new System.Random(ResourceScenario.Instance.gameSettings.Seed);
			for (int i = resourceInterpolation / 2; i >= 1; i /= 2)
			{
				SCANuiUtil.interpolate(resourceCache, resourceMapHeight, resourceMapWidth, i, i, i, rr, randomEdges, profile.ResourceHardEdges);
				SCANuiUtil.interpolate(resourceCache, resourceMapHeight, resourceMapWidth, 0, i, i, rr, randomEdges, profile.ResourceHardEdges);
				SCANuiUtil.interpolate(resourceCache, resourceMapHeight, resourceMapWidth, i, 0, i, rr, randomEdges, profile.ResourceHardEdges);
			}

			resourceGridKey.Stamp(key);
		}

		// Upload resourceCache (geographic resW x resH) as an R-float abundance texture (fraction 0..1).
		private void uploadResourceTexture()
		{
			if (resourceCache == null) return;
			if (resourceTex == null || resourceTex.width != resourceMapWidth || resourceTex.height != resourceMapHeight)
			{
				if (resourceTex != null) UnityEngine.Object.Destroy(resourceTex);
				resourceTex = new Texture2D(resourceMapWidth, resourceMapHeight, TextureFormat.RFloat, false);
				resourceTex.wrapMode = TextureWrapMode.Clamp;
				resourceTexKey.Invalidate();
			}
			else if (resourceTexKey.Valid(resourceGridKey.Key))
			{
				return;   // this texture already holds this grid
			}

			// Written through the raw texture data rather than SetPixels: RFloat is one float per pixel in
			// the same bottom-up row order, so the Color[] mirror SetPixels wants was 16 bytes a pixel of
			// pure garbage - 33 MB on the large object heap per upload at the 2048x1024 ceiling, thrown
			// away again on every reset. Same pattern as ensureDataTex. global:: because a bare "Unity"
			// binds to SCANsat.Unity in this namespace.
			global::Unity.Collections.NativeArray<float> raw = resourceTex.GetRawTextureData<float>();
			for (int y = 0; y < resourceMapHeight; y++)
			{
				int row = y * resourceMapWidth;
				for (int x = 0; x < resourceMapWidth; x++)
					raw[row + x] = resourceCache[x, y] / 100f;
			}
			resourceTex.Apply(false);
			// Keyed by the grid, not by the pass: a grid that failed to finish sampling holds key 0, and
			// stamping that here leaves the texture just as invalid as the grid behind it.
			resourceTexKey.Stamp(resourceGridKey.Key);
		}

		// Bake heightToColor across [min, min+range] into a 1-D LUT so the shader is a plain fetch and
		// the map matches the legend (which calls the same heightToColor) by construction.
		// 1-D LUT of the body's stock biome mapColors, indexed by the biome fraction. Cached per body.
		private void buildBiomeLUT()
		{
			if (body.BiomeMap == null) return;
			int n = body.BiomeMap.Attributes.Length;
			// The body index rather than the body itself: these maps are static and outlive a scene, so
			// holding the CelestialBody here kept the last browsed body's object alive for the session.
			int key = seal(mix(mix(0, body.flightGlobalsIndex), n));
			if (biomeLUT != null && biomeLUTKey.Valid(key)) return;
			if (biomeLUT != null) UnityEngine.Object.Destroy(biomeLUT);
			int w = Mathf.Max(n, 1);
			biomeLUT = new Texture2D(w, 1, TextureFormat.RGBA32, false);
			biomeLUT.filterMode = FilterMode.Point;
			biomeLUT.wrapMode = TextureWrapMode.Clamp;
			Color[] c = new Color[w];
			for (int i = 0; i < n; i++) c[i] = SCANUtil.getBiomeDisplayColor(body, i);   // one colour source for map, legend and tooltips (grayscale-encoded biome maps get a generated palette)
			biomeLUT.SetPixels(c);
			biomeLUT.Apply(false);
			biomeLUTCount = n;
			biomeLUTKey.Stamp(key);
		}

		private void buildPaletteLUT(float min, float range)
		{
			SCANterrainConfig tc = SCANUtil.getTerrainConfig(data);
			// Folded, not XOR-combined as this one used to be: XOR is order-free and the two flags below
			// sit in the same low bits, so they could cancel against the palette's hash and hand back a
			// LUT baked for the other colour mode.
			int key = mix(0, tc.ColorPal.Hash);
			key = mix(key, colorMap ? 1 : 0);
			key = mix(key, min.GetHashCode());
			key = mix(key, range.GetHashCode());
			key = seal(mix(key, useCustomRange ? 1 : 0));
			if (paletteLUT != null && paletteLUTKey.Valid(key))
				return;
			if (paletteLUT == null)
			{
				paletteLUT = new Texture2D(1024, 1, TextureFormat.RGBA32, false);
				paletteLUT.wrapMode = TextureWrapMode.Clamp;
			}
			if (paletteGreyLUT == null)
			{
				paletteGreyLUT = new Texture2D(1024, 1, TextureFormat.RGBA32, false);
				paletteGreyLUT.wrapMode = TextureWrapMode.Clamp;
			}
			Color[] lut = new Color[1024];
			Color[] grey = new Color[1024];
			for (int x = 0; x < 1024; x++)
			{
				float val = min + (x / 1023f) * range;
				lut[x] = useCustomRange
					? (Color)palette.heightToColor(val, colorMap, tc, customMin, customMax, customRange, true)
					: (Color)palette.heightToColor(val, colorMap, tc);
				grey[x] = useCustomRange
					? (Color)palette.heightToColor(val, false, tc, customMin, customMax, customRange, true)
					: (Color)palette.heightToColor(val, false, tc);
			}
			paletteLUT.SetPixels(lut);
			paletteLUT.Apply(false);
			paletteGreyLUT.SetPixels(grey);
			paletteGreyLUT.Apply(false);
			paletteLUTKey.Stamp(key);
		}

		private void updateCoverageFlags()
		{
			bool created = false;
			if (coverageFlags == null)
			{
				coverageFlags = new Texture2D(360, 180, TextureFormat.RGBA32, false);
				coverageFlags.filterMode = FilterMode.Point;
				coverageFlags.wrapMode = TextureWrapMode.Clamp;
				created = true;
			}

			// Coverage only changes between render passes (resetMap sets coverageFlagsDirty), never
			// within the per-row sweep - so rebuild + GPU-upload once per pass instead of on every
			// composite. The CPU buffer is a reused member, so the sweep allocates nothing.
			if (!created && !coverageFlagsDirty)
				return;

			if (coverageFlagsBuf == null)
				coverageFlagsBuf = new Color32[360 * 180];

			Int16[,] cov = data.Coverage;

			for (int x = 0; x < 360; x++)
			{
				for (int y = 0; y < 180; y++)
				{
					// Pack the raw 16-bit coverage: R = low byte, G = high byte. The shader decodes
					// cov = round(R*255) + 256*round(G*255) and tests any SCANtype bit (covHas). This
					// exposes every bit (Altimetry 0/1, VisualLoRes 2, Biome 3, VisualHiRes 6,
					// Resource 7/8) with one texture, vs the old 4-bit RGBA pack.
					int c = cov[x, y];
					coverageFlagsBuf[y * 360 + x] = new Color32((byte)(c & 0xFF), (byte)((c >> 8) & 0xFF), 0, 255);
				}
			}

			coverageFlags.SetPixels32(coverageFlagsBuf);
			coverageFlags.Apply(false);
			coverageFlagsDirty = false;
		}

		// Per-frame CPU budget for building a GPU data pass: the setting is the budget, in milliseconds.
		// It replaced the one-row-per-call cadence for the GPU data modes; the CPU renderer still goes by
		// rows per call. The slider offers 2/4/8/16; the clamp is for a hand-edited settings file.
		private static double gpuBuildBudgetMs()
		{
			return Mathf.Clamp(SCAN_Settings_Config.Instance.MapGenerationBudgetMs, 1, 50);
		}

		/// <summary>
		/// A body change on a geographic map (the big map): park the grid we are leaving and take back
		/// the one we are going to, if it is still parked and still the right size. Everything parked is
		/// at the current width, so the byte budget reduces to a count - and at a width where a single
		/// grid does not fit, that count is zero and this degrades to the old clear-and-resample.
		/// </summary>
		private void swapParkedHeightmap(int outgoing, int incoming)
		{
			if (big_heightmap != null && outgoing >= 0)
			{
				parkedHeightmaps[outgoing] = big_heightmap;
				parkedOrder.Remove(outgoing);
				parkedOrder.Add(outgoing);
			}

			big_heightmap = null;   // ensureModeCaches allocates on the reset that follows setBody

			// The size check is the guard, not the key: a grid parked before a resize is dead weight.
			if (parkedHeightmaps.TryGetValue(incoming, out float[,] grid)
				&& grid.GetLength(0) == mapwidth && grid.GetLength(1) == mapheight)
			{
				big_heightmap = grid;
				parkedHeightmaps.Remove(incoming);
				parkedOrder.Remove(incoming);
			}

			long gridBytes = (long)mapwidth * mapheight * sizeof(float);
			int maxParked = gridBytes > 0 ? (int)(ParkedHeightmapBudget / gridBytes) : 0;

			while (parkedOrder.Count > maxParked)
			{
				parkedHeightmaps.Remove(parkedOrder[0]);
				parkedOrder.RemoveAt(0);
			}
		}

		private void clearBiomeRowCache()
		{
			if (biomeRowCached != null)
				System.Array.Clear(biomeRowCached, 0, biomeRowCached.Length);
		}

		// A window map's elevation and biome caches (no GeographicCache) are in pixel space, valid for
		// one window: body, size, projection, centre and zoom. Any of those changing drops them.
		private void clearWindowCaches()
		{
			if (big_heightmap != null)
				System.Array.Clear(big_heightmap, 0, big_heightmap.Length);
			clearBiomeRowCache();
			dataTexKey.Invalidate();
		}

		// One row of the auto-range pre-pass: every 4th pixel of row rangeRow, sampled at its unprojected
		// coordinate into the cache (the build skips those pixels later), min/max accumulated. Same
		// stride and coordinates as the windows' old calcTerrainLimits, coverage ignored as there.
		private void preSampleRangeRow()
		{
			if (rangeRow < 0)
			{
				rangeRow = 0;
				rangeMin = float.MaxValue;
				rangeMax = float.MinValue;
				rangeSamples = 0;
			}

			int row = rangeRow;

			if (row < mapheight && row >= startLine && row <= stopLine && big_heightmap != null)
			{
				for (int i = 0; i < mapwidth; i += 4)
				{
					double lat = (row * 1.0f / mapscale) - 90f + lat_offset;
					double lon = (i * 1.0f / mapscale) - 180f + lon_offset;
					double la = lat, lo = lon;
					lat = unprojectLatitude(lo, la);
					lon = unprojectLongitude(lo, la);

					if (double.IsNaN(lat) || double.IsNaN(lon) || lat < -90 || lat > 90 || lon < -180 || lon > 180)
					{
						continue;
					}

					float v = big_heightmap[i, row];

					if (v == 0f)
					{
						terrainHeightToArray(lon, lat, i, row);
						v = big_heightmap[i, row];
					}

					if (v == -0.001f)
					{
						v = 0f;   // the cache's stand-in for a true 0 m
					}

					if (v < rangeMin) rangeMin = v;
					if (v > rangeMax) rangeMax = v;
					rangeSamples++;
				}
			}

			rangeRow += 4;
		}

		// calcTerrainLimits' guards, then the range the palette LUT and the legend use.
		private void applyAutoTerrainRange()
		{
			if (rangeSamples == 0)
			{
				return;
			}

			float min = rangeMin, max = rangeMax;

			if (min >= max)
			{
				min = max - 1f;
			}

			useCustomRange = true;
			customMin = min;
			customMax = max;
			customRange = max - min;
		}

		// The resource range over the window from the cells generateResourceCache just sampled, with
		// calcTerrainLimits' guards (never outside the body's configured range, never empty).
		private void applyAutoResourceRange()
		{
			if (resource == null || resource.CurrentBody == null || resourceCache == null)
			{
				return;
			}

			int step = Math.Max(1, resourceInterpolation);
			float min = 100f, max = 0f;
			bool any = false;

			for (int j = 0; j < resourceMapHeight; j += step)
			{
				for (int i = 0; i < resourceMapWidth; i += step)
				{
					float v = resourceCache[i, j];
					if (v < min) min = v;
					if (v > max) max = v;
					any = true;
				}
			}

			if (!any)
			{
				min = 0f;
				max = 100f;
			}

			if (min < resource.CurrentBody.MinValue) min = resource.CurrentBody.MinValue;
			if (min >= max) max = min + 1f;
			if (min < 0f) min = 0f;
			if (max > resource.CurrentBody.MaxValue) max = resource.CurrentBody.MaxValue;
			if (min >= max) min = max - 1f;

			useCustomRange = true;
			customResourceMin = min;
			customResourceMax = max;
		}

		// The biomes present in this map's window, read from the biome index cache once rows are
		// built (the zoom map's legend lists them). Null until any row is cached.
		internal List<CBAttributeMapSO.MapAttribute> BiomesInView()
		{
			if (body == null || body.BiomeMap == null || biome_indexmap == null || biomeRowCached == null)
			{
				return null;
			}

			CBAttributeMapSO.MapAttribute[] atts = body.BiomeMap.Attributes;
			CBAttributeMapSO.MapAttribute[] pal = SCANUtil.GetOrBuildPalette(body);
			bool[] seen = new bool[atts.Length];
			List<CBAttributeMapSO.MapAttribute> list = new List<CBAttributeMapSO.MapAttribute>();
			bool anyRow = false;

			for (int y = 0; y < mapheight && y < biomeRowCached.Length; y++)
			{
				if (!biomeRowCached[y])
				{
					continue;
				}

				anyRow = true;

				for (int x = 0; x < mapwidth; x++)
				{
					int idx = Mathf.RoundToInt(biome_indexmap[x, y] * atts.Length);

					if (idx < 0 || idx >= atts.Length || seen[idx])
					{
						continue;
					}

					seen[idx] = true;
					list.Add(pal != null && idx < pal.Length && pal[idx] != null ? pal[idx] : atts[idx]);
				}
			}

			return anyRow ? list : null;
		}

		// Composite this map once more at w x h into a fresh RenderTexture (the caller releases it). The
		// planet overlay renders its geographic data at the overlay's own size this way. Every other
		// uniform is the last composite's; the next composite resets them all.
		internal RenderTexture renderAt(int w, int h)
		{
			if (!gpuRendered || compositeMaterial == null || mapwidth <= 0)
			{
				return null;
			}

			RenderTexture rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
			rt.wrapMode = TextureWrapMode.Clamp;
			rt.Create();

			setFramingUniforms(w, h, mapscale * w / mapwidth);

			Graphics.Blit(null, rt, compositeMaterial);
			return rt;
		}

		// Re-frame the last composite's uniforms for a one-off render at w x h: the same geographic
		// window, more pixels. Everything the shader expresses in map pixels has to move together -
		// the size, the pixels-per-degree, and the row window, which is what the on-screen composite
		// set from startLine/stopLine and would otherwise clip the render to the on-screen height.
		// A one-off render is never mid-sweep, so the reveal is complete.
		private void setFramingUniforms(int w, int h, double scale)
		{
			compositeMaterial.SetFloat("_MapWidth", w);
			compositeMaterial.SetFloat("_MapHeight", h);
			compositeMaterial.SetFloat("_MapScale", (float)scale);
			compositeMaterial.SetFloat("_RowMin", 0f);
			compositeMaterial.SetFloat("_RowMax", h - 1);
			compositeMaterial.SetFloat("_SweepY", 1f);
		}

		// Bodies the CPU renderers drew as black-white static: no PQS for Altimetry/Slope, no biome
		// map for Biome. The shader does the same (_NoData); there is nothing to build for them.
		private bool gpuNoData()
		{
			if (mType == mapType.Biome)
				return !biomeMap;
			return (mType == mapType.Altimetry || mType == mapType.Slope) && !pqs;
		}

		// One CPU budget per frame shared by every map that is building (the big map, the zoom map and
		// the small map can all be open at once): each takes what is left, and always at least one
		// step, so all of them progress and together they cost what the setting says.
		private static int budgetFrame = -1;
		private static long budgetUsedTicks;

		// The same budget for builders outside this class (SCANcontroller.pumpHeightMap): claim what is
		// left of this frame's allowance, run at least one step however long that takes, then report
		// what was spent so the next builder this frame sees it.
		internal static long claimBuildBudget()
		{
			if (budgetFrame != Time.frameCount)
			{
				budgetFrame = Time.frameCount;
				budgetUsedTicks = 0;
			}

			return (long)(gpuBuildBudgetMs() * System.Diagnostics.Stopwatch.Frequency / 1000.0) - budgetUsedTicks;
		}

		internal static void reportBuildBudgetUsed(long ticks)
		{
			budgetUsedTicks += ticks;
		}

		// The CPU sampling for one map row: elevation into big_heightmap where the cache has none, and in
		// Biome mode the biome index into biomeIndex. buildSlice stages the results into the data
		// textures; the shader does all the colourising, and reads its own neighbouring texels for the
		// slope and the biome borders, so no row needs another row sampled first.
		private void prepRow(int row)
		{
			// RPM reserved rows: the shader draws them clear, so nothing is sampled for them.
			if (row < startLine || row > stopLine)
			{
				return;
			}

			// Biome rows are cached in biome_indexmap (per body and size, and per window for a pixel-space
			// map, like the height cache): a row already sampled skips the per-pixel biome lookups entirely.
			bool wantBiome = mType == mapType.Biome && biomeMap && biomeRowCached != null && row < biomeRowCached.Length && !biomeRowCached[row];
			// The planet overlay's elevation comes only from the body's prebuilt height grid (the terrain
			// overlay never samples PQS on its own account).
			bool wantElev = mType != mapType.Visual && (mType != mapType.Biome || profile.BiomeUnderlay)
				&& (!profile.PlanetUV || heightGridPass) && pqs && big_heightmap != null;

			if (!wantBiome && !wantElev)
			{
				return;
			}

			double rawLat = (row * 1.0f / mapscale) - 90f + lat_offset;

			for (int i = 0; i < mapwidth; i++)
			{
				// Column i's longitude on the map's raw grid. (A planet overlay's pixels run the planet's
				// way, but its caches are geographic: the shader maps each pixel back to the true longitude
				// and reads them there, so nothing here needs the planet's layout.)
				double rawLon = (i * 1.0f / mapscale) - 180f + lon_offset;

				if (wantElev && big_heightmap[i, row] == 0f)
				{
					// The big map's cache is geographic: its raw grid is the globe, so the raw coords are the
					// sample coords and the shader reads it through the unprojected lon/lat. A window map's
					// cache is pixel space: sample at this pixel's unprojected coordinate, which is where the
					// shader (pixel uv) expects it.
					double sampleLon = rawLon, sampleLat = rawLat;
					bool onMap = true;

					if (!profile.GeographicCache)
					{
						sampleLat = unprojectLatitude(rawLon, rawLat);
						sampleLon = unprojectLongitude(rawLon, rawLat);
						onMap = !(double.IsNaN(sampleLat) || double.IsNaN(sampleLon) || sampleLat < -90 || sampleLat > 90 || sampleLon < -180 || sampleLon > 180);
					}

					if (onMap)
					{
						// The prebuilt grid has every cell, so fill them all: the shader stencils by coverage
						// anyway, and a map that is upsampled from this texture (the terrain overlay, 4 output
						// pixels per texel) would otherwise blend real heights with the zeros of uncovered
						// neighbours into dark fringes along every coverage edge. PQS is sampled for covered
						// pixels only, as ever.
						if (heightGridPass)
							gridHeightToArray(i, row);   // one pixel per degree: the body's prebuilt height map, no PQS
						else if (SCANUtil.isCovered(sampleLon, sampleLat, data, SCANtype.Altimetry))
							terrainHeightToArray(sampleLon, sampleLat, i, row);
					}
				}

				if (!wantBiome)
				{
					continue;
				}

				// The biome cache has the elevation cache's layout: the globe's own grid for a geographic
				// cache (the raw coords are the sample coords, as for elevation above), else this pixel's
				// unprojected coordinate, where the shader reads it by pixel uv.
				double lat = rawLat, lon = rawLon;

				if (!profile.GeographicCache)
				{
					lat = unprojectLatitude(rawLon, rawLat);
					lon = unprojectLongitude(rawLon, rawLat);

					if (double.IsNaN(lat) || double.IsNaN(lon) || lat < -90 || lat > 90 || lon < -180 || lon > 180)
					{
						biomeIndex[i] = 0;
						continue;
					}
				}

				// One biome lookup per pixel: the shader colourises from _BiomeLUT[biomeIndex] (stock
				// colours or the low/high gradient) and finds the borders in the index texture itself.
				passBiomeSamples++;
				biomeIndex[i] = SCANUtil.getBiomeIndexFraction(body, lon, lat);
			}
		}

		// One frame's slice of a data pass (Altimetry / Slope / Biome): rows sampled under the per-frame
		// CPU budget shared by every building map, staged into the data textures, each texture uploaded
		// once. Only called with rows left to build. Returns false while the auto-range pre-pass is
		// still running, when there is nothing to show yet. The resource cache is not built here: the
		// first composite builds it lazily (setModeUniforms -> buildResourceCache), as for Visual.
		private bool buildSlice()
		{
			long start = System.Diagnostics.Stopwatch.GetTimestamp();
			long budget = claimBuildBudget();
			bool elevDirty = false, biomeDirty = false;

			if (biomeRowCached == null || biomeRowCached.Length != mapheight)
				biomeRowCached = new bool[mapheight];

			if (passBuildStart < 0f)
				passBuildStart = Time.realtimeSinceStartup;
			passBuildFrames++;

			// Auto range (zoom map, RPM): before any row is built or revealed, sample every 4th pixel of
			// the window into the cache and fit the palette range to what is there - what the windows'
			// calcTerrainLimits did synchronously before each reset, now under the budget. Rows start only
			// once the range is final, so the first revealed row already has the right colours.
			if (profile.AutoRange && pqs && rangeRow < mapheight && (mType == mapType.Altimetry || mType == mapType.Biome))
			{
				while (rangeRow < mapheight)
				{
					preSampleRangeRow();

					if (rangeRow >= mapheight)
					{
						applyAutoTerrainRange();
						break;
					}

					if (System.Diagnostics.Stopwatch.GetTimestamp() - start >= budget)
						break;
				}

				if (rangeRow < mapheight)
				{
					reportBuildBudgetUsed(System.Diagnostics.Stopwatch.GetTimestamp() - start);
					return false;
				}
			}

			// At least one row per frame, however long it takes, then as many as fit the budget. Row
			// mapstep is sampled and staged in the same step; the shader reads neighbouring rows itself.
			while (mapstep < mapheight)
			{
				prepRow(mapstep);

				// Biome: biomeIndex is this row's fresh lookup, or biome_indexmap already holds it.
				if (mType == mapType.Biome && biome_indexmap != null)
				{
					if (!biomeRowCached[mapstep])
					{
						for (int bi = 0; bi < mapwidth; bi++)
							biome_indexmap[bi, mapstep] = (float)biomeIndex[bi];
						biomeRowCached[mapstep] = true;
					}
					ensureDataTex(ref biomeIndexTex, true);
					stageDataRow(biomeIndexTex, biome_indexmap, mapstep);
					biomeDirty = true;
				}
				// Only a mode that samples elevation gets an elevation texture. A Biome map with no
				// underlay (the small map, the biome planet overlay) staged a full row of zeros per row
				// into an RFloat texture the shader never reads (_HasElevation 0) - 2 MB and a full
				// Apply per build frame for the 1024x512 biome overlay.
				if (big_heightmap != null)
				{
					ensureDataTex(ref elevationTex);
					stageDataRow(elevationTex, big_heightmap, mapstep);
					elevDirty = true;
				}

				mapstep++;
				if (mapstep >= mapheight)
					break;
				if (System.Diagnostics.Stopwatch.GetTimestamp() - start >= budget)
					break;
			}

			if (elevDirty) elevationTex.Apply(false);
			if (biomeDirty) biomeIndexTex.Apply(false);

			reportBuildBudgetUsed(System.Diagnostics.Stopwatch.GetTimestamp() - start);

			if (mapstep >= mapheight)
			{
				dataTexKey.Stamp(pendingDataKey);   // fully sampled, for the coverage this build started from (see resetMap)
				passBuildMs = (Time.realtimeSinceStartup - passBuildStart) * 1000f;
			}

			return true;
		}

		/* MAP: build: one pump call per frame while !isMapComplete. Every pass is the same sequence,
		   whatever the mode or the source: make sure the render target exists, build a slice of the
		   data under the shared CPU budget (nothing at all for Visual, a recolour or a body with no
		   data - resetMap left those with no rows to build), then composite once with the reveal at
		   the smaller of the timed sweep and the rows built. */
		internal void getPartialMap()
		{
			if (data == null)
			{
				return;
			}

			if (!willRenderGPU(mType))
			{
				// Nothing renders this map: the composite shader is missing or unsupported on this graphics
				// device (SCAN_UI_Loader logged which), or Visual maps are disabled in the settings. There is
				// no CPU renderer any more, so the pass is over, said once per pass, and DisplayTexture
				// stays null.
				if (!passComplete)
				{
					passComplete = true;
					SCANUtil.SCANlog("[{0}] {1} map not rendered: composite shader unavailable{2}", body.bodyName, mType,
						mType == mapType.Visual && !SCAN_Settings_Config.Instance.VisibleMapsActive ? " (or Visual maps disabled)" : "");
				}

				return;
			}

			ensureRenderTex();

			if (mapstep < mapheight && !buildSlice())
			{
				return;   // the auto-range pre-pass is still running: nothing to show yet, the target keeps the previous pass
			}

			if (!profile.Sweep && mapstep < mapheight)
			{
				return;   // no reveal (the planet overlay): nothing is shown mid-build, one composite when it is done
			}

			composite();

			if (!passComplete)
			{
				return;
			}

			if (gpuRecolorSweep)
			{
				gpuRecolorSweep = false;
				SCANUtil.SCANlog("[{0}] {1} GPU recolour pass {2}x{3}: no re-sample, sweep {4:F2} s", body.bodyName, mType, mapwidth, mapheight, Time.realtimeSinceStartup - sweepStart);
			}
			else if (passBuildStart >= 0f)
			{
				SCANUtil.SCANlog("[{0}] {1} GPU pass {2}x{3}: build {4} frames / {5:F0} ms ({6} height samples, {7} biome lookups), pass total {8:F2} s",
					body.bodyName, mType, mapwidth, mapheight, passBuildFrames, passBuildMs, passHeightSamples, passBiomeSamples, Time.realtimeSinceStartup - passBuildStart);
				passBuildStart = -1f;   // one line per pass
			}
		}

		/* Calculates the terrain elevation based on scanning coverage; fetches data from elevation cache if possible */
		private float terrainElevation(double Lon, double Lat, int w, int h, float[,] heightMap, bool c, SCANdata Data, out bool NowColor, bool exporting = false)
		{
			float elevation = 0f;
			NowColor = colorMap;
			if (SCANUtil.isCovered(Lon, Lat, Data, SCANtype.AltimetryHiRes))
			{
				if (c)
				{
					double lon = fixUnscale(unScaleLongitude(Lon), w);
					double lat = fixUnscale(unScaleLatitude(Lat), h);

					int ilon = Mathf.RoundToInt((float)lon);
					int ilat = Mathf.RoundToInt((float)lat);

					if (ilon >= w)
					{
						ilon = w - 1;
					}

					if (ilat >= h)
					{
						ilat = h - 1;
					}

					elevation = heightMap[ilon, ilat];
					if (elevation == 0f && !exporting)
					{
						elevation = (float)SCANUtil.getElevation(body, Lon, Lat);
					}
				}
				else
				{
					elevation = (float)SCANUtil.getElevation(body, Lon, Lat);
				}
			}
			else
			{
				if (c)
				{
					double lon = fixUnscale(unScaleLongitude(Lon), w);
					double lat = fixUnscale(unScaleLatitude(Lat), h);

					int ilon = ((int)(lon * 5)) / 5;
					int ilat = ((int)(lat * 5)) / 5;

					if (ilon >= w)
					{
						ilon = w - 1;
					}

					if (ilat >= h)
					{
						ilat = h - 1;
					}

					elevation = heightMap[ilon, ilat];
					if (elevation == 0f && !exporting)
					{
						elevation = (float)SCANUtil.getElevation(body, ((int)(Lon * 5)) / 5, ((int)(Lat * 5)) / 5);
					}
				}
				else
				{
					elevation = (float)SCANUtil.getElevation(body, ((int)(Lon * 5)) / 5, ((int)(Lat * 5)) / 5);
				}

				NowColor = false;
			}

			return elevation;
		}

		public float terrainElevation(double Lon, double Lat, int W, int H, float[,] heightMap, SCANdata Data, bool export = false)
		{
			bool c = true;

			return terrainElevation(Lon, Lat, W, H, heightMap, true, Data, out c, export);
		}


		#endregion

	}
}
