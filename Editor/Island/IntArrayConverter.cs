using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.aoyon.modulecreator
{
    public static class IntArrayConverter
    {
        public static Vector3[] Decode(Mesh mesh, int[] triangleIndices)
        {
            Vector3[] vertices = mesh.vertices;
            Vector3[] positions = new Vector3[triangleIndices.Length * 3]; // triangleIndicesは三角形のインデックスなので、頂点数は3倍

            for (int i = 0; i < triangleIndices.Length; i++)
            {
                int triangleIndex = triangleIndices[i];
                positions[i * 3] = vertices[mesh.triangles[triangleIndex * 3]];
                positions[i * 3 + 1] = vertices[mesh.triangles[triangleIndex * 3 + 1]];
                positions[i * 3 + 2] = vertices[mesh.triangles[triangleIndex * 3 + 2]];
            }

            return positions;
        }

        public static int[] Encode(Mesh mesh, Vector3[] positions)
        {
            int[] triangles = mesh.triangles;
            List<int> triangleIndices = new List<int>();

            for (int i = 0; i < positions.Length; i += 3) // positionsは3つずつが1つの三角形を表す
            {
                Vector3 p0 = positions[i];
                Vector3 p1 = positions[i + 1];
                Vector3 p2 = positions[i + 2];

                for (int j = 0; j < triangles.Length; j += 3)
                {
                    if (mesh.vertices[triangles[j]] == p0 &&
                        mesh.vertices[triangles[j + 1]] == p1 &&
                        mesh.vertices[triangles[j + 2]] == p2)
                    {
                        triangleIndices.Add(j / 3); // jは頂点インデックスなので、3で割って三角形インデックスに変換
                        break;
                    }
                }
            }

            return triangleIndices.ToArray();
        }
        }
}