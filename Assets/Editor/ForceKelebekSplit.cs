using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

public class ForceKelebekSplit
{
    [MenuItem("Tools/Kelebeği 3 Parçaya Ayır ve Materyal At (Fix Butterfly)")]
    public static void RunSplitKelebek()
    {
        // 1. Varsa eski objeleri temizle
        GameObject oldContainer = GameObject.Find("Kelebek_Parcalanmis");
        if (oldContainer != null) Object.DestroyImmediate(oldContainer);

        GameObject oldKelebek = GameObject.Find("kelebek");
        if (oldKelebek != null) Object.DestroyImmediate(oldKelebek);

        GameObject oldKelebekUpper = GameObject.Find("Kelebek");
        if (oldKelebekUpper != null) Object.DestroyImmediate(oldKelebekUpper);

        // 2. FBX modelini ve Materyalini bul
        string[] guids = AssetDatabase.FindAssets("Meshy_AI_Chromatic_Convergence");
        if (guids.Length == 0)
        {
            Debug.LogError("[ForceKelebekSplit] Kelebek FBX dosyası bulunamadı!");
            return;
        }

        string fbxPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);

        // Materyali yükle
        Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/MeshyImports/Chromatic Convergence_20260730_204648/Material.001.mat");

        GameObject kelebekInstance = (GameObject)PrefabUtility.InstantiatePrefab(fbxPrefab);
        kelebekInstance.name = "kelebek";

        MeshFilter mf = kelebekInstance.GetComponentInChildren<MeshFilter>();
        if (mf == null)
        {
            Debug.LogError("[ForceKelebekSplit] Modelde MeshFilter bulunamadı!");
            return;
        }

        Mesh srcMesh = mf.sharedMesh;
        if (mat == null)
        {
            Renderer r = mf.GetComponent<Renderer>();
            if (r != null) mat = r.sharedMaterial;
        }

        List<Mesh> splitMeshes = SplitMeshByConnectedComponents(srcMesh);

