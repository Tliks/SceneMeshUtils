using UnityEngine;
using UnityEditor;

namespace com.aoyon.scenemeshutils
{
    [CustomEditor(typeof(AddShrinkBlendShape))]
    public class AddShrinkBlendShapeEditor: Editor
    {
        private RenderSelector _renderSelector;

        private void OnEnable()
        {
            SkinnedMeshRenderer skinnedMeshRenderer = (target as AddShrinkBlendShape).GetComponent<SkinnedMeshRenderer>();
            TriangleSelection targetselection = (target as AddShrinkBlendShape).triangleSelection;
            _renderSelector = CreateInstance<RenderSelector>();
            RenderSelectorContext ctx = new()
            {
                isblendhsape = true,
            };
            _renderSelector.Initialize(skinnedMeshRenderer, ctx, targetselection);
        }

        private void OnDisable()
        {
            _renderSelector.Dispose();
        }

        public override void OnInspectorGUI()
        {
            _renderSelector.RenderGUI();
        }

    }
}