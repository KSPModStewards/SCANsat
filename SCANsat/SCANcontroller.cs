#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANcontroller - scenario module that handles all scanning
 * 
 * Copyright (c)2013 damny;
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using Contracts;
using FinePrint.Contracts;
using FinePrint.Contracts.Parameters;
using KSPTextureLoader;
using SCANsat.SCAN_Data;
using SCANsat.SCAN_Map;
using SCANsat.SCAN_Platform.Extensions.ConfigNodes;
using SCANsat.SCAN_Reflection;
using SCANsat.SCAN_Toolbar;
using SCANsat.SCAN_UI.UI_Framework;
using SCANsat.SCAN_Unity;
using SCANsat.Unity.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Log = KSPCommunityLib.Logging.Log;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANcolorUtil;

namespace SCANsat
{
	/// <summary>
	/// Per-body Visual-map texture sources. Reads the body's SCANSAT_BODY_TEXTURES node (if any) into
	/// resolved absolute paths and loads the colour/normal maps to the GPU one mip level at a time -
	/// the smallest mip that still covers the widest map asking for it - through KSPTextureLoader's
	/// owned-texture API. Nothing is decompressed on the CPU and nothing is kept in RAM.
	/// </summary>
	public class SCANtextures
	{
		public CelestialBody body;

		/* Resolved absolute paths from a SCANSAT_BODY_TEXTURES node; null when not declared */
		public string heightMapPath = null;
		public string normalMapPath = null;
		public string colorMapPath = null;

		/* GPU textures loaded from the cfg, one mip each */
		private SCANddsHeader colorHeader;
		private SCANddsHeader normalHeader;
		private Texture colorTex;    // the loader's Texture2D, or a RenderTexture holding a downscale of a mip-less file
		private Texture normalTex;
		private int colorMip = -1;
		private int normalMip = -1;
		private bool colorFailed;    // header or loader rejected the file: logged once, not retried
		private bool normalFailed;

		private static HashSet<string> configuredBodies;
		private static readonly HashSet<string> failedColorBodies = new HashSet<string>();

		public bool HasConfig
		{
			get { return colorMapPath != null || normalMapPath != null || heightMapPath != null; }
		}

		public Texture ColorTexture
		{
			get { return colorTex; }
		}

		public Texture NormalTexture
		{
			get { return normalTex; }
		}

		/// <summary>The declared colorMap will not load: the cfg cannot serve this body's Visual map.</summary>
		public bool ColorFailed
		{
			get { return colorFailed; }
		}

		/// <summary>Which normal-map channel carries Y for the shader: 1 = green (DXT5nm, BC5), 0 = blue.</summary>
		public int NormalYChannel
		{
			get { return normalHeader == null ? 0 : NormalYChannelFor((TextureFormat)(int)normalHeader.Format); }
		}

		public static int NormalYChannelFor(TextureFormat format)
		{
			switch (format)
			{
				case TextureFormat.DXT5:   // DXT5nm: X in alpha, Y in green
				case TextureFormat.BC5:    // X in red, Y in green
					return 1;
				default:
					return 0;              // uncompressed RGB normals: KSP keeps Y in blue
			}
		}

		/// <summary>True when a SCANSAT_BODY_TEXTURES node declares a colorMap for the body (cached from GameDatabase).</summary>
		/// Declarative only - whether the file actually loads is another matter, so the OnDemand paths
		/// ask UseConfigFor, not this.
		public static bool HasConfigFor(CelestialBody b)
		{
			if (b == null)
			{
				return false;
			}

			if (configuredBodies == null)
			{
				configuredBodies = new HashSet<string>();

				ConfigNode[] nodes = GameDatabase.Instance.GetConfigNodes("SCANSAT_BODY_TEXTURES");

				for (int i = 0; i < nodes.Length; i++)
				{
					string name = nodes[i].GetValue("name");

					if (!string.IsNullOrEmpty(name) && nodes[i].HasValue("colorMap"))
					{
						configuredBodies.Add(name);
					}
				}
			}

			return configuredBodies.Contains(b.name);
		}

		/// <summary>
		/// True while the cfg really is this body's Visual source: a node declares a colorMap and that
		/// file has not failed to load. Only then can Kopernicus OnDemand be left alone - once the
		/// declared colour map is unusable the body needs its own ScaledSpace textures like any other.
		/// Static, so it still answers after the body's SCANtextures handler has been released.
		/// </summary>
		public static bool UseConfigFor(CelestialBody b)
		{
			return HasConfigFor(b) && !failedColorBodies.Contains(b.name);
		}

		/// <summary>Records a failed colorMap; true the first time, so the caller logs once per session.</summary>
		public static bool MarkColorFailed(CelestialBody b)
		{
			return b != null && failedColorBodies.Add(b.name);
		}

		public SCANtextures(CelestialBody b)
		{
			body = b;

			ConfigNode[] visualOverrides = GameDatabase.Instance.GetConfigNodes("SCANSAT_BODY_TEXTURES");

			for (int i = 0; i < visualOverrides.Length; i++)
			{
				ConfigNode node = visualOverrides[i];
				if (node.HasValue("name"))
				{
					string bodyName = node.GetValue("name");
					if (bodyName == b.name)
					{
						GetTexturePathsFromConfig(node);
						break;
					}
				}
			}

			SCANUtil.SCANlog(TextureState());
		}

		public void GetTexturePathsFromConfig(ConfigNode node)
		{
			string bodyName = node.GetValue("name");
			if (bodyName == body.name)
			{
				// Resolution kept exactly as the cfg format was documented to planet packs: relative to the KSP root.
				string baseFolder = System.IO.Directory.GetParent(KSPUtil.ApplicationRootPath).FullName;

				heightMapPath = resolve(baseFolder, node.GetValue("heightMap"));
				normalMapPath = resolve(baseFolder, node.GetValue("normalMap"));
				colorMapPath = resolve(baseFolder, node.GetValue("colorMap"));
			}
		}

		private static string resolve(string baseFolder, string cfgPath)
		{
			if (string.IsNullOrEmpty(cfgPath))
			{
				return null;
			}

			return (baseFolder + '/' + cfgPath).Replace("\\", "/");
		}

		/// <summary>
		/// Load, or upgrade to, the mip that covers targetWidth pixels across 360 degrees of longitude.
		/// Only ever grows: a zoom map that zoomed in keeps the larger mip until the body is released.
		/// Blocks on the load - mip 3 of a 16K DXT5 map is 2 MiB, the base mip of one is 128 MiB.
		/// </summary>
		public bool EnsureLoaded(int targetWidth)
		{
			// A RenderTexture downscale loses its contents on a graphics device reset; rebuild it then.
			if (colorTex is RenderTexture crt && !crt.IsCreated())
			{
				destroyTexture(colorTex);
				colorTex = null;
				colorMip = -1;
			}

			if (normalTex is RenderTexture nrt && !nrt.IsCreated())
			{
				destroyTexture(normalTex);
				normalTex = null;
				normalMip = -1;
			}

			if (colorMapPath != null && !colorFailed)
			{
				loadMip(colorMapPath, "colorMap", false, targetWidth, ref colorHeader, ref colorTex, ref colorMip, ref colorFailed);
			}

			if (normalMapPath != null && !normalFailed)
			{
				loadMip(normalMapPath, "normalMap", true, targetWidth, ref normalHeader, ref normalTex, ref normalMip, ref normalFailed);
			}

			return colorTex != null;
		}

		/// <summary>Ceiling on a single resident source texture, compressed or not.</summary>
		private const long TextureBudget = 32L * 1048576;

		/// <summary>
		/// Size of the GPU downscale kept in place of the base level of a file with no mip chain: about
		/// two texels per map pixel, as the mip pick, never wider than the source and never over the same
		/// 32 MiB budget. False when the file has a mip chain, or when the downscale would not be clearly
		/// smaller than the base level and so is not worth the blit.
		/// </summary>
		private static bool downscaleSize(SCANddsHeader header, int targetWidth, out int w, out int h)
		{
			w = 0;
			h = 0;

			if (header.MipCount != 1)
			{
				return false;
			}

			w = Mathf.Min(header.Width, Mathf.NextPowerOfTwo(Mathf.Max(1, targetWidth) * 2));
			h = Mathf.Max(1, (int)((long)w * header.Height / header.Width));

			// ARGB32, so the budget bites sooner than on a compressed mip: 4096x2048 is exactly 32 MiB.
			while (w > 1 && (long)w * h * 4 > TextureBudget)
			{
				w >>= 1;
				h = Mathf.Max(1, (int)((long)w * header.Height / header.Width));
			}

			return (long)w * h * 4 * 2 < header.MipBytes(0);
		}

		private void loadMip(string path, string role, bool linear, int targetWidth, ref SCANddsHeader header, ref Texture tex, ref int loadedMip, ref bool failed)
		{
			if (header == null && !SCANddsHeader.TryRead(path, out header, out string error))
			{
				failed = true;
				Log.Error($"[{body.name}] {role} {path}: {error}");
				return;
			}

			// Aim for about two texels per map pixel - bilinear at 1:1 looks soft next to the old full-res
			// CPU sampling - unless that mip costs more than 32 MiB, then settle for 1:1 (the zoom map at
			// high magnification lands here, where the 1:1 mip is already the base level or close to it).
			int mip = header.MipForWidth(targetWidth * 2);

			if (header.MipBytes(mip) > TextureBudget && header.MipForWidth(targetWidth) > mip)
			{
				mip = header.MipForWidth(targetWidth);
			}

			// The width this call would end up holding: the mip's own, or - for a file with no mip
			// chain, where a downscale replaces the base level - the downscale's.
			bool downscale = downscaleSize(header, targetWidth, out int dsWidth, out int dsHeight);
			int wantWidth = downscale ? dsWidth : header.MipWidth(mip);

			// Only ever grow, and never shrink. The mip compare alone is not enough: a file with one
			// level always picks mip 0, so once loaded every later call - however much wider the map
			// asking - matched "mip >= loadedMip" and returned, and the GPU downscale stayed at whatever
			// the first caller wanted (small map at 360 px -> 1024x512, and then the big map at 1440 and
			// a 4096 export both composited from that).
			if (tex != null && mip >= loadedMip && wantWidth <= tex.width)
			{
				return;   // already holding this mip or a larger one, at this width or wider
			}

			// The mip chain is the file's own claim; nothing guarantees the bytes behind it were
			// written. Check the range against the file's length before asking the loader to read it.
			if (!header.TryMipRange(mip, out long mipOffset, out long mipBytes, out string rangeError))
			{
				failed = true;
				Log.Error($"[{body.name}] {role} {path}: {rangeError}");
				return;
			}

			float start = Time.realtimeSinceStartup;

			try
			{
				Texture2DConfig config = header.ConfigForMip(mip, linear);
				TextureLoadTask<Texture2D> task = TextureLoader.LoadOwnedTexture2D(config, path, mipOffset, mipBytes);
				Texture2D loaded = task.GetTexture();

				if (loaded == null)
				{
					throw new Exception("loader returned no texture");
				}

				loaded.name = $"SCANsat {body.name} {role} mip{mip}";
				loaded.wrapModeU = TextureWrapMode.Repeat;   // longitude wraps at the seam
				loaded.wrapModeV = TextureWrapMode.Clamp;
				loaded.filterMode = FilterMode.Bilinear;

				Texture result = loaded;
				string note = "";

				// No mip chain to choose from, so the only level is the full-size one (RSS ships most of
				// its PluginData maps that way: 16K, one level, 128 MiB). When a GPU downscale that covers
				// the request is clearly smaller, keep that instead and release the base level - the
				// transient cost is the same, the resident cost drops from 128 MiB to 2.
				if (downscale)
				{
					long rtBytes = (long)dsWidth * dsHeight * 4;

					RenderTexture rt = new RenderTexture(dsWidth, dsHeight, 0, RenderTextureFormat.ARGB32, linear ? RenderTextureReadWrite.Linear : RenderTextureReadWrite.sRGB);
					rt.name = $"SCANsat {body.name} {role} downscale";
					rt.wrapModeU = TextureWrapMode.Repeat;
					rt.wrapModeV = TextureWrapMode.Clamp;
					rt.filterMode = FilterMode.Bilinear;
					rt.useMipMap = false;
					rt.Create();
					Graphics.Blit(loaded, rt);
					UnityEngine.Object.Destroy(loaded);
					result = rt;
					note = $"; no mip chain, kept a {dsWidth}x{dsHeight} GPU downscale ({rtBytes / 1048576f:F1} MiB) and released the base level";
				}

				if (tex != null)
				{
					destroyTexture(tex);
				}

				tex = result;
				loadedMip = mip;

				SCANUtil.SCANlog("[{0}] {1}: mip {2}/{3} ({4}x{5} {6}, {7:F1} MiB at offset {8}) for target width {9}px in {10:F0} ms{11}",
					body.name, role, mip, header.MipCount, config.Width, config.Height, header.FormatName,
					mipBytes / 1048576f, mipOffset, targetWidth, (Time.realtimeSinceStartup - start) * 1000f, note);
			}
			catch (Exception e)
			{
				failed = true;
				Log.Error($"[{body.name}] {role} {path}: mip {mip} failed to load");
				Log.Exception(e);
			}
		}

