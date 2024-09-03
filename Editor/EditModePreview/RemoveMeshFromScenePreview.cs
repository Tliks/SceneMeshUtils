using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using nadena.dev.ndmf.preview;
using UnityEditor;
using UnityEngine;

namespace com.aoyon.scenemeshutils
{
    internal class RemoveMeshFromScenePreview : IRenderFilter
    {
        public static TogglablePreviewNode ToggleNode = TogglablePreviewNode.Create(
            () => "Remove Mesh From Scene",
            qualifiedName: "com.aoyon.scenemeshutils/RemoveMeshFromScenePreview",
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
            return context.GetComponentsByType<RemoveMeshFromScene>()
            .Select(c => (c, context.GetComponent<SkinnedMeshRenderer>(c.gameObject)))
            .Where(p => p.Item2 != null && p.Item2.sharedMesh != null)
            .Select(p => RenderGroup.For(p.Item2).WithData<RemoveMeshFromScene[]>(new[] { p.Item1 }))
            .ToImmutableList();
        }

        public Task<IRenderFilterNode> Instantiate(RenderGroup group, IEnumerable<(Renderer, Renderer)> proxyPairs, ComputeContext context)
        {
            var component = group.GetData<RemoveMeshFromScene[]>().SingleOrDefault();
            if (component == default) return null;

            context.Observe(component);

            var pair = proxyPairs.SingleOrDefault();
            if (pair == default) return null;

            if (!(pair.Item1 is SkinnedMeshRenderer original)) return null;
            if (!(pair.Item2 is SkinnedMeshRenderer proxy)) return null;

            Mesh mesh = proxy.sharedMesh;
            if (mesh == null) return null;

            Mesh modifiedMesh = MeshUtility.DeleteMesh(mesh, component.triangleSelection.ToHashSet());
            return Task.FromResult<IRenderFilterNode>(new RemoveMeshFromScenePreviewNode(modifiedMesh));
        }
    }

    internal class RemoveMeshFromScenePreviewNode : IRenderFilterNode
    {
        public RenderAspects WhatChanged => RenderAspects.Mesh;
        private Mesh _modifiedMesh; 

        public RemoveMeshFromScenePreviewNode(Mesh mesh)
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
