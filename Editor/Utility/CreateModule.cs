using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CreateModuleUtilty
{
    private bool _showAdvancedOptions = false;
    private static ModuleCreatorSettings _Settings = new ModuleCreatorSettings
    {
        IncludePhysBone = true,
        IncludePhysBoneColider = true
    };

    private readonly SkinnedMeshRenderer _OriginskinnedMeshRenderer;
    private readonly string _rootname;
    private readonly HashSet<int> _SelectedTriangleIndices;
    private readonly HashSet<int> _UnselectedTriangleIndices;

    public CreateModuleUtilty(SkinnedMeshRenderer _OriginskinnedMeshRenderer, string _rootname, HashSet<int> _SelectedTriangleIndices, HashSet<int> _UnselectedTriangleIndices)
    {
        this._OriginskinnedMeshRenderer = _OriginskinnedMeshRenderer;
        this._rootname = _rootname;
        this._SelectedTriangleIndices = _SelectedTriangleIndices;
        this._UnselectedTriangleIndices = _UnselectedTriangleIndices;
    }
    
    public void RenderModuleCreator()
    {
        RenderPhysBoneOptions();

        EditorGUILayout.Space();

        RenderCreateModuleButtons();
        RenderCreateBothModuleButtons();
        EditorGUILayout.Space();
        
        process_advanced_options();
    }


    private void RenderPhysBoneOptions()
    {
        EditorGUILayout.Space();

        _Settings.IncludePhysBone = EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("PhysBoneToggle"), _Settings.IncludePhysBone);

        GUI.enabled = _Settings.IncludePhysBone;
        _Settings.IncludePhysBoneColider = EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("PhysBoneColiderToggle"), _Settings.IncludePhysBoneColider);
        GUI.enabled = true;
    }
    private void CreateModule(HashSet<int> Triangles)
    {
        if (Triangles.Count > 0)
        {
            Mesh newMesh = MeshUtility.DeleteMesh(_OriginskinnedMeshRenderer, Triangles);

            string path = AssetPathUtility.GenerateMeshPath(_rootname);
            AssetDatabase.CreateAsset(newMesh, path);
            AssetDatabase.SaveAssets();

            _Settings.newmesh = newMesh;
            new ModuleCreator(_Settings).CheckAndCopyBones(_OriginskinnedMeshRenderer.gameObject);
        }
    }
    private void RenderCreateBothModuleButtons()
    {
        GUI.enabled = _OriginskinnedMeshRenderer != null && _SelectedTriangleIndices.Count > 0;

        // Create Both Modules
        if (GUILayout.Button(LocalizationEditor.GetLocalizedText("CreateBothModulesButton")))
        {
            CreateModule(_SelectedTriangleIndices);
            CreateModule(_UnselectedTriangleIndices);
            //Close();
        }

        GUI.enabled = true;
    }
    private void RenderCreateModuleButtons()
    {
        GUI.enabled = _OriginskinnedMeshRenderer != null && _SelectedTriangleIndices.Count > 0;
        
        // Create Selected Islands Module
        if (GUILayout.Button(LocalizationEditor.GetLocalizedText("CreateModuleButton")))
        {
            CreateModule(_SelectedTriangleIndices);
            //Close();
        }

        GUI.enabled = true;
    }

    private void process_advanced_options()
    {   

        _showAdvancedOptions = EditorGUILayout.Foldout(_showAdvancedOptions, LocalizationEditor.GetLocalizedText("advancedoptions"));
        if (_showAdvancedOptions)
        {
            
            GUI.enabled = _Settings.IncludePhysBone;
            GUIContent content_at = new GUIContent(LocalizationEditor.GetLocalizedText("AdditionalTransformsToggle"), LocalizationEditor.GetLocalizedText("tooltip.AdditionalTransformsToggle"));
            _Settings.RemainAllPBTransforms = EditorGUILayout.Toggle(content_at, _Settings.RemainAllPBTransforms);

            GUIContent content_ii = new GUIContent(LocalizationEditor.GetLocalizedText("IncludeIgnoreTransformsToggle"), LocalizationEditor.GetLocalizedText("tooltip.IncludeIgnoreTransformsToggle"));
            _Settings.IncludeIgnoreTransforms = EditorGUILayout.Toggle(content_ii, _Settings.IncludeIgnoreTransforms);

            GUIContent content_rr = new GUIContent(
                LocalizationEditor.GetLocalizedText("RenameRootTransformToggle"),
                LocalizationEditor.GetLocalizedText("tooltip.RenameRootTransformToggle"));
            _Settings.RenameRootTransform = EditorGUILayout.Toggle(content_rr, _Settings.RenameRootTransform);

            GUI.enabled = true;

            GUIContent content_sr = new GUIContent(LocalizationEditor.GetLocalizedText("SpecifyRootObjectLabel"), LocalizationEditor.GetLocalizedText("tooltip.SpecifyRootObjectLabel"));
            _Settings.RootObject = (GameObject)EditorGUILayout.ObjectField(content_sr, _Settings.RootObject, typeof(GameObject), true);
                    
        }

        EditorGUILayout.Space();
    }


}