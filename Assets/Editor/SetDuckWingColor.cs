using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class SetDuckWingColor
{
    [MenuItem("Tools/Ördek Kanat Rengini Aç ve Parçaları Güncelle")]
    public static void ApplyLighterYellowAndSetParts()
    {
        // 1. Kanat Materyalinin Rengini Daha Açık/Canlı Sarı Yap
        Material wingMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DuckWing_DarkYellow.mat");
        if (wingMat != null)
        {
            // Daha açık, canlı sarı renk
            Color lightYellow = new Color(0.96f, 0.78f, 0.12f, 1.0f);
            if (wingMat.HasProperty("_BaseColor")) wingMat.SetColor("_BaseColor", lightYellow);
            if (wingMat.HasProperty("_Color")) wingMat.SetColor("_Color", lightYellow);
            if (wingMat.HasProperty("_Smoothness")) wingMat.SetFloat("_Smoothness", 0.2f);
            
            EditorUtility.SetDirty(wingMat);
            Debug.Log("[SetDuckWingColor] Kanat materyali rengi daha açık canlı sarıya güncellendi.");
        }

        // 2. Orijinal Gövde Materyali
        Material bodyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/MeshyImports/Big-Handed Rubber Duck_20260730_212737/Material.001.mat");

        // 3. Sahnede Parça 2 ve Parça 3'ü Kanat Yap, Parça 1'i Gövde Yap
        GameObject rootContainer = GameObject.Find("Ordek_Parcalanmis");
        if (rootContainer != null)
        {
            Transform p1 = rootContainer.transform.Find("Ordek_Parca_1");
            Transform p2 = rootContainer.transform.Find("Ordek_Parca_2");
            Transform p3 = rootContainer.transform.Find("Ordek_Parca_3");

            // Parça 1 -> Gövde (Orijinal dokulu materyal)
            if (p1 != null && bodyMat != null)
            {
                MeshRenderer mr1 = p1.GetComponent<MeshRenderer>();
                if (mr1 != null) mr1.sharedMaterial = bodyMat;
            }

            // Parça 2 -> Kanat 1 (Açık sarı materyal)
            if (p2 != null && wingMat != null)
            {
                MeshRenderer mr2 = p2.GetComponent<MeshRenderer>();
                if (mr2 != null) mr2.sharedMaterial = wingMat;
            }

            // Parça 3 -> Kanat 2 (Açık sarı materyal)
            if (p3 != null && wingMat != null)
            {
                MeshRenderer mr3 = p3.GetComponent<MeshRenderer>();
                if (mr3 != null) mr3.sharedMaterial = wingMat;
            }

            // WingFlapper script'inde kanatları Parça 2 ve Parça 3 olarak güncelle
            WingFlapper flapper = rootContainer.GetComponent<WingFlapper>();
            if (flapper != null)
            {
                flapper.wing1 = p2;
                flapper.wing2 = p3;
                flapper.SaveInitialRotations();
                EditorUtility.SetDirty(flapper);
            }

            EditorSceneManager.MarkSceneDirty(rootContainer.scene);
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[SetDuckWingColor] Ordek_Parca_2 ve Ordek_Parca_3 kanat yapılıp açık sarı materyal atandı. Parça 1 gövde olarak korundu.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
