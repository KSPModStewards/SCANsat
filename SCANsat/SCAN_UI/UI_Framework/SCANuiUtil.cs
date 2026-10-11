#region license
/* 
 *  [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANsat - UI Utilities methods
 * 
 * Copyright (c)2013 damny;
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 *
 */
#endregion

using System;
using System.Linq;
using System.Text;
using SCANsat.SCAN_Data;
using SCANsat.SCAN_Map;
using SCANsat.SCAN_Unity;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANcolorUtil;
using UnityEngine;

namespace SCANsat.SCAN_UI.UI_Framework
{
	static class SCANuiUtil
	{
		#region UI Utilities

		internal static string getResourceAbundance(CelestialBody Body, double lat, double lon, bool fuzzy, SCANresourceGlobal resource)
		{
			if (fuzzy)
			{
				return resourceLabel(true, lat, lon, resource, Body);
			}
			else if (narrowBandInOrbit(Body, lat, resource))
			{
				return resourceLabel(false, lat, lon, resource, Body);
			}
			else
			{
				return resourceLabel(true, lat, lon, resource, Body);
			}
		}

		internal static bool narrowBandInOrbit(CelestialBody b, double lat, SCANresourceGlobal resource)
		{
			if (SCAN_Settings_Config.Instance.RequireNarrowBand)
			{
				if (resource == null)
				{
					return false;
				}

				bool scanner = false;

				foreach (Vessel vessel in FlightGlobals.Vessels)
				{
					if (vessel.protoVessel.protoPartSnapshots.Count <= 1)
					{
						continue;
					}

					VesselType vType = vessel.vesselType;

					if (vType == VesselType.Debris || vType == VesselType.Unknown
						|| vType == VesselType.EVA || vType == VesselType.Flag
						|| vType == VesselType.DeployedScienceController || vType == VesselType.DeployedSciencePart)
					{
						continue;
					}

					if (vessel.mainBody != b)
					{
						continue;
					}

					if ((vessel.loaded ? vessel.situation : vessel.protoVessel.situation) != Vessel.Situations.ORBITING)
					{
						continue;
					}

					if ((inc(vessel.orbit.inclination) + 5) < Math.Abs(lat))
					{
						continue;
					}

					if (SCAN_Settings_Config.Instance.DisableStockResource)
					{
						var scanners = from pref in vessel.protoVessel.protoPartSnapshots
									   where pref.modules.Any(a => a.moduleName == "SCANsat")
									   select pref;

						if (scanners.Count() > 0)
						{
							foreach (var p in scanners)
							{
								if (p.partInfo == null)
								{
									continue;
								}

								ConfigNode node = p.partInfo.partConfig;

								if (node == null)
								{
									continue;
								}

								var moduleNodes = from nodes in node.GetNodes("MODULE")
												  where nodes.GetValue("name") == "SCANsat"
												  select nodes;

								foreach (ConfigNode moduleNode in moduleNodes)
								{
									if (moduleNode == null)
									{
										continue;
									}

									if (!moduleNode.HasValue("sensorType"))
									{
										continue;
									}

									string type = moduleNode.GetValue("sensorType");

									int sType = 0;

									if (!int.TryParse(type, out sType))
									{
										continue;
									}

									if (((SCANtype)sType & SCANtype.ResourceHiRes) == SCANtype.Nothing)
									{
										continue;
									}

									if (moduleNode.HasValue("max_alt") && !vessel.Landed)
									{
										string alt = moduleNode.GetValue("max_alt");

										float f = 0;

										if (!float.TryParse(alt, out f))
										{
											continue;
										}

										if (f < vessel.altitude)
										{
											scanner = false;
											continue;
										}
									}

									scanner = true;
									break;
								}
								if (scanner)
								{
									break;
								}
							}
							if (scanner)
							{
								break;
							}
						}

						scanners = from pref in vessel.protoVessel.protoPartSnapshots
								   where pref.modules.Any(a => a.moduleName == "ModuleSCANresourceScanner")
								   select pref;

						if (scanners.Count() > 0)
						{
							foreach (var p in scanners)
							{
								if (p.partInfo == null)
								{
									continue;
								}

								ConfigNode node = p.partInfo.partConfig;

								if (node == null)
								{
									continue;
								}

								var moduleNodes = from nodes in node.GetNodes("MODULE")
												  where nodes.GetValue("name") == "ModuleSCANresourceScanner"
												  select nodes;

								foreach (ConfigNode moduleNode in moduleNodes)
								{
									if (moduleNode == null)
									{
										continue;
									}

									if (!moduleNode.HasValue("sensorType"))
									{
										continue;
									}

									string type = moduleNode.GetValue("sensorType");

									int sType = 0;

									if (!int.TryParse(type, out sType))
									{
										continue;
									}

									if (((SCANtype)sType & SCANtype.ResourceHiRes) == SCANtype.Nothing)
									{
										continue;
									}

									if (moduleNode.HasValue("max_alt") && !vessel.Landed)
									{
										string alt = moduleNode.GetValue("max_alt");

										float f = 0;

										if (!float.TryParse(alt, out f))
										{
											continue;
										}

										if (f < vessel.altitude)
										{
											scanner = false;
											continue;
										}
									}

									scanner = true;
									break;
								}
								if (scanner)
								{
									break;
								}
							}
							if (scanner)
							{
								break;
							}
						}
					}
					else
					{
						if (vessel.altitude > 1000000)
						{
							continue;
						}

						var scanners = from pref in vessel.protoVessel.protoPartSnapshots
									   where pref.modules.Any(a => a.moduleName == "ModuleKerbNetAccess")
									   select pref;

						if (scanners.Count() == 0)
						{
							continue;
						}

						foreach (var p in scanners)
						{
							if (p.partInfo == null)
							{
								continue;
							}

							ConfigNode node = p.partInfo.partConfig;

							if (node == null)
							{
								continue;
							}

							var moduleNodes = from nodes in node.GetNodes("MODULE")
											  where nodes.GetValue("name") == "ModuleKerbNetAccess"
											  select nodes;

							foreach (ConfigNode moduleNode in moduleNodes)
							{
								if (!moduleNode.HasNode("DISPLAY_MODES"))
								{
									continue;
								}

								ConfigNode displayMode = moduleNode.GetNode("DISPLAY_MODES");

								if (!displayMode.HasValue("Mode"))
								{
									continue;
								}

								foreach (string mode in displayMode.GetValues("Mode"))
								{
									string[] subMode = mode.Split(',');

									if (subMode[0].Trim() != "Resources")
									{
										continue;
									}

									scanner = true;
									break;
								}

								if (scanner)
								{
									break;
								}
							}
							if (scanner)
							{
								break;
							}
						}
						if (scanner)
						{
							break;
						}
					}
				}

				if (!scanner)
				{
					return false;
				}
			}

			return true;
		}

