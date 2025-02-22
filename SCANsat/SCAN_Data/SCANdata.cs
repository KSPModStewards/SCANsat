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
using SCANsat.SCAN_Palettes;
using SCANsat.SCAN_Unity;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANcolorUtil;

namespace SCANsat.SCAN_Data
{
	public class SCANdata
	{
		private static Dictionary<int, float[,]> heightMaps = new Dictionary<int, float[,]>();

		/* MAP: state */
		internal Int16[,] coverage;
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

			coverage = new Int16[360, 180];

			if (heightMaps.ContainsKey(body.flightGlobalsIndex))
			{
				built = true;
			}

			terrainConfig = SCANcontroller.getTerrainNode(b.bodyName);

			if (terrainConfig == null)
			{
				float? clamp = null;
				if (b.ocean)
				{
					clamp = 0;
				}

				float newMax;

				try
				{
					newMax = ((float)CelestialUtilities.GetHighestPeak(b)).Mathf_Round(-2);
				}
				catch (Exception e)
				{
					SCANUtil.SCANlog("Error in calculating Max Height for {0}; using default value\n{1}", b.displayName.LocalizeBodyName(), e);
					newMax = SCANconfigLoader.SCANNode.DefaultMaxHeightRange;
				}

				terrainConfig = new SCANterrainConfig(SCANconfigLoader.SCANNode.DefaultMinHeightRange, newMax, clamp, SCAN_Palette_Config.DefaultPalette.GetPalette(0), 7, false, false, body);
				SCANcontroller.addToTerrainConfigData(body.bodyName, terrainConfig);
			}
		}

		public SCANdata(SCANdata copy)
		{
			coverage = copy.coverage;
			terrainConfig = new SCANterrainConfig(copy.terrainConfig);

			if (!heightMaps.ContainsKey(copy.body.flightGlobalsIndex))
			{
				return;
			}

			tempHeightMap = heightMaps[copy.body.flightGlobalsIndex];
		}

		#region Public accessors
		/* Accessors: body-specific variables */
		public Int16[,] Coverage
		{
			get { return coverage; }
			internal set { coverage = value; }
		}

