using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;

public class UnionFind
{
    private int[] parent;
    private int[] rank;

    public UnionFind(int size)
    {
        parent = new int[size];
        rank = new int[size];

        for (int i = 0; i < size; i++)
        {
            parent[i] = i;
            rank[i] = 0;
        }
    }

    public int Find(int x)
    {
        if (parent[x] == x)
            return x;
        else
            return parent[x] = Find(parent[x]);
    }

    public void Unite(int x, int y)
    {
        int rootX = Find(x);
        int rootY = Find(y);
        if (rootX == rootY) return;

        if (rank[rootX] < rank[rootY])
        {
            parent[rootX] = rootY;
        }
        else if (rank[rootX] > rank[rootY])
        {
            parent[rootY] = rootX;
        }
        else
        {
            parent[rootX] = rootY;
            rank[rootY]++;
        }
    }
}

public class MergedIsland
{
    public List<Island> ChildIslands { get; }
    public int MergedIndex { get; }

    public MergedIsland()
    {
        ChildIslands = new List<Island>();
    }
}

public class Island
{
    public List<int> Vertices { get; }
    public int UnmergedStartIndex { get; }
    public HashSet<(int, int)> AllEdges { get; }

    public Island(List<int> vertices, int unmergedStartIndex, HashSet<(int, int)> allEdges)
    {
        Vertices = vertices;
        UnmergedStartIndex = unmergedStartIndex;
        AllEdges = allEdges;
    }
}

public static class MeshIslandUtility
{
public static List<MergedIsland> GetIslands(SkinnedMeshRenderer skinnedMeshRenderer)
{
    Mesh mesh = skinnedMeshRenderer.sharedMesh;
    int[] triangles = mesh.triangles;
    Vector3[] vertices = mesh.vertices;

    UnionFind unionFind = new UnionFind(vertices.Length);
    Dictionary<int, List<int>> vertexEdges = new Dictionary<int, List<int>>();

    for (int i = 0; i < triangles.Length; i += 3)
    {
        int v1 = triangles[i];
        int v2 = triangles[i + 1];
        int v3 = triangles[i + 2];

        unionFind.Unite(v1, v2);
        unionFind.Unite(v2, v3);
        unionFind.Unite(v3, v1);

        AddEdge(vertexEdges, v1, v2);
        AddEdge(vertexEdges, v2, v3);
        AddEdge(vertexEdges, v3, v1);
    }

    Dictionary<int, List<int>> islandDict = new Dictionary<int, List<int>>();
    for (int i = 0; i < vertices.Length; i++)
    {
        int root = unionFind.Find(i);
        if (!islandDict.ContainsKey(root))
        {
            islandDict[root] = new List<int>();
        }
        islandDict[root].Add(i);
    }

    Dictionary<Vector3, List<int>> vertexMap = new Dictionary<Vector3, List<int>>();

    for (int i = 0; i < vertices.Length; i++)
    {
        if (!vertexMap.ContainsKey(vertices[i]))
        {
            vertexMap[vertices[i]] = new List<int>();
        }
        vertexMap[vertices[i]].Add(i);
    }


    foreach (var kvp in vertexMap)
    {
        var indices = kvp.Value;
        int rootIndex = unionFind.Find(indices[0]);
        for (int i = 1; i < indices.Count; i++)
        {
            unionFind.Unite(rootIndex, indices[i]);
        }
    }

    Dictionary<int, List<List<int>>> mergedIslandDict = new Dictionary<int, List<List<int>>>();
    foreach (var kvp in islandDict)
    {
        int mergedRoot = unionFind.Find(kvp.Key);
        if (!mergedIslandDict.ContainsKey(mergedRoot))
        {
            mergedIslandDict[mergedRoot] = new List<List<int>>();
        }
        mergedIslandDict[mergedRoot].Add(kvp.Value);
    }

    List<MergedIsland> mergedIslands = new List<MergedIsland>();
    foreach (var kvp in mergedIslandDict)
    {
        MergedIsland mergedIsland = new MergedIsland();
        foreach (var unmergedIslands in kvp.Value)
        {
            List<int> allVertices = unmergedIslands.Distinct().ToList();
            var allEdges = GetAllEdges(allVertices, vertexEdges);
            Island island = new Island(unmergedIslands, unmergedIslands[0], allEdges);
            mergedIsland.ChildIslands.Add(island);
        }
        mergedIslands.Add(mergedIsland);
    }
    return mergedIslands;
}

    private static void AddEdge(Dictionary<int, List<int>> vertexEdges, int v1, int v2)
    {
        if (!vertexEdges.ContainsKey(v1))
        {
            vertexEdges[v1] = new List<int>();
        }
        if (!vertexEdges.ContainsKey(v2))
        {
            vertexEdges[v2] = new List<int>();
        }
        vertexEdges[v1].Add(v2);
        vertexEdges[v2].Add(v1);
    }

    private static HashSet<(int, int)> GetAllEdges(List<int> vertices, Dictionary<int, List<int>> vertexEdges)
    {
        HashSet<(int, int)> allEdges = new HashSet<(int, int)>();
        foreach (int v in vertices)
        {
            if (vertexEdges.ContainsKey(v))
            {
                foreach (var adjacent in vertexEdges[v])
                {
                    var edge = v < adjacent ? (v, adjacent) : (adjacent, v);
                    allEdges.Add(edge);
                }
            }
        }
        return allEdges;
    }


public static int[] GetIslandIndexFromTriangleIndex(SkinnedMeshRenderer skinnedMeshRenderer, int triangleIndex, List<MergedIsland> mergedIslands, bool mergeSamePosition)
{
    Mesh mesh = skinnedMeshRenderer.sharedMesh;
    int[] triangles = mesh.triangles;

    if (triangleIndex < 0 || triangleIndex >= triangles.Length / 3) // 修正: 三角形のインデックスチェック
    {
        throw new ArgumentOutOfRangeException("triangleIndex", "Triangle index out of range.");
    }

    int vertexIndex = triangles[triangleIndex * 3]; // 修正前の正しいコードに戻す

    List<int> foundIndices = new List<int>();
    foreach (var mergedIsland in mergedIslands)
    {
        foreach (var island in mergedIsland.ChildIslands)
        {
            if (island.Vertices.Contains(vertexIndex))
            {
                if (mergeSamePosition)
                {
                    foreach (var samePosIsland in mergedIsland.ChildIslands)
                    {
                        foundIndices.Add(samePosIsland.UnmergedStartIndex);
                    }
                    break; // 外側のループも脱出するためにラベル付きbreakを使用
                }
                else
                {
                    foundIndices.Add(island.UnmergedStartIndex);
                }
            }
        }
        if (mergeSamePosition && foundIndices.Count > 0) break; // 外側のループも脱出する条件
    }

    if (foundIndices.Count == 0) // デバッグ用のログを追加
    {
        Debug.LogWarning($"No island found for vertexIndex: {vertexIndex}");
    }

    return foundIndices.ToArray();
}}