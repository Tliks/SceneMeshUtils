using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

namespace com.aoyon.modulecreator
{
    [CustomEditor(typeof(RemoveMeshFromScene))]
    public class RemoveMeshFromSceneEditor: Editor
    {
        private SkinnedMeshRenderer _skinnedMeshRenderer;
        private Mesh _mesh;
        private TriangleSelectionContainer _triangleSelectionContainer;
        private List<TriangleSelection> _triangleSelections;
        private TriangleSelection _triangleSelection;
        private string[] _displayedOptions;
        private int _selectedIndex = 0;
        private TriangleSelectorContext _context;

        private bool _isAutoPreview = true;

        private void OnEnable()
        {
            _skinnedMeshRenderer = (target as RemoveMeshFromScene).GetComponent<SkinnedMeshRenderer>();
            _mesh = _skinnedMeshRenderer.sharedMesh;
            LoadAsset();
            _triangleSelection = (target as RemoveMeshFromScene).triangleSelection;
            _selectedIndex = FindIndex(_triangleSelections, _triangleSelection) + 1;
            _context = CreateInstance<TriangleSelectorContext>();
            StartPreview();
        }

        private void OnDisable()
        {
            CustomAnimationMode.StopAnimationMode();
        }

        private void LoadAsset()
        {
            _triangleSelectionContainer = SaveAsScriptableObject.GetContainer(_mesh);
            _triangleSelections = _triangleSelectionContainer.selections;
            _displayedOptions = _triangleSelections.Select(ts => ts.selection.Count().ToString()).ToArray();
            _displayedOptions = new[] { "None" }.Concat(_displayedOptions).ToArray();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            int selectedIndex = EditorGUILayout.Popup("Triangle Selection", _selectedIndex, _displayedOptions);
            if (selectedIndex != _selectedIndex)
            {   
                _selectedIndex = selectedIndex;
                if (_selectedIndex == 0)
                {
                    _triangleSelection.selection = new();
                    StopPrview();
                }
                else
                {
                    _triangleSelection.selection = new List<int>(_triangleSelections[_selectedIndex - 1].selection);
                    StartPreview();
                }
            }
            
            if (GUILayout.Button("Open Triangle Selector"))
            {
                StopPrview();
                TriangleSelector.ShowWindow(_context, _skinnedMeshRenderer);
            }
            
            List<int> newSelection = _context.selectedTriangleIndices;
            if (newSelection != null && newSelection.Count > 0)
            {
                //Debug.Log("update");
                _context.selectedTriangleIndices = new List<int>();
                TriangleSelection newTriangleSelection = new TriangleSelection { selection = newSelection };

                SaveAsScriptableObject.UpdateData(_triangleSelectionContainer, newTriangleSelection);

                LoadAsset();
                _triangleSelection.selection = new List<int>(newSelection);
                _selectedIndex = FindIndex(_triangleSelections, _triangleSelection) + 1;
                StartPreview();
            }

            if (GUILayout.Button(_isAutoPreview ? "Disable Auto Preview" : "Enable Auto Preview"))
            {
                ToggleAutoPreview();
            }

            /*
            string label = (target as RemoveMeshFromScene).triangleSelection != null ? (target as RemoveMeshFromScene).triangleSelection.selection.Count.ToString() : "なくない?";
            GUILayout.Label(label);
            */
        }

        private void StartPreview()
        {
            if (_isAutoPreview && _triangleSelection != null && _triangleSelection.selection.Count > 0)
            {
                CustomAnimationMode.StopAnimationMode();
                CustomAnimationMode.StartAnimationMode(_skinnedMeshRenderer);
                _skinnedMeshRenderer.sharedMesh = MeshUtility.RemoveTriangles(_mesh, _triangleSelection.selection.ToHashSet());
            }
        }

        private void StopPrview()
        {
            CustomAnimationMode.StopAnimationMode();
        }

        private void ToggleAutoPreview()
        {
            if ( !_isAutoPreview)
            {
                _isAutoPreview = true;
                StartPreview();
            }
            else
            {
                _isAutoPreview = false;
                StopPrview();
            }
        }

        private int FindIndex(List<TriangleSelection> triangleSelections, TriangleSelection triangleSelection)
        {
            int index = triangleSelections.FindIndex(ts => ts.selection.SequenceEqual(triangleSelection.selection));
            return index;
        }
    }
}