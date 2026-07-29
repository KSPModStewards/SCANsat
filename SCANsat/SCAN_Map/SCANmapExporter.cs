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
		private volatile int exportedRows;
		private volatile string threadError;

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

			string path = Path.Combine(new DirectoryInfo(KSPUtil.ApplicationRootPath).FullName, "GameData/SCANsat/PluginData/").Replace("\\", "/");
			string mode = "";

			switch (map.MType)
			{
				case mapType.Altimetry: mode = "elevation"; break;
				case mapType.Slope: mode = "slope"; break;
				case mapType.Biome: mode = "biome"; break;
				case mapType.Visual: mode = "visual"; break;
			}

			if (map.ResourceActive && SCANconfigLoader.GlobalResource && map.Resource != null)
			{
				mode += "-" + map.Resource.Name;
			}

			if (!map.ColorMap)
			{
				mode += "-grey";
			}

			string baseFileName = string.Format("{0}_{1}_{2}x{3}", map.Body.bodyName, mode, map.Map.width, map.Map.height);

			if (map.Projection != MapProjection.Rectangular)
			{
				baseFileName += "_" + map.Projection.ToString();
			}

			if (map.MSource == mapSource.ZoomMap)
			{
				baseFileName += string.Format("_{0:F3}_{1:F3}_{2:F3}", map.CenteredLat, map.CenteredLong, map.MapScale);
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
			double latitudeOffset = 0;
			double longitudeOffset = 0;

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

			float[,] copyResourceMap = null;
			if (resourceActive)
			{
				copyResourceMap = new float[map.MapWidth, map.MapHeight];

				for (int y = 0; y < map.MapHeight; y++)
				{
					int resourceY = y * map.ResourceCache.GetLength(1) / map.MapHeight;

					for (int x = 0; x < map.MapWidth; x++)
					{
						int resourceX = x * map.ResourceCache.GetLength(0) / map.MapWidth;
						copyResourceMap[x, y] = map.ResourceCache[resourceX, resourceY];
					}
				}
			}

			int width = map.MapWidth;
			int height = map.MapHeight;
			double scale = map.MapScale;
			mapType mode = map.MType;

			if (map.MSource == mapSource.ZoomMap)
			{
				latitudeOffset = map.Lat_Offset;
				longitudeOffset = map.Lon_Offset;
			}

			exporting = true;

			Thread t = new Thread(() => exportThread(filePath, fileName, width, height, scale, latitudeOffset, longitudeOffset, map, copy, copyHeightMap, copySlopeMap, copyResourceMap, mode, resourceActive));
			exportedRows = 0;
			threadError = null;
			threadFinished = false;
			threadRunning = true;
			t.Start();

			while (threadRunning && timer < 50000)
			{
				timer++;

				if (timer % 60 == 0)
				{
					int rows = exportedRows;
					double percent = height > 0 ? rows * 100.0 / height : 0;

					ScreenMessages.PostScreenMessage(string.Format("SCANsat CSV export: {0:N0}/{1:N0} rows ({2:F1}%)", rows, height, percent), 1.1f, ScreenMessageStyle.UPPER_CENTER);
				}

				yield return null;
			}

			if (threadRunning)
			{
				Log.Error(string.Format("SCANsat CSV export timed out after {0} frames for [{1}_data.csv] at row {2} of {3}", timer, fileName, exportedRows, height));

				ScreenMessages.PostScreenMessage("SCANsat CSV export timed out; see KSP.log", 8, ScreenMessageStyle.UPPER_CENTER);

				t.Abort();
				while (threadRunning)
				{
					yield return null;
				}

				exporting = false;
				yield break;
			}

			if (!threadFinished)
			{
				Log.Error(string.Format("SCANsat CSV export failed for [{0}_data.csv]\n{1}", fileName, threadError ?? "No error details"));

				ScreenMessages.PostScreenMessage("SCANsat CSV export failed; see KSP.log", 8, ScreenMessageStyle.UPPER_CENTER);

				exporting = false;
				yield break;
			}

			SCANUtil.SCANlog(".csv data file export complete; exported over {0} frames\nFile saved to GameData/SCANsat/PluginData/{1}_data.csv", timer, fileName);

			ScreenMessages.PostScreenMessage("SCANsat CSV saved: GameData/SCANsat/PluginData/" + fileName + "_data.csv", 8, ScreenMessageStyle.UPPER_CENTER);

			exporting = false;
		}

		private void exportThread(string path, string fileName, int w, int h, double s, double latitudeOffset, double longitudeOffset, SCANmap map, SCANdata copyData, float[,] copyHeightMap, float[,] copySlopeMap, float[,] copyResourceMap, mapType mode, bool resourceActive)
		{
			try
			{
				using (StreamWriter writer = new StreamWriter(Path.Combine(path, fileName + "_data" + ".csv")))
				{
					string line = "Row,Column,Lat,Long";
					switch (mode)
					{
						case mapType.Altimetry: line += ",Height"; break;
						case mapType.Slope: line += ",Slope"; break;
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
							double lat = (i * 1.0d / s) - 90d + latitudeOffset;
							double lon = (j * 1.0d / s) - 180d + longitudeOffset;

							lat = map.unprojectLatitude(lon, lat);
							lon = map.unprojectLongitude(lon, lat);

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
									float terrain = copyHeightMap[j, i];
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
									line += string.Format(",{0:F3}", copyResourceMap[j, i]);
								}
								else
								{
									line += ",";
								}
							}

							writer.WriteLine(line);
						}
						writer.Flush();
						exportedRows = i + 1;
					}
				}

				threadFinished = true;
			}
			catch (Exception ex)
			{
				threadError = ex.ToString();
				threadFinished = false;
			}
			finally
			{
				threadRunning = false;
			}
		}
	}
}