		internal static string resourceLabel(bool fuzz, double lat, double lon, SCANresourceGlobal resource, CelestialBody b)
		{
			if (fuzz)
			{
				return string.Format("{0}: {1}", resource.DisplayName, LoResourceGroup(SCANUtil.ResourceOverlay(lat, lon, resource.Name, b, SCAN_Settings_Config.Instance.BiomeLock)));
			}
			else
			{
				return string.Format("{0}: {1}", resource.DisplayName, SCANUtil.ResourceOverlay(lat, lon, resource.Name, b, SCAN_Settings_Config.Instance.BiomeLock).ToString("P2"));
			}
		}

		internal static string LoResourceGroup(float abundance)
		{
			if (abundance > 0)
			{
				abundance = Mathf.Floor(abundance * 100 / 5) * 5;

				return string.Format("{0}-{1}%", abundance.ToString("F0"), (abundance + 5).ToString("F0"));
			}
			else
			{
				return "0%";
			}
		}

		private static double inc(double d)
		{
			d = Math.Abs(d);

			if (d > 90)
			{
				d = 180 - d;
			}

			return d;
		}

		internal static string getMouseOverElevation(double Lon, double Lat, SCANdata d, int precision, bool high)
		{
			if (high)
			{
				return string.Format("{0}m", SCANUtil.getElevation(d.Body, Lon, Lat).ToString(string.Format("N{0}", precision.ToString())));
			}
			else
			{
				return string.Format("{0}m", (((int)SCANUtil.getElevation(d.Body, Lon, Lat) / 500) * 500).ToString());
			}
		}

		internal static void getMouseOverElevation(StringBuilder sb, double Lon, double Lat, SCANdata d, int precision, bool high)
		{

			if (high)
			{
				sb.Append(SCANUtil.getElevation(d.Body, Lon, Lat).ToString(string.Format("N{0}", precision.ToString())));
				sb.Append("m");
			}
			else
			{
				sb.Append((((int)SCANUtil.getElevation(d.Body, Lon, Lat) / 500) * 500).ToString());
				sb.Append("m");
			}
		}

