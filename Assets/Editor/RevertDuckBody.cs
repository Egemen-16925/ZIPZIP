using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class RevertDuckBody
{
    [MenuItem("Tools/Gövde Materyalini Geri Yükle (Revert Duck Body)")]
    public static void RestoreBodyAndSetWingsOnly()
    {
        // 1. Gövde Materyalini (Material.001.mat) Orijinal Haline Getir
        Material bodyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/MeshyImports/Big-Handed Rubber Duck_20260730_212737/Material.001.mat");
        if (bodyMat != null)
        {
            if (bodyMat.HasProperty("_Smoothness")) bodyMat.SetFloat("_Smoothness", 0.5f);
            if (bodyMat.HasProperty("_SpecularHighlights")) bodyMat.SetFloat("_SpecularHighlights", 1f);
            if (bodyMat.HasProperty("_EnvironmentReflections")) bodyMat.SetFloat("_EnvironmentReflections", 1f);
            EditorUtility.SetDirty(bodyMat);
            Debug.Log("[RevertDuckBody] Gövde materyali (Material.001) orijinal parlaklık ve ayarlarına geri yüklendi.");
        }

        // 2. Koyu Sarı Kanat Materyalini Yükle
        Material wingMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DuckWing_DarkYellow.mat");

        // 3. Sahnede Sadece 2 Kanat Objesinin Materyalini Değiştir, Gövdeye Dokunma (veya Orijinal Gövde Materyalini Bağla)
        GameObject rootContainer = GameObject.Find("Ordek_Parcalanmis");
        if (rootContainer != null)
        {
            Transform wing1 = rootContainer.transform.Find("Ordek_Parca_1");
            Transform wing2 = rootContainer.transform.Find("Ordek_Parca_2");
            Transform body  = rootContainer.transform.Find("Ordek_Parca_3");

            // Kanat 1
            if (wing1 != null && wingMat != null)
            {
                MeshRenderer mr1 = wing1.GetComponent<MeshRenderer>();
                if (mr1 != null) mr1.sharedMaterial = wingMat;
            }

            // Kanat 2
            if (wing2 != null && wingMat != null)
            {
                MeshRenderer mr2 = wing2.GetComponent<MeshRenderer>();
                if (mr2 != null) mr2.sharedMaterial = wingMat;
            }

            // Gövde (Orijinal Material.001 materyali kalsın)
            if (body != null && bodyMat != null)
            {
                MeshRenderer mrBody = body.GetComponent<MeshRenderer>();
                if (mrBody != null) mrBody.sharedMaterial = bodyMat;
            }

            EditorSceneManager.MarkSceneDirty(rootContainer.scene);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[RevertDuckBody] Sadece 2 kanat objesine koyu sarı materyal atandı, gövde materyali orijinal haline getirildi.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
