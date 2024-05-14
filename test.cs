using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class IslandTextureGeneratorEditor : EditorWindow
{
    private SkinnedMeshRenderer skinnedMeshRenderer;
    private List<Island> islands;
    private Texture2D generatedTexture;
    private int padding = 5; // パディングのデフォルト値
    private Vector2 scrollPosition;

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
                islands = MeshIslandUtility.GetIslands(skinnedMeshRenderer, padding);
                Debug.Log(islands.Count);
                generatedTexture = MeshIslandUtility.GenerateIslandMaskedTexture(skinnedMeshRenderer, islands, 512, 512, padding);
            }
        }

        if (generatedTexture != null)
        {
            GUILayout.Label("Generated Texture:");
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            GUILayout.Label(generatedTexture);
            GUILayout.EndScrollView();

            Rect textureRect = GUILayoutUtility.GetLastRect();
            Vector2 textureCoords = Event.current.mousePosition - new Vector2(textureRect.x, textureRect.y);
            
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {   
                Debug.Log(1);
                HandleTextureClick(textureCoords, textureRect);
            }
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

    private void HandleTextureClick(Vector2 textureCoords, Rect textureRect)
    {
        if (islands != null && islands.Count > 0)
        {
            Debug.Log(2);
            Island clickedIsland = null;
            foreach (var island in islands)
            {
                if (textureCoords.x >= island.StartX && textureCoords.x <= island.EndX &&
                    textureCoords.y >= island.StartY && textureCoords.y <= island.EndY)
                {
                    if (clickedIsland == null || 
                        (clickedIsland.EndX - clickedIsland.StartX) * (clickedIsland.EndY - clickedIsland.StartY) > 
                        (island.EndX - island.StartX) * (island.EndY - island.StartY))
                    {
                        Debug.Log(3);
                        clickedIsland = island;
                    }
                }
            }

            if (clickedIsland != null)
            {
                ColorVertices(clickedIsland.Vertices, Color.cyan);
                generatedTexture = MeshIslandUtility.GenerateIslandMaskedTexture(skinnedMeshRenderer, islands, 512, 512, padding); // テクスチャを再生成
                Repaint();
            }
        }
    }

    private void ColorVertices(List<int> vertices, Color color)
    {
        Mesh mesh = skinnedMeshRenderer.sharedMesh;
        Color[] vertexColors = new Color[mesh.vertexCount];

        for (int i = 0; i < vertexColors.Length; i++)
        {
            vertexColors[i] = mesh.colors.Length > 0 ? mesh.colors[i] : Color.white;
        }

        foreach (int vertex in vertices)
        {
            vertexColors[vertex] = color;
        }

        mesh.colors = vertexColors;
    }
}

public static class MeshIslandUtility
{
    public static List<Island> GetIslands(SkinnedMeshRenderer skinnedMeshRenderer, int padding)
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
        foreach (var kvp in islandDict)
        {
            var island = CreateIsland(kvp.Value, mesh.uv, padding);
            islands.Add(island);
        }

        return islands;
    }

    public static Texture2D GenerateIslandMaskedTexture(SkinnedMeshRenderer skinnedMeshRenderer, List<Island> islands, int width, int height, int padding)
    {
        Mesh mesh = skinnedMeshRenderer.sharedMesh;
        Vector2[] uv = mesh.uv;
        Texture2D originalTexture = MakeReadable((Texture2D)skinnedMeshRenderer.sharedMaterial.mainTexture);

        Color backgroundColor = GetDominantCornerColor(originalTexture);
        Texture2D texture = new Texture2D(width, height);
        Color[] colors = Enumerable.Repeat(backgroundColor, width * height).ToArray();

        foreach (var island in islands)
        {
            int startX = Mathf.Max(0, island.StartX - padding);
            int startY = Mathf.Max(0, island.StartY - padding);
            int endX = Mathf.Min(width - 1, island.EndX + padding);
            int endY = Mathf.Min(height - 1, island.EndY + padding);

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
        }

        texture.SetPixels(colors);
        texture.Apply();
        return texture;
    }

    private static Island CreateIsland(List<int> vertices, Vector2[] uv, int padding)
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

        int startX = Mathf.FloorToInt(minX * 512); // 512はテクスチャの幅
        int startY = Mathf.FloorToInt(minY * 512); // 512はテクスチャの高さ
        int endX = Mathf.CeilToInt(maxX * 512);
        int endY = Mathf.CeilToInt(maxY * 512);

        return new Island(vertices, startX, startY, endX, endY);
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

    public Island(List<int> vertices, int startX, int startY, int endX, int endY)
    {
        Vertices = vertices;
        StartX = startX;
        StartY = startY;
        EndX = endX;
        EndY = endY;
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