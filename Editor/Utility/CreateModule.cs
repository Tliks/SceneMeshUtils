using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CreateModuleUtilty
{
    private static bool _showAdvancedOptions = false;
    private static ModuleCreatorSettings _Settings = new ModuleCreatorSettings
    {
        IncludePhysBone = true,
        IncludePhysBoneColider = true
    };

    private static SkinnedMeshRenderer _originskinnedMeshRenderer;
    private static Mesh _originalMesh;
    private static string _rootname;
    private static TriangleSelectionManager _triangleSelectionManager;

    public static void Initialize(SkinnedMeshRenderer originskinnedMeshRenderer, string rootname, Mesh originalMesh, TriangleSelectionManager triangleSelectionManager)
    {
        _originskinnedMeshRenderer = originskinnedMeshRenderer;
        _rootname = rootname;
        _originalMesh = originalMesh;
        _triangleSelectionManager = triangleSelectionManager;
    }
    
    public static void RenderModuleCreator()
    {
        RenderPhysBoneOptions();

        EditorGUILayout.Space();

        RenderCreateModuleButtons();
        RenderCreateBothModuleButtons();
        EditorGUILayout.Space();
        
        process_advanced_options();
    }


    private static void RenderPhysBoneOptions()
    {
        EditorGUILayout.Space();

        _Settings.IncludePhysBone = EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("PhysBoneToggle"), _Settings.IncludePhysBone);

        GUI.enabled = _Settings.IncludePhysBone;
        _Settings.IncludePhysBoneColider = EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("PhysBoneColiderToggle"), _Settings.IncludePhysBoneColider);
        GUI.enabled = true;
    }
    private static void CreateModule(HashSet<int> Triangles)
    {
        if (Triangles.Count > 0)
        {
            Mesh newMesh = MeshUtility.DeleteMesh(_originalMesh, Triangles);

            string path = AssetPathUtility.GenerateMeshPath(_rootname, "PartialMesh");
            AssetDatabase.CreateAsset(newMesh, path);
            AssetDatabase.SaveAssets();

            _Settings.newmesh = newMesh;
            new ModuleCreator(_Settings).CheckAndCopyBones(_originskinnedMeshRenderer.gameObject);
        }
    }
    private static void RenderCreateBothModuleButtons()
    {
        GUI.enabled = _originskinnedMeshRenderer != null && _triangleSelectionManager.GetSelectedTriangles().Count > 0;

        // Create Both Modules
        if (GUILayout.Button(LocalizationEditor.GetLocalizedText("CreateBothModulesButton")))
        {
            CreateModule(_triangleSelectionManager.GetSelectedTriangles());
            CreateModule(_triangleSelectionManager.GetUnselectedTriangles());
            //Close();
        }

        GUI.enabled = true;
    }
    private static void RenderCreateModuleButtons()
    {
        GUI.enabled = _originskinnedMeshRenderer != null && _triangleSelectionManager.GetSelectedTriangles().Count > 0;
        
        // Create Selected Islands Module
        if (GUILayout.Button(LocalizationEditor.GetLocalizedText("CreateModuleButton")))
        {
            CreateModule(_triangleSelectionManager.GetSelectedTriangles());
            MeshPreview.StopPreview();
            MeshPreview.StartPreview(_originskinnedMeshRenderer);
            //Close();
        }

        GUI.enabled = true;
    }

    private static void process_advanced_options()
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