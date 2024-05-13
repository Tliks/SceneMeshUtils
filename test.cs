using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class IslandTextureGeneratorEditor : EditorWindow
{
    private SkinnedMeshRenderer skinnedMeshRenderer;
    private List<List<int>> islands;
    private Texture2D texture;

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
                texture = MeshIslandUtility.GenerateIslandExclusionTexture(skinnedMeshRenderer, islands, 512, 512);
            }
        }

        if (texture != null)
        {
            GUILayout.Label("Generated Texture:");
            GUILayout.Label(texture);
        }

        if (GUILayout.Button("Save Texture"))
        {
            if (texture != null)
            {
                byte[] bytes = texture.EncodeToPNG();
                System.IO.File.WriteAllBytes("Assets/IslandExclusionTexture.png", bytes);
                AssetDatabase.Refresh();
                Debug.Log("Texture saved to Assets/IslandExclusionTexture.png");
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

    public static Texture2D GenerateIslandExclusionTexture(SkinnedMeshRenderer skinnedMeshRenderer, List<List<int>> islands, int width, int height)
    {
        Mesh mesh = skinnedMeshRenderer.sharedMesh;
        Vector2[] uv = mesh.uv;
        Texture2D texture = new Texture2D(width, height);
        Color[] colors = new Color[width * height];

        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.black;
        }

        foreach (var island in islands)
        {
            foreach (int vertexIndex in island)
            {
                Vector2 uvCoord = uv[vertexIndex];
                int x = Mathf.FloorToInt(uvCoord.x * width);
                int y = Mathf.FloorToInt(uvCoord.y * height);
                colors[y * width + x] = Color.white;
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        return texture;
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