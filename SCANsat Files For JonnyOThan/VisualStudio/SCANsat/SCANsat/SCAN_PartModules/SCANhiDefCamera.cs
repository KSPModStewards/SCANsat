#region license
/*  [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANhiDefCamera - Controls modified zoom map
 * 
 * Copyright (c)2013 damny;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 DMagic
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using SCANsat.SCAN_UI;
using UnityEngine;

namespace SCANsat.SCAN_PartModules
{
	public class SCANhiDefCamera : PartModule, IAnimatedModule
	{
		//[KSPField]
		//public float minZoom = 2;
		//[KSPField]
		//public float maxZoom = 1000;
		//[KSPField]
		//public bool hiDetailOnly;

		//private List<ModuleHighDefCamera> stockCameras;

		private bool activated;
		private bool refreshState;

		//public override void OnStart(PartModule.StartState state)
		//{
		//	if (state == StartState.Editor)
		//		return;

		//	part.force_activate();
		//	this.isEnabled = true;
		//	activated = true;
		//	refreshState = true;

		//	//stockCameras = findCameras();

		//	minZoom = clampValue(minZoom, 1, 10);
		//	maxZoom = clampValue(maxZoom, 10, 1000);

		//	Events["toggleSCANHiDef"].guiName = "Toggle Map";
		//	Events["resetCenter"].guiName = "Reset Map Center";
		//}

		//private void OnDestroy()
		//{
		//	Events["resetCenter"].active = false;
		//}

		//private List<ModuleHighDefCamera> findCameras()
		//{
		//	return part.FindModulesImplementing<ModuleHighDefCamera>().ToList();
		//}

		private float clampValue (float value, float min, float max)
		{
			return Mathf.Clamp(value, min, max);
		}

		//private void Update()
		//{
		//	if (!activated)
		//	{
		//		Events["toggleSCANHiDef"].active = false;
		//		return;
		//	}

		//	if (!HighLogic.LoadedSceneIsFlight || !FlightGlobals.ready)
		//		return;

		//	if (SCANcontroller.controller == null)
		//		return;

		//	if (refreshState)
		//	{
		//		if (SCANcontroller.controller.disableStockResource)
		//			disableConnectedModules();
		//		refreshState = false;
		//	}

		//	if (!SCANcontroller.controller.disableStockResource)
		//	{
		//		Events["toggleSCANHiDef"].active = false;
		//		return;
		//	}

		//	Events["toggleSCANHiDef"].active = true;
		//}

		//[KSPEvent(guiActive = true, active = false)]
		//public void toggleSCANHiDef()
		//{
		//	if (SCANcontroller.controller.hiDefMap == null)
		//	{
		//		SCANcontroller.controller.hiDefMap = gameObject.AddComponent<SCANzoomHiDef>();
		//	}

		//	if (!SCANcontroller.controller.hiDefMap.Visible)
		//	{
		//		SCANcontroller.controller.hiDefMap.setMapCenter(SCANUtil.fixLatShift(vessel.latitude), SCANUtil.fixLonShift(vessel.longitude), hiDetailOnly || SCANcontroller.controller.hiDetailZoomMap, null, false, this);
		//	}
		//	else
		//		SCANcontroller.controller.hiDefMap.closeMap();

		//	Events["resetCenter"].active = SCANcontroller.controller.hiDefMap.Visible;
		//}

		//[KSPEvent(guiActive = true, active = false)]
		//public void resetCenter()
		//{
		//	if (SCANcontroller.controller.hiDefMap == null)
		//		return;

		//	SCANcontroller.controller.hiDefMap.setMapCenter(SCANUtil.fixLatShift(vessel.latitude), SCANUtil.fixLonShift(vessel.longitude), hiDetailOnly || SCANcontroller.controller.hiDetailZoomMap, null, false, this);
		//}

		private void enableConnectedModules()
		{
			//if (stockCameras != null)
			//{
			//	foreach (ModuleHighDefCamera m in stockCameras)
			//	{
			//		m.EnableModule();
			//	}
			//}
		}

		private void disableConnectedModules()
		{
			//if (stockCameras != null)
			//{
			//	foreach (ModuleHighDefCamera m in stockCameras)
			//	{
			//		m.DisableModule();
			//	}
			//}
		}

		public void EnableModule()
		{
			activated = true;
			if (SCANcontroller.controller != null && SCANcontroller.controller.disableStockResource)
				disableConnectedModules();
		}

		public void DisableModule()
		{
			activated = false;
			Events["resetCenter"].active = false;
			if (SCANcontroller.controller != null && SCANcontroller.controller.disableStockResource)
				disableConnectedModules();
		}

		public bool ModuleIsActive()
		{
			return activated;
		}

		public bool IsSituationValid()
		{
			return true;
		}

	}
}
