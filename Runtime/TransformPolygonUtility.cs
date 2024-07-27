using System.Collections.Generic;
using UnityEngine;

namespace com.aoyon.modulecreator
{
    public class TransformPolygonUtility : MonoBehaviour
    {
        [HideInInspector]
        public SkinnedMeshRenderer origSkinnedMeshRenderer;
        [HideInInspector]
        public string rootname;
        [HideInInspector]
        public Mesh originalMesh;
        [HideInInspector]
        public HashSet<int> triangleIndices;

        public Vector3 position = Vector3.zero;
        public Vector3 rotation = Vector3.zero;
        public Vector3 scale = Vector3.one;
        [HideInInspector]
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
