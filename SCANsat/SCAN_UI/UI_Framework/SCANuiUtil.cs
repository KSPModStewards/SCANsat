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
using SCANsat.SCAN_UI.UI_Framework;
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
		private static string toDMS(double thing, char neg, char pos, int prec)
		{
			StringBuilder sb = SCANStringBuilderCache.Acquire();

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

			sb.Append(thing.ToString(string.Format("F{0}", prec.ToString())));
			sb.Append(SECONDS);

			sb.Append(neg);

			return sb.SCANToStringAndRelease();
		}

		private static void toDMS(StringBuilder sb, double thing, char neg, char pos)
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

			sb.Append(thing.ToString("F2"));
			sb.Append(SECONDS);

			sb.Append(neg);
		}

		internal static string toDMS(double lat, double lon, int precision = 2)
		{
			return string.Format("{0} {1}", toDMS(lat, SOUTH, NORTH, precision), toDMS(lon, WEST, EAST, precision));
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

		private static double fixLon(double Lon)
		{
			if (Lon <= 180)
			{
				Lon = 180 - Lon;
			}
			else
			{
				Lon = (Lon - 180) * -1;
			}

			Lon -= 90;
			if (Lon < -180)
			{
				Lon += 360;
			}

			return Lon;
		}

		private static double unFixLon(double Lon)
		{
			Lon += 90;

			Lon = (Lon - 180) * -1;

			if (Lon < 0)
			{
				Lon += 360;
			}

			Lon -= 180;

			return Lon;
		}

		internal static void generateOverlayResourceValues(ref float[,] values, int height, SCANdata data, SCANresourceGlobal resource, int stepScale = 8)
		{
			int width = height * 2;
			float scale = height / 180f;

			if (values == null || height * width != values.Length)
			{
				values = new float[width, height];
			}

			for (int j = 0; j < height; j += stepScale)
			{
				double lat = (j / scale) - 90;
				for (int i = 0; i < width; i += stepScale)
				{
					double lon = fixLon(i / scale);

					values[i, j] = SCANUtil.ResourceOverlay(lat, lon, resource.Name, data.Body, SCAN_Settings_Config.Instance.BiomeLock) * 100;
				}
			}
		}

		internal static void generateOverlayResourcePixels(ref Color32[] pix, ref float[,] values, int height, SCANdata data, SCANresourceGlobal resource, System.Random r, int stepScale, float transparency = 0f)
		{
			int width = height * 2;
			float scale = height / 180f;

			if (pix == null || height * width != pix.Length)
			{
				pix = new Color32[width * height];
			}

			for (int i = stepScale / 2; i >= 1; i /= 2)
			{
				interpolate(values, height, width, i, i, i, r, true);
				interpolate(values, height, width, 0, i, i, r, true);
				interpolate(values, height, width, i, 0, i, r, true);
			}

			for (int i = 0; i < width; i++)
			{
				double lon = fixLon(i / scale);
				for (int j = 0; j < height; j++)
				{
					double lat = (j / scale) - 90;

					pix[j * width + i] = resourceToColor32(palette.Clear, resource, resource.CurrentBody.MinValue, resource.CurrentBody.MaxValue, values[i, j], data, lon, lat, transparency);
				}
			}
		}

		internal static Texture2D drawResourceTexture(ref Texture2D map, ref Color32[] pix, ref float[,] values, int height, SCANdata data, SCANresourceGlobal resource, int stepScale = 8, float transparency = 0f)
		{
			int width = height * 2;
			float scale = height / 180f;

			if (map == null || pix == null || values == null || map.height != height)
			{
				map = new Texture2D(width, height, TextureFormat.ARGB32, true);
				pix = new Color32[width * height];
				values = new float[width, height];
			}

			System.Random r = new System.Random(ResourceScenario.Instance.gameSettings.Seed);

			for (int j = 0; j < height; j += stepScale)
			{
				double lat = (j / scale) - 90;
				for (int i = 0; i < width; i += stepScale)
				{
					double lon = fixLon(i / scale);

					values[i, j] = SCANUtil.ResourceOverlay(lat, lon, resource.Name, data.Body, SCAN_Settings_Config.Instance.BiomeLock) * 100;
				}
			}

			for (int i = stepScale / 2; i >= 1; i /= 2)
			{
				interpolate(values, height, width, i, i, i, r, true);
				interpolate(values, height, width, 0, i, i, r, true);
				interpolate(values, height, width, i, 0, i, r, true);
			}

			for (int i = 0; i < width; i++)
			{
				double lon = fixLon(i / scale);
				for (int j = 0; j < height; j++)
				{
					double lat = (j / scale) - 90;

					pix[j * width + i] = resourceToColor32(palette.Clear, resource, resource.CurrentBody.MinValue, resource.CurrentBody.MaxValue, values[i, j], data, lon, lat, transparency);
				}
			}

			map.SetPixels32(pix);
			map.Apply();

			return map;
		}

		internal static Texture2D drawBiomeMap(ref Texture2D map, ref Color32[] pix, SCANdata data, float transparency, int height = 256, bool useStock = false, bool whiteBorder = false)
		{
			if (!useStock && !whiteBorder)
			{
				return drawBiomeMap(ref map, ref pix, data, height);
			}

			int width = height * 2;
			float scale = (width * 1f) / 360f;
			double[] mapline = new double[width];

			if (map == null || pix == null || map.height != height)
			{
				map = new Texture2D(width, height, TextureFormat.ARGB32, true);
				pix = new Color32[width * height];
			}

			for (int j = 0; j < height; j++)
			{
				double lat = (j / scale) - 90;
				for (int i = 0; i < width; i++)
				{
					double lon = fixLon(i / scale);

					if (!SCANUtil.isCovered(lon, lat, data, SCANtype.Biome))
					{
						pix[j * width + i] = palette.lerp(palette.Clear, palette.Grey, transparency);
						continue;
					}

					float biomeIndex = (float)SCANUtil.getBiomeIndexFraction(data.Body, lon, lat);

					if (whiteBorder && i > 0 && mapline[i - 1] != biomeIndex || (j > 0 && mapline[i] != biomeIndex))
					{
						pix[j * width + i] = palette.White;
					}
					else if (useStock)
					{
						pix[j * width + i] = palette.lerp((Color32)SCANUtil.getBiome(data.Body, lon, lat).mapColor, palette.Clear, SCAN_Settings_Config.Instance.BiomeTransparency);
					}
					else
					{
						pix[j * width + i] = palette.lerp(palette.lerp(SCANcontroller.controller.lowBiomeColor32, SCANcontroller.controller.highBiomeColor32, biomeIndex), palette.Clear, SCAN_Settings_Config.Instance.BiomeTransparency);
					}
				}
			}

			map.SetPixels32(pix);
			map.Apply();

			return map;
		}

		private static Texture2D drawBiomeMap(ref Texture2D m, ref Color32[] p, SCANdata d, int h)
		{
			if (d.Body.BiomeMap == null)
			{
				return null;
			}

			if (m == null || m.height != h)
			{
				m = new Texture2D(h * 2, h, TextureFormat.RGBA32, true);
			}

			if (p == null || p.Length != h * h * 2)
			{
				//p = new Color32[m.width * m.height];
				p = new Color32[m.width];
			}

			float scale = m.width / 360f;

			for (int j = 0; j < m.height; j++)
			{
				//if (j % 2 != 0)
				//	continue;

				double lat = (j / scale) - 90;
				for (int i = 0; i < m.width; i++)
				{
					//if (i % 2 != 0)
					//	continue;

					double lon = fixLon(i / scale);

					//if (SCANUtil.isCovered(lon, lat, d, SCANtype.Biome))
					//	p[j * m.width + i] = (Color32)SCANUtil.getBiomeCached(d.Body, lon, lat).mapColor;
					//else
					//	p[j * m.width + i] = palette.Clear;

					if (SCANUtil.isCovered(lon, lat, d, SCANtype.Biome))
					{
						p[i] = (Color32)SCANUtil.getBiomeCached(d.Body, lon, lat).mapColor;
					}
					else
					{
						p[i] = palette.Clear;
					}
				}

				m.SetPixels32(0, j, m.width, 1, p);
			}

			//for (int i = 2 / 2; i >= 1; i /= 2)
			//{
			//	SCANuiUtil.interpolate(p, m.height, m.width, i, i, i, null);
			//	SCANuiUtil.interpolate(p, m.height, m.width, 0, i, i, null);
			//	SCANuiUtil.interpolate(p, m.height, m.width, i, 0, i, null);
			//}

			//m.SetPixels32(p);

			m.Apply();

			return m;
		}

		internal static void drawTerrainMap(ref Color32[] pix, ref float[,] values, SCANdata data, int height, int stepScale)
		{
			int width = height * 2;
			float scale = height / 180f;

			if (pix == null)
			{
				pix = new Color32[width * height];
			}

			for (int i = 0; i < width; i++)
			{
				double lon = fixLon(i / scale);
				for (int j = 0; j < height; j++)
				{
					double lat = (j / scale) - 90;

					Color32 c = palette.Clear;

					if (SCANUtil.isCovered(lon, lat, data, SCANtype.Altimetry))
					{
						if (SCANUtil.isCovered(lon, lat, data, SCANtype.AltimetryHiRes))
						{
							c = palette.heightToColor(values[i, j], true, data.TerrainConfig);
						}
						else
						{
							int ilon = SCANUtil.icLON(unFixLon(lon));
							int ilat = SCANUtil.icLAT(lat);
							int lo = ((int)(ilon * scale * 5)) / 5;
							int la = ((int)(ilat * scale * 5)) / 5;
							c = palette.heightToColor(values[lo, la], false, data.TerrainConfig);
						}

						c = palette.lerp(c, palette.Clear, 0.1f);
					}
					else
					{
						c = palette.Clear;
					}

					pix[j * width + i] = c;
				}
			}
		}

		internal static Texture2D drawSlopeMap(ref Texture2D map, ref Color32[] pix, ref float[,] values, SCANdata data, int height, int stepScale)
		{
			int width = height * 2;
			float scale = height / 180f;

			double run = ((data.Body.Radius * 2 * Math.PI) / width) / 3;

			if (map == null || pix == null || map.height != height)
			{
				map = new Texture2D(width, height, TextureFormat.ARGB32, true);
				pix = new Color32[width * height];
			}

			for (int j = 0; j < height; j++)
			{
				double lat = (j / scale) - 90;
				double runFixed = Math.Max(run * Math.Cos(Mathf.Deg2Rad * lat), 1);

				for (int i = 0; i < width; i++)
				{
					double lon = fixLon(i / scale);

					Color32 c = palette.Clear;

					if (SCANUtil.isCovered(lon, lat, data, SCANtype.Altimetry))
					{
						double[] e = new double[5];
						float slope = 0;

						e[0] = values[i, j];

						Vector2 s = slipCoordinates(i + 1, j, width, height);
						e[1] = values[(int)s.x, (int)s.y];
						s = slipCoordinates(i - 1, j, width, height);
						e[2] = values[(int)s.x, (int)s.y];
						s = slipCoordinates(i, j + 1, width, height);
						e[3] = values[(int)s.x, (int)s.y];
						s = slipCoordinates(i, j - 1, width, height);
						e[4] = values[(int)s.x, (int)s.y];
						s = slipCoordinates(i + 1, j + 1, width, height);
						//e[5] = values[(int)s.x, (int)s.y];
						//s = slipCoordinates(i + 1, j - 1, width, height);
						//e[6] = values[(int)s.x, (int)s.y];
						//s = slipCoordinates(i - 1, j + 1, width, height);
						//e[7] = values[(int)s.x, (int)s.y];
						//s = slipCoordinates(i - 1, j - 1, width, height);
						//e[8] = values[(int)s.x, (int)s.y];

						if (data.Body.ocean)
						{
							for (int a = 0; a < 5; a++)
							{
								if (e[a] < 0)
								{
									e[a] = 0;
								}
							}
						}

						slope = (float)SCANUtil.slopeShort(e, runFixed);

						if (SCANUtil.isCovered(lon, lat, data, SCANtype.AltimetryHiRes))
						{
							float slopeNormal = slope / 30;

							if (slopeNormal > 1)
							{
								slopeNormal = 1;
							}

							if (slopeNormal < 0.6f)
							{
								c = palette.lerp(SCANcontroller.controller.lowSlopeColorOne32, SCANcontroller.controller.highSlopeColorOne32, slopeNormal);
							}
							else
							{
								c = palette.lerp(SCANcontroller.controller.lowSlopeColorTwo32, SCANcontroller.controller.highSlopeColorTwo32, slopeNormal);
							}
						}
						else
						{
							float slopeRoundNormal = (float)(Math.Round(slope / 5) * 5) / 30;

							if (slopeRoundNormal > 1)
							{
								slopeRoundNormal = 1;
							}

							if (slopeRoundNormal < 0.6f)
							{
								c = palette.lerp(SCANcontroller.controller.lowSlopeColorOne32, SCANcontroller.controller.highSlopeColorOne32, slopeRoundNormal);
							}
							else
							{
								c = palette.lerp(SCANcontroller.controller.lowSlopeColorTwo32, SCANcontroller.controller.highSlopeColorTwo32, slopeRoundNormal);
							}
						}

						c = palette.lerp(c, palette.Clear, 0.1f);
					}
					else
					{
						c = palette.Clear;
					}

					pix[j * width + i] = c;
				}
			}

			map.SetPixels32(pix);
			map.Apply();

			return map;
		}

		internal static void generateTerrainArray(ref float[,] values, int height, int stepScale, SCANdata data, int index)
		{
			int width = height * 2;
			float scale = height / 180f;

			values = new float[width, height];

			for (int i = 0; i < 360; i++)
			{
				for (int j = 0; j < 180; j++)
				{
					values[i * stepScale, j * stepScale] = data.HeightMapValue(index, (int)fixLon(i) + 180, j, true);
				}
			}

			for (int i = stepScale / 2; i >= 1; i /= 2)
			{
				SCANuiUtil.interpolate(values, height, width, i, i, i, null, false);
				SCANuiUtil.interpolate(values, height, width, 0, i, i, null, false);
				SCANuiUtil.interpolate(values, height, width, i, 0, i, null, false);
			}
		}

		private static Vector2 slipCoordinates(int x, int y, int width, int height)
		{
			if (y < 0)
			{
				y = Math.Abs(y);
				x += (width / 2);
			}

			else if (y > height)
			{
				while (y > 180)
				{
					y -= 180;
				}

				y = 180 - Math.Abs(y);
				x -= (width / 2);
			}

			y = (y + height) % height;
			x = (x + width) % width;

			return new Vector2(x, y);
		}

		internal static Texture2D drawLoDetailMap(ref Color32[] pix, ref float[,] values, SCANmap map, SCANdata data, int width, int height, int stepScale, bool withResources)
		{
			if (map.Map == null || pix == null || map.Map.height != height)
			{
				map.Map = new Texture2D(width, height, TextureFormat.ARGB32, false);
				pix = new Color32[width * height];
				values = new float[width, height];
			}

			for (int i = 0; i < width; i += stepScale)
			{
				for (int j = 0; j < height; j += stepScale)
				{
					double lon = (i * 1.0f / map.MapScale) - 180f + map.Lon_Offset;
					double lat = (j * 1.0f / map.MapScale) - 90f + map.Lat_Offset;
					double la = lat, lo = lon;
					lat = map.unprojectLatitude(lo, la);
					lon = map.unprojectLongitude(lo, la);

					values[i, j] = (float)SCANUtil.getElevation(data.Body, lon, lat);
				}
			}

			for (int i = stepScale / 2; i >= 1; i /= 2)
			{
				SCANuiUtil.interpolate(values, height, width, i, i, i, null, false, true);
				SCANuiUtil.interpolate(values, height, width, 0, i, i, null, false, true);
				SCANuiUtil.interpolate(values, height, width, i, 0, i, null, false, true);
			}

			for (int i = 0; i < width; i++)
			{
				for (int j = 0; j < height; j++)
				{
					if (map.UseCustomRange)
					{
						pix[j * width + i] = palette.heightToColor(values[i, j], true, data.TerrainConfig, map.CustomMin, map.CustomMax, map.CustomRange, true);
					}
					else
					{
						pix[j * width + i] = palette.heightToColor(values[i, j], true, data.TerrainConfig);
					}
				}
			}

			if (withResources)
			{
				stepScale = 2;

				generateResourceCache(ref values, height, width, stepScale, map.MapScale, map);

				for (int i = stepScale / 2; i >= 1; i /= 2)
				{
					SCANuiUtil.interpolate(values, height, width, i, i, i, null, false, true);
					SCANuiUtil.interpolate(values, height, width, 0, i, i, null, false, true);
					SCANuiUtil.interpolate(values, height, width, i, 0, i, null, false, true);
				}

				for (int i = 0; i < width; i++)
				{
					for (int j = 0; j < height; j++)
					{
						double lon = (i * 1.0f / map.MapScale) - 180f + map.Lon_Offset;
						double lat = (j * 1.0f / map.MapScale) - 90f + map.Lat_Offset;
						double la = lat, lo = lon;
						lat = map.unprojectLatitude(lo, la);
						lon = map.unprojectLongitude(lo, la);

						Color32 c = pix[j * width + i];

						pix[j * width + i] = resourceToColor32(c, map.Resource, map.Resource.CurrentBody.MinValue, map.Resource.CurrentBody.MaxValue, values[i, j], data, lon, lat);
					}
				}
			}

			map.Map.SetPixels32(pix);
			map.Map.Apply();

			return map.Map;
		}

		internal static void generateResourceCache(ref float[,] values, int height, int width, int stepScale, double scale, SCANmap map)
		{
			for (int j = 0; j < height; j += stepScale)
			{
				for (int i = 0; i < width; i += stepScale)
				{
					Vector2d coords;
					if (map.Projection == MapProjection.Orthographic)
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

		private static void interpolate(Color32[] c, int height, int width, int x, int y, int step, System.Random r)
		{
			for (int j = y; j < height + y; j += 2 * step)
			{
				int ypos1 = j - step;
				if (ypos1 < 0)
				{
					ypos1 = 0;
				}

				int ypos2 = j + step;
				if (ypos2 >= height)
				{
					ypos2 = height - 1;
				}

				for (int i = x; i < width + x; i += 2 * step)
				{
					int xpos1 = i - step;
					if (xpos1 < 0)
					{
						xpos1 = 0;
					}

					int xpos2 = i + step;
					if (xpos2 >= width)
					{
						xpos2 = width - 1;
					}

					Color32 avgX = Color.clear;
					Color32 avgY = Color.clear;

					float lerp = 0.5f;

					if (x == y)
					{
						avgX = Color32.Lerp(c[ypos1 * width + xpos1], c[ypos2 * width + xpos2], lerp); //Mathf.Lerp(v[xpos1, ypos1], v[xpos2, ypos2], lerp);
						avgY = Color32.Lerp(c[ypos2 * width + xpos1], c[ypos1 * width + xpos2], lerp); //Mathf.Lerp(v[xpos1, ypos2], v[xpos2, ypos1], lerp);
					}
					else
					{
						avgX = Color32.Lerp(c[j * width + xpos1], c[j * width + xpos2], lerp); //Mathf.Lerp(v[xpos1, j], v[xpos2, j], lerp);
						avgY = Color32.Lerp(c[ypos2 * width + i], c[ypos1 * width + i], lerp); //Mathf.Lerp(v[i, ypos2], v[i, ypos1], lerp);
					}

					Color32 avgFinal = Color32.Lerp(avgX, avgY, lerp);

					c[j * width + i] = avgFinal;
				}
			}
		}

		internal static void interpolate(float[,] v, int height, int width, int x, int y, int step, System.Random r, bool softEdges, bool hardEdges = false)
		{
			for (int j = y; j < height + y; j += 2 * step)
			{
				int ypos1 = j - step;
				if (ypos1 < 0)
				{
					ypos1 = 0;
				}

				int ypos2 = j + step;
				if (ypos2 >= height)
				{
					ypos2 = height - 1;
				}

				for (int i = x; i < width + x; i += 2 * step)
				{
					int xpos1 = i - step;
					if (xpos1 < 0)
					{
						if (hardEdges)
						{
							xpos1 = 0;
						}
						else
						{
							xpos1 += width;
						}
					}
					int xpos2 = i + step;
					if (xpos2 >= width)
					{
						if (hardEdges)
						{
							xpos2 = width - 1;
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
