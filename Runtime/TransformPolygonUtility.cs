using System.Collections.Generic;
using UnityEngine;

namespace com.aoyon.modulecreator.runtime
{
    public class TransformPolygonUtility : MonoBehaviour
    {
        public SkinnedMeshRenderer origSkinnedMeshRenderer;
        public string rootname;
        public Mesh originalMesh;
        public HashSet<int> triangleIndices;

        public Vector3 position = Vector3.zero;
        public Vector3 rotation = Vector3.zero;
        public Vector3 scale = Vector3.one;
        public Vector3 centroid = Vector3.zero;
        
        public void Initialize(SkinnedMeshRenderer origSkinnedMeshRenderer, string rootname, Mesh originalMesh, HashSet<int> triangleIndices)
        {
            this.origSkinnedMeshRenderer = origSkinnedMeshRenderer;
            this.rootname = rootname;
            this.originalMesh = originalMesh;
            this.triangleIndices = triangleIndices;
        }
    }
    
}
