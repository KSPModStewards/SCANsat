using UnityEditor;
using UnityEngine;

public class Bundler
{
    [MenuItem("SCANsat_Shaders/Build Bundles")]
    static void BuildAllAssetBundles()
    {
		string dir = "Bundles";

		BuildTarget[] platforms = { BuildTarget.StandaloneWindows
									  , BuildTarget.StandaloneOSX
									  , BuildTarget.StandaloneLinux64 };

		string[] platformExts = { "-windows", "-macosx", "-linux" };

		for (int i = 0; i < platforms.Length; i++)
		{
			BuildPipeline.BuildAssetBundles(dir, BuildAssetBundleOptions.UncompressedAssetBundle, platforms[i]);

			string outFile = dir + "/scan_shaders" + platformExts[i] + ".scan";
			FileUtil.ReplaceFile(dir + "/scan_shaders", outFile);
		}

    }
}
