using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class IslandTextureViewer : EditorWindow
{
    private Texture2D texture;
    private SkinnedMeshRenderer skinnedMeshRenderer;
    private List<Island> islands;
    private HashSet<int> selectedIslandIndices;
    private int padding;

    private const int windowSize = 512;

    public static IslandTextureViewer ShowTextureWindow(Texture2D texture, SkinnedMeshRenderer skinnedMeshRenderer, List<Island> islands, HashSet<int> selectedIslandIndices, int padding)
    {
        var window = CreateInstance<IslandTextureViewer>();
        window.texture = texture;
        window.skinnedMeshRenderer = skinnedMeshRenderer;
        window.islands = islands;
        window.selectedIslandIndices = selectedIslandIndices;
        window.padding = padding;
        window.titleContent = new GUIContent("Island Texture Viewer");
        window.minSize = new Vector2(windowSize, windowSize);
        window.maxSize = new Vector2(windowSize, windowSize);
        window.ShowUtility();
        return window;
    }

    void OnGUI()
    {
        if (texture != null)
        {
            Rect textureRect = new Rect(0, 0, windowSize, windowSize);
            GUI.DrawTexture(textureRect, texture, ScaleMode.ScaleToFit);

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                Vector2 textureCoords = Event.current.mousePosition;
                HandleTextureClick(textureCoords, textureRect);
            }
        }
    }

    private void HandleTextureClick(Vector2 textureCoords, Rect textureRect)
    {
        float u = textureCoords.x / textureRect.width;
        float v = 1.0f - (textureCoords.y / textureRect.height);

        Texture2D originalTexture = (Texture2D)skinnedMeshRenderer.sharedMaterial.mainTexture;
        int originalWidth = originalTexture.width;
        int originalHeight = originalTexture.height;

        if (islands != null && islands.Count > 0)
        {
            for (int i = 0; i < islands.Count; i++)
            {
                var island = islands[i];
                if (u >= (float)island.StartX / originalWidth && u <= (float)island.EndX / originalWidth &&
                    v >= (float)island.StartY / originalHeight && v <= (float)island.EndY / originalHeight)
                {
                    if (selectedIslandIndices.Contains(i))
                    {
                        selectedIslandIndices.Remove(i);
                    }
                    else
                    {
                        selectedIslandIndices.Add(i);
                    }

                    texture = MeshIslandUtility.GenerateIslandMaskedTexture(skinnedMeshRenderer, islands, originalWidth, originalHeight, padding, selectedIslandIndices);
                    Repaint();
                    break;
                }
            }
        }
    }
}