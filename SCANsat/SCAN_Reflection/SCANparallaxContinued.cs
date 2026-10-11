using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Log = KSPCommunityLib.Logging.Log;

namespace SCANsat.SCAN_Reflection
{
	static class SCANparallaxContinued
	{
		static MethodInfo x_ParallaxScaledBody_Load = null;
		static MethodInfo x_ParallaxScaledBody_SetScaledMaterialParams = null;
		static FieldInfo x_ParallaxScaledBody_scaledMaterial = null;
		
		static FieldInfo x_ConfigLoader_parallaxScaledBodies = null;

		internal static bool ParallaxContinuedLoaded = false;

		internal static void Initialize(AssemblyLoader.LoadedAssembly parallaxAssembly)
		{
			try
			{
				Type parallaxScaledBody_Type = parallaxAssembly.assembly.GetType("Parallax.ParallaxScaledBody");
				x_ParallaxScaledBody_Load = parallaxScaledBody_Type.GetMethod("Load", BindingFlags.Instance | BindingFlags.Public);
				x_ParallaxScaledBody_scaledMaterial = parallaxScaledBody_Type.GetField("scaledMaterial", BindingFlags.Instance | BindingFlags.Public);
				//x_ParallaxScaledBody_SetScaledMaterialParams = parallaxScaledBody_Type.GetMethod("SetScaledMaterialParams", BindingFlags.Instance | BindingFlags.Public);

				Type parallaxConfigLoader_Type = parallaxAssembly.assembly.GetType("Parallax.ConfigLoader");
				x_ConfigLoader_parallaxScaledBodies = parallaxConfigLoader_Type.GetField("parallaxScaledBodies", BindingFlags.Static | BindingFlags.Public);
			}
			catch (Exception e)
			{
				Log.Exception(e);
			}

			if (x_ParallaxScaledBody_Load == null || x_ParallaxScaledBody_scaledMaterial == null || x_ConfigLoader_parallaxScaledBodies == null)
			{
				Log.Error("Failed to initialize Parallax Continued reflection methods");
			}
			else
			{
				ParallaxContinuedLoaded = true;
			}
		}

		internal static void LoadParallax(CelestialBody body, ref Material material)
		{
			if (!ParallaxContinuedLoaded) return;

			var parallaxBodies = x_ConfigLoader_parallaxScaledBodies.GetValue(null) as System.Collections.IDictionary;

			if (parallaxBodies == null) return;

			if (parallaxBodies.Contains(body.name))
			{
				var parallaxBody = parallaxBodies[body.name];

				if (parallaxBody != null)
				{
					Log.Message($"Loading Parallax Continued data for {body.name}");
					x_ParallaxScaledBody_Load.Invoke(parallaxBody, null);
					material = x_ParallaxScaledBody_scaledMaterial.GetValue(parallaxBody) as Material;

#if DEBUG
					for (int propertyIndex = 0; propertyIndex < material.shader.GetPropertyCount(); ++propertyIndex)
					{
						var propertyType = material.shader.GetPropertyType(propertyIndex);
						var propertyName = material.shader.GetPropertyName(propertyIndex);
						object propertyValue = null;

						switch (propertyType)
						{
							case UnityEngine.Rendering.ShaderPropertyType.Color: propertyValue = material.GetColor(propertyName); break;
							case UnityEngine.Rendering.ShaderPropertyType.Vector: propertyValue = material.GetVector(propertyName); break;
							case UnityEngine.Rendering.ShaderPropertyType.Float: propertyValue = material.GetFloat(propertyName); break;
							case UnityEngine.Rendering.ShaderPropertyType.Range: propertyValue = material.GetFloat(propertyName); break;
							case UnityEngine.Rendering.ShaderPropertyType.Texture: propertyValue = material.GetTexture(propertyName); break;
						}

						Log.Debug($"Property {propertyIndex} is {propertyType} named {propertyName} with value {propertyValue}");
					}
#endif
				}
			}
		}
	}
}