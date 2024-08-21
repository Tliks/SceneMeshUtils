using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace com.aoyon.modulecreator
{
    public class ClampBlendShapeUtility
    {
        private static SkinnedMeshRenderer _originskinnedMeshRenderer;
        private static string _rootname;
        private static TriangleSelectionManager _triangleSelectionManager;

        public static void Initialize(SkinnedMeshRenderer origSkinnedMeshRenderer, TriangleSelectionManager triangleSelectionManager)
        {
            _originskinnedMeshRenderer = origSkinnedMeshRenderer;
            _triangleSelectionManager = triangleSelectionManager;
            _rootname = CheckUtility.CheckRoot(origSkinnedMeshRenderer.gameObject).name;
        }

        public static Mesh GenerateClampBlendShape(Mesh originalMesh, HashSet<int> triangleIndices)
        {
            Mesh newMesh = Object.Instantiate(originalMesh);
            Vector3[] vertices = newMesh.vertices;
            int[] triangles = newMesh.triangles;

            HashSet<int> nonSelectedVertices = new HashSet<int>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                if (!triangleIndices.Contains(i / 3))
                {
                    nonSelectedVertices.Add(triangles[i]);
                    nonSelectedVertices.Add(triangles[i + 1]);
                    nonSelectedVertices.Add(triangles[i + 2]);
                }
            }

            HashSet<int> selectedVertices = new HashSet<int>();
            foreach (int triIndex in triangleIndices)
            {
                selectedVertices.Add(triangles[triIndex * 3]);
                selectedVertices.Add(triangles[triIndex * 3 + 1]);
                selectedVertices.Add(triangles[triIndex * 3 + 2]);
            }

            // Union-Find の初期化
            UnionFind unionFind = new UnionFind(vertices.Length);

            // 選択されたトライアングルの頂点を結合
            foreach (int triIndex in triangleIndices)
            {
                int v1 = triangles[triIndex * 3];
                int v2 = triangles[triIndex * 3 + 1];
                int v3 = triangles[triIndex * 3 + 2];

                // selectedVerticesに含まれている頂点同士、または含まれていない頂点同士のみを結合
                if (nonSelectedVertices.Contains(v1) == nonSelectedVertices.Contains(v2))
                    unionFind.Unite(v1, v2);
                if (nonSelectedVertices.Contains(v2) == nonSelectedVertices.Contains(v3))
                    unionFind.Unite(v2, v3);
                if (nonSelectedVertices.Contains(v3) == nonSelectedVertices.Contains(v1))
                    unionFind.Unite(v3, v1);
            }

            // アイランドごとの頂点リストとその重心を計算
            Dictionary<int, List<int>> islands = new Dictionary<int, List<int>>();
            Dictionary<int, Vector3> islandCentroids = new Dictionary<int, Vector3>();

            foreach (int vertexIndex in selectedVertices)
            {
                int root = unionFind.Find(vertexIndex);
                if (!islands.ContainsKey(root))
                {
                    islands[root] = new List<int>();
                    islandCentroids[root] = Vector3.zero;
                }
                islands[root].Add(vertexIndex);
                islandCentroids[root] += vertices[vertexIndex];
            }

            // 各アイランドの重心を計算
            foreach (var key in islandCentroids.Keys.ToList())
            {
                if (islands[key].Count > 0)
                {
                    islandCentroids[key] /= islands[key].Count;
                }
            }

            // BlendShape 頂点の計算
            Vector3[] blendShapeVertices = new Vector3[vertices.Length];
            foreach (var kvp in islands)
            {
                int root = kvp.Key;
                Vector3 centroid = islandCentroids[root];
                foreach (int vertexIndex in kvp.Value)
                {
                    blendShapeVertices[vertexIndex] = centroid - vertices[vertexIndex];
                }
            }

            // 選択されていない頂点の移動量は0になるため、初期化時のVector3.zeroのままでOK

            string blendShapeName = "ClampBlendShape";
            List<string> blendShapeNames = GetBlendShapeNames(newMesh);
            string uniqueBlendShapeName = GetUniqueBlendShapeName(blendShapeNames, blendShapeName);

            newMesh.AddBlendShapeFrame(uniqueBlendShapeName, 100.0f, blendShapeVertices, null, null);
            //Debug.Log($"Blend shape '{uniqueBlendShapeName}' has been added.");
            return newMesh;
        }

        private static List<string> GetBlendShapeNames(Mesh mesh)
        {
            List<string> blendShapeNames = new List<string>();
            int blendShapeCount = mesh.blendShapeCount;
            for (int i = 0; i < blendShapeCount; i++)
            {
                blendShapeNames.Add(mesh.GetBlendShapeName(i));
            }
            return blendShapeNames;
        }

        private static string GetUniqueBlendShapeName(List<string> blendShapeNames, string baseName)
        {
            string uniqueName = baseName;
            int counter = 1;
            while (blendShapeNames.Contains(uniqueName))
            {
                uniqueName = baseName + "_" + counter++;
            }

            return uniqueName;
        }

        private static void ReplaceMesh()
        {
            Mesh newMesh = GenerateClampBlendShape(_originskinnedMeshRenderer.sharedMesh, _triangleSelectionManager.GetSelectedTriangles());

            string path = AssetPathUtility.GenerateMeshPath(_rootname, "ClampMesh");
            AssetDatabase.CreateAsset(newMesh, path);
            AssetDatabase.SaveAssets();

            _originskinnedMeshRenderer.sharedMesh = newMesh;
        }

        public static void RendergenerateClamp()
        {
            EditorGUILayout.Space();
            GUI.enabled = _triangleSelectionManager.GetSelectedTriangles().Count > 0;
            if (GUILayout.Button(LocalizationEditor.GetLocalizedText("Utility.BlendShape")))
            {
                CustomAnimationMode.StopAnimationMode();
                ReplaceMesh();
                CustomAnimationMode.StartAnimationMode(_originskinnedMeshRenderer);
            }
            GUI.enabled = true;
        }
    }
}