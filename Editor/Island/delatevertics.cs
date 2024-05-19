using UnityEngine;
using System.Collections.Generic;

public class MeshDeletionUtility
{
    public static Mesh DeleteMesh(SkinnedMeshRenderer skinnedMeshRenderer, List<int> keepVerticesIndexes)
    {
        Mesh originalMesh = skinnedMeshRenderer.sharedMesh;

        List<Vector3> newVerticesList = new();
        List<Vector3> newNormalsList = new();
        List<Vector4> newTangentsList = new();
        List<Vector2> newUvList = new();
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
            }
        }

        Mesh newMesh = new()
        {
            vertices = newVerticesList.ToArray(),
            normals = newNormalsList.ToArray(),
            tangents = newTangentsList.ToArray(),
            uv = newUvList.ToArray(),
            boneWeights = newBoneWeight.ToArray(),
            uv2 = originalMesh.uv2,
            uv3 = originalMesh.uv3,
            uv4 = originalMesh.uv4
        };

        CopyColors(originalMesh, newMesh);
        CopyTriangles(originalMesh, newMesh, indexMap, ref newTrianglesList);
        CopyBlendShapes(originalMesh, newMesh, indexMap);
        newMesh.bindposes = originalMesh.bindposes;

        return newMesh;
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