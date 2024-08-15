using UnityEditor;
using UnityEngine;

namespace com.aoyon.modulecreator
{
    public static class DeleteMeshUtilty
    {
        private static SkinnedMeshRenderer _originskinnedMeshRenderer;
        private static string _rootname;
        private static TriangleSelectionManager _triangleSelectionManager;

        public static void Initialize(SkinnedMeshRenderer originskinnedMeshRenderer, TriangleSelectionManager triangleSelectionManager)
        {
            _originskinnedMeshRenderer = originskinnedMeshRenderer;
            _rootname = CheckUtility.CheckRoot(originskinnedMeshRenderer.gameObject).name;
            _triangleSelectionManager = triangleSelectionManager;
        }

        private static void DeleteMesh()
        {   
            Mesh newMesh = MeshUtility.DeleteMesh(_originskinnedMeshRenderer.sharedMesh, _triangleSelectionManager.GetSelectedTriangles());

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
                PreviewController.StopAnimationMode();
                DeleteMesh();
                PreviewController.StartAnimationMode(_originskinnedMeshRenderer);
            }
            GUI.enabled = true;
        }
    }
}