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
    public class ModuleCreatorIsland : EditorWindow
    {
        
        private PreviewController _previewController;
        private GameObject _RootObject;

        private SkinnedMeshRenderer _OriginskinnedMeshRenderer;

        private const double raycastInterval = 0.01;
        private double _lastUpdateTime = 0;
        private Rect _selectionRect = new Rect();
        private bool _isdragging = false;
        private const float dragThreshold = 10f;
        private Vector2 _startPoint;

        private int _UtilityIndex = 0;

        public bool _mergeSamePosition = true;
        private bool _checkAll = true;
        private bool _isIsland = true;
        public float _scale = 0.03f;

        private bool _isPreviewEnabled = true;

        private void OnEnable()
        {
            GameObject targetObject = Selection.activeGameObject;
            _OriginskinnedMeshRenderer = targetObject.GetComponent<SkinnedMeshRenderer>();
            _previewController = new();
            _previewController.Initialize(_OriginskinnedMeshRenderer);
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            _previewController.Dispose();
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
            using (new GUILayout.HorizontalScope())
            {   
                /*
                using (new GUILayout.VerticalScope())
                {
                    RenderSelectionWinodw();
                }
                */

                
                float halfWidth = position.width / 2f;

                using (new GUILayout.VerticalScope(GUILayout.Width(halfWidth)))
                {
                    RenderSelectionWinodw();
                }

                GUILayout.Box("", GUILayout.Width(2), GUILayout.ExpandHeight(true));

                using (new GUILayout.VerticalScope())
                {
                    RenderUtility();
                }
                

            }
        }

        private void RenderSelectionWinodw()
        {
            LocalizationEditor.RenderLocalize();

            EditorGUILayout.Space();
            RenderVertexCount();
            EditorGUILayout.Space();

            RenderSelectionButtons();
            RenderUndoRedoButtons();

            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            RenderSelectionMode();
            RenderModeoff();
            GUILayout.EndHorizontal();

            process_options();

        }

        
        private void RenderUtility()
        {
            string[] options = 
            { 
                LocalizationEditor.GetLocalizedText("Utility.None"),
                LocalizationEditor.GetLocalizedText("Utility.ModuleCreator"), 
                LocalizationEditor.GetLocalizedText("Utility.GenerateMask"),
                LocalizationEditor.GetLocalizedText("Utility.DeleteMesh"),
                LocalizationEditor.GetLocalizedText("Utility.BlendShape"),
                LocalizationEditor.GetLocalizedText("Utility.TransformPolygon")
            };

            int new_index;
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(LocalizationEditor.GetLocalizedText("Utility.description"));
                new_index = EditorGUILayout.Popup(_UtilityIndex, options);
            }

            switch (new_index)
            {
                case 1:
                    if (new_index != _UtilityIndex)
                    {
                        CreateModuleUtilty.Initialize(_OriginskinnedMeshRenderer, _previewController._triangleSelectionManager);
                        _UtilityIndex = new_index;
                    }
                    CreateModuleUtilty.RenderModuleCreator();
                    break;

                case 2:
                    if (new_index != _UtilityIndex)
                    {
                        GenerateMaskUtilty.Initialize(_OriginskinnedMeshRenderer, _previewController._triangleSelectionManager);
                        _UtilityIndex = new_index;
                    }
                    GenerateMaskUtilty.RenderGenerateMask();
                    break;

                case 3:
                    if (new_index != _UtilityIndex)
                    {
                        DeleteMeshUtilty.Initialize(_OriginskinnedMeshRenderer, _previewController._triangleSelectionManager);
                        _UtilityIndex = new_index;
                    }
                    DeleteMeshUtilty.RenderDeleteMesh();
                    break;

                case 4:
                    if (new_index != _UtilityIndex)
                    {
                        ClampBlendShapeUtility.Initialize(_OriginskinnedMeshRenderer, _previewController._triangleSelectionManager);
                        _UtilityIndex = new_index;
                    }
                    ClampBlendShapeUtility.RendergenerateClamp();
                    break;
                case 5:
                    if (new_index != _UtilityIndex)
                    {
                        TransformPolygonUtilityEditor.Initialize(_OriginskinnedMeshRenderer, _previewController._triangleSelectionManager.GetSelectedTriangles());
                        _UtilityIndex = new_index;
                    }
                    break;
            }
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


        private void RenderislandDescription()
        {
            //EditorGUILayout.Space();
            EditorGUILayout.HelpBox(LocalizationEditor.GetLocalizedText("description"), MessageType.Info);
            //EditorGUILayout.Space();
        }

        private void RenderVertexCount()
        {
            GUILayout.Label(LocalizationEditor.GetLocalizedText("SelectedTotalPolygonsLabel"), EditorStyles.boldLabel);
            GUILayout.Label($"{_previewController._triangleSelectionManager.GetSelectedTriangles().Count}/{_previewController._triangleSelectionManager.GetAllTriangles().Count}");
        }

        private void RenderSelectionButtons()
        {
            GUILayout.BeginHorizontal();
        
            GUI.enabled = _isPreviewEnabled;
            if (GUILayout.Button(LocalizationEditor.GetLocalizedText("SelectAllButton")))
            {
                _previewController.SelectAll();
            }

            if (GUILayout.Button(LocalizationEditor.GetLocalizedText("UnselectAllButton")))
            {
                _previewController.UnselectAll();
            }

            if (GUILayout.Button(LocalizationEditor.GetLocalizedText("ReverseAllButton")))
            {
                _previewController.ReverseAll();
            }
            GUI.enabled = true;

            GUILayout.EndHorizontal();
        }

        private void RenderSelectionMode()
        {
            string[] options = { LocalizationEditor.GetLocalizedText("SelectionMode.Island"), LocalizationEditor.GetLocalizedText("SelectionMode.Polygon") };
            GUILayout.BeginHorizontal();
            GUILayout.Label(LocalizationEditor.GetLocalizedText("SelectionMode.description"));
            int SelectionModeIndex = _isIsland ? 0 : 1;
            SelectionModeIndex = EditorGUILayout.Popup(SelectionModeIndex, options);
            _isIsland = SelectionModeIndex == 0 ? true: false;
            GUILayout.EndHorizontal();
        }


        private void process_options()
        {
            EditorGUILayout.Space();

            if (_isIsland)
            {
                RenderislandDescription();
                _mergeSamePosition = !EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("SplitMeshMoreToggle"), !_mergeSamePosition);
                EditorGUILayout.HelpBox(LocalizationEditor.GetLocalizedText("tooltip.SplitMeshMoreToggle"), MessageType.Info);
                _checkAll = !EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("SelectAllInRangeToggle"), !_checkAll);
                EditorGUILayout.HelpBox(LocalizationEditor.GetLocalizedText("tooltip.SelectAllInRangeToggle"), MessageType.Info);
            }
            else
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label(LocalizationEditor.GetLocalizedText("SelectionMode.Polygon.scale"));
                    _scale = EditorGUILayout.Slider(_scale, 0.0f, 0.1f);
                }
            }

            EditorGUILayout.Space();

        }

        private void RenderModeoff()
        {
            if (GUILayout.Button(!_isPreviewEnabled ? LocalizationEditor.GetLocalizedText("EnableSelectionButton") : LocalizationEditor.GetLocalizedText("DisableSelectionButton")))
            {
                _isPreviewEnabled = !_isPreviewEnabled;
            }
        }

        private void RenderUndoRedoButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(LocalizationEditor.GetLocalizedText("UndoButton")))
            {
                _previewController.PerformUndo();
            }

            if (GUILayout.Button(LocalizationEditor.GetLocalizedText("RedoButton")))
            {
                _previewController.PerformRedo();
            }

            EditorGUILayout.EndHorizontal();

        }

    }
}
