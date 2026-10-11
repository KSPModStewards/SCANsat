using UnityEditor;
using UnityEngine;

public class Bundler
{
	const string dir = "AssetBundles";
	const string extension = ".scan";
	static readonly string[] bundles = new string[]
	{
		"scan_prefabs",
		"scan_icons",
		"scan_unity_skin",
		"scan_shaders"
	};

	// Shaders are compiled into one program per graphics API and a bundle only carries the APIs of
	// its build target (Metal only comes from a macOS target), so the shader bundle is per platform.
	// The Windows build is the historical scan_shaders.scan; the Linux and macOS ones are built when
	// the editor has that build-support module, as scan_shaders_linux.scan / scan_shaders_osx.scan.
	// SCAN_UI_Loader picks the file by Application.platform and falls back to the Windows one.
	struct ShaderPlatform
	{
		public BuildTarget target;
		public string subdir;
		public string suffix;
	}

	static readonly ShaderPlatform[] shaderPlatforms = new ShaderPlatform[]
	{
		new ShaderPlatform { target = BuildTarget.StandaloneLinux64, subdir = "linux", suffix = "_linux" },
		new ShaderPlatform { target = BuildTarget.StandaloneOSX, subdir = "osx", suffix = "_osx" },
	};

	const BuildAssetBundleOptions options = BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle;

	[MenuItem("SCANsat/Build All Bundles")]
	static void BuildAllAssetBundles()
	{
		BuildPipeline.BuildAssetBundles(dir, options, BuildTarget.StandaloneWindows);

		foreach (var bundle in bundles)
		{
			var sourceFile = dir + "/" + bundle;
			FileUtil.ReplaceFile(sourceFile, sourceFile + extension);
			//FileUtil.DeleteFile(sourceFile);
		}

		// Only the shader bundle for the other platforms: prefabs, icons and skin are platform-neutral.
		AssetBundleBuild[] shaderBuild = new AssetBundleBuild[]
		{
			new AssetBundleBuild
			{
				assetBundleName = "scan_shaders",
				assetNames = AssetDatabase.GetAssetPathsFromAssetBundle("scan_shaders")
			}
		};

		foreach (var p in shaderPlatforms)
		{
			string output = dir + "/scan_shaders" + p.suffix + extension;

			if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, p.target))
			{
				Debug.LogWarning("[SCANsat bundler] no build support installed for " + p.target + "; " + output + " not built");
				continue;
			}

			string sub = dir + "/" + p.subdir;
			System.IO.Directory.CreateDirectory(sub);
			BuildPipeline.BuildAssetBundles(sub, shaderBuild, options, p.target);
			FileUtil.ReplaceFile(sub + "/scan_shaders", output);
			Debug.Log("[SCANsat bundler] built " + output);
		}
	}
}
