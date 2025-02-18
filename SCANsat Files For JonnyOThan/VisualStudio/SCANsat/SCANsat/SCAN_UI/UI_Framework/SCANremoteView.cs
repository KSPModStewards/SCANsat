#region license
/*
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * RemoteView - A camera looking at a GameObject, rendering to a texture.
 *
 * Copyright (c)2013 damny;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
*/
#endregion

using System;
using System.Collections.Generic;
using SCANsat.SCAN_Unity;
using UnityEngine;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANpalette;


namespace SCANsat.SCAN_UI.UI_Framework
{
	public class SCANremoteView
	{
		private static Camera cam;
		private static GameObject camgo;
		private static Material edgeDetectMaterial, grayscaleMaterial;
		private RenderTexture rt;
		private int updateFrame, width, height;
		private List<CrashObjectName> cons;
		private Bounds bounds;
		private int activeCon;
		public GameObject lookat;
		public CrashObjectName lookdetail;

		public void setup(int w, int h, GameObject focus)
		{
			edgeDetectMaterial = new Material(SCAN_UI_Loader.EdgeDetectShader);
			edgeDetectMaterial.SetFloat("_Threshold", 0.05f);

			grayscaleMaterial = new Material(SCAN_UI_Loader.GreyScaleShader);
			Texture2D t = new Texture2D(256, 1, TextureFormat.RGB24, false);

			// ramp texture to render everything in dark shades of Amber,
			// except originally dark lines, which become bright Amber
			for (int i = 0; i < 256; ++i)
				t.SetPixel(i, 0, palette.lerp(palette.black, palette.xkcd_Amber, i / 1024f));
			for (int i = 0; i < 10; ++i)
				t.SetPixel(i, 0, palette.xkcd_Amber);
			t.Apply();
			grayscaleMaterial.SetTexture("_RampTex", t);

			if (lookat != focus)
			{
				lookdetail = null;
				bounds = new Bounds();
				foreach (Collider c in focus.GetComponentsInChildren<Collider>())
					bounds.Encapsulate(c.bounds);
				cons = new List<CrashObjectName>();
				foreach (CrashObjectName con in focus.GetComponentsInChildren<CrashObjectName>())
				{
					cons.Add(con);
				}
			}
			lookat = focus;
			width = w;
			height = h;
		}

		public bool valid(GameObject focus)
		{
			if (focus == null)
				return false;

			if (focus != lookat)
				return false;

			return true;
		}

		public void free()
		{
			GameObject.Destroy(camgo);
			RenderTexture.Destroy(rt);
			cam = null;
			camgo = null;
			rt = null;
		}

		private void updateCamera()
		{
			if (updateFrame > Time.frameCount - 5)
				return;

			if (rt == null || rt.width != width || rt.height != height)
			{
				rt = new RenderTexture(width, height, 32, RenderTextureFormat.RGB565);
				rt.Create();
			}

			if (camgo == null)
				camgo = new GameObject();

			if (cam == null)
			{
				cam = camgo.AddComponent<Camera>();
				cam.enabled = false;
				cam.targetTexture = rt;
				cam.aspect = width * 1f / height;
				cam.fieldOfView = 90;
			}

			Vector3 pos_target = lookat.transform.position;

			if (lookdetail != null)
				pos_target = lookdetail.transform.position;

			Vector3 pos_cam = FlightGlobals.ActiveVessel.transform.position;
			Vector3 target_up = (pos_target - FlightGlobals.currentMainBody.transform.position).normalized;
			Vector3 dir = (pos_target - pos_cam).normalized;
			float dist = 100;

			pos_cam = pos_target - dir * dist / 2 + target_up * dist / 3;
			Vector3 cam_up = (pos_cam - FlightGlobals.currentMainBody.transform.position).normalized;

			cam.transform.position = pos_cam;
			cam.transform.LookAt(pos_target, cam_up);
			cam.farClipPlane = dist * 3;

			RenderTexture old = RenderTexture.active;
			RenderTexture.active = rt;
			cam.Render();
			Graphics.Blit(rt, rt, edgeDetectMaterial);
			Graphics.Blit(rt, rt, grayscaleMaterial);

			RenderTexture.active = old;
			updateFrame = Time.frameCount;
		}

		public Texture getTexture()
		{
			updateCamera();
			return rt;
		}

		public string getInfoString()
		{
			string info = "No structures found";

			if (cons.Count > 0)
				info = string.Format("Identified {0} structure{1}", cons.Count, cons.Count > 1 ? "s" : "");

			info = "> " + info;
			
			return info;
		}

		public string getAnomalyDataString(bool mouse)
		{
			string sname = lookat.name;

			Vector3 lookvec = lookat.transform.position;

			if (cons.Count > 0)
			{
				float scroll = Input.GetAxis("Mouse ScrollWheel");

				if (mouse && scroll != 0)
				{
					activeCon += (scroll > 0 ? 1 : -1);

					if (activeCon >= cons.Count)
						activeCon = 0;
					else if (activeCon < 0)
						activeCon = cons.Count - 1;
				}

				lookdetail = cons[activeCon];
				lookvec = lookdetail.transform.position;
				sname = lookdetail.objectName;
			}

			string dist = SCANuiUtil.distanceString((FlightGlobals.ActiveVessel.transform.position - lookvec).magnitude, 2000);

			return string.Format("{0}\n{1}", sname, dist);
		}

	}
}

