#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANmechjeb - A monobehaviour for watching SCANsat and MechJeb for the addition of a landing target
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System.Collections;
using SCANsat;
using SCANsat.SCAN_Data;
using log = SCANsat.SCAN_Platform.Logging.ConsoleLogger;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANpalette;
using MuMech;

using UnityEngine;
using UnityEngine.Events;

namespace SCANmechjeb
{
	class SCANmechjeb : MonoBehaviour
	{
		private const string siteName = "MechJeb Landing Target";
		private Vessel vessel;
		private CelestialBody body;
		private MechJebCore mjCore;
		private MechJebModuleTargetController target;
		private DisplayModule guidanceModule;
		private SCANwaypoint way;
		private SCANdata data;
		private Vector2d coords = new Vector2d();
		private bool shutdown, mjOnboard, mjTechTreeLocked;

		private void Start()
		{
			GameEvents.onVesselWasModified.Add(VesselChange);
			GameEvents.onVesselChange.Add(VesselChange);
			GameEvents.onVesselSOIChanged.Add(SOIChange);
			SCANcontroller.controller.MJTargetSet.AddListener(new UnityAction<Vector2d, CelestialBody>(OnTargetSet));

			StartCoroutine(WaitForReady());
		}

		private IEnumerator WaitForReady()
		{
			shutdown = true;

			while (!FlightGlobals.ready || FlightGlobals.ActiveVessel == null)
				yield return null;

			vessel = FlightGlobals.ActiveVessel;

			if (vessel == null)
				yield break;

			VesselChange(vessel);

			body = vessel.mainBody;

			data = SCANUtil.getData(body);

			if (data == null)
				shutdown = true;
			else
				shutdown = false;
		}

		private void OnDestroy()
		{
			GameEvents.onVesselChange.Remove(VesselChange);
			GameEvents.onVesselWasModified.Remove(VesselChange);
			GameEvents.onVesselSOIChanged.Remove(SOIChange);
			SCANcontroller.controller.MJTargetSet.RemoveListener(OnTargetSet);
		}

		private void LateUpdate()
		{
			if (shutdown || !mjOnboard || mjTechTreeLocked || body == null || vessel == null || data == null)
				return;

			if (!HighLogic.LoadedSceneIsFlight || !FlightGlobals.ready)
				return;

			way = null;

			if (!SCAN_Settings_Config.Instance.MechJebTarget)
				return;

			if (SCANcontroller.controller == null)
				return;

			if (!SCANcontroller.controller.MechJebLoaded)
				SCANcontroller.controller.MechJebLoaded = true;

			if (target.Target == null)
				return;

			if (target.targetBody != body)
				return;

			if ((target.Target is DirectionTarget))
				return;

			coords.x = target.targetLongitude;
			coords.y = target.targetLatitude;

			if (SCANcontroller.controller.LandingTarget != null)
				way = SCANcontroller.controller.LandingTarget;

			if (way != null)
			{
				if (!SCANUtil.ApproxEq(coords.x, way.Longitude) || !SCANUtil.ApproxEq(coords.y, way.Latitude))
				{
					way = new SCANwaypoint(coords.y, coords.x, siteName);
					SCANcontroller.controller.LandingTarget = way;
					data.addToWaypoints();
				}
			}
			else
			{
				way = new SCANwaypoint(coords.y, coords.x, siteName);
				SCANcontroller.controller.LandingTarget = way;
				data.addToWaypoints();
			}
		}

		private void OnTargetSet(Vector2d pos, CelestialBody b)
		{
			if (!mjOnboard || target == null)
				return;

			target.SetPositionTarget(b, pos.y, pos.x);
		}

		private void SOIChange(GameEvents.HostedFromToAction<Vessel, CelestialBody> action)
		{
			if (vessel == null)
				return;

			if (vessel != action.host)
				return;

			body = action.to;

			data = SCANUtil.getData(body);

			if (data == null)
				shutdown = true;
		}

		private void VesselChange(Vessel v)
		{
			if (vessel != v)
				return;

			body = v.mainBody;

			if (vessel.FindPartModulesImplementing<MechJebCore>().Count <= 0)
			{
				SCANcontroller.controller.MechJebLoaded = false;
				mjOnboard = false;
				mjCore = null;
				return;
			}

			mjCore = vessel.GetMasterMechJeb();

			if (mjCore == null)
			{
				SCANcontroller.controller.MechJebLoaded = false;
				mjOnboard = false;
				target = null;
				return;
			}

			target = mjCore.target;

			if (target == null)
			{
				SCANcontroller.controller.MechJebLoaded = false;
				mjOnboard = false;
				return;
			}

			mjOnboard = true;

			if (HighLogic.CurrentGame.Mode != Game.Modes.SANDBOX)
			{
				if (guidanceModule == null)
					guidanceModule = (DisplayModule)mjCore.GetComputerModule("MechJebModuleLandingGuidance");

				if (guidanceModule == null)
				{
					SCANcontroller.controller.MechJebLoaded = false;
					way = null;
					mjOnboard = false;
					mjTechTreeLocked = true;
					return;
				}

				guidanceModule.UnlockCheck();

				if (guidanceModule.hidden)
				{
					SCANcontroller.controller.MechJebLoaded = false;
					way = null;
					mjOnboard = false;
					mjTechTreeLocked = true;
					return;
				}
			}

			SCANcontroller.controller.MechJebLoaded = true;
			mjTechTreeLocked = false;
		}
	}
}
