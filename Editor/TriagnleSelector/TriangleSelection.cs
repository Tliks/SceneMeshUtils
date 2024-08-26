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
            _selectedIndex = SaveAsScriptableObject.FindIndex(_triangleSelections, _target) + 1;
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
            LocalizationEditor.RenderLocalize();
            GUI.enabled = _triangleSelector == null;
            using (new GUILayout.HorizontalScope())
            {   
                GUILayout.Label(LocalizationEditor.GetLocalizedText("TriangleSelection.TriangleSelection"));
                int selectedIndex = EditorGUILayout.Popup(_selectedIndex, _displayedOptions);
                if (selectedIndex != _selectedIndex)
                {   
                    _selectedIndex = selectedIndex;
                    if (_selectedIndex == 0)
                    {
                        _target.selection = new();
                        StopPrview();
                    }
                    else
                    {
                        _target.selection = new List<int>(_triangleSelections[_selectedIndex - 1].selection);
                        StartPreview();
                    }
                }

                GUI.enabled = _selectedIndex != 0;
                if (GUILayout.Button(LocalizationEditor.GetLocalizedText("TriangleSelection.Remove")))
                {
                    StopPrview();
                    SaveAsScriptableObject.RemoveData(_triangleSelectionContainer, _selectedIndex - 1);
                    LoadAsset();
                    _selectedIndex = _selectedIndex > 0 ? _selectedIndex - 1 : 0;
                    if (_selectedIndex != 0)
                    {
                        _target.selection = new List<int>(_triangleSelections[_selectedIndex - 1].selection);
                    }
                    else
                    {
                        _target.selection = new List<int>();
                    }
                    StartPreview();
                    
                }
                GUI.enabled = true;
            }
            GUI.enabled = true;
            
            string label = _selectedIndex == 0 ? LocalizationEditor.GetLocalizedText("TriangleSelection.Add") : LocalizationEditor.GetLocalizedText("TriangleSelection.Edit");
            if (GUILayout.Button(_triangleSelector == null ? label : LocalizationEditor.GetLocalizedText("TriangleSelection.CloseSelector")))
            {
                if (_triangleSelector == null)
                {
                    StopPrview();

                    // Add New Selection
                    if (_selectedIndex == 0)
                    {
                        _selectorcontext.isedit = false;
                        _selectorcontext.target_default = new HashSet<int>();
                    }
                    // Edit Current Selection
                    else
                    {
                        _selectorcontext.isedit = true;
                        _selectorcontext.target_default = new HashSet<int>(_target.selection);
                        _selectorcontext.displayname = _triangleSelections[_selectedIndex - 1].displayname;
                    }

                    _triangleSelector = TriangleSelector.ShowWindow(_selectorcontext, _skinnedMeshRenderer);
                }
                else
                {
                    _triangleSelector.Dispose();
                }
            }
            
            // 新規選択
            GUI.enabled = _triangleSelector == null;
            if (_selectorcontext.end)
            {
                _selectorcontext.end = false;

                List<int> newSelection = _selectorcontext.target;
                if (newSelection != null && newSelection.Count > 0)
                {
                    //Debug.Log("update");
                    _selectorcontext.target = new List<int>();
                    
                    string displayname = _selectorcontext.displayname;
                    // displaynameが未入力時は自動決定
                    if (displayname == null || displayname == "")
                    {
                        string uniquename = SaveAsScriptableObject.GetUniqueDisplayname(_triangleSelections);
                        displayname = uniquename;
                    }
                    _selectorcontext.displayname = null;

                    if (_selectorcontext.iseditmodeasnew)
                    {
                        TriangleSelection newTriangleSelection = new TriangleSelection { selection = newSelection };
                        newTriangleSelection.displayname = displayname;
                        newTriangleSelection.createtime = SaveAsScriptableObject.GetTimestamp();
                        SaveAsScriptableObject.AddData(_triangleSelectionContainer, newTriangleSelection);
                    }
                    else
                    {
                        TriangleSelection currentTriangleSelection = _triangleSelectionContainer.selections[_selectedIndex - 1];
                        currentTriangleSelection.selection = newSelection;
                        currentTriangleSelection.displayname = displayname;
                        currentTriangleSelection.createtime = SaveAsScriptableObject.GetTimestamp();
                        SaveAsScriptableObject.UpdateData(_triangleSelectionContainer);
                    }

                    _target.selection = new List<int>(newSelection);
                    LoadAsset();
                    _selectedIndex = SaveAsScriptableObject.FindIndex(_triangleSelections, _target) + 1;
                }
                
                StartPreview();
            }

            if (_renderctx.isRenderToggle && GUILayout.Button(_isAutoPreview ? LocalizationEditor.GetLocalizedText("TriangleSelection.DisablePreview") : LocalizationEditor.GetLocalizedText("TriangleSelection.EnablePreview")))
            {
                ToggleAutoPreview();
            }
            GUI.enabled = true;

            /*
            string label = (target as RemoveMeshFromScene).triangleSelection != null ? (target as RemoveMeshFromScene).triangleSelection.selection.Count.ToString() : "なくない?";
            GUILayout.Label(label);
            */
        }

        private void LoadAsset()
        {
            _triangleSelectionContainer = SaveAsScriptableObject.GetContainer(_mesh);
            _triangleSelections = _triangleSelectionContainer.selections;
            _displayedOptions = _triangleSelections
                .Select(ts => $"{ts.displayname} ({SaveAsScriptableObject.CalculatePercent(ts.selection.Count(), _triangleSelectionContainer.TriangleCount)}%)")
                .ToArray();
            _displayedOptions = new[] { "None" }
                .Concat(_displayedOptions)
                .ToArray();
        }

        private void StartPreview()
        {
            if (_isAutoPreview && _target != null && _target.selection.Count > 0)
            {
                CustomAnimationMode.StopAnimationMode();
                CustomAnimationMode.StartAnimationMode(_skinnedMeshRenderer);
                if (_renderctx.isblendhsape)
                {
                    _skinnedMeshRenderer.sharedMesh = ShrinkBlendShapeUtility.GenerateShrinkBlendShape(_mesh, _target.selection.ToHashSet());
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

    }

    public class SaveAsScriptableObject
    {
        private const string SAVE_PATH = "Assets/SceneMeshUtils/TriangleSelection";

        public static void AddData(TriangleSelectionContainer triangleSelection, TriangleSelection newSelection)
        {   
            int index = FindIndex(triangleSelection.selections, newSelection);
            if (index != -1)
            {
                RemoveData(triangleSelection, index);
            }
            triangleSelection.selections.Add(newSelection);
            UpdateData(triangleSelection);
        }

        public static void RemoveData(TriangleSelectionContainer triangleSelection, int index)
        {
            if (index >= 0 && index < triangleSelection.selections.Count)
            {
                triangleSelection.selections.RemoveAt(index);
                UpdateData(triangleSelection);
            }
        }

        public static void UpdateData(TriangleSelectionContainer triangleSelection)
        {
            EditorUtility.SetDirty(triangleSelection);
            AssetDatabase.SaveAssets();
        }

        public static int FindIndex(List<TriangleSelection> triangleSelections, TriangleSelection triangleSelection)
        {
            int index = triangleSelections.FindIndex(ts => ts.selection.SequenceEqual(triangleSelection.selection));
            return index;
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
                    int targetcount = mesh.triangles.Count() / 3;
                    int cachecount = selection.TriangleCount != 0 ? selection.TriangleCount : targetcount; // TriangleCountが0の場合はtargetcountを使用
                    if (targetcount != cachecount)
                    {
                        Debug.LogWarning($"Mesh changes detected. A new TriangleSelectionContainer will be created. Current polygon count: {targetcount}. Saved polygon count: {cachecount}.");
                    }
                    else
                    {
                        return selection;
                    }
                }
            }

            //Debug.Log("存在しない場合は新規作成");
            return CreateAsset(mesh);
        }

        private static TriangleSelectionContainer CreateAsset(Mesh mesh)
        {
            var instance = ScriptableObject.CreateInstance<TriangleSelectionContainer>();
            instance.mesh = mesh;
            instance.TriangleCount = mesh.triangles.Count() / 3;

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

            int year = now.Year % 100;
            int month = now.Month;
            int day = now.Day;
            int hour = now.Hour;
            int minute = now.Minute;
            int second = now.Second;

            string timestampStr = $"{year:D2}{month:D2}{day:D2}{hour:D2}{minute:D2}{second:D2}";
            long timestamp = long.Parse(timestampStr);

            return timestamp;
        }

        public static int CalculatePercent(int selection, int total)
        {
            float percent = (float)selection / (float)total * 100;
            percent = (int)Math.Round((double)percent);
            return (int)percent;
        }

    }
}