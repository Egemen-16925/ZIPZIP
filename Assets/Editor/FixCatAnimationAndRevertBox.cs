using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public class FixCatAnimationAndRevertBox
{
    [MenuItem("Tools/Kedi Animasyonunu Düzelt ve Kedi Kutusunu Geri Al")]
    public static void FixCatAndRevertBox()
    {
        // 1. Kedi Kutusunun (Golden Winged Crate) Materyalini Orijinal Haline Getir
        Material boxMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/MeshyImports/Golden Winged Crate_20260730_221254/Material.001.mat");
        GameObject boxContainer = GameObject.Find("KediKutusu_Parcalanmis");
        if (boxContainer != null && boxMat != null)
        {
            Renderer[] renderers = boxContainer.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                r.sharedMaterial = boxMat;
            }
            EditorSceneManager.MarkSceneDirty(boxContainer.scene);
            Debug.Log("[FixCatAnimationAndRevertBox] Kedi kutusunun materyali orijinal dokusuna (Material.001) geri döndürüldü.");
        }

        // 2. Kedi FBX Model Importer Ayarlarını Düzelt (Legacy Animation Yap)
        string[] guids = AssetDatabase.FindAssets("Meshy_AI_Low_Poly_Tabby");
        if (guids.Length == 0)
        {
            Debug.LogError("[FixCatAnimationAndRevertBox] Kedi FBX dosyası bulunamadı!");
            return;
        }

        string fbxPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;

        if (importer != null)
        {
            importer.animationType = ModelImporterAnimationType.Legacy;
            importer.importAnimation = true;

            // Clip ayarlarında döngüyü aktif et
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips != null && clips.Length > 0)
            {
                foreach (var clip in clips)
                {
                    clip.loopTime = true;
                    clip.wrapMode = WrapMode.Loop;
                }
                importer.clipAnimations = clips;
            }

            importer.SaveAndReimport();
            Debug.Log("[FixCatAnimationAndRevertBox] Kedi FBX modeli Legacy animasyon tipine dönüştürüldü ve reimport edildi.");
        }

        // 3. FBX içindeki AnimationClip'i yükle
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
            catClip.wrapMode = WrapMode.Loop;
        }

        // 4. Sahnede Kedi Objelerini Bul ve Animation Komponenti Bağla
        GameObject catObj = GameObject.Find("kedi");
        if (catObj == null) catObj = GameObject.Find("Kedi");

        if (catObj != null && catClip != null)
        {
            // Eski Animator komponentini kaldır (Avatar olmadan çalışmadığı için)
            Animator oldAnimator = catObj.GetComponent<Animator>();
            if (oldAnimator != null) Object.DestroyImmediate(oldAnimator);

            // Legacy Animation komponenti ekle
            Animation anim = catObj.GetComponent<Animation>();
            if (anim == null) anim = catObj.AddComponent<Animation>();

            anim.clip = catClip;
            anim.AddClip(catClip, catClip.name);
            anim.playAutomatically = true;
            anim.wrapMode = WrapMode.Loop;

            // Kedi Materyalini Bağla
            Material catMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/MeshyImports/Low-Poly Tabby_20260730_221920/Material_1.mat");
            if (catMat != null)
            {
                Renderer[] rList = catObj.GetComponentsInChildren<Renderer>();
                foreach (Renderer r in rList)
                {
                    r.sharedMaterial = catMat;
                }
            }

            EditorUtility.SetDirty(catObj);
            EditorSceneManager.MarkSceneDirty(catObj.scene);
            Debug.Log($"[FixCatAnimationAndRevertBox] '{catObj.name}' objesine Legacy Animation bağlandı. Animasyon: '{catClip.name}'");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.SaveOpenScenes();
    }
}
