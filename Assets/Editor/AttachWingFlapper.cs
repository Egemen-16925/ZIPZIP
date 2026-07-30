using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class AttachWingFlapper
{
    [MenuItem("Tools/Kanat Çırpma Komponenti Ekle (Attach WingFlapper)")]
    public static void AttachToKanatPivot()
    {
        GameObject pivotObj = GameObject.Find("kanat pivot");
        if (pivotObj == null)
        {
            pivotObj = GameObject.Find("Kanat Pivot");
        }
        if (pivotObj == null)
        {
            WingFlapper existing = Object.FindAnyObjectByType<WingFlapper>();
            if (existing != null) pivotObj = existing.gameObject;
        }

        if (pivotObj != null)
        {
            WingFlapper flapper = pivotObj.GetComponent<WingFlapper>();
            if (flapper == null)
            {
                flapper = pivotObj.AddComponent<WingFlapper>();
                Undo.RegisterCreatedObjectUndo(flapper, "Add WingFlapper");
            }

            if (pivotObj.transform.childCount > 0 && flapper.wing1 == null)
            {
                flapper.wing1 = pivotObj.transform.GetChild(0);
            }
            if (pivotObj.transform.childCount > 1 && flapper.wing2 == null)
            {
                flapper.wing2 = pivotObj.transform.GetChild(1);
            }

            flapper.wing1MinAngle = -60f;
            flapper.wing1MaxAngle = 60f;
            flapper.wing2MinAngle = -60f;
            flapper.wing2MaxAngle = 60f;
            flapper.flapSpeed = 5f;
            flapper.invertWing2 = true;

            EditorUtility.SetDirty(flapper);
            EditorSceneManager.MarkSceneDirty(pivotObj.scene);

            Debug.Log($"[WingFlapper] '{pivotObj.name}' objesine WingFlapper eklendi ve kanat transformları atandı!");
        }
    }
}
