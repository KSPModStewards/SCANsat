using UnityEditor;

public class Bundler
{
	const string dir = "AssetBundles";
	const string extension = ".scan";

    [MenuItem("SCANsat/Build All Bundles")]
    static void BuildAllAssetBundles()
    {
		BuildTarget[] platforms = { BuildTarget.StandaloneWindows
									  , BuildTarget.StandaloneOSX
									  , BuildTarget.StandaloneLinux };

		string[] platformExts = { "-windows", "-macosx", "-linux" };

		for (int i = 0; i < platforms.Length; i++)
		{
			BuildPipeline.BuildAssetBundles(dir, BuildAssetBundleOptions.UncompressedAssetBundle | BuildAssetBundleOptions.ForceRebuildAssetBundle, platforms[i]);

			string outFile = dir + "/scan_shaders" + platformExts[i] + extension;
			FileUtil.ReplaceFile(dir + "/scan_shaders", outFile);
		}

		BuildPipeline.BuildAssetBundles(dir, BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows);

		FileUtil.ReplaceFile(dir + "/scan_prefabs", dir + "/scan_prefabs" + extension);
		FileUtil.ReplaceFile(dir + "/scan_icons", dir + "/scan_icons" + extension);
		FileUtil.ReplaceFile(dir + "/scan_unity_skin", dir + "/scan_unity_skin" + extension);

		FileUtil.DeleteFileOrDirectory(dir + "/scan_prefabs");
		FileUtil.DeleteFileOrDirectory(dir + "/scan_icons");
		FileUtil.DeleteFileOrDirectory(dir + "/scan_unity_skin");

		FileUtil.DeleteFileOrDirectory(dir + "/scan_shaders");
	}

	[MenuItem("SCANsat/Build Core Bundles")]
	static void BuildCoreAssetBundles()
	{
		BuildPipeline.BuildAssetBundles(dir, BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows);

		FileUtil.ReplaceFile(dir + "/scan_prefabs", dir + "/scan_prefabs" + extension);
		FileUtil.ReplaceFile(dir + "/scan_icons", dir + "/scan_icons" + extension);
		FileUtil.ReplaceFile(dir + "/scan_unity_skin", dir + "/scan_unity_skin" + extension);

		FileUtil.DeleteFileOrDirectory(dir + "/scan_prefabs");
		FileUtil.DeleteFileOrDirectory(dir + "/scan_icons");
		FileUtil.DeleteFileOrDirectory(dir + "/scan_unity_skin");

		FileUtil.DeleteFileOrDirectory(dir + "/scan_shaders");
	}


}
