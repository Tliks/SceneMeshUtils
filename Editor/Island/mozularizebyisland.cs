using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Color = UnityEngine.Color;


public class ModuleCreatorIsland : EditorWindow
{
    
    private IslandUtility _islandUtility;

    private CreateModuleUtilty _CreateModuleUtilty;
    private GenerateMaskUtilty _GenerateMaskUtilty;
    private DeleteMeshUtilty _DeleteMeshUtilty;
    private ClampBlendShapeUtility _ClampBlendShapeUtility;

    private TriangleSelectionManager _triangleSelectionManager; 

    private GameObject _RootObject;
    private SkinnedMeshRenderer _OriginskinnedMeshRenderer;
    private Mesh _bakedMesh;
    private Mesh _originalMesh;

    private const int MENU_PRIORITY = 49;
    private const double raycastInterval = 0.01;
    private double _lastUpdateTime = 0;

    private Stopwatch _stopwatch = new Stopwatch();
    public bool _mergeSamePosition = true;
    private Vector2 _startPoint;
    private Rect _selectionRect = new Rect();
    private bool _isdragging = false;
    private const float dragThreshold = 10f;
    private bool _isAll = true;
    private int _SelectionModeIndex = 0;
    private int _UtilityIndex = 0;
    private bool _isPreviewSelected;
    private bool _isPreviewEnabled;
    private Dictionary<int, int> _oldToNewIndexMap;
    private float _scale = 0.03f;


    [MenuItem("GameObject/AoyonAvatarUtils", false, MENU_PRIORITY)]
    public static void ShowWindowFromGameObject()
    {
        if (HasOpenInstances<ModuleCreatorIsland>())
        {
            var existingWindow = GetWindow<ModuleCreatorIsland>("AoyonAvatarUtils");
            existingWindow.Close();
        }
        CreateWindow<ModuleCreatorIsland>("AoyonAvatarUtils");
    }

    [MenuItem("GameObject/AoyonAvatarUtils", true)]
    private static bool ValidateShowWindowFromGameObject()
    {
        return Selection.activeGameObject != null 
            && Selection.activeGameObject.transform.parent != null 
            && Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>() != null;
    }

    private void OnEnable()
    {
        _OriginskinnedMeshRenderer = Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>();
        DuplicateAndSetup();
        CalculateIslands();

        HashSet<int> allTriangleIndices = Enumerable.Range(0, _bakedMesh.triangles.Count() / 3).ToHashSet();
        _triangleSelectionManager = new TriangleSelectionManager(allTriangleIndices);

        ToggleSelectionEnabled(true);
        ToggleSelectionSelected(false);

        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        MeshPreview.StopPreview();
        SceneRaycastUtility.DeleteCollider();
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.RepaintAll();
    }

    private void OnGUI()
    {
        using (new GUILayout.HorizontalScope())
        {
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

        RenderPreviewSelectedToggle();

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
            LocalizationEditor.GetLocalizedText("Utility.BlendShape")
        };

        int new_index;
        using (new GUILayout.HorizontalScope())
        {
            GUILayout.Label(LocalizationEditor.GetLocalizedText("Utility.description"));
            new_index = EditorGUILayout.Popup(_UtilityIndex, options);
        }

        if (new_index == 1)
        {
            if (new_index !=_UtilityIndex)
            {
                _CreateModuleUtilty = new CreateModuleUtilty(_OriginskinnedMeshRenderer, _RootObject.name, _originalMesh, _triangleSelectionManager);
                _UtilityIndex = new_index;
            }
            _CreateModuleUtilty.RenderModuleCreator();
        }
        else if (new_index == 2)
        {
            if (new_index !=_UtilityIndex)
            {
                _GenerateMaskUtilty = new GenerateMaskUtilty(_OriginskinnedMeshRenderer, _RootObject.name, _originalMesh, _triangleSelectionManager);
                _UtilityIndex = new_index;
            }
            _GenerateMaskUtilty.RenderGenerateMask();
        }
        else if (new_index == 3)
        {
            if (new_index !=_UtilityIndex)
            {
                _DeleteMeshUtilty = new DeleteMeshUtilty(_OriginskinnedMeshRenderer, _RootObject.name, _originalMesh, _triangleSelectionManager );
                _UtilityIndex = new_index;
            }
            _DeleteMeshUtilty.RenderDeleteMesh();
        }
        else if (new_index == 4)
        {
            if (new_index !=_UtilityIndex)
            {
                _ClampBlendShapeUtility = new ClampBlendShapeUtility(_OriginskinnedMeshRenderer, _RootObject.name, _originalMesh, _triangleSelectionManager);
                _UtilityIndex = new_index;
            }
            _ClampBlendShapeUtility.RendergenerateClamp();
        }
    }


    private void ToggleSelectionSelected(bool newMode)
    {
        if (_isPreviewSelected == newMode)
        {
            //Debug.LogWarning("current mode is already specified mode");
        }
        else
        {
            _isPreviewSelected = newMode;
        }
        UpdateMesh();
    }

    private void ToggleSelectionEnabled(bool newMode)
    {
        if (_isPreviewEnabled == newMode)
        {
            //Debug.LogWarning("current mode is already specified mode");
        }
        else
        {
            _isPreviewEnabled = newMode;
        }

        if (_isPreviewEnabled)
        {
            SceneRaycastUtility.AddCollider(_OriginskinnedMeshRenderer.transform);
            UpdateMesh(); // コライダーのメッシュを更新
        }
        else
        {
            SceneRaycastUtility.DeleteCollider();
        }

    }

    private void CalculateIslands()
    {
        _stopwatch.Restart();
        _islandUtility = new IslandUtility(_bakedMesh);
        _stopwatch.Stop();
        Debug.Log($"Islands Merged: {_islandUtility.GetMergedIslandCount()} of {_islandUtility.GetIslandCount()} - Elapsed Time: {_stopwatch.ElapsedMilliseconds} ms");
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        //if (_PreviewSkinnedMeshRenderer == null) Close();
        if (!_isPreviewEnabled) return;
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        Event e = Event.current;
        //tiveSKin(e);
        HandleUndoRedoEvent(e);
        //HandleScrollWheel(e);
        HandleMouseEvents(e, sceneView);
        DrawSelectionRectangle();
        HighlightEdgesManager.DrawHighlights();
    }

    private void HandleScrollWheel(Event e)
    {
        if (_SelectionModeIndex == 1 && e.type == EventType.ScrollWheel && (e.control || e.command))
        {
            Debug.Log("scale");
            _scale += e.delta.y * 0.001f;
            _scale = Mathf.Clamp(_scale, 0.0f, 0.1f);
            e.Use();
        }
    }

    private void DontActiveSKin(Event e)
    {
        if (e != null && Selection.activeGameObject != null)
        {
            GameObject currentActiveObject = Selection.activeGameObject;
            if (currentActiveObject == _OriginskinnedMeshRenderer.gameObject)
            {
                Selection.activeGameObject = null;
            }
        }
    }

    void HandleUndoRedoEvent(Event e)
    {
        if (e.type == EventType.KeyDown && (e.control || e.command))
        {
            if (e.keyCode == KeyCode.Z) // Ctrl/Cmd + Z
            {
                PerformUndo();
                e.Use();
            }
            else if (e.keyCode == KeyCode.Y) // Ctrl/Cmd + Y
            {
                PerformRedo();
                e.Use();
            }
        }
    }

