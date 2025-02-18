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

using System;
using System.Collections.Generic;
using System.Linq;
using SCANsat.SCAN_Data;
using KSP.Localization;

namespace SCANsat.SCAN_PartModules
{
	public class SCANresourceDisplay : PartModule, IAnimatedModule
	{
		[KSPField]
		public int sensorType;
		[KSPField(guiActive = true)]
		public string abundanceField;
		[KSPField]
		public string ResourceName;
		[KSPField]
		public float MaxAbundanceAltitude;
		[KSPField]
		public bool RequiresUnlock;

		private float abundanceValue;

		private List<ModuleResourceScanner> stockScanners;
		private ModuleAnimationGroup animGroup;
		private Dictionary<string, ResourceCache.AbundanceSummary> abundanceSummary;
		private CelestialBody body;
		private bool tooHigh;
		private bool fuzzy;
		private bool refreshState;
		private bool activated;
		private string resourceDisplayName;

		public override void OnStart(PartModule.StartState state)
		{
			if (state == StartState.Editor)
				return;

			Fields["abundanceField"].guiName = Localization.Format("#autoLOC_SCANsat_Abundance");

			GameEvents.onVesselSOIChanged.Add(onSOIChange);

			part.force_activate();
			this.isEnabled = true;
			activated = true;
			refreshState = true;
			resourceDisplayName = SCANUtil.displayNameFromResource(ResourceName);

			stockScanners = findScanners();
			animGroup = findAnimator();

			if (animGroup == null || animGroup.isDeployed)
				enableConnectedScanners();

			setupFields(stockScanners.FirstOrDefault());

			body = FlightGlobals.currentMainBody;
			refreshAbundance(body.flightGlobalsIndex);
		}

		private List<ModuleResourceScanner> findScanners()
		{
			return part.FindModulesImplementing<ModuleResourceScanner>().Where(r => r.ScannerType == 0 && r.ResourceName == ResourceName).ToList();
		}

		private ModuleAnimationGroup findAnimator()
		{
			return part.FindModulesImplementing<ModuleAnimationGroup>().FirstOrDefault();
		}

		private void setupFields(ModuleResourceScanner m)
		{
			if (m != null)
			{
				SCANUtil.SCANlog("{0} Resource Display Module set to Max Alt: {1} ; Unlock: {2}", m.ResourceName, m.MaxAbundanceAltitude, m.RequiresUnlock);
				MaxAbundanceAltitude = m.MaxAbundanceAltitude;
				RequiresUnlock = m.RequiresUnlock;
			}
			else
			{
				MaxAbundanceAltitude = 250000;
				RequiresUnlock = true;
			}

			Fields["abundanceField"].guiName = string.Format("{0}[{1}]", resourceDisplayName, Localization.Format("#autoLOC_SCANsat_Surface"));
		}

		private void OnDestroy()
		{
			GameEvents.onVesselSOIChanged.Remove(onSOIChange);
		}

		private void Update()
		{
			if (!activated)
			{
				Fields["abundanceField"].guiActive = false;
				return;
			}

			if (!HighLogic.LoadedSceneIsFlight || !FlightGlobals.ready)
				return;

			if (refreshState)
			{
				if (SCAN_Settings_Config.Instance.DisableStockResource)
					disableConnectedScanners();
				refreshState = false;
			}

			if (!SCAN_Settings_Config.Instance.DisableStockResource)
			{
				Fields["abundanceField"].guiActive = false;
				return;
			}

			Fields["abundanceField"].guiActive = true;

			if (tooHigh)
			{
				abundanceField = Localization.Format("#autoLOC_SCANsat_TooHigh");
				return;
			}
			else if (abundanceValue < 0)
			{
				abundanceField = Localization.Format("#autoLOC_SCANsat_NoData");
				return;
			}

			string biome = "Landed";

			if (body.BiomeMap != null)
				biome = SCANUtil.getBiomeName(body, SCANUtil.fixLonShift(vessel.longitude), SCANUtil.fixLatShift(vessel.latitude));

			if (checkBiome(biome) || !SCAN_Settings_Config.Instance.BiomeLock)
			{
				if (fuzzy)
					abundanceField = abundanceValue.ToString("P0");
				else
					abundanceField = abundanceValue.ToString("P2");
			}
			else
			{
				float biomeAbundance = abundanceSummary.ContainsKey(biome) ? abundanceSummary[biome].Abundance : 0f;
				if (fuzzy)
					abundanceField = biomeAbundance.ToString("P0") + "avg.";
				else
					abundanceField = biomeAbundance.ToString("P2") + "avg.";
			}
		}

		private bool checkBiome (string b)
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

			if (!vessel.Landed && ResourceUtilities.GetAltitude(vessel) > MaxAbundanceAltitude)
			{
				tooHigh = true;
				return;
			}

			tooHigh = false;
			double lat = SCANUtil.fixLatShift(vessel.latitude);
			double lon = SCANUtil.fixLonShift(vessel.longitude);
			if (SCANUtil.isCovered(lon, lat, vessel.mainBody, sensorType))
			{
				abundanceValue = SCANUtil.ResourceOverlay(lat, lon, ResourceName, vessel.mainBody, RequiresUnlock && SCAN_Settings_Config.Instance.BiomeLock);
				fuzzy = false;
			}
			else if (SCANUtil.isCovered(lon, lat, vessel.mainBody, 524288))
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
				disableConnectedScanners();
		}

		public void DisableModule()
		{
			activated = false;
			if (SCAN_Settings_Config.Instance.DisableStockResource)
				disableConnectedScanners();
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
