using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;

public class MeshDeletionUtility
{
    public static Mesh DeleteMeshByKeepVertices(SkinnedMeshRenderer skinnedMeshRenderer, List<int> keepVerticesIndexes)
    {
        return DeleteMesh(skinnedMeshRenderer, keepVerticesIndexes, null, true);
    }

    public static Mesh DeleteMeshByKeepVertices(SkinnedMeshRenderer skinnedMeshRenderer, List<int> keepVerticesIndexes, Mesh existingMesh)
    {
        return DeleteMesh(skinnedMeshRenderer, keepVerticesIndexes, existingMesh, true);
    }

    public static Mesh DeleteMeshByRemoveVertices(SkinnedMeshRenderer skinnedMeshRenderer, List<int> removeVerticesIndexes)
    {
        return DeleteMesh(skinnedMeshRenderer, removeVerticesIndexes, null, false);
    }

    public static Mesh DeleteMeshByRemoveVertices(SkinnedMeshRenderer skinnedMeshRenderer, List<int> removeVerticesIndexes, Mesh existingMesh)
    {
        return DeleteMesh(skinnedMeshRenderer, removeVerticesIndexes, existingMesh, false);
    }

    public static Mesh RemoveVerticesUsingDegenerateTriangles(SkinnedMeshRenderer skinnedMeshRenderer, List<int> removeVerticesIndexes)
    {
        return RemoveVerticesUsingDegenerateTriangles(skinnedMeshRenderer, removeVerticesIndexes, null);
    }

    public static Mesh RemoveVerticesUsingDegenerateTriangles(SkinnedMeshRenderer skinnedMeshRenderer, List<int> removeVerticesIndexes, Mesh existingMesh)
    {
        return MarkVerticesForDegeneration(skinnedMeshRenderer, removeVerticesIndexes, existingMesh, false);
    }

    public static Mesh KeepVerticesUsingDegenerateTriangles(SkinnedMeshRenderer skinnedMeshRenderer, List<int> keepVerticesIndexes)
    {
        return KeepVerticesUsingDegenerateTriangles(skinnedMeshRenderer, keepVerticesIndexes, null);
    }

    public static Mesh KeepVerticesUsingDegenerateTriangles(SkinnedMeshRenderer skinnedMeshRenderer, List<int> keepVerticesIndexes, Mesh existingMesh)
    {
        return MarkVerticesForDegenerationbeta(skinnedMeshRenderer, keepVerticesIndexes, existingMesh, true);
    }

    // DeleteMesh関連のコード
    private static Mesh DeleteMesh(SkinnedMeshRenderer skinnedMeshRenderer, List<int> verticesIndexes, Mesh existingMesh, bool keepVertices)
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

        Mesh newMesh = existingMesh ? existingMesh : new Mesh();
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

    // RemoveVerticesUsingDegenerateTriangles専用の関数
    private static Mesh MarkVerticesForDegeneration(SkinnedMeshRenderer skinnedMeshRenderer, List<int> vertexIndexes, Mesh existingMesh, bool keepVertices)
    {
        Stopwatch stopwatch = new Stopwatch();
        Mesh originalMesh = skinnedMeshRenderer.sharedMesh;

        stopwatch.Start();
        Vector3[] vertices = originalMesh.vertices;
        stopwatch.Stop();
        UnityEngine.Debug.Log("Extracting vertices: " + stopwatch.ElapsedMilliseconds + " ms");

        stopwatch.Restart();
        if (keepVertices)
        {
            HashSet<int> vertexIndexesSet = new HashSet<int>(vertexIndexes);
            for (int i = 0; i < vertices.Length; i++)
            {
                if (!vertexIndexesSet.Contains(i))
                {
                    vertices[i] = Vector3.zero; // Collapse triangles for vertices not in the keep list
                }
            }
        }
        else
        {
            foreach (int index in vertexIndexes)
            {
                vertices[index] = Vector3.zero; // Collapse triangles for vertices in the remove list
            }
        }
        stopwatch.Stop();
        UnityEngine.Debug.Log("Processing vertices: " + stopwatch.ElapsedMilliseconds + " ms");

        stopwatch.Restart();
        Mesh newMesh = existingMesh ? existingMesh : new Mesh();
        newMesh.Clear();
        stopwatch.Stop();
        UnityEngine.Debug.Log("Initializing new mesh: " + stopwatch.ElapsedMilliseconds + " ms");

        stopwatch.Restart();
        newMesh.vertices = vertices;

        CopyNormals(originalMesh, newMesh);
        CopyTangents(originalMesh, newMesh);
        CopyUV(originalMesh, newMesh);
        CopyBoneWeights(originalMesh, newMesh);
        CopyBindposes(originalMesh, newMesh);
        CopyColors(originalMesh, newMesh);
        newMesh.triangles = originalMesh.triangles;
        stopwatch.Stop();
        UnityEngine.Debug.Log("Copying mesh data: " + stopwatch.ElapsedMilliseconds + " ms");

        stopwatch.Restart();
        if (keepVertices)
        {
            // DegenerateTrianglesKeeping(originalMesh, newMesh, vertexIndexes);
        }
        else
        {
            // DegenerateTriangles(originalMesh, newMesh, vertexIndexes);
        }
        //CopyBlendShapesForDegeneration(originalMesh, newMesh, vertexIndexes);
        stopwatch.Stop();
        UnityEngine.Debug.Log("Handling degenerations: " + stopwatch.ElapsedMilliseconds + " ms");

        return newMesh;
    }

    private static Mesh MarkVerticesForDegenerationbeta(SkinnedMeshRenderer skinnedMeshRenderer, List<int> vertexIndexes, Mesh existingMesh, bool keepVertices)
    {
        Stopwatch stopwatch = new Stopwatch();
        Mesh originalMesh = skinnedMeshRenderer.sharedMesh;
        Mesh newMesh = Object.Instantiate(originalMesh);
        
        stopwatch.Start();
        Vector3[] vertices = newMesh.vertices;
        
        if (keepVertices)
        {
            HashSet<int> vertexIndexesSet = new HashSet<int>(vertexIndexes);
            for (int i = 0; i < vertices.Length; i++)
            {
                if (!vertexIndexesSet.Contains(i))
                {
                    vertices[i] = Vector3.zero;
                }
            }
        }
        else
        {
            foreach (int index in vertexIndexes)
            {
                vertices[index] = Vector3.zero;
            }
        }
        newMesh.vertices = vertices;
        stopwatch.Stop();
        UnityEngine.Debug.Log("Processing vertices: " + stopwatch.ElapsedMilliseconds + " ms");

        return newMesh;
    }
    
    private static void DegenerateTrianglesKeeping(Mesh originalMesh, Mesh newMesh, List<int> keepVerticesIndexes)
    {
        HashSet<int> keepVerticesSet = new HashSet<int>(keepVerticesIndexes);
        int subMeshCount = originalMesh.subMeshCount;
        newMesh.subMeshCount = subMeshCount;

        for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
        {
            int[] originalTriangles = originalMesh.GetTriangles(subMeshIndex);
            for (int i = 0; i < originalTriangles.Length; i += 3)
            {
                if (!keepVerticesSet.Contains(originalTriangles[i]) || 
                    !keepVerticesSet.Contains(originalTriangles[i + 1]) || 
                    !keepVerticesSet.Contains(originalTriangles[i + 2]))
                {
                    // Make this a degenerate triangle
                    originalTriangles[i] = originalTriangles[i + 1] = originalTriangles[i + 2] = originalTriangles[i];
                }
            }
            newMesh.SetTriangles(originalTriangles, subMeshIndex);
        }
    }

    private static void CopyNormals(Mesh originalMesh, Mesh newMesh)
    {
        Vector3[] normals = originalMesh.normals;
        if (normals.Length > 0)
        {
            newMesh.normals = normals;
        }
    }

    private static void CopyTangents(Mesh originalMesh, Mesh newMesh)
    {
        Vector4[] tangents = originalMesh.tangents;
        if (tangents.Length > 0)
        {
            newMesh.tangents = tangents;
        }
    }

    private static void CopyUV(Mesh originalMesh, Mesh newMesh)
    {
        Vector2[] uv = originalMesh.uv;
        if (uv.Length > 0)
        {
            newMesh.uv = uv;
        }

        Vector2[] uv2 = originalMesh.uv2;
        if (uv2.Length > 0)
        {
            newMesh.uv2 = uv2;
        }

        Vector2[] uv3 = originalMesh.uv3;
        if (uv3.Length > 0)
        {
            newMesh.uv3 = uv3;
        }

        Vector2[] uv4 = originalMesh.uv4;
        if (uv4.Length > 0)
        {
            newMesh.uv4 = uv4;
        }
    }

    private static void CopyBoneWeights(Mesh originalMesh, Mesh newMesh)
    {
        BoneWeight[] boneWeights = originalMesh.boneWeights;
        if (boneWeights.Length > 0)
        {
            newMesh.boneWeights = boneWeights;
        }
    }

    private static void CopyBindposes(Mesh originalMesh, Mesh newMesh)
    {
        newMesh.bindposes = originalMesh.bindposes;
    }

    private static void CopyColors(Mesh originalMesh, Mesh newMesh)
    {
        if (originalMesh.colors != null && originalMesh.colors.Length > 0)
        {
            newMesh.colors = originalMesh.colors;
        }
        else if (originalMesh.colors32 != null && originalMesh.colors32.Length > 0)
        {
            newMesh.colors32 = originalMesh.colors32;
        }
    }

    private static void DegenerateTriangles(Mesh originalMesh, Mesh newMesh, List<int> removeVerticesIndexes)
    {
        int subMeshCount = originalMesh.subMeshCount;
        newMesh.subMeshCount = subMeshCount;

        for (int subMeshIndex = 0; subMeshIndex < subMeshCount; subMeshIndex++)
        {
            int[] originalTriangles = originalMesh.GetTriangles(subMeshIndex);
            for (int i = 0; i < originalTriangles.Length; i += 3)
            {
                if (removeVerticesIndexes.Contains(originalTriangles[i]) || 
                    removeVerticesIndexes.Contains(originalTriangles[i + 1]) || 
                    removeVerticesIndexes.Contains(originalTriangles[i + 2]))
                {
                    // Make this a degenerate triangle
                    originalTriangles[i] = originalTriangles[i + 1] = originalTriangles[i + 2] = originalTriangles[i];
                }
            }
            newMesh.SetTriangles(originalTriangles, subMeshIndex);
        }
    }

    private static void CopyBlendShapesForDegeneration(Mesh originalMesh, Mesh newMesh, List<int> removeVerticesIndexes)
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

                // Set vertices for degenerate triangles to zero
                foreach (int index in removeVerticesIndexes)
                {
                    frameVertices[index] = Vector3.zero;
                    frameNormals[index] = Vector3.zero;
                    frameTangents[index] = Vector3.zero;
                }

                newMesh.AddBlendShapeFrame(blendShapeName, frameWeight, frameVertices, frameNormals, frameTangents);
            }
        }
    }
}