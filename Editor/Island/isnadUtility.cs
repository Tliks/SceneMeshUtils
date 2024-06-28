using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using Stopwatch = System.Diagnostics.Stopwatch;

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


public struct Island
{
    public List<int> VertexIndices { get; }
    public List<int> TriangleIndices { get; }

    public Island(List<int> vertexIndices, List<int> triangleIndices)
    {
        VertexIndices = vertexIndices;
        TriangleIndices = triangleIndices;
    }
}

public class IslandUtility
{
    List<Island> MergedIslands { get; }
    List<Island> UnMergedIslands { get; }

    public IslandUtility(Mesh mesh)
    {
        (UnMergedIslands, MergedIslands) = GetIslands(mesh);
    }

    public static (List<Island>, List<Island>) GetIslands(Mesh mesh)
    {
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;
        int triCount = triangles.Length;
        int vertCount = vertices.Length;

        UnionFind unionFind = new UnionFind(vertCount);

        // 三角形の頂点をUnionFindで結合
        for (int i = 0; i < triCount; i += 3)
        {
            unionFind.Unite(triangles[i], triangles[i + 1]);
            unionFind.Unite(triangles[i + 1], triangles[i + 2]);
        }

        // 結合前のアイランドを作成
        Dictionary<int, Island> islandDict = new Dictionary<int, Island>();
        for (int i = 0; i < vertCount; i++)
        {
            int root = unionFind.Find(i);
            if (!islandDict.TryGetValue(root, out Island island))
            {
                island = new Island(new List<int>(), new List<int>());
                islandDict[root] = island;
            }
            island.VertexIndices.Add(i);
        }

        // 三角形インデックスを追加
        for (int i = 0; i < triCount; i += 3)
        {
            int root = unionFind.Find(triangles[i]);
            islandDict[root].TriangleIndices.Add(i / 3);
        }

        // 結合前のアイランドを作成
        List<Island> beforeMerge = new List<Island>(islandDict.Values);

        // 同一座標の頂点を結合
        Dictionary<Vector3, int> vertexMap = new Dictionary<Vector3, int>(vertCount);
        for (int i = 0; i < vertCount; i++)
        {
            if (vertexMap.TryGetValue(vertices[i], out int existingIndex))
            {
                unionFind.Unite(existingIndex, i);
            }
            else
            {
                vertexMap[vertices[i]] = i;
            }
        }

        // 結合後のアイランドを作成
        Dictionary<int, Island> mergedIslandDict = new Dictionary<int, Island>();
        foreach (var kvp in islandDict)
        {
            int mergedRoot = unionFind.Find(kvp.Key);
            if (!mergedIslandDict.TryGetValue(mergedRoot, out Island mergedIsland))
            {
                mergedIsland = new Island(new List<int>(), new List<int>());
                mergedIslandDict[mergedRoot] = mergedIsland;
            }
            mergedIsland.VertexIndices.AddRange(kvp.Value.VertexIndices);
            mergedIsland.TriangleIndices.AddRange(kvp.Value.TriangleIndices);
        }

        List<Island> afterMerge = new List<Island>(mergedIslandDict.Values);

        return (beforeMerge, afterMerge);
    }

    public List<int> GetIslandtrianglesFromTriangleIndex(int triangleIndex, bool mergeSamePosition)
    {   
        List<int> foundTriangles = new List<int>();
        List<Island> islands = mergeSamePosition ? MergedIslands : UnMergedIslands;

        foreach (var island in islands)
        {
            if (island.TriangleIndices.Contains(triangleIndex))
            {
                foundTriangles = new List<int>(island.TriangleIndices);
                break;
            }
        }

        if (foundTriangles.Count == 0)
        {
            Debug.LogWarning($"No island found for triangleIndex: {triangleIndex}");
        }

        return foundTriangles;
    }

    public List<int> GetIslandVerticesInCollider(Vector3[] vertices, MeshCollider collider, bool mergeSamePosition, bool checkAll, Transform transform)
    {
        HashSet<int> foundVertices = new HashSet<int>();
        
        List<Island> islands = mergeSamePosition ? MergedIslands : UnMergedIslands;
        foreach (var island in islands)
        {
            if (IsInsideIsland(island, checkAll))
            {
                foundVertices.UnionWith(island.VertexIndices);
            }
        }

        return foundVertices.ToList();

        bool IsInsideIsland(Island island, bool checkAll) =>
            checkAll ? island.VertexIndices.TrueForAll(IsVertexCloseToCollider) 
                    : island.VertexIndices.Exists(IsVertexCloseToCollider);

        bool IsVertexCloseToCollider(int vertexIndex)
        {
            Vector3 point = transform.TransformPoint(vertices[vertexIndex]);
            Vector3 closestPoint = collider.ClosestPoint(point);
            return Vector3.Distance(closestPoint, point) < 0.001f;
        }
    }

    public int GetIslandCount()
    {
        return UnMergedIslands.Count;
    }

    public int GetMergedIslandCount()
    {
        return MergedIslands.Count;
    }    

}