using UnityEngine;
using System.Collections.Generic;
using System;

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

public class Island
{
    public List<int> Vertices { get; }
    public int Index { get; }
    public HashSet<(int, int)> AllEdges { get; }

    public Island(List<int> vertices, int index, HashSet<(int, int)> allEdges)
    {
        Vertices = vertices;
        Index = index;
        AllEdges = allEdges;
    }
}

public static class MeshIslandUtility
{
public static List<List<Island>> GetIslands(SkinnedMeshRenderer skinnedMeshRenderer)
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

    List<List<Island>> mergedIslands = new List<List<Island>>();
    int index = 0;
    foreach (var kvp in mergedIslandDict)
    {
        List<Island> mergedIsland = new List<Island>();
        foreach (List<int> allVertices in kvp.Value)
        {
            var allEdges = GetAllEdges(allVertices, vertexEdges);
            Island island = new Island(allVertices, index++, allEdges);
            mergedIsland.Add(island);
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


public static List<int> GetIslandIndexFromTriangleIndex(SkinnedMeshRenderer skinnedMeshRenderer, int triangleIndex, List<List<Island>> mergedIslands, bool mergeSamePosition)
{
    Mesh mesh = skinnedMeshRenderer.sharedMesh;
    int[] triangles = mesh.triangles;

    if (triangleIndex < 0 || triangleIndex >= triangles.Length / 3)
    {
        throw new ArgumentOutOfRangeException("triangleIndex", "Triangle index out of range.");
    }

    int vertexIndex = triangles[triangleIndex * 3];

    List<int> foundIndices = new List<int>();
    foreach (var mergedIsland in mergedIslands)
    {
        foreach (var island in mergedIsland)
        {
            if (island.Vertices.Contains(vertexIndex))
            {
                if (mergeSamePosition)
                {
                    foreach (var samePosIsland in mergedIsland)
                    {
                        foundIndices.Add(samePosIsland.Index);
                    }
                }
                else
                {
                    foundIndices.Add(island.Index);
                }
                break;
            }
        }
        if (foundIndices.Count > 0) break;
    }

    if (foundIndices.Count == 0)
    {
        Debug.LogWarning($"No island found for vertexIndex: {vertexIndex}");
    }

    return foundIndices;
}}