#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 * 
 * SCANdata - encapsulates scanned data for a body
 * 
 * Copyright (c)2013 damny;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Contracts;
using FinePrint;
using FinePrint.Contracts;
using FinePrint.Contracts.Parameters;
using FinePrint.Utilities;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using SCANsat.SCAN_Platform;
using SCANsat.SCAN_Platform.Palettes;
using SCANsat.SCAN_Platform.Palettes.ColorBrewer;
using SCANsat.SCAN_Unity;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANpalette;

namespace SCANsat.SCAN_Data
{
	public class SCANdata
	{
		private static Dictionary<int, float[,]> heightMaps = new Dictionary<int, float[,]>();

		/* MAP: state */
		private Int32[,] coverage;
		private CelestialBody body;
		private SCANterrainConfig terrainConfig;
		private bool mapBuilding, overlayBuilding, controllerBuilding, built;

		private float[,] tempHeightMap;

		/* MAP: options */
		private bool disabled;

		/* MAP: constructor */
		internal SCANdata(CelestialBody b)
		{
			body = b;

			coverage = new int[360, 180];

			if (heightMaps.ContainsKey(body.flightGlobalsIndex))
				built = true;

			terrainConfig = SCANcontroller.getTerrainNode(b.name);

			if (terrainConfig == null)
			{
				float? clamp = null;
				if (b.ocean)
					clamp = 0;

				float newMax;

				try
				{
					newMax = ((float)CelestialUtilities.GetHighestPeak(b)).Mathf_Round(-2);
				}
				catch (Exception e)
				{
					SCANUtil.SCANlog("Error in calculating Max Height for {0}; using default value\n{1}", b.displayName, e);
					newMax = SCANconfigLoader.SCANNode.DefaultMaxHeightRange;
				}

				terrainConfig = new SCANterrainConfig(SCANconfigLoader.SCANNode.DefaultMinHeightRange, newMax, clamp, SCANUtil.paletteLoader(SCANconfigLoader.SCANNode.DefaultPalette, 7), 7, false, false, body);
				SCANcontroller.addToTerrainConfigData(body.name, terrainConfig);
			}
		}

		public SCANdata (SCANdata copy)
		{
			coverage = copy.coverage;
			terrainConfig = new SCANterrainConfig(copy.terrainConfig);

			if (!heightMaps.ContainsKey(copy.body.flightGlobalsIndex))
				return;

			tempHeightMap = heightMaps[copy.body.flightGlobalsIndex];
		}

		#region Public accessors
		/* Accessors: body-specific variables */
		public Int32[,] Coverage
		{
			get { return coverage; }
			internal set { coverage = value; }
		}

		public float HeightMapValue(int i, int lon, int lat, bool useTemp = false)
		{
			if (useTemp)
				return tempHeightMap[lon, lat];

			if (!heightMaps.ContainsKey(i))
				return 0;

			if (body.pqsController == null)
				return 0;

			if (heightMaps[i].Length < 10)
				return 0;

			return heightMaps[i][lon, lat];
		}

		public CelestialBody Body
		{
			get { return body; }
		}

		public SCANterrainConfig TerrainConfig
		{
			get { return terrainConfig; }
			internal set { terrainConfig = value; }
		}

		public bool Disabled
		{
			get { return disabled; }
			internal set { disabled = value; }
		}

		public bool MapBuilding
		{
			get { return mapBuilding; }
			internal set { mapBuilding = value; }
		}

		public bool OverlayBuilding
		{
			get { return overlayBuilding; }
			internal set { overlayBuilding = value; }
		}

		public bool ControllerBuilding
		{
			get { return controllerBuilding; }
			internal set { controllerBuilding = value; }
		}

		public bool Built
		{
			get { return built; }
		}
		#endregion

		#region Anomalies
		/* DATA: anomalies and such */
		private SCANanomaly[] anomalies;

		public SCANanomaly[] Anomalies
		{
			get
			{
				if (anomalies == null)
				{
					PQSSurfaceObject[] sites = body.pqsSurfaceObjects;
					anomalies = new SCANanomaly[sites.Length];
					for (int i = 0; i < sites.Length; ++i)
					{
						anomalies[i] = new SCANanomaly(sites[i].SurfaceObjectName
							, body.GetLongitude(sites[i].transform.position)
							, body.GetLatitude(sites[i].transform.position)
							, sites[i]);
					}
				}

				for (int i = 0; i < anomalies.Length; ++i)
				{
					anomalies[i].Known = SCANUtil.isCovered(anomalies[i].Longitude
						, anomalies[i].Latitude
						, this
						, SCANtype.Anomaly);

					anomalies[i].Detail = SCANUtil.isCovered(anomalies[i].Longitude
						, anomalies[i].Latitude
						, this
						, SCANtype.AnomalyDetail);
				}
				return anomalies;
			}
		}

		#endregion

		#region Waypoints

		private List<SCANwaypoint> waypoints;

		public void addToWaypoints()
		{
			if (SCANcontroller.controller == null)
				return;

			addToWaypoints(SCANcontroller.controller.LandingTarget);

			if (SCAN_UI_ZoomMap.Instance != null && SCAN_UI_ZoomMap.Instance.IsVisible && SCAN_UI_ZoomMap.Body == body)
				SCAN_UI_ZoomMap.Instance.RefreshIcons();

			if (SCAN_UI_BigMap.Instance != null && SCAN_UI_BigMap.Instance.IsVisible && SCAN_UI_BigMap.Body == body)
				SCAN_UI_BigMap.Instance.RefreshIcons();
		}

		public void addToWaypoints(SCANwaypoint w)
		{
			if (waypoints == null)
			{
				waypoints = new List<SCANwaypoint>() { w };
				return;
			}

			if (waypoints.Any(a => a.LandingTarget))
				waypoints.RemoveAll(a => a.LandingTarget);

			waypoints.Insert(0, w);
		}

		public void removeTargetWaypoint()
		{
			if (waypoints == null)
				return;

			if (waypoints.Any(a => a.LandingTarget))
				waypoints.RemoveAll(a => a.LandingTarget);

			SCANcontroller.controller.LandingTarget = null;
		}

		public void addSurveyWaypoints(CelestialBody b, SurveyContract c)
		{
			if (b != body)
				return;

			if (c == null)
				return;

			for (int i = 0; i < c.AllParameters.Count(); i++)
			{
				if (c.AllParameters.ElementAt(i).GetType() == typeof(SurveyWaypointParameter))
				{
					SurveyWaypointParameter s = (SurveyWaypointParameter)c.AllParameters.ElementAt(i);
					if (s.State == ParameterState.Incomplete)
					{
						if (waypoints.Any(w => w.Way == s.wp))
							continue;

						SCANwaypoint p = new SCANwaypoint(s);
						if (p.Way != null)
							waypoints.Add(p);
					}
				}
			}

		}

