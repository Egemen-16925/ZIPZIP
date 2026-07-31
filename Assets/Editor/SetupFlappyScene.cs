using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class SetupFlappyScene
{
    [MenuItem("Tools/Setup Flappy Scene")]
    public static void SetupScene()
    {
        // 1. Find or Create GameManager
        GameObject gmObj = GameObject.Find("GameManager");
        if (gmObj == null)
            gmObj = new GameObject("GameManager");
            
        GameManager gm = gmObj.GetComponent<GameManager>();
        if (gm == null)
            gm = gmObj.AddComponent<GameManager>();

        // 2. Set up Main Camera
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.position = new Vector3(0, 0, -10);
            mainCam.orthographic = true;
            mainCam.orthographicSize = 5;
        }

        // 3. Set up Kedi
        GameObject kedi = GameObject.Find("Kedi");
        if (kedi != null)
        {
            SetupPlayer(kedi);
            kedi.transform.position = new Vector3(-2, 0, 0);
        }

        // 4. Set up Arı (Sproutwing)
        GameObject ari = GameObject.Find("Sproutwing_20260730_212822");
        if (ari != null)
        {
            SetupPlayer(ari);
            ari.transform.position = new Vector3(-2, 0, 0);
            ari.SetActive(false); // Initially only Kedi is active
        }
        
        // 5. Create a basic Floor
        GameObject floor = GameObject.Find("Floor");
        if (floor == null)
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
        }
        floor.transform.position = new Vector3(0, -5, 0);
        floor.transform.localScale = new Vector3(20, 1, 5);
        floor.tag = "Bottom";
        if (floor.GetComponent<Collider>() == null)
            floor.AddComponent<BoxCollider>();

        // 6. Connect Player to GameManager
        if (kedi != null)
            gm.GetType().GetField("player", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(gm, kedi.GetComponent<Player>());

        Debug.Log("Scene Setup Complete!");
    }

    private static void SetupPlayer(GameObject obj)
    {
        Player p = obj.GetComponent<Player>();
        if (p == null)
            p = obj.AddComponent<Player>();
            
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
            rb = obj.AddComponent<Rigidbody>();
            
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY;
        
        CapsuleCollider col = obj.GetComponent<CapsuleCollider>();
        if (col == null)
            col = obj.AddComponent<CapsuleCollider>();
            
        col.isTrigger = true;
    }
}
