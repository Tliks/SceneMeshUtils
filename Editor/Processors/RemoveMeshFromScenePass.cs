using UnityEngine;
using nadena.dev.ndmf;
using System.Linq;
using System.Collections.Generic;

namespace com.aoyon.scenemeshutils
{
    public class RemoveMeshFromScenePass : Pass<RemoveMeshFromScenePass>
    {
        protected override void Execute(BuildContext context)
        {
            var comonent = context.AvatarRootObject.GetComponentInChildren<RemoveMeshFromScene>();

            TriangleSelection triangleSelection = comonent.triangleSelection;

            if (triangleSelection == null || triangleSelection.selection == null || triangleSelection.selection.Count > 0)
            {
                Object.DestroyImmediate(comonent);
                return;
            }

            SkinnedMeshRenderer skinnedMeshRenderer = comonent.GetComponent<SkinnedMeshRenderer>();

            Mesh newMesh = MeshUtility.DeleteMesh(skinnedMeshRenderer.sharedMesh, triangleSelection.selection.ToHashSet());

            skinnedMeshRenderer.sharedMesh = newMesh;

            Object.DestroyImmediate(comonent);
        }
    }
}
            