		public List<SCANwaypoint> Waypoints
		{
			get
			{
				if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER)
				{
					if (waypoints == null)
						waypoints = new List<SCANwaypoint>();
				}

				if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER && !SCANcontroller.controller.ContractsLoaded)
					return new List<SCANwaypoint>();
				else// if (!waypointsLoaded)
				{
					SCANwaypoint landingTarget = null;

					//waypointsLoaded = true;
					if (waypoints == null)
						waypoints = new List<SCANwaypoint>();
					else
					{
						landingTarget = waypoints.FirstOrDefault(w => w.LandingTarget);
						
						waypoints.Clear();
					}

					if (landingTarget != null)
						waypoints.Add(landingTarget);

					if (ContractSystem.Instance != null)
					{
						var surveys = ContractSystem.Instance.GetCurrentActiveContracts<SurveyContract>();
						for (int i = 0; i < surveys.Length; i++)
						{
							if (surveys[i].targetBody == body)
							{
								for (int j = 0; j < surveys[i].AllParameters.Count(); j++)
								{
									if (surveys[i].AllParameters.ElementAt(j).GetType() == typeof(SurveyWaypointParameter))
									{
										SurveyWaypointParameter s = (SurveyWaypointParameter)surveys[i].AllParameters.ElementAt(j);
										if (s.State == ParameterState.Incomplete)
										{
											SCANwaypoint p = new SCANwaypoint(s);
											if (p.Way != null)
												waypoints.Add(p);
										}
									}
								}
							}
						}

						var stationary = ContractSystem.Instance.GetCurrentActiveContracts<SatelliteContract>();
						for (int i = 0; i < stationary.Length; i++)
						{
							SpecificOrbitParameter orbit = stationary[i].GetParameter<SpecificOrbitParameter>();
							if (orbit == null)
								continue;

							if (orbit.TargetBody == body)
							{
								for (int j = 0; j < stationary[i].AllParameters.Count(); j++)
								{
									if (stationary[i].AllParameters.ElementAt(j).GetType() == typeof(StationaryPointParameter))
									{
										StationaryPointParameter s = (StationaryPointParameter)stationary[i].AllParameters.ElementAt(j);
										if (s.State == ParameterState.Incomplete)
										{
											SCANwaypoint p = new SCANwaypoint(s);
											if (p.Way != null)
												waypoints.Add(p);
										}
									}
								}
							}
						}
					}

					if (WaypointManager.Instance() != null)
					{
						var remaining = WaypointManager.Instance().Waypoints;
						for (int i = 0; i < remaining.Count; i++)
						{
							Waypoint p = remaining[i];
							if (p.isOnSurface && p.isNavigatable)
							{
								if (p.celestialName == body.GetName())
								{
									bool add = true;

									for (int j = waypoints.Count - 1; j >= 0; j--)
									{
										SCANwaypoint w = waypoints[j];

										if (w.Seed != p.uniqueSeed)
											continue;

										add = false;
										break;
									}

									if (add)
									{
										if (p.contractReference != null)
										{
											if (p.contractReference.ContractState == Contract.State.Active)
											{
												waypoints.Add(new SCANwaypoint(p));
											}
										}
										else
											waypoints.Add(new SCANwaypoint(p));
									}
								}
							}
						}
					}
				}

