using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FuzzyMaps
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	class VisualMaps : MonoBehaviour
	{
		private Rect WindowRect;
		private CelestialBody body;
		private MeshRenderer mesh;
		private Texture oldMainTex;
		private Texture oldBumpMap;
		private Texture2D newScaledMap;
		private Texture2D smallScaledMap;
		private bool twice;
		private bool multi;
		private int pass = -1;
		private float alpha = 1;
		private float shiny;
		private int visualHeight = 128;
		private int rescaleType = 0;
		private int WindowID;

		private void Start()
		{
			WindowID = UnityEngine.Random.Range(1000, 2000000);

			setBody(FlightGlobals.currentMainBody);
		}

		/// <summary>
		/// Watch for changes to the celestial body we are lookin at
		/// </summary>
		private void Update()
		{
			if ((MapView.MapIsEnabled && HighLogic.LoadedSceneIsFlight && FlightGlobals.ready))
			{
				CelestialBody mapBody = getTargetBody(MapView.MapCamera.target);				

				if (mapBody == null)
					return;

				if (mapBody != body)
					setBody(mapBody);
			}
			else if (HighLogic.LoadedSceneIsFlight && FlightGlobals.ready)
			{
				if (body != FlightGlobals.currentMainBody)
					setBody(FlightGlobals.currentMainBody);
			}
		}

		private CelestialBody getTargetBody(MapObject target)
		{
			switch (target.type)
			{
				case MapObject.ObjectType.CelestialBody:
					return target.celestialBody;
				case MapObject.ObjectType.ManeuverNode:
					return target.maneuverNode.patch.referenceBody;
				case MapObject.ObjectType.Vessel:
					return target.vessel.mainBody;
				default:
					return null;
			}
		}

		/// <summary>
		/// This caches the current celestial body and it's visual and nor
		/// </summary>
		/// <param name="B"></param>
		private void setBody(CelestialBody B)
		{
			body = B;

			mesh = body.scaledBody.GetComponent<MeshRenderer>();
			oldMainTex = mesh.material.GetTexture("_MainTex");
			oldBumpMap = mesh.material.GetTexture("_BumpMap");

			var s = mesh.material.GetFloat("_Shininess");
			shiny = s;
		}

		private void OnGUI()
		{
			WindowRect = GUILayout.Window(WindowID, WindowRect, drawButtons, "Fuzzy Maps");
		}

		/// <summary>
		/// Draw the window and a bucn of control buttons
		/// </summary>
		/// <param name="id"></param>
		private void drawButtons(int id)
		{
			GUILayout.BeginHorizontal();

			//Toggle the low resolution visual map
			if (GUILayout.Button("Visual On"))
			{
				if (twice)
				{
					getMeshTexture();
					body.SetResourceMap(newScaledMap);
				}
				else
				{
					//Destroy(newScaledMap);

					getMeshTexture();

					OverlayGenerator.Instance.ClearDisplay();

					rescaleMap();

					mesh.material.SetTexture("_MainTex", (Texture)smallScaledMap);
					mesh.material.SetFloat("_Shininess", shiny);
				}
			}

			if (GUILayout.Button("Visual Off"))
			{
				if (twice)
				{
					Destroy(newScaledMap);
				}
				else
					mesh.material.SetTexture("_MainTex", oldMainTex);
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

			//Toggle the normal map
			if (GUILayout.Button("Bump On"))
			{
				mesh.material.SetTexture("_BumpMap", oldBumpMap);
			}

			if (GUILayout.Button("Bump Off"))
			{
				mesh.material.SetTexture("_BumpMap", null);
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

			GUILayout.Label("Visual Map Height:");

			GUILayout.FlexibleSpace();

			//Change the visual map scaling resolution
			if (GUILayout.Button("-", GUILayout.Width(18)))
			{
				visualHeight = Math.Max(64, visualHeight / 2);
			}
			GUILayout.Label(visualHeight.ToString(), GUILayout.Width(36));
			if (GUILayout.Button("+", GUILayout.Width(18)))
			{
				visualHeight = Math.Min(512, visualHeight * 2);
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

			GUILayout.Label("Scale Type:");

			GUILayout.FlexibleSpace();

			//Change the texture scaling method; between Point and Bilinear
			if (GUILayout.Button("-", GUILayout.Width(18)))
			{
				rescaleType = Math.Max(0, rescaleType - 1);
			}
			GUILayout.Label(rescaleType.ToString(), GUILayout.Width(36));
			if (GUILayout.Button("+", GUILayout.Width(18)))
			{
				rescaleType = Math.Min(1, rescaleType + 1);
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

			GUILayout.Label("Alpha:");

			GUILayout.FlexibleSpace();

			//Adjust the visual map alpha channel; used by ocean planets
			if (GUILayout.Button("-", GUILayout.Width(18)))
			{
				alpha = (float)Math.Max(0, alpha - 0.05);
			}
			GUILayout.Label(alpha.ToString("P0"), GUILayout.Width(36));
			if (GUILayout.Button("+", GUILayout.Width(18)))
			{
				alpha = (float)Math.Min(1, alpha + 0.05);
			}

			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();

			GUILayout.Label("Shiny:");

			GUILayout.FlexibleSpace();

			//I think this has something to do with specularity
			if (GUILayout.Button("-", GUILayout.Width(18)))
			{
				shiny = (float)Math.Max(0, shiny - 0.05);
			}
			GUILayout.Label(shiny.ToString("P0"), GUILayout.Width(36));
			if (GUILayout.Button("+", GUILayout.Width(18)))
			{
				shiny = (float)Math.Min(1, shiny + 0.05);
			}

			GUILayout.EndHorizontal();

			//Reset the visual map to the cached version
			if (GUILayout.Button("Reset Main"))
			{
				mesh.material.SetTexture("_MainTex", oldMainTex);
			}
		}		

		/// <summary>
		/// This takes the visual map that was cached in the SetBody method and turns it into a readable texture; this is a commonly used method and you can find references to it all over in Unity support forums
		/// </summary>
		private void getMeshTexture()
		{
			//if (newScaledMap == null)
			newScaledMap = new Texture2D(oldMainTex.width, oldMainTex.height);

			var rt = RenderTexture.GetTemporary(oldMainTex.width, oldMainTex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, 1);

			blit(rt);

			RenderTexture.active = rt;

			newScaledMap.ReadPixels(new Rect(0, 0, oldMainTex.width, oldMainTex.height), 0, 0);

			RenderTexture.active = null;
			RenderTexture.ReleaseTemporary(rt);

			rt = null;

			newScaledMap.Apply();
		}

		/// <summary>
		/// Can't quite remember what this was used for, the first option is the standard one, I think the second may have been something I was trying with troublesome planets like Kerbin; the same goes for the pass argument
		/// </summary>
		/// <param name="t"></param>
		private void blit(RenderTexture t)
		{
			if (!multi)
				Graphics.Blit(oldMainTex, t, mesh.material, pass);
			else
				Graphics.BlitMultiTap(oldMainTex, t, mesh.material);
		}

		/// <summary>
		/// This uses the Unity Addon TextureScale to re-size the visual map, it also adjusts the alpha channel by multiplying each pixel by the alpha level you set with the window controls
		/// </summary>
		private void rescaleMap()
		{
			smallScaledMap = newScaledMap;

			if (alpha < 0.95f)
			{
				var pix = smallScaledMap.GetPixels32();

				for (int i = 0; i < pix.Length; i++)
				{
					pix[i].a = (byte)(pix[i].a * alpha);
				}

				smallScaledMap.SetPixels32(pix);
				smallScaledMap.Apply();

				pix = null;
			}

			switch (rescaleType)
			{
				case 0:
					TextureScale.Bilinear(smallScaledMap, visualHeight * 2, visualHeight);
					break;
				case 1:
					TextureScale.Point(smallScaledMap, visualHeight * 2, visualHeight);
					break;
				default:
					break;
			}
		}
	}
}
