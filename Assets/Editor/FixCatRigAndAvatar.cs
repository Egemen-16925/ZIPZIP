using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System.IO;

public class FixCatRigAndAvatar
{
    [MenuItem("Tools/Kedi Rig ve Animasyonunu Tam Onar")]
    public static void RepairCatRigAndAnimation()
    {
        string[] guids = AssetDatabase.FindAssets("Meshy_AI_Low_Poly_Tabby");
        if (guids.Length == 0)
        {
            Debug.LogError("Kedi FBX bulunamadı");
            return;
        }

        string fbxPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;

        if (importer != null)
        {
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;

            ModelImporterClipAnimation[] defaultClips = importer.defaultClipAnimations;
            if (defaultClips != null && defaultClips.Length > 0)
            {
                foreach (var clip in defaultClips)
                {
                    clip.loopTime = true;
                    clip.wrapMode = WrapMode.Loop;
                }
                importer.clipAnimations = defaultClips;
            }

            importer.SaveAndReimport();
            Debug.Log("[FixCatRig] Kedi FBX Generic + Avatar olarak yeniden import edildi.");
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

        Debug.Log($"[FixCatRig] Avatar bulundu: {catAvatar != null}, Clip bulundu: {catClip != null}");

        // Controller Oluştur
        string animFolder = "Assets/Animations";
        if (!Directory.Exists(animFolder)) Directory.CreateDirectory(animFolder);

        string controllerPath = $"{animFolder}/CatGenericController.controller";
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        if (catClip != null)
        {
            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            AnimatorState state = sm.AddState(catClip.name);
            state.motion = catClip;
            sm.defaultState = state;
        }

        // Sahnede Kediye Ata
        GameObject catObj = GameObject.Find("kedi");
        if (catObj == null) catObj = GameObject.Find("Kedi");

        if (catObj != null)
        {
            // Animation komponenti varsa kaldır
            Animation oldAnim = catObj.GetComponent<Animation>();
            if (oldAnim != null) Object.DestroyImmediate(oldAnim);

            Animator animator = catObj.GetComponent<Animator>();
            if (animator == null) animator = catObj.AddComponent<Animator>();

            animator.runtimeAnimatorController = controller;
            if (catAvatar != null) animator.avatar = catAvatar;
            animator.applyRootMotion = false;

            // Kedi Materyalini Koruma
            Material catMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/MeshyImports/Low-Poly Tabby_20260730_221920/Material_1.mat");
            if (catMat != null)
            {
                Renderer[] rends = catObj.GetComponentsInChildren<Renderer>();
                foreach (Renderer r in rends) r.sharedMaterial = catMat;
            }

            EditorUtility.SetDirty(catObj);
            EditorSceneManager.MarkSceneDirty(catObj.scene);
            EditorSceneManager.SaveOpenScenes();

            Debug.Log($"[FixCatRig] BAŞARILI! '{catObj.name}' objesine Avatar ({catAvatar?.name}) ve Controller atandı.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
