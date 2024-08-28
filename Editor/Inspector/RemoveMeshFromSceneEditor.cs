using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace com.aoyon.scenemeshutils
{
    [CustomEditor(typeof(RemoveMeshFromScene))]
    public class RemoveMeshFromSceneEditor: Editor
    {
        private RenderSelector _renderSelector;

        private void OnEnable()
        {
            SkinnedMeshRenderer skinnedMeshRenderer = (target as RemoveMeshFromScene).GetComponent<SkinnedMeshRenderer>();
            SerializedProperty targetselection = serializedObject.FindProperty(nameof(RemoveMeshFromScene.triangleSelection));
            _renderSelector = CreateInstance<RenderSelector>();
            RenderSelectorContext ctx = new();
            ctx.isKeep = false;
            _renderSelector.Initialize(skinnedMeshRenderer, ctx, targetselection);
        }

        private void OnDisable()
        {
            _renderSelector.Dispose();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            _renderSelector.RenderGUI();
            EditorGUILayout.HelpBox(LocalizationEditor.GetLocalizedText("Utility.DeleteMesh.description"), MessageType.Info);
            serializedObject.ApplyModifiedProperties();
        }

    }
}