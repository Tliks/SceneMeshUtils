using UnityEditor;
using UnityEngine;

namespace com.aoyon.scenemeshutils
{
    public class MenuItems : EditorWindow
    {
        private const int MENU_PRIORITY = 49;

        [MenuItem("GameObject/SceneMeshUtils/Create Mask texture", true, MENU_PRIORITY)]
        static bool CreateMaskTextureValidation()
        {
            return Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>() != null;
        }

        [MenuItem("GameObject/SceneMeshUtils/Create Mask texture", false, MENU_PRIORITY)]
        static void CreateMaskTexture()
        {
            SkinnedMeshRenderer skinnedMeshRenderer = Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>();
            MaskTextureGenerator.ShowWindow(skinnedMeshRenderer);
        }
        
    }

}
