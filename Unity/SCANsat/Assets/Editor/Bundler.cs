using UnityEditor;

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

	[MenuItem("SCANsat/Build All Bundles")]
	static void BuildAllAssetBundles()
	{
		BuildPipeline.BuildAssetBundles(dir, BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.ForceRebuildAssetBundle, BuildTarget.StandaloneWindows);

		foreach (var bundle in bundles)
		{
			var sourceFile = dir + "/" + bundle;
			FileUtil.ReplaceFile(sourceFile, sourceFile + extension);
			//FileUtil.DeleteFile(sourceFile);
		}
	}
}