		private const char WEST = 'W';
		private const char EAST = 'E';
		private const char SOUTH = 'S';
		private const char NORTH = 'N';
		private const char HOURS = '°';
		private const string MINUTES = "'";
		private const string SECONDS = "\"";

		/* UI: conversions to and from DMS */
		/* FIXME: These do not belong here. And they are only used once! */

		private static void toDMS(StringBuilder sb, double thing, char neg, char pos, string precFormat = "F2")
		{
			if (thing >= 0)
			{
				neg = pos;
			}

			thing = Math.Abs(thing);

			sb.Append(Math.Floor(thing).ToString());
			sb.Append(HOURS);
			thing = (thing - Math.Floor(thing)) * 60;

			sb.Append(Math.Floor(thing).ToString());
			sb.Append(MINUTES);
			thing = (thing - Math.Floor(thing)) * 60;

			sb.Append(thing.ToString(precFormat));
			sb.Append(SECONDS);

			sb.Append(neg);
		}

		internal static void toDMS(StringBuilder sb, double lat, double lon)
		{
			toDMS(sb, lat, SOUTH, NORTH);
			sb.Append(" ");
			toDMS(sb, lon, WEST, EAST);
		}

		internal static string distanceString(double dist, double cutoff, double cutoff2 = double.MaxValue)
		{
			if (dist < cutoff)
			{
				return string.Format("{0}m", dist.ToString("N1"));
			}
			else if (dist < cutoff2)
			{
				return string.Format("{0}km", (dist / 1000d).ToString("N2"));
			}
			else
			{
				return string.Format("{0}km", (dist / 1000d).ToString("N0"));
			}
		}

		//Reset window positions;
		internal static void resetMainMapPos()
		{
			if (SCAN_UI_MainMap.Instance == null)
			{
				return;
			}

			SCAN_UI_MainMap.Instance.ResetPosition();
		}

		internal static void resetInstUIPos()
		{
			if (SCAN_UI_Instruments.Instance == null)
			{
				return;
			}

			SCAN_UI_Instruments.Instance.ResetPosition();
		}

		internal static void resetBigMapPos()
		{
			if (SCAN_UI_BigMap.Instance == null)
			{
				return;
			}

			SCAN_UI_BigMap.Instance.ResetPosition();
		}

		internal static void resetOverlayControllerPos()
		{
			if (SCAN_UI_Overlay.Instance == null)
			{
				return;
			}

			SCAN_UI_Overlay.Instance.ResetPosition();
		}

		internal static void resetZoomMapPos()
		{
			if (SCAN_UI_ZoomMap.Instance == null)
			{
				return;
			}

			SCAN_UI_ZoomMap.Instance.ResetPosition();
		}

		#endregion

		#region Texture/Icon labels

		internal static void drawMapIconGL(Rect pos, Texture2D tex, Color c, Material iconMat, Color shadow = new Color(), bool outline = false, Rect texPos = new Rect(), bool texCoords = false)
		{
			if (texCoords)
			{
				if (outline)
				{
					iconMat.color = shadow;
					pos.x -= 1;
					Graphics.DrawTexture(pos, tex, texPos, 0, 0, 0, 0, iconMat);
					pos.x += 2;
					Graphics.DrawTexture(pos, tex, texPos, 0, 0, 0, 0, iconMat);
					pos.x -= 1;
					pos.y -= 1;
					Graphics.DrawTexture(pos, tex, texPos, 0, 0, 0, 0, iconMat);
					pos.y += 2;
					Graphics.DrawTexture(pos, tex, texPos, 0, 0, 0, 0, iconMat);
					pos.y -= 1;
				}
				iconMat.color = c;

				Graphics.DrawTexture(pos, tex, texPos, 0, 0, 0, 0, iconMat);
			}
			else
			{
				if (outline)
				{
					iconMat.color = shadow;
					pos.x -= 1;
					Graphics.DrawTexture(pos, tex, 0, 0, 0, 0, iconMat);
					pos.x += 2;
					Graphics.DrawTexture(pos, tex, 0, 0, 0, 0, iconMat);
					pos.x -= 1;
					pos.y -= 1;
					Graphics.DrawTexture(pos, tex, 0, 0, 0, 0, iconMat);
					pos.y += 2;
					Graphics.DrawTexture(pos, tex, 0, 0, 0, 0, iconMat);
					pos.y -= 1;
				}
				iconMat.color = c;

				Graphics.DrawTexture(pos, tex);
			}

		}

		#endregion

