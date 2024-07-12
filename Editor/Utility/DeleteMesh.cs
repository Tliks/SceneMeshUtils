using UnityEditor;
using UnityEngine;

public class DeleteMeshUtilty
{
    private readonly SkinnedMeshRenderer _OriginskinnedMeshRenderer;
    private readonly string _rootname;
    private readonly Mesh _originalMesh;
    private TriangleSelectionManager _triangleSelectionManager;

    public DeleteMeshUtilty(SkinnedMeshRenderer _OriginskinnedMeshRenderer, string _rootname, Mesh _originalMesh, TriangleSelectionManager _triangleSelectionManager)
    {
        this._OriginskinnedMeshRenderer = _OriginskinnedMeshRenderer;
        this._rootname = _rootname;
        this._originalMesh = _originalMesh;
        this._triangleSelectionManager = _triangleSelectionManager;
    }

    private void DeleteMesh()
    {   
        Mesh newMesh = MeshUtility.DeleteMesh(_originalMesh, _triangleSelectionManager.GetSelectedTriangles());
        
        string path = AssetPathUtility.GenerateMeshPath(_rootname, "PartialMesh");
        AssetDatabase.CreateAsset(newMesh, path);
        AssetDatabase.SaveAssets();

        _OriginskinnedMeshRenderer.sharedMesh = newMesh;
    }

    public void RenderDeleteMesh()
    {
        EditorGUILayout.Space();
        GUI.enabled = _triangleSelectionManager.GetSelectedTriangles().Count > 0;
        if (GUILayout.Button(LocalizationEditor.GetLocalizedText("Utility.DeleteMesh")))
        {
            MeshPreview.StopPreview();
            DeleteMesh();
            MeshPreview.StartPreview(_OriginskinnedMeshRenderer);
        }
        GUI.enabled = true;
    }
}
