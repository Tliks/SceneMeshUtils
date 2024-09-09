using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using nadena.dev.ndmf.preview;
using Debug = UnityEngine.Debug;
using Color = UnityEngine.Color;

namespace com.aoyon.scenemeshutils
{

    public class TriangleSelectorContext : ScriptableObject
    {
        public SkinnedMeshRenderer SkinnedMeshRenderer;
        public List<int> target = new();
        public bool isedit;
        public HashSet<int> target_default = new();
        public string displayname;
        public bool iseditmodeasnew;
        public bool end = false;
    }

    public class TriangleSelector : EditorWindow
    {
        private TriangleSelectorContext _triangleSelectorContext;

        private PreviewController _previewController;
        //private GameObject _RootObject;

        private SkinnedMeshRenderer _OriginskinnedMeshRenderer;

        private const double raycastInterval = 0.01;
        private double _lastUpdateTime = 0;
        private Rect _selectionRect = new Rect();
        private bool _isdragging = false;
        private const float dragThreshold = 10f;
        private Vector2 _startPoint;

        public bool _mergeSamePosition = true;
        private bool _checkAll = true;
        private enum SelectionModes
        {
            Island,
            Poligon
        }
        private SelectionModes _selectionMode;
        public float _scale = 0.03f;
        private string _selectionName = "";

        private bool _isPreviewEnabled = true;
        private int _iseditmodeasnew = 0;

        private Color selectionColor = new Color(0.6f, 0.7f, 0.8f, 0.25f);

        public static TriangleSelector ShowWindow(TriangleSelectorContext context, SkinnedMeshRenderer skinnedMeshRenderer)
        {
            Type[] types = new Type[] { typeof(ModuleCreator), typeof(MaskTextureGenerator) };
            TriangleSelector window = GetWindow<TriangleSelector>(types); 
            context.SkinnedMeshRenderer = skinnedMeshRenderer;
            window.Initialize(context);
            window.Show();
            return window;
        }

        private void Initialize(TriangleSelectorContext _triangleSelectorContext)
        {
            NDMFPreview.DisablePreviewDepth += 1;

            this._triangleSelectorContext = _triangleSelectorContext;
            _OriginskinnedMeshRenderer = _triangleSelectorContext.SkinnedMeshRenderer;
            _previewController = new();
            _previewController.Initialize(_OriginskinnedMeshRenderer, _triangleSelectorContext.target_default);
            if (_triangleSelectorContext.isedit) _selectionName = _triangleSelectorContext.displayname;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            _previewController.Dispose();
            _triangleSelectorContext.end = true;
            SceneView.duringSceneGui -= OnSceneGUI;

            NDMFPreview.DisablePreviewDepth -= 1;
        }

        public void Dispose()
        {
            Close();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            //if (_PreviewSkinnedMeshRenderer == null) Close();
            if (!_isPreviewEnabled) return;
            Event e = Event.current;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            HandleUndoRedoEvent(e);
            HandleMouseEvents(sceneView, e);
            DrawSelectionRectangle();
        }

        private void OnGUI()
        {
            LocalizationEditor.RenderLocalize();
            
            EditorGUILayout.Space();
            string label  = $"{LocalizationEditor.GetLocalizedText("TriangleSelector.SelectedTotalPolygonsLabel")}: {_OriginskinnedMeshRenderer.name}";
            GUILayout.Label(label, EditorStyles.boldLabel);
            GUILayout.Label($"{_previewController._triangleSelectionManager.GetSelectedTriangles().Count}/{_previewController._triangleSelectionManager.GetAllTriangles().Count}");
            EditorGUILayout.HelpBox(LocalizationEditor.GetLocalizedText("TriangleSelector.commondescription"), MessageType.Info);

            EditorGUILayout.Space();
            RenderSelectionButtons();
            RenderUndoRedoButtons();

            EditorGUILayout.Space();

            RenderSelectionMode();

            EditorGUILayout.Space();

            process_options();

            EditorGUILayout.Space();
            RenderApply();

        }

        private void RenderApply()
        {
            _selectionName = EditorGUILayout.TextField(LocalizationEditor.GetLocalizedText("TriangleSelector.SelectionName"), _selectionName);
            if (_triangleSelectorContext.isedit)
            {   
                string label = LocalizationEditor.GetLocalizedText("TriangleSelector.SaveMode");
                string editmode = LocalizationEditor.GetLocalizedText("TriangleSelector.SaveMode.edit");
                string newmode = LocalizationEditor.GetLocalizedText("TriangleSelector.SaveMode.new");
                _iseditmodeasnew = EditorGUILayout.Popup(label, _iseditmodeasnew, new string[] {editmode, newmode});
            }
            GUI.enabled = _previewController._triangleSelectionManager.GetSelectedTriangles().Count() > 0;
            if (GUILayout.Button(LocalizationEditor.GetLocalizedText("TriangleSelector.Apply")))
            {
                _triangleSelectorContext.target = _previewController._triangleSelectionManager.GetSelectedTriangles().ToList();
                _triangleSelectorContext.displayname = _selectionName;
                if (_triangleSelectorContext.isedit) _triangleSelectorContext.iseditmodeasnew = _iseditmodeasnew == 0 ? false : true; 
                else _triangleSelectorContext.iseditmodeasnew = true;
                Close();
            }
            GUI.enabled = true;
        }

        void HandleUndoRedoEvent(Event e)
        {
            if (e.type == EventType.KeyDown && (e.control || e.command))
            {
                if (e.keyCode == KeyCode.Z) // Ctrl/Cmd + Z
                {
                    _previewController.PerformUndo();
                    e.Use();
                    Repaint();
                }
                else if (e.keyCode == KeyCode.Y) // Ctrl/Cmd + Y
                {
                    _previewController.PerformRedo();
                    e.Use();
                    Repaint();
                }
            }
        }

        private bool FilterEvent(Event e)
        {
            switch (e.type)
            {
                case EventType.Layout:
                case EventType.Repaint:
                case EventType.ExecuteCommand:
                    return false;
                case EventType.MouseMove:
                case EventType.MouseUp:
                case EventType.MouseDown:
                case EventType.MouseDrag:
                    return true;
                default:
                    return false;
            }
        }

        private void HandleMouseEvents(SceneView sceneView, Event e)
        {   
            if (FilterEvent(e))
            {
                Vector2 mousePos = e.mousePosition;
                //consoleがrectに入っているので多分あまり正確ではない
                float xoffset = 10f;
                float yoffset = 30f; 
                Rect sceneViewRect = new Rect(0, 0, sceneView.position.width -xoffset, sceneView.position.height - yoffset);

                //sceneviewの外側にある場合の初期化処理
                if (!sceneViewRect.Contains(mousePos))
                {
                    HighlightEdgesManager.ClearHighlights();
                    if (_isdragging)
                    {
                        _isdragging = false;
                        _selectionRect = new Rect();
                        HandleUtility.Repaint();
                        DrawSelectionRectangle();
                    }
                    return;
                }

                bool _isIsland = _selectionMode == SelectionModes.Island;

                //左クリック
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    _startPoint = mousePos;
                }
                //左クリック解放
                else if (e.type == EventType.MouseUp && e.button == 0)
                {
                    //クリック
                    if (!_isdragging)
                    {
                        _previewController.HandleClick(true, _isIsland, _mergeSamePosition, _scale);
                    }
                    //ドラッグ解放
                    else
                    {
                        Vector2 endPoint = mousePos;
                        _previewController.HandleDrag(true, _isIsland, _mergeSamePosition, _checkAll, _startPoint, endPoint);
                    }
                    
                    _isdragging = false;
                    _selectionRect = new Rect();
                    DrawSelectionRectangle();
                    Repaint();

                }
                //ドラッグ中
                else if (e.type == EventType.MouseDrag && e.button == 0 && Vector2.Distance(_startPoint, mousePos) >= dragThreshold)
                {
                    _isdragging = true;
                    _selectionRect = new Rect(_startPoint.x, _startPoint.y, mousePos.x - _startPoint.x, mousePos.y - _startPoint.y);
                    double currentTime = EditorApplication.timeSinceStartup;
                    if (currentTime - _lastUpdateTime >= raycastInterval)
                    {
                        _lastUpdateTime = currentTime;
                        Vector2 endPoint = mousePos;
                        _previewController.HandleDrag(false, _isIsland, _mergeSamePosition, _checkAll, _startPoint, endPoint);
                    }
                    HandleUtility.Repaint();

                }
                //ドラッグしていないとき
                else if (e.type != EventType.MouseDrag && !_isdragging)
                {
                    double currentTime = EditorApplication.timeSinceStartup;
                    if (currentTime - _lastUpdateTime >= raycastInterval)
                    {
                        _lastUpdateTime = currentTime;
                        _previewController.HandleClick(false, _isIsland, _mergeSamePosition, _scale);
                    }
                }
            }
        }

