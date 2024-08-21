using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;

namespace com.aoyon.modulecreator
{

    public class RenderSelectorContext
    {
        public bool isKeep = true;
        public bool isRenderToggle = true;
        public bool FixedPreview = true;
        public bool isblendhsape = false;
    }

    public class RenderSelector : Editor
    {
        private SkinnedMeshRenderer _skinnedMeshRenderer;
        private Mesh _mesh;
        private TriangleSelectionContainer _triangleSelectionContainer;
        private List<TriangleSelection> _triangleSelections;
        private TriangleSelection _target;
        private string[] _displayedOptions;
        private int _selectedIndex = 0;
        private TriangleSelectorContext _selectorcontext;
        private RenderSelectorContext _renderctx;

        private bool _isAutoPreview = true;

        public void Initialize(SkinnedMeshRenderer skinnedMeshRenderer, TriangleSelection target)
        {
            Initialize(skinnedMeshRenderer, new RenderSelectorContext(), target);
        }

        public void Initialize(SkinnedMeshRenderer skinnedMeshRenderer, RenderSelectorContext ctx, TriangleSelection target)
        {
            CustomAnimationMode.StopAnimationMode();
            _skinnedMeshRenderer = skinnedMeshRenderer;
            _renderctx = ctx;
            _mesh = _skinnedMeshRenderer.sharedMesh;
            LoadAsset();
            _target = target;
            _selectedIndex = FindIndex(_triangleSelections, _target) + 1;
            _selectorcontext = CreateInstance<TriangleSelectorContext>();
            if (!_renderctx.isRenderToggle) _isAutoPreview = _renderctx.FixedPreview;
            StartPreview();
        }

        public void Dispose()
        {
            CustomAnimationMode.StopAnimationMode();
        }

        public void RenderGUI()
        {
            //serializedObject.Update();
            using (new GUILayout.HorizontalScope())
            {   
                GUILayout.Label("Triangle Selection");
                int selectedIndex = EditorGUILayout.Popup(_selectedIndex, _displayedOptions);
                if (selectedIndex != _selectedIndex)
                {   
                    _selectedIndex = selectedIndex;
                    if (_selectedIndex == 0)
                    {
                        _target = new();
                        StopPrview();
                    }
                    else
                    {
                        _target.selection = new List<int>(_triangleSelections[_selectedIndex - 1].selection);
                        StartPreview();
                    }
                }

                if (GUILayout.Button("Edit"))
                {
                    StopPrview();
                    _selectorcontext.target_default = new HashSet<int>(_target.selection);
                    TriangleSelector.ShowWindow(_selectorcontext, _skinnedMeshRenderer);
                }

            }
            
            if (GUILayout.Button("Open Triangle Selector"))
            {
                StopPrview();
                _selectorcontext.target_default = new HashSet<int>();
                TriangleSelector.ShowWindow(_selectorcontext, _skinnedMeshRenderer);
            }
            
            List<int> newSelection = _selectorcontext.target;
            if (newSelection != null && newSelection.Count > 0)
            {
                //Debug.Log("update");
                _selectorcontext.target = new List<int>();
                TriangleSelection newTriangleSelection = new TriangleSelection { selection = newSelection };

                SaveAsScriptableObject.UpdateData(_triangleSelectionContainer, newTriangleSelection);

                LoadAsset();
                _target.selection = new List<int>(newSelection);
                _selectedIndex = FindIndex(_triangleSelections, _target) + 1;
                StartPreview();
            }

            if (_renderctx.isRenderToggle && GUILayout.Button(_isAutoPreview ? "Disable Auto Preview" : "Enable Auto Preview"))
            {
                ToggleAutoPreview();
            }

            /*
            string label = (target as RemoveMeshFromScene).triangleSelection != null ? (target as RemoveMeshFromScene).triangleSelection.selection.Count.ToString() : "なくない?";
            GUILayout.Label(label);
            */
        }

        private void LoadAsset()
        {
            _triangleSelectionContainer = SaveAsScriptableObject.GetContainer(_mesh);
            _triangleSelections = _triangleSelectionContainer.selections;
            _displayedOptions = _triangleSelections.Select(ts => ts.selection.Count().ToString()).ToArray();
            _displayedOptions = new[] { "None" }.Concat(_displayedOptions).ToArray();
        }

        private void StartPreview()
        {
            if (_isAutoPreview && _target != null && _target.selection.Count > 0)
            {
                CustomAnimationMode.StopAnimationMode();
                CustomAnimationMode.StartAnimationMode(_skinnedMeshRenderer);
                if (_renderctx.isblendhsape)
                {
                    _skinnedMeshRenderer.sharedMesh = ClampBlendShapeUtility.GenerateClampBlendShape(_mesh, _target.selection.ToHashSet());
                }
                else if (_renderctx.isKeep)
                {
                    _skinnedMeshRenderer.sharedMesh = MeshUtility.keepTriangles(_mesh, _target.selection.ToHashSet());
                }
                else
                {
                    _skinnedMeshRenderer.sharedMesh = MeshUtility.RemoveTriangles(_mesh, _target.selection.ToHashSet());
                }
                
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

    public class SaveAsScriptableObject
    {
        private const string SAVE_PATH = "Assets/TriangleSelection";

        public static void UpdateData(TriangleSelectionContainer triangleSelection, TriangleSelection newSelection)
        {
            if (!triangleSelection.selections.Contains(newSelection))
            {
                triangleSelection.selections.Add(newSelection);
                EditorUtility.SetDirty(triangleSelection);
                AssetDatabase.SaveAssets();
                //Debug.Log("追加");
            }
        }

        public static TriangleSelectionContainer GetContainer(Mesh mesh)
        {
            if (!Directory.Exists(SAVE_PATH)) Directory.CreateDirectory(SAVE_PATH);
            string[] guids = AssetDatabase.FindAssets("t:TriangleSelectionContainer", new[] { SAVE_PATH });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                TriangleSelectionContainer selection = AssetDatabase.LoadAssetAtPath<TriangleSelectionContainer>(assetPath);
                if (selection != null && selection.mesh == mesh)
                {
                    //Debug.Log("既存のTriangleSelectionを取得");
                    //Debug.Log(selection.mesh.name);
                    //Debug.Log(selection.selections.Count);
                    return selection;
                }
            }

            //Debug.Log("存在しない場合は新規作成");
            return CreateAsset(mesh);
        }

        private static TriangleSelectionContainer CreateAsset(Mesh mesh)
        {
            var instance = ScriptableObject.CreateInstance<TriangleSelectionContainer>();
            instance.mesh = mesh;

            if (!Directory.Exists(SAVE_PATH)) Directory.CreateDirectory(SAVE_PATH);
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath($"{SAVE_PATH}/{mesh.name}.asset");
            AssetDatabase.CreateAsset(instance, uniquePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var loadedInstance = (TriangleSelectionContainer)AssetDatabase.LoadAssetAtPath(uniquePath, typeof(TriangleSelectionContainer));
            return loadedInstance;
        }
    }
}