		#region MechJeb Target Overlay

		/*These methods borrowed from MechJeb GLUtils: 
		 * https://github.com/MuMech/MechJeb2/blob/master/MechJeb2/GLUtils.cs
		 * 
		*/
		internal static void drawTargetOverlay(CelestialBody body, double latitude, double longitude, Color c)
		{
			double rotation = 0;
			double radius = 0;
			Vector3d up = body.GetSurfaceNVector(latitude, longitude);
			var height = SCANUtil.getElevation(body, longitude, latitude);
			if (height < body.Radius)
			{
				height = body.Radius;
			}

			Vector3d center = body.position + height * up;

			if (occluded(center, body))
			{
				return;
			}

			Vector3d north = Vector3d.Exclude(up, body.transform.up).normalized;

			radius = body.Radius / 15;

			GLTriangleMap(new Vector3d[3] { center, center + radius * (QuaternionD.AngleAxis(rotation - 55, up) * north), center + radius * (QuaternionD.AngleAxis(rotation - 35, up) * north) }, c);

			GLTriangleMap(new Vector3d[3] { center, center + radius * (QuaternionD.AngleAxis(rotation + 55, up) * north), center + radius * (QuaternionD.AngleAxis(rotation + 35, up) * north) }, c);

			GLTriangleMap(new Vector3d[3] { center, center + radius * (QuaternionD.AngleAxis(rotation - 145, up) * north), center + radius * (QuaternionD.AngleAxis(rotation - 125, up) * north) }, c);

			GLTriangleMap(new Vector3d[3] { center, center + radius * (QuaternionD.AngleAxis(rotation + 145, up) * north), center + radius * (QuaternionD.AngleAxis(rotation + 125, up) * north) }, c);
		}

		internal static void drawGroundTrackTris(CelestialBody body, Vessel v, double width, Color c)
		{
			double lat = SCANUtil.fixLatShift(v.latitude);
			double lon = SCANUtil.fixLonShift(v.longitude);

			Vector3d center = v.transform.position;

			if (occluded(center, body))
			{
				return;
			}

			var height = SCANUtil.getElevation(body, lon, lat);
			if (height < body.Radius)
			{
				height = body.Radius;
			}

			Vector3d up = body.GetSurfaceNVector(lat, lon);

			Vector3d srfCenter = body.position + height * up;

			Vector3d VelFor = Vector3.ProjectOnPlane(v.obt_velocity, up).normalized;
			Vector3d vesselPerp = Vector3d.Cross(VelFor, up).normalized;

			Vector3d left = srfCenter + width * vesselPerp;
			Vector3d right = srfCenter - width * vesselPerp;

			GLTriangleMap(new Vector3d[3] { center, left, right }, c);
		}

		private static bool occluded(Vector3d pos, CelestialBody body)
		{
			if (Vector3d.Distance(pos, body.position) < body.Radius - 100)
			{
				return true;
			}

			Renderer scaledR = body.scaledBody.GetComponent<Renderer>();

			if (!scaledR.isVisible)
			{
				return true;
			}

			Vector3d camPos = ScaledSpace.ScaledToLocalSpace(PlanetariumCamera.Camera.transform.position);

			if (Vector3d.Angle(camPos - pos, body.position - pos) > 90)
			{
				return false;
			}

			double bodyDistance = Vector3d.Distance(camPos, body.position);
			double separationAngle = Vector3d.Angle(pos - camPos, body.position - camPos);
			double altitude = bodyDistance * Math.Sin(Math.PI / 180 * separationAngle);
			return (altitude < body.Radius);
		}

		private static Material mat;

		private static void GLTriangleMap(Vector3d[] vert, Color c)
		{
			GL.PushMatrix();
			if (mat == null)
			{
				mat = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
			}

			mat.SetPass(0);
			GL.LoadOrtho();
			GL.Begin(GL.TRIANGLES);
			GL.Color(c);
			GLVertexMap(vert[0]);
			GLVertexMap(vert[1]);
			GLVertexMap(vert[2]);
			GL.End();
			GL.PopMatrix();
		}

		private static void GLVertexMap(Vector3d pos)
		{
			Vector3 screenPoint = PlanetariumCamera.Camera.WorldToViewportPoint(ScaledSpace.LocalToScaledSpace(pos));
			GL.Vertex3(screenPoint.x, screenPoint.y, 0);
		}

		#endregion

		#region Planet Overlay Textures

		// unproject: the grid is the map's own window in pixel space (a window map), so each cell is
		// sampled at its unprojected coordinate; else the grid is the globe (the big map, the planet
		// overlay), and the raw coordinate is the sample coordinate whatever projection the map shows.
		internal static void generateResourceCache(ref float[,] values, int height, int width, int stepScale, double scale, SCANmap map, bool unproject)
		{
			for (int j = 0; j < height; j += stepScale)
			{
				for (int i = 0; i < width; i += stepScale)
				{
					Vector2d coords;
					if (unproject)
					{
						double rLon = (i * 1.0f / scale) - 180f + map.Lon_Offset;
						double rLat = (j * 1.0f / scale) - 90f + map.Lat_Offset;

						double la = rLat, lo = rLon;
						rLat = map.unprojectLatitude(lo, la);
						rLon = map.unprojectLongitude(lo, la);

						if (double.IsNaN(rLat) || double.IsNaN(rLon) || rLat < -90 || rLat > 90 || rLon < -180 || rLon > 180)
						{
							values[i, j] = 0;
							continue;
						}

						coords = new Vector2d(rLon, rLat);
					}
					else
					{
						double rLon = SCANUtil.fixLonShift((i * 1.0f / scale) - 180f + map.Lon_Offset);
						double rLat = (j * 1.0f / scale) - 90f + map.Lat_Offset;
						coords = SCANUtil.fixRetardCoordinates(new Vector2d(rLon, rLat));
					}

					values[i, j] = SCANUtil.ResourceOverlay(coords.y, coords.x, map.Resource.Name, map.Body, SCAN_Settings_Config.Instance.BiomeLock) * 100f;
				}
			}
		}

		//internal static void generateResourceCache(Texture2D tex, int height, int width, int stepScale, double scale, SCANmap map)
		//{
		//	for (int j = 0; j < height; j += stepScale)
		//	{
		//		for (int i = 0; i < width; i += stepScale)
		//		{
		//			Vector2d coords;
		//			if (map.Projection == MapProjection.Orthographic)
		//			{
		//				double rLon = (i * 1.0f / scale) - 180f + map.Lon_Offset;
		//				double rLat = (j * 1.0f / scale) - 90f + map.Lat_Offset;

		//				double la = rLat, lo = rLon;
		//				rLat = map.unprojectLatitude(lo, la);
		//				rLon = map.unprojectLongitude(lo, la);

		//				if (double.IsNaN(rLat) || double.IsNaN(rLon) || rLat < -90 || rLat > 90 || rLon < -180 || rLon > 180)
		//				{
		//					tex.SetPixel(i, j, palette.clear);
		//					continue;
		//				}

		//				coords = new Vector2d(rLon, rLat);
		//			}
		//			else
		//			{
		//				double rLon = SCANUtil.fixLonShift((i * 1.0f / scale) - 180f + map.Lon_Offset);
		//				double rLat = (j * 1.0f / scale) - 90f + map.Lat_Offset;
		//				coords = SCANUtil.fixRetardCoordinates(new Vector2d(rLon, rLat));
		//			}

		//			float abundance = SCANUtil.ResourceOverlay(coords.y, coords.x, map.Resource.Name, map.Body, SCAN_Settings_Config.Instance.BiomeLock) * 100f;

		//			tex.SetPixel(i, j, resourceToColor(palette.clear, map.Resource, abundance, );
		//		}
		//	}
		//}

		private static float getLerp(System.Random rand, int l)
		{
			if (l == 0)
			{
				return 0.5f;
			}

			return (float)l / 100f + (float)rand.Next(100 - (l / 2)) / 100f;
		}