		private static void destroyTexture(Texture t)
		{
			if (t is RenderTexture rt)
			{
				rt.Release();
			}

			UnityEngine.Object.Destroy(t);
		}

		public void Release()
		{
			if (colorTex != null)
			{
				destroyTexture(colorTex);
				colorTex = null;
			}

			if (normalTex != null)
			{
				destroyTexture(normalTex);
				normalTex = null;
			}

			colorMip = -1;
			normalMip = -1;
		}

		public string TextureState()
		{
			return $"{body.name} Visual textures: colorMap={(colorMapPath ?? "none")}, normalMap={(normalMapPath ?? "none")}, heightMap={(heightMapPath ?? "none")}";
		}
	}

	[KSPScenario(ScenarioCreationOptions.AddToAllGames | ScenarioCreationOptions.AddToExistingGames, GameScenes.FLIGHT, GameScenes.SPACECENTER, GameScenes.TRACKSTATION)]
	public class SCANcontroller : ScenarioModule
	{
		public static SCANcontroller controller
		{
			get { return instance; }
		}

		private static int minScanAlt = 5000;
		private static int maxScanAlt = 500000;
		private static int bestScanAlt = 250000;

		[KSPField(isPersistant = true)]
		public bool storageUpgraded = false;
		[KSPField(isPersistant = true)]
		public bool mainMapVisible = false;
		[KSPField(isPersistant = true)]
		public bool mainMapColor = true;
		[KSPField(isPersistant = true)]
		public bool mainMapTerminator = false;
		[KSPField(isPersistant = true)]
		public MainMapDisplayMode mainMapDisplayMode = MainMapDisplayMode.Terrain;
		[KSPField(isPersistant = true)]
		public bool mainMapMinimized = false;
		[KSPField(isPersistant = true)]
		public bool bigMapVisible = false;
		[KSPField(isPersistant = true)]
		public bool bigMapColor = true;
		[KSPField(isPersistant = true)]
		public bool bigMapTerminator = false;
		[KSPField(isPersistant = true)]
		public bool bigMapGrid = true;
		[KSPField(isPersistant = true)]
		public bool bigMapOrbit = true;
		[KSPField(isPersistant = true)]
		public bool bigMapWaypoint = true;
		[KSPField(isPersistant = true)]
		public bool bigMapAnomaly = true;
		[KSPField(isPersistant = true)]
		public bool bigMapFlag = true;
		[KSPField(isPersistant = true)]
		public bool bigMapLegend = true;
		[KSPField(isPersistant = true)]
		public bool bigMapResourceOn = false;
		[KSPField(isPersistant = true)]
		public string bigMapProjection = "Rectangular";
		[KSPField(isPersistant = true)]
		public string bigMapType = "Altimetry";
		[KSPField(isPersistant = true)]
		public string bigMapResource = "Ore";
		[KSPField(isPersistant = true)]
		public string bigMapBody = "Kerbin";
		[KSPField(isPersistant = true)]
		public bool zoomMapVesselLock = false;
		[KSPField(isPersistant = true)]
		public bool zoomMapColor = true;
		[KSPField(isPersistant = true)]
		public bool zoomMapTerminator = false;
		[KSPField(isPersistant = true)]
		public bool zoomMapOrbit = true;
		[KSPField(isPersistant = true)]
		public bool zoomMapIcons = true;
		[KSPField(isPersistant = true)]
		public bool zoomMapLegend = true;
		[KSPField(isPersistant = true)]
		public bool zoomMapResourceOn = false;
		[KSPField(isPersistant = true)]
		public string zoomMapType = "Altimetry";
		[KSPField(isPersistant = true)]
		public string zoomMapResource = "Ore";
		[KSPField(isPersistant = true)]
		public int zoomMapState = 0;
		[KSPField(isPersistant = true)]
		public int zoomMapRefresh = 0;
		[KSPField(isPersistant = true)]
		public bool zoomMapZoomPersist = false;
		[KSPField(isPersistant = true)]
		public float zoomMapZoom = 10f;
		[KSPField(isPersistant = true)]
		public int overlaySelection = 0;
		[KSPField(isPersistant = true)]
		public string overlayResource = "Ore";

		public Color32 lowBiomeColor32 = new Color(0, 0.46f, 0.02345098f, 1);
		public Color32 highBiomeColor32 = new Color(0.7f, 0.2388235f, 0, 1);
		public Color32 lowSlopeColorOne32 = new Color(0.004705883f, 0.6f, 0.3788235f, 1);
		public Color32 highSlopeColorOne32 = new Color(0.9764706f, 1, 0.4627451f, 1);
		public Color32 lowSlopeColorTwo32 = new Color(0.9764706f, 1, 0.4627451f, 1);
		public Color32 highSlopeColorTwo32 = new Color(0.94f, 0.2727843f, 0.007372549f, 1);

		/* Available resources for overlays; loaded from SCANsat configs; only loaded once */
		private static DictionaryValueList<string, SCANresourceGlobal> masterResourceNodes = new DictionaryValueList<string, SCANresourceGlobal>();

		/* Terrain height and color option containers loaded from SCANsat configs; only needs to be loaded once */
		private static Dictionary<string, SCANterrainConfig> masterTerrainNodes = new Dictionary<string, SCANterrainConfig>();

		/* List of resources currently loaded from resource addons */
		private static List<string> loadedResources = new List<string>();

		/* Primary SCANsat vessel dictionary; loaded every time */
		public DictionaryValueList<Guid, SCANvessel> knownVessels = new DictionaryValueList<Guid, SCANvessel>();

		/* Primary SCANdata dictionary; loaded every time */
		private DictionaryValueList<string, SCANdata> body_data = new DictionaryValueList<string, SCANdata>();

		/* MechJeb Landing Target Integration */
		private bool mechjebLoaded;
		private SCANwaypoint landingTarget;

		/* Kopernicus On Demand Loading Data */
		private List<CelestialBody> dataBodies = new List<CelestialBody>();
		private CelestialBody bigMapBodyPQS;
		private CelestialBody zoomMapBodyPQS;
		private CelestialBody bigMapBodyVisual;
		private CelestialBody zoomMapBodyVisual;

		/* Visual Map Texture Data */
		private Dictionary<CelestialBody, SCANtextures> mapTextureHandler = new Dictionary<CelestialBody, SCANtextures>();
		private CelestialBody bigMapBodyScaledSpace;
		private CelestialBody zoomMapBodyScaledSpace;
		private CelestialBody dataBodyScaledSpace;   // the small main map
		private CelestialBody rpmBodyScaledSpace;

		private SCAN_UI_MainMap _mainMap;
		private SCAN_UI_Instruments _instruments;
		private SCAN_UI_BigMap _bigMap;
		private SCAN_UI_ZoomMap _zoomMap;
		private SCAN_UI_Overlay _overlay;
		private SCAN_UI_Settings _settings;

		/* App launcher object */
		internal SCANappLauncher appLauncher;

		/* Used in case the loading process is interupted somehow */
		private bool loaded = false;

		/* Used to make sure all contracts are loaded */
		private bool contractsLoaded = false;

		/* Used as holder for vessel id's while loading */
		private List<Guid> tempIDs = new List<Guid>();

		/* Is the Breaking Ground Expansion installed */
		private bool serenityLoaded = false;

		private static SCANcontroller instance;

		#region Public Accessors

		public SCANdata getData(string bodyName)
		{
			if (body_data.Contains(bodyName))
			{
				return body_data[bodyName];
			}

			return null;
		}

		public SCANdata getData(int index)
		{
			if (body_data.Count > index)
			{
				return body_data.At(index);
			}
			else
			{
				SCANUtil.SCANdebugLog("SCANdata dictionary index out of range; something went wrong here...");
			}

			return null;
		}

		public List<SCANdata> GetAllData
		{
			get { return body_data.Values.ToList(); }
		}

		public int GetDataCount
		{
			get { return body_data.Count; }
		}

		/* Use this method to protect against duplicate dictionary keys */
		public void addToBodyData(CelestialBody b, SCANdata data)
		{
			if (!body_data.Contains(b.bodyName))
			{
				body_data.Add(b.bodyName, data);
			}
			else
			{
				Log.Error("Warning: SCANdata Dictionary Already Contains Key of This Type");
			}
		}

		// Returns the body's ORIGINAL ScaledSpace color/normal textures (already resident on the
		// GPU) with no readable CPU copy - for the GPU Visual-map compositor (SCANmap). Returns
		// false if the material/texture isn't ready (e.g. a 1x1 on-demand placeholder), so the
		// caller retries once loadOnDemandScaledSpace has run.
		internal bool getScaledSpaceSource(CelestialBody b, out Texture colorTex, out Texture normalTex, out Material material, out Texture gasRamp)
		{
			colorTex = null;
			normalTex = null;
			gasRamp = null;

			GetVisualMapTexturesForBody(b, out material, out string gasRampTextureName, out string colorMapTextureName, out string normalMapTextureName);

			if (material == null)
				return false;

			if (colorMapTextureName != null)
				colorTex = material.GetTexture(colorMapTextureName);

			if (normalMapTextureName != null)
				normalTex = material.GetTexture(normalMapTextureName);

			// Non-null only for a gas giant, whose "colour map" is a cloud pattern (see
			// GetVisualMapTexturesForBody): the gradient the composite looks that pattern up in.
			if (gasRampTextureName != null)
				gasRamp = material.GetTexture(gasRampTextureName);

			if (colorTex == null || colorTex.width <= 1 || colorTex.height <= 1)
				return false;

			return true;
		}

		/// <summary>
		/// The Visual map's GPU source textures for a body, for a map that needs targetWidth pixels
		/// across 360 degrees. A SCANSAT_BODY_TEXTURES cfg wins: its files are loaded at the mip that
		/// covers the request and Kopernicus OnDemand is never involved. Otherwise the body's own
		/// ScaledSpace textures, which must already be resident (see getScaledSpaceSource) - or are
		/// paged in here when a cfg the OnDemand paths trusted turns out not to load (see cfgFallback).
		/// </summary>
		internal bool getVisualSource(CelestialBody b, int targetWidth, out Texture colorTex, out Texture normalTex, out int normalYChannel, out Texture gasRamp, out bool fromConfig)
		{
			colorTex = null;
			normalTex = null;
			normalYChannel = 0;
			gasRamp = null;
			fromConfig = false;

			if (b == null)
			{
				return false;
			}

			mapTextureHandler.TryGetValue(b, out SCANtextures t);

			if (t != null && t.HasConfig && t.EnsureLoaded(targetWidth))
			{
				colorTex = t.ColorTexture;
				normalTex = t.NormalTexture;
				normalYChannel = t.NormalYChannel;
				fromConfig = true;
				return true;
			}

			if (!getScaledSpaceSource(b, out colorTex, out normalTex, out _, out gasRamp))
			{
				// A cfg body whose colour map would not load: OnDemand was told to skip it back in
				// setBody, before the load could fail, so its own textures are not resident. Page them
				// in here and take them instead of showing nothing.
				if (!cfgFallback(b, t) || !getScaledSpaceSource(b, out colorTex, out normalTex, out _, out gasRamp))
				{
					colorTex = null;   // never hand back OnDemand's 1x1 placeholder (getScaledSpaceSource assigns it before rejecting it)
					normalTex = null;
					gasRamp = null;
					return false;
				}
			}

			Texture2D n2d = normalTex as Texture2D;
			normalYChannel = n2d != null ? SCANtextures.NormalYChannelFor(n2d.format) : 0;
			return true;
		}

		/// <summary>
		/// The body declares a SCANSAT_BODY_TEXTURES colorMap that will not load. The cfg promised to
		/// replace ScaledSpace, so loadOnDemandScaledSpace skipped this body - the skip happens in
		/// setBody, before the first EnsureLoaded, so the failure is only known here. Remember it (both
		/// OnDemand paths treat the body as an ordinary one from now on) and ask Kopernicus for its
		/// textures. Not one-shot: two maps on the same body can evict each other's copy, and the next
		/// composite has to be able to ask again.
		/// </summary>
		private bool cfgFallback(CelestialBody b, SCANtextures t)
		{
			if (t == null || !t.ColorFailed || b.scaledBody == null || !SCAN_Settings_Config.Instance.VisibleMapsActive)
			{
				return false;
			}

			if (SCANtextures.MarkColorFailed(b))
			{
				Log.Error($"[{b.bodyName}] Visual: SCANSAT_BODY_TEXTURES colorMap \"{t.colorMapPath}\" could not be loaded; falling back to this body's ScaledSpace textures");
			}

			SCANkopernicus.LoadOnDemand(b);
			return true;
		}

		public static List<SCANterrainConfig> EncodeTerrainConfigs
		{
			get
			{
				try
				{
					return masterTerrainNodes.Values.ToList();
				}
				catch (Exception e)
				{
					SCANUtil.SCANlog("Error while saving SCANsat altimetry config data: {0}", e);
				}

				return new List<SCANterrainConfig>();
			}
		}

		public static void setMasterTerrainNodes(List<SCANterrainConfig> terrainConfigs)
		{
			masterTerrainNodes.Clear();
			try
			{
				masterTerrainNodes = terrainConfigs.ToDictionary(a => a.Name, a => a);
			}
			catch (Exception e)
			{
				SCANUtil.SCANlog("Error while loading SCANsat terrain config settings: {0}", e);
			}
		}


		public static void checkLoadedTerrainNodes()
		{
			for (int i = 0; i < FlightGlobals.Bodies.Count; i++)
			{
				CelestialBody b = FlightGlobals.Bodies[i];

				if (b == null)
				{
					continue;
				}

				if (!hasTerrainNode(b.bodyName))   // quiet check: the getter logs "cannot be found" for every body on first generation
				{
					SCANUtil.generateTerrainConfig(b);  // Sets the terrain config in the dictionary
				}
			}
		}

		public static bool hasTerrainNode(string name)
		{
			return masterTerrainNodes.ContainsKey(name);
		}

		public static SCANterrainConfig getTerrainNode(string name)
		{
			if (masterTerrainNodes.ContainsKey(name))
			{
				return masterTerrainNodes[name];
			}

			SCANUtil.SCANlog("SCANsat terrain config [{0}] cannot be found in master terrain storage list", name);
			return null;
		}

		public static void updateTerrainConfig(SCANterrainConfig t)
		{
			SCANterrainConfig update = getTerrainNode(t.Name);
			if (update != null)
			{
				// One call: the MinTerrain / MaxTerrain setters each refuse a value that crosses the other
				// end, so assigning them in sequence drops half of any pair that does not overlap what is
				// stored - which the colour window's Default button does whenever the defaults sit outside
				// the range currently on screen.
				update.SetRange(t.MinTerrain, t.MaxTerrain);
				update.ClampTerrain = t.ClampTerrain;
				update.PalSize = t.PalSize;
				update.PalRev = t.PalRev;
				update.PalDis = t.PalDis;
				update.ColorPal = t.ColorPal;

				// AutoGenerated records that the range is still the startup PQS guess, which is what lets
				// an authored SCANSAT_TERRAIN node replace a guess that was written to SCANcolors.cfg.
				// Only a value that differs from this config's own defaults is a user setting: Default,
				// and an Apply that changed nothing, must leave the flag as it was.
				update.AutoGenerated = update.DefaultAutoGenerated && atDefaultValues(update);
			}
		}

		private static bool atDefaultValues(SCANterrainConfig t)
		{
			if (t.ColorPal == null || t.DefaultPalette == null || t.ColorPal.Hash != t.DefaultPalette.Hash)
			{
				return false;
			}

			return Mathf.Approximately(t.MinTerrain, t.DefaultMinHeight)
				&& Mathf.Approximately(t.MaxTerrain, t.DefaultMaxHeight)
				&& t.ClampTerrain == t.DefaultClampHeight
				&& t.PalSize == t.DefaultPaletteSize
				&& t.PalRev == t.DefaultReverse
				&& t.PalDis == t.DefaultDiscrete;
		}

		// Terrain configs are generated once, at the main menu (checkLoadedTerrainNodes), for every
		// body the cfg did not cover; nothing removes them at runtime. No duplicate guard: the one
		// that used to be here only ever fired because of the controller-null bug in the getter.
		public static void addToTerrainConfigData(string name, SCANterrainConfig data)
		{
			masterTerrainNodes[name] = data;
		}

		public static int MasterResourceCount
		{
			get { return loadedResources.Count; }
		}

		public static List<SCANresourceGlobal> EncodeResourceConfigs
		{
			get
			{
				try
				{
					return masterResourceNodes.Values.ToList();
				}
				catch (Exception e)
				{
					SCANUtil.SCANlog("Error while saving SCANsat resource config data: {0}", e);
				}

				return new List<SCANresourceGlobal>();
			}
		}

		public static void setMasterResourceNodes(List<SCANresourceGlobal> resourceConfigs)
		{
			masterResourceNodes.Clear();

			try
			{
				int l = resourceConfigs.Count;

				for (int i = 0; i < l; i++)
				{
					SCANresourceGlobal r = resourceConfigs[i];

					if (r == null || string.IsNullOrEmpty(r.Name))
					{
						continue;
					}

					if (!masterResourceNodes.Contains(r.Name))
					{
						masterResourceNodes.Add(r.Name, r);
					}
				}
			}
			catch (Exception e)
			{
				SCANUtil.SCANlog("Error while loading SCANsat resource config settings: {0}", e);
			}
		}

		public static SCANresourceGlobal getResourceNode(string resourceName, bool warn = false)
		{
			if (masterResourceNodes.Contains(resourceName))
			{
				return masterResourceNodes[resourceName];
			}
			else if (warn)
			{
				SCANUtil.SCANlog("SCANsat resource [{0}] cannot be found in master resource storage list", resourceName);
			}

			return null;
		}

		public static SCANresourceGlobal GetFirstResource
		{
			get
			{
				if (masterResourceNodes.Count > 0)
				{
					return masterResourceNodes.At(0);
				}
				else
				{
					SCANUtil.SCANlog("SCANsat resource storage list is empty; something probably went wrong here...");
				}

				return null;
			}
		}

		public static void updateSCANresource(SCANresourceGlobal r, bool all)
		{
			SCANresourceGlobal update = getResourceNode(r.Name, true);
			if (update != null)
			{
				update.MinColor = r.MinColor;
				update.MaxColor = r.MaxColor;
				update.Transparency = r.Transparency;
				if (all)
				{
					for (int i = 0; i < update.getBodyCount; i++)
					{
						SCANresourceBody b = update.getBodyConfig(i);
						if (b != null)
						{
							SCANresourceBody bNew = r.getBodyConfig(b.BodyName);
							if (bNew != null)
							{
								b.MinValue = bNew.MinValue;
								b.MaxValue = bNew.MaxValue;
							}
						}
					}
				}
				else
				{
					SCANresourceBody b = update.getBodyConfig(r.CurrentBody.BodyName);
					if (b != null)
					{
						b.MinValue = r.CurrentBody.MinValue;
						b.MaxValue = r.CurrentBody.MaxValue;
					}
				}
			}
		}

		public static void addToResourceData(string name, SCANresourceGlobal res)
		{
			if (!masterResourceNodes.Contains(name))
			{
				masterResourceNodes.Add(name, res);
			}
			else
			{
				Log.Error(string.Format("SCANResource Dictionary Already Contains Key of This Type: Resource: {0}", name));
			}
		}

		public static void addToLoadedResourceNames(string name, bool warn = true)
		{
			if (!loadedResources.Contains(name))
			{
				loadedResources.Add(name);
			}
			else if (warn)
			{
				Log.Error(string.Format("Loaded Resource List Already Contains Resource Of Name: {0}", name));
			}
		}

		public static List<SCANresourceGlobal> setLoadedResourceList()
		{
			List<SCANresourceGlobal> rList = new List<SCANresourceGlobal>();
			SCANresourceGlobal ore = null;

			int l = loadedResources.Count;

			for (int i = 0; i < l; i++)
			{
				string r = loadedResources[i];

				if (string.IsNullOrEmpty(r))
				{
					continue;
				}

				if (masterResourceNodes.Contains(r))
				{
					if (r != "Ore")
					{
						rList.Add(masterResourceNodes[r]);
					}
					else
					{
						ore = masterResourceNodes[r];
					}
				}
			}

			if (ore != null)
			{
				rList.Insert(0, ore);
			}

			return rList;
		}

		public static List<SCANresourceGlobal> resources()
		{
			return masterResourceNodes.Values.ToList();
		}

		public List<SCANvessel> Known_Vessels
		{
			get { return knownVessels.Values.ToList(); }
		}

		public int ActiveSensors
		{
			get { return activeSensors; }
		}

		public int ActiveVessels
		{
			get { return activeVessels; }
		}

		public int ActualPasses
		{
			get { return actualPasses; }
		}

		public bool ContractsLoaded
		{
			get { return contractsLoaded; }
		}

		public bool MechJebLoaded
		{
			get { return mechjebLoaded; }
			set { mechjebLoaded = value; }
		}

		public SCANwaypoint LandingTarget
		{
			get { return landingTarget; }
			set { landingTarget = value; }
		}

		public bool SerenityLoaded
		{
			get { return serenityLoaded; }
		}

		public class OnMJTargetSet : UnityEvent<Vector2d, CelestialBody> { }

		public OnMJTargetSet MJTargetSet = new OnMJTargetSet();

		#endregion

		#region save/load

		public override void OnLoad(ConfigNode node)
		{
			// Saves from before mainMapDisplayMode existed stored the main map's mode as a bool.
			if (node.HasValue("mainMapBiome") && !node.HasValue("mainMapDisplayMode"))
			{
				mainMapDisplayMode = node.parse("mainMapBiome", false) ? MainMapDisplayMode.Biome : MainMapDisplayMode.Terrain;
			}

			if (storageUpgraded)
			{
				ConfigNode node_vessels = node.GetNode("Scanners");
				if (node_vessels != null)
				{
					SCANUtil.SCANlog("SCANsat Controller: Loading {0} known vessels", node_vessels.CountNodes);
					foreach (ConfigNode node_vessel in node_vessels.GetNodes("Vessel"))
					{
						Guid id = node_vessel.parse("guid", new Guid());

						if (id == new Guid())
						{
							SCANUtil.SCANlog("Something Went Wrong Loading This SCAN Vessel; Moving On To The Next");
							continue;
						}

						foreach (ConfigNode node_sensor in node_vessel.GetNodes("Sensor"))
						{
							int sensor = node_sensor.parse("type", (int)0);
							double fov = node_sensor.parse("fov", 3d);
							double min_alt = node_sensor.parse("min_alt", (double)minScanAlt);
							double max_alt = node_sensor.parse("max_alt", (double)maxScanAlt);
							double best_alt = node_sensor.parse("best_alt", (double)bestScanAlt);
							bool require_light = node_sensor.parse("require_light", (bool)false);

							registerSensorTemp(id, (SCANtype)sensor, fov, min_alt, max_alt, best_alt, require_light);

							tempIDs.Add(id);
						}
					}
				}
			}

			ConfigNode node_progress = node.GetNode("Progress");
			if (node_progress != null)
			{
				foreach (ConfigNode node_body in node_progress.GetNodes("Body"))
				{
					string body_name = node_body.parse("Name", "");

					if (string.IsNullOrEmpty(body_name))
					{
						SCANUtil.SCANlog("SCANsat Controller: Error while loading Celestial Body data; skipping value...");
						continue;
					}

					SCANUtil.SCANlog("SCANsat Controller: Loading map for {0}", body_name);

					CelestialBody body;
					try
					{
						body = FlightGlobals.Bodies.FirstOrDefault(b => b.bodyName == body_name);
					}
					catch (Exception e)
					{
						Log.Error(string.Format("Error in loading Celestial Body [{0}]...\n{1}", body_name, e));
						continue;
					}

					if (body != null)
					{
						SCANdata data = getData(body.bodyName);
						if (data == null)
						{
							data = new SCANdata(body);
						}

						if (!body_data.Contains(body_name))
						{
							body_data.Add(body_name, data);
						}
						else
						{
							body_data[body_name] = data;
						}

						try
						{
							string mapdata = node_body.parse("Map", "");

							if (string.IsNullOrEmpty(mapdata))
							{
								SCANUtil.SCANlog("SCANsat Controller: Error while loading Celestial Body map data; skipping value...");
								continue;
							}

							if (storageUpgraded)
							{
								data.shortDeserialize(mapdata);
							}
							else
							{
								data.ConvertStorage(mapdata);
							}

						}
						catch (Exception e)
						{
							SCANUtil.SCANlog("Something Went Wrong Loading Scanning Data; Resetting Coverage: {0}", e);
							data.reset();
							// fail somewhat gracefully; don't make the save unloadable 
						}

						try
						{
							if (SCANmainMenuLoader.MechJebLoaded && SCAN_Settings_Config.Instance.MechJebTarget && SCAN_Settings_Config.Instance.MechJebTargetLoad)
							{
								string targetName = node_body.parse("LandingTarget", "");

								if (!string.IsNullOrEmpty(targetName))
								{
									loadWaypoint(targetName, body);
								}
							}

							data.Disabled = node_body.parse("Disabled", false);
						}
						catch (Exception e)
						{
							SCANUtil.SCANlog("Error Loading SCANdata; Reverting To Default Settings: {0}", e);
						}
					}
				}
			}

			ConfigNode node_resources = node.GetNode("SCANResources");
			if (node_resources != null)
			{
				foreach (ConfigNode node_resource_type in node_resources.GetNodes("ResourceType"))
				{
					if (node_resource_type != null)
					{
						loadCustomResourceValues(node_resource_type);
					}
				}
			}
			loaded = true;
			storageUpgraded = true;
		}

		public override void OnSave(ConfigNode node)
		{
			int l = knownVessels.Count;
			ConfigNode node_vessels = new ConfigNode("Scanners");

			for (int i = 0; i < l; i++)
			{
				SCANvessel sv = knownVessels.At(i);
				ConfigNode node_vessel = new ConfigNode("Vessel");
				node_vessel.AddValue("guid", sv.id.ToString());
				if (sv.vessel != null)
				{
					node_vessel.AddValue("name", sv.vessel.vesselName);
				}

				foreach (SCANsensor sensor in sv.sensors)
				{
					ConfigNode node_sensor = new ConfigNode("Sensor");
					node_sensor.AddValue("type", (int)sensor.sensor);
					node_sensor.AddValue("fov", sensor.fov);
					node_sensor.AddValue("min_alt", sensor.min_alt);
					node_sensor.AddValue("max_alt", sensor.max_alt);
					node_sensor.AddValue("best_alt", sensor.best_alt);
					node_sensor.AddValue("require_light", sensor.requireLight);
					node_vessel.AddNode(node_sensor);
				}
				node_vessels.AddNode(node_vessel);
			}
			node.AddNode(node_vessels);
			if (body_data != null)
			{
				ConfigNode node_progress = new ConfigNode("Progress");
				foreach (string body_name in body_data.Keys)
				{
					ConfigNode node_body = new ConfigNode("Body");
					SCANdata body_scan = body_data[body_name];
					node_body.AddValue("Name", body_name);
					node_body.AddValue("Disabled", body_scan.Disabled);
					if (SCANmainMenuLoader.MechJebLoaded && SCAN_Settings_Config.Instance.MechJebTarget && SCAN_Settings_Config.Instance.MechJebTargetLoad)
					{
						SCANwaypoint w = body_scan.Waypoints.FirstOrDefault(a => a.LandingTarget);
						if (w != null)
						{
							node_body.AddValue("LandingTarget", string.Format("{0:N4},{1:N4}", w.Latitude, w.Longitude));
						}
					}
					node_body.AddValue("Map", body_scan.shortSerialize());
					node_progress.AddNode(node_body);
				}
				node.AddNode(node_progress);
			}
			if (masterResourceNodes.Count > 0)
			{
				ConfigNode node_resources = new ConfigNode("SCANResources");
				foreach (SCANresourceGlobal r in masterResourceNodes.Values)
				{
					if (r != null)
					{
						ConfigNode node_resource_type = new ConfigNode("ResourceType");
						node_resource_type.AddValue("Resource", r.Name);
						node_resource_type.AddValue("MinColor", ConfigNode.WriteColor(r.MinColor));
						node_resource_type.AddValue("MaxColor", ConfigNode.WriteColor(r.MaxColor));
						node_resource_type.AddValue("Transparency", r.Transparency);

						string rMinMax = saveResources(r);
						node_resource_type.AddValue("MinMaxValues", rMinMax);
						node_resources.AddNode(node_resource_type);
					}
				}
				node.AddNode(node_resources);
			}
		}

		#endregion

		public override void OnAwake()
		{
			instance = this;

			serenityLoaded = Expansions.ExpansionsLoader.IsExpansionInstalled("Serenity");

			if (SCAN_Settings_Config.Instance == null)
			{
				return;
			}

			SCANUtil.fillLocalizedNames();

			lowBiomeColor32 = SCAN_Settings_Config.Instance.LowBiomeColor;
			highBiomeColor32 = SCAN_Settings_Config.Instance.HighBiomeColor;
			lowSlopeColorOne32 = SCAN_Settings_Config.Instance.BottomLowSlopeColor;
			highSlopeColorOne32 = SCAN_Settings_Config.Instance.BottomHighSlopeColor;
			lowSlopeColorTwo32 = SCAN_Settings_Config.Instance.TopLowSlopeColor;
			highSlopeColorTwo32 = SCAN_Settings_Config.Instance.TopHighSlopeColor;
		}

		private void Start()
		{
			for (int i = tempIDs.Count - 1; i >= 0; i--)
			{
				finishRegistration(tempIDs[i]);
			}

			GameEvents.OnScienceRecieved.Add(watcher);
			GameEvents.OnOrbitalSurveyCompleted.Add(onSurvey);
			GameEvents.onVesselSOIChanged.Add(SOIChange);
			GameEvents.onVesselCreate.Add(newVesselCheck);
			GameEvents.onPartCouple.Add(dockingEventCheck);
			GameEvents.Contract.onContractsLoaded.Add(contractsCheck);
			GameEvents.Contract.onParameterChange.Add(onParamChange);

			if (HighLogic.LoadedSceneIsFlight)
			{
				if (!body_data.Contains(FlightGlobals.currentMainBody.bodyName))
				{
					body_data.Add(FlightGlobals.currentMainBody.bodyName, new SCANdata(FlightGlobals.currentMainBody));
				}


				try
				{
					_mainMap = new SCAN_UI_MainMap();
					_bigMap = new SCAN_UI_BigMap();
					_zoomMap = new SCAN_UI_ZoomMap();
					_instruments = new SCAN_UI_Instruments();
					_overlay = new SCAN_UI_Overlay();
					_settings = new SCAN_UI_Settings();
				}
				catch (Exception e)
				{
					SCANUtil.SCANlog("Something Went Wrong Initializing UI Objects:\n{0}", e);
				}

				StartCoroutine(WaitForScienceUpdate());
			}
			else if (HighLogic.LoadedSceneHasPlanetarium)
			{
				if (!body_data.Contains(Planetarium.fetch.Home.bodyName))
				{
					body_data.Add(Planetarium.fetch.Home.bodyName, new SCANdata(Planetarium.fetch.Home));
				}

				try
				{
					_bigMap = new SCAN_UI_BigMap();
					_settings = new SCAN_UI_Settings();
					_zoomMap = new SCAN_UI_ZoomMap();

					if (HighLogic.LoadedScene == GameScenes.TRACKSTATION)
					{
						_overlay = new SCAN_UI_Overlay();
					}
				}
				catch (Exception e)
				{
					SCANUtil.SCANlog("Something Went Wrong Initializing UI Objects:\n{0}", e);
				}
			}
			if (SCAN_Settings_Config.Instance.StockToolbar)
			{
				appLauncher = gameObject.AddComponent<SCANappLauncher>();
			}

			if (SCAN_Settings_Config.Instance.DisableStockResource && SCAN_Settings_Config.Instance.UseStockTreshold)
			{
				for (int i = FlightGlobals.Bodies.Count - 1; i >= 0; i--)
				{
					CelestialBody b = FlightGlobals.Bodies[i];

					checkResourceScanStatus(b);
				}
			}
			else if (!SCAN_Settings_Config.Instance.DisableStockResource && SCAN_Settings_Config.Instance.InstantScan)
			{
				for (int i = FlightGlobals.Bodies.Count - 1; i >= 0; i--)
				{
					CelestialBody b = FlightGlobals.Bodies[i];

					checkStockResourceScanStatus(b);
				}
			}

			for (int i = body_data.Count - 1; i >= 0; i--)
			{
				body_data.At(i).updateCoverage();
			}
		}

		private void Update()
		{
			if (SCAN_Settings_Config.Instance.BackgroundScanning && loaded)
			{
				scanFromAllVessels();
			}

			if (heightMapWorkPending)
			{
				checkHeightMapStatus();
			}
		}

		private IEnumerator WaitForScienceUpdate()
		{
			while (!FlightGlobals.ready || FlightGlobals.ActiveVessel == null)
			{
				yield return null;
			}

			SCANUtil.UpdateAllVesselData(FlightGlobals.ActiveVessel);
		}

		public void checkStockResourceScanStatus(CelestialBody body)
		{
			if (SCAN_Settings_Config.Instance.DisableStockResource || !SCAN_Settings_Config.Instance.InstantScan)
			{
				return;
			}

			if (body == null)
			{
				return;
			}

			if (!ResourceMap.Instance.IsPlanetScanned(body.flightGlobalsIndex))
			{
				return;
			}

			if (SCANUtil.GetCoverage((short)SCANtype.ResourceHiRes, body) >= 100)
			{
				return;
			}

			SCANdata data = getData(body.bodyName);

			if (data == null)
			{
				return;
			}

			data.fillResourceMap();
		}

		public void checkResourceScanStatus(CelestialBody body)
		{
			if (!SCAN_Settings_Config.Instance.UseStockTreshold)
			{
				return;
			}

			if (body == null)
			{
				return;
			}

			if (ResourceMap.Instance.IsPlanetScanned(body.flightGlobalsIndex))
			{
				return;
			}

			SCANdata data = getData(body.bodyName);

			if (data == null)
			{
				return;
			}

			if (SCANUtil.getCoveragePercentage(data, SCANtype.ResourceLoRes) > (SCAN_Settings_Config.Instance.StockTreshold * 100) || SCANUtil.getCoveragePercentage(data, SCANtype.ResourceHiRes) > (SCAN_Settings_Config.Instance.StockTreshold * 100))
			{
				SCANUtil.SCANlog("SCANsat resource scanning for {0} meets threshold value [{1:P0}]\nConducting stock orbital resource scan...", body.bodyName, SCAN_Settings_Config.Instance.StockTreshold);
				ResourceMap.Instance.UnlockPlanet(body.flightGlobalsIndex);
			}
		}

		private bool currentlyBuilding;
		private SCANdata buildingData;
		private readonly List<SCANdata> heightMapRequests = new List<SCANdata>();

		/// <summary>
		/// Build a body's 360x180 height map in the background, under the shared per-frame CPU budget
		/// (see pumpHeightMap), if it is not built yet. Only the two maps that actually read it - the
		/// small map's Terrain and the terrain overlay - ever ask: the overlay from here when it takes a
		/// body, so the grid is warm by the time terrain is picked, and both from their own pumps if it
		/// is not. Nothing else builds it. The palette range no longer comes from here at all; it is the
		/// authored terrain node, or the body's own pqsController.radiusMin/Max.
		/// </summary>
		internal void RequestHeightMap(SCANdata d)
		{
			// "d == buildingData" only bars a build this controller is actually running: the reference is
			// dropped the moment the build is handed over, or every later request would be swallowed.
			if (d == null || d.Built || (currentlyBuilding && d == buildingData) || heightMapRequests.Contains(d))
			{
				return;
			}

			heightMapRequests.Add(d);
		}

		/// <summary>
		/// One frame's worth of a body's height map build, under the per-frame CPU budget the GPU map
		/// builds share (SCANmap.claimBuildBudget): at least one row, then as many more as fit. The old
		/// one-row-per-call cadence made this frame-rate bound rather than CPU bound - the 64,800 PQS
		/// samples are only about 80 ms of work, but they took 4.5 s on the Moon (RSS) and around ten
		/// in a heavier scene, ~98% of it spent waiting for the next frame. Whoever owns the build (here,
		/// the small map or the overlay) pumps it through this, so all three run at the same pace. The
		/// row cursor is the SCANdata's own, so a build taken over by another builder carries on from
		/// where it stopped.
		/// </summary>
		internal static void pumpHeightMap(SCANdata d)
		{
			long start = System.Diagnostics.Stopwatch.GetTimestamp();
			long budget = SCANmap.claimBuildBudget();

			while (true)
			{
				d.generateHeightMap(360);

				// Every call either finishes the map or advances a row, so this always terminates.
				if (d.Built || System.Diagnostics.Stopwatch.GetTimestamp() - start >= budget)
				{
					break;
				}
			}

			SCANmap.reportBuildBudgetUsed(System.Diagnostics.Stopwatch.GetTimestamp() - start);
		}

		private bool heightMapWorkPending
		{
			get { return currentlyBuilding || heightMapRequests.Count > 0; }
		}

		private void checkHeightMapStatus()
		{
			if (!currentlyBuilding)
			{
				while (heightMapRequests.Count > 0)
				{
					buildingData = heightMapRequests[0];
					heightMapRequests.RemoveAt(0);

					if (buildingData == null || buildingData.Built)
					{
						continue;   // built meanwhile
					}

					// A window is pumping it - unless its claim has lapsed, in which case that window is
					// gone and the build is ours to finish, from the row it stopped on.
					if ((buildingData.MapBuilding || buildingData.OverlayBuilding) && !buildingData.BuildStalled)
					{
						continue;
					}

					buildingData.takeOverBuild();
					buildingData.ControllerBuilding = true;
					currentlyBuilding = true;
					return;
				}

				buildingData = null;
				return;
			}

			if (buildingData == null)
			{
				currentlyBuilding = false;
				return;
			}

			if (buildingData.Built)
			{
				currentlyBuilding = false;
				buildingData.ControllerBuilding = false;
				buildingData = null;
				return;
			}

			if (buildingData.ControllerBuilding)
			{
				pumpHeightMap(buildingData);
				return;
			}

			// Another window took the build over. Drop the reference as well: heightMapWorkPending goes
			// false here, so nothing would ever clear it again, and RequestHeightMap for this body would
			// be a no-op for the rest of the scene.
			buildingData = null;
			currentlyBuilding = false;
		}

		private void OnDestroy()
		{
			GameEvents.OnScienceRecieved.Remove(watcher);
			GameEvents.OnOrbitalSurveyCompleted.Remove(onSurvey);
			GameEvents.onVesselSOIChanged.Remove(SOIChange);
			GameEvents.onVesselCreate.Remove(newVesselCheck);
			GameEvents.onPartCouple.Remove(dockingEventCheck);
			GameEvents.Contract.onContractsLoaded.Remove(contractsCheck);
			GameEvents.Contract.onParameterChange.Remove(onParamChange);

			if (appLauncher != null)
			{
				Destroy(appLauncher);
			}

			if (_mainMap != null)
			{
				_mainMap.OnDestroy();
				_mainMap = null;
			}
			if (_bigMap != null)
			{
				_bigMap.OnDestroy();
				_bigMap = null;
			}
			if (_instruments != null)
			{
				_instruments.OnDestroy();
				_instruments = null;
			}
			if (_overlay != null)
			{
				_overlay.OnDestroy();
				_overlay = null;
			}
			if (_settings != null)
			{
				_settings.OnDestroy();
				_settings = null;
			}
			if (_zoomMap != null)
			{
				_zoomMap.OnDestroy();
				_zoomMap = null;
			}

			if (SCAN_Settings_Config.Instance != null)
			{
				SCAN_Settings_Config.Instance.Save();
			}

			// SCANdata.generateHeightMap loads and unloads PQS with mapSource.Data whichever builder drives
			// it, so a build the small map or the terrain overlay owned leaves data loaded with this
			// controller's own build flag clear. dataBodies is what is actually loaded; unload all of it.
			for (int i = dataBodies.Count - 1; i >= 0; i--)
			{
				unloadPQS(dataBodies[i]);
			}

			// GPU textures loaded for Visual maps are not scene-managed either.
			foreach (SCANtextures t in mapTextureHandler.Values)
			{
				t.Release();
			}
			mapTextureHandler.Clear();
		}

		private void watcher(float sci, ScienceSubject sub, ProtoVessel v, bool b)
		{
			if (!HighLogic.LoadedSceneIsFlight)
			{
				return;
			}

			if (FlightGlobals.ActiveVessel == null)
			{
				return;
			}

			if (sub == null)
			{
				return;
			}

			if (!sub.id.StartsWith("SCAN"))
			{
				return;
			}

			SCANUtil.UpdateVesselData(FlightGlobals.ActiveVessel, sub);
		}

		private void onSurvey(Vessel v, CelestialBody b)
		{
			if (!SCAN_Settings_Config.Instance.InstantScan || SCAN_Settings_Config.Instance.DisableStockResource)
			{
				return;
			}

			if (b == null)
			{
				return;
			}

			if (SCANUtil.GetCoverage((short)SCANtype.ResourceHiRes, b) >= 100)
			{
				return;
			}

			SCANdata data = SCANUtil.getData(b);

			if (data == null)
			{
				data = new SCANdata(b);
				addToBodyData(b, data);
			}

			data.fillResourceMap();
		}

		internal void loadPQS(CelestialBody b, mapSource s = mapSource.Data)
		{
			if (!SCANkopernicus.KopernicusLoaded)
			{
				return;
			}

			if (b == null)
			{
				return;
			}

			switch (s)
			{
				case mapSource.Data:
					if (dataBodies.Contains(b))
					{
						return;
					}

					dataBodies.Add(b);

					if (bigMapBodyPQS != null && bigMapBodyPQS == b)
					{
						return;
					}

					if (zoomMapBodyPQS != null && zoomMapBodyPQS == b)
					{
						return;
					}

					break;
				case mapSource.BigMap:
					if (bigMapBodyPQS != null && bigMapBodyPQS == b)
					{
						return;
					}

					bigMapBodyPQS = b;

					if (zoomMapBodyPQS != null && zoomMapBodyPQS == b)
					{
						return;
					}

					if (dataBodies.Contains(b))
					{
						return;
					}

					break;

				case mapSource.ZoomMap:
					if (zoomMapBodyPQS != null && zoomMapBodyPQS == b)
					{
						return;
					}

					zoomMapBodyPQS = b;

					if (bigMapBodyPQS != null && bigMapBodyPQS == b)
					{
						return;
					}

					if (dataBodies.Contains(b))
					{
						return;
					}

					break;
				case mapSource.RPM:
					return;
			}

			SCANkopernicus.LoadPQS(b);
		}

		internal void unloadPQS(CelestialBody b, mapSource s = mapSource.Data)
		{
			if (!SCANkopernicus.KopernicusLoaded)
			{
				return;
			}

			if (b == null)
			{
				return;
			}

			switch (s)
			{
				case mapSource.Data:
					if (dataBodies.Contains(b))
					{
						dataBodies.RemoveAll(a => a == b);
					}

					if (bigMapBodyPQS != null && bigMapBodyPQS == b)
					{
						return;
					}

					if (zoomMapBodyPQS != null && zoomMapBodyPQS == b)
					{
						return;
					}

					break;
				case mapSource.BigMap:
					bigMapBodyPQS = null;

					if (zoomMapBodyPQS != null && zoomMapBodyPQS == b)
					{
						return;
					}

					if (dataBodies.Contains(b))
					{
						return;
					}

					break;

				case mapSource.ZoomMap:
					zoomMapBodyPQS = null;

					if (bigMapBodyPQS != null && bigMapBodyPQS == b)
					{
						return;
					}

					if (dataBodies.Contains(b))
					{
						return;
					}

					break;
				case mapSource.RPM:
					return;
			}

			bool setInactive = false;

			switch (HighLogic.LoadedScene)
			{
				case GameScenes.SPACECENTER:
					if (b != Planetarium.fetch.Home)
					{
						setInactive = true;
					}

					break;
				case GameScenes.TRACKSTATION:
					setInactive = true;
					break;
				case GameScenes.FLIGHT:
					if (b != FlightGlobals.currentMainBody)
					{
						setInactive = true;
					}

					break;
			}

			if (!setInactive)
			{
				return;
			}

			SCANkopernicus.UnloadPQS(b);
		}

		internal void loadOnDemandScaledSpace(CelestialBody b, mapSource s)
		{
			// Bodies with a SCANSAT_BODY_TEXTURES cfg never touch Kopernicus OnDemand: the Visual map
			// reads its own mip of the declared files, so forcing the full-size ScaledSpace textures in
			// (or out) would only cost VRAM and a stall. A body whose declared colour map failed to
			// load is not one of them any more (see UseConfigFor): it pages in and out like any other.
			if (SCANtextures.UseConfigFor(b))
			{
				return;
			}

			if (!SCAN_Settings_Config.Instance.VisibleMapsActive)
			{
				return;
			}

			if (b == null)
			{
				return;
			}

			if (b.scaledBody == null)
			{
				return;
			}

			switch (s)
			{
				case mapSource.BigMap:
					if (bigMapBodyVisual != null && bigMapBodyVisual == b)
					{
						return;
					}

					bigMapBodyVisual = b;

					if (zoomMapBodyVisual != null && zoomMapBodyVisual == b)
					{
						return;
					}

					break;
				case mapSource.ZoomMap:
					if (zoomMapBodyVisual != null && zoomMapBodyVisual == b)
					{
						return;
					}

					zoomMapBodyVisual = b;

					if (bigMapBodyVisual != null && bigMapBodyVisual == b)
					{
						return;
					}

					break;
			}

			SCANUtil.SCANdebugLog("[SCANmem] OnDemand LOAD {0} src={1} texMem={2:F1}MB", b.bodyName, s, UnityEngine.Texture.currentTextureMemory / 1048576f);
				SCANkopernicus.LoadOnDemand(b);
		}

		internal void unloadOnDemandScaledSpace(CelestialBody b, mapSource s)
		{
			// Bodies with a SCANSAT_BODY_TEXTURES cfg never touch Kopernicus OnDemand: the Visual map
			// reads its own mip of the declared files, so forcing the full-size ScaledSpace textures in
			// (or out) would only cost VRAM and a stall. A body whose declared colour map failed to
			// load is not one of them any more (see UseConfigFor): it pages in and out like any other.
			if (SCANtextures.UseConfigFor(b))
			{
				return;
			}

			if (!SCANkopernicus.KopernicusLoaded)
			{
				return;
			}

			if (!SCAN_Settings_Config.Instance.VisibleMapsActive)
			{
				return;
			}

			if (b == null)
			{
				return;
			}

			if (b.scaledBody == null)
			{
				return;
			}

			switch (s)
			{
				case mapSource.BigMap:
					bigMapBodyVisual = null;

					if (zoomMapBodyVisual != null && zoomMapBodyVisual == b)
					{
						return;
					}

					break;
				case mapSource.ZoomMap:
					zoomMapBodyVisual = null;

					if (bigMapBodyVisual != null && bigMapBodyVisual == b)
					{
						return;
					}

					break;
			}

			bool setInactive = false;

			switch (HighLogic.LoadedScene)
			{
				case GameScenes.SPACECENTER:
					setInactive = true;
					break;
				case GameScenes.TRACKSTATION:
					setInactive = !InCurrentBodyFamily(b);
					break;
				case GameScenes.FLIGHT:
					setInactive = !InCurrentBodyFamily(b);
					break;
			}

			if (!setInactive)
			{
				return;
			}

			SCANUtil.SCANdebugLog("[SCANmem] OnDemand UNLOAD {0} src={1} texMem={2:F1}MB", b.bodyName, s, UnityEngine.Texture.currentTextureMemory / 1048576f);
				SCANkopernicus.UnloadOnDemand(b);
		}

		private bool InCurrentBodyFamily(CelestialBody b)
		{
			switch (HighLogic.LoadedScene)
			{
				case GameScenes.TRACKSTATION:
					if (PlanetariumCamera.fetch == null || PlanetariumCamera.fetch.target == null)
						break;
					MapObject mo = PlanetariumCamera.fetch.target;

					CelestialBody tgt = null;

					if (mo.vessel != null)
					{
						tgt = mo.vessel.mainBody;
					}
					else if (mo.celestialBody != null)
					{
						tgt = mo.celestialBody;
					}

					if (tgt == null)
					{
						return false;
					}

					if (b == tgt)
					{
						return true;
					}

					if (tgt.HasChild(b))
					{
						return true;
					}

					if (tgt.HasParent(b))
					{
						return true;
					}

					break;
				case GameScenes.FLIGHT:
					if (FlightGlobals.currentMainBody == null)   // null mid scene-teardown (called via OnDestroy)
						break;
					if (b == FlightGlobals.currentMainBody)
					{
						return true;
					}

					if (FlightGlobals.currentMainBody.HasChild(b))
					{
						return true;
					}

					if (FlightGlobals.currentMainBody.HasParent(b))
					{
						return true;
					}

					break;
			}

			return false;
		}

		// Can't remove this or parallax's patches break
		internal void LoadVisualMapTexture(CelestialBody b, mapSource s)
		{

		}

		void GetVisualMapTexturesForBody(CelestialBody b, out Material material, out string gasRampTextureName, out string colorMapTextureName, out string normalMapTextureName)
		{
			material = null;
			colorMapTextureName = null;
			normalMapTextureName = null;
			gasRampTextureName = null;

			if (b.scaledBody == null)
			{
				return;
			}

			MeshRenderer scaledMesh = b.scaledBody.GetComponent<MeshRenderer>();

			if (scaledMesh == null)
			{
				return;
			}

			material = scaledMesh.sharedMaterial; // TODO: what if there are multiple materials?  do we need to check all of them?
			string shaderName = material.shader.name;

			// A gas giant (Terrain/Gas Giant: Jool and everything templated on it) has no colour map to
			// read. KSP builds its colour by looking a cloud PATTERN up in a 1-D gradient - Jool's
			// _CloudColorMap is a 2x4096 ramp of greens - and both patterns are masks: _CloudPatternTexture
			// keeps one cloud layer in red and another in green, _DetailCloudPatternTexture is a
			// single-channel BC4 detail that the shader TILES. Drawing a pattern as if it were a colour
			// map is what made Jool's Visual map come out red. Hand the composite the un-tiled pattern
			// plus the ramp instead and let it do the lookup (SCANVisualComposite's _GasGiant branch).
			// Decided by what the material actually has, not by the shader's name, so a stripped or
			// re-shadered gas giant (Kopernicus, planet packs) is judged on its own textures; KSP's
			// low-quality gas giant material has a real _MainTex and falls through to the checks below.
			if (material.HasProperty("_CloudPatternTexture") && material.HasProperty("_CloudColorMap")
				&& material.GetTexture("_CloudPatternTexture") != null && material.GetTexture("_CloudColorMap") != null)
			{
				colorMapTextureName = "_CloudPatternTexture";
				gasRampTextureName = "_CloudColorMap";
				normalMapTextureName = material.HasProperty("_NormalMap") ? "_NormalMap" : null;
				return;
			}
			// HapkeScaled is Sol's Parallax-dependent scaled shader (from the simplify_map_drawing branch).
			else if (shaderName.Contains("ParallaxScaled") || shaderName.Contains("HapkeScaled"))
			{
				SCANparallaxContinued.LoadParallax(b, ref material);
				colorMapTextureName = "_ColorMap";
				// Parallax normal maps are BC5 (Y in green). The shader selects the channel by format
				// (_NormalYChannel); that is what the old "doesn't work with parallax" note was about.
				normalMapTextureName = "_BumpMap";
				return;
			}
			else if (material.HasProperty("_MainTex"))
			{
				colorMapTextureName = "_MainTex";
			}
			else if (material.HasProperty("_ColorMap"))
			{
				colorMapTextureName = "_ColorMap";
			}

			if (material.HasProperty("_BumpMap"))
			{
				normalMapTextureName = "_BumpMap";
			}
			else if (material.HasProperty("_NormalMap"))
			{
				normalMapTextureName = "_NormalMap";
			}
		}

		internal void LoadVisualMapTexture_Renamed(CelestialBody b, mapSource s)
		{
			if (!SCAN_Settings_Config.Instance.VisibleMapsActive)
			{
				return;
			}

			if (b == null)
			{
				return;
			}

			switch (s)
			{
				case mapSource.BigMap:
					bigMapBodyScaledSpace = b;
					break;
				case mapSource.ZoomMap:
					zoomMapBodyScaledSpace = b;
					break;
				case mapSource.Data:
					dataBodyScaledSpace = b;
					break;
				case mapSource.RPM:
					rpmBodyScaledSpace = b;
					break;
			}

			// Register the body; its textures load lazily, at the needed mip, in getVisualSource.
			if (!mapTextureHandler.ContainsKey(b))
			{
				mapTextureHandler.Add(b, new SCANtextures(b));
			}
		}

		internal void UnloadVisualMapTexture(CelestialBody b, mapSource s)
		{
			if (!SCAN_Settings_Config.Instance.VisibleMapsActive)
			{
				return;
			}

			if (b == null)
			{
				return;
			}

			switch (s)
			{
				case mapSource.BigMap:
					bigMapBodyScaledSpace = null;
					break;
				case mapSource.ZoomMap:
					zoomMapBodyScaledSpace = null;
					break;
				case mapSource.Data:
					dataBodyScaledSpace = null;
					break;
				case mapSource.RPM:
					rpmBodyScaledSpace = null;
					break;
			}

			// Another map is still showing this body's Visual textures: keep them.
			if (bigMapBodyScaledSpace == b || zoomMapBodyScaledSpace == b || dataBodyScaledSpace == b || rpmBodyScaledSpace == b)
			{
				return;
			}

			if (mapTextureHandler.TryGetValue(b, out SCANtextures t))
			{
				t.Release();
				mapTextureHandler.Remove(b);
			}
		}

		private void OnGUI()
		{
			if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedScene == GameScenes.TRACKSTATION)
			{
				drawTarget();
			}
		}

		private void drawTarget()
		{
			if (!MapView.MapIsEnabled)
			{
				return;
			}

			CelestialBody b = SCANUtil.getTargetBody(MapView.MapCamera.target);

			if (b == null)
			{
				return;
			}

			SCANdata d = getData(b.bodyName);

			if (d == null)
			{
				return;
			}

			if (SCAN_Settings_Config.Instance.ShowGroundTracks && HighLogic.LoadedSceneIsFlight && !d.Disabled && SCAN_Settings_Config.Instance.BackgroundScanning)
			{
				drawGroundTracks(b);
			}

			return;
		}

		private void drawGroundTracks(CelestialBody body)
		{
			if (SCAN_Settings_Config.Instance.GroundTracksActiveOnly)
			{
				Vessel v = FlightGlobals.ActiveVessel;

				if (v.mainBody != body)
				{
					return;
				}

				if (v.situation == Vessel.Situations.LANDED || v.situation == Vessel.Situations.PRELAUNCH || v.situation == Vessel.Situations.SPLASHED)
				{
					return;
				}

				if (!isVesselKnown(v))
				{
					return;
				}

				SCANvessel sv = knownVessels[v.id];

				if (sv == null)
				{
					return;
				}

				//Color col;

				double groundWidth = getFOV(sv, body);//, out col);

				if (groundWidth < 1)
				{
					return;
				}

				double surfaceScale = (2 * Math.PI * body.Radius) / 360;

				groundWidth *= surfaceScale;

				SCANuiUtil.drawGroundTrackTris(body, sv.vessel, groundWidth, sv.trackColor);
			}
			else
			{
				double surfaceScale = (2 * Math.PI * body.Radius) / 360;

				for (int j = knownVessels.Count - 1; j >= 0; j--)
				{
					SCANvessel sv = knownVessels.At(j);

					if (sv == null)
					{
						continue;
					}

					if (sv.vessel.mainBody != body)
					{
						continue;
					}

					Vessel.Situations sit = sv.vessel.loaded ? sv.vessel.situation : sv.vessel.protoVessel.situation;

					if (sit == Vessel.Situations.LANDED || sit == Vessel.Situations.PRELAUNCH || sit == Vessel.Situations.SPLASHED)
					{
						continue;
					}

					//Color col;

					double groundWidth = getFOV(sv, body);//, out col);

					if (groundWidth < 1)
					{
						continue;
					}

					groundWidth *= surfaceScale;

					SCANuiUtil.drawGroundTrackTris(body, sv.vessel, groundWidth, sv.trackColor);
				}
			}
		}

		private double getFOV(SCANvessel v, CelestialBody b)//, out Color c)
		{
			//c = palette.xkcd_DarkGreenAlpha;
			double maxFOV = 0;
			double alt = v.vessel.altitude;
			double soi_radius = b.sphereOfInfluence - b.Radius;
			double surfscale = Planetarium.fetch.Home.Radius / b.Radius;
			if (surfscale < 1)
			{
				surfscale = 1;
			}

			surfscale = Math.Sqrt(surfscale);

			for (int j = v.sensors.Count - 1; j >= 0; j--)
			{
				SCANsensor s = v.sensors[j];

				if (alt < s.min_alt)
				{
					continue;
				}

				if (alt > Math.Min(s.max_alt, soi_radius))
				{
					continue;
				}

				double fov = s.fov;
				double ba = Math.Min(s.best_alt, soi_radius);
				if (alt < ba)
				{
					fov = (alt / ba) * fov;
				}

				fov *= surfscale;
				if (fov > 20)
				{
					fov = 20;
				}

				if (fov > maxFOV)
				{
					maxFOV = fov;
				}
			}

			return maxFOV;
		}

		private Color32 getScanTypeColor(SCANtype s)
		{
			float r = 0;
			float g = 0;
			float b = 0;
			float a = 0;

			int count = 0;

			for (int i = 0; i < 31; i++)
			{
				if ((1 << i & (int)s) != 0)
				{
					Color32 c = typeColor(i);

					r += (c.r * c.r);
					g += (c.g * c.g);
					b += (c.b * c.b);
					a += (c.a * c.a);

					count++;
				}
			}

			r /= count;
			g /= count;
			b /= count;
			a /= count;

			Color32 col = new Color32((byte)Mathf.Sqrt(r), (byte)Mathf.Sqrt(g), (byte)Mathf.Sqrt(b), (byte)Mathf.Sqrt(a));

			return col;
		}

		private Color32 typeColor(int i)
		{
			switch (i)
			{
				case 0:
					return SCAN_Settings_Config.Instance.LoResAltimetryTrackColor;
				case 1:
					return SCAN_Settings_Config.Instance.HiResAltimetryTrackColor;
				case 2:
					return SCAN_Settings_Config.Instance.LoResVisualTrackColor;
				case 3:
					return SCAN_Settings_Config.Instance.BiomeTrackColor;
				case 4:
					return SCAN_Settings_Config.Instance.AnomalyTrackColor;
				case 5:
					return SCAN_Settings_Config.Instance.AnomalyDetailTrackColor;
				case 6:
					return SCAN_Settings_Config.Instance.HiResVisualTrackColor;
				case 7:
					return SCAN_Settings_Config.Instance.FuzzyResourceTrackColor;
				case 8:
					return SCAN_Settings_Config.Instance.HiResResourceTrackColor;
				default:

					return palette.XKCD_DarkGreenAlpha;
			}
		}

		private void removeVessel(Vessel v)
		{
			if (isVesselKnown(v))
			{
				unregisterVessel(v);
			}
		}

		private void addVessel(Vessel v)
		{
			foreach (SCANsat.SCAN_PartModules.SCANsat s in v.FindPartModulesImplementing<SCANsat.SCAN_PartModules.SCANsat>())
			{
				if (s.scanningNow)
				{
					registerSensor(v.id, (SCANtype)s.sensorType, s.fov, s.min_alt, s.max_alt, s.best_alt, s.requireLight);
				}
			}
		}

		private void dockingEventCheck(GameEvents.FromToAction<Part, Part> Parts)
		{
			StartCoroutine(dockingCheckCoRoutine(Parts.to.vessel, Parts.from.vessel));
		}

		IEnumerator dockingCheckCoRoutine(Vessel to, Vessel from)
		{
			int timer = 0;

			while (timer < 45)
			{
				timer++;
				yield return null;
			}

			if (from != null)
			{
				removeVessel(from);
			}

			if (to != null)
			{
				removeVessel(to);
			}

			addVessel(FlightGlobals.ActiveVessel);
		}

		private void newVesselCheck(Vessel v)
		{
			if (v.loaded)
			{
				Vessel newVessel = null;

				if (v.Parts.Count > 1)
				{
					newVessel = v;
				}
				else
				{
					newVessel = null;
				}

				Vessel oldVessel = FlightGlobals.ActiveVessel;

				StartCoroutine(newVesselCoRoutine(newVessel, oldVessel));
			}
		}

		IEnumerator newVesselCoRoutine(Vessel newV, Vessel oldV)
		{
			int timer = 0;

			while (timer < 45)
			{
				timer++;
				yield return null;
			}

			if (newV != null)
			{
				removeVessel(newV);
				addVessel(newV);
			}

			if (oldV != null)
			{
				removeVessel(oldV);
				addVessel(oldV);
			}
		}

		private void contractsCheck()
		{
			contractsLoaded = true;
		}

		private void onParamChange(Contract c, ContractParameter p)
		{
			if (c.GetType() == typeof(SurveyContract))
			{
				SurveyContract s = c as SurveyContract;

				CelestialBody b = s.targetBody;

				SCANdata data = getData(b.bodyName);

				if (data == null)
				{
					return;
				}

				data.addSurveyWaypoints(b, s);
			}
			else if (c.GetType() == typeof(SatelliteContract))
			{
				SatelliteContract s = c as SatelliteContract;

				SpecificOrbitParameter orbit = s.GetParameter<SpecificOrbitParameter>();

				if (orbit == null)
				{
					return;
				}

				CelestialBody b = orbit.TargetBody;

				SCANdata data = getData(b.bodyName);

				if (data == null)
				{
					return;
				}

				data.addStationaryWaypoints(b, s);
			}

			if (_bigMap.IsVisible && _bigMap.WaypointToggle)
			{
				_bigMap.RefreshIcons();
			}

			if (_zoomMap.IsVisible && _zoomMap.IconsToggle)
			{
				_zoomMap.RefreshIcons();
			}
		}

		private void SOIChange(GameEvents.HostedFromToAction<Vessel, CelestialBody> VC)
		{
			if (!body_data.Contains(VC.to.bodyName))
			{
				body_data.Add(VC.to.bodyName, new SCANdata(VC.to));
			}
		}

		private string saveResources(SCANresourceGlobal resource)
		{
			List<string> sL = new List<string>();
			for (int j = 0; j < resource.getBodyCount; j++)
			{
				SCANresourceBody bodyRes = resource.getBodyConfig(j);
				if (bodyRes != null)
				{
					string a = string.Format("{0}|{1:F3}|{2:F3}", bodyRes.Index, bodyRes.MinValue, bodyRes.MaxValue);
					sL.Add(a);
				}
			}

			return string.Join(",", sL.ToArray());
		}

		private void loadCustomResourceValues(ConfigNode node)
		{
			SCANresourceGlobal r;

			string resource = node.parse("Resource", "");

			if (string.IsNullOrEmpty(resource))
			{
				return;
			}

			if (masterResourceNodes.Contains(resource))
			{
				r = masterResourceNodes[resource];
			}
			else
			{
				return;
			}

			Color lowColor = node.parse("MinColor", r.DefaultLowColor);
			Color highColor = node.parse("MaxColor", r.DefaultHighColor);
			float transparent = node.parse("Transparency", r.DefaultTrans);

			r.MinColor = lowColor;
			r.MaxColor = highColor;
			r.Transparency = transparent;

			string s = node.parse("MinMaxValues", "");

			if (!string.IsNullOrEmpty(s))
			{
				string[] sA = s.Split(',');
				for (int i = 0; i < sA.Length; i++)
				{
					string[] sB = sA[i].Split('|');
					try
					{
						int j = 0;
						float min = 0;
						float max = 0;
						if (!int.TryParse(sB[0], out j))
						{
							continue;
						}

						CelestialBody b = null;

						try
						{
							b = FlightGlobals.Bodies.FirstOrDefault(a => a.flightGlobalsIndex == j);
						}
						catch (Exception e)
						{
							Log.Error("Error in loading Celestial Body...\n" + e);
							return;
						}

						if (b != null)
						{
							SCANresourceBody res = r.getBodyConfig(b.bodyName, false);
							if (res != null)
							{
								if (!float.TryParse(sB[1], out min))
								{
									min = res.DefaultMinValue;
								}

								if (!float.TryParse(sB[2], out max))
								{
									max = res.DefaultMaxValue;
								}

								res.MinValue = min;
								res.MaxValue = max;
							}
						}
						else
						{
							SCANUtil.SCANlog("No Celestial Body found matching this saved resource value: {0}, skipping...", j);
						}
					}
					catch (Exception e)
					{
						SCANUtil.SCANlog("Something Went Wrong While Loading Custom Resource Settings; Reverting To Default Values: {0}", e);
					}
				}
			}
		}

		private void loadWaypoint(string s, CelestialBody b)
		{
			if (!HighLogic.LoadedSceneIsFlight)
			{
				return;
			}

			StartCoroutine(WaitForWaypoint(s, b));
		}

		private IEnumerator WaitForWaypoint(string s, CelestialBody b)
		{
			while (!FlightGlobals.ready || FlightGlobals.ActiveVessel == null)
			{
				yield return null;
			}

			int timer = 0;

			while (timer < 5)
			{
				timer++;
				yield return null;
			}

			if (!mechjebLoaded || b != FlightGlobals.currentMainBody)
			{
				yield break;
			}

			SCANwaypoint w = null;
			string[] a = s.Split(',');
			double lat = 0;
			double lon = 0;

			if (!double.TryParse(a[0], out lat))
			{
				yield break;
			}

			if (!double.TryParse(a[1], out lon))
			{
				yield break;
			}

			w = new SCANwaypoint(lat, lon, "MechJeb Landing Target");

			MJTargetSet.Invoke(new Vector2d(lon, lat), b);

			SCANdata d = getData(b.bodyName);

			if (d != null)
			{
				d.addToWaypoints(w);
			}
		}

		public class SCANsensor
		{
			public SCANtype sensor;
			public double fov;
			public double min_alt, max_alt, best_alt;

			public Color32 trackColor;

			public bool inRange;
			public bool bestRange;
			public bool inDarkness;

			public bool requireLight;

			public SCANsensor() { }
		}

		public class SCANvessel
		{
			public Guid id;
			public Vessel vessel;

			public Color32 trackColor;

			public List<SCANsensor> sensors = new List<SCANsensor>();

			public CelestialBody body;
			public double latitude, longitude;
			public int frame;
			public double lastUT;
		}

		private void registerSensorTemp(Guid id, SCANtype sensors, double _fov, double _min_alt, double _max_alt, double _best_alt, bool _require_light)
		{
			if (id == null)
			{
				return;
			}

			if (!knownVessels.Contains(id))
			{
				knownVessels[id] = new SCANvessel();
			}

			SCANvessel sv = knownVessels[id];
			sv.id = id;

			if (_max_alt <= 0)
			{
				_min_alt = 5000;
				_max_alt = 500000;
				_best_alt = 200000;
				_fov = 5;
			}

			foreach (SCANtype sensorType in Enum.GetValues(typeof(SCANtype)))
			{
				if (SCANUtil.countBits((int)sensorType) != 1)
				{
					continue;
				}

				if ((sensorType & sensors) == SCANtype.Nothing)
				{
					continue;
				}

				bool flag = true;

				for (int i = sv.sensors.Count - 1; i >= 0; i--)
				{
					SCANsensor sen = sv.sensors[i];

					if (sen.min_alt == _min_alt && sen.max_alt == _max_alt
						&& sen.best_alt == _best_alt && sen.fov == _fov
						&& sen.requireLight == _require_light)
					{
						SCANtype t = sen.sensor | sensorType;

						sv.sensors[i] = new SCANsensor()
						{
							min_alt = _min_alt,
							max_alt = _max_alt,
							best_alt = _best_alt,
							fov = _fov,
							sensor = t,
							requireLight = _require_light,
							trackColor = getScanTypeColor(t),
							//sensorType = (int)sv.sensors[i].sensor,
						};

						flag = false;

						break;
					}
				}

				if (flag)
				{
					sv.sensors.Add(new SCANsensor()
					{
						min_alt = _min_alt,
						max_alt = _max_alt,
						best_alt = _best_alt,
						fov = _fov,
						requireLight = _require_light,
						sensor = sensorType,
						trackColor = getScanTypeColor(sensorType),
					});
				}
			}

			sv.trackColor = palette.combineColors(sv.sensors.Select(s => s.trackColor).ToArray());
		}

		private void finishRegistration(Guid id)
		{
			if (!knownVessels.Contains(id))
			{
				return;
			}

			SCANvessel sv = knownVessels[id];

			try
			{
				sv.vessel = FlightGlobals.Vessels.FirstOrDefault(a => a.id == id);
			}
			catch (Exception e)
			{
				Log.Error("Something went wrong while trying to load this SCANsat vessel; moving on the next vessel... \n" + e);
			}

			if (sv.vessel == null)
			{
				knownVessels.Remove(id);
			}
		}

		internal void registerSensor(Vessel v, SCANtype sensors, double fov, double min_alt, double max_alt, double best_alt, bool _require_light)
		{
			registerSensor(v.id, sensors, fov, min_alt, max_alt, best_alt, _require_light);
			knownVessels[v.id].vessel = v;
			knownVessels[v.id].latitude = SCANUtil.fixLatShift(v.latitude);
			knownVessels[v.id].longitude = SCANUtil.fixLonShift(v.longitude);
		}

		private void registerSensor(Guid id, SCANtype sensors, double _fov, double _min_alt, double _max_alt, double _best_alt, bool _require_light)
		{
			if (id == null)
			{
				return;
			}

			if (!knownVessels.Contains(id))
			{
				knownVessels[id] = new SCANvessel();
			}

			SCANvessel sv = knownVessels[id];
			sv.id = id;
			try
			{
				sv.vessel = FlightGlobals.Vessels.FirstOrDefault(a => a.id == id);
			}
			catch (Exception e)
			{
				Log.Error("Something went wrong while trying to load this SCANsat vessel; moving on the next vessel... \n" + e);
			}
			if (sv.vessel == null)
			{
				knownVessels.Remove(id);
				return;
			}

			if (_max_alt <= 0)
			{
				_min_alt = 5000;
				_max_alt = 500000;
				_best_alt = 200000;
				_fov = 5;
			}

			foreach (SCANtype sensorType in Enum.GetValues(typeof(SCANtype)))
			{
				if (SCANUtil.countBits((int)sensorType) != 1)
				{
					continue;
				}

				if ((sensorType & sensors) == SCANtype.Nothing)
				{
					continue;
				}

				bool flag = true;

				for (int i = sv.sensors.Count - 1; i >= 0; i--)
				{
					SCANsensor sen = sv.sensors[i];

					if (sen.min_alt == _min_alt && sen.max_alt == _max_alt
						&& sen.best_alt == _best_alt && sen.fov == _fov
						&& sen.requireLight == _require_light)
					{
						SCANtype t = sen.sensor | sensorType;

						sv.sensors[i] = new SCANsensor()
						{
							min_alt = _min_alt,
							max_alt = _max_alt,
							best_alt = _best_alt,
							fov = _fov,
							sensor = t,
							requireLight = _require_light,
							trackColor = getScanTypeColor(t),
						};

						flag = false;

						break;
					}
				}

				if (flag)
				{
					sv.sensors.Add(new SCANsensor()
					{
						min_alt = _min_alt,
						max_alt = _max_alt,
						best_alt = _best_alt,
						fov = _fov,
						sensor = sensorType,
						requireLight = _require_light,
						trackColor = getScanTypeColor(sensorType),
					});
				}
			}

			sv.trackColor = palette.combineColors(sv.sensors.Select(s => s.trackColor).ToArray());
		}

		internal void unregisterSensor(Vessel v, SCANtype sensors, double _fov, double _min_alt, double _max_alt, double _best_alt, bool require_light)
		{
			if (!knownVessels.Contains(v.id))
			{
				return;
			}

			SCANvessel sv = knownVessels[v.id];
			sv.id = v.id;
			sv.vessel = v;

			if (_max_alt <= 0)
			{
				_min_alt = 5000;
				_max_alt = 500000;
				_best_alt = 200000;
				_fov = 5;
			}

			foreach (SCANtype sensor in Enum.GetValues(typeof(SCANtype)))
			{
				if ((sensors & sensor) == SCANtype.Nothing)
				{
					continue;
				}

				for (int i = sv.sensors.Count - 1; i >= 0; i--)
				{
					SCANsensor sen = sv.sensors[i];

					if ((sen.sensor & sensor) != SCANtype.Nothing)
					{
						if (sen.min_alt == _min_alt && sen.max_alt == _max_alt
							&& sen.best_alt == _best_alt && sen.fov == _fov
							&& sen.requireLight == require_light)
						{
							SCANtype t = sen.sensor ^ sensor;

							sv.sensors[i] = new SCANsensor()
							{
								min_alt = sen.min_alt,
								max_alt = sen.max_alt,
								best_alt = sen.best_alt,
								fov = sen.fov,
								sensor = t,
								requireLight = require_light,
								trackColor = getScanTypeColor(t),
							};
						}
					}

					if (sv.sensors[i].sensor == SCANtype.Nothing)
					{
						sv.sensors.RemoveAt(i);
					}
				}
			}

			sv.trackColor = palette.combineColors(sv.sensors.Select(s => s.trackColor).ToArray());

			if (sv.sensors.Count == 0)
			{
				knownVessels.Remove(v.id);
				//SCANUtil.SCANdebugLog("Unregister Vessel");
			}
		}

		public void unregisterVessel(Vessel v)
		{
			if (!knownVessels.Contains(v.id))
			{
				return;
			}

			knownVessels.Remove(v.id);
		}

		internal bool isVesselKnown(Guid id, SCANtype sensor)
		{
			if (!knownVessels.Contains(id))
			{
				return false;
			}

			SCANtype all = SCANtype.Nothing;

			for (int i = knownVessels[id].sensors.Count - 1; i >= 0; i--)
			{
				all |= knownVessels[id].sensors[i].sensor;
			}

			return (all & sensor) != SCANtype.Nothing;
		}

		public bool isVesselKnown(Guid id)
		{
			if (!knownVessels.Contains(id))
			{
				return false;
			}

			return knownVessels[id].sensors.Count > 0;
		}

		public bool isVesselKnown(Vessel v)
		{
			if (v.vesselType == VesselType.Debris)
			{
				return false;
			}

			return isVesselKnown(v.id);
		}

		internal SCANsensor getSensorStatus(Vessel v, SCANtype sensor)
		{
			if (!knownVessels.Contains(v.id))
			{
				return null;
			}

			for (int i = knownVessels[v.id].sensors.Count - 1; i >= 0; i--)
			{
				if ((knownVessels[v.id].sensors[i].sensor & sensor) != SCANtype.Nothing)
				{
					return knownVessels[v.id].sensors[i];
				}
			}

			return null;
		}

		internal SCANtype activeSensorsOnVessel(Guid id, bool daylight)
		{
			if (!knownVessels.Contains(id))
			{
				return SCANtype.Nothing;
			}

			SCANtype sensors = SCANtype.Nothing;

			for (int i = knownVessels[id].sensors.Count - 1; i >= 0; i--)
			{
				if (daylight)
				{
					SCANsensor sen = knownVessels[id].sensors[i];

					if (sen.requireLight)
					{
						Vessel ves = knownVessels[id].vessel;
						if (SCANUtil.InDarkness(ves.orbit.getPositionAtUT(Planetarium.GetUniversalTime()), ves.mainBody.position, SCANUtil.LocalSun(ves.mainBody).position))
						{
							continue;
						}
					}
				}

				sensors |= knownVessels[id].sensors[i].sensor;
			}
			return sensors;
		}

		//private System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
		private int i = 0;
		private static int last_scan_frame;
		private static float last_scan_time;
		private static double scan_UT;
		private int activeSensors, activeVessels;
		private static int currentActiveSensor, currentActiveVessel;
		private void scanFromAllVessels()
		{
			if (Time.realtimeSinceStartup - last_scan_time < 1 && Time.realtimeSinceStartup > last_scan_time)
			{
				return;
			}

			if (last_scan_frame == Time.frameCount)
			{
				return;
			}

			last_scan_frame = Time.frameCount;
			last_scan_time = Time.realtimeSinceStartup;
			scan_UT = Planetarium.GetUniversalTime();
			currentActiveSensor = 0;
			currentActiveVessel = 0;
			actualPasses = 0;
			if (body_data.Count > 0)
			{
				body_data.At(i).updateCoverage();    //Update coverage for planets one at a time, rather than all together
				i++;
				if (i >= body_data.Count)
				{
					i = 0;
				}
			}
			int l = knownVessels.Count;

			SCANvessel vessel = null;
			SCANdata data = null;

			//watch.Reset();
			//watch.Start();

			for (int j = 0; j < l; j++)
			{
				//Profiler.BeginSample("SCAN Vessel scan");
				vessel = knownVessels.At(j);

				data = SCANUtil.getData(vessel.vessel.mainBody);

				if (data == null)
				{
					continue;
				}

				if (!data.Disabled)
				{
					if (isVesselKnown(vessel.vessel))
					{
						doScanPass(vessel, vessel.vessel, data, scan_UT, scan_UT, vessel.lastUT);
						++currentActiveVessel;
						currentActiveSensor += knownVessels[vessel.vessel.id].sensors.Count;
					}
				}

				vessel.body = vessel.vessel.mainBody;
				vessel.frame = Time.frameCount;
				vessel.lastUT = scan_UT;
				vessel.latitude = SCANUtil.fixLatShift(vessel.vessel.latitude);
				vessel.longitude = SCANUtil.fixLonShift(vessel.vessel.longitude);
				//Profiler.EndSample();
			}

			//watch.Stop();

			//SCANUtil.SCANlog("SCAN Time: {0} - Passes: {1}", watch.ElapsedMilliseconds.ToString(), actualPasses.ToString());
			activeVessels = currentActiveVessel;
			activeSensors = currentActiveSensor;
		}

		private int actualPasses;
		private static Queue<double> scanQueue;
		private void doScanPass(SCANvessel vessel, Vessel v, SCANdata data, double UT, double startUT, double lastUT)
		{
			//SCANUtil.SCANlog("Start New Scan Pass");
			CelestialBody sun = SCANUtil.LocalSun(v.mainBody);
			double soi_radius = v.mainBody.sphereOfInfluence - v.mainBody.Radius;
			double alt = v.altitude;
			double llat = SCANUtil.fixLat(v.latitude);
			double llon = SCANUtil.fixLon(v.longitude);
			int lat = (int)Math.Floor(llat);
			int lon = (int)Math.Floor(llon);
			double res = 0;
			Orbit o = v.orbit;
			bool uncovered;
			bool darkness = SCANUtil.InDarkness(o.getPositionAtUT(UT), v.mainBody.position, sun.position);
			short sensorType;

			double surfscale = Planetarium.fetch.Home.Radius / v.mainBody.Radius;

			if (surfscale < 1)
			{
				surfscale = 1;
			}

			surfscale = Math.Sqrt(surfscale);

			if (scanQueue == null)
			{
				scanQueue = new Queue<double>();
			}

			if (scanQueue.Count != 0)
			{
				scanQueue.Clear();
			}

		loop: // don't look at me like that, I just unrolled the recursion
			if (res > 0)
			{
				if (double.IsNaN(UT))
				{
					goto dequeue;
				}

				if (double.IsNaN(o.getObtAtUT(UT)))
				{
					goto dequeue;
				}

				Vector3d pos = o.getPositionAtUT(UT);
				double rotation = 0;

				if (v.mainBody.rotates)
				{
					rotation = (360 * ((UT - scan_UT) / v.mainBody.rotationPeriod)) % 360;
				}

				alt = v.mainBody.GetAltitude(pos);
				lat = SCANUtil.fixLatInt(v.mainBody.GetLatitude(pos));
				lon = SCANUtil.fixLonInt(v.mainBody.GetLongitude(pos) - rotation);
				darkness = SCANUtil.InDarkness(pos, v.mainBody.position, sun.position);

				if (alt < 0)
				{
					alt = 0;
				}
			}
			else
			{
				alt = v.radarAltitude;
				if (alt < 0)
				{
					alt = v.altitude;
				}
			}

			if (Math.Abs((lat * 1d) - llat) < 1 && Math.Abs((lon * 1d) - llon) < 1 && res > 0)
			{
				goto dequeue;
			}

			actualPasses++;

			uncovered = res <= 0;
			//SCANUtil.SCANlog("Begin scan pass: {0}", vessel.sensors.Count);
			for (int j = vessel.sensors.Count - 1; j >= 0; j--)
			{
				SCANsensor sensor = vessel.sensors[j];

				sensorType = (short)sensor.sensor;

				if (res <= 0)
				{
					if (data.getCoverage(sensor.sensor) > 0)
					{
						uncovered = false;
					}
				}

				sensor.inDarkness = sensor.requireLight;

				if (sensor.requireLight && darkness)
				{
					//SCANUtil.SCANlog("Sensor {0} fail light check...", sensorType);
					continue;
				}

				sensor.inDarkness = false;
				sensor.inRange = false;
				sensor.bestRange = false;

				double ma = Math.Max(0, Math.Min(sensor.min_alt, soi_radius - 100000));

				if (alt < ma)
				{
					continue;
				}

				if (alt > Math.Min(sensor.max_alt, soi_radius))
				{
					continue;
				}

				//Profiler.BeginSample("SCAN Sensor Check");

				sensor.inRange = true;

				double fov = sensor.fov;
				double ba = Math.Min(sensor.best_alt, soi_radius);

				if (alt < ba)
				{
					fov = (alt / ba) * fov;
				}
				else
				{
					sensor.bestRange = true;
				}

				fov *= surfscale;

				if (fov > 20)
				{
					fov = 20;
				}

				int f = (int)Math.Truncate(fov);
				int f1 = f + (int)Math.Round(fov - f);

				int w = f;
				double fovW = fov;

				if (lat < 180)
				{
					fovW = fov * (1 / SCANUtil.cosLookUp[lat]);

					if (fovW > 120)
					{
						fovW = 120;
					}

					w = (int)Math.Truncate(fovW);
				}

				int w1 = w + (int)Math.Round(fovW - w);

				for (int x = -w; x <= w1; ++x)
				{
					int clampLon = lon + x;

					if (clampLon > 359 || clampLon < 0)
					{
						clampLon = (clampLon + 360) % 360;
					}

					for (int y = -f; y <= f1; ++y)
					{
						int clampLat = lat + y;
						int clampLonAgain = clampLon;

						if (clampLat > 179)
						{
							clampLat = 359 - clampLat;
							clampLonAgain += 180;

							clampLat = (clampLat + 180) % 180;

							if (clampLonAgain > 359 || clampLonAgain < 0)
							{
								clampLonAgain = (clampLonAgain + 360) % 360;
							}
						}
						else if (clampLat < 0)
						{
							clampLat = 0 - clampLat;
							clampLonAgain += 180;

							clampLat = (clampLat + 180) % 180;

							if (clampLonAgain > 359 || clampLonAgain < 0)
							{
								clampLonAgain = (clampLonAgain + 360) % 360;
							}
						}

						data.coverage[clampLonAgain, clampLat] |= sensorType;
					}
				}
				//Profiler.EndSample();
			}
			if (uncovered)
			{
				return;
			}

			if (vessel.lastUT <= 0)
			{
				return;
			}

			if (vessel.frame <= 0)
			{
				return;
			}

			if (v.LandedOrSplashed)
			{
				return;
			}

			if (res >= SCAN_Settings_Config.Instance.TimeWarpResolution)
			{
				goto dequeue;
			}

			if (startUT > UT)
			{
				scanQueue.Enqueue((startUT + UT) / 2);
				scanQueue.Enqueue(startUT);
				scanQueue.Enqueue(UT);
				scanQueue.Enqueue(lat);
				scanQueue.Enqueue(lon);
				scanQueue.Enqueue(res + 1);
			}

			startUT = UT;
			UT = (lastUT + UT) / 2;
			llat = lat;
			llon = lon;
			res = res + 1;
			goto loop;

		dequeue:
			if (scanQueue.Count <= 0)
			{
				return;
			}

			UT = scanQueue.Dequeue();
			startUT = scanQueue.Dequeue();
			lastUT = scanQueue.Dequeue();
			llat = scanQueue.Dequeue();
			llon = scanQueue.Dequeue();
			res = scanQueue.Dequeue();
			goto loop;
		}

	}
}