		public float HeightMapValue(int i, int lon, int lat, bool useTemp = false)
		{
			if (useTemp)
			{
				return tempHeightMap[lon, lat];
			}

			if (!heightMaps.ContainsKey(i))
			{
				return 0;
			}

			if (body.pqsController == null)
			{
				return 0;
			}

			if (heightMaps[i].Length < 10)
			{
				return 0;
			}

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

		#region ROCS

		private List<SCANROC> rocs;

		public List<SCANROC> ROCS(bool refresh)
		{
			if (ROCManager.Instance == null)
			{
				return null;
			}

			if (!ROCManager.Instance.RocsEnabledInCurrentGame)
			{
				return null;
			}

			if (!SCANcontroller.controller.SerenityLoaded)
			{
				return null;
			}

			if (rocs == null)
			{
				rocs = new List<SCANROC>();
			}

			if (!refresh && rocs.Count > 0)
			{
				return rocs;
			}

			rocs.Clear();

			if (body == null)
			{
				return null;
			}

			PQS controller = body.pqsController;

			if (controller == null || controller.transform == null)
			{
				return null;
			}

			for (int i = controller.transform.childCount - 1; i >= 0; i--)
			{
				Transform child = controller.transform.GetChild(i);

				if (child == null)
				{
					continue;
				}

				if (child.name.StartsWith("ROC"))
				{
					int index = child.name.IndexOf(' ');

					if (index > 0 && index < child.name.Length - 1)
					{
						string id = child.name.Substring(index + 1);

						bool scanned = false;

						if (HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX || HighLogic.CurrentGame.Mode == Game.Modes.MISSION)
						{
							scanned = true;
						}
						else
						{
							List<ScienceSubject> subjects = ResearchAndDevelopment.GetSubjects();

							if (subjects != null)
							{
								for (int k = subjects.Count - 1; k >= 0; k--)
								{
									if (subjects[k].id.Contains(id))
									{
										scanned = true;
										break;
									}
								}
							}
						}

						for (int j = child.childCount - 1; j >= 0; j--)
						{
							Transform cache = child.GetChild(j);

							if (cache == null)
							{
								continue;
							}

							if (cache.name != ("Unassigned"))
							{
								ROC roc = cache.GetComponentInChildren<ROC>();

								if (roc != null)
								{
									if (!roc.smallROC && !roc.canbetaken)
									{
										if (roc.transform != null)
										{
											double lon = body.GetLongitude(roc.transform.position);
											double lat = body.GetLatitude(roc.transform.position);

											rocs.Add(new SCANROC(roc
												, roc.displayName
												, lon
												, lat
												, /*SCANUtil.isCovered(lon, lat, this, SCANtype.Anomaly) &&*/ SCANUtil.isCovered(lon, lat, this, SCANtype.AnomalyDetail)
												, scanned));
										}
									}
								}
							}
						}
					}
				}
			}

			return rocs;
		}

		#endregion

		#region Waypoints

		private List<SCANwaypoint> waypoints = new List<SCANwaypoint>();
		private bool waypointsLoaded;
		private int localWaypointCount;

		public void addToWaypoints()
		{
			if (SCANcontroller.controller == null)
			{
				return;
			}

			addToWaypoints(SCANcontroller.controller.LandingTarget);

			if (SCAN_UI_ZoomMap.Instance != null && SCAN_UI_ZoomMap.Instance.IsVisible && SCAN_UI_ZoomMap.Body == body)
			{
				SCAN_UI_ZoomMap.Instance.RefreshIcons();
			}

			if (SCAN_UI_BigMap.Instance != null && SCAN_UI_BigMap.Instance.IsVisible && SCAN_UI_BigMap.Body == body)
			{
				SCAN_UI_BigMap.Instance.RefreshIcons();
			}
		}

		public void addToWaypoints(SCANwaypoint w)
		{
			if (waypoints == null)
			{
				waypoints = new List<SCANwaypoint>() { w };
				return;
			}

			if (waypoints.Any(a => a.LandingTarget))
			{
				waypoints.RemoveAll(a => a.LandingTarget);
			}

			waypoints.Insert(0, w);
		}

		public void removeTargetWaypoint()
		{
			if (waypoints == null)
			{
				return;
			}

			if (waypoints.Any(a => a.LandingTarget))
			{
				waypoints.RemoveAll(a => a.LandingTarget);
			}

			SCANcontroller.controller.LandingTarget = null;
		}

		public void addSurveyWaypoints(CelestialBody b, SurveyContract c)
		{
			if (b != body)
			{
				return;
			}

			if (c == null)
			{
				return;
			}

			for (int i = c.ParameterCount - 1; i >= 0; i--)
			{
				ContractParameter cp = c.GetParameter(i);

				if (cp.GetType() == typeof(SurveyWaypointParameter))
				{
					if (cp.State == ParameterState.Incomplete)
					{
						Waypoint wp = ((SurveyWaypointParameter)cp).wp;

						if (wp == null)
						{
							continue;
						}

						bool add = true;

						for (int j = waypoints.Count - 1; j >= 0; j--)
						{
							SCANwaypoint w = waypoints[j];

							if (w == null || w.Way == null)
							{
								continue;
							}

							if (w.Way == wp)
							{
								add = false;
								break;
							}
						}

						if (add)
						{
							SCANwaypoint p = new SCANwaypoint((SurveyWaypointParameter)cp);

							if (p.Way != null)
							{
								waypoints.Add(p);
							}
						}
					}
				}
			}

			int count = GetLocalWaypointCount();

			if (count != localWaypointCount + 1)
			{
				waypointsLoaded = false;
			}
			else
			{
				localWaypointCount = count;
			}
		}

		public void addStationaryWaypoints(CelestialBody b, SatelliteContract c)
		{
			for (int i = 0; i < c.AllParameters.Count(); i++)
			{
				ContractParameter cp = c.GetParameter(i);

				if (cp.GetType() == typeof(SurveyWaypointParameter))
				{
					SurveyWaypointParameter s = (SurveyWaypointParameter)cp;

					if (cp.State == ParameterState.Incomplete)
					{
						Waypoint wp = ((SurveyWaypointParameter)cp).wp;

						if (wp == null)
						{
							continue;
						}

						bool add = true;

						for (int j = waypoints.Count - 1; j >= 0; j--)
						{
							SCANwaypoint w = waypoints[j];

							if (w == null || w.Way == null)
							{
								continue;
							}

							if (w.Way == wp)
							{
								add = false;
								break;
							}
						}

						if (add)
						{
							SCANwaypoint p = new SCANwaypoint((SurveyWaypointParameter)cp);

							if (p.Way != null)
							{
								waypoints.Add(p);
							}
						}
					}
				}
			}

			int count = GetLocalWaypointCount();

			if (count != localWaypointCount + 1)
			{
				waypointsLoaded = false;
			}
			else
			{
				localWaypointCount = count;
			}
		}

		public void addCustomWaypoint(Waypoint wp)
		{
			if (wp.isOnSurface && wp.isNavigatable)
			{
				if (wp.celestialName == body.GetName())
				{
					bool add = true;

					for (int j = waypoints.Count - 1; j >= 0; j--)
					{
						SCANwaypoint w = waypoints[j];

						if (w.Seed != wp.uniqueSeed)
						{
							continue;
						}

						add = false;
						break;
					}

					if (add)
					{
						waypoints.Add(new SCANwaypoint(wp));
					}
				}
			}

			int count = GetLocalWaypointCount();

			if (count != localWaypointCount + 1)
			{
				waypointsLoaded = false;
			}
			else
			{
				localWaypointCount = count;
			}
		}

		private int GetLocalWaypointCount()
		{
			if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER)
			{
				return 0;
			}

			if (WaypointManager.Instance() == null)
			{
				return 0;
			}

			int count = 0;

			var points = WaypointManager.Instance().Waypoints;

			for (int i = 0; i < points.Count; i++)
			{
				Waypoint p = points[i];

				if (p.isOnSurface && p.isNavigatable)
				{
					if (p.celestialName == body.GetName())
					{
						count++;
					}
				}
			}

			return count;
		}

