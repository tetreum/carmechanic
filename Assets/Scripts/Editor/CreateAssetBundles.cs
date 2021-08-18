using System.IO;
using UnityEditor;
using UnityEngine;

public class CreateAssetBundles : MonoBehaviour
{
    [MenuItem("AssetBundles/generate windows")]
    private static void BuildAllAssetBundlesWindows()
    {
        var assetBundleDirectory = Application.streamingAssetsPath;
        if (!Directory.Exists(assetBundleDirectory)) Directory.CreateDirectory(assetBundleDirectory);
        BuildPipeline.BuildAssetBundles(assetBundleDirectory,
            BuildAssetBundleOptions.None,
            BuildTarget.StandaloneWindows64);
    }
}