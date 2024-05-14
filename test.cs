using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class IslandTextureGeneratorEditor : EditorWindow
{
    private SkinnedMeshRenderer skinnedMeshRenderer;
    private List<List<int>> islands;
    private Texture2D generatedTexture;
    private int padding = 0; // パディングのデフォルト値

    [MenuItem("Window/Island Texture Generator")]
    public static void ShowWindow()
    {
        GetWindow<IslandTextureGeneratorEditor>("Island Texture Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Island Texture Generator", EditorStyles.boldLabel);
        SkinnedMeshRenderer newRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Skinned Mesh Renderer", skinnedMeshRenderer, typeof(SkinnedMeshRenderer), true);

        if (newRenderer != skinnedMeshRenderer)
        {
            skinnedMeshRenderer = newRenderer;
            if (skinnedMeshRenderer != null)
            {
                islands = MeshIslandUtility.GetIslands(skinnedMeshRenderer);
                generatedTexture = MeshIslandUtility.GenerateIslandMaskedTexture(skinnedMeshRenderer, islands, 512, 512, padding);
            }
        }

        if (generatedTexture != null)
        {
            GUILayout.Label("Generated Texture:");
            GUILayout.Label(generatedTexture);
        }

        padding = EditorGUILayout.IntField("Padding", padding);

        if (GUILayout.Button("Save Texture"))
        {
            if (generatedTexture != null)
            {
                byte[] bytes = generatedTexture.EncodeToPNG();
                System.IO.File.WriteAllBytes("Assets/IslandMaskedTexture.png", bytes);
                AssetDatabase.Refresh();
                Debug.Log("Texture saved to Assets/IslandMaskedTexture.png");
            }
        }
    }
}

public static class MeshIslandUtility
{
    public static List<List<int>> GetIslands(SkinnedMeshRenderer skinnedMeshRenderer)
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

        List<List<int>> islands = new List<List<int>>();
        foreach (var island in islandDict.Values)
        {
            islands.Add(island);
        }

        return islands;
    }

    public static Texture2D GenerateIslandMaskedTexture(SkinnedMeshRenderer skinnedMeshRenderer, List<List<int>> islands, int width, int height, int padding)
    {
        Mesh mesh = skinnedMeshRenderer.sharedMesh;
        Vector2[] uv = mesh.uv;
        Texture2D originalTexture = MakeReadable((Texture2D)skinnedMeshRenderer.sharedMaterial.mainTexture);

        Color backgroundColor = GetDominantCornerColor(originalTexture);
        Texture2D texture = new Texture2D(width, height);
        Color[] colors = Enumerable.Repeat(backgroundColor, width * height).ToArray();

        foreach (var island in islands)
        {
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (int vertexIndex in island)
            {
                Vector2 uvCoord = uv[vertexIndex];
                if (uvCoord.x < minX) minX = uvCoord.x;
                if (uvCoord.y < minY) minY = uvCoord.y;
                if (uvCoord.x > maxX) maxX = uvCoord.x;
                if (uvCoord.y > maxY) maxY = uvCoord.y;
            }

            int startX = Mathf.Max(0, Mathf.FloorToInt(minX * width) - padding);
            int startY = Mathf.Max(0, Mathf.FloorToInt(minY * height) - padding);
            int endX = Mathf.Min(width - 1, Mathf.CeilToInt(maxX * width) + padding);
            int endY = Mathf.Min(height - 1, Mathf.CeilToInt(maxY * height) + padding);

            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    float u = (float)x / width;
                    float v = (float)y / height;

                    if (u < 0 || u > 1 || v < 0 || v > 1) continue;

                    Color color = originalTexture.GetPixelBilinear(u, v);
                    colors[y * width + x] = color;
                }
            }
            foreach (int vertexIndex in island)
            {
                Vector2 uvCoord = uv[vertexIndex];
                int x = Mathf.FloorToInt(uvCoord.x * width);
                int y = Mathf.FloorToInt(uvCoord.y * height);
                colors[y * width + x] = Color.cyan;
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        return texture;
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