using UnityEngine;
using nadena.dev.ndmf;
using System.Linq;

namespace com.aoyon.modulecreator
{
    public class RemoveMeshFromScenePass : Pass<RemoveMeshFromScenePass>
    {
        protected override void Execute(BuildContext context)
        {
            var comonent = context.AvatarRootObject.GetComponentInChildren<RemoveMeshFromScene>();

            TriangleSelection triangleSelection = comonent.triangleSelection;

            SkinnedMeshRenderer skinnedMeshRenderer = comonent.GetComponent<SkinnedMeshRenderer>();

            Mesh newMesh = MeshUtility.DeleteMesh(skinnedMeshRenderer.sharedMesh, triangleSelection.selection.ToHashSet());

            skinnedMeshRenderer.sharedMesh = newMesh;

            Object.DestroyImmediate(comonent);
        }
    }
}
            
