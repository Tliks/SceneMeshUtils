using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;

namespace com.aoyon.modulecreator
{
    public class SaveAsScriptableObject
    {
        private const string SAVE_PATH = "Assets/TriangleSelection";

        public static void UpdateData(TriangleSelectionContainer triangleSelection, TriangleSelection newSelection)
        {
            if (!triangleSelection.selections.Contains(newSelection))
            {
                triangleSelection.selections.Add(newSelection);
                EditorUtility.SetDirty(triangleSelection);
                AssetDatabase.SaveAssets();
                //Debug.Log("追加");
            }
        }

        public static TriangleSelectionContainer GetContainer(Mesh mesh)
        {
            string[] guids = AssetDatabase.FindAssets("t:TriangleSelectionContainer", new[] { SAVE_PATH });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                TriangleSelectionContainer selection = AssetDatabase.LoadAssetAtPath<TriangleSelectionContainer>(assetPath);
                if (selection != null && selection.mesh == mesh)
                {
                    //Debug.Log("既存のTriangleSelectionを取得");
                    //Debug.Log(selection.mesh.name);
                    //Debug.Log(selection.selections.Count);
                    return selection;
                }
            }

            //Debug.Log("存在しない場合は新規作成");
            return CreateAsset(mesh);
        }

        private static TriangleSelectionContainer CreateAsset(Mesh mesh)
        {
            var instance = ScriptableObject.CreateInstance<TriangleSelectionContainer>();
            instance.mesh = mesh;

            if (!Directory.Exists(SAVE_PATH)) Directory.CreateDirectory(SAVE_PATH);
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath($"{SAVE_PATH}/{mesh.name}.asset");
            AssetDatabase.CreateAsset(instance, uniquePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var loadedInstance = (TriangleSelectionContainer)AssetDatabase.LoadAssetAtPath(uniquePath, typeof(TriangleSelectionContainer));
            return loadedInstance;
        }
    }
}
