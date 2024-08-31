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
            var comonents = context.AvatarRootObject.GetComponentsInChildren<RemoveMeshFromScene>(true);

            foreach (var comonent in comonents)
            {
                List<int> triangleSelection = comonent.triangleSelection;

                if (triangleSelection == null || triangleSelection.Count == 0)
                {
                    Object.DestroyImmediate(comonent);
                    return;
                }

                SkinnedMeshRenderer skinnedMeshRenderer = comonent.GetComponent<SkinnedMeshRenderer>();

                Mesh newMesh = MeshUtility.DeleteMesh(skinnedMeshRenderer.sharedMesh, triangleSelection.ToHashSet());

                skinnedMeshRenderer.sharedMesh = newMesh;

                Object.DestroyImmediate(comonent);
            }
        }
    }
}
            