		public List<SCANwaypoint> Waypoints
		{
			get
			{
				if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER)
				{
					if (waypoints == null)
					{
						waypoints = new List<SCANwaypoint>();
					}
				}

				if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER && !SCANcontroller.controller.ContractsLoaded)
				{
					return new List<SCANwaypoint>();
				}

				if (GetLocalWaypointCount() != localWaypointCount)
				{
					waypointsLoaded = false;
				}

				if (!waypointsLoaded)
				{
					SCANwaypoint landingTarget = null;

					waypointsLoaded = true;
					if (waypoints == null)
					{
						waypoints = new List<SCANwaypoint>();
					}
					else
					{
						landingTarget = waypoints.FirstOrDefault(w => w.LandingTarget);

						waypoints.Clear();
					}

					if (landingTarget != null)
					{
						waypoints.Add(landingTarget);
					}

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
											{
												waypoints.Add(p);
											}
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
							{
								continue;
							}

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
											{
												waypoints.Add(p);
											}
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
										{
											continue;
										}

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
										{
											waypoints.Add(new SCANwaypoint(p));
										}
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
		//Populate coverage array with max value of 360*180 accounting for the Cos of lat
		private double[] coverage_count = Enumerable.Repeat(41251.914, 16).ToArray();
		internal void updateCoverage()
		{
			for (int i = 0; i < 16; ++i)
			{
				SCANtype t = (SCANtype)(1 << i);

				if (!SCANUtil.scanTypeValid(t))
				{
					coverage_count[i] = 41251.914;
					continue;
				}

				double cc = 0;

				for (int x = 0; x < 360; ++x)
				{
					for (int y = 0; y < 180; ++y)
					{
						if ((coverage[x, y] & (short)t) == 0)
						{
							cc += SCANUtil.cosLookUp[y];
						}
					}
				}

				coverage_count[i] = cc;
			}
		}
		internal double getCoverage(SCANtype type)
		{
			double uncov = 0;
			if ((type & SCANtype.AltimetryLoRes) != SCANtype.Nothing)
			{
				uncov += coverage_count[0];
			}

			if ((type & SCANtype.AltimetryHiRes) != SCANtype.Nothing)
			{
				uncov += coverage_count[1];
			}

			if ((type & SCANtype.VisualLoRes) != SCANtype.Nothing)
			{
				uncov += coverage_count[2];
			}

			if ((type & SCANtype.Biome) != SCANtype.Nothing)
			{
				uncov += coverage_count[3];
			}

			if ((type & SCANtype.Anomaly) != SCANtype.Nothing)
			{
				uncov += coverage_count[4];
			}

			if ((type & SCANtype.AnomalyDetail) != SCANtype.Nothing)
			{
				uncov += coverage_count[5];
			}

			if ((type & SCANtype.VisualHiRes) != SCANtype.Nothing)
			{
				uncov += coverage_count[6];
			}

			if ((type & SCANtype.ResourceLoRes) != SCANtype.Nothing)
			{
				uncov += coverage_count[7];
			}

			if ((type & SCANtype.ResourceHiRes) != SCANtype.Nothing)
			{
				uncov += coverage_count[8];
			}

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
				{
					heightMaps.Add(body.flightGlobalsIndex, new float[1, 1]);
				}

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
					{
						heightMaps.Add(body.flightGlobalsIndex, new float[1, 1]);
					}

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
				{
					heightMaps.Add(body.flightGlobalsIndex, tempHeightMap);
				}

				tempHeightMap = null;
				SCANUtil.SCANlog("Height Map Of [{0}] Completed...", body.bodyName);
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
			short fill = (short)type;

			for (int i = 0; i < 360; i++)
			{
				for (int j = 0; j < 180; j++)
				{
					coverage[i, j] |= fill;
				}
			}
		}

		internal void fillResourceMap()
		{
			short fill = (short)SCANtype.ResourceHiRes;

			for (int i = 0; i < 360; i++)
			{
				for (int j = 0; j < 180; j++)
				{
					coverage[i, j] |= fill;
				}
			}
		}

		/* DATA: reset the map */
		internal void reset()
		{
			coverage = new Int16[360, 180];

			if (SCAN_UI_MainMap.Instance != null && SCAN_UI_MainMap.Instance.IsVisible)
			{
				SCAN_UI_MainMap.Instance.resetImages();
			}
		}

		internal void reset(SCANtype type)
		{
			SCANtype mask = type;

			mask ^= SCANtype.Everything;

			short m = (short)mask;

			for (int x = 0; x < 360; x++)
			{
				for (int y = 0; y < 180; y++)
				{
					coverage[x, y] &= m;
				}
			}

			if (SCAN_UI_MainMap.Instance != null && SCAN_UI_MainMap.Instance.IsVisible)
			{
				SCAN_UI_MainMap.Instance.resetImages();
			}
		}

		#endregion

		#region Data Serialize/Deserialize

		//Take the Int32[] coverage and convert it to a single dimension byte array
		private byte[] ConvertToByte(Int16[,] iArray)
		{
			byte[] bArray = new byte[360 * 180 * 2];
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
		private Int16[,] ConvertToInt(byte[] bArray)
		{
			Int16[,] iArray = new Int16[360, 180];
			int k = 0;
			for (int i = 0; i < 360; i++)
			{
				for (int j = 0; j < 180; j++)
				{
					iArray[i, j] = BitConverter.ToInt16(bArray, k);
					k += 2;
				}
			}
			return iArray;
		}

		/* DATA: serialization and compression */
		internal string shortSerialize()
		{
			byte[] bytes = ConvertToByte(Coverage);
			MemoryStream mem = new MemoryStream();
			BinaryFormatter binf = new BinaryFormatter();
			binf.Serialize(mem, bytes);
			string blob = Convert.ToBase64String(SCAN_CLZF2.Compress(mem.ToArray()));
			return blob.Replace("/", "-").Replace("=", "_");
		}

		internal void shortDeserialize(string blob)
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
				Coverage = new Int16[360, 180];
				throw e;
			}
		}

