using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class MeshIslandUtility
{
    public static List<Island> GetIslands(SkinnedMeshRenderer skinnedMeshRenderer, int padding, int textureWidth, int textureHeight)
    {
        Mesh mesh = skinnedMeshRenderer.sharedMesh;
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;

        UnionFind unionFind = new UnionFind(vertices.Length);

        for (int i = 0; i < triangles.Length; i += 3)
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

        List<Island> islands = new List<Island>();
        int index = 0;
        foreach (var kvp in islandDict)
        {
            var island = CreateIsland(kvp.Value, mesh.uv, padding, textureWidth, textureHeight, index);
            islands.Add(island);
            index++;
        }

        return islands;
    }

    public static Texture2D GenerateIslandMaskedTexture(SkinnedMeshRenderer skinnedMeshRenderer, List<Island> islands, int textureWidth, int textureHeight, int padding, HashSet<int> selectedIslandIndices)
    {
        Mesh mesh = skinnedMeshRenderer.sharedMesh;
        Vector2[] uv = mesh.uv;
        Texture2D originalTexture = MakeReadable((Texture2D)skinnedMeshRenderer.sharedMaterial.mainTexture);

        Color backgroundColor = GetDominantCornerColor(originalTexture);
        Texture2D texture = new Texture2D(textureWidth, textureHeight);
        Color[] colors = Enumerable.Repeat(backgroundColor, textureWidth * textureHeight).ToArray();

        foreach (var island in islands)
        {
            int startX = Mathf.Max(0, island.StartX - padding);
            int startY = Mathf.Max(0, island.StartY - padding);
            int endX = Mathf.Min(textureWidth - 1, island.EndX + padding);
            int endY = Mathf.Min(textureHeight - 1, island.EndY + padding);

            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    float u = (float)x / textureWidth;
                    float v = (float)y / textureHeight;

                    if (u < 0 || u > 1 || v < 0 || v > 1) continue;

                    Color color = originalTexture.GetPixelBilinear(u, v);
                    if (selectedIslandIndices != null && selectedIslandIndices.Contains(island.Index))
                    {
                        color = Color.red;
                    }
                    else
                    {
                        color = Color.blue;
                    }
                    colors[y * textureWidth + x] = color;
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }

    private static Island CreateIsland(List<int> vertices, Vector2[] uv, int padding, int textureWidth, int textureHeight, int index)
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

        int startX = Mathf.FloorToInt(minX * textureWidth);
        int startY = Mathf.FloorToInt(minY * textureHeight);
        int endX = Mathf.CeilToInt(maxX * textureWidth);
        int endY = Mathf.CeilToInt(maxY * textureHeight);

        return new Island(vertices, startX, startY, endX, endY, index);
    }

    public static Texture2D MakeReadable(Texture2D original)
    {
        RenderTexture tempRT = RenderTexture.GetTemporary(
            original.width,
            original.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear
        );

        Graphics.Blit(original, tempRT);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = tempRT;

        Texture2D readableTexture = new Texture2D(original.width, original.height);
        readableTexture.ReadPixels(new Rect(0, 0, tempRT.width, tempRT.height), 0, 0);
        readableTexture.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(tempRT);

        return readableTexture;
    }

    public static Color GetDominantCornerColor(Texture2D texture)
    {
        Color[] cornerColors = new Color[]
        {
            texture.GetPixel(0, 0), // Bottom-left corner
            texture.GetPixel(texture.width - 1, 0), // Bottom-right corner
            texture.GetPixel(0, texture.height - 1), // Top-left corner
            texture.GetPixel(texture.width - 1, texture.height - 1) // Top-right corner
        };

        return cornerColors.GroupBy(c => c)
                           .OrderByDescending(g => g.Count())
                           .First()
                           .Key;
    }
}

public class Island
{
    public List<int> Vertices { get; }
    public int StartX { get; }
    public int StartY { get; }
    public int EndX { get; }
    public int EndY { get; }
    public int Index { get; }

    public Island(List<int> vertices, int startX, int startY, int endX, int endY, int index)
    {
        Vertices = vertices;
        StartX = startX;
        StartY = startY;
        EndX = endX;
        EndY = endY;
        Index = index;
    }
}

public class UnionFind
{
    private int[] parent;

    public UnionFind(int size)
    {
        parent = new int[size];
        for (int i = 0; i < size; i++)
        {
            parent[i] = i;
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
        if (rootX != rootY)
        {
            parent[rootX] = rootY;
        }
    }
}