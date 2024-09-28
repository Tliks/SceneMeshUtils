using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using nadena.dev.ndmf.preview;

namespace com.aoyon.scenemeshutils
{
    [CustomEditor(typeof(AddShrinkBlendShape))]
    public class AddShrinkBlendShapeEditor: Editor
    {
        private AddShrinkBlendShape _target;
        private RenderSelector _renderSelector;

        private void OnEnable()
        {
            _target = target as AddShrinkBlendShape;
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
            NDMFToggleButton.RenderNDMFToggle(AddShrinkBlendShapePreview.ToggleNode);
            //EditorGUILayout.HelpBox(LocalizationEditor.GetLocalizedText("Utility.BlendShape.description"), MessageType.Info);
        }
    }
}