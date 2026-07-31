using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Legacy2D;

public class AutoApplyVisuals
{
    [MenuItem("Tools/FORCE Apply AI Visuals NOW")]
    public static void Apply()
    {

        string bgPath = "Assets/AI_Visuals/magical_forest_bg.png";
        string obstaclePath = "Assets/AI_Visuals/magical_vine_obstacle.png";

        Sprite bgSprite = GetSpriteFromPath(bgPath);
        Sprite obsSprite = GetSpriteFromPath(obstaclePath);

        if (bgSprite == null || obsSprite == null)
        {
            Debug.LogError("Sprite'lar yüklenemedi. Lütfen resimlerin Assets/AI_Visuals klasöründe olduğundan emin olun.");
            return;
        }

        // Açık olan sahneyi kaydedip Flappy Bird sahnesine zorla geçiş yapıyoruz
        string scenePath = "Assets/Scenes/Flappy Bird.unity";
        UnityEngine.SceneManagement.Scene currentScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (currentScene.path != scenePath)
        {
            UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
        }

        bool changed = false;

        // 1. Backgrounds
        BackgroundSystem bgSystem = Object.FindObjectOfType<BackgroundSystem>(true);
        if (bgSystem != null)
        {
            SerializedObject so = new SerializedObject(bgSystem);
            SerializedProperty spritesProp = so.FindProperty("_sprites");
            for (int i = 0; i < spritesProp.arraySize; i++)
            {
                spritesProp.GetArrayElementAtIndex(i).objectReferenceValue = bgSprite;
            }
            
            UpdateSpriteProp(so.FindProperty("_firstSprite"), bgSprite);
            UpdateSpriteProp(so.FindProperty("_secondSprite"), bgSprite);
            UpdateSpriteProp(so.FindProperty("_thirdSprite"), bgSprite);
            
            so.ApplyModifiedProperties();
            changed = true;

            // Arkaplan ölçeklerini (Scale) kameraya tam oturacak şekilde otomatik düzelt!
            Camera cam = Camera.main;
            if (cam != null && bgSprite != null)
            {
                float targetHeight = 2f * cam.orthographicSize; // Kameranın tam yüksekliği
                float spriteHeight = bgSprite.bounds.size.y;
                float scale = targetHeight / spriteHeight;
                
                // Arkaplanın boyunu tam kameraya göre oranlıyoruz
                Vector3 newScale = new Vector3(scale, scale, 1f);
                
                SpriteRenderer s1 = bgSystem.GetType().GetField("_firstSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(bgSystem) as SpriteRenderer;
                SpriteRenderer s2 = bgSystem.GetType().GetField("_secondSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(bgSystem) as SpriteRenderer;
                SpriteRenderer s3 = bgSystem.GetType().GetField("_thirdSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(bgSystem) as SpriteRenderer;
                
                if (s1 != null) { s1.transform.localScale = newScale; s1.transform.localPosition = new Vector3(0, 0, 10f); }
                if (s2 != null) { s2.transform.localScale = newScale; s2.transform.localPosition = new Vector3(s1.bounds.size.x, 0, 10f); }
                if (s3 != null) { s3.transform.localScale = newScale; s3.transform.localPosition = new Vector3(s1.bounds.size.x * 2f, 0, 10f); }
            }
        }

        // 2. Obstacles (Pipes)
        Spawner spawner = Object.FindObjectOfType<Spawner>(true);
        if (spawner != null)
        {
            UpdatePipes(spawner.level1Obstacles, obsSprite);
            UpdatePipes(spawner.level2Obstacles, obsSprite);
            UpdatePipes(spawner.level3Obstacles, obsSprite);
            UpdatePipes(spawner.level4Obstacles, obsSprite);
            UpdatePipes(spawner.level5Obstacles, obsSprite);
            changed = true;
        }

        if (changed)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
            Debug.Log(">>> MAGIC: Background and Obstacles automatically applied to Flappy Bird! <<<");
        }
    }

    private static Sprite GetSpriteFromPath(string path)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (Object o in assets)
        {
            if (o is Sprite s) return s;
        }
        return null;
    }

    private static void UpdateSpriteProp(SerializedProperty prop, Sprite sprite)
    {
        if (prop != null && prop.objectReferenceValue != null)
        {
            SpriteRenderer sr = prop.objectReferenceValue as SpriteRenderer;
            if (sr != null)
            {
                sr.sprite = sprite;
            }
        }
    }

    private static void UpdatePipes(System.Collections.Generic.List<Pipes> pipesList, Sprite newSprite)
    {
        if (pipesList == null) return;
        foreach (Pipes pipe in pipesList)
        {
            if (pipe != null)
            {
                string prefabPath = AssetDatabase.GetAssetPath(pipe);
                if (!string.IsNullOrEmpty(prefabPath))
                {
                    GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
                    SpriteRenderer[] srs = contents.GetComponentsInChildren<SpriteRenderer>(true);
                    foreach (SpriteRenderer sr in srs)
                    {
                        sr.sprite = newSprite;
                    }
                    PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                    PrefabUtility.UnloadPrefabContents(contents);
                }
            }
        }
    }
}
