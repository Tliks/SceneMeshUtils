using UnityEditor;
using UnityEngine;

public static class DeleteMeshUtilty
{
    private static SkinnedMeshRenderer _originskinnedMeshRenderer;
    private static string _rootname;
    private static Mesh _originalMesh;
    private static TriangleSelectionManager _triangleSelectionManager;

    public static void Initialize(SkinnedMeshRenderer originskinnedMeshRenderer, string rootname, Mesh originalMesh, TriangleSelectionManager triangleSelectionManager)
    {
        _originskinnedMeshRenderer = originskinnedMeshRenderer;
        _rootname = rootname;
        _originalMesh = originalMesh;
        _triangleSelectionManager = triangleSelectionManager;
    }

    private static void DeleteMesh()
    {   
        Mesh newMesh = MeshUtility.DeleteMesh(_originalMesh, _triangleSelectionManager.GetSelectedTriangles());

        string path = AssetPathUtility.GenerateMeshPath(_rootname, "PartialMesh");
        AssetDatabase.CreateAsset(newMesh, path);
        AssetDatabase.SaveAssets();

        _originskinnedMeshRenderer.sharedMesh = newMesh;
    }

    public static void RenderDeleteMesh()
    {
        EditorGUILayout.Space();
        GUI.enabled = _triangleSelectionManager.GetSelectedTriangles().Count > 0;
        if (GUILayout.Button(LocalizationEditor.GetLocalizedText("Utility.DeleteMesh")))
        {
            MeshPreview.StopPreview();
            DeleteMesh();
            MeshPreview.StartPreview(_originskinnedMeshRenderer);
        }
        GUI.enabled = true;
    }
}
