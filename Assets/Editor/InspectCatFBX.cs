using UnityEngine;
using UnityEditor;

public class InspectCatFBX
{
    [MenuItem("Tools/Inspect Cat FBX SubAssets")]
    public static void Inspect()
    {
        string[] guids = AssetDatabase.FindAssets("Meshy_AI_Low_Poly_Tabby");
        if (guids.Length == 0)
        {
            Debug.LogError("Cat FBX not found");
            return;
        }

        string fbxPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        Debug.Log($"FBX Path: {fbxPath}");

        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        Debug.Log($"Total sub-assets in FBX: {allAssets.Length}");

        foreach (Object obj in allAssets)
        {
            Debug.Log($"SubAsset: '{obj.name}' (Type: {obj.GetType().Name})");
        }
    }
}
