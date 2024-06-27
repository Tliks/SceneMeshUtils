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

public class Island
{
    public List<int> Triangles { get; }
    public int Index { get; }
    
    public Island(List<int> triangles, int index)
    {
        Triangles = triangles;
        Index = index;
    }
}

public class IslandUtility
{

    private List<List<Island>> _mergedIslands;
    
    public IslandUtility(Mesh mesh)
    {
        _mergedIslands = GetIslands(mesh);
    }

    private static List<List<Island>> GetIslands(Mesh mesh)
    {
        Stopwatch stopwatch = new Stopwatch();

        stopwatch.Start();
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;
        int tricount = triangles.Length;

        UnionFind unionFind = new UnionFind(vertices.Length);

        for (int i = 0; i < tricount; i += 3)
        {
            int v1 = triangles[i];
            int v2 = triangles[i + 1];
            int v3 = triangles[i + 2];

            unionFind.Unite(v1, v2);
            unionFind.Unite(v2, v3);
            unionFind.Unite(v3, v1);
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

        Dictionary<Vector3, List<int>> vertexMap = new Dictionary<Vector3, List<int>>(vertices.Length);
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

        Dictionary<int, List<int>> vertexToTriangleMap = new Dictionary<int, List<int>>(vertices.Length);
        for (int i = 0; i < tricount; i++)
        {
            int vertexIndex = triangles[i];
            if (!vertexToTriangleMap.ContainsKey(vertexIndex))
            {
                vertexToTriangleMap[vertexIndex] = new List<int>();
            }
            vertexToTriangleMap[vertexIndex].Add(i / 3); // トライアングルのインデックスを保存
        }

        Dictionary<int, List<List<int>>> islandDictWithTriangles = new Dictionary<int, List<List<int>>>(islandDict.Count);
        foreach (var kvp in islandDict)
        {
            int mergedRoot = unionFind.Find(kvp.Key);
            if (!islandDictWithTriangles.ContainsKey(mergedRoot))
            {
                islandDictWithTriangles[mergedRoot] = new List<List<int>>();
            }

            HashSet<int> triangleIndices = new HashSet<int>();
            foreach (int vertexIndex in kvp.Value)
            {
                triangleIndices.UnionWith(vertexToTriangleMap[vertexIndex]);
            }
            islandDictWithTriangles[mergedRoot].Add(triangleIndices.ToList());
        }

        List<List<Island>> mergedIslands = new List<List<Island>>(islandDictWithTriangles.Count);
        int index = 0;
        foreach (var kvp in islandDictWithTriangles)
        {
            List<Island> mergedIsland = new List<Island>(kvp.Value.Count);
            foreach (List<int> allTriangles in kvp.Value)
            {
                Island island = new Island(allTriangles, index++);
                mergedIsland.Add(island);
            }
            mergedIslands.Add(mergedIsland);
        }
        stopwatch.Stop();
        Debug.Log($"GetIslands completed in {stopwatch.ElapsedMilliseconds} ms");

        return mergedIslands;
    }

    public List<int> GetIslandtrianglesFromTriangleIndex(Mesh mesh, int triangleIndex, bool mergeSamePosition)
    {   
        int[] triangles = mesh.triangles;

        if (triangleIndex < 0 || triangleIndex >= triangles.Length/ 3)
        {
            throw new ArgumentOutOfRangeException("triangleIndex", "Triangle index out of range.");
        }

        HashSet<int> foundVertices = new HashSet<int>();
        foreach (var mergedIsland in _mergedIslands)
        {
            foreach (var island in mergedIsland)
            {
                if (island.Triangles.Contains(triangleIndex))
                {
                    if (mergeSamePosition)
                    {
                        foreach (var samePosIsland in mergedIsland)
                        {
                            foundVertices.UnionWith(samePosIsland.Triangles);
                        }
                    }
                    else
                    {
                        foundVertices.UnionWith(island.Triangles);
                    }
                    break;
                }
            }
            if (foundVertices.Count > 0) break;
        }

        if (foundVertices.Count == 0)
        {
            Debug.LogWarning($"No island found for triangleIndex: {triangleIndex}");
        }

        return foundVertices.ToList();
    }

    public List<int> GetIslandVerticesInCollider(Vector3[] vertices, MeshCollider collider, bool mergeSamePosition, bool isall, Transform transform)
    {
        HashSet<int> foundVertices = new HashSet<int>();

        bool IsInsideIsland(Island island, bool checkAll)
        {
            return checkAll ? 
                island.Triangles.TrueForAll(vertexIndex => 
                    IsVertexCloseToCollider(vertexIndex)) : 
                island.Triangles.Exists(vertexIndex => 
                    IsVertexCloseToCollider(vertexIndex));
        }

        bool IsVertexCloseToCollider(int vertexIndex)
        {
            Vector3 point = transform.TransformPoint(vertices[vertexIndex]);
            Vector3 closestPoint = collider.ClosestPoint(point);
            float distance = Vector3.Distance(closestPoint, point);
            return distance < 0.001f;
        }

        foreach (var mergedIsland in _mergedIslands)
        {
            bool isInsideMergedIsland = false;

            if (mergeSamePosition)
            {
                isInsideMergedIsland = isall ? 
                    mergedIsland.All(island => IsInsideIsland(island, true)) :
                    mergedIsland.Any(island => IsInsideIsland(island, false));

                if (isInsideMergedIsland)
                {
                    foreach (var samePosIsland in mergedIsland)
                    {
                        foundVertices.UnionWith(samePosIsland.Triangles);
                    }
                }
            }
            else
            {
                foreach (var island in mergedIsland)
                {
                    if (IsInsideIsland(island, isall))
                    {
                        foundVertices.UnionWith(island.Triangles);
                    }
                }
            }
        }

        return foundVertices.ToList();
    }
    public int GetIslandCount()
    {
        return _mergedIslands.Sum(innerList => innerList.Count);
    }

    public int GetMergedIslandCount()
    {
        return _mergedIslands.Count;
    }    

}