using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class IslandTextureGeneratorEditor : EditorWindow
{
    private SkinnedMeshRenderer skinnedMeshRenderer;
    private List<Island> islands;
    private Texture2D generatedTexture;
    private int padding = 0; 
    private int textureWidth = 512;
    private int textureHeight = 512;
    private HashSet<int> selectedIslandIndices;

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
                islands = MeshIslandUtility.GetIslands(skinnedMeshRenderer, padding, textureWidth, textureHeight);
                selectedIslandIndices = new HashSet<int>(); 
            }
        }

        padding = EditorGUILayout.IntField("Padding", padding);
        textureWidth = EditorGUILayout.IntField("Texture Width", textureWidth);
        textureHeight = EditorGUILayout.IntField("Texture Height", textureHeight);

        if (GUILayout.Button("Generate Texture"))
        {
            if (skinnedMeshRenderer != null)
            {
                generatedTexture = MeshIslandUtility.GenerateIslandMaskedTexture(skinnedMeshRenderer, islands, textureWidth, textureHeight, padding, selectedIslandIndices);
                IslandTextureViewer.ShowTextureWindow(generatedTexture, skinnedMeshRenderer, islands, selectedIslandIndices, padding);
            }
        }

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