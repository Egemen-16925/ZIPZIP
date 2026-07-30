using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public class FixDuckMaterials
{
    [MenuItem("Tools/Ördek Materyal ve Parlaklık Düzenle (Fix Duck Materials)")]
    public static void ApplyDuckFixes()
    {
        // 1. Klasör Oluştur
        string matFolder = "Assets/Materials";
        if (!Directory.Exists(matFolder))
        {
            Directory.CreateDirectory(matFolder);
        }

        // 2. Koyu Sarı Kanat Materyali Oluştur
        string wingMatPath = $"{matFolder}/DuckWing_DarkYellow.mat";
        Material wingMat = AssetDatabase.LoadAssetAtPath<Material>(wingMatPath);
        if (wingMat == null)
        {
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader == null) urpLitShader = Shader.Find("Standard");

            wingMat = new Material(urpLitShader);
            wingMat.name = "DuckWing_DarkYellow";
            
            // Koyu Sarı Renk (Golden / Dark Yellow)
            Color darkYellow = new Color(0.82f, 0.52f, 0.05f, 1.0f);
            if (wingMat.HasProperty("_BaseColor")) wingMat.SetColor("_BaseColor", darkYellow);
            if (wingMat.HasProperty("_Color")) wingMat.SetColor("_Color", darkYellow);

            // Parlaklığı düşür
            if (wingMat.HasProperty("_Smoothness")) wingMat.SetFloat("_Smoothness", 0.1f);
            if (wingMat.HasProperty("_SpecularHighlights")) wingMat.SetFloat("_SpecularHighlights", 0f);
            if (wingMat.HasProperty("_EnvironmentReflections")) wingMat.SetFloat("_EnvironmentReflections", 0f);

            AssetDatabase.CreateAsset(wingMat, wingMatPath);
            Debug.Log($"[FixDuckMaterials] Koyu sarı kanat materyali oluşturuldu: {wingMatPath}");
        }

        // 3. Ördek Gövde Materyalinin Parlaklığını Düşür
        Material bodyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/MeshyImports/Big-Handed Rubber Duck_20260730_212737/Material.001.mat");
        if (bodyMat != null)
        {
            if (bodyMat.HasProperty("_Smoothness")) bodyMat.SetFloat("_Smoothness", 0.05f);
            if (bodyMat.HasProperty("_SpecularHighlights")) bodyMat.SetFloat("_SpecularHighlights", 0f);
            if (bodyMat.HasProperty("_EnvironmentReflections")) bodyMat.SetFloat("_EnvironmentReflections", 0f);
            EditorUtility.SetDirty(bodyMat);
            Debug.Log("[FixDuckMaterials] Ördek gövde materyalinin (Material.001) yansıma ve parlaklığı düşürüldü.");
        }

        // 4. Sahnede Ördek Objelerini Bul ve Kanatlara Yeni Materyali Ata
        GameObject rootContainer = GameObject.Find("Ordek_Parcalanmis");
        if (rootContainer != null)
        {
            // Ordek_Parca_1 ve Ordek_Parca_2 kanatlardır
            Transform wing1 = rootContainer.transform.Find("Ordek_Parca_1");
            Transform wing2 = rootContainer.transform.Find("Ordek_Parca_2");

            if (wing1 != null)
            {
                MeshRenderer mr1 = wing1.GetComponent<MeshRenderer>();
                if (mr1 != null) mr1.sharedMaterial = wingMat;
            }

            if (wing2 != null)
            {
                MeshRenderer mr2 = wing2.GetComponent<MeshRenderer>();
                if (mr2 != null) mr2.sharedMaterial = wingMat;
            }

            EditorSceneManager.MarkSceneDirty(rootContainer.scene);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[FixDuckMaterials] Sahnede ördeğin kanatlarına koyu sarı materyal atandı ve sahne kaydedildi.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
