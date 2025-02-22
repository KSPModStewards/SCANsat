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
using System.Text;
using SCANsat.SCAN_Data;
using SCANsat.SCAN_Toolbar;
using SCANsat.SCAN_UI;
using SCANsat.SCAN_Unity;
using KSP.UI.Screens.Flight.Dialogs;
using KSP.Localization;

using UnityEngine;
using palette = SCANsat.SCAN_UI.UI_Framework.SCANcolorUtil;

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
		public bool requireLight;
		[KSPField]
		public string animationName;
		//[KSPField(guiName = "SCANsat Altitude", guiActive = false)]
		//public string alt_indicator;
		[KSPField(isPersistant = true)]
		protected bool scanning = false;

		[KSPField(groupName = "scaninfo", groupDisplayName = "#autoLOC_SCANsat_ScanInfo", guiName = "#autoLOC_SCANsat_ScanInfoStatus", groupStartCollapsed = true, guiActive = true, guiActiveUnfocused = true, unfocusedRange = 3f)]
		public string scanStatus = "";
		[KSPField(groupName = "scaninfo", groupDisplayName = "#autoLOC_SCANsat_ScanInfo", guiName = "#autoLOC_SCANsat_ScanInfoAltitude", groupStartCollapsed = true, guiActive = true, guiActiveUnfocused = true, unfocusedRange = 3f)]
		public string scanAltitude = "";
		[KSPField(groupName = "scaninfo", groupDisplayName = "#autoLOC_SCANsat_ScanInfo", guiName = "#autoLOC_SCANsat_ScanInfoType", groupStartCollapsed = true, guiActive = true, guiActiveUnfocused = true, unfocusedRange = 3f)]
		public string scanType = "";
		[KSPField(groupName = "scaninfo", groupDisplayName = "#autoLOC_SCANsat_ScanInfo", guiName = "#autoLOC_SCANsat_ScanInfoFOV", groupStartCollapsed = true, guiActive = true, guiActiveUnfocused = true, unfocusedRange = 3f)]
		public string scanFOV = "0";
		[KSPField(groupName = "scaninfo", groupDisplayName = "#autoLOC_SCANsat_ScanInfo", guiName = "#autoLOC_SCANsat_ScanInfoPower", groupStartCollapsed = true, guiActive = true, guiActiveUnfocused = true, unfocusedRange = 3f)]
		public string scanPower = "0";
		[KSPField(groupName = "scaninfo", groupDisplayName = "#autoLOC_SCANsat_ScanInfo", guiName = "#autoLOC_SCANsat_ScanInfoDaylight", groupStartCollapsed = true, guiActive = true, guiActiveUnfocused = true, unfocusedRange = 3f)]
		public string scanDaylight = "";

		private string powerMessage;

		public bool scanningNow
		{
			get { return scanning; }
		}

		private bool powerIsProblem;
		private int powerTimer;
		private Animation anim = null;

		protected BaseField scanInfoStatus;
		protected BaseField scanInfoAltitude;
		protected BaseField scanInfoType;
		protected BaseField scanInfoFOV;
		protected BaseField scanInfoPower;
		protected BaseField scanInfoDaylight;

		protected bool UpdateScannerInfo = true;

		public override void OnAwake()
		{
			base.OnAwake();
			scanInfoStatus = Fields["scanStatus"];
			scanInfoAltitude = Fields["scanAltitude"];
			scanInfoType = Fields["scanType"];
			scanInfoFOV = Fields["scanFOV"];
			scanInfoPower = Fields["scanPower"];
			scanInfoDaylight = Fields["scanDaylight"];
		}

		/* SAT: KSP entry points */
		public override void OnStart(StartState state)
		{
			if (state == StartState.Editor)
			{
				print("[SCANsat] start: in editor");
				Events["editorExtend"].active = !string.IsNullOrEmpty(animationName);
				scanInfoAltitude.guiActive = false;
				scanInfoStatus.guiActive = false;
				scanInfoType.guiActive = false;
				scanInfoFOV.guiActive = false;
				scanInfoPower.guiActive = false;
				scanInfoDaylight.guiActive = false;
			}
			else
			{
				scanType = getTypeString();
				scanAltitude = getAltString();
				scanPower = getECString();
				print("[SCANsat] start: live");
				GameEvents.onVesselSOIChanged.Add(ChangeSOI);
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
					anim.playAutomatically = false;
					anim.cullingType = AnimationCullingType.BasedOnRenderers;
					anim[animationName].wrapMode = WrapMode.Once;
					anim[animationName].speed = 0;
				}
			}
			if (scanName != null)
			{
				Events["startScan"].guiName = string.Format("{0}: {1}", Localizer.Format("#autoLOC_SCANsat_StartScan"), scanName);
				Events["stopScan"].guiName = string.Format("{0}: {1}", Localizer.Format("#autoLOC_SCANsat_StopScan"), scanName);
				Actions["startScanAction"].guiName = string.Format("{0}: {1}", Localizer.Format("#autoLOC_SCANsat_StartScan"), scanName);
				Actions["stopScanAction"].guiName = string.Format("{0}: {1}", Localizer.Format("#autoLOC_SCANsat_StopScan"), scanName);
				Actions["toggleScanAction"].guiName = string.Format("{0}: {1}", Localizer.Format("#autoLOC_SCANsat_ToggleScan"), scanName);
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

			if (scanning)
			{
				animate(1, 1);
			}

			powerIsProblem = false;
			print("[SCANsat] sensorType: " + sensorType.ToString() + " fov: " + fov.ToString() + " min_alt: " + min_alt.ToString() + " max_alt: " + max_alt.ToString() + " best_alt: " + best_alt.ToString());
		}

		private void OnDestroy()
		{
			GameEvents.onVesselSOIChanged.Remove(ChangeSOI);
		}

		protected virtual void Update()
		{
			if (!HighLogic.LoadedSceneIsFlight)
			{
				return;
			}

			if (!FlightGlobals.ready)
			{
				return;
			}

			if (sensorType == 0)
			{
				return;
			}

			Events["startScan"].active = !scanning && !powerIsProblem;
			Events["stopScan"].active = scanning || powerIsProblem;

			SCANdata data = SCANUtil.getData(vessel.mainBody);

			if (data == null)
			{
				return;
			}

			if (part.PartActionWindow != null && UpdateScannerInfo)
			{
				scanAlt(data, scanning);
			}
		}

		protected virtual void FixedUpdate()
		{
			if (!HighLogic.LoadedSceneIsFlight)
			{
				return;
			}

			if (!FlightGlobals.ready)
			{
				return;
			}

			if (SCANcontroller.controller == null)
			{
				return;
			}

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
					if (!resHandler.UpdateModuleResourceInputs(ref powerMessage, 1, 0.9, false, true))
					{
						unregisterScanner();
						powerIsProblem = true;
						powerTimer = 0;
					}
					else
					{
						powerIsProblem = false;
					}
				}
				else
				{
					unregisterScanner();
				}
			}
		}

		public override string GetInfo()
		{
			if (sensorType == 0)
			{
				return "";
			}

			StringBuilder sb = StringBuilderCache.Acquire();

			sb.Append(base.GetInfo());

			if (min_alt != 0)
			{
				sb.Append(Localizer.Format("#autoLOC_SCANsat_AltitudeMin", (min_alt / 1000).ToString("F0")));
			}

			if (best_alt != min_alt)
			{
				sb.Append(Localizer.Format("#autoLOC_SCANsat_AltitudeBest", (best_alt / 1000).ToString("F0")));
			}

			if (max_alt != 0)
			{
				sb.Append(Localizer.Format("#autoLOC_SCANsat_AltitudeMax", (max_alt / 1000).ToString("F0")));
			}

			if (fov != 0)
			{
				sb.Append(Localizer.Format("#autoLOC_SCANsat_FOV", fov.ToString("F1") + "°"));
			}

			sb.AppendLine();

			sb.Append(Localizer.Format("#autoLOC_SCANsat_Daylight", RUIutils.GetYesNoUIString(requireLight)));

			sb.AppendLine();
			sb.AppendLine();
			sb.Append(Localizer.Format("#autoLOC_SCANsat_Types"));

			if ((sensorType & (short)SCANtype.AltimetryLoRes) != 0)
			{
				sb.AppendLine();
				sb.Append(SCANtype.AltimetryLoRes.ToString());
			}
			if ((sensorType & (short)SCANtype.AltimetryHiRes) != 0)
			{
				sb.AppendLine();
				sb.Append(SCANtype.AltimetryHiRes.ToString());
			}
			if ((sensorType & (short)SCANtype.Biome) != 0)
			{
				sb.AppendLine();
				sb.Append(SCANtype.Biome.ToString());
			}
			if ((sensorType & (short)SCANtype.Anomaly) != 0)
			{
				sb.AppendLine();
				sb.Append(SCANtype.Anomaly.ToString());
			}
			if ((sensorType & (short)SCANtype.AnomalyDetail) != 0)
			{
				sb.AppendLine();
				sb.Append(SCANtype.AnomalyDetail.ToString());
			}
			if ((sensorType & (short)SCANtype.VisualLoRes) != 0)
			{
				sb.AppendLine();
				sb.Append(SCANtype.VisualLoRes.ToString());
			}
			if ((sensorType & (short)SCANtype.VisualHiRes) != 0)
			{
				sb.AppendLine();
				sb.Append(SCANtype.VisualHiRes.ToString());
			}
			if ((sensorType & (short)SCANtype.ResourceLoRes) != 0)
			{
				sb.AppendLine();
				sb.Append(SCANtype.ResourceLoRes.ToString());
			}
			if ((sensorType & (short)SCANtype.ResourceHiRes) != 0)
			{
				sb.AppendLine();
				sb.Append(SCANtype.ResourceHiRes.ToString());
			}

			sb.AppendLine();
			sb.Append(resHandler.PrintModuleResources(1));

			return sb.ToStringAndRelease();
		}

		/* SCAN: context (right click) buttons in FLIGHT */
		[KSPEvent(guiActive = true, guiName = "Start RADAR Scan", active = true)]
		public void startScan()
		{
			if (!ToolbarManager.ToolbarAvailable && SCANcontroller.controller != null)
			{
				if (!SCAN_Settings_Config.Instance.StockToolbar && SCAN_UI_MainMap.Instance != null && !SCAN_UI_MainMap.Instance.IsVisible)
				{
					SCAN_UI_MainMap.Instance.Open();
				}
			}
			registerScanner();
			animate(1, 0);
		}

		[KSPEvent(guiActive = true, guiName = "Stop RADAR Scan", active = true, requireFullControl = false)]
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
			{
				stopScan();
			}
			else
			{
				startScan();
			}
		}

		/* SCAN: add static (a warning that we're low on electric charge) */
		private void addStatic()
		{
			if (SCANcontroller.controller == null)
			{
				return;
			}

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
			{
				SCANcontroller.controller.registerSensor(vessel, (SCANtype)sensorType, fov, min_alt, max_alt, best_alt, requireLight);
			}
		}

		protected void unregisterScanner()
		{
			scanning = false;
			if (sensorType > 0 && SCANcontroller.controller != null)
			{
				SCANcontroller.controller.unregisterSensor(vessel, (SCANtype)sensorType, fov, min_alt, max_alt, best_alt, requireLight);
			}
		}

		private void scanAlt(SCANdata d, bool scan)
		{
			//scanAltitude = Localizer.Format("#autoLOC_SCANsat_Unknown");
			//scanAltitude = scanAltRange;
			float alt = (float)vessel.altitude;
			if (!SCAN_Settings_Config.Instance.BackgroundScanning)
			{
				scanStatus = Localizer.Format("#autoLOC_SCANsat_All_Disabled");
				scanInfoAltitude.guiActive = false;
				scanInfoStatus.guiActive = true;
				scanInfoType.guiActive = false;
				scanInfoFOV.guiActive = false;
				scanInfoPower.guiActive = false;
				scanInfoDaylight.guiActive = false;
			}
			else if (d.Disabled)
			{
				scanStatus = string.Format("{0}: {1}", SCANUtil.displayNameFromBody(d.Body), Localizer.Format("#autoLOC_SCANsat_Disabled"));
				scanInfoAltitude.guiActive = true;
				scanInfoStatus.guiActive = true;
				scanInfoType.guiActive = true;
				scanInfoFOV.guiActive = false;
				scanInfoPower.guiActive = false;
				scanInfoDaylight.guiActive = false;
			}
			else if (powerIsProblem)
			{
				scanStatus = powerMessage;
				scanInfoAltitude.guiActive = true;
				scanInfoStatus.guiActive = true;
				scanInfoType.guiActive = true;
				scanInfoFOV.guiActive = false;
				scanInfoPower.guiActive = true;
				scanInfoDaylight.guiActive = false;
			}
			else if (alt < min_alt)
			{
				scanStatus = Localizer.Format("#autoLOC_SCANsat_TooLow");
				scanInfoAltitude.guiActive = true;
				scanInfoStatus.guiActive = true;
				scanInfoType.guiActive = true;
				scanInfoFOV.guiActive = false;
				scanInfoDaylight.guiActive = false;
				if (scan)
				{
					scanInfoPower.guiActive = true;
				}
				else
				{
					scanInfoPower.guiActive = false;
				}
			}
			else if (alt < best_alt)
			{
				scanStatus = Localizer.Format("#autoLOC_SCANsat_SubOptimal");
				scanInfoAltitude.guiActive = true;
				scanInfoStatus.guiActive = true;
				scanInfoType.guiActive = true;
				scanInfoFOV.guiActive = true;
				scanInfoPower.guiActive = true;
				if (scan)
				{
					scanInfoFOV.guiActive = true;
					scanInfoPower.guiActive = true;
					scanFOV = string.Format("{0}°", CurrentFOV(alt).ToString("N1"));

					if (requireLight && SCAN_Settings_Config.Instance.DaylightCheck)
					{
						scanInfoDaylight.guiActive = true;
						scanDaylight = RUIutils.GetYesNoUIString(
							!SCANUtil.InDarkness(vessel.orbit.getPositionAtUT(Planetarium.GetUniversalTime()), vessel.mainBody.position, SCANUtil.LocalSun(vessel.mainBody).position)
							);
					}
					else
					{
						scanInfoDaylight.guiActive = false;
					}
				}
				else
				{
					scanInfoFOV.guiActive = false;
					scanInfoPower.guiActive = false;
					scanInfoDaylight.guiActive = false;
				}

			}
			else if (alt >= best_alt && alt <= max_alt)
			{
				scanStatus = Localizer.Format("#autoLOC_SCANsat_Ideal");
				scanInfoAltitude.guiActive = true;
				scanInfoStatus.guiActive = true;
				scanInfoType.guiActive = true;
				if (scan)
				{
					scanInfoFOV.guiActive = true;
					scanInfoPower.guiActive = true;
					scanFOV = string.Format("{0}°", CurrentFOV(alt).ToString("N1"));
					if (requireLight && SCAN_Settings_Config.Instance.DaylightCheck)
					{
						scanInfoDaylight.guiActive = true;
						scanDaylight = RUIutils.GetYesNoUIString(
							!SCANUtil.InDarkness(vessel.orbit.getPositionAtUT(Planetarium.GetUniversalTime()), vessel.mainBody.position, SCANUtil.LocalSun(vessel.mainBody).position)
							);
					}
					else
					{
						scanInfoDaylight.guiActive = false;
					}
				}
				else
				{
					scanInfoFOV.guiActive = false;
					scanInfoPower.guiActive = false;
					scanInfoDaylight.guiActive = false;
				}

			}
			else if (alt > max_alt)
			{
				scanStatus = Localizer.Format("#autoLOC_SCANsat_TooHigh");
				scanInfoAltitude.guiActive = true;
				scanInfoStatus.guiActive = true;
				scanInfoType.guiActive = true;
				scanInfoFOV.guiActive = false;
				scanInfoDaylight.guiActive = false;
				if (scan)
				{
					scanInfoPower.guiActive = true;
				}
				else
				{
					scanInfoPower.guiActive = false;
				}
			}
		}

		private void ChangeSOI(GameEvents.HostedFromToAction<Vessel, CelestialBody> vc)
		{
			scanAltitude = getAltString();
		}

		private string getTypeString()
		{
			StringBuilder sb = StringBuilderCache.Acquire();

			if ((sensorType & (short)SCANtype.AltimetryLoRes) != 0)
			{
				sb.Append("Alt Lo");
			}
			if ((sensorType & (short)SCANtype.AltimetryHiRes) != 0)
			{
				if (sb.Length > 0)
				{
					sb.Append(", ");
				}

				sb.Append("Alt Hi");
			}
			if ((sensorType & (short)SCANtype.Biome) != 0)
			{
				if (sb.Length > 0)
				{
					sb.Append(", ");
				}

				sb.Append("Biome");
			}
			if ((sensorType & (short)SCANtype.Anomaly) != 0)
			{
				if (sb.Length > 0)
				{
					sb.Append(", ");
				}

				sb.Append("Anomaly");
			}
			if ((sensorType & (short)SCANtype.AnomalyDetail) != 0)
			{
				if (sb.Length > 0)
				{
					sb.Append(", ");
				}

				sb.Append("BTDT");
			}
			if ((sensorType & (short)SCANtype.VisualLoRes) != 0)
			{
				if (sb.Length > 0)
				{
					sb.Append(", ");
				}

				sb.Append("Vis Lo");
			}
			if ((sensorType & (short)SCANtype.VisualHiRes) != 0)
			{
				if (sb.Length > 0)
				{
					sb.Append(", ");
				}

				sb.Append("Vis Hi");
			}
			if ((sensorType & (short)SCANtype.ResourceLoRes) != 0)
			{
				if (sb.Length > 0)
				{
					sb.Append(", ");
				}

				sb.Append("Res Lo");
			}
			if ((sensorType & (short)SCANtype.ResourceHiRes) != 0)
			{
				if (sb.Length > 0)
				{
					sb.Append(", ");
				}

				sb.Append("Res Hi");
			}

			return sb.ToStringAndRelease();
		}

		private string getAltString()
		{
			float ba = Math.Min(best_alt, (float)(vessel.mainBody.sphereOfInfluence - vessel.mainBody.Radius));

			if (ba >= max_alt)
			{
				return string.Format("{0} - {1}km", (min_alt / 1000).ToString("N0"), (ba / 1000).ToString("N0"));
			}
			else
			{
				return string.Format("{0} - {1}km: > {2}km Ideal", (min_alt / 1000).ToString("N0"), (max_alt / 1000).ToString("N0"), (ba / 1000).ToString("N0"));
			}
		}

		private string getECString()
		{
			float ec = 0;

			for (int i = resHandler.inputResources.Count - 1; i >= 0; i--)
			{
				if (resHandler.inputResources[i].name == "ElectricCharge")
				{
					ec = (float)resHandler.inputResources[i].rate;

					break;
				}
			}

			return string.Format("{0}EC/s", ec.ToString("N1"));
		}

		private float CurrentFOV(float alt)
		{
			float f = fov;
			float ba = Math.Min(best_alt, (float)(vessel.mainBody.sphereOfInfluence - vessel.mainBody.Radius));

			if (alt < ba)
			{
				f = (alt / ba) * f;
			}

			f = f * (float)(Planetarium.fetch.Home.Radius / vessel.mainBody.Radius);

			if (f > 20)
			{
				f = 20;
			}

			return f;
		}

	}
}
