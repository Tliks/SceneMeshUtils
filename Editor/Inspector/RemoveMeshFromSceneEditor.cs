using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using nadena.dev.ndmf.preview;

namespace com.aoyon.scenemeshutils
{
    [CustomEditor(typeof(RemoveMeshFromScene))]
    public class RemoveMeshFromSceneEditor: Editor
    {
        private RemoveMeshFromScene _target;
        private RenderSelector _renderSelector;

        private void OnEnable()
        {
            _target = target as RemoveMeshFromScene;
            var skinnedMeshRenderer = _target.GetComponent<SkinnedMeshRenderer>();
            _renderSelector = CreateInstance<RenderSelector>();
            _renderSelector.Initialize(skinnedMeshRenderer, _target.triangleSelection);
            _renderSelector.RegisterApplyCallback(OnTriangleSelectionChanged);
        }

        private void OnDisable()
        {
            _renderSelector.Dispose();
        }

        private void OnTriangleSelectionChanged(List<int> newSelection)
        {
            _target.triangleSelection = newSelection;
            ChangeNotifier.NotifyObjectUpdate(_target);
        }

        public override void OnInspectorGUI()
        {
            _renderSelector.RenderGUI();
            NDMFToggleButton.RenderNDMFToggle(RemoveMeshFromScenePreview.ToggleNode);
            //EditorGUILayout.HelpBox(LocalizationEditor.GetLocalizedText("Utility.DeleteMesh.description"), MessageType.Info);
        }
    }
}