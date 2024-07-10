
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class GenerateMaskUtilty
{
    private readonly SkinnedMeshRenderer _OriginskinnedMeshRenderer;
    private readonly string _rootname;
    private readonly HashSet<int> _SelectedTriangleIndices;

    private readonly int[] optionValues = { 512, 1024, 2048 };
    private readonly string[] displayOptions = { "512", "1024", "2048" };
    private int selectedValue = 512;
    private int _areacolorindex = 0;
    private int _backcolorindex = 1;
    private int _expansion = 2;
    private Mesh _originalMesh;


    public GenerateMaskUtilty(SkinnedMeshRenderer _OriginskinnedMeshRenderer, string _rootname, HashSet<int> _SelectedTriangleIndices, Mesh _originalMesh)
    {
        this._OriginskinnedMeshRenderer = _OriginskinnedMeshRenderer;
        this._rootname = _rootname;
        this._SelectedTriangleIndices = _SelectedTriangleIndices;
        this._originalMesh = _originalMesh;
    }

    public void RenderGenerateMask()
    {
        EditorGUILayout.Space();
        //EditorGUILayout.HelpBox(LocalizationEditor.GetLocalizedText("mask.description"), MessageType.Info);
        string[] options = { 
            LocalizationEditor.GetLocalizedText("mask.color.white"), 
            LocalizationEditor.GetLocalizedText("mask.color.black"), 
            LocalizationEditor.GetLocalizedText("mask.color.alpha"),
            LocalizationEditor.GetLocalizedText("mask.color.original"),
            LocalizationEditor.GetLocalizedText("mask.color.grayscale")
            };

        _areacolorindex = EditorGUILayout.Popup(LocalizationEditor.GetLocalizedText("mask.areacolor"), _areacolorindex, options);
        _backcolorindex = EditorGUILayout.Popup(LocalizationEditor.GetLocalizedText("mask.backcolor"), _backcolorindex, options);

        selectedValue = EditorGUILayout.IntPopup(LocalizationEditor.GetLocalizedText("mask.resolution"), selectedValue, displayOptions, optionValues);
        _expansion = EditorGUILayout.IntField(LocalizationEditor.GetLocalizedText("mask.expansion"), _expansion);
        
        // Create Selected Islands Module
        GUI.enabled = _OriginskinnedMeshRenderer != null && _SelectedTriangleIndices.Count > 0;
        EditorGUILayout.Space();
        if (GUILayout.Button(LocalizationEditor.GetLocalizedText("GenerateMaskTexture")))
        {
            GenerateMask();
        }
        GUI.enabled = true;

    }
    private Color[] CreateColorArray(int indexValue, Texture2D originalTexture, int textureSize)
    {
        Color[] colors = new Color[textureSize * textureSize];

        switch (indexValue)
        {
            case 0:
                for (int i = 0; i < colors.Length; i++)
                    colors[i] = Color.white;
                break;
            case 1:
                for (int i = 0; i < colors.Length; i++)
                    colors[i] = Color.black;
                break;
            case 2:
                for (int i = 0; i < colors.Length; i++)
                    colors[i] = new Color(0, 0, 0, 0);
                break;
            case 3:
                for (int y = 0; y < textureSize; y++)
                {
                    for (int x = 0; x < textureSize; x++)
                    {
                        colors[y * textureSize + x] = originalTexture.GetPixelBilinear((float)x / textureSize, (float)y / textureSize);
                    }
                }
                break;
            case 4:
                for (int y = 0; y < textureSize; y++)
                {
                    for (int x = 0; x < textureSize; x++)
                    {
                        Color origColor = originalTexture.GetPixelBilinear((float)x / textureSize, (float)y / textureSize);
                        float gray = origColor.grayscale;
                        colors[y * textureSize + x] = new Color(gray, gray, gray, origColor.a);
                    }
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return colors;
    }


    private void GenerateMask()
    {
        MeshMaskGenerator generator = new MeshMaskGenerator(selectedValue, _expansion);
        Texture2D originalTexture = GetReadableTexture(_OriginskinnedMeshRenderer.sharedMaterial.mainTexture as Texture2D);

        Color[] targetColors = CreateColorArray(_areacolorindex, originalTexture, selectedValue);
        Color[] baseColors = CreateColorArray(_backcolorindex, originalTexture, selectedValue);

        Dictionary<string, Texture2D> maskTextures = generator.GenerateMaskTextures(_OriginskinnedMeshRenderer, _SelectedTriangleIndices, baseColors, targetColors, _originalMesh);
        
        List<UnityEngine.Object> selectedObjects = new List<UnityEngine.Object>();
        foreach (KeyValuePair<string, Texture2D> kvp in maskTextures)
        {
            string timeStamp = DateTime.Now.ToString("yyMMdd_HHmmss");
            string path = AssetPathUtility.GenerateTexturePath(_rootname, $"{timeStamp}_{_OriginskinnedMeshRenderer.name}_{kvp.Key}");
            byte[] bytes = kvp.Value.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();

            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (obj != null)
            {
                selectedObjects.Add(obj);
                EditorGUIUtility.PingObject(obj);
                Debug.Log("Saved MaskTexture to " + path);
            }
        }
        //Selection.activeGameObject = null;
        Selection.objects = selectedObjects.ToArray();
    }

    private Texture2D GetReadableTexture(Texture2D originalTexture)
    {
        RenderTexture renderTexture = RenderTexture.GetTemporary(
            originalTexture.width,
            originalTexture.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);

        Graphics.Blit(originalTexture, renderTexture);

        RenderTexture previousRenderTexture = RenderTexture.active;
        RenderTexture.active = renderTexture;

        Texture2D readableTexture = new Texture2D(originalTexture.width, originalTexture.height);
        readableTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        readableTexture.Apply();

        RenderTexture.active = previousRenderTexture;
        RenderTexture.ReleaseTemporary(renderTexture);

        return readableTexture;
    }

}

