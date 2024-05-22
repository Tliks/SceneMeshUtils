using UnityEngine;
using System.Collections.Generic;

public class MeshDeletionUtility
{
    public static Mesh DeleteMesh(SkinnedMeshRenderer skinnedMeshRenderer, List<int> keepVerticesIndexes)
    {
        return DeleteMesh(skinnedMeshRenderer, keepVerticesIndexes, null);
    }

    public static Mesh DeleteMesh(SkinnedMeshRenderer skinnedMeshRenderer, List<int> keepVerticesIndexes, Mesh existingMesh)
    {
        Mesh originalMesh = skinnedMeshRenderer.sharedMesh;

        List<Vector3> newVerticesList = new();
        List<Vector3> newNormalsList = new();
        List<Vector4> newTangentsList = new();
        List<Vector2> newUvList = new();
        List<Vector2> newUv2List = new();
        List<Vector2> newUv3List = new();
        List<Vector2> newUv4List = new();
        // If you have UV5 and beyond, initialize more lists like newUv5List, newUv6List, etc.
        List<BoneWeight> newBoneWeight = new();
        List<int> newTrianglesList = new();

        Dictionary<int, int> indexMap = new();

        keepVerticesIndexes.Sort();

        for (int i = 0; i < originalMesh.vertexCount; i++)
        {
            if (keepVerticesIndexes.Contains(i))
            {
                indexMap[i] = newVerticesList.Count;
                newVerticesList.Add(originalMesh.vertices[i]);
                newNormalsList.Add(originalMesh.normals[i]);
                newTangentsList.Add(originalMesh.tangents[i]);
                newUvList.Add(originalMesh.uv[i]);
                newBoneWeight.Add(originalMesh.boneWeights[i]);
                
                if (originalMesh.uv2.Length > 0) newUv2List.Add(originalMesh.uv2[i]);
                if (originalMesh.uv3.Length > 0) newUv3List.Add(originalMesh.uv3[i]);
                if (originalMesh.uv4.Length > 0) newUv4List.Add(originalMesh.uv4[i]);
                // Add similar conditions for UV5 and beyond
            }
        }
        
        Mesh newMesh;
        if (existingMesh)
        {
            newMesh = existingMesh;
            newMesh.Clear();
        }
        else
        {
            newMesh = new Mesh();
        }

        newMesh.vertices = newVerticesList.ToArray();
        newMesh.normals = newNormalsList.ToArray();
        newMesh.tangents = newTangentsList.ToArray();
        newMesh.uv = newUvList.ToArray();
        newMesh.boneWeights = newBoneWeight.ToArray();

        if (newUv2List.Count > 0) newMesh.uv2 = newUv2List.ToArray();
        if (newUv3List.Count > 0) newMesh.uv3 = newUv3List.ToArray();
        if (newUv4List.Count > 0) newMesh.uv4 = newUv4List.ToArray();
        // Set similar conditions for UV5 and beyond

        CopyColors(originalMesh, newMesh, indexMap);
        CopyTriangles(originalMesh, newMesh, indexMap, ref newTrianglesList);
        CopyBlendShapes(originalMesh, newMesh, indexMap);
        newMesh.bindposes = originalMesh.bindposes;

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
}