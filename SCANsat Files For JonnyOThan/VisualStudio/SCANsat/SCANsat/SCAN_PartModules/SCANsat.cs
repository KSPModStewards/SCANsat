#region license
/*  [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANsat - SCAN RADAR Altimetry Sensor part (& More)
 * 
 * Copyright (c)2013 damny;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using SCANsat.SCAN_Data;
using SCANsat.SCAN_Toolbar;
using SCANsat.SCAN_UI;
using SCANsat.SCAN_Unity;
using KSP.UI.Screens.Flight.Dialogs;
using KSP.Localization;

using UnityEngine;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANpalette;

namespace SCANsat.SCAN_PartModules
{
	public class SCANsat : PartModule
	{
		[KSPField]
		public int sensorType;
		[KSPField]
		public int experimentType;
		[KSPField]
		public float fov;
		[KSPField]
		public float min_alt;
		[KSPField]
		public float max_alt;
		[KSPField]
		public float best_alt;
		[KSPField]
		public string scanName;
		[KSPField]
		public string animationName;
		[KSPField(guiName = "SCANsat Altitude", guiActive = false)]
		public string alt_indicator;
		[KSPField(isPersistant = true)]
		protected bool scanning = false;

		public bool scanningNow
		{
			get { return scanning; }
		}

		private bool powerIsProblem;
		private int powerTimer;
		private Animation anim = null;

		/* SAT: KSP entry points */
		public override void OnStart(StartState state)
		{
			if (state == StartState.Editor)
			{
				print("[SCANsat] start: in editor");
				Events["editorExtend"].active = !string.IsNullOrEmpty(animationName);
			}
			else
			{
				print("[SCANsat] start: live");
			}
			if (!string.IsNullOrEmpty(animationName))
			{
				Animation[] a = part.FindModelAnimators(animationName);
				if (a.Length == 0)
				{
					print("[SCANsat] animation '" + animationName + "' not found");
				}
				else
				{
					print("[SCANsat] using animation #1 out of " + a.Length.ToString() + " animations named '" + animationName + "'");
					anim = a[0];
				}
			}
			if (scanName != null)
			{
				Events["startScan"].guiName = string.Format("{0}: {1}", Localization.Format("#autoLOC_SCANsat_StartScan"), scanName);
				Events["stopScan"].guiName = string.Format("{0}: {1}", Localization.Format("#autoLOC_SCANsat_StopScan"), scanName);
				Actions["startScanAction"].guiName = string.Format("{0}: {1}", Localization.Format("#autoLOC_SCANsat_StartScan"), scanName);
				Actions["stopScanAction"].guiName = string.Format("{0}: {1}", Localization.Format("#autoLOC_SCANsat_StopScan"), scanName);
				Actions["toggleScanAction"].guiName = string.Format("{0}: {1}", Localization.Format("#autoLOC_SCANsat_ToggleScan"), scanName);
			}

			if (sensorType == 0)
			{
				// here, we override all event and action labels
				// and we also disable the analyze button (it does nothing)
				Events["startScan"].active = false;
				Events["stopScan"].active = false;
				Events["editorExtend"].active = false;
				Actions["startScanAction"].active = false;
				Actions["stopScanAction"].active = false;
				Actions["toggleScanAction"].active = false;
			}

			if (scanning) animate(1, 1);
			powerIsProblem = false;
			print("[SCANsat] sensorType: " + sensorType.ToString() + " fov: " + fov.ToString() + " min_alt: " + min_alt.ToString() + " max_alt: " + max_alt.ToString() + " best_alt: " + best_alt.ToString());
		}

		protected virtual void Update()
		{
			if (!HighLogic.LoadedSceneIsFlight)
				return;

			if (!FlightGlobals.ready)
				return;

			if (sensorType == 0)
				return;

			Events["startScan"].active = !scanning && !powerIsProblem;
			Events["stopScan"].active = scanning || powerIsProblem;
			if (sensorType != 32)
				Fields["alt_indicator"].guiActive = scanning || powerIsProblem;

			SCANdata data = SCANUtil.getData(vessel.mainBody);

			if (data == null)
				return;

			if (scanning)
				alt_indicator = scanAlt(data);
		}

		protected virtual void FixedUpdate()
		{
			if (!HighLogic.LoadedSceneIsFlight)
				return;

			if (!FlightGlobals.ready)
				return;

			if (SCANcontroller.controller == null)
				return;

			if (powerIsProblem)
			{
				if (powerTimer < 30)
				{
					powerTimer++;
					return;
				}

				addStatic();
				registerScanner();
			}

			if (scanning)
			{
				if (sensorType != 0 || SCANcontroller.controller.isVesselKnown(vessel.id, (SCANtype)sensorType))
				{
					if (!resHandler.UpdateModuleResourceInputs(ref alt_indicator, 1, 0.9, false, true))
					{
						unregisterScanner();
						powerIsProblem = true;
						powerTimer = 0;
					}
					else
						powerIsProblem = false;
				}
				else
					unregisterScanner();
			}
		}

		public override string GetInfo()
		{
			if (sensorType == 0)
				return "";

			string str = base.GetInfo();
			if (min_alt != 0)
				str += Localization.Format("#autoLOC_SCANsat_AltitudeMin", (min_alt / 1000).ToString("F0"));
			if (best_alt != min_alt)
				str += Localization.Format("#autoLOC_SCANsat_AltitudeBest", (best_alt / 1000).ToString("F0"));
			if (max_alt != 0)
				str += Localization.Format("#autoLOC_SCANsat_AltitudeMax", (max_alt / 1000).ToString("F0"));
			if (fov != 0)
				str += Localization.Format("#autoLOC_SCANsat_FOV", fov.ToString("F0") + "°");

			str += resHandler.PrintModuleResources(1);
			return str;
		}
				
		/* SCAN: context (right click) buttons in FLIGHT */
		[KSPEvent(guiActive = true, guiName = "Start RADAR Scan", active = true)]
		public void startScan()
		{
			if (!ToolbarManager.ToolbarAvailable && SCANcontroller.controller != null)
			{
				if (!SCAN_Settings_Config.Instance.StockToolbar && SCAN_UI_MainMap.Instance != null && !SCAN_UI_MainMap.Instance.IsVisible)
					SCAN_UI_MainMap.Instance.Open();
			}
			registerScanner();
			animate(1, 0);
		}

		[KSPEvent(guiActive = true, guiName = "Stop RADAR Scan", active = true)]
		public void stopScan()
		{
			unregisterScanner();
			powerIsProblem = false;
			animate(-1, 1);
		}

		/* SCAN: context (right click) buttons in EDTIOR */
		[KSPEvent(guiActiveEditor = true, guiName = "Extend", active = true)]
		public void editorExtend()
		{
			Events["editorExtend"].active = false;
			Events["editorRetract"].active = true;
			animate(1, 0);
		}

		[KSPEvent(guiActiveEditor = true, guiName = "Retract", active = false)]
		public void editorRetract()
		{
			Events["editorExtend"].active = true;
			Events["editorRetract"].active = false;
			animate(-1, 1);
		}

		/* SCAN: trivial function to do animation */
		private void animate(float speed, float time)
		{
			if (anim != null && anim[animationName] != null)
			{
				anim[animationName].speed = speed;
				if (!anim.IsPlaying(animationName))
				{
					anim[animationName].wrapMode = WrapMode.Clamp;
					anim[animationName].normalizedTime = time;
					anim.Play(animationName);
				}
			}
		}

		/* SCAN: actions for ... something ... */
		[KSPAction("Start Scan")]
		public void startScanAction(KSPActionParam param)
		{
			startScan();
		}

		[KSPAction("Stop Scan")]
		public void stopScanAction(KSPActionParam param)
		{
			stopScan();
		}

		[KSPAction("Toggle Scan")]
		public void toggleScanAction(KSPActionParam param)
		{
			if (scanning)
				stopScan();
			else
				startScan();
		}

		/* SCAN: add static (a warning that we're low on electric charge) */
		private void addStatic()
		{
			if (SCANcontroller.controller == null)
				return;

			//if (SCANcontroller.controller.mainMap == null)
			//	return;

			//if (SCANcontroller.controller.mainMap.Map == null)
			//	return;

			//for (int i = 0; i < 1000; i++)
			//{
			//	SCANcontroller.controller.mainMap.Map.SetPixel(UnityEngine.Random.Range(0, 360), UnityEngine.Random.Range(0, 180), palette.lerp(palette.black, palette.white, UnityEngine.Random.value));
			//}
		}

		/* SCAN: register scanners without going through animation */
		private void registerScanner()
		{
			scanning = true;
			powerTimer = 0;
			if (sensorType > 0 && SCANcontroller.controller != null)
				SCANcontroller.controller.registerSensor(vessel, (SCANtype)sensorType, fov, min_alt, max_alt, best_alt);
		}

		protected void unregisterScanner()
		{
			scanning = false;
			if (sensorType > 0 && SCANcontroller.controller != null)
				SCANcontroller.controller.unregisterSensor(vessel, (SCANtype)sensorType);
		}

		private string scanAlt(SCANdata d)
		{
			string altitude = Localization.Format("#autoLOC_SCANsat_Unknown");
			if (!SCAN_Settings_Config.Instance.BackgroundScanning)
				altitude = Localization.Format("#autoLOC_SCANsat_All_Disabled");
			else if (d.Disabled)
				altitude = d.Body.displayName + ": " + Localization.Format("#autoLOC_SCANsat_Disabled");
			else if (vessel.altitude < min_alt)
				altitude = Localization.Format("#autoLOC_SCANsat_TooLow");
			else if (vessel.altitude < best_alt)
				altitude = Localization.Format("#autoLOC_SCANsat_SubOptimal");
			else if (vessel.altitude >= best_alt && vessel.altitude <= max_alt)
				altitude = Localization.Format("#autoLOC_SCANsat_Ideal");
			else if (vessel.altitude > max_alt)
				altitude = Localization.Format("#autoLOC_SCANsat_TooHigh");
			return altitude;
		}

	}
}
