/*
MIT License

Copyright (c) 2022 anatawa12

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using UnityEngine;
using UnityEditor;

public class MeshRestorer
{
    private SkinnedMeshRenderer _targetRenderer;
    private Mesh _originalMesh;
    private bool _isInAnimationMode;

    public MeshRestorer(SkinnedMeshRenderer renderer)
    {
        _targetRenderer = renderer;
        _originalMesh = renderer.sharedMesh;
        _isInAnimationMode = false;
    }

    public void RestoreOriginalMesh()
    {
        if (!_isInAnimationMode)
        {
            AnimationMode.StartAnimationMode();
            _isInAnimationMode = true;
        }

        try
        {
            AnimationMode.BeginSampling();

            var binding = EditorCurveBinding.PPtrCurve("", typeof(SkinnedMeshRenderer), "m_Mesh");
            var modification = new PropertyModification
            {
                target = _targetRenderer,
                propertyPath = "m_Mesh",
                objectReference = _originalMesh
            };

            AnimationMode.AddPropertyModification(binding, modification, true);

            _targetRenderer.sharedMesh = _originalMesh;
        }
        finally
        {
            AnimationMode.EndSampling();
        }
    }

    public void StopRestoring()
    {
        if (_isInAnimationMode)
        {
            AnimationMode.StopAnimationMode();
            _isInAnimationMode = false;
        }
    }
}
