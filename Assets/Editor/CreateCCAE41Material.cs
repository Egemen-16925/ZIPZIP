using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

public class CreateCCAE41Material
{
    [MenuItem("Tools/#CCAE41 Materyali Oluştur")]
    public static void CreateMaterial()
    {
        string targetFolder = "Assets/MeshyImports/Big-Handed Rubber Duck_20260730_212737";
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        string matPath = $"{targetFolder}/Material_CCAE41.mat";

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = "Material_CCAE41";

        Color hexColor;
        if (ColorUtility.TryParseHtmlString("#CCAE41", out hexColor))
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", hexColor);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", hexColor);
        }

        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.2f);
        if (mat.HasProperty("_SpecularHighlights")) mat.SetFloat("_SpecularHighlights", 0f);
        if (mat.HasProperty("_EnvironmentReflections")) mat.SetFloat("_EnvironmentReflections", 0f);

        AssetDatabase.CreateAsset(mat, matPath);

        // Ördek kanatlarına (Parça 2 ve Parça 3) materyali bağla
        GameObject rootContainer = GameObject.Find("Ordek_Parcalanmis");
        if (rootContainer != null)
        {
            Transform p2 = rootContainer.transform.Find("Ordek_Parca_2");
            Transform p3 = rootContainer.transform.Find("Ordek_Parca_3");

            if (p2 != null)
            {
                MeshRenderer mr2 = p2.GetComponent<MeshRenderer>();
                if (mr2 != null) mr2.sharedMaterial = mat;
            }

            if (p3 != null)
            {
                MeshRenderer mr3 = p3.GetComponent<MeshRenderer>();
                if (mr3 != null) mr3.sharedMaterial = mat;
            }

            EditorSceneManager.MarkSceneDirty(rootContainer.scene);
            EditorSceneManager.SaveOpenScenes();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[CreateCCAE41Material] #CCAE41 renginde materyal başarıyla oluşturuldu: '{matPath}'");
    }
}
