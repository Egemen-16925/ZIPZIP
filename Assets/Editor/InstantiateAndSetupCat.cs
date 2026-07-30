using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System.IO;

public class InstantiateAndSetupCat
{
    [MenuItem("Tools/Sahnede Kedi Oluştur ve Animasyonu Bağla")]
    public static void CreateCatInScene()
    {
        // 1. FBX Modelini Bul
        string[] guids = AssetDatabase.FindAssets("Meshy_AI_Low_Poly_Tabby");
        if (guids.Length == 0)
        {
            Debug.LogError("[CreateCat] Kedi FBX dosyası bulunamadı!");
            return;
        }

        string fbxPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        Material catMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/MeshyImports/Low-Poly Tabby_20260730_221920/Material_1.mat");

        if (fbxPrefab == null)
        {
            Debug.LogError($"[CreateCat] '{fbxPath}' adresindeki model yüklenemedi!");
            return;
        }

        // Sub-asset'lerden Avatar ve AnimationClip al
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        Avatar catAvatar = null;
        AnimationClip catClip = null;

        foreach (Object obj in subAssets)
        {
            if (obj is Avatar av) catAvatar = av;
            if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__")) catClip = clip;
        }

        // 2. Controller Hazırla
        string animFolder = "Assets/Animations";
        if (!Directory.Exists(animFolder)) Directory.CreateDirectory(animFolder);

        string controllerPath = $"{animFolder}/CatGenericController.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            if (catClip != null)
            {
                AnimatorStateMachine sm = controller.layers[0].stateMachine;
                AnimatorState state = sm.AddState(catClip.name);
                state.motion = catClip;
                sm.defaultState = state;
            }
        }

        // 3. Sahnede Varsa Eski Kediyi Sil, Yenisini Ekle
        GameObject oldCat = GameObject.Find("Kedi");
        if (oldCat != null) Object.DestroyImmediate(oldCat);
        GameObject oldCatLower = GameObject.Find("kedi");
        if (oldCatLower != null) Object.DestroyImmediate(oldCatLower);

        GameObject catInstance = (GameObject)PrefabUtility.InstantiatePrefab(fbxPrefab);
        catInstance.name = "Kedi";
        catInstance.transform.position = Vector3.zero;
        catInstance.transform.rotation = Quaternion.identity;
        catInstance.transform.localScale = Vector3.one;

        Undo.RegisterCreatedObjectUndo(catInstance, "Instantiate Kedi");

        // 4. Animator ve Avatar Bağla
        Animator anim = catInstance.GetComponent<Animator>();
        if (anim == null) anim = catInstance.AddComponent<Animator>();

        SerializedObject animSO = new SerializedObject(anim);
        animSO.FindProperty("m_Controller").objectReferenceValue = controller;
        if (catAvatar != null)
        {
            animSO.FindProperty("m_Avatar").objectReferenceValue = catAvatar;
        }
        animSO.FindProperty("m_CullingType").intValue = 0; // Always Animate
        animSO.ApplyModifiedProperties();

        // 5. CatAutoPlayer Script Ekle
        CatAutoPlayer player = catInstance.GetComponent<CatAutoPlayer>();
        if (player == null) player = catInstance.AddComponent<CatAutoPlayer>();

        // 6. Materyal Bağla
        if (catMat != null)
        {
            Renderer[] renderers = catInstance.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                r.sharedMaterial = catMat;
            }
        }

        EditorUtility.SetDirty(catInstance);
        EditorSceneManager.MarkSceneDirty(catInstance.scene);
        EditorSceneManager.SaveOpenScenes();

        Selection.activeGameObject = catInstance;
        Debug.Log($"[CreateCat] BAŞARILI! Sahnede 'Kedi' objesi oluşturuldu, Avatar ({catAvatar?.name}) ve Controller atandı ve sahne kaydedildi.");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
