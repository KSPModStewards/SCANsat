#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANterrainConfig - Serializable object for storing data about each planet's terrain options and color palette
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System.Linq;
using SCANsat.SCAN_Platform;
using SCANsat.SCAN_Palettes;

namespace SCANsat.SCAN_Data
{
	public class SCANterrainConfig : SCAN_ConfigNodeStorage
	{
		[Persistent]
		private string name;
		[Persistent]
		private int index;
		[Persistent]
		private float minHeightRange;
		[Persistent]
		private float maxHeightRange;
		[Persistent]
		private string clampHeight;
		[Persistent]
		private float maxHeightMultiplier = 1;
		[Persistent]
		private float minHeightMultiplier = 1;
		[Persistent]
		private float clampHeightMultiplier = 1;
		[Persistent]
		private string paletteName;
		[Persistent]
		private int paletteSize;
		[Persistent]
		private bool paletteReverse;
		[Persistent]
		private bool paletteDiscrete;

		private SCANPalette colorPal;
		private CelestialBody body;
		private float? clampTerrain;

		private float defaultMinHeight, defaultMaxHeight;
		private float terrainRange;
		private SCANPalette defaultPalette;
		private int defaultPaletteSize;
		private bool defaultReverse, defaultDiscrete;
		private float? defaultClamp;
		private float internalMaxHeightMult = 1;
		private float internalMinHeightMult = 1;
		private float internalClampHeightMult = 1;

		internal SCANterrainConfig(float min, float max, float? clamp, SCANPalette color, int size, bool reverse, bool discrete, CelestialBody b)
		{
			minHeightRange = min;
			maxHeightRange = max;
			terrainRange = max * maxHeightMultiplier - min * minHeightMultiplier;
			clampTerrain = clamp;
			if (clampTerrain == null)
			{
				clampHeight = "Null";
			}
			else
			{
				clampHeight = clampTerrain.Value.ToString("F0");
			}

			colorPal = color;
			paletteName = colorPal.Name;
			paletteSize = size;
			paletteReverse = reverse;
			paletteDiscrete = discrete;
			body = b;
			name = body.bodyName;
			index = body.flightGlobalsIndex;

			setDefaultValues();
		}

		public SCANterrainConfig()
		{
		}

		internal SCANterrainConfig(SCANterrainConfig copy)
		{
			maxHeightMultiplier = copy.maxHeightMultiplier;
			minHeightMultiplier = copy.minHeightMultiplier;
			clampHeightMultiplier = copy.clampHeightMultiplier;
			internalMaxHeightMult = maxHeightMultiplier;
			internalMinHeightMult = minHeightMultiplier;
			internalClampHeightMult = clampHeightMultiplier;
			minHeightRange = copy.minHeightRange;
			maxHeightRange = copy.maxHeightRange;
			terrainRange = maxHeightRange * maxHeightMultiplier - minHeightRange * minHeightMultiplier;
			clampTerrain = copy.clampTerrain;
			clampHeight = copy.clampHeight;
			colorPal = copy.colorPal;
			paletteName = copy.paletteName;
			paletteSize = copy.paletteSize;
			paletteReverse = copy.paletteReverse;
			paletteDiscrete = copy.paletteDiscrete;
			body = copy.body;
			name = copy.name;
		}

		public override void OnDecodeFromConfigNode()
		{
			body = FlightGlobals.Bodies.FirstOrDefault(b => b.flightGlobalsIndex == index);
			if (body != null)
			{
				name = body.bodyName;
			}
			else
			{
				name = "WrongBody" + index;
			}

			colorPal = SCANUtil.PaletteLoader(paletteName, paletteSize);

			float tempClamp = 0;
			if (clampHeight == "Null" || clampHeight == "null" || string.IsNullOrEmpty(clampHeight))
			{
				clampTerrain = null;
			}
			else if (float.TryParse(clampHeight, out tempClamp))
			{
				clampTerrain = tempClamp;
			}
			else
			{
				clampTerrain = null;
			}

			terrainRange = maxHeightRange * maxHeightMultiplier - minHeightRange * minHeightMultiplier;
			internalMaxHeightMult = maxHeightMultiplier;
			internalMinHeightMult = minHeightMultiplier;
			internalClampHeightMult = clampHeightMultiplier;

			setDefaultValues();
		}

