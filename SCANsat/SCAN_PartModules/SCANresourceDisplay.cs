#region license
/*  [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANresourceDisplay - Resource abundance display
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
using SCANsat.SCAN_UI.UI_Framework;
using KSP.Localization;
using UnityEngine;

namespace SCANsat.SCAN_PartModules
{
	public class SCANresourceDisplay : PartModule, IAnimatedModule
	{
		[KSPField(guiActive = true, groupName = "SCANResourceDisplayGroup", groupDisplayName = "#autoLOC_SCANsat_ScanResourceDisplay", groupStartCollapsed = true)]
		public string abundance;
		[KSPField]
		public string ResourceName;
		[KSPField]
		public bool RequiresUnlock;

		private float abundanceValue;

		private List<ModuleResourceScanner> stockScanners;
		private SCANsat scanScanner;
		private ModuleAnimationGroup animGroup;
		private Dictionary<string, ResourceCache.AbundanceSummary> abundanceSummary;
		private CelestialBody body;
		private float maxAbundanceAltitude;
		private bool tooHigh;
		private bool fuzzy;
		private bool refreshState;
		private bool activated;
		private bool zeroResource;
		private string resourceDisplayName;

		private SCANresourceGlobal scanResource;
		private SCANresourceBody scanResourceBody;

		private bool resourceScanThreshold;

		private BaseField abundanceField;

		private const float RESOURCE_TIME = 2;
		private float resourceTimer = 0;

		public float MaxAbundanceAltitude
		{
			get { return maxAbundanceAltitude; }
		}

		public override void OnStart(PartModule.StartState state)
		{
			if (state == StartState.Editor)
			{
				return;
			}

			abundanceField = Fields["abundance"];

			GameEvents.onVesselSOIChanged.Add(onSOIChange);

			part.force_activate();
			this.isEnabled = true;
			activated = true;
			refreshState = true;
			resourceDisplayName = SCANUtil.displayNameFromResource(ResourceName);

			abundanceField.guiName = resourceDisplayName;

			stockScanners = findScanners();
			scanScanner = findSCANScanner();
			animGroup = findAnimator();

			if (animGroup == null || animGroup.isDeployed)
			{
				enableConnectedScanners();
			}

			setupFields(stockScanners.FirstOrDefault(), scanScanner);

			body = FlightGlobals.currentMainBody;
			refreshAbundance(body.flightGlobalsIndex);

			scanResource = SCANcontroller.getResourceNode(ResourceName);

			if (scanResource != null)
			{
				scanResourceBody = scanResource.getBodyConfig(body.bodyName, false);
			}
		}

		private List<ModuleResourceScanner> findScanners()
		{
			return part.FindModulesImplementing<ModuleResourceScanner>().Where(r => r.ScannerType == 0 && r.ResourceName == ResourceName).ToList();
		}

		private SCANsat findSCANScanner()
		{
			return part.FindModuleImplementing<SCANsat>();
		}

		private ModuleAnimationGroup findAnimator()
		{
			return part.FindModulesImplementing<ModuleAnimationGroup>().FirstOrDefault();
		}

		private void setupFields(ModuleResourceScanner m, SCANsat s)
		{
			if (m != null)
			{
				//SCANUtil.SCANlog("{0} Resource Display Module set to Max Alt: {1} ; Unlock: {2}", m.ResourceName, m.MaxAbundanceAltitude, m.RequiresUnlock);
				maxAbundanceAltitude = m.MaxAbundanceAltitude;
				RequiresUnlock = m.RequiresUnlock;
			}
			else if (s != null)
			{
				maxAbundanceAltitude = s.max_alt;
				RequiresUnlock = true;
			}
			else
			{
				RequiresUnlock = true;
			}

			abundanceField.guiName = string.Format("{0}[{1}]", resourceDisplayName, Localizer.Format("#autoLOC_SCANsat_Surface"));
		}

		private void OnDestroy()
		{
			GameEvents.onVesselSOIChanged.Remove(onSOIChange);
		}

		private void Update()
		{
			if (!HighLogic.LoadedSceneIsFlight || !FlightGlobals.ready)
			{
				return;
			}

			if (part.PartActionWindow == null)
			{
				return;
			}

			if (!SCAN_Settings_Config.Instance.HideZeroResources && Time.realtimeSinceStartup > resourceTimer)
			{
				resourceTimer = Time.realtimeSinceStartup + RESOURCE_TIME;

				SCANdata data = SCANUtil.getData(part.vessel.mainBody);

				if (data != null)
				{
					resourceScanThreshold = SCANUtil.getCoveragePercentage(data, SCANtype.ResourceLoRes) > (SCAN_Settings_Config.Instance.StockTreshold * 100) || SCANUtil.getCoveragePercentage(data, SCANtype.ResourceHiRes) > (SCAN_Settings_Config.Instance.StockTreshold * 100);
				}
				else
				{
					resourceScanThreshold = false;
				}
			}

			if (!activated)
			{
				abundanceField.guiActive = false;
				return;
			}

			if (scanResourceBody != null && scanResourceBody.DefaultZero && (SCAN_Settings_Config.Instance.HideZeroResources || resourceScanThreshold))
			{
				abundanceField.guiActive = false;
				zeroResource = true;
				return;
			}

			if (refreshState)
			{
				if (SCAN_Settings_Config.Instance.DisableStockResource)
				{
					disableConnectedScanners();
				}

				refreshState = false;
			}

			//if (!SCAN_Settings_Config.Instance.DisableStockResource)
			//{
			//    abundanceField.guiActive = false;
			//    return;
			//}

			abundanceField.guiActive = true;
			zeroResource = false;

			if (tooHigh)
			{
				abundance = Localizer.Format("#autoLOC_SCANsat_TooHigh");
				return;
			}
			else if (abundanceValue < 0)
			{
				abundance = Localizer.Format("#autoLOC_SCANsat_NoData");
				return;
			}

			string biome = "Landed";

			if (body.BiomeMap != null)
			{
				biome = SCANUtil.getBiomeName(body, SCANUtil.fixLonShift(vessel.longitude), SCANUtil.fixLatShift(vessel.latitude));
			}

			if (checkBiome(biome) || !SCAN_Settings_Config.Instance.BiomeLock)
			{
				if (fuzzy)
				{
					abundance = SCANuiUtil.LoResourceGroup(abundanceValue);
				}
				else
				{
					abundance = abundanceValue.ToString("P2");
				}
			}
			else
			{
				float biomeAbundance = abundanceSummary.ContainsKey(biome) ? abundanceSummary[biome].Abundance : 0f;
				if (fuzzy)
				{
					abundance = SCANuiUtil.LoResourceGroup(biomeAbundance);
				}
				else
				{
					abundance = string.Format("{0} avg.", biomeAbundance.ToString("P2"));
				}
			}
		}

		private bool checkBiome(string b)
		{
			return ResourceMap.Instance.IsBiomeUnlocked(body.flightGlobalsIndex, b);
		}

		private void FixedUpdate()
		{
			if (!activated)
			{
				abundanceValue = -1f;
				tooHigh = false;
				return;
			}

			if (zeroResource)
			{
				return;
			}

			if (part.PartActionWindow == null)
			{
				return;
			}

			if (!vessel.Landed && ResourceUtilities.GetAltitude(vessel) > maxAbundanceAltitude)
			{
				tooHigh = true;
				return;
			}

			tooHigh = false;
			double lat = SCANUtil.fixLatShift(vessel.latitude);
			double lon = SCANUtil.fixLonShift(vessel.longitude);
			if (SCANUtil.isCovered(lon, lat, vessel.mainBody, (short)SCANtype.ResourceHiRes))
			{
				abundanceValue = SCANUtil.ResourceOverlay(lat, lon, ResourceName, vessel.mainBody, RequiresUnlock && SCAN_Settings_Config.Instance.BiomeLock);
				fuzzy = false;
			}
			else if (SCANUtil.isCovered(lon, lat, vessel.mainBody, (short)SCANtype.ResourceLoRes))
			{
				abundanceValue = SCANUtil.ResourceOverlay(lat, lon, ResourceName, vessel.mainBody, RequiresUnlock && SCAN_Settings_Config.Instance.BiomeLock);
				fuzzy = true;
			}
			else
			{
				abundanceValue = -1;
			}
		}

		private void onSOIChange(GameEvents.HostedFromToAction<Vessel, CelestialBody> VB)
		{
			body = VB.to;
			refreshAbundance(body.flightGlobalsIndex);

			if (scanResource != null)
			{
				scanResourceBody = scanResource.getBodyConfig(body.bodyName, false);
			}
		}

		private void refreshAbundance(int bodyID)
		{
			abundanceSummary = new Dictionary<string, ResourceCache.AbundanceSummary>();

			abundanceSummary = ResourceCache.Instance.AbundanceCache.
				Where(a => a.ResourceName == ResourceName && a.HarvestType == HarvestTypes.Planetary && a.BodyId == bodyID).
				GroupBy(a => a.BiomeName).
				ToDictionary(b => b.Key, b => b.First());
		}

		private void disableConnectedScanners()
		{
			if (stockScanners != null)
			{
				foreach (ModuleResourceScanner m in stockScanners)
				{
					m.DisableModule();
				}
			}
		}

		private void enableConnectedScanners()
		{
			if (stockScanners != null)
			{
				foreach (ModuleResourceScanner m in stockScanners)
				{
					m.EnableModule();
				}
			}
		}

		public void EnableModule()
		{
			activated = true;
			if (SCAN_Settings_Config.Instance.DisableStockResource)
			{
				disableConnectedScanners();
			}
		}

		public void DisableModule()
		{
			activated = false;
			if (SCAN_Settings_Config.Instance.DisableStockResource)
			{
				disableConnectedScanners();
			}
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
