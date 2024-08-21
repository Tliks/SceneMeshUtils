using UnityEngine;
using VRC.SDKBase;
using System.Collections.Generic;
using System;

namespace com.aoyon.modulecreator
{

    [AddComponentMenu("SceneMeshUtils/Remove Mesh From Scene")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public class RemoveMeshFromScene: MonoBehaviour, IEditorOnly
    {
        public TriangleSelection triangleSelection;
    }
}