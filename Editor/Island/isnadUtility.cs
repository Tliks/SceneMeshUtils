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

public class Island
{
    public List<int> Vertices { get; }
    public Vector2 StartUV { get; }
    public Vector2 EndUV { get; }
    public float Area { get; }
    public int Index { get; }
    public HashSet<(int, int)> BoundaryVertices { get; }
    public HashSet<(int, int)> AllEdges { get; }

    public Island(List<int> vertices, Vector2 startUV, Vector2 endUV, int index, float area, HashSet<(int, int)> boundaryVertices, HashSet<(int, int)> allEdges)
    {
        Vertices = vertices;
        StartUV = startUV;
        EndUV = endUV;
        Index = index;
        Area = area;
        BoundaryVertices = boundaryVertices;
        AllEdges = allEdges;
    }
}

public static class MeshIslandUtility
{
    public static List<Island> GetIslands(SkinnedMeshRenderer skinnedMeshRenderer)
    {
        Mesh mesh = skinnedMeshRenderer.sharedMesh;
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;

        UnionFind unionFind = new UnionFind(vertices.Length);
        Dictionary<(int, int), int> edgeCount = new Dictionary<(int, int), int>();
        Dictionary<int, List<int>> vertexEdges = new Dictionary<int, List<int>>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v1 = triangles[i];
            int v2 = triangles[i + 1];
            int v3 = triangles[i + 2];

            unionFind.Unite(v1, v2);
            unionFind.Unite(v2, v3);
            unionFind.Unite(v3, v1);

            AddEdge(edgeCount, vertexEdges, v1, v2);
            AddEdge(edgeCount, vertexEdges, v2, v3);
            AddEdge(edgeCount, vertexEdges, v3, v1);
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

        List<Island> islands = new List<Island>();
        int index = 0;
        foreach (var kvp in islandDict)
        {
            var allEdges = GetAllEdges(kvp.Value, vertexEdges);
            var boundaryVertices = GetBoundaryVertices(kvp.Value, edgeCount, vertexEdges);
            var island = CreateIsland(kvp.Value, mesh.uv, index, boundaryVertices, allEdges);
            islands.Add(island);
            index++;
        }

        return islands;
    }

    private static void AddEdge(Dictionary<(int, int), int> edgeCount, Dictionary<int, List<int>> vertexEdges, int v1, int v2)
    {
        var sortedEdge = v1 < v2 ? (v1, v2) : (v2, v1);
        if (edgeCount.ContainsKey(sortedEdge))
        {
            edgeCount[sortedEdge]++;
        }
        else
        {
            edgeCount[sortedEdge] = 1;
        }

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

    private static HashSet<(int, int)> GetBoundaryVertices(List<int> vertices, Dictionary<(int, int), int> edgeCount, Dictionary<int, List<int>> vertexEdges)
    {
        HashSet<int> boundaryVertices = new HashSet<int>();
        HashSet<(int, int)> boundaryEdges = new HashSet<(int, int)>();

        foreach (int v in vertices)
        {
            if (vertexEdges.ContainsKey(v))
            {
                foreach (var adjacent in vertexEdges[v])
                {
                    var edge = v < adjacent ? (v, adjacent) : (adjacent, v);
                    if (edgeCount[edge] == 1)
                    {
                        boundaryVertices.Add(v);
                        boundaryVertices.Add(adjacent);
                        boundaryEdges.Add(edge);
                    }
                }
            }
        }
        return boundaryEdges;
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

    private static Island CreateIsland(List<int> vertices, Vector2[] uv, int index, HashSet<(int, int)> boundaryVertices, HashSet<(int, int)> allEdges)
    {
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        foreach (int vertexIndex in vertices)
        {
            Vector2 uvCoord = uv[vertexIndex];
            if (uvCoord.x < minX) minX = uvCoord.x;
            if (uvCoord.y < minY) minY = uvCoord.y;
            if (uvCoord.x > maxX) maxX = uvCoord.x;
            if (uvCoord.y > maxY) maxY = uvCoord.y;
        }

        Vector2 startUV = new Vector2(minX, minY);
        Vector2 endUV = new Vector2(maxX, maxY);

        float width = Mathf.Abs(endUV.x - startUV.x);
        float height = Mathf.Abs(endUV.y - startUV.y);
        float area = width * height;

        return new Island(vertices, startUV, endUV, index, area, boundaryVertices, allEdges);
    }

    public static int GetIslandIndexFromTriangleIndex(SkinnedMeshRenderer skinnedMeshRenderer, int triangleIndex, List<Island> islands)
    {
        Mesh mesh = skinnedMeshRenderer.sharedMesh;
        int[] triangles = mesh.triangles;

        if (triangleIndex < 0 || triangleIndex >= triangles.Length)
        {
            throw new ArgumentOutOfRangeException("triangleIndex", "Triangle index out of range.");
        }

        int vertexIndex = triangles[triangleIndex* 3];

        for (int i = 0; i < islands.Count; i++)
        {
            if (islands[i].Vertices.Contains(vertexIndex))
            {
                return i;
            }
        }

        throw new InvalidOperationException("Triangle index does not belong to any island.");
    }
}