#region license
/*  [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANresourceScanner - Resource scanner part module
 * 
 * Copyright (c)2013 damny;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 DMagic
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using SCANsat.SCAN_Data;

namespace SCANsat.SCAN_PartModules
{
	public class ModuleSCANresourceScanner : SCANsat, IAnimatedModule
	{
		private List<ModuleOrbitalSurveyor> mSurvey;
		private List<ModuleOrbitalScanner> mScanner;
		private ModuleAnimationGroup animGroup;
		private bool activated;
		private bool refreshState;
		private bool loaded;

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);

			Actions["startScanAction"].active = false;
			Actions["stopScanAction"].active = false;
			Actions["toggleScanAction"].active = false;
			Actions["analyzeData"].active = false;
			Actions["startResourceScanAction"].guiName = "Start " + scanName;
			Actions["stopResourceScanAction"].guiName = "Stop " + scanName;
			Actions["toggleResourceScanAction"].guiName = "Toggle " + scanName;

			if (state == StartState.Editor)
				return;

			mSurvey = findSurvey();
			mScanner = findScanner();
			animGroup = findAnimator();

			if (animGroup == null)
				activated = true;

			refreshState = true;
		}

		public override string GetInfo()
		{
			string info = base.GetInfo();
			info += "Resource Scan: " + (SCANtype)sensorType + "\n";

			return info;
		}

		private List<ModuleOrbitalScanner> findScanner()
		{
			return part.FindModulesImplementing<ModuleOrbitalScanner>();
		}

		private List<ModuleOrbitalSurveyor> findSurvey()
		{
			return part.FindModulesImplementing<ModuleOrbitalSurveyor>();
		}

		private ModuleAnimationGroup findAnimator()
		{
			return part.FindModulesImplementing<ModuleAnimationGroup>().FirstOrDefault();
		}

		private void updateEvents()
		{
			base.Events["startScan"].active = !scanning;
			base.Events["stopScan"].active = scanning;
		}

		override protected void Update()
		{
			if (!activated)
			{
				base.Events["startScan"].active = false;
				base.Events["stopScan"].active = false;
				base.Events["analyze"].active = false;
				if (scanning && loaded)
					unregisterScanner();
				return;
			}

			if (!HighLogic.LoadedSceneIsFlight || !FlightGlobals.ready)
				return;

			if (SCANcontroller.controller == null)
				return;

			base.Update();

			if (refreshState)
			{
				refreshState = false;

				if (SCAN_Settings_Config.Instance.DisableStockResource)
				{
					if (mSurvey != null)
					{
						foreach (ModuleOrbitalSurveyor m in mSurvey)
							m.DisableModule();
					}

					if (mScanner != null)
					{
						foreach (ModuleOrbitalScanner m in mScanner)
							m.DisableModule();
					}
				}
				loaded = true;
			}

			if (!SCAN_Settings_Config.Instance.InstantScan || SCAN_Settings_Config.Instance.DisableStockResource)
				updateEvents();
			else
			{
				base.Events["startScan"].active = false;
				base.Events["stopScan"].active = false;
				base.Events["analyze"].active = false;
				if (scanning)
					unregisterScanner();
			}
		}

		[KSPAction("Start Resource Scan")]
		public void startResourceScanAction(KSPActionParam param)
		{
			if (!SCAN_Settings_Config.Instance.InstantScan || SCAN_Settings_Config.Instance.DisableStockResource)
			{
				if (animGroup != null && !scanning && !animGroup.isDeployed)
					animGroup.DeployModule();
				startScan();
			}
		}

		[KSPAction("Stop Resource Scan")]
		public void stopResourceScanAction(KSPActionParam param)
		{
			stopScan();
		}

		[KSPAction("Toggle Resource Scan")]
		public void toggleResourceScanAction(KSPActionParam param)
		{
			if (scanning)
				stopScan();
			else
			{
				if (!SCAN_Settings_Config.Instance.InstantScan || SCAN_Settings_Config.Instance.DisableStockResource)
				{
					if (animGroup != null && !animGroup.isDeployed)
						animGroup.DeployModule();
					startScan();
				}
			}
		}

		public void DisableModule()
		{
			activated = false;
			base.Events["startScan"].active = false;
			base.Events["stopScan"].active = false;
			base.Events["analyze"].active = false;
			if (scanning && loaded)
				unregisterScanner();

			if (SCAN_Settings_Config.Instance.DisableStockResource)
			{
				if (mSurvey != null)
				{
					foreach (ModuleOrbitalSurveyor m in mSurvey)
						m.DisableModule();
				}

				if (mScanner != null)
				{
					foreach (ModuleOrbitalScanner m in mScanner)
						m.DisableModule();
				}
			}
		}

		public void EnableModule()
		{
			activated = true;
			if (SCAN_Settings_Config.Instance.DisableStockResource)
			{
				base.Events["analyze"].active = (sensorType & (int)SCANtype.FuzzyResources) != 0;

				if (mSurvey != null)
				{
					foreach (ModuleOrbitalSurveyor m in mSurvey)
						m.DisableModule();
				}

				if (mScanner != null)
				{
					foreach (ModuleOrbitalScanner m in mScanner)
						m.DisableModule();
				}
			}
		}

		public bool IsSituationValid()
		{
			return true;
		}

		public bool ModuleIsActive()
		{
			return activated;
		}
	}
}