        string saveFolder = "Assets/SplitMeshes";
        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
        }

        // Ana Parent GameObject
        GameObject rootContainer = new GameObject("Kelebek_Parcalanmis");
        Undo.RegisterCreatedObjectUndo(rootContainer, "Force Split Kelebek");
        rootContainer.transform.position = Vector3.zero;
        rootContainer.transform.rotation = Quaternion.identity;
        rootContainer.transform.localScale = Vector3.one;

        List<Transform> partTransforms = new List<Transform>();

        for (int i = 0; i < splitMeshes.Count; i++)
        {
            Mesh partMesh = splitMeshes[i];
            string assetPath = $"{saveFolder}/Kelebek_Part_{i + 1}.asset";
            AssetDatabase.CreateAsset(partMesh, assetPath);

            GameObject partObj = new GameObject($"Kelebek_Parca_{i + 1}");
            partObj.transform.SetParent(rootContainer.transform, false);

            MeshFilter partMf = partObj.AddComponent<MeshFilter>();
            partMf.sharedMesh = partMesh;

            MeshRenderer partMr = partObj.AddComponent<MeshRenderer>();
            if (mat != null) partMr.sharedMaterial = mat;

            partTransforms.Add(partObj.transform);
        }

        Object.DestroyImmediate(kelebekInstance);

        // WingFlapper ekle
        if (partTransforms.Count >= 2)
        {
            WingFlapper flapper = rootContainer.AddComponent<WingFlapper>();
            flapper.wing1 = partTransforms[0];
            flapper.wing2 = partTransforms[1];
            flapper.wing1MinAngle = -50f;
            flapper.wing1MaxAngle = 50f;
            flapper.wing2MinAngle = -50f;
            flapper.wing2MaxAngle = 50f;
            flapper.flapSpeed = 8f;
            flapper.invertWing2 = true;
            flapper.flapAxis = WingFlapper.FlapAxis.Z_Axis;
            flapper.SaveInitialRotations();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.MarkSceneDirty(rootContainer.scene);
        EditorSceneManager.SaveOpenScenes();

        Selection.activeGameObject = rootContainer;
        Debug.Log($"[ForceKelebekSplit] BAŞARILI! Sahnede 'Kelebek_Parcalanmis' altında {splitMeshes.Count} parça oluşturuldu, materyali atandı ve sahne kaydedildi.");
    }

    private static List<Mesh> SplitMeshByConnectedComponents(Mesh sourceMesh)
    {
        Vector3[] vertices = sourceMesh.vertices;
        Vector3[] normals = sourceMesh.normals;
        Vector2[] uvs = sourceMesh.uv;
        Vector4[] tangents = sourceMesh.tangents;
        Color[] colors = sourceMesh.colors;
        int[] triangles = sourceMesh.triangles;

        int numVerts = vertices.Length;
        int numTriangles = triangles.Length / 3;

        Dictionary<Vector3, List<int>> posToVerts = new Dictionary<Vector3, List<int>>();
        for (int i = 0; i < numVerts; i++)
        {
            Vector3 pos = vertices[i];
            Vector3 key = new Vector3(
                Mathf.Round(pos.x * 10000f) / 10000f,
                Mathf.Round(pos.y * 10000f) / 10000f,
                Mathf.Round(pos.z * 10000f) / 10000f
            );
            if (!posToVerts.TryGetValue(key, out var list))
            {
                list = new List<int>();
                posToVerts[key] = list;
            }
            list.Add(i);
        }

        List<int>[] vertToTriangles = new List<int>[numVerts];
        for (int i = 0; i < numVerts; i++) vertToTriangles[i] = new List<int>();

        for (int t = 0; t < numTriangles; t++)
        {
            int v0 = triangles[t * 3];
            int v1 = triangles[t * 3 + 1];
            int v2 = triangles[t * 3 + 2];

            vertToTriangles[v0].Add(t);
            vertToTriangles[v1].Add(t);
            vertToTriangles[v2].Add(t);
        }

        int[] parent = new int[numTriangles];
        for (int t = 0; t < numTriangles; t++) parent[t] = t;

        int Find(int i)
        {
            if (parent[i] == i) return i;
            return parent[i] = Find(parent[i]);
        }

        void Union(int i, int j)
        {
            int rootI = Find(i);
            int rootJ = Find(j);
            if (rootI != rootJ) parent[rootI] = rootJ;
        }

        foreach (var pair in posToVerts)
        {
            List<int> sharedVerts = pair.Value;
            int firstTri = -1;
            foreach (int vIndex in sharedVerts)
            {
                foreach (int triIndex in vertToTriangles[vIndex])
                {
                    if (firstTri == -1) firstTri = triIndex;
                    else Union(firstTri, triIndex);
                }
            }
        }

        Dictionary<int, List<int>> componentTriangles = new Dictionary<int, List<int>>();
        for (int t = 0; t < numTriangles; t++)
        {
            int root = Find(t);
            if (!componentTriangles.TryGetValue(root, out var triList))
            {
                triList = new List<int>();
                componentTriangles[root] = triList;
            }
            triList.Add(t);
        }

        List<Mesh> resultMeshes = new List<Mesh>();

        foreach (var component in componentTriangles.Values)
        {
            Dictionary<int, int> oldToNewVertIndex = new Dictionary<int, int>();
            List<Vector3> newVerts = new List<Vector3>();
            List<Vector3> newNormals = normals != null && normals.Length == numVerts ? new List<Vector3>() : null;
            List<Vector2> newUVs = uvs != null && uvs.Length == numVerts ? new List<Vector2>() : null;
            List<Vector4> newTangents = tangents != null && tangents.Length == numVerts ? new List<Vector4>() : null;
            List<Color> newColors = colors != null && colors.Length == numVerts ? new List<Color>() : null;
            List<int> newTriangles = new List<int>();

            foreach (int triIdx in component)
            {
                for (int c = 0; c < 3; c++)
                {
                    int oldV = triangles[triIdx * 3 + c];
                    if (!oldToNewVertIndex.TryGetValue(oldV, out int newV))
                    {
                        newV = newVerts.Count;
                        oldToNewVertIndex[oldV] = newV;
                        newVerts.Add(vertices[oldV]);
                        if (newNormals != null) newNormals.Add(normals[oldV]);
                        if (newUVs != null) newUVs.Add(uvs[oldV]);
                        if (newTangents != null) newTangents.Add(tangents[oldV]);
                        if (newColors != null) newColors.Add(colors[oldV]);
                    }
                    newTriangles.Add(newV);
                }
            }

            Mesh subMesh = new Mesh();
            subMesh.name = $"{sourceMesh.name}_Part_{resultMeshes.Count + 1}";
            subMesh.SetVertices(newVerts);
            if (newNormals != null) subMesh.SetNormals(newNormals);
            if (newUVs != null) subMesh.SetUVs(0, newUVs);
            if (newTangents != null) subMesh.SetTangents(newTangents);
            if (newColors != null) subMesh.SetColors(newColors);
            subMesh.SetTriangles(newTriangles, 0);

            if (newNormals == null || newNormals.Count == 0)
                subMesh.RecalculateNormals();
            subMesh.RecalculateBounds();

            resultMeshes.Add(subMesh);
        }

        return resultMeshes;
    }
}
