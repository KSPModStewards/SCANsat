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

using SCANsat.SCAN_Data;
using System.Collections.Generic;
using SCANsat.SCAN_Unity;
using UnityEngine;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANcolorUtil;


namespace SCANsat.SCAN_UI.UI_Framework
{
	public class SCANremoteView
	{
		private const float MAX_DISTANCE = 400;
		private const float MIN_DISTANCE = 15;
		private const float MAX_DISTANCE_UP = 75;
		private const float MIN_DISTANCE_UP = 30;

		private static Camera cam;
		private static GameObject camgo;
		private RenderTexture rt;
		private int updateFrame, width, height;
		private List<CrashObjectName> cons;
		private List<SCANROC> rocs;
		private Bounds bounds;
		private int activeCon;
		private int activeROC;
		public GameObject lookat;
		public CrashObjectName lookdetail;
		public SCANROC rocDetail;
		public bool rocsMode;

		public void Initialize(int w, int h)
		{
			width = w;
			height = h;
		}

		public void setup(GameObject focus)
		{
			if (lookat != focus)
			{
				lookdetail = null;

				bounds = new Bounds(focus.transform.position, Vector3.zero);
				foreach (MeshRenderer c in focus.GetComponentsInChildren<MeshRenderer>())
				{
					bounds.Encapsulate(c.bounds);
				}

				cons = new List<CrashObjectName>();

				foreach (CrashObjectName con in focus.GetComponentsInChildren<CrashObjectName>())
				{
					cons.Add(con);
				}
			}
			lookat = focus;

			rocDetail = null;

			rocsMode = false;
		}

		public void setup(List<SCANROC> rocList, Vessel v)
		{
			if (rocList == null)
			{
				return;
			}

			double nearest = -1;

			rocs = new List<SCANROC>();

			for (int i = rocList.Count - 1; i >= 0; i--)
			{
				SCANROC roc = rocList[i];

				if (!roc.Known)
				{
					continue;
				}

				rocs.Add(roc);

				double d = (roc.Roc.transform.position - v.transform.position).sqrMagnitude;

				if (d < nearest || nearest < 0)
				{
					lookat = roc.Roc.gameObject;
					rocDetail = roc;
					nearest = d;
				}
			}

			if (lookat != null)
			{
				bounds = new Bounds(lookat.transform.position, Vector3.zero);
				foreach (MeshRenderer c in lookat.GetComponentsInChildren<MeshRenderer>())
				{
					bounds.Encapsulate(c.bounds);
				}
			}
			else
			{
				bounds = new Bounds();
			}

			lookdetail = null;

			rocsMode = true;
		}

		public bool valid(GameObject focus)
		{
			if (focus == null)
			{
				return false;
			}

			if (focus != lookat)
			{
				return false;
			}

			return true;
		}

		public bool validROC()
		{
			if (!rocsMode)
			{
				return false;
			}

			if (rocDetail.Roc == null)
			{
				return false;
			}

			if (lookat != rocDetail.Roc.gameObject)
			{
				return false;
			}

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
			{
				return;
			}

			if (rt == null || rt.width != width || rt.height != height)
			{
				RenderTextureFormat format = RenderTextureFormat.RGB565;

				if (!SystemInfo.SupportsRenderTextureFormat(format))
				{
					format = RenderTextureFormat.Default;
				}

				rt = new RenderTexture(width, height, 32, format);
				rt.Create();
			}

			if (camgo == null)
			{
				camgo = new GameObject();
			}

			if (cam == null)
			{
				cam = camgo.AddComponent<Camera>();
				//Add image processing component to camera game object
				camgo.AddComponent<SCANEdgeDetect>();
				cam.enabled = false;

				cam.targetTexture = rt;
				cam.aspect = width * 1f / height;
				cam.fieldOfView = 90;
			}

			Vector3 pos_target = lookat.transform.position;

			if (lookdetail != null)
			{
				pos_target = lookdetail.transform.position;
			}

			if (rocDetail != null)
			{
				pos_target = rocDetail.Roc.transform.position;
			}

			Vector3 pos_cam = FlightGlobals.ActiveVessel.transform.position;
			Vector3 target_up = (pos_target - FlightGlobals.currentMainBody.transform.position).normalized;
			Vector3 dir = (pos_target - pos_cam).normalized;

			float dist = 100;
			float bound = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));

			dist = Mathf.Clamp(bound * 1.5f, MIN_DISTANCE, MAX_DISTANCE);
			float distUp = Mathf.Clamp(bound * 1.5f, MIN_DISTANCE_UP, MAX_DISTANCE_UP);

			pos_cam = pos_target - dir * dist / 1.75f + target_up * distUp / 2f;
			Vector3 cam_up = (pos_cam - FlightGlobals.currentMainBody.transform.position).normalized;

			cam.transform.position = pos_cam;
			cam.transform.LookAt(pos_target, cam_up);
			cam.farClipPlane = dist * 3;

			cam.Render();

			updateFrame = Time.frameCount;
		}

		public Texture getTexture()
		{
			updateCamera();
			return rt;
		}

		public string getInfoString()
		{
			if (!rocsMode && cons.Count > 0)
			{
				return string.Format("> Identified {0} structure{1}", cons.Count.ToString(), cons.Count > 1 ? "s" : "");
			}
			else if (rocsMode)
			{
				return string.Format("> Identified {0} surface object{1}", rocs.Count.ToString(), rocs.Count > 1 ? "s" : "");
			}
			else
			{
				return "";
			}
		}

		public string getAnomalyDataString(bool mouse, bool anomalyKnown)
		{
			string sname = lookat.name;

			Vector3 lookvec = lookat.transform.position;

			bool distance = false;

			if (!rocsMode && cons.Count > 0)
			{
				float scroll = Input.GetAxis("Mouse ScrollWheel");

				lookdetail = cons[activeCon];

				if (mouse && scroll != 0)
				{
					activeCon += (scroll > 0 ? 1 : -1);

					if (activeCon >= cons.Count)
					{
						activeCon = 0;
					}
					else if (activeCon < 0)
					{
						activeCon = cons.Count - 1;
					}

					lookdetail = cons[activeCon];

					bounds = new Bounds(lookdetail.transform.position, Vector3.zero);
					foreach (MeshRenderer c in lookdetail.GetComponentsInChildren<MeshRenderer>())
					{
						bounds.Encapsulate(c.bounds);
					}
				}

				lookvec = lookdetail.transform.position;
				sname = lookdetail.objectName;

				rocDetail = null;

				distance = anomalyKnown;
			}
			else if (rocsMode && rocs.Count > 0)
			{
				float scroll = Input.GetAxis("Mouse ScrollWheel");

				if (mouse && scroll != 0)
				{
					activeROC += (scroll > 0 ? 1 : -1);

					if (activeROC >= rocs.Count)
					{
						activeROC = 0;
					}
					else if (activeROC < 0)
					{
						activeROC = rocs.Count - 1;
					}

					rocDetail = rocs[activeROC];
					lookat = rocDetail.Roc.gameObject;

					bounds = new Bounds(lookat.transform.position, Vector3.zero);
					foreach (MeshRenderer c in lookat.GetComponentsInChildren<MeshRenderer>())
					{
						bounds.Encapsulate(c.bounds);
					}
				}

				lookvec = rocDetail.Roc.transform.position;
				sname = rocDetail.Scanned ? rocDetail.Name : "Unknown";

				lookdetail = null;

				distance = true;
			}

			if (distance)
			{
				string dist = SCANuiUtil.distanceString((FlightGlobals.ActiveVessel.transform.position - lookvec).magnitude, 2000);

				return string.Format("{0}\n{1}", sname, dist);
			}
			else
			{
				return string.Format("\n{0}", sname);
			}
		}

	}
}
