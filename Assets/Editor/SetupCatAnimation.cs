using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System.IO;

public class SetupCatAnimation
{
    [MenuItem("Tools/Kedi Animasyonunu Otomatik Çalıştır (Setup Cat Animation)")]
    public static void AutoSetupCatAnimation()
    {
        // 1. Klasör Oluştur
        string animFolder = "Assets/Animations";
        if (!Directory.Exists(animFolder))
        {
            Directory.CreateDirectory(animFolder);
        }

        // 2. FBX Modelini ve Materyalini Bul
        string[] guids = AssetDatabase.FindAssets("Meshy_AI_Low_Poly_Tabby");
        if (guids.Length == 0)
        {
            Debug.LogError("[SetupCatAnimation] Kedi FBX modeli bulunamadı!");
            return;
        }

        string fbxPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        Material catMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/MeshyImports/Low-Poly Tabby_20260730_221920/Material_1.mat");

        // FBX içindeki AnimationClip sub-asset'lerini bul
        Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        AnimationClip catClip = null;

        foreach (Object obj in subAssets)
        {
            if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                catClip = clip;
                break;
            }
        }

        if (catClip != null)
        {
            SerializedObject clipSO = new SerializedObject(catClip);
            SerializedProperty loopTimeProp = clipSO.FindProperty("m_AnimationClipSettings.m_LoopTime");
            if (loopTimeProp != null)
            {
                loopTimeProp.boolValue = true;
                clipSO.ApplyModifiedProperties();
            }
            Debug.Log($"[SetupCatAnimation] Kedi animasyonu bulundu ve Döngü (Loop) aktif edildi: '{catClip.name}'");
        }

        // 3. AnimatorController Oluştur
        string controllerPath = $"{animFolder}/CatAnimatorController.controller";
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        if (catClip != null)
        {
            AnimatorStateMachine rootStateMachine = controller.layers[0].stateMachine;
            AnimatorState state = rootStateMachine.AddState(catClip.name);
            state.motion = catClip;
            rootStateMachine.defaultState = state;
        }

        // 4. Sahnede Kedi Objelerini Bul veya Sahneye Ekle
        GameObject catObj = GameObject.Find("kedi");
        if (catObj == null) catObj = GameObject.Find("Kedi");
        if (catObj == null) catObj = GameObject.Find("Meshy_AI_Low_Poly_Tabby_quadruped_Character_output");

        if (catObj == null && fbxPrefab != null)
        {
            catObj = (GameObject)PrefabUtility.InstantiatePrefab(fbxPrefab);
            catObj.name = "kedi";
            Undo.RegisterCreatedObjectUndo(catObj, "Instantiate Kedi");
            Debug.Log("[SetupCatAnimation] Kedi sahneye 'kedi' adıyla eklendi.");
        }

        if (catObj != null)
        {
            Animator anim = catObj.GetComponent<Animator>();
            if (anim == null) anim = catObj.AddComponent<Animator>();

            anim.runtimeAnimatorController = controller;
            anim.applyRootMotion = false;

            if (catMat != null)
            {
                Renderer[] renderers = catObj.GetComponentsInChildren<Renderer>();
                foreach (Renderer r in renderers)
                {
                    r.sharedMaterial = catMat;
                }
            }

            EditorUtility.SetDirty(catObj);
            EditorSceneManager.MarkSceneDirty(catObj.scene);
            EditorSceneManager.SaveOpenScenes();

            Selection.activeGameObject = catObj;
            Debug.Log($"[SetupCatAnimation] BAŞARILI! '{catObj.name}' objesine AnimatorController atandı ve otomatik animasyon başlatmaya hazırlandı.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
