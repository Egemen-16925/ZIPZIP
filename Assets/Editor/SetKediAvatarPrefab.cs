using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class SetKediAvatarPrefab
{
    [MenuItem("Tools/Kedi Avatar Modifikasyonunu Kaydet")]
    public static void FixAvatarOnPrefab()
    {
        string[] guids = AssetDatabase.FindAssets("Meshy_AI_Low_Poly_Tabby");
        if (guids.Length == 0) return;

        string fbxPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        Avatar catAvatar = null;
        foreach (Object obj in subAssets)
        {
            if (obj is Avatar av) { catAvatar = av; break; }
        }

        GameObject catObj = GameObject.Find("Kedi");
        if (catObj == null) catObj = GameObject.Find("kedi");

        if (catObj != null && catAvatar != null)
        {
            Animator anim = catObj.GetComponent<Animator>();
            if (anim != null)
            {
                anim.avatar = catAvatar;
                PrefabUtility.RecordPrefabInstancePropertyModifications(anim);
                EditorUtility.SetDirty(anim);
                EditorSceneManager.MarkSceneDirty(catObj.scene);
                EditorSceneManager.SaveOpenScenes();
                Debug.Log($"[SetKediAvatarPrefab] BAŞARILI! '{catObj.name}' objesinin Animator'ına Avatar ({catAvatar.name}) atandı ve Prefab modifikasyonu kaydedildi!");
            }
        }
    }
}