		#endregion

		#region Serialized Data Upgrade

		private Int32[,] oldCoverage;

		public void ConvertStorage(string blob)
		{
			SCANUtil.SCANlog("[SCANsat Legacy Conversion] Version < 19.0 detected; converting old scan data for {0}...", KSP.Localization.Localizer.Format(body.displayName));

			integerDeserializeUpgrade(blob);

			convertToShort(oldCoverage);
		}

		//Convert byte array from persistent file to usable Int32[]
		private Int32[,] ConvertToIntUpgrade(byte[] bArray)
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

		private void integerDeserializeUpgrade(string blob)
		{
			try
			{
				blob = blob.Replace("-", "/").Replace("_", "=");
				byte[] bytes = Convert.FromBase64String(blob);
				bytes = SCAN_CLZF2.Decompress(bytes);
				MemoryStream mem = new MemoryStream(bytes, false);
				BinaryFormatter binf = new BinaryFormatter();
				byte[] bArray = (byte[])binf.Deserialize(mem);
				oldCoverage = ConvertToIntUpgrade(bArray);
			}
			catch (Exception e)
			{
				oldCoverage = new Int32[360, 180];
				throw e;
			}
		}

		private void convertToShort(Int32[,] iArray)
		{
			coverage = new Int16[360, 180];

			SCANUtil.SCANlog("[SCANsat Legacy Conversion] Converting legacy integer array to short array...");

			for (int i = 0; i < 360; i++)
			{
				for (int j = 0; j < 180; j++)
				{
					if ((oldCoverage[i, j] & (int)SCANtype.AltimetryLoRes) != 0)
					{
						coverage[i, j] |= (short)SCANtype.AltimetryLoRes;
					}

					if ((oldCoverage[i, j] & (int)SCANtype.AltimetryHiRes) != 0)
					{
						coverage[i, j] |= (short)SCANtype.AltimetryHiRes;
					}

					if ((oldCoverage[i, j] & (int)SCANtype.Biome) != 0)
					{
						coverage[i, j] |= (short)SCANtype.Biome;
					}

					if ((oldCoverage[i, j] & (int)SCANtype.Anomaly) != 0)
					{
						coverage[i, j] |= (short)SCANtype.Anomaly;
					}

					if ((oldCoverage[i, j] & (int)SCANtype.AnomalyDetail) != 0)
					{
						coverage[i, j] |= (short)SCANtype.AnomalyDetail;
					}

					if ((oldCoverage[i, j] & (int)SCANtype.ResourceHiRes) != 0)
					{
						coverage[i, j] |= (short)SCANtype.ResourceHiRes;
					}

					if ((oldCoverage[i, j] & 524288) != 0)
					{
						coverage[i, j] |= (short)SCANtype.ResourceLoRes;
					}
				}
			}

			oldCoverage = null;
		}

		#endregion
	}
}
