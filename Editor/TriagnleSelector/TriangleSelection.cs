using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace com.aoyon.scenemeshutils
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
        private TriangleSelector _triangleSelector;
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
            if (_triangleSelector != null)
            {
                _triangleSelector.Dispose();
            }
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
                    _triangleSelector = TriangleSelector.ShowWindow(_selectorcontext, _skinnedMeshRenderer);
                }

            }
            
            if (GUILayout.Button("Open Triangle Selector"))
            {
                StopPrview();
                _selectorcontext.target_default = new HashSet<int>();
                _triangleSelector = TriangleSelector.ShowWindow(_selectorcontext, _skinnedMeshRenderer);
            }
            
            if (_selectorcontext.end)
            {
                _selectorcontext.end = false;

                List<int> newSelection = _selectorcontext.target;
                if (newSelection != null && newSelection.Count > 0)
                {
                    //Debug.Log("update");
                    _selectorcontext.target = new List<int>();

                    TriangleSelection newTriangleSelection = new TriangleSelection { selection = newSelection };
                    string displayname;
                    float percent = (float)newSelection.Count() / ((float)_mesh.triangles.Count() / 3) * 100;
                    percent = (int)Math.Round((double)percent);
                    if (_selectorcontext.displayname == null || _selectorcontext.displayname == "")
                    {
                        string uniquename = SaveAsScriptableObject.GetUniqueDisplayname(_triangleSelections);
                        displayname = $"{uniquename} ({percent}%)";
                    }
                    else
                    {
                        displayname = $"{_selectorcontext.displayname} ({percent}%)";
                    }
                    _selectorcontext.displayname = null;
                    newTriangleSelection.displayname = displayname;
                    newTriangleSelection.createtime = SaveAsScriptableObject.GetTimestamp();

                    SaveAsScriptableObject.UpdateData(_triangleSelectionContainer, newTriangleSelection);

                    LoadAsset();
                    _target.selection = new List<int>(newSelection);
                    _selectedIndex = FindIndex(_triangleSelections, _target) + 1;
                }
                
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
            _displayedOptions = _triangleSelections.Select(ts => ts.displayname).ToArray();
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

        public static string GetUniqueDisplayname(List<TriangleSelection> selections)
        {
            int maxIndex = -1;
            var regex = new Regex(@"Selection(\d+)");

            foreach (var selection in selections)
            {
                var match = regex.Match(selection.displayname);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int index))
                {
                    if (index > maxIndex)
                    {
                        maxIndex = index;
                    }
                }
            }

            return $"Selection{maxIndex + 1}";
        }

        public static long GetTimestamp()
        {
            DateTime now = DateTime.Now;

            int year = now.Year;
            int month = now.Month;
            int day = now.Day;
            int hour = now.Hour;
            int minute = now.Minute;
            int second = now.Second;

            string timestampStr = $"{year:D4}{month:D2}{day:D2}{hour:D2}{minute:D2}{second:D2}";
            long timestamp = long.Parse(timestampStr);

            return timestamp;
        }
    }
}
