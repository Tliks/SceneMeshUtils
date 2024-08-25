using UnityEngine;
using UnityEditor;

namespace com.aoyon.scenemeshutils
{
    [CustomEditor(typeof(RemoveMeshFromScene))]
    public class RemoveMeshFromSceneEditor: Editor
    {
        private RenderSelector _renderSelector;

        private void OnEnable()
        {
            SkinnedMeshRenderer skinnedMeshRenderer = (target as RemoveMeshFromScene).GetComponent<SkinnedMeshRenderer>();
            TriangleSelection targetselection = (target as RemoveMeshFromScene).triangleSelection;
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
            _renderSelector.RenderGUI();
        }

    }
}