using UnityEngine;
using VRC.SDKBase;
using System.Collections.Generic;
using System;

namespace com.aoyon.modulecreator
{

    [RequireComponent(typeof(SkinnedMeshRenderer))]
    public class AddShrinkBlendShape: MonoBehaviour, IEditorOnly
    {
        public TriangleSelection triangleSelection;
    }
}