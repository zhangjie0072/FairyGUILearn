using UnityEngine;
using System.Collections;
using UnityEditor;

public class BuildAssetBundles{

	[MenuItem("Window/Build FairyGUI Example Bundles")]
    public static void Builde()
    {
#if UNITY_5
        for(int i = 0; i < 10; i++){
            AssetImporter.GetAtPath("Assets/Resources/Icons/i" + i + ".png").assetBundleName = "bag_icons/i" + i + ".ab";
        }

        BuildPipeline.BuildAssetBundles(Application.streamingAssetsPath, BuildAssetBundleOptions.None, BuildTarget.Android);
#else
        Debug.Log("只有Unity 5+生成ab的方法，没有实现4的方法");
#endif
    }

}
