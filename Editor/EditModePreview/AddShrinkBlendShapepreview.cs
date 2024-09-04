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
            .Select(component => (component, context.GetComponent<SkinnedMeshRenderer>(component.gameObject)))
            .Where(((component, renderer) => renderer != null && renderer.sharedMesh != null))
            .Select((component, renderer) => RenderGroup.For(renderer).WithData(new[] { component }))
            .ToImmutableList();
        }

        public Task<IRenderFilterNode> Instantiate(RenderGroup group, IEnumerable<(Renderer, Renderer)> proxyPairs, ComputeContext context)
        {
            var component = group.GetData<AddShrinkBlendShape[]>().SingleOrDefault();
            if (component == default) return null;

            context.Observe(component);

            var pair = proxyPairs.SingleOrDefault();
            if (pair == default) return null;

            if (!(pair.Item1 is SkinnedMeshRenderer original)) return null;
            if (!(pair.Item2 is SkinnedMeshRenderer proxy)) return null;

            Mesh mesh = proxy.sharedMesh;
            if (mesh == null) return null;

            Mesh modifiedMesh = ShrinkBlendShapeUtility.GenerateShrinkBlendShape(mesh, component.triangleSelection.ToHashSet());
            return Task.FromResult<IRenderFilterNode>(new AddShrinkBlendShapePreviewNode(modifiedMesh));
        }
    }

    internal class AddShrinkBlendShapePreviewNode : IRenderFilterNode
    {
        public RenderAspects WhatChanged => RenderAspects.Mesh;
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
                return;
            }
        }

    }
}