        private void DrawSelectionRectangle()
        {
            Handles.BeginGUI();
            GUI.color = selectionColor;
            GUI.DrawTexture(_selectionRect, EditorGUIUtility.whiteTexture);
            Handles.EndGUI();
        }

        private void RenderSelectionButtons()
        {
            GUILayout.BeginHorizontal();
        
            GUI.enabled = _isPreviewEnabled;
            if (GUILayout.Button(LocalizationEditor.GetLocalizedText("TriangleSelector.SelectAllButton")))
            {
                _previewController.SelectAll();
            }

            if (GUILayout.Button(LocalizationEditor.GetLocalizedText("TriangleSelector.UnselectAllButton")))
            {
                _previewController.UnselectAll();
            }

            if (GUILayout.Button(LocalizationEditor.GetLocalizedText("TriangleSelector.ReverseAllButton")))
            {
                _previewController.ReverseAll();
            }
            GUI.enabled = true;

            GUILayout.EndHorizontal();
        }

        private void RenderUndoRedoButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(LocalizationEditor.GetLocalizedText("TriangleSelector.UndoButton")))
            {
                _previewController.PerformUndo();
            }

            if (GUILayout.Button(LocalizationEditor.GetLocalizedText("TriangleSelector.RedoButton")))
            {
                _previewController.PerformRedo();
            }

            EditorGUILayout.EndHorizontal();

        }

        private void RenderSelectionMode()
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayoutOption[] options = {GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false)};
                GUILayout.Label(LocalizationEditor.GetLocalizedText("TriangleSelector.SelectionMode"), options);
                string[] modes = { LocalizationEditor.GetLocalizedText("TriangleSelector.islandMode"), LocalizationEditor.GetLocalizedText("TriangleSelector.polygonMode") };
                _selectionMode = (SelectionModes)GUILayout.Toolbar((int)_selectionMode, modes);
                string lebal;
                if (_selectionMode == SelectionModes.Island)
                {
                    lebal = LocalizationEditor.GetLocalizedText("TriangleSelector.island.description");
                }
                else if (_selectionMode == SelectionModes.Poligon)
                {
                    lebal = LocalizationEditor.GetLocalizedText("TriangleSelector.polygon.description");
                }
                else
                {
                    throw new InvalidOperationException("invalid selection mode");
                }
                RenderInfo(lebal);
            }
        }

        private void process_options()
        {
            if (_selectionMode == SelectionModes.Island)
            {
                using (new GUILayout.HorizontalScope())
                {
                    string label = LocalizationEditor.GetLocalizedText("TriangleSelector.island.SplitMeshMoreToggle");
                    _mergeSamePosition = !EditorGUILayout.Toggle(label, !_mergeSamePosition);
                    RenderInfo(LocalizationEditor.GetLocalizedText("TriangleSelector.island.SplitMeshMoreToggle.description"));
                }

                using (new GUILayout.HorizontalScope())
                {
                    string label = LocalizationEditor.GetLocalizedText("TriangleSelector.island.SelectAllInRangeToggle");
                    _checkAll = !EditorGUILayout.Toggle(label, !_checkAll);
                    RenderInfo(LocalizationEditor.GetLocalizedText("TriangleSelector.island.SelectAllInRangeToggle.description"));
                }
            }
            else if (_selectionMode == SelectionModes.Poligon)
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(LocalizationEditor.GetLocalizedText("TriangleSelector.polygon.scale"));
                    _scale = EditorGUILayout.Slider(_scale, 0.0f, 0.1f);
                    RenderInfo(LocalizationEditor.GetLocalizedText("TriangleSelector.polygon.scale.description"));  
                }
            }
            else
            {
                throw new InvalidOperationException("invalid selection mode");
            }

        }

        private void RenderInfo(string label)
        {
            GUIContent infoContent = new GUIContent(EditorGUIUtility.IconContent("_Help"));
            infoContent.tooltip = label;
            GUILayoutOption[] options = {GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false)};
            GUILayout.Label(infoContent, options);
        }

    }
}
