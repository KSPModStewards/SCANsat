#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANversions - logs version numbers for SCANsat and various associated mods
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace SCANsat
{
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	public class SCANmainMenuLoader : MonoBehaviour
	{
		private string[] Assemblies = new string[9] { "SCANsatKethane", "RasterPropMonitor", "MechJeb2", "ContractConfigurator", "CC_SCANsat", "SCANmechjeb", "ModuleManager", "Kopernicus", "Kopernicus.OnDemand" };

		internal static string SCANsatVersion = "";
		internal static bool FinePrintFlightBand = false;
		internal static bool FinePrintStationaryWaypoint = false;
		public static bool MechJebLoaded = false;
		public static bool MMLoaded = false;
		public static bool KopernicusLoaded = false;

		private static Texture2D orbitIconsMap;

		public static Texture2D OrbitIconsMap
		{
			get { return orbitIconsMap; }
		}

		private static bool loaded;

		private List<AssemblyLog> assemblyList = new List<AssemblyLog>();

		private void Start()
		{
			if (loaded)
			{
				Destroy(gameObject);
				return;
			}

			loaded = true;

			PopulateCosLookupArray();

			if (orbitIconsMap == null)
			{
				getOrbitIcons();
			}

			findAssemblies(Assemblies);
			FinePrintStationaryWaypoint = SCANreflection.FinePrintStationaryWaypointReflection();
			FinePrintFlightBand = SCANreflection.FinePrintFlightBandReflection();
			SCANconfigLoader.configLoader();
		}

		private void PopulateCosLookupArray()
		{
			for (int i = 0; i < 180; i++)
			{
				SCANUtil.cosLookUp[i] = Math.Cos((i - 90) * 0.0174532924);
			}
		}

		private void getOrbitIcons()
		{
			foreach (Texture2D t in Resources.FindObjectsOfTypeAll<Texture2D>())
			{
				if (t.name == "OrbitIcons")
				{
					orbitIconsMap = t;
					break;
				}
			}
		}

		private void findAssemblies(string[] assemblies)
		{
			assemblyList.Add(new AssemblyLog(AssemblyLoader.loadedAssemblies.GetByAssembly(Assembly.GetExecutingAssembly()))); //More reliable method for SCANsat.dll
			foreach (string name in assemblies)
			{ //Search for the relevant plugins among the loaded assemblies
				var assembly = AssemblyLoader.loadedAssemblies.FirstOrDefault(a => a.assembly.GetName().Name == name);
				if (assembly != null)
				{
					AssemblyLog alog = new AssemblyLog(assembly);
					assemblyList.Add(alog);
					if (alog.name == "ModuleManager")
					{
						MMLoaded = true;
					}
					else if (alog.name == "Kopernicus")
					{
						KopernicusLoaded = true;
						//SCANreflection.LoadKopernicusReflection();
					}
				}
			}
			if (assemblyList.Count > 0)
			{
				SCANsatVersion = assemblyList[0].infoVersion;
				debugWriter();
			}

			//foreach(AssemblyLoader.LoadedAssembly ass in AssemblyLoader.loadedAssemblies)
			//{
			//    AssemblyLog asslog = new AssemblyLog(ass);

			//    print(string.Format("[SCANsat] Assembly: {0} found; Version: {1}; File Version: {2}; Info Version: {3}; Location: {4}"
			//        , asslog.name, asslog.version, asslog.fileVersion, asslog.infoVersion, asslog.location));

			//}
		}

		private void debugWriter()
		{
			foreach (AssemblyLog log in assemblyList)
			{
				print(string.Format("[SCANsat] Assembly: {0} found; Version: {1}; File Version: {2}; Info Version: {3}; Location: {4}", log.name, log.version, log.fileVersion, log.infoVersion, log.location));
			}
		}

	}

	//A class to gather and store information about assemblies
	internal class AssemblyLog
	{
		internal string name, version, fileVersion, infoVersion, location;
		internal Assembly assemblyLoaded;

		internal AssemblyLog(AssemblyLoader.LoadedAssembly Assembly)
		{
			assemblyLoaded = Assembly.assembly;
			var ainfoV = Attribute.GetCustomAttribute(Assembly.assembly, typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
			var afileV = Attribute.GetCustomAttribute(Assembly.assembly, typeof(AssemblyFileVersionAttribute)) as AssemblyFileVersionAttribute;

			switch (afileV == null)
			{
				case true: fileVersion = ""; break;
				default: fileVersion = afileV.Version; break;
			}

			switch (ainfoV == null)
			{
				case true: infoVersion = ""; break;
				default: infoVersion = ainfoV.InformationalVersion; break;
			}

			name = Assembly.assembly.GetName().Name;
			version = Assembly.assembly.GetName().Version.ToString();
			location = Assembly.url.ToString();
		}

	}

}