		private void setDefaultValues()
		{
			defaultMinHeight = minHeightRange * internalMinHeightMult;
			defaultMaxHeight = maxHeightRange * internalMaxHeightMult;
			defaultClamp = clampTerrain * internalClampHeightMult;
			defaultPalette = colorPal;
			defaultPaletteSize = paletteSize;
			defaultDiscrete = paletteDiscrete;
			defaultReverse = paletteReverse;
		}

		public override void OnEncodeToConfigNode()
		{
			if (clampTerrain == null)
			{
				clampHeight = "Null";
			}
			else
			{
				clampHeight = clampTerrain.Value.ToString("F0");
			}

			paletteName = colorPal.Name;

			maxHeightMultiplier = 1;
			minHeightMultiplier = 1;
			clampHeightMultiplier = 1;
		}

		public override void onSavePost()
		{
			maxHeightMultiplier = internalMaxHeightMult;
			minHeightMultiplier = internalMinHeightMult;
			clampHeightMultiplier = internalClampHeightMult;
		}

		public float MinTerrain
		{
			get
			{
				float min = minHeightRange * internalMinHeightMult;

				if (min < -250000)
				{
					return 200000;
				}

				return min;
			}
			internal set
			{
				if (value < -250000)
				{
					value = -250000;
				}

				if (value < maxHeightRange * internalMaxHeightMult)
				{
					terrainRange = maxHeightRange * internalMaxHeightMult - value;
					minHeightRange = value / internalMinHeightMult;
				}
			}
		}

		public float MaxTerrain
		{
			get
			{
				float max = maxHeightRange * internalMaxHeightMult;

				if (max > 500000)
				{
					return 500000;
				}

				return max;
			}
			internal set
			{
				if (value > 500000)
				{
					value = 500000;
				}

				if (value > minHeightRange * internalMinHeightMult)
				{
					terrainRange = value - minHeightRange * internalMinHeightMult;
					maxHeightRange = value / internalMaxHeightMult;
				}
			}
		}

		public float TerrainRange
		{
			get { return terrainRange; }
		}

		public float? ClampTerrain
		{
			get { return clampTerrain * internalClampHeightMult; }
			internal set
			{
				if (value == null)
				{
					clampTerrain = null;
				}
				else if (value > minHeightRange * internalMinHeightMult && value < maxHeightRange * internalMaxHeightMult)
				{
					clampTerrain = value / internalClampHeightMult;
				}
			}
		}

		public SCANPalette ColorPal
		{
			get { return colorPal; }
			internal set { colorPal = value; }
		}

		public int PalSize
		{
			get { return paletteSize; }
			internal set { paletteSize = value; }
		}

		public bool PalRev
		{
			get { return paletteReverse; }
			internal set { paletteReverse = value; }
		}

		public bool PalDis
		{
			get { return paletteDiscrete; }
			internal set { paletteDiscrete = value; }
		}

		public CelestialBody Body
		{
			get { return body; }
		}

		public string Name
		{
			get { return name; }
		}

		public float DefaultMinHeight
		{
			get { return defaultMinHeight; }
		}

		public float DefaultMaxHeight
		{
			get { return defaultMaxHeight; }
		}

		public float? DefaultClampHeight
		{
			get { return defaultClamp; }
		}

		public SCANPalette DefaultPalette
		{
			get { return defaultPalette; }
		}

		public int DefaultPaletteSize
		{
			get { return defaultPaletteSize; }
		}

		public bool DefaultReverse
		{
			get { return defaultReverse; }
		}

		public bool DefaultDiscrete
		{
			get { return defaultDiscrete; }
		}

		public float MaxHeightMultiplier
		{
			get { return internalMaxHeightMult; }
		}

		public float MinHeightMultiplier
		{
			get { return internalMinHeightMult; }
		}

		public float ClampHeightMultiplier
		{
			get { return internalClampHeightMult; }
		}
	}

}
