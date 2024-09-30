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
            .Where(pair => pair.Item2 != null && pair.Item2.sharedMesh != null)
            .Select(pair => RenderGroup.For(pair.Item2).WithData(new[] { pair.Item1 }))
            .ToImmutableList();
        }

        public Task<IRenderFilterNode> Instantiate(RenderGroup group, IEnumerable<(Renderer, Renderer)> proxyPairs, ComputeContext context)
        {
            var node = new AddShrinkBlendShapePreviewNode();
            if (!node.Initialize(group, proxyPairs, context)) return null;
            return Task.FromResult<IRenderFilterNode>(node);
        }
    }

    internal class AddShrinkBlendShapePreviewNode : IRenderFilterNode
    {
        public RenderAspects WhatChanged => RenderAspects.Mesh;
        private Mesh _modifiedMesh; 
        private AddShrinkBlendShape _component;

        public bool Initialize(RenderGroup group, IEnumerable<(Renderer, Renderer)> proxyPairs, ComputeContext context)
        {
            _component = group.GetData<AddShrinkBlendShape[]>().SingleOrDefault();
            if (_component == default) 
            {
                return false;
            }

            SkinnedMeshRenderer proxy = GetProxy(proxyPairs);
            if (proxy == null)
            {
                return false;
            } 

            context.Observe(_component);
            _modifiedMesh = ShrinkBlendShape.GenerateShrinkBlendShape(proxy.sharedMesh, _component.triangleSelection.ToHashSet());

            return true;
        }

        public Task<IRenderFilterNode> Refresh(IEnumerable<(Renderer, Renderer)> proxyPairs, ComputeContext context, RenderAspects updatedAspects)
        {              
            if ((updatedAspects & RenderAspects.Mesh) == 0 && updatedAspects != 0)
            {
                return Task.FromResult<IRenderFilterNode>(null);
            }

            if (_component == null) 
            {
                return Task.FromResult<IRenderFilterNode>(null);
            }

            SkinnedMeshRenderer proxy = GetProxy(proxyPairs);
            if (proxy == null) 
            {
                return Task.FromResult<IRenderFilterNode>(null);
            }

            context.Observe(_component);
            _modifiedMesh = ShrinkBlendShape.GenerateShrinkBlendShape(proxy.sharedMesh, _component.triangleSelection.ToHashSet());

            return Task.FromResult<IRenderFilterNode>(this);
        }

        private static SkinnedMeshRenderer GetProxy(IEnumerable<(Renderer, Renderer)> proxyPairs)
        {
            var pair = proxyPairs.SingleOrDefault();
            if (pair == default) return null;

            if (!(pair.Item1 is SkinnedMeshRenderer original)) return null;
            if (!(pair.Item2 is SkinnedMeshRenderer proxy)) return null;

            if (proxy.sharedMesh == null) return null;

            return proxy;
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
