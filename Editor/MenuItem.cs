using UnityEditor;
using UnityEngine;

namespace com.aoyon.scenemeshutils
{
    public class MenuItems : EditorWindow
    {
        private const int MENU_PRIORITY = 49;

         [MenuItem("GameObject/Module Creator/Create Module", true, MENU_PRIORITY)]
        static bool CreateModuleValidation()
        {
            return Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>() != null;
        }

        [MenuItem("GameObject/Module Creator/Create Module", false, MENU_PRIORITY)]
        static void CreateModule()
        {
            SkinnedMeshRenderer skinnedMeshRenderer = Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>();
            ModuleCreatorProcessor.CheckAndCopyBones(skinnedMeshRenderer.gameObject, new ModuleCreatorSettings());
        }

         [MenuItem("GameObject/Module Creator/Create Module with Triangle Selector", true, MENU_PRIORITY)]
        static bool CreateModuleTRValidation()
        {
            return Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>() != null;
        }

        [MenuItem("GameObject/Module Creator/Create Module with Triangle Selector", false, MENU_PRIORITY)]
        static void CreateModuleTR()
        {
            SkinnedMeshRenderer skinnedMeshRenderer = Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>();
            ModuleCreator.ShowWindow(skinnedMeshRenderer);
        }


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
    

    /*
    public class EditorWindowA : EditorWindow
    {
        public SkinnedMeshRenderer SkinnedMeshRenderer;
        private TriangleSelectorContext context;

        private void OnEnable()
        {
            context = ScriptableObject.CreateInstance<TriangleSelectorContext>();
        }

        private void OnGUI()
        {
            GUILayout.Label("Selected Triangle Indices Count: " + context.selectedTriangleIndices.Count.ToString()); 

            if (GUILayout.Button("Open Triangle Selector"))
            {
                TriangleSelector window = GetWindow<TriangleSelector>();
                context.SkinnedMeshRenderer = SkinnedMeshRenderer;
                window.Initialize(context);
                window.Show();
            }
        }
    }
    */


}
