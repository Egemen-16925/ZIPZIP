using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class AnkaKusuSplitter
{
    [MenuItem("Tools/Anka Kusu Parcala (Split Mesh)")]
    public static void SplitSelectedOrAnkaKusu()
    {
        // 1. Hedef Obje Arama
        GameObject targetObj = Selection.activeGameObject;
        if (targetObj == null)
        {
            targetObj = GameObject.Find("anka kusu");
        }
        if (targetObj == null)
        {
            targetObj = GameObject.Find("Blazing Phoenix");
        }
        if (targetObj == null)
        {
            MeshFilter[] mfs = Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            if (mfs.Length > 0) targetObj = mfs[0].gameObject;
        }

        if (targetObj == null)
        {
            Debug.Log("[AnkaKusuSplitter] Sahnede parçalanacak bir obje/MeshFilter bulunamadı.");
            return;
        }

        MeshFilter mf = targetObj.GetComponentInChildren<MeshFilter>();
        if (mf == null)
        {
            Debug.Log($"[AnkaKusuSplitter] '{targetObj.name}' objesinde MeshFilter bulunamadı.");
            return;
        }

        Mesh srcMesh = mf.sharedMesh;
        if (srcMesh == null)
        {
            Debug.Log($"[AnkaKusuSplitter] '{mf.gameObject.name}' üzerinde sharedMesh yok.");
            return;
        }

        Renderer r = mf.GetComponent<Renderer>();
        Material mat = r != null ? r.sharedMaterial : null;

        Debug.Log($"[AnkaKusuSplitter] '{mf.gameObject.name}' meşi inceleniyor... Vertex: {srcMesh.vertexCount}, Triangles: {srcMesh.triangles.Length / 3}");

        List<Mesh> splitMeshes = SplitMeshByConnectedComponents(srcMesh);

        if (splitMeshes.Count <= 1)
        {
            Debug.LogWarning($"[AnkaKusuSplitter] Mesh tek parçadan oluşuyor veya ayrık geometri bulunamadı. (Tespit edilen parça sayısı: {splitMeshes.Count})");
            return;
        }

        Debug.Log($"[AnkaKusuSplitter] BAŞARILI! Toplam {splitMeshes.Count} ayrık parça tespit edildi. Yeni objeler oluşturuluyor...");

        string saveFolder = "Assets/SplitMeshes";
        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
        }

        GameObject rootContainer = new GameObject($"{targetObj.name}_Parcalanmis");
        Undo.RegisterCreatedObjectUndo(rootContainer, "Anka Kusu Parcala");
        rootContainer.transform.position = mf.transform.position;
        rootContainer.transform.rotation = mf.transform.rotation;
        rootContainer.transform.localScale = mf.transform.localScale;

        for (int i = 0; i < splitMeshes.Count; i++)
        {
            Mesh partMesh = splitMeshes[i];
            string assetPath = $"{saveFolder}/{targetObj.name}_Part_{i + 1}.asset";
            AssetDatabase.CreateAsset(partMesh, assetPath);

            GameObject partObj = new GameObject($"{targetObj.name}_Parca_{i + 1}");
            partObj.transform.SetParent(rootContainer.transform, false);

            MeshFilter partMf = partObj.AddComponent<MeshFilter>();
            partMf.sharedMesh = partMesh;

            MeshRenderer partMr = partObj.AddComponent<MeshRenderer>();
            if (mat != null) partMr.sharedMaterial = mat;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = rootContainer;
        Debug.Log($"[AnkaKusuSplitter] İŞLEM TAMAMLANDI! '{rootContainer.name}' altında {splitMeshes.Count} ayrı parça GameObject oluşturuldu.");
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
