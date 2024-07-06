
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
        string[] options = { LocalizationEditor.GetLocalizedText("mask.color.white"), LocalizationEditor.GetLocalizedText("mask.color.black"), LocalizationEditor.GetLocalizedText("mask.color.original") };
        _areacolorindex = EditorGUILayout.Popup(LocalizationEditor.GetLocalizedText("mask.color"), _areacolorindex, options);

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

    private void GenerateMask()
    {
        MeshMaskGenerator generator = new MeshMaskGenerator(selectedValue, _expansion);
        Dictionary<string, Texture2D> maskTextures = generator.GenerateMaskTextures(_OriginskinnedMeshRenderer, _SelectedTriangleIndices, _areacolorindex, _originalMesh);
        
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
}