				return waypoints;
			}
		}

		#endregion

		#region Scanning coverage
		/* DATA: coverage */
		private int[] coverage_count = Enumerable.Repeat(360 * 180, 32).ToArray();
		internal void updateCoverage()
		{
			for (int i = 0; i < 32; ++i)
			{
				SCANtype t = (SCANtype)(1 << i);
				int cc = 0;
				for (int x = 0; x < 360; ++x)
				{
					for (int y = 0; y < 180; ++y)
					{
						if ((coverage[x, y] & (Int32)t) == 0)
							++cc;
					}
				}
				coverage_count[i] = cc;
			}
		}
		internal int getCoverage(SCANtype type)
		{
			int uncov = 0;
			if ((type & SCANtype.AltimetryLoRes) != SCANtype.Nothing)
				uncov += coverage_count[0];
			if ((type & SCANtype.AltimetryHiRes) != SCANtype.Nothing)
				uncov += coverage_count[1];
			if ((type & SCANtype.Biome) != SCANtype.Nothing)
				uncov += coverage_count[3];
			if ((type & SCANtype.Anomaly) != SCANtype.Nothing)
				uncov += coverage_count[4];
			if ((type & SCANtype.AnomalyDetail) != SCANtype.Nothing)
				uncov += coverage_count[5];
			if ((type & SCANtype.Kethane) != SCANtype.Nothing)
				uncov += coverage_count[6];
			if ((type & SCANtype.MetallicOre) != SCANtype.Nothing)
				uncov += coverage_count[7];
			if ((type & SCANtype.Ore) != SCANtype.Nothing)
				uncov += coverage_count[8];
			if ((type & SCANtype.SolarWind) != SCANtype.Nothing)
				uncov += coverage_count[9];
			if ((type & SCANtype.Uraninite) != SCANtype.Nothing)
				uncov += coverage_count[10];
			if ((type & SCANtype.Monazite) != SCANtype.Nothing)
				uncov += coverage_count[11];
			if ((type & SCANtype.Alumina) != SCANtype.Nothing)
				uncov += coverage_count[12];
			if ((type & SCANtype.Water) != SCANtype.Nothing)
				uncov += coverage_count[13];
			if ((type & SCANtype.Aquifer) != SCANtype.Nothing)
				uncov += coverage_count[14];
			if ((type & SCANtype.Minerals) != SCANtype.Nothing)
				uncov += coverage_count[15];
			if ((type & SCANtype.Substrate) != SCANtype.Nothing)
				uncov += coverage_count[16];
			if ((type & SCANtype.MetalOre) != SCANtype.Nothing)
				uncov += coverage_count[17];
			if ((type & SCANtype.Karbonite) != SCANtype.Nothing)
				uncov += coverage_count[18];
			if ((type & SCANtype.FuzzyResources) != SCANtype.Nothing)
				uncov += coverage_count[19];
			if ((type & SCANtype.Hydrates) != SCANtype.Nothing)
				uncov += coverage_count[20];
			if ((type & SCANtype.Gypsum) != SCANtype.Nothing)
				uncov += coverage_count[21];
			if ((type & SCANtype.RareMetals) != SCANtype.Nothing)
				uncov += coverage_count[22];
			if ((type & SCANtype.ExoticMinerals) != SCANtype.Nothing)
				uncov += coverage_count[23];
			if ((type & SCANtype.Dirt) != SCANtype.Nothing)
				uncov += coverage_count[24];
			if ((type & SCANtype.Borate) != SCANtype.Nothing)
				uncov += coverage_count[25];
			if ((type & SCANtype.GeoEnergy) != SCANtype.Nothing)
				uncov += coverage_count[26];
			if ((type & SCANtype.SaltWater) != SCANtype.Nothing)
				uncov += coverage_count[27];
			if ((type & SCANtype.Silicates) != SCANtype.Nothing)
				uncov += coverage_count[28];
			
			return uncov;
		}
		
		#endregion

		#region Height Map

		internal void generateHeightMap(ref int step, ref int xStart, int width)
		{
			if (body.pqsController == null)
			{
				built = true;
				mapBuilding = false;
				overlayBuilding = false;
				controllerBuilding = false;
				if (!heightMaps.ContainsKey(body.flightGlobalsIndex))
					heightMaps.Add(body.flightGlobalsIndex, new float[1, 1]);
				return;
			}

			if (step <= 0 && xStart <= 0)
			{
				SCANcontroller.controller.loadPQS(body);

				try
				{
					double d = SCANUtil.getElevation(body, 0, 0);
				}
				catch (Exception e)
				{
					Debug.LogError("[SCANsat] Error In Detecting Terrain Height Map; Stopping Height Map Generator\n" + e);
					built = true;
					mapBuilding = false;
					overlayBuilding = false;
					controllerBuilding = false;
					if (!heightMaps.ContainsKey(body.flightGlobalsIndex))
						heightMaps.Add(body.flightGlobalsIndex, new float[1, 1]);
					return;
				}
			}

			if (tempHeightMap == null)
			{
				tempHeightMap = new float[360, 180];
			}

			if (step >= 179)
			{
				SCANcontroller.controller.unloadPQS(body);
				step = 0;
				xStart = 0;
				built = true;
				mapBuilding = false;
				overlayBuilding = false;
				controllerBuilding = false;
				if (!heightMaps.ContainsKey(body.flightGlobalsIndex))
					heightMaps.Add(body.flightGlobalsIndex, tempHeightMap);
				tempHeightMap = null;
				SCANUtil.SCANlog("Height Map Of [{0}] Completed...", body.displayName);
				return;
			}

			for (int i = xStart; i < xStart + width; i++)
			{
				tempHeightMap[i, step] = (float)SCANUtil.getElevation(body, i - 180, step - 90);
			}

			if (xStart + width >= 359)
			{
				step++;
				xStart = 0;
				return;
			}

			xStart += width;
		}
		#endregion

		#region Map Utilities
		/* DATA: debug option to fill in the map */
		internal void fillMap(SCANtype type)
		{
			for (int i = 0; i < 360; i++)
			{
				for (int j = 0; j < 180; j++)
				{
					coverage[i, j] |= (int)type;
				}
			}
		}

		internal void fillResourceMap()
		{
			for (int i = 0; i < 360; i++)
			{
				for (int j = 0; j < 180; j++)
				{
					coverage[i, j] |= (Int32)SCANtype.AllResources;
				}
			}
		}

		/* DATA: reset the map */
		internal void reset()
		{
			coverage = new Int32[360, 180];

			if (SCAN_UI_MainMap.Instance != null && SCAN_UI_MainMap.Instance.IsVisible)
				SCAN_UI_MainMap.Instance.resetImages();
		}

		internal void reset(SCANtype type)
		{
			SCANtype mask = type;

			mask ^= SCANtype.Everything;

			for (int x = 0; x < 360; x++)
			{
				for (int y = 0; y < 180; y++)
				{
					coverage[x, y] &= (int)mask;
				}
			}

			if (SCAN_UI_MainMap.Instance != null && SCAN_UI_MainMap.Instance.IsVisible)
				SCAN_UI_MainMap.Instance.resetImages();
		}

		#endregion

		#region Data Serialize/Deserialize

		//Take the Int32[] coverage and convert it to a single dimension byte array
		private byte[] ConvertToByte(Int32[,] iArray)
		{
			byte[] bArray = new byte[360 * 180 * 4];
			int k = 0;
			for (int i = 0; i < 360; i++)
			{
				for (int j = 0; j < 180; j++)
				{
					byte[] bytes = BitConverter.GetBytes(iArray[i, j]);
					for (int m = 0; m < bytes.Length; m++)
					{
						bArray[k++] = bytes[m];
					}
				}
			}
			return bArray;
		}

		//Convert byte array from persistent file to usable Int32[]
		private Int32[,] ConvertToInt(byte[] bArray)
		{
			Int32[,] iArray = new Int32[360, 180];
			int k = 0;
			for (int i = 0; i < 360; i++)
			{
				for (int j = 0; j < 180; j++)
				{
					iArray[i, j] = BitConverter.ToInt32(bArray, k);
					k += 4;
				}
			}
			return iArray;
		}

		/* DATA: serialization and compression */
		internal string integerSerialize()
		{
			byte[] bytes = ConvertToByte(Coverage);
			MemoryStream mem = new MemoryStream();
			BinaryFormatter binf = new BinaryFormatter();
			binf.Serialize(mem, bytes);
			string blob = Convert.ToBase64String(SCAN_CLZF2.Compress(mem.ToArray()));
			return blob.Replace("/", "-").Replace("=", "_");
		}

		internal void integerDeserialize(string blob)
		{
			try
			{
				blob = blob.Replace("-", "/").Replace("_", "=");
				byte[] bytes = Convert.FromBase64String(blob);
				bytes = SCAN_CLZF2.Decompress(bytes);
				MemoryStream mem = new MemoryStream(bytes, false);
				BinaryFormatter binf = new BinaryFormatter();
				byte[] bArray = (byte[])binf.Deserialize(mem);
				Coverage = ConvertToInt(bArray);
			}
			catch (Exception e)
			{
				Coverage = new Int32[360, 180];
				throw e;
			}
		}

#endregion

	}
}
