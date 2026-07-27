using System;
using System.Collections;
using System.IO;
using System.Threading;
using UnityEngine;
using SCANsat.SCAN_Data;
using SCANsat.SCAN_Platform;
using Log = KSPBuildTools.Log;

namespace SCANsat.SCAN_Map
{
	public class SCANmapExporter : SCAN_MBE
	{
		private bool exporting;
		private volatile bool threadRunning, threadFinished;

		public bool Exporting
		{
			get { return exporting; }
		}

		public void exportPNG(SCANmap map, SCANdata data)
		{
			if (map == null || data == null)
			{
				return;
			}

			exporting = true;

			string path = Path.Combine(new DirectoryInfo(KSPUtil.ApplicationRootPath).FullName, "GameData/SCANsat/PluginData/").Replace("\\", "/");
			string mode = "";

			switch (map.MType)
			{
				case mapType.Altimetry: mode = "elevation"; break;
				case mapType.Slope: mode = "slope"; break;
				case mapType.Biome: mode = "biome"; break;
				case mapType.Visual: mode = "visual"; break;
			}

			if (map.ResourceActive && SCANconfigLoader.GlobalResource && !string.IsNullOrEmpty(SCANcontroller.controller.bigMapResource))
			{
				mode += "-" + SCANcontroller.controller.bigMapResource;
			}

			if (!SCANcontroller.controller.bigMapColor)
			{
				mode += "-grey";
			}

			string baseFileName = string.Format("{0}_{1}_{2}x{3}", map.Body.bodyName, mode, map.Map.width, map.Map.height);

			if (map.Projection != MapProjection.Rectangular)
			{
				baseFileName += "_" + map.Projection.ToString();
			}

			string filename = baseFileName + ".png";

			string fullPath = Path.Combine(path, filename);

			File.WriteAllBytes(fullPath, map.Map.EncodeToPNG());

			ScreenMessages.PostScreenMessage("SCANsat Map saved: GameData/SCANsat/PluginData/" + filename, 8, ScreenMessageStyle.UPPER_CENTER);

			SCANUtil.SCANlog("Map of [{0}] saved\nMap Size: {1} X {2}\nMinimum Altitude: {3:F0}m; Maximum Altitude: {4:F0}m\nPixel Width At Equator: {5:F6}m", map.Body.displayName.LocalizeBodyName(), map.Map.width, map.Map.height, data.TerrainConfig.MinTerrain, data.TerrainConfig.MaxTerrain, (map.Body.Radius * 2 * Math.PI) / (map.Map.width * 1f));

			if (SCAN_Settings_Config.Instance.ExportCSV && map.MType != mapType.Visual)
			{
				StartCoroutine(exportCSV(path, baseFileName, map, data, map.ResourceActive && SCANconfigLoader.GlobalResource && map.Resource != null));
			}
			else
			{
				exporting = false;
			}
		}

		private IEnumerator exportCSV(string filePath, string fileName, SCANmap map, SCANdata data, bool resourceActive)
		{
			int timer = 0;

			SCANdata copy = new SCANdata(data);

			float[,] copyHeightMap = null;
			if (map.MType == mapType.Altimetry)
			{
				copyHeightMap = new float[map.MapWidth, map.MapHeight];
				Array.Copy(map.Big_HeightMap, copyHeightMap, map.MapWidth * map.MapHeight);
			}

			float[,] copySlopeMap = null;
			if (map.MType == mapType.Slope)
			{
				copySlopeMap = new float[map.MapWidth, map.MapHeight];
				Array.Copy(map.Big_SlopeMap, copySlopeMap, map.MapWidth * map.MapHeight);
			}

			int width = map.MapWidth;
			int height = map.MapHeight;
			double scale = map.MapScale;
			mapType mode = map.MType;

			Thread t = new Thread(() => exportThread(filePath, fileName, width, height, scale, map, copy, copyHeightMap, copySlopeMap, mode, resourceActive));
			threadFinished = false;
			threadRunning = true;
			t.Start();

			while (threadRunning && timer < 20000)
			{
				timer++;
				yield return null;
			}

			SCANUtil.SCANlog(".csv data file export complete; exported over {0} frames\nFile saved to GameData/SCANsat/PluginData/{1}_data.csv", timer, fileName);

			copy = null;
			copyHeightMap = null;
			copySlopeMap = null;
			exporting = false;

			if (timer >= 20000)
			{
				Log.Error("Something went wrong while exporting .csv data file\nCanceling export thread...");
				t.Abort();
				threadRunning = false;
				yield break;
			}

			if (!threadFinished)
			{
				Log.Error("Something went wrong while exporting .csv data file\nExport thread has been interrupted...");
				yield break;
			}
		}

		private void exportThread(string path, string fileName, int w, int h, double s, SCANmap map, SCANdata copyData,
			float[,] copyHeightMap, float[,] copySlopeMap, mapType mode, bool resourceActive)
		{
			try
			{
				using (StreamWriter writer = new StreamWriter(Path.Combine(path, fileName + "_data" + ".csv")))
				{
					string line = "Row,Column,Lat,Long";
					switch (mode)
					{
						case mapType.Altimetry: line += ",Height"; break;
						case mapType.Slope: line += ",SlopeDisplayValue"; break;
						case mapType.Biome: line += ",Biome"; break;
					}

					if (resourceActive)
					{
						line += ",ResourceAbundancePercent";
					}

					writer.WriteLine(line);
					for (int i = 0; i < h; i++)
					{
						for (int j = 0; j < w; j++)
						{
							double lat = (i * 1.0f / s) - 90f;
							double lon = (j * 1.0f / s) - 180f;
							double la = lat, lo = lon;
							lat = map.unprojectLatitude(lo, la);
							lon = map.unprojectLongitude(lo, la);

							if (double.IsNaN(lat) || double.IsNaN(lon) || lat < -90 || lat > 90 || lon < -180 || lon > 180)
							{
								continue;
							}

							SCANtype coverage = mode == mapType.Biome ? SCANtype.Biome : SCANtype.Altimetry;
							if (!SCANUtil.isCovered(lon, lat, copyData, coverage))
							{
								continue;
							}

							switch (mode)
							{
								case mapType.Altimetry:
									float terrain = map.terrainElevation(lon, lat, w, h, copyHeightMap, copyData, true);
									line = string.Format("{0},{1},{2:F3},{3:F3},{4:F3}", i, j, lat, lon, terrain);
									break;
								case mapType.Slope:
									line = string.Format("{0},{1},{2:F3},{3:F3},{4:F6}", i, j, lat, lon, copySlopeMap[j, i]);
									break;
								case mapType.Biome:
									string biome = SCANUtil.getBiomeName(map.Body, lon, lat).Replace("\"", "\"\"");
									line = string.Format("{0},{1},{2:F3},{3:F3},\"{4}\"", i, j, lat, lon, biome);
									break;
								default:
									continue;
							}

							if (resourceActive)
							{
								if (SCANUtil.isCovered(lon, lat, copyData, SCANtype.ResourceHiRes) ||
									SCANUtil.isCovered(lon, lat, copyData, SCANtype.ResourceLoRes))
								{
									line += string.Format(",{0:F3}", map.getResoureCache(lon, lat));
								}
								else
								{
									line += ",";
								}
							}

							writer.WriteLine(line);
						}
						writer.Flush();
					}
				}
				threadFinished = true;
			}
			catch
			{
				threadFinished = false;
			}
			finally
			{
				threadRunning = false;
			}
		}
	}
}