    private void HandleMouseEvents(Event e, SceneView sceneView)
    {
        Vector2 mousePos = e.mousePosition;
        //consoleがrectに入っているので多分あまり正確ではない
        float xoffset = 10f;
        float yoffset = 30f; 
        Rect sceneViewRect = new Rect(0, 0, sceneView.position.width -xoffset, sceneView.position.height - yoffset);
        //Debug.Log($"{mousePos.x}/{sceneView.position.width - xoffset}, {mousePos.y}/{sceneView.position.height - yoffset}");

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
                HandleClick(true);
            }
            //ドラッグ解放
            else
            {
                Vector2 endPoint = mousePos;
                HandleDrag(_startPoint, endPoint, true);
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
                HandleDrag(_startPoint, endPoint, false);
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
                HandleClick(false);
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

    private void HandleClick(bool isclick)
    {
        if (SceneRaycastUtility.TryRaycast(out RaycastHit hitInfo))
        {
            int triangleIndex = hitInfo.triangleIndex;
            int newIndex = MeshUtility.ConvertNewTriangleIndexToOld(triangleIndex, _oldToNewIndexMap);

            HashSet<int> TriangleIndices = null;
            if (_SelectionModeIndex == 0)
            {
                TriangleIndices = _islandUtility.GetIslandtrianglesFromTriangleIndex(newIndex, _mergeSamePosition);
            }
            else if (_SelectionModeIndex == 1)
            {
                TriangleIndices = _islandUtility.GetTrianglesNearPositionInIsland(newIndex, hitInfo.point, _scale, _OriginskinnedMeshRenderer.transform);
            }
            TriangleIndices = _triangleSelectionManager.GetUniqueTriangles(TriangleIndices, _isPreviewSelected);

            HandleTriangleClick(TriangleIndices, isclick);
        }

    }

    private void HandleDrag(Vector2 startpos, Vector2 endpos, bool isclick)
    {
        if (startpos.x == endpos.x || startpos.y == endpos.y) return;
        
        MeshCollider meshCollider = GenerateColider(startpos, endpos);

        HashSet<int> TriangleIndices = null;
        if (_SelectionModeIndex == 0)
        {
            TriangleIndices = _islandUtility.GetIslandTrianglesInCollider(meshCollider, _mergeSamePosition, _isAll, _OriginskinnedMeshRenderer.transform);
        }
        else if (_SelectionModeIndex == 1)
        {
            TriangleIndices = _islandUtility.GetTrianglesInsideCollider(meshCollider, _OriginskinnedMeshRenderer.transform);
        }
        DestroyImmediate(meshCollider.gameObject);
        TriangleIndices = _triangleSelectionManager.GetUniqueTriangles(TriangleIndices, _isPreviewSelected);

        HandleTriangleClick(TriangleIndices, isclick);   
    }

    private void HandleTriangleClick(HashSet<int> TriangleIndices, bool isclick)
    {
        if (isclick)
        {
            _triangleSelectionManager.UpdateSelection(TriangleIndices, _isPreviewSelected);
            UpdateMesh();
            HighlightEdgesManager.ClearHighlights();
        }
        else
        {
            Color color = _isPreviewSelected ? Color.red : Color.cyan;
            HighlightEdgesManager.SetHighlightColor(color);
            HighlightEdgesManager.PrepareTriangleHighlights(_bakedMesh.triangles, TriangleIndices, _bakedMesh.vertices, _OriginskinnedMeshRenderer.transform);
        }
    }
 

    private MeshCollider GenerateColider(Vector2 startpos, Vector2 endpos)
    {
        Vector2 corner2 = new Vector2(startpos.x, endpos.y);
        Vector2 corner4 = new Vector2(endpos.x, startpos.y);
        
        Ray ray1 = HandleUtility.GUIPointToWorldRay(startpos);
        Ray ray2 = HandleUtility.GUIPointToWorldRay(corner2);
        Ray ray3 = HandleUtility.GUIPointToWorldRay(endpos);
        Ray ray4 = HandleUtility.GUIPointToWorldRay(corner4);

        bool isiso = ray1.direction == ray3.direction;

        float depth = isiso ? 10f : 3f;

        Vector3[] vertices = new Vector3[8];
        vertices[0] = ray1.origin;
        vertices[1] = ray2.origin;
        vertices[2] = ray3.origin;
        vertices[3] = ray4.origin;
        vertices[4] = ray1.origin + ray1.direction * depth;
        vertices[5] = ray2.origin + ray2.direction * depth;
        vertices[6] = ray3.origin + ray3.direction * depth;
        vertices[7] = ray4.origin + ray4.direction * depth;
        Mesh mesh = new Mesh();

        mesh.vertices = vertices;
        mesh.triangles = new int[]
        {
            //裏面ポリゴンだとcollider.ClosestPointがうまく動作しないことがある？
            // Front face
            0, 2, 1, 0, 3, 2,
            // Back face
            4, 5, 6, 4, 6, 7,
            // Top face
            1, 6, 5, 1, 2, 6,
            // Bottom face
            0, 7, 3, 0, 4, 7,
            // Left face
            0, 1, 4, 1, 5, 4,
            // Right face
            3, 6, 2, 3, 7, 6
        };

        GameObject coliderObject = new GameObject();
        MeshCollider meshCollider = coliderObject.AddComponent<MeshCollider>();
        try
        {
            meshCollider.sharedMesh = mesh;

            // bug:エラーをcatch出来ていないっぽい？ その影響で不正な範囲選択が停止されず実行されている
            // starposとendposの座標確認もしくはupdate selection内のインデクッスの確認で不正な操作は防がれているはず
            // [Physics.PhysX] QuickHullConvexHullLib::findSimplex: Simplex input points appers to be coplanar.
            // UnityEngine.StackTraceUtility:ExtractStackTrace ()
            meshCollider.convex = true;
        }
        catch
        {
            Debug.LogWarning("MeshColliderの設定中にエラーが発生しました: ");
        }

        return meshCollider;
    }

    private void RenderPreviewSelectedToggle()
    {
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 14; // 文字のサイズを大きくする

        GUILayout.Label(LocalizationEditor.GetLocalizedText("PreviewModeLabel") + (_isPreviewSelected ? LocalizationEditor.GetLocalizedText("SelectedMesh") : LocalizationEditor.GetLocalizedText("UnselectedMesh")), labelStyle);

        if (GUILayout.Button(LocalizationEditor.GetLocalizedText("SwitchPreviewModeButton")))
        {
            bool isselected = !_isPreviewSelected;
            ToggleSelectionSelected(isselected);
        }
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
        GUILayout.Label($"{_triangleSelectionManager.GetSelectedTriangles().Count}/{_triangleSelectionManager.GetAllTriangles().Count}");
    }

    private void RenderSelectionButtons()
    {
        GUILayout.BeginHorizontal();
    
        GUI.enabled = _islandUtility != null && _isPreviewEnabled;
        if (GUILayout.Button(LocalizationEditor.GetLocalizedText("SelectAllButton")))
        {
            _triangleSelectionManager.SelectAllTriangles();
            UpdateMesh();
        }

        if (GUILayout.Button(LocalizationEditor.GetLocalizedText("UnselectAllButton")))
        {
            _triangleSelectionManager.UnselectAllTriangles();
            UpdateMesh();
        }

        if (GUILayout.Button(LocalizationEditor.GetLocalizedText("ReverseAllButton")))
        {
            _triangleSelectionManager.ReverseAllTriangles();
            UpdateMesh();
        }
        GUI.enabled = true;

        GUILayout.EndHorizontal();
    }

    private void RenderSelectionMode()
    {
        string[] options = { LocalizationEditor.GetLocalizedText("SelectionMode.Island"), LocalizationEditor.GetLocalizedText("SelectionMode.Polygon") };
        GUILayout.BeginHorizontal();
        GUILayout.Label(LocalizationEditor.GetLocalizedText("SelectionMode.description"));
        _SelectionModeIndex = EditorGUILayout.Popup(_SelectionModeIndex, options);
        GUILayout.EndHorizontal();
        //string optt = options[_SelectionModeIndex];
    }


    private void process_options()
    {
        EditorGUILayout.Space();

        if (_SelectionModeIndex == 0)
        {
            RenderislandDescription();
            _mergeSamePosition = !EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("SplitMeshMoreToggle"), !_mergeSamePosition);
            EditorGUILayout.HelpBox(LocalizationEditor.GetLocalizedText("tooltip.SplitMeshMoreToggle"), MessageType.Info);
            _isAll = !EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("SelectAllInRangeToggle"), !_isAll);
            EditorGUILayout.HelpBox(LocalizationEditor.GetLocalizedText("tooltip.SelectAllInRangeToggle"), MessageType.Info);
        }
        else if (_SelectionModeIndex == 1)
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
            bool isenabled = !_isPreviewEnabled;
            ToggleSelectionEnabled(isenabled);
        }
    }

    private void RenderUndoRedoButtons()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(LocalizationEditor.GetLocalizedText("UndoButton")))
        {
            PerformUndo();
        }

        if (GUILayout.Button(LocalizationEditor.GetLocalizedText("RedoButton")))
        {
            PerformRedo();
        }

        EditorGUILayout.EndHorizontal();

    }


    public void ResetAllBlendShapes(SkinnedMeshRenderer skinnedMeshRenderer)
    {
        int blendShapeCount = skinnedMeshRenderer.sharedMesh.blendShapeCount;
        for (int i = 0; i < blendShapeCount; i++)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(i, 0f);
        }
    }

    private void DuplicateAndSetup()
    {
        _RootObject = CheckRoot(_OriginskinnedMeshRenderer.gameObject);
        _originalMesh = _OriginskinnedMeshRenderer.sharedMesh;

        //ResetAllBlendShapes(_OriginskinnedMeshRenderer);

        Vector3 parentScale = _RootObject.transform.localScale;
        _OriginskinnedMeshRenderer.transform.localScale = new Vector3(1 / parentScale.x, 1 / parentScale.y, 1 / parentScale.z);

        _bakedMesh = new Mesh(); 
        _OriginskinnedMeshRenderer.BakeMesh(_bakedMesh);

        Mesh PreviewMesh = Instantiate(_originalMesh);
        PreviewMesh.name += "AO Preview";
        MeshPreview.StartPreview(_OriginskinnedMeshRenderer);

    }

    private GameObject CheckRoot(GameObject targetObject)
    {
        //親オブジェクトが存在するか確認
        Transform parent = targetObject.transform.parent;
        if (parent == null)
        {
            throw new InvalidOperationException("Please select the object with SkinnedMeshRenderer directly under the avatar/costume");
        }

        GameObject root;
        if (PrefabUtility.IsPartOfPrefabInstance(targetObject))
        {
            root = PrefabUtility.GetOutermostPrefabInstanceRoot(targetObject);
        }
        else
        {
            root = parent.gameObject;
        }
        return root;
    }

    private void UpdateMesh()
    {   
        Mesh PreviewMesh;
        Mesh colliderMesh;

        HashSet<int> KeeptriangleIndices = _isPreviewSelected ?
            _triangleSelectionManager.GetSelectedTriangles() : 
            _triangleSelectionManager.GetUnselectedTriangles();

        PreviewMesh = MeshUtility.RemoveTriangles(_originalMesh, KeeptriangleIndices);
        _OriginskinnedMeshRenderer.sharedMesh = PreviewMesh;

        if (_isPreviewEnabled)
        {
            if (KeeptriangleIndices.Count > 0)
            {
                (colliderMesh, _oldToNewIndexMap) = MeshUtility.ProcesscolliderMesh(_bakedMesh, KeeptriangleIndices);
                SceneRaycastUtility.UpdateColider(colliderMesh);
            }
            else
            {
                colliderMesh = null;
                SceneRaycastUtility.UpdateColider(colliderMesh);
            }
        }

        Repaint();
    }

    private void PerformUndo()
    {
        _triangleSelectionManager.Undo();
        UpdateMesh();
    }

    private void PerformRedo()
    {
        _triangleSelectionManager.Redo();
        UpdateMesh();       
    }
}