using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DeleteMeshUtilty
{
    private readonly SkinnedMeshRenderer _OriginskinnedMeshRenderer;
    private readonly string _rootname;
    private readonly HashSet<int> _KeepTriangles;
    private readonly Mesh _originalMesh;

    public DeleteMeshUtilty(SkinnedMeshRenderer _OriginskinnedMeshRenderer, string _rootname, HashSet<int> KeepTriangles, Mesh _originalMesh)
    {
        this._OriginskinnedMeshRenderer = _OriginskinnedMeshRenderer;
        this._rootname = _rootname;
        this._KeepTriangles = KeepTriangles;
        this._originalMesh = _originalMesh;
    }

    private void DeleteMesh()
    {
        Mesh newMesh = MeshUtility.DeleteMesh(_originalMesh, _KeepTriangles);

        string path = AssetPathUtility.GenerateMeshPath(_rootname, "PartialMesh");
        AssetDatabase.CreateAsset(newMesh, path);
        AssetDatabase.SaveAssets();

        _OriginskinnedMeshRenderer.sharedMesh = newMesh;
    }

    public void RenderDeleteMesh()
    {
        EditorGUILayout.Space();
        GUI.enabled = _KeepTriangles.Count > 0;
        if (GUILayout.Button(LocalizationEditor.GetLocalizedText("Utility.DeleteMesh")))
        {
            MeshPreview.StopPreview();
            DeleteMesh();
            MeshPreview.StartPreview(_OriginskinnedMeshRenderer);
        }
        GUI.enabled = true;
    }
}
