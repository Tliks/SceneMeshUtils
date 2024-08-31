using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace com.aoyon.scenemeshutils
{
    public class MenuItems : EditorWindow
    {
        private const int MENU_PRIORITY = 49;
        private static int count = 0;

        private const string MCPATH = "GameObject/Module Creator";

        private const string CREATEMODULE = "Create Module"; 

        [MenuItem(MCPATH + "/" + CREATEMODULE, true, MENU_PRIORITY)]
        static bool CreateModuleValidation()
        {
            return Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>() != null;
        }

        [MenuItem(MCPATH + "/" + CREATEMODULE, false, MENU_PRIORITY)]
        static void CreateModule()
        {
            count++; 
            if (count != Selection.gameObjects.Count()) return;

            foreach (var obj in Selection.gameObjects)
            {
                ModuleCreatorProcessor.CheckAndCopyBones(new List<GameObject> {obj}, new ModuleCreatorSettings());
            }

            count = 0;
        }
        
        private const string CREATEMODULEMERGED = "Create Module (Merged)";

        [MenuItem(MCPATH + "/" + CREATEMODULEMERGED, true, MENU_PRIORITY)]
        static bool CreateModuleMergedValidation()
        {
            if (Selection.gameObjects.Length < 2) return false;
            foreach (var obj in Selection.gameObjects)
            {
                if (obj.GetComponent<SkinnedMeshRenderer>() == null)
                {
                    return false;
                }
            }
            return true;
        }

        [MenuItem(MCPATH + "/" + CREATEMODULEMERGED, false, MENU_PRIORITY)]
        static void CreateModuleMerged()
        {   
            count++; 
            if (count != Selection.gameObjects.Count()) return;

            ModuleCreatorProcessor.CheckAndCopyBones(Selection.gameObjects, new ModuleCreatorSettings());
            
            count = 0;
        }
        
        private const string CREATEMODULETR= "Create Module with Triangle Selector";

        [MenuItem(MCPATH + "/" + CREATEMODULETR, true, MENU_PRIORITY)]
        static bool CreateModuleTRValidation()
        {
            if (Selection.gameObjects.Length >= 2) return false;
            return Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>() != null;
        }

        [MenuItem(MCPATH + "/" + CREATEMODULETR, false, MENU_PRIORITY)]
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
