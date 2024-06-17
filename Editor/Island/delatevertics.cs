using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;

public class MeshDeletionUtility
{
    public static Mesh DeleteMesh(SkinnedMeshRenderer skinnedMeshRenderer, List<int> verticesIndexes, bool keepVertices)
    {
        Stopwatch stopwatch = new Stopwatch();
        Mesh originalMesh = skinnedMeshRenderer.sharedMesh;

        HashSet<int> verticesIndexesSet = new(verticesIndexes);
        int newVerticesCount = keepVertices ? verticesIndexesSet.Count : originalMesh.vertexCount - verticesIndexesSet.Count;

        stopwatch.Start();

        List<Vector3> newVerticesList = new(newVerticesCount);
        List<Vector3> newNormalsList = new(newVerticesCount);
        List<Vector4> newTangentsList = new(newVerticesCount);
        List<Vector2> newUvList = new(newVerticesCount);
        List<Vector2> newUv2List = new();
        List<Vector2> newUv3List = new();
        List<Vector2> newUv4List = new();
        List<BoneWeight> newBoneWeight = new(newVerticesCount);
        List<int> newTrianglesList = new();

        Dictionary<int, int> indexMap = new(newVerticesCount);

        stopwatch.Stop();
        //UnityEngine.Debug.Log("Initialization: " + stopwatch.ElapsedMilliseconds + " ms");

        stopwatch.Start();

        Vector3[] vertices = originalMesh.vertices;
        Vector3[] normals = originalMesh.normals;
        Vector4[] tangents = originalMesh.tangents;
        Vector2[] uv = originalMesh.uv;
        Vector2[] uv2 = originalMesh.uv2;
        Vector2[] uv3 = originalMesh.uv3;
        Vector2[] uv4 = originalMesh.uv4;
        BoneWeight[] boneWeights = originalMesh.boneWeights;

        for (int i = 0; i < originalMesh.vertexCount; i++)
        {
            if ((keepVertices && verticesIndexesSet.Contains(i)) || (!keepVertices && !verticesIndexesSet.Contains(i)))
            {
                indexMap[i] = newVerticesList.Count;
                newVerticesList.Add(vertices[i]);
                newNormalsList.Add(normals[i]);
                newTangentsList.Add(tangents[i]);
                newUvList.Add(uv[i]);
                newBoneWeight.Add(boneWeights[i]);

                if (uv2.Length > 0) newUv2List.Add(uv2[i]);
                if (uv3.Length > 0) newUv3List.Add(uv3[i]);
                if (uv4.Length > 0) newUv4List.Add(uv4[i]);
            }
        }

        stopwatch.Stop();
        //UnityEngine.Debug.Log("Vertex Processing: " + stopwatch.ElapsedMilliseconds + " ms");

        Mesh newMesh = new Mesh();
        newMesh.Clear();

        newMesh.vertices = newVerticesList.ToArray();
        newMesh.normals = newNormalsList.ToArray();
        newMesh.tangents = newTangentsList.ToArray();
        newMesh.uv = newUvList.ToArray();
        newMesh.boneWeights = newBoneWeight.ToArray();

        if (newUv2List.Count > 0) newMesh.uv2 = newUv2List.ToArray();
        if (newUv3List.Count > 0) newMesh.uv3 = newUv3List.ToArray();
        if (newUv4List.Count > 0) newMesh.uv4 = newUv4List.ToArray();

        stopwatch.Start();
        CopyColors(originalMesh, newMesh, indexMap);
        CopyTriangles(originalMesh, newMesh, indexMap, ref newTrianglesList);
        CopyBlendShapes(originalMesh, newMesh, indexMap);
        newMesh.bindposes = originalMesh.bindposes;
        stopwatch.Stop();

        //UnityEngine.Debug.Log("Copy Process: " + stopwatch.ElapsedMilliseconds + " ms");

        return newMesh;
    }

    private static void CopyColors(Mesh originalMesh, Mesh newMesh, Dictionary<int, int> indexMap)
    {
        if (originalMesh.colors != null && originalMesh.colors.Length > 0)
        {
            Color[] newColors = new Color[newMesh.vertexCount];
            foreach (var kv in indexMap)
            {
                newColors[kv.Value] = originalMesh.colors[kv.Key];
            }
            newMesh.colors = newColors;
        }
        else if (originalMesh.colors32 != null && originalMesh.colors32.Length > 0)
        {
            Color32[] newColors32 = new Color32[newMesh.vertexCount];
            foreach (var kv in indexMap)
            {
                newColors32[kv.Value] = originalMesh.colors32[kv.Key];
            }
            newMesh.colors32 = newColors32;
        }
    }

    private static void CopyTriangles(Mesh originalMesh, Mesh newMesh, Dictionary<int, int> indexMap, ref List<int> newTrianglesList)
    {
        int subMeshCount = originalMesh.subMeshCount;
        newMesh.subMeshCount = subMeshCount;

        for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
        {
            int[] originalTriangles = originalMesh.GetTriangles(subMeshIndex);

            newTrianglesList.Clear();
            for (int i = 0; i < originalTriangles.Length; i += 3)
            {
                int index0 = originalTriangles[i];
                int index1 = originalTriangles[i + 1];
                int index2 = originalTriangles[i + 2];

                if (indexMap.ContainsKey(index0) && indexMap.ContainsKey(index1) && indexMap.ContainsKey(index2))
                {
                    newTrianglesList.Add(indexMap[index0]);
                    newTrianglesList.Add(indexMap[index1]);
                    newTrianglesList.Add(indexMap[index2]);
                }
            }

            if (newTrianglesList.Count > 0)
            {
                newMesh.SetTriangles(newTrianglesList.ToArray(), subMeshIndex);
            }
        }
    }

    private static void CopyBlendShapes(Mesh originalMesh, Mesh newMesh, Dictionary<int, int> indexMap)
    {
        for (int i = 0; i < originalMesh.blendShapeCount; i++)
        {
            string blendShapeName = originalMesh.GetBlendShapeName(i);
            int frameCount = originalMesh.GetBlendShapeFrameCount(i);

            for (int j = 0; j < frameCount; j++)
            {
                float frameWeight = originalMesh.GetBlendShapeFrameWeight(i, j);
                Vector3[] frameVertices = new Vector3[originalMesh.vertexCount];
                Vector3[] frameNormals = new Vector3[originalMesh.vertexCount];
                Vector3[] frameTangents = new Vector3[originalMesh.vertexCount];

                originalMesh.GetBlendShapeFrameVertices(i, j, frameVertices, frameNormals, frameTangents);

                Vector3[] newFrameVertices = new Vector3[newMesh.vertexCount];
                Vector3[] newFrameNormals = new Vector3[newMesh.vertexCount];
                Vector3[] newFrameTangents = new Vector3[newMesh.vertexCount];

                foreach (var kv in indexMap)
                {
                    int originalIndex = kv.Key;
                    int newIndex = kv.Value;
                    newFrameVertices[newIndex] = frameVertices[originalIndex];
                    newFrameNormals[newIndex] = frameNormals[originalIndex];
                    newFrameTangents[newIndex] = frameTangents[originalIndex];
                }

                newMesh.AddBlendShapeFrame(blendShapeName, frameWeight, newFrameVertices, newFrameNormals, newFrameTangents);
            }
        }
    }

    public static Mesh KeepVerticesUsingDegenerateTriangles(Mesh originalMesh, List<int> vertexIndexes, bool infnity)
    {
        Mesh newMesh = Object.Instantiate(originalMesh);
        
        Vector3[] vertices = newMesh.vertices;
        
        HashSet<int> vertexIndexesSet = new HashSet<int>(vertexIndexes);
        Vector3 replacementValue = infnity ? Vector3.positiveInfinity : Vector3.zero;
        
        for (int i = 0; i < vertices.Length; i++)
        {
            if (!vertexIndexesSet.Contains(i))
            {
                vertices[i] = replacementValue;
            }
        }

        newMesh.vertices = vertices;
        return newMesh;
    }

    public static Mesh GenerateBacksideMesh(Mesh mesh)
    {
        Mesh newMesh = Object.Instantiate(mesh);

        int[] triangles = mesh.triangles;    
        int triCount = triangles.Length;

        int[] newTriangles = new int[triCount * 2];

        // Copy original triangles
        for (int i = 0; i < triCount; i += 3)
        {
            newTriangles[i] = triangles[i];
            newTriangles[i + 1] = triangles[i + 1];
            newTriangles[i + 2] = triangles[i + 2];

            // Generate backface triangles (reverse winding order for backface)
            newTriangles[i + triCount] = triangles[i];
            newTriangles[i + triCount + 1] = triangles[i + 2];
            newTriangles[i + triCount + 2] = triangles[i + 1];
        }

        newMesh.triangles = newTriangles;
        
        newMesh.RecalculateBounds();
        newMesh.RecalculateNormals();

        return newMesh;
    }

    public static void RemoveUnusedMaterials(SkinnedMeshRenderer skinnedMeshRenderer)
    {
        Mesh newMesh = skinnedMeshRenderer.sharedMesh;
        // 使われているマテリアルのインデックスを集める
        HashSet<int> usedMaterialIndices = new HashSet<int>();

        for (int subMeshIndex = 0; subMeshIndex < newMesh.subMeshCount; subMeshIndex++)
        {
            int[] triangles = newMesh.GetTriangles(subMeshIndex);
            if (triangles.Length > 0)
            {
                usedMaterialIndices.Add(subMeshIndex);
            }
        }

        // 未使用のマテリアルを削除する
        List<Material> usedMaterials = new List<Material>();
        for (int i = 0; i < skinnedMeshRenderer.sharedMaterials.Length; i++)
        {
            if (usedMaterialIndices.Contains(i))
            {
                usedMaterials.Add(skinnedMeshRenderer.sharedMaterials[i]);
            }
        }

        skinnedMeshRenderer.sharedMaterials = usedMaterials.ToArray();
    }
}