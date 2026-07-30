using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class ArrangeSceneObjects
{
    [MenuItem("Tools/Sahne Objelerini ve Kamerayı Düzenle")]
    public static void ArrangeScene()
    {
        // 1. Kedi Kutusunun Aşırı Ölçeğini (1000x) Standart 1x'e Çek
        GameObject boxObj = GameObject.Find("KediKutusu");
        if (boxObj == null) boxObj = GameObject.Find("KediKutusu_Parcalanmis");

        if (boxObj != null)
        {
            boxObj.transform.localScale = Vector3.one;
            boxObj.transform.position = new Vector3(3f, 0f, 0f); // Kedinin yanına koy
            EditorUtility.SetDirty(boxObj);
        }

        // 2. Kediyi Kameranın Tam Karşısına Koy ve Görünür Yap
        GameObject catObj = GameObject.Find("Kedi");
        if (catObj == null) catObj = GameObject.Find("kedi");

        if (catObj != null)
        {
            catObj.transform.position = Vector3.zero;
            catObj.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // Kameraya baksın
            catObj.transform.localScale = Vector3.one;
            EditorUtility.SetDirty(catObj);
        }

        // 3. Ana Kamerayı Objeleri Tam Görecek Şekilde Konumlandır
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0f, 1.5f, -5f);
            mainCam.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
            EditorUtility.SetDirty(mainCam);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[ArrangeSceneObjects] SAHNE DÜZENLENDİ! Kedi (0,0,0) konumuna getirildi, kamera hizalandı.");
    }
}
