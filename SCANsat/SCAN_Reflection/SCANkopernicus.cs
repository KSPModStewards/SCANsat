#region license
/* 
 * [Scientific Committee on Advanced Navigation]
 * 			S.C.A.N. Satellite
 *
 * SCANreflection - assigns reflection methods at startup
 * 
 * Copyright (c)2014 David Grandy <david.grandy@gmail.com>;
 * Copyright (c)2014 technogeeky <technogeeky@gmail.com>;
 * Copyright (c)2014 (Your Name Here) <your email here>; see LICENSE.txt for licensing details.
 */
#endregion

using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Log = KSPCommunityLib.Logging.Log;

namespace SCANsat.SCAN_Reflection
{
	static class SCANkopernicus
	{
		public static bool KopernicusLoaded = false;

		private static Type x_ScaledSpaceOnDemand_Type = null;
		private static MethodInfo x_ScaledSpaceOnDemand_LoadTextures = null;
		private static MethodInfo x_ScaledSpaceOnDemand_UnloadTextures = null;

		private static Type x_PQSMod_OnDemandHandler_Type = null;

		internal static void Initialize(AssemblyLoader.LoadedAssembly kopernicusAssembly)
		{
			try
			{
				x_ScaledSpaceOnDemand_Type = kopernicusAssembly.assembly.GetType("Kopernicus.OnDemand.ScaledSpaceOnDemand");
				x_ScaledSpaceOnDemand_LoadTextures = x_ScaledSpaceOnDemand_Type.GetMethod("LoadTextures", BindingFlags.Instance | BindingFlags.Public);
				x_ScaledSpaceOnDemand_UnloadTextures = x_ScaledSpaceOnDemand_Type.GetMethod("UnloadTextures", BindingFlags.Instance | BindingFlags.Public);

				x_PQSMod_OnDemandHandler_Type = kopernicusAssembly.assembly.GetType("Kopernicus.OnDemand.PQSMod_OnDemandHandler");
			}
			catch(Exception e)
			{
				Log.Exception(e);
			}

			if (x_ScaledSpaceOnDemand_Type == null || x_ScaledSpaceOnDemand_LoadTextures == null || x_ScaledSpaceOnDemand_UnloadTextures == null || x_PQSMod_OnDemandHandler_Type == null)
			{
				Log.Error("SCANsat: Unable to reflect Kopernicus OnDemand methods");
			}
			else
			{
				KopernicusLoaded = true;
			}
		}

		internal static void LoadOnDemand(CelestialBody body)
		{
			if (!KopernicusLoaded) return;

			Component scaledSpaceOnDemand = body.scaledBody.GetComponent(x_ScaledSpaceOnDemand_Type);

			if (scaledSpaceOnDemand == null) return;

			SCANUtil.SCANlog("Loading Kopernicus On Demand Scaled Space Map For {0}", body.bodyName);
			x_ScaledSpaceOnDemand_LoadTextures.Invoke(scaledSpaceOnDemand, null);
		}

		internal static void UnloadOnDemand(CelestialBody body)
		{
			if (!KopernicusLoaded) return;

			Component scaledSpaceOnDemand = body.scaledBody.GetComponent(x_ScaledSpaceOnDemand_Type);

			if (scaledSpaceOnDemand == null) return;

			SCANUtil.SCANlog("Unloading Kopernicus On Demand Scaled Space Map For {0}", body.bodyName);
			x_ScaledSpaceOnDemand_UnloadTextures.Invoke(scaledSpaceOnDemand, null);
		}

		private static PQSMod GetOnDemandHandlerPQSMod(CelestialBody body)
		{
			return body.GetComponentsInChildren(x_PQSMod_OnDemandHandler_Type, true).FirstOrDefault() as PQSMod;
		}

		internal static void LoadPQS(CelestialBody body)
		{
			if (!KopernicusLoaded) return;

			PQSMod KopernicusOnDemand = GetOnDemandHandlerPQSMod(body);

			if (KopernicusOnDemand == null)
			{
				return;
			}

			SCANUtil.SCANlog("Loading Kopernicus On Demand PQSMod For {0}", body.bodyName);
			KopernicusOnDemand.OnQuadPreBuild(null);
		}

		internal static void UnloadPQS(CelestialBody body)
		{
			if (!KopernicusLoaded) return;

			PQSMod KopernicusOnDemand = GetOnDemandHandlerPQSMod(body);

			if (KopernicusOnDemand == null)
			{
				return;
			}

			KopernicusOnDemand.OnSphereInactive();

			SCANUtil.SCANlog("Unloading Kopernicus On Demand PQSMod For {0}", body.bodyName);
		}
	}
}
