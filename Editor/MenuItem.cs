using UnityEditor;
using UnityEngine;

namespace com.aoyon.modulecreator
{
    public class MenuItems : EditorWindow
    {
        private const int MENU_PRIORITY = 49;

        /*
        [MenuItem("GameObject/AAU/Create Module", false, MENU_PRIORITY)]
        public static void ShowWindow()
        {
            TriangleSelector window = GetWindow(typeof(TriangleSelector));
            TriangleSelectorContext triangleSelectorContext = new();
            window.Initialize(triangleSelectorContext);
            window.Show();
        }
        */

        [MenuItem("GameObject/Show Triangle Selector Context", false, MENU_PRIORITY)]
        public static void ShowTriangleSelectorContextWindow()
        {
            EditorWindowA window = GetWindow<EditorWindowA>();
            window.SkinnedMeshRenderer = Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>();
            window.Show();
        }
    }

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

}
