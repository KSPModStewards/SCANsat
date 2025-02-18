#region license
/*  [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANexperiment - Part module for controlling science experiments
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
using KSP.UI.Screens.Flight.Dialogs;
using KSP.Localization;

namespace SCANsat.SCAN_PartModules
{
	public class SCANexperiment : PartModule, IScienceDataContainer
	{
		[KSPField]
		public string experimentType;

		private SCANexperimentType expType;
		private List<ScienceData> storedData = new List<ScienceData>();
		private ExperimentsResultDialog expDialog = null;

		public override void OnStart(PartModule.StartState state)
		{
			base.OnStart(state);

			try
			{
				expType = (SCANexperimentType)Enum.Parse(typeof(SCANexperimentType), experimentType);
			}
			catch (Exception e)
			{
				expType = SCANexperimentType.Nothing;
				SCANUtil.SCANlog("Error assigning SCANsat experiment type - Type: {0}\n{1}", experimentType, e);
			}

			if (expType == SCANexperimentType.Nothing)
			{
				Events["analyze"].active = false;
				Actions["analyzeData"].active = false;
			}
			else
			{
				Events["analyze"].active = true;
				Actions["analyzeData"].active = false;
			}

			UpdateEventNames();
		}

		private void UpdateEventNames()
		{
			string dataType = "";

			switch(expType)
			{
				case SCANexperimentType.SCANsatAltimetryLoRes:
					dataType = "RADAR";
					break;
				case SCANexperimentType.SCANsatAltimetryHiRes:
					dataType = "SAR";
					break;
				case SCANexperimentType.SCANsatBiomeAnomaly:
					dataType = "Multispectral";
					break;
				case SCANexperimentType.SCANsatResources:
					dataType = "Resources";
					break;
			}

			Events["analyze"].guiName = string.Format("{0}: {1}", Localization.Format("#autoLOC_SCANsat_Analyze"), dataType);
			Events["reviewEvent"].guiName = string.Format("{0}: {1}", Localization.Format("#autoLOC_502204"), dataType);
			Events["EVACollect"].guiName = string.Format("{0}: {1}", Localization.Format("#autoLOC_6004057"), dataType);
			Actions["analyzeData"].guiName = string.Format("{0}: {1}", Localization.Format("#autoLOC_SCANsat_Analyze"), dataType);
		}

		private void Update()
		{
			Events["reviewEvent"].active = storedData.Count > 0;
			Events["EVACollect"].active = storedData.Count > 0;
		}

		public override void OnLoad(ConfigNode node)
		{
			if (node.HasNode("ScienceData"))
			{
				foreach (ConfigNode storedDataNode in node.GetNodes("ScienceData"))
				{
					ScienceData data = new ScienceData(storedDataNode);
					storedData.Add(data);
				}
			}
		}

		public override void OnSave(ConfigNode node)
		{
			node.RemoveNodes("ScienceData");

			foreach (ScienceData SCANData in storedData)
			{
				ConfigNode storedDataNode = node.AddNode("ScienceData");
				SCANData.Save(storedDataNode);
			}
		}


		[KSPEvent(guiActiveUnfocused = true, guiName = "Collect Stored Data", externalToEVAOnly = true, unfocusedRange = 1.5f, active = false)]
		public void EVACollect()
		{
			List<ModuleScienceContainer> EVACont = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceContainer>();
			if (storedData.Count > 0)
			{
				if (EVACont.First().StoreData(new List<IScienceDataContainer>() { this }, false))
				{
					foreach (ScienceData data in storedData)
						DumpData(data);
				}
			}
		}

		[KSPEvent(guiActive = true, guiName = "Analyze Data", active = true)]
		public void analyze()
		{
			gatherScienceData();
		}

		[KSPAction("Analyze Data")]
		public void analyzeData(KSPActionParam param)
		{
			gatherScienceData();
		}

		public void gatherScienceData(bool silent = false)
		{
			makeScienceData();

			if (!silent)
				ReviewData();
		}

		[KSPEvent(guiActive = true, guiName = "Review Data", active = false)]
		public void reviewEvent()
		{
			ReviewData();
		}

		private void makeScienceData()
		{
			storedData.Clear();
			ScienceData sd = getAvailableScience();
			if (sd == null)
				return;
			storedData.Add(sd);
		}

		private ScienceData getAvailableScience()
		{
			SCANdata data = SCANUtil.getData(vessel.mainBody);

			if (data == null)
				return null;

			ScienceData sd = null;
			ScienceExperiment se = null;
			ScienceSubject su = null;
			float coverage = 0;
			float multiplier = 1f;

			switch (expType)
			{
				case SCANexperimentType.SCANsatAltimetryLoRes:
					if (vessel.mainBody.pqsController == null)
						multiplier = 0.5f;

					coverage = (float)SCANUtil.getCoveragePercentage(data, SCANtype.AltimetryLoRes);

					coverage = GetScienceCoverage(expType.ToString(), ref se, ref su, coverage, multiplier);
					break;
				case SCANexperimentType.SCANsatAltimetryHiRes:
					if (vessel.mainBody.pqsController == null)
						multiplier = 0.5f;

					coverage = (float)SCANUtil.getCoveragePercentage(data, SCANtype.AltimetryHiRes);

					coverage = GetScienceCoverage(expType.ToString(), ref se, ref su, coverage, multiplier);
					break;
				case SCANexperimentType.SCANsatBiomeAnomaly:
					if (vessel.mainBody.BiomeMap == null)
						multiplier = 0.5f;

					coverage = (float)SCANUtil.getCoveragePercentage(data, SCANtype.Biome);

					coverage = GetScienceCoverage(expType.ToString(), ref se, ref su, coverage, multiplier);
					break;
				case SCANexperimentType.SCANsatResources:
					coverage = (float)SCANUtil.getCoveragePercentage(data, SCANtype.FuzzyResources);

					coverage = GetScienceCoverage(expType.ToString(), ref se, ref su, coverage, multiplier);
					break;
			}

			if (su == null || se == null)
				return null;

			su.scientificValue = 1;

			coverage = Math.Max(0, (coverage - su.science));
			coverage /= su.subjectValue;

			if (coverage <= 0)
				coverage = 0.0000001f;

			string title = Localization.Format("#autoLOC_301689", se.experimentTitle, vessel.mainBody.displayName.LocalizeNameNeutral());

			su.title = title;

			sd = new ScienceData(coverage * su.dataScale, 1, 0, su.id, su.title, false, part.flightID);
			return sd;
		}

		private float GetScienceCoverage(string scienceID, ref ScienceExperiment se, ref ScienceSubject su, float coverage, float mult)
		{
			se = ResearchAndDevelopment.GetExperiment(scienceID);

			if (se == null)
				return 0;

			su = ResearchAndDevelopment.GetExperimentSubject(se, ExperimentSituations.InSpaceHigh, vessel.mainBody, "", "");

			if (su == null)
				return 0;

			su.scienceCap *= mult;

			if (coverage > 95)
				coverage = 100;
			else if (coverage < 30)
				coverage = 0;

			coverage /= 100;

			coverage *= su.scienceCap;

			return coverage;
		}

		public ScienceData[] GetData()
		{
			return storedData.ToArray();
		}

		public void ReturnData(ScienceData data)
		{
			if (data == null)
				return;

			storedData.Clear();

			storedData.Add(data);
		}

		private void KeepData(ScienceData data)
		{
			expDialog = null;
		}

		private void TransmitData(ScienceData data)
		{
			expDialog = null;

			IScienceDataTransmitter bestTransmitter = ScienceUtil.GetBestTransmitter(vessel);

			if (bestTransmitter != null)
			{
				bestTransmitter.TransmitData(new List<ScienceData> { data });
				DumpData(data);
			}
			else if (CommNet.CommNetScenario.CommNetEnabled)
				ScreenMessages.PostScreenMessage(Localization.Format("#autoLOC_237738"), 3f, ScreenMessageStyle.UPPER_CENTER);
			else
				ScreenMessages.PostScreenMessage(Localization.Format("#autoLOC_237740"), 3f, ScreenMessageStyle.UPPER_CENTER);
		}

		private void LabData(ScienceData data)
		{
			expDialog = null;
			ScienceLabSearch labSearch = new ScienceLabSearch(vessel, data);

			if (labSearch.NextLabForDataFound)
			{
				StartCoroutine(labSearch.NextLabForData.ProcessData(data, null));
				DumpData(data);
			}
			else
				labSearch.PostErrorToScreen();
		}

		public void DumpData(ScienceData data)
		{
			expDialog = null;

			if (storedData.Contains(data))
				storedData.Remove(data);
		}

		public void ReviewDataItem(ScienceData sd)
		{
			ReviewData();
		}

		public void ReviewData()
		{
			if (storedData.Count < 1)
				return;

			expDialog = null;
			ScienceData sd = storedData[0];
			expDialog = ExperimentsResultDialog.DisplayResult(
				new ExperimentResultDialogPage(
					part, sd, 1f, 0f, false, "", true, new ScienceLabSearch(vessel, sd), DumpData, KeepData, TransmitData, LabData));
		}

		public bool IsRerunnable()
		{
			return true;
		}

		public int GetScienceCount()
		{
			return storedData.Count;
		}
	}
}
