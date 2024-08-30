using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using nadena.dev.ndmf.preview;
using UnityEditor;
using UnityEngine;

namespace com.aoyon.scenemeshutils
{
    internal class AddShrinkBlendShapePreview : IRenderFilter
    {
        public static TogglablePreviewNode ToggleNode = TogglablePreviewNode.Create(
            () => "Add Shrink BlendShape Preview",
            qualifiedName: "com.aoyon.scenemeshutils/AddShrinkBlendShapePreview",
            true
        );
        
        public IEnumerable<TogglablePreviewNode> GetPreviewControlNodes()
        {
            yield return ToggleNode;
        }

        public bool IsEnabled(ComputeContext context)
        {
            return context.Observe(ToggleNode.IsEnabled);
        }

        public ImmutableList<RenderGroup> GetTargetGroups(ComputeContext context)
        {
            return context.GetComponentsByType<AddShrinkBlendShape>()
            .Select(c => RenderGroup.For(context.GetComponent<SkinnedMeshRenderer>(c.gameObject)))
            .ToImmutableList();
        }

        public Task<IRenderFilterNode> Instantiate(RenderGroup group, IEnumerable<(Renderer, Renderer)> proxyPairs, ComputeContext context)
        {
            var addShrinkBlendShape = group.Renderers.First().GetComponent<AddShrinkBlendShape>();
            var pair = proxyPairs.First();
            var targetRenderer = pair.Item2;
            Mesh mesh = null;

            switch (targetRenderer)
            {
                case SkinnedMeshRenderer smr: { mesh = smr.sharedMesh; break; }
                default: { break; }
            }

            if (mesh == null) {return null;}
            context.Observe(addShrinkBlendShape);

            Mesh modifiedMesh = ShrinkBlendShapeUtility.GenerateShrinkBlendShape(mesh, addShrinkBlendShape.triangleSelection.ToHashSet());
            return Task.FromResult<IRenderFilterNode>(new AddShrinkBlendShapePreviewNode(modifiedMesh));

        }
    }

    internal class AddShrinkBlendShapePreviewNode : IRenderFilterNode
    {
        public RenderAspects WhatChanged => RenderAspects.Everything;
        private Mesh _modifiedMesh; 

        public AddShrinkBlendShapePreviewNode(Mesh mesh)
        {
            _modifiedMesh = mesh;
        }

        public void OnFrame(Renderer original, Renderer proxy)
        {
            if (original is SkinnedMeshRenderer o_smr && proxy is SkinnedMeshRenderer p_smr)
            {
                p_smr.sharedMesh = _modifiedMesh;
            }
        }

    }
}
