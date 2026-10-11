// SCANsat map compositing on the GPU (all modes).
//
// A full-screen Blit shader that reproduces SCANmap.getPartialMap on the GPU. Originally the
// Visual mode only (so SCANsat no longer needs a CPU-readable copy of each body's ScaledSpace
// textures - the RSS RAM hog); now branches on _MapMode to also draw Altimetry / Slope / Biome,
// plus the resource overlay. The point for the non-Visual modes is latency/UX (instant recolour
// on palette / clamp / setting changes, no per-frame CPU scanline loop), NOT RAM.
//
// Data flow: the C# side (SCANmap.tryRenderGPU) uploads the mode's already-computed CPU data as a
// texture - elevation (big_heightmap), biome index (biome_indexmap), resource abundance
// (resourceCache) - plus 1-D palette LUTs baked by SCANcolorUtil.heightToColor (so map + legend
// stay pixel-consistent). The shader does the coverage mask + LUT/colorize + overlay + terminator.
//
// Coverage stencil is now the raw 16-bit SCANdata.coverage, packed R=low byte / G=high byte.
//
// NOTE: rebuild the scan_shaders asset bundles (SCANsat -> Build All Bundles, Unity 2019.4.18f1)
// after any change here; the C# side sets every uniform below each frame, and a uniform the loaded
// bundle does not know is silently ignored, so DLL and bundle can be updated independently.
// Data-texture ORIENTATION (the geo UV mappings below) is the main thing to verify in-game - if a
// mode is mirrored/flipped, adjust geoUVOf / dataUVAt (same class of fix as Visual's
// `fLon = 1 - fLon`).
Shader "Hidden/SCANsat/VisualComposite"
{
	Properties
	{
		_ScaledColor ("Scaled Color", 2D) = "gray" {}
		_ScaledNormal ("Scaled Normal", 2D) = "bump" {}
		_NormalYChannel ("Normal Y channel (0 = blue, 1 = green)", Float) = 0
		_GasRamp ("Gas Giant Colour Ramp", 2D) = "gray" {}
		_GasGiant ("Scaled Color Is A Cloud Pattern", Float) = 0
		_CoverageFlags ("Coverage Flags", 2D) = "black" {}
		_ElevationTex ("Elevation", 2D) = "black" {}
		_BiomeIndexTex ("Biome Index", 2D) = "black" {}
		_ResourceTex ("Resource Abundance", 2D) = "black" {}
		_PaletteLUT ("Palette LUT", 2D) = "white" {}
		// Cosmetic sweep reveal. Defaults render the whole map (SweepY >= 1) so a fresh
		// Material is fully revealed even if the C# side never sets these (bundle/DLL skew).
		_SweepY ("Sweep Reveal", Float) = 1
		_SweepBand ("Sweep Band (rows)", Float) = 2
		_MapMode ("Map Mode", Float) = 3
		_RedlineColor ("Redline", Color) = (1,0,0,1)
		// Defaults that keep an unset material drawing the whole map the classic way.
		_RowMin ("Row Min", Float) = 0
		_RowMax ("Row Max", Float) = 1000000
		_HasSource ("Has Visual Source", Float) = 1
		_HasElevation ("Has Elevation", Float) = 1
		_OutputAlpha ("Output Alpha", Float) = 1
		_ResGreyBlend ("Resource Below-Range Or Empty Grey Blend", Float) = 0.3
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag
			// tex2Dgrad / tex2Dlod below: shader model 3.0. Without this Unity assumes 2.5
			// (D3D11 ps_4_0_level_9_3), which has neither.
			#pragma target 3.0
			#include "UnityCG.cginc"

			sampler2D _ScaledColor;
			sampler2D _ScaledNormal;
			sampler2D _GasRamp;         // 1-D (2xN) colour gradient a gas giant's cloud pattern is looked up in
			sampler2D _CoverageFlags;   // 360x180, point-sampled. R=low byte, G=high byte of SCANdata.coverage Int16
			sampler2D _ElevationTex;    // mapW x mapH, R = raw elevation (metres); from big_heightmap. Addressed per _PixelSpace
			sampler2D _BiomeIndexTex;   // mapW x mapH, R = biome index fraction [0,1]; from biome_indexmap. Addressed per _PixelSpace
			sampler2D _ResourceTex;     // resW x resH, R = abundance fraction [0,1]; from resourceCache. Addressed per _PixelSpace
			sampler2D _PaletteLUT;      // 1-D (Nx1) elevation colour ramp baked from heightToColor (colour)
			sampler2D _PaletteGreyLUT;  // 1-D grey ramp for LoRes-only altimetry (nowColor=false)
			sampler2D _BiomeLUT;        // 1-D (biomeCount x1) stock biome mapColors, indexed by biome fraction

			// Projection / map framing (mirror SCANmap fields)
			float _MapWidth;
			float _MapHeight;
			float _MapScale;
			float _LonOffset;
			float _LatOffset;
			float _Projection;      // 0 Rectangular, 1 KavrayskiyVII, 2 Polar, 3 Orthographic
			float _CenteredLon;
			float _CenteredLat;

			float _MapMode;         // 0 Altimetry, 1 Slope, 2 Biome, 3 Visual (mapType enum)

			// Visual layer
			float _ColorMode;       // 1 colour, 0 grayscale
			float _HasNormal;       // 1 if a normal map is bound
			float _NormalYChannel;  // 1: Y in green (DXT5nm, BC5); 0: Y in blue (uncompressed RGB normals)
			float _GasGiant;        // 1: _ScaledColor is a cloud pattern, not a colour map (see gasColor)

			// Altimetry / Slope
			float _TerrainMin;      // LUT domain min (metres)
			float _TerrainRange;    // LUT domain range (metres)
			float _SlopeCutoff;
			float4 _SlopeLoColorOne;
			float4 _SlopeHiColorOne;
			float4 _SlopeLoColorTwo;
			float4 _SlopeHiColorTwo;

			// Biome
			float4 _LowBiomeColor;
			float4 _HighBiomeColor;
			float _BiomeTransparency;
			float _BiomeBorder;     // 1 draw white biome borders
			float _StockBiomes;     // 1 use stock biome mapColors (_BiomeLUT), 0 low/high gradient
			float _BiomeCount;      // number of biomes (for the _BiomeLUT index)
			float _HasElevation;    // 1 when the body has terrain to sample for the underlay (the map's pqs flag); 0 keeps it grey

			// Resource overlay
			float _ResourceActive;  // 1 apply resource overlay on top of the base colour
			float4 _ResMinColor;
			float4 _ResMaxColor;
			float _ResMinRange;     // 0..100
			float _ResMaxRange;     // 0..100
			float _ResTransparency; // 0..1 (config /100)

			// Terminator
			float _Terminator;      // 1 on
			float _SunLonCenter;
			float _SunLatCenter;
			float _Gamma;

			float4 _UnscannedColor;
			float4 _ClearColor;
			float4 _GreyColor;      // palette.Grey (for below-range or empty resource)

			// Cosmetic sweep reveal (matches the CPU modes' line-by-line render look).
			float _SweepY;                 // revealed fraction in texture-row space (uv.y). >=1 = done, no redline.
			float _SweepBand;              // redline thickness in rows
			float4 _RedlineColor;          // the advancing scanline colour

			// Data-texture addressing, one rule for the three data textures (elevation, biome index,
			// resource): 0 = geographic, the texture is an equirect grid of the whole globe, read at the
			// pixel's unprojected lon/lat (the big map, the planet overlay - their caches survive a
			// projection change or a re-centring); 1 = pixel space, the texture is the map's own window,
			// filled per rendered pixel and read at the pixel uv (the zoom map, RPM, the small map). See
			// dataUVAt.
			float _PixelSpace;

			// Rows outside [_RowMin, _RowMax] (map rows, inclusive) draw _ClearColor: RPM props reserve
			// screen rows this way (SCANmap.startLine/stopLine, the CPU path's mapHidden).
			float _RowMin;
			float _RowMax;

			float _Grid;        // 1: the small map's dotted 30-degree graticule
			float _HasSource;   // Visual: 0 when there is no colour texture for the body -> whole map _UnscannedColor (the old CPU fallback)
			float _BaseNone;    // 1: no base layer at all, every pixel starts as _UnscannedColor (the resource-only planet overlay)
			float _PlanetUV;    // 1: columns in the planet's ScaledSpace UV layout (u = 0 at 90 E, longitude decreasing) - the planet overlays
			float _OutputAlpha; // final multiplier on the whole colour (the terrain planet overlay is 90 percent)
			float _ResGreyBlend;   // resource overlay: blend toward grey for below-range or empty cells (resourceToColor32's Transparency argument)
			float _NoData;      // 1: no terrain (Altimetry/Slope on a body without PQS) or no biome map (Biome): black-white static like the CPU renderers
			float _NoiseSeed;   // re-rolled per pass by the C# side, so the static changes between passes rather than every frame

			static const float SCAN_PI = 3.14159265358979;
			static const float DEG2RAD = 0.0174532925199433;
			static const float RAD2DEG = 57.2957795130823;

			// SCANmap.unprojectLongitude/unprojectLatitude prologue + normalization.
			void normalizeRaw(inout float lon, inout float lat)
			{
				if (lat > 90.0) { lat = 180.0 - lat; lon += 180.0; }
				else if (lat < -90.0) { lat = -180.0 - lat; lon += 180.0; }
				lon = fmod(lon + 3600.0 + 180.0, 360.0) - 180.0;
				lat = fmod(lat + 1800.0 + 90.0, 180.0) - 90.0;
			}

			// Pixel raw coords -> geographic lon/lat (degrees). Returns 0.0 for out-of-disc pixels, else 1.0.
			// A float, not a bool: FXC folds a bool return across the projection branches into a movc that
			// merges a bool with a float, and HLSLcc's Vulkan cross-compile fails on that ("ERROR missing
			// components in GetBitcastOp()"); which way FXC folds it depends on unrelated code below.
			float unproject(float lonRaw, float latRaw, out float lon, out float lat)
			{
				lon = 0.0;   // assigned again below; keeps FXC from flagging the outs as maybe-uninitialised
				lat = 0.0;
				normalizeRaw(lonRaw, latRaw);
				lon = lonRaw;
				lat = latRaw;

				if (_Projection < 0.5)               // Rectangular
				{
					// identity
				}
				else if (_Projection < 1.5)          // KavrayskiyVII (lat unchanged)
				{
					float lonr = DEG2RAD * lonRaw;
					float latr = DEG2RAD * latRaw;
					lon = RAD2DEG * (lonr / sqrt(SCAN_PI * SCAN_PI / 3.0 - latr * latr) * 2.0 * SCAN_PI / 3.0);
					lat = latRaw;
				}
				else if (_Projection < 2.5)          // Polar
				{
					float lonr = DEG2RAD * lonRaw;
					float latr = DEG2RAD * latRaw;
					float lat0 = SCAN_PI / 2.0;
					if (lonr < 0.0) { lonr += SCAN_PI / 2.0; lat0 = -SCAN_PI / 2.0; }
					else { lonr -= SCAN_PI / 2.0; }
					lonr /= 1.3;
					latr /= 1.3;
					float p = sqrt(lonr * lonr + latr * latr);
					float c = asin(p);
					float gl = atan2(lonr * sin(c), p * cos(lat0) * cos(c) - latr * sin(lat0) * sin(c));
					gl = fmod(RAD2DEG * gl + 180.0, 360.0) - 180.0;
					if (gl <= -180.0) gl = -180.0;
					lon = gl;
					lat = RAD2DEG * asin(cos(c) * sin(lat0) + (latr * sin(c) * cos(lat0)) / p);
				}
				else                                  // Orthographic
				{
					float lonr = DEG2RAD * lonRaw;
					float latr = DEG2RAD * latRaw;
					float centerLon = DEG2RAD * _CenteredLon;
					float centerLat = DEG2RAD * _CenteredLat;
					float p2 = sqrt(lonr * lonr + latr * latr);
					float c2 = asin(p2 / 1.5);
					if (cos(c2) < 0.0) return 0.0;   // back hemisphere
					float gl = centerLon + atan2(lonr * sin(c2), p2 * cos(c2) * cos(centerLat) - latr * sin(c2) * sin(centerLat));
					gl = fmod(RAD2DEG * gl + 180.0, 360.0) - 180.0;
					if (gl <= -180.0) gl += 360.0;
					lon = gl;
					lat = RAD2DEG * asin(cos(c2) * sin(centerLat) + (latr * sin(c2) * cos(centerLat)) / p2);
				}

				// Written so a NaN fails too: FXC compiles under fast-math and may drop isnan(), and every
				// comparison with NaN is false, so "NaN or out of range -> false" has to be one positive test.
				return (lat >= -90.0 && lat <= 90.0 && lon >= -180.0 && lon <= 180.0) ? 1.0 : 0.0;
			}

			// SCANcolorUtil.ConvertToGrayscale weights.
			float3 grayscale(float3 c)
			{
				float l = saturate(c.r * 0.2126 + c.g * 0.7152 + c.b * 0.0722);
				return float3(l, l, l);
			}

			float3 rgb2hsl(float3 c)
			{
				float mn = min(min(c.r, c.g), c.b);
				float mx = max(max(c.r, c.g), c.b);
				float l = (mn + mx) * 0.5;
				float h = 0.0, s = 0.0;
				if (mn != mx)
				{
					float d = mx - mn;
					s = l < 0.5 ? d / (mx + mn) : d / (2.0 - mx - mn);
					if (mx == c.r)      h = (c.g - c.b) / d + (c.g < c.b ? 6.0 : 0.0);
					else if (mx == c.g) h = (c.b - c.r) / d + 2.0;
					else                h = (c.r - c.g) / d + 4.0;
					h /= 6.0;
				}
				return float3(h, s, l);
			}

			float hue2rgb(float p, float q, float t)
			{
				if (t < 0.0) t += 1.0;
				if (t > 1.0) t -= 1.0;
				if (t < 1.0 / 6.0) return p + (q - p) * 6.0 * t;
				if (t < 1.0 / 2.0) return q;
				if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
				return p;
			}

			float3 hsl2rgb(float3 hsl)
			{
				if (hsl.y == 0.0) return float3(hsl.z, hsl.z, hsl.z);
				float q = hsl.z < 0.5 ? hsl.z * (1.0 + hsl.y) : hsl.z + hsl.y - hsl.z * hsl.y;
				float p = 2.0 * hsl.z - q;
				return float3(hue2rgb(p, q, hsl.x + 1.0 / 3.0), hue2rgb(p, q, hsl.x), hue2rgb(p, q, hsl.x - 1.0 / 3.0));
			}

			// A gas giant has no colour map: KSP's Terrain/Gas Giant builds the body's colour by looking a
			// cloud PATTERN up in a 1-D gradient (Jool's _CloudColorMap is a 2x4096 ramp of greens), and
			// the pattern is a mask with the cloud layers in red and green and nothing in blue - drawn as a
			// colour it is pure red, which is what Jool's Visual map used to be. _ScaledColor holds that
			// pattern for such a body (SCANcontroller.GetVisualMapTexturesForBody) and the lookup happens
			// here. Explicit LOD: the ramp is a gradient, its mip chain is of no use, and this is called
			// from inside the coverage branches where derivatives are not to be relied on.
			float3 gasColor(float3 pattern)
			{
				float3 c = pattern;
				if (_GasGiant > 0.5)
					c = tex2Dlod(_GasRamp, float4(0.5, pattern.r, 0.0, 0.0)).rgb;
				return c;
			}

			// Which channel of the normal map holds Y depends on its format (DXT5nm and BC5 keep it in
			// green, uncompressed RGB in blue); SCANtextures decides and passes _NormalYChannel.
			float normalY(float4 n)
			{
				return _NormalYChannel > 0.5 ? n.g : n.b;
			}

			// Modulate lightness by the normal map's Y channel (the CPU renderer used to do this per pixel).
			float3 normalSoftLight(float3 rgb, float lumOver)
			{
				float3 hsl = rgb2hsl(rgb);
				float lum = hsl.z;
				float opacity = 0.8;
				if (lum > 0.5)
					lum = opacity * (1.0 - (1.0 - (2.0 * (lumOver - 0.5))) * (1.0 - lum)) + (1.0 - opacity) * lum;
				else
					lum = opacity * (2.0 * lumOver * lum) + (1.0 - opacity) * lum;
				return hsl2rgb(float3(hsl.x, hsl.y, lum));
			}

			// Decode the packed 16-bit coverage cell (R=low byte, G=high byte) and test a bit.
			float decodeCoverage(float2 geoStencilUV)
			{
				float2 f = tex2D(_CoverageFlags, geoStencilUV).rg;
				return floor(f.r * 255.0 + 0.5) + 256.0 * floor(f.g * 255.0 + 0.5);
			}
			bool covHas(float cov, float bit)  // bit = SCANtype exponent (AltLo=0,AltHi=1,VisLo=2,Biome=3,VisHi=6,ResLo=7,ResHi=8)
			{
				return fmod(floor(cov / exp2(bit)), 2.0) >= 0.5;
			}

			// Per-pixel black-white static for bodies with no data (the CPU renderers drew
			// palette.lerp(Black, White, Random.value) there; the seed changes per pass like their re-roll).
			float4 staticNoise(float2 px)
			{
				float2 p = frac((px + _NoiseSeed * 17.0) * float2(0.1031, 0.1030));
				p += dot(p, p.yx + 33.33);
				float n = frac((p.x + p.y) * p.x);
				return float4(n, n, n, 1.0);
			}

			// Screen pixel -> raw map coordinate (degrees, before unprojection). A planet overlay texture is
			// laid out the way the body's ScaledSpace UVs read it: u = 0 at 90 degrees east and longitude
			// decreasing with u - the inverse of Visual's fLon, and what the old CPU overlays' fixLon did per
			// column.
			float rawLonAt(float u)
			{
				return _PlanetUV > 0.5 ? 90.0 - (u * _MapWidth / _MapScale) : (u * _MapWidth / _MapScale) - 180.0 + _LonOffset;
			}

			float rawLatAt(float v)
			{
				return (v * _MapHeight / _MapScale) - 90.0 + _LatOffset;
			}

			// Equirect uv of a lon/lat, for a geographic data texture.
			// NOTE verify orientation in-game; mirror like Visual's fLon if a mode comes out flipped.
			float2 geoUVOf(float lon, float lat)
			{
				return float2(saturate((lon + 180.0) / 360.0), saturate((lat + 90.0) / 180.0));
			}

			// The one addressing rule for the data textures: where screen pixel p reads them. Pixel space
			// reads at p itself; geographic reads at the equirect uv of p's unprojected lon/lat. A
			// neighbour is always the neighbouring SCREEN pixel put through this same rule, so a biome
			// border stays one screen pixel wide under every projection. Returns 0.0 when p is off the map
			// (outside the projection's disc), 1.0 otherwise - a float flag, as unproject's.
			float dataUVAt(float2 p, out float2 uv)
			{
				uv = p;
				if (_PixelSpace > 0.5)
					return 1.0;
				float lon, lat;
				float onMap = unproject(rawLonAt(p.x), rawLatAt(p.y), lon, lat);
				uv = geoUVOf(lon, lat);
				return onMap;
			}

			fixed4 frag(v2f_img i) : SV_Target
			{
				// Cosmetic sweep reveal first, before any work: rows ahead of the scanline are discarded (the
				// RenderTexture keeps the previous pass there, as the CPU path overwrote its Texture2D row by
				// row without clearing it first) and the frontier is a redline, full width like the CPU's.
				if (_SweepY < 1.0)
				{
					if (i.uv.y > _SweepY)
						discard;
					if (i.uv.y > _SweepY - _SweepBand / _MapHeight)
						return _RedlineColor;
				}

				float vy = i.uv.y;
				float2 pixUV = float2(i.uv.x, vy);
				float2 pix = floor(pixUV * float2(_MapWidth, _MapHeight));

				// Row window (RPM reserved rows) = the CPU path's mapHidden -> palette.Clear.
				if (pix.y < _RowMin || pix.y > _RowMax)
					return _ClearColor;

				// Pixel -> raw coord, then unproject (rawLonAt: a planet overlay's columns run the planet's way).
				float lon, lat;
				if (unproject(rawLonAt(i.uv.x), rawLatAt(vy), lon, lat) < 0.5)
					return _ClearColor;

				// Coverage stencil lookup (SCANUtil.icLON/icLAT -> Coverage[ilon,ilat]).
				float ilon = fmod(floor(lon + 540.0), 360.0);
				float ilat = fmod(floor(lat + 270.0), 180.0);
				float cov = decodeCoverage(float2((ilon + 0.5) / 360.0, (ilat + 0.5) / 180.0));

				// Where this pixel reads the three data textures (dataUVAt's rule, with the lon/lat in hand).
				float2 dUV = _PixelSpace > 0.5 ? pixUV : geoUVOf(lon, lat);

				// Base ScaledSpace UV for Visual (SCANmap.cs:1183-1199).
				float fLat = saturate((lat + 90.0) / 180.0);
				float fLon = (lon + 270.0) / 360.0;
				if (fLon < 0.0) fLon += 1.0;
				if (fLon > 1.0) fLon -= 1.0;
				fLon = saturate(1.0 - fLon);

				// Explicit gradients for the two Visual samples. fLon wraps at 90 E, so on the pixel quad
				// straddling the wrap ddx(fLon) is about +-1 and the hardware picks the coarsest mip for
				// that quad - a 1-2 px blurred vertical seam at 90 E on any mipmapped source (a body with
				// no SCANSAT_BODY_TEXTURES node, drawn from its ScaledSpace textures; the cfg path loads a
				// single mip level, so nothing on RSS shows it). Fold the +-1 step back out of the
				// derivative, and take it HERE - once, before the coverage branches - so the samples do not
				// rely on helper-lane derivatives inside divergent flow.
				float2 visUV = float2(fLon, fLat);
				float2 visDX = ddx(visUV);
				float2 visDY = ddy(visUV);
				visDX.x -= floor(visDX.x + 0.5);   // floor(x+0.5), not round(): plainest thing FXC/HLSLcc can fold
				visDY.x -= floor(visDY.x + 0.5);

				float4 col = _UnscannedColor;

				// The CPU small map drew these dots on unfilled areas of each map
				if (_Grid > 0.5 && _NoData < 0.5)
				{
					// A white dot every 3rd pixel along each 30-degree row and column, before the terminator,
					// as there. (The big map's graticule is a separate texture drawn point by point: SCANmap.renderGrid.)
					if ((fmod(pix.y, 30.0) < 0.5 && fmod(pix.x, 3.0) < 0.5) || (fmod(pix.x, 30.0) < 0.5 && fmod(pix.y, 3.0) < 0.5))
						col = float4(1.0, 1.0, 1.0, 1.0);
				}

				if (_BaseNone > 0.5)            // ---- no base layer: the resource-only planet overlay ----
				{
					// col stays _UnscannedColor (clear); the resource pass below draws on it
				}
				else if (_NoData > 0.5)  // Skip map logic if there is no data for it
				{
					col = staticNoise(pix);
				}
				else if (_MapMode < 0.5)        // ---- Altimetry ----
				{
					if (covHas(cov, 0.0) || covHas(cov, 1.0))
					{
						float elev = tex2D(_ElevationTex, dUV).r;
						float t = _TerrainRange > 0.0 ? saturate((elev - _TerrainMin) / _TerrainRange) : 0.5;
						// HiRes -> colour ramp; LoRes-only -> grey ramp (matches CPU nowColor).
						col = covHas(cov, 1.0) ? tex2D(_PaletteLUT, float2(t, 0.5)) : tex2D(_PaletteGreyLUT, float2(t, 0.5));
						col.a = 1.0;
					}
				}
				else if (_MapMode < 1.5)        // ---- Slope ----
				{
					if (covHas(cov, 0.0) || covHas(cov, 1.0))
					{
						// The elevation gradient over ONE MAP TEXEL OF ARC in every direction, at every
						// latitude. A texel column is a meridian on a geographic texture and on a rectangular
						// window (RPM), so along a row the texels close up by cos(lat) toward the poles: the
						// east and west samples are taken 1/cos(lat) texels out, which keeps them one texel of
						// ground apart, and nothing is divided by cos(lat). (The old lonRun did divide by it,
						// and turned the polar rows into cliffs: at 89 degrees on a 1440-wide map of Earth the
						// neighbouring texels are 500 m apart, and real relief over 500 m is steep at the
						// map's scale.) The zoom map is Orthographic in pixel space, where a pixel step is one
						// ground step already. Central differences and the gradient magnitude, so a slope
						// reads the same whichever way it faces, in the CPU renderer's unit: metres of rise
						// per degree of arc, over 1000, clamped to 0..2 (getPartialMap's |dh| / (1000 / mapscale);
						// a texel is 1/_MapScale degrees whatever the map's size or zoom, and the body's radius
						// never enters). A neighbour at exactly 0 was never sampled (uncovered cells stay 0 on
						// the PQS path; a true 0 m is stored as -0.001) and drops out, leaving a one-sided
						// difference, so a coverage edge is not a cliff down to 0 m. The widened step wraps at
						// the dateline on a geographic texture (frac) and clamps at a window's edge (the
						// texture's wrap mode); it is capped at half the texture, so the last texel beside a
						// pole, where 1/cos(lat) explodes, reads its north-south slope only.
						float2 tx = float2(1.0 / _MapWidth, 1.0 / _MapHeight);
						float meridians = (_PixelSpace < 0.5 || _Projection < 0.5) ? 1.0 : 0.0;
						float su = meridians > 0.5 ? min(tx.x / max(cos(lat * DEG2RAD), 0.000001), 0.5) : tx.x;
						float uE = dUV.x + su;
						float uW = dUV.x - su;
						if (_PixelSpace < 0.5)
						{
							uE = frac(uE);
							uW = frac(uW);
						}
						float e  = tex2D(_ElevationTex, dUV).r;
						float eE = tex2D(_ElevationTex, float2(uE, dUV.y)).r;
						float eW = tex2D(_ElevationTex, float2(uW, dUV.y)).r;
						float eN = tex2D(_ElevationTex, dUV + float2(0, tx.y)).r;
						float eS = tex2D(_ElevationTex, dUV - float2(0, tx.y)).r;
						float hasE = abs(eE) > 0.0 ? 1.0 : 0.0;
						float hasW = abs(eW) > 0.0 ? 1.0 : 0.0;
						float hasN = abs(eN) > 0.0 ? 1.0 : 0.0;
						float hasS = abs(eS) > 0.0 ? 1.0 : 0.0;
						float dx = hasE * hasW > 0.5 ? (eE - eW) * 0.5 : (hasE > 0.5 ? eE - e : (hasW > 0.5 ? e - eW : 0.0));
						float dy = hasN * hasS > 0.5 ? (eN - eS) * 0.5 : (hasN > 0.5 ? eN - e : (hasS > 0.5 ? e - eS : 0.0));
						float v = min(sqrt(dx * dx + dy * dy) / (1000.0 / _MapScale), 2.0);
						if (_ColorMode <= 0.5)
						{
							// Greyscale (SCANmap.getPartialMap: palette.lerp(Black, White, v / 2)). The
							// cutoff and the four slope colours are not consulted.
							float g = v * 0.5;
							col = float4(g, g, g, 1.0);
						}
						else if (v < _SlopeCutoff)
							col = lerp(_SlopeLoColorOne, _SlopeHiColorOne, v / _SlopeCutoff);
						else
							col = lerp(_SlopeLoColorTwo, _SlopeHiColorTwo, (v - _SlopeCutoff) / (2.0 - _SlopeCutoff));
						col.a = 1.0;
					}
				}
				else if (_MapMode < 2.5)        // ---- Biome ----
				{
					if (covHas(cov, 3.0))
					{
						float bIdx = tex2D(_BiomeIndexTex, dUV).r;
						float2 tx = float2(1.0 / _MapWidth, 1.0 / _MapHeight);
						// Border: differs from the SCREEN pixel to the left or the row below (the CPU's mapline
						// compare), each read through the same addressing rule as this pixel, so the border is one
						// screen pixel wide under every projection. A neighbour off the map (outside a projection's
						// disc) is no border.
						float2 uvL, uvD;
						float onL = dataUVAt(pixUV - float2(tx.x, 0), uvL);
						float onD = dataUVAt(pixUV - float2(0, tx.y), uvD);
						float bL = onL > 0.5 ? tex2D(_BiomeIndexTex, uvL).r : bIdx;
						float bD = onD > 0.5 ? tex2D(_BiomeIndexTex, uvD).r : bIdx;
						// A float flag, not a bool: keep boolean results out of anything FXC can fold into a
						// movc (see unproject).
						float edge = (abs(bIdx - bL) > 0.0001 || abs(bIdx - bD) > 0.0001) ? 1.0 : 0.0;
						if (_ColorMode <= 0.5)
						{
							// Greyscale (SCANmap.getPartialMap: palette.lerp(Black, White, biome index)),
							// with no elevation underlay. The CPU renderer drew the white edge in greyscale
							// whatever the border toggle said; here the toggle is honoured.
							float g = (edge > 0.5 && _BiomeBorder > 0.5) ? 1.0 : bIdx;
							col = float4(g, g, g, 1.0);
						}
						else if (_BiomeBorder > 0.5 && edge > 0.5)
						{
							col = float4(1.0, 1.0, 1.0, 1.0);   // palette.White border
						}
						else
						{
							// stock mapColor (LUT by biome fraction) or low/high gradient, + grey elevation underlay
							float4 g = _StockBiomes > 0.5 ? tex2D(_BiomeLUT, float2(bIdx + 0.5 / max(_BiomeCount, 1.0), 0.5)) : lerp(_LowBiomeColor, _HighBiomeColor, bIdx);
							// The underlay is palette.Grey (0.5) unless the body has terrain and this pixel's
							// altimetry is scanned - what the CPU renderer did. An unsampled cell is 0 in the
							// cache, so shading it anyway drags the biome colour toward black. Coverage test as
							// in the Altimetry branch (SCANtype.Altimetry = LoRes | HiRes, "either").
							float eg = 0.5;
							if (_HasElevation > 0.5 && (covHas(cov, 0.0) || covHas(cov, 1.0)))
							{
								float belev = tex2D(_ElevationTex, dUV).r;
								eg = _TerrainRange > 0.0 ? saturate((belev - _TerrainMin) / _TerrainRange) : 0.5;
							}
							col = lerp(g, float4(eg, eg, eg, 1.0), _BiomeTransparency);
							col.a = 1.0;
						}
					}
				}
				else if (_HasSource < 0.5)      // ---- Visual (3) without a source texture: unscanned everywhere (the old CPU fallback)
				{
					col = staticNoise(pix);
				}
				else                            // ---- Visual (3) ----
				{
					bool visHi = covHas(cov, 6.0);
					bool visLo = covHas(cov, 2.0);
					if (visHi)
					{
						col = tex2Dgrad(_ScaledColor, visUV, visDX, visDY);
						col.rgb = gasColor(col.rgb);
						if (_ColorMode > 0.5)
						{
							if (_HasNormal > 0.5)
								col.rgb = normalSoftLight(col.rgb, normalY(tex2Dgrad(_ScaledNormal, visUV, visDX, visDY)));
						}
						else
						{
							col.rgb = grayscale(col.rgb);
						}
						col.a = 1.0;
					}
					else if (visLo)
					{
						float2 q = float2(floor(fLon * 512.0) / 512.0, floor(fLat * 256.0) / 256.0);
						// The floor() step makes the derivative spike at every block edge, which would pick a
						// coarse mip there. Level 0 is what the CPU did (GetPixelBilinear on the base level).
						col = tex2Dlod(_ScaledColor, float4(q, 0.0, 0.0));
						col.rgb = gasColor(col.rgb);
						if (_ColorMode <= 0.5)
							col.rgb = grayscale(col.rgb);
						col.a = 1.0;
					}
				}

				// Resource overlay on top of the base colour (SCANmap.cs:1496-1518, resourceToColor32).
				if (_ResourceActive > 0.5)
				{
					bool resHi = covHas(cov, 8.0);
					bool resLo = covHas(cov, 7.0);
					if (resHi || resLo)
					{
						float ab = tex2D(_ResourceTex, dUV).r * 100.0;   // stored as fraction, *100 -> percent
						if (resLo && !resHi && ab > 0.0)
							ab = floor(ab / 5.0) * 5.0 + 2.5;              // LoRes 5% buckets (a true zero stays zero, as resourceToColor32)
						// resourceToColor32 zeroes a below-range abundance and only then tests Abundance == 0,
						// so a scanned cell with nothing in it takes the "scanned, empty" grey blend whatever
						// the range minimum is - including a minimum of 0, where ab < _ResMinRange never fires.
						if (ab <= 0.0 || ab < _ResMinRange)
							col = lerp(col, _GreyColor, _ResGreyBlend);
						else
						{
							float rt = _ResMaxRange > _ResMinRange ? (ab - _ResMinRange) / (_ResMaxRange - _ResMinRange) : 0.0;
							col = lerp(lerp(_ResMinColor, _ResMaxColor, saturate(rt)), col, _ResTransparency);
						}
					}
				}

				// Terminator day/night darkening (SCANmap.cs:1342-1360).
				if (_Terminator > 0.5)
				{
					float crossingLat = atan(_Gamma * sin(DEG2RAD * lon - DEG2RAD * _SunLonCenter)) * RAD2DEG;
					bool night = _SunLatCenter >= 0.0 ? (lat < crossingLat) : (lat > crossingLat);
					if (night)
						col.rgb = lerp(col.rgb, float3(0.0, 0.0, 0.0), 0.5);
				}

				return col * _OutputAlpha;   // 1 for the windows; the terrain planet overlay fades to 90 percent (lerp toward clear)
			}
			ENDCG
		}
	}
	Fallback Off
}