		internal static void interpolate(float[,] v, int height, int width, int x, int y, int step, System.Random r, bool softEdges, bool hardEdges = false)
		{
			for (int j = y; j < height + y; j += 2 * step)
			{
				// Latitude has no wrap, so a neighbour off the top or bottom mirrors to the one on the
				// other side rather than clamping to the edge row. Clamping folded j itself into the
				// vertical average, and at the coarsest pass the row it clamped to had not been written
				// yet - so a zero leaked in, and every finer pass spread it further down the cap. On a
				// body with uniform abundance the north cap decayed to 12 percent of the truth by the
				// top row and the south pole row read 69 percent; mirroring keeps both ends an average
				// of two cells that hold real data. generateResourceCache samples no higher than
				// height - stepScale, so the rows above the last sample are a flat extension of it.
				int ypos1 = j - step;
				int ypos2 = j + step;

				if (ypos1 < 0)
				{
					ypos1 = ypos2 < height ? ypos2 : j;
				}

				if (ypos2 >= height)
				{
					ypos2 = ypos1;
				}

				for (int i = x; i < width + x; i += 2 * step)
				{
					int xpos1 = i - step;
					int xpos2 = i + step;

					// The same mirror along the other axis for a window map (hardEdges), whose left and
					// right edges are real edges and were clamping onto the cell being written. A
					// full-globe map wraps at the seam instead, which was already right.
					if (xpos1 < 0)
					{
						if (hardEdges)
						{
							xpos1 = xpos2 < width ? xpos2 : i;
						}
						else
						{
							xpos1 += width;
						}
					}

					if (xpos2 >= width)
					{
						if (hardEdges)
						{
							xpos2 = xpos1;
						}
						else
						{
							xpos2 -= width;
						}
					}

					float avgX = 0;
					float avgY = 0;

					float lerp = 0.5f;
					if (softEdges)
					{
						lerp = getLerp(r, step * 2);
					}

					if (x == y)
					{
						avgX = Mathf.Lerp(v[xpos1, ypos1], v[xpos2, ypos2], lerp);
						avgY = Mathf.Lerp(v[xpos1, ypos2], v[xpos2, ypos1], lerp);
					}
					else
					{
						avgX = Mathf.Lerp(v[xpos1, j], v[xpos2, j], lerp);
						avgY = Mathf.Lerp(v[i, ypos2], v[i, ypos1], lerp);
					}

					float avgFinal = Mathf.Lerp(avgX, avgY, lerp);

					v[i, j] = avgFinal;
				}
			}
		}

		/* Converts resource amount to pixel color */
		internal static Color resourceToColor(Color BaseColor, SCANresourceGlobal Resource, float Abundance, SCANdata Data, double Lon, double Lat)
		{
			if (SCANUtil.isCovered(Lon, Lat, Data, SCANtype.ResourceHiRes))
			{
				if (Abundance >= Resource.CurrentBody.MinValue)
				{
					if (Abundance > Resource.CurrentBody.MaxValue)
					{
						Abundance = Resource.CurrentBody.MaxValue;
					}
				}
				else
				{
					Abundance = 0;
				}
			}
			else if (SCANUtil.isCovered(Lon, Lat, Data, SCANtype.ResourceLoRes))
			{
				Abundance = Mathf.RoundToInt(Abundance);
				if (Abundance >= Resource.CurrentBody.MinValue)
				{
					if (Abundance > Resource.CurrentBody.MaxValue)
					{
						Abundance = Resource.CurrentBody.MaxValue;
					}
				}
				else
				{
					Abundance = 0;
				}
			}
			else
			{
				return BaseColor;
			}

			if (Abundance == 0)
			{
				return palette.lerp(BaseColor, palette.grey, 0.3f);
			}
			else
			{
				return palette.lerp(palette.lerp(Resource.MinColor, Resource.MaxColor, (Abundance - Resource.CurrentBody.MinValue) / (Resource.CurrentBody.MaxValue - Resource.CurrentBody.MinValue)), BaseColor, Resource.Transparency / 100f);
			}
		}

		internal static Color32 resourceToColor32(Color32 BaseColor, SCANresourceGlobal Resource, float MinRange, float MaxRange, float Abundance, SCANdata Data, double Lon, double Lat, float Transparency = 0.3f)
		{
			if (SCANUtil.isCovered(Lon, Lat, Data, SCANtype.ResourceHiRes))
			{
				if (Abundance >= MinRange)
				{
					if (Abundance > MaxRange)
					{
						Abundance = MaxRange;
					}
				}
				else
				{
					Abundance = 0;
				}
			}
			else if (SCANUtil.isCovered(Lon, Lat, Data, SCANtype.ResourceLoRes))
			{
				if (Abundance > 0)
				{
					Abundance = Mathf.Floor(Abundance / 5) * 5 + 2.5f;

					if (Abundance >= MinRange)
					{
						if (Abundance > MaxRange)
						{
							Abundance = MaxRange;
						}
					}
					else
					{
						Abundance = 0;
					}
				}
			}
			else
			{
				return BaseColor;
			}

			if (Abundance == 0)
			{
				return palette.lerp(BaseColor, palette.Grey, Transparency);
			}
			else
			{
				return palette.lerp(palette.lerp(Resource.MinColor32, Resource.MaxColor32, (Abundance - MinRange) / (MaxRange - MinRange)), BaseColor, Resource.Transparency / 100f);
			}
		}

		#endregion
	}
}
