using UnityEditor;
using UnityEngine;

namespace com.aoyon.modulecreator
{
    public class ModuleCreatorWindow : EditorWindow
    {
        private const int MENU_PRIORITY = 49;

        [MenuItem("GameObject/AAU/Create Module", false, MENU_PRIORITY)]
        public static void ShowWindow()
        {
            ModuleCreatorIsland window = (ModuleCreatorIsland)GetWindow(typeof(ModuleCreatorIsland));
            window.Show();
        }

        [MenuItem("GameObject/AAU/Create Module", true, MENU_PRIORITY)]
        private static bool ShowWindowValidation()
        {
            return Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>() != null;
        }        
    }

}
