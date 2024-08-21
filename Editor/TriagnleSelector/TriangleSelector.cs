using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Color = UnityEngine.Color;

namespace com.aoyon.modulecreator
{

    public class TriangleSelectorContext : ScriptableObject
    {
        public SkinnedMeshRenderer SkinnedMeshRenderer;
        public List<int> target = new();
        public HashSet<int> target_default = new();
        public string displayname;
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
        private bool _isIsland = true;
        public float _scale = 0.03f;
        private string _selectionName = "";

        private bool _isPreviewEnabled = true;

        public static void ShowWindow(TriangleSelectorContext context, SkinnedMeshRenderer skinnedMeshRenderer)
        {
            TriangleSelector window = GetWindow<TriangleSelector>();
            context.SkinnedMeshRenderer = skinnedMeshRenderer;
            window.Initialize(context);
            window.Show();
        }

        private void Initialize(TriangleSelectorContext _triangleSelectorContext)
        {
            this._triangleSelectorContext = _triangleSelectorContext;
            _OriginskinnedMeshRenderer = _triangleSelectorContext.SkinnedMeshRenderer;
            _previewController = new();
            _previewController.Initialize(_OriginskinnedMeshRenderer, _triangleSelectorContext.target_default);
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            _previewController.Dispose();
            _triangleSelectorContext.end = true;
            SceneView.duringSceneGui -= OnSceneGUI;
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
            GUILayout.Label(LocalizationEditor.GetLocalizedText("TriangleSelector.SelectedTotalPolygonsLabel"), EditorStyles.boldLabel);
            GUILayout.Label($"{_previewController._triangleSelectionManager.GetSelectedTriangles().Count}/{_previewController._triangleSelectionManager.GetAllTriangles().Count}");
            EditorGUILayout.HelpBox(LocalizationEditor.GetLocalizedText("TriangleSelector.commondescription"), MessageType.Info);

            EditorGUILayout.Space();
            RenderSelectionButtons();
            RenderUndoRedoButtons();

            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            RenderSelectionMode();
            RenderModeoff();
            GUILayout.EndHorizontal();

            process_options();

            EditorGUILayout.Space();
            RenderNameInput();
            RenderApply();

        }

        private void RenderNameInput()
        {
            _selectionName = EditorGUILayout.TextField(LocalizationEditor.GetLocalizedText("TriangleSelector.SelectionName"), _selectionName);
        }

        private void RenderApply()
        {
            GUI.enabled = _previewController._triangleSelectionManager.GetSelectedTriangles().Count() > 0;
            if (GUILayout.Button(LocalizationEditor.GetLocalizedText("TriangleSelector.Apply")))
            {
                _triangleSelectorContext.target = _previewController._triangleSelectionManager.GetSelectedTriangles().ToList();
                _triangleSelectorContext.displayname = _selectionName;
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
                }
                else if (e.keyCode == KeyCode.Y) // Ctrl/Cmd + Y
                {
                    _previewController.PerformRedo();
                    e.Use();
                }
            }
        }

        private void HandleMouseEvents(SceneView sceneView, Event e)
        {   
            if (e.isMouse)
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
                else if (!_isdragging)
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
            //Color selectionColor = _isPreviewSelected ? new Color(1, 0, 0, 0.2f) : new Color(0, 1, 1, 0.2f);
            Color selectionColor = new Color(0.6f, 0.7f, 0.8f, 0.25f); 
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
            GUILayout.BeginHorizontal();

            GUILayout.Label(LocalizationEditor.GetLocalizedText("TriangleSelector.SelectionMode"));

            string[] options = { LocalizationEditor.GetLocalizedText("TriangleSelector.island"), LocalizationEditor.GetLocalizedText("TriangleSelector.polygon") };
            int SelectionModeIndex = _isIsland ? 0 : 1;
            SelectionModeIndex = EditorGUILayout.Popup(SelectionModeIndex, options);
            _isIsland = SelectionModeIndex == 0 ? true: false;

            GUILayout.EndHorizontal();
        }

        private void RenderModeoff()
        {
            if (GUILayout.Button(!_isPreviewEnabled ? LocalizationEditor.GetLocalizedText("TriangleSelector.EnableSelectionButton") : LocalizationEditor.GetLocalizedText("TriangleSelector.DisableSelectionButton")))
            {
                _isPreviewEnabled = !_isPreviewEnabled;
            }
        }

        private void process_options()
        {
            if (_isIsland)
            {
                EditorGUILayout.HelpBox(LocalizationEditor.GetLocalizedText("TriangleSelector.island.description"), MessageType.Info);
                EditorGUILayout.Space();
                _mergeSamePosition = !EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("TriangleSelector.island.SplitMeshMoreToggle"), !_mergeSamePosition);
                EditorGUILayout.HelpBox(LocalizationEditor.GetLocalizedText("TriangleSelector.island.SplitMeshMoreToggle.description"), MessageType.Info);
                _checkAll = !EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("TriangleSelector.island.SelectAllInRangeToggle"), !_checkAll);
                EditorGUILayout.HelpBox(LocalizationEditor.GetLocalizedText("TriangleSelector.island.SelectAllInRangeToggle.description"), MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(LocalizationEditor.GetLocalizedText("TriangleSelector.polygon.description"), MessageType.Info);
                EditorGUILayout.Space();
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(LocalizationEditor.GetLocalizedText("TriangleSelector.polygon.scale"));
                    _scale = EditorGUILayout.Slider(_scale, 0.0f, 0.1f);
                }
                EditorGUILayout.HelpBox(LocalizationEditor.GetLocalizedText("TriangleSelector.polygon.scale.description"), MessageType.Info);
            }

            EditorGUILayout.Space();

        }


    }
}
