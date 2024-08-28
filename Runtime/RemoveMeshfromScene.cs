using UnityEngine;
using VRC.SDKBase;
using System.Collections.Generic;
using System;

namespace com.aoyon.scenemeshutils
{

    [AddComponentMenu("SceneMeshUtils/SMU Remove Mesh From Scene")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public class RemoveMeshFromScene: MonoBehaviour, IEditorOnly
    {
        public List<int> triangleSelection = new();
    }
}