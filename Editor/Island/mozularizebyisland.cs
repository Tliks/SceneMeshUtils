using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Color = UnityEngine.Color;


public class ModuleCreatorIsland : EditorWindow
{
    
    private List<List<Island>> _islands = new List<List<Island>>();
    [SerializeField] private List<int> _unselected_Island_Indcies = new List<int>();
    [SerializeField] private List<int> _selected_Island_Indcies = new List<int>();

    private SkinnedMeshRenderer _OriginskinnedMeshRenderer;
    private SkinnedMeshRenderer _PreviewSkinnedMeshRenderer;
    private Mesh _bakedMesh;
    private Mesh _BacksideMesh;
    private GameObject _PreviewMeshObject;
    private List<int> _PreviousIslandIndices = new List<int>();

    private static ModuleCreatorSettings _Settings;

    private const int MENU_PRIORITY = 49;
    private const double raycastInterval = 0.01;
    private double _lastUpdateTime = 0;

    private bool _showAdvancedOptions = false;
    private bool _isselectionEnabled;
    private bool _isGameObjectContext = false;

    private HighlightEdgesManager _highlightManager;

    private Stopwatch _stopwatch = new Stopwatch();

    private string _textFieldValue;

    public bool _mergeSamePosition = true;
    private MeshCollider _PreviewMeshCollider;
    private bool _isPreviewSelected = false;
    private int _total_islands_index = 0;
    private int _Selected_Vertices_Count;
    private int _Total_Vertices_Count;
    private Vector2 _startPoint;
    private Rect _selectionRect = new Rect();
    private bool _isdragging = false;
    private const float dragThreshold = 10f;
    private bool _isAll = true;
    private Vector2 _scrollPosition;
    private const float minZOffset = 5f;     



    [MenuItem("Window/Module Creator/Modularize Mesh by Island")]
    public static void ShowWindow()
    {
        var window = GetWindow<ModuleCreatorIsland>("Module Creator");
        window.InitializeFromMenuItem();
    }

    [MenuItem("GameObject/Module Creator/Modularize Mesh by Island", false, MENU_PRIORITY)]
    public static void ShowWindowFromGameObject()
    {
        var window = GetWindow<ModuleCreatorIsland>("Module Creator");
        if (Selection.activeGameObject != null)
        {
            window._OriginskinnedMeshRenderer = Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>();
            if (window._OriginskinnedMeshRenderer != null)
            {
                window.InitializeFromGameObject();
                window.AutoSetup();
            }
        }
    }

    [MenuItem("GameObject/Module Creator/Modularize Mesh by Island", true)]
    private static bool ValidateShowWindowFromGameObject()
    {
        return Selection.activeGameObject != null && Selection.activeGameObject.transform.parent != null;
    }

    private void SaveUndoState()
    {
        Undo.RecordObject(this, "State Change");
    }

    // Initialization Methods
    private void InitializeFromMenuItem()
    {
        ResetState();
        _OriginskinnedMeshRenderer = null;
        _isGameObjectContext = false;
        Repaint();
    }

    private void InitializeFromGameObject()
    {
        ResetState();
        _isGameObjectContext = true;
        Repaint();
    }

    private void ResetState()
    {
        _islands.Clear();
        _unselected_Island_Indcies.Clear();
        _selected_Island_Indcies.Clear();
    }

    private void OnGUI()
    {
        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        LocalizationEditor.RenderLocalize();

        if (!_isGameObjectContext)
        {
            RenderGUI();
            RenderSetupButtons();
        }

        EditorGUILayout.Space();
        RenderVertexCount();
        EditorGUILayout.Space();

        RenderSelectionButtons();
        RenderDescription();
        process_options();

        RenderPreviewSelectedToggle();

        RenderPhysBoneOptions();

        EditorGUILayout.Space();

        RenderCreateModuleButtons();
        EditorGUILayout.Space();
        
        process_advanced_options();

        EditorGUILayout.EndScrollView();
    }

    private void CreateModule(List<int> islandIndices)
    {
        Debug.Log(_textFieldValue);
        SaveUndoState();
        if (islandIndices.Count > 0)
        {
            var allVertices = IslandUtility.GetVerticesFromIndices(_islands, islandIndices);
            SaveModule(allVertices.ToList());
        }

        ToggleSelectionMode(false);
    }
    private void SaveModule(List<int> vertices)
    {
        _stopwatch.Restart();
        Mesh newMesh = MeshDeletionUtility.DeleteMesh(_OriginskinnedMeshRenderer, vertices, true);
        _stopwatch.Stop();
        //Debug.Log($"Delete Mesh: {stopwatch.ElapsedMilliseconds} ms");

        _stopwatch.Restart();
        string path = GenerateVariantPath(_OriginskinnedMeshRenderer.transform.parent.gameObject, "newMesh");
        AssetDatabase.CreateAsset(newMesh, path);
        AssetDatabase.SaveAssets();
        _stopwatch.Stop();
        //Debug.Log($"Save newMesh: {stopwatch.ElapsedMilliseconds} ms");

        _Settings.newmesh = newMesh;
        _stopwatch.Restart();
        new ModuleCreator(_Settings).CheckAndCopyBones(_OriginskinnedMeshRenderer.gameObject);
        _stopwatch.Stop();

        string GenerateVariantPath(GameObject root_object, string source_name)
        {
            string base_path = $"Assets/ModuleCreator";
            if (!AssetDatabase.IsValidFolder(base_path))
            {
                AssetDatabase.CreateFolder("Assets", "ModuleCreator");
                AssetDatabase.Refresh();
            }
            
            string folderPath = $"{base_path}/{root_object.name}";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(base_path, root_object.name);
                AssetDatabase.Refresh();
            }

            string folderPath1 = $"{folderPath}/Mesh";
            if (!AssetDatabase.IsValidFolder(folderPath1))
            {
                AssetDatabase.CreateFolder(folderPath, "Mesh");
                AssetDatabase.Refresh();
            }

            string fileName = source_name;
            string fileExtension = "asset";
            
            return AssetDatabase.GenerateUniqueAssetPath(folderPath1 + "/" + fileName + "." + fileExtension);
        }

    }

    private void ValidateAndApplyNewValue(string newValue)
    {
        int[] decodedArray = IntArrayConverter.Decode(newValue);
        int total_islands_count = _total_islands_index + 1;
        
        if (decodedArray != null && decodedArray[decodedArray.Length - 1] == total_islands_count) // 最後の要素が島の数と一致するか確認
        {
            _selected_Island_Indcies = new List<int>(decodedArray.Take(decodedArray.Length - 1)); // 最後の要素を除外
            _unselected_Island_Indcies = _unselected_Island_Indcies.Except(_selected_Island_Indcies).ToList();
            UpdateMesh();
            _textFieldValue = newValue;
            GUI.FocusControl(""); 
        }
        else
        {
            if (decodedArray == null)
            {
                Debug.LogWarning("Island Hashのデコードに失敗しました。無効な形式です。");
            }
            else if (decodedArray[decodedArray.Length - 1] != total_islands_count)
            {
                Debug.LogWarning($"Island Hashのデコード成功しましたが、島の数が一致しません。デコードされた島の数 (最後の要素): {decodedArray[decodedArray.Length - 1]}, 現在の島の数: {total_islands_count}");
            }
        }
    }

    private void UpdateEncodedString()
    {   
        int total_islands_count = _total_islands_index + 1;
        _selected_Island_Indcies.Sort();
        string encodedString = IntArrayConverter.Encode(_selected_Island_Indcies.Append(total_islands_count).ToArray());
        if (encodedString != _textFieldValue)
        {
            _textFieldValue = encodedString;
            GUI.FocusControl(""); 
        }
    }

    private void SelectAllIslands()
    {
        SaveUndoState();
        _selected_Island_Indcies = Enumerable.Range(0, _total_islands_index).ToList();
        _unselected_Island_Indcies.Clear();
        UpdateMesh();
    }

    private void UnselectAllIslands()
    {
        SaveUndoState();
        _unselected_Island_Indcies = Enumerable.Range(0, _total_islands_index).ToList();
        _selected_Island_Indcies.Clear();
        UpdateMesh();
    }

    private void ReverseAllIslands()
    {
        SaveUndoState();
        var temp = new List<int>(_selected_Island_Indcies);
        _selected_Island_Indcies = new List<int>(_unselected_Island_Indcies);
        _unselected_Island_Indcies = temp;
        UpdateMesh();
    }
    private void AutoSetup()
    {
        DuplicateAndSetup();
        CalculateIslands();
        ToggleSelectionMode(true);
    }

    private void OnEnable()
    {
        _Settings = new ModuleCreatorSettings
        {
            IncludePhysBone = true,
            IncludePhysBoneColider = true
        };
        
        SceneView.duringSceneGui += OnSceneGUI;
        ToggleSelectionMode(false);

        processend();
        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.RepaintAll();
        processend();
        FocusCustomViewObject(_OriginskinnedMeshRenderer.transform, _OriginskinnedMeshRenderer.sharedMesh, Quaternion.LookRotation(Vector3.back));
        Undo.undoRedoPerformed -= OnUndoRedo;
    }

    private void OnUndoRedo()
    {
        UpdateMesh();
    }

    private void processend()
    {
        if (_highlightManager != null)
        {
            DestroyImmediate(_highlightManager);
        }
        if (_PreviewSkinnedMeshRenderer != null)
        {
            DestroyImmediate(_PreviewMeshObject);
        }
        ToggleSelectionMode(false);
        RemoveHighlight();
        CloseCustomSceneView();
        //Close();
    }
    
    private void FocusCustomViewObject(Transform transform, Mesh mesh, Quaternion rotation)
    {
        Vector3 middleVertex = Vector3.zero;

        if (mesh != null)
        {
            Vector3[] vertices = mesh.vertices;
            middleVertex = vertices
                .Select(v => transform.TransformPoint(v))
                .Aggregate((acc, v) => acc + v) / vertices.Length;
        }

        float cameraDistance = 0.3f;
        Vector3 direction = SceneView.lastActiveSceneView.camera.transform.forward;
        Vector3 newCameraPosition = middleVertex - direction * cameraDistance;

        //Debug.Log(middleVertex);
        SceneView.lastActiveSceneView.LookAt(middleVertex, rotation, cameraDistance);

        SceneView.lastActiveSceneView.Repaint();
    }



    private void EnsureHighlightManagerExists()
    {
        if (_highlightManager == null)
        {
            _highlightManager = _PreviewSkinnedMeshRenderer.gameObject.AddComponent<HighlightEdgesManager>();
        }
    }

    private void ToggleSelectionMode(bool newMode)
    {
        if (_isselectionEnabled == newMode)
        {
            //Debug.LogWarning("current mode is already specofed mode");
        }
        else
        {
            _isselectionEnabled = newMode;
        }

        if (_isselectionEnabled == true)
        {   
            _PreviewMeshCollider = SceneRaycastUtility.AddCollider(_PreviewSkinnedMeshRenderer);
            EnsureHighlightManagerExists();
            UpdateMesh(); // コライダーのメッシュを更新
            SceneView.lastActiveSceneView.drawGizmos = true;
        }
        else
        {
            SceneRaycastUtility.DeleteCollider(_PreviewMeshCollider);
            RemoveHighlight();
        }

    }

    private void CalculateIslands()
    {
        _stopwatch.Restart();
        _islands = IslandUtility.GetIslands(_bakedMesh);
        _stopwatch.Stop();
        _total_islands_index = GetTotalElementCount(_islands);
        Debug.Log($"Islands Merged: {_islands.Count} of {_total_islands_index + 1} - Elapsed Time: {_stopwatch.ElapsedMilliseconds} ms");
        _selected_Island_Indcies.Clear();
        _unselected_Island_Indcies = Enumerable.Range(0, _total_islands_index).ToList(); 
        _Total_Vertices_Count = IslandUtility.GetVerticesFromIndices(_islands, _unselected_Island_Indcies).Count;
    }

    private int GetTotalElementCount<T>(List<List<T>> lists)
    {
        return lists.Sum(innerList => innerList.Count);
    }

    private void RemoveHighlight()
    {
        if (_highlightManager != null)
        {
            DestroyImmediate(_highlightManager);
            SceneView.RepaintAll();
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (_PreviewSkinnedMeshRenderer == null) Close();
        if (!_isselectionEnabled) return;
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        Event e = Event.current;
        //DontActiveSKin(e);
        HandleUndoRedoEvent(e);
        HandleMouseEvents(e, sceneView);
        DrawSelectionRectangle();
    }

    private void DontActiveSKin(Event e)
    {
        if (e != null && Selection.activeGameObject != null)
        {
            GameObject currentActiveObject = Selection.activeGameObject;
            if (currentActiveObject == _PreviewSkinnedMeshRenderer.gameObject)
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
                Undo.PerformUndo();
                e.Use();
            }
            else if (e.keyCode == KeyCode.Y) // Ctrl/Cmd + Y
            {
                Undo.PerformRedo();
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

        if (!sceneViewRect.Contains(mousePos))
        {
            HighlightNull();
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
                HandleClick();
            }
            //ドラッグ解放
            else
            {
                Vector2 endPoint = mousePos;
                HandleDrag(_startPoint, endPoint);
            }
            
            _isdragging = false;
            _selectionRect = new Rect();
            DrawSelectionRectangle();

        }
        //ドラッグ中
        else if (e.type == EventType.MouseDrag && e.button == 0 && Vector2.Distance(_startPoint, mousePos) >= dragThreshold)
        {
            _isdragging = true;
            HighlightNull();
            _selectionRect = new Rect(_startPoint.x, _startPoint.y, mousePos.x - _startPoint.x, mousePos.y - _startPoint.y);
            HandleUtility.Repaint();
        }
        //ドラッグしていないとき
        else if (!_isdragging)
        {
            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - _lastUpdateTime >= raycastInterval)
            {
                _lastUpdateTime = currentTime;
                PerformRaycast();
            }
        }

        //sceneviewの外側にある場合の初期化処理
    }

    private void DrawSelectionRectangle()
    {
        Handles.BeginGUI();
        Color selectionColor = _isPreviewSelected ? new Color(1, 0, 0, 0.2f) : new Color(0, 1, 1, 0.2f);
        GUI.color = selectionColor;
        GUI.DrawTexture(_selectionRect, EditorGUIUtility.whiteTexture);
        Handles.EndGUI();
    }

    private void HandleClick()
    {
        UpdateSelection(_PreviousIslandIndices);
        HighlightNull();
    }

    private void PerformRaycast()
    {
        bool conditionMet = false;
        if (SceneRaycastUtility.TryRaycast(out RaycastHit hitInfo))
        {
            if (SceneRaycastUtility.IsHitObject(_PreviewSkinnedMeshRenderer.gameObject, hitInfo))
            {
                int triangleIndex = hitInfo.triangleIndex;
                List<int> indices = IslandUtility.GetIslandIndexFromTriangleIndex(_BacksideMesh, triangleIndex, _islands, _mergeSamePosition);
                if (_mergeSamePosition) indices = _isPreviewSelected ? indices.Intersect(_selected_Island_Indcies).ToList() : indices.Intersect(_unselected_Island_Indcies).ToList();
                if (indices.Count > 0 && indices != _PreviousIslandIndices)
                {
                    _PreviousIslandIndices = indices;
                    Color color = _isPreviewSelected ? Color.red : Color.cyan;
                    HighlightIslandEdges(_PreviewSkinnedMeshRenderer.transform, _bakedMesh.vertices, color, indices);
                    conditionMet = true;
                }
            }
        }
        if (!conditionMet)
        {
            HighlightNull();
        }
    }

    private void HighlightIslandEdges(Transform transform, Vector3[] vertices, Color color, List<int> islandIndices)
    {
        HashSet<(int, int)> edgesToHighlight = IslandUtility.GetEdgesFromIndices(_islands, islandIndices);
        _highlightManager.HighlightEdges(edgesToHighlight, vertices, color, transform);
    }

    private void HighlightNull()
    {
        _PreviousIslandIndices.Clear();
        HashSet<(int, int)> edgesToHighlight = new HashSet<(int, int)>();
        _highlightManager.HighlightEdges(edgesToHighlight, _bakedMesh.vertices, Color.cyan, _PreviewSkinnedMeshRenderer.transform);
    }

    private void HandleDrag(Vector2 startpos, Vector2 endpos)
    {
        MeshCollider meshCollider = GenerateColider(startpos, endpos);
        List<int> indices = IslandUtility.GetIslandIndicesInColider(_bakedMesh, meshCollider, _islands, _mergeSamePosition, _isAll, _PreviewSkinnedMeshRenderer.transform);
        //Debug.Log(indices.Count);
        DestroyImmediate(meshCollider.gameObject);
        UpdateSelection(indices);
    }

    private MeshCollider GenerateColider(Vector2 startpos, Vector2 endpos)
    {
        Vector2 corner2 = new Vector2(startpos.x, endpos.y);
        Vector2 corner4 = new Vector2(endpos.x, startpos.y);
        
        Ray ray1 = HandleUtility.GUIPointToWorldRay(startpos);
        Ray ray2 = HandleUtility.GUIPointToWorldRay(corner2);
        Ray ray3 = HandleUtility.GUIPointToWorldRay(endpos);
        Ray ray4 = HandleUtility.GUIPointToWorldRay(corner4);
        float depth = 10f;

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
            //裏面ポリゴンだとcollider.ClosestPointがうまく動作しないことがある
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
            meshCollider.convex = true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("MeshColliderの設定中にエラーが発生しました: " + e.Message);
        }

        return meshCollider;
    }


    private void UpdateSelection(List<int> indices)
    {
        SaveUndoState();
        foreach (var index in indices)
        {
            if (_isPreviewSelected)
            {
                _unselected_Island_Indcies.Add(index);
                _selected_Island_Indcies.Remove(index);
            }
            else
            {
                _selected_Island_Indcies.Add(index);
                _unselected_Island_Indcies.Remove(index);
            }
        }
        UpdateMesh();
    }

    private void RenderPreviewSelectedToggle()
    {
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 14; // 文字のサイズを大きくする

        GUILayout.Label(LocalizationEditor.GetLocalizedText("PreviewModeLabel") + (_isPreviewSelected ? LocalizationEditor.GetLocalizedText("SelectedMesh") : LocalizationEditor.GetLocalizedText("UnselectedMesh")), labelStyle);

        if (GUILayout.Button(LocalizationEditor.GetLocalizedText("SwitchPreviewModeButton")))
        {
            _isPreviewSelected = !_isPreviewSelected;
            UpdateMesh();
        }
    }

    private void RenderGUI()
    {
        SkinnedMeshRenderer newskinnedMeshRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(LocalizationEditor.GetLocalizedText("SkinnedMeshRendererField"), _OriginskinnedMeshRenderer, typeof(SkinnedMeshRenderer), true);
        if (_OriginskinnedMeshRenderer == newskinnedMeshRenderer || _OriginskinnedMeshRenderer == null)
        {
            _OriginskinnedMeshRenderer = newskinnedMeshRenderer;
        }
        else
        {
            FocusCustomViewObject(_OriginskinnedMeshRenderer.transform, _OriginskinnedMeshRenderer.sharedMesh, SceneView.lastActiveSceneView.rotation);
            processend();
            _OriginskinnedMeshRenderer = newskinnedMeshRenderer;
        }
    }

    private void RenderSetupButtons()
    {
        GUI.enabled = _OriginskinnedMeshRenderer != null;
        if (GUILayout.Button(LocalizationEditor.GetLocalizedText("PreviewMeshButton")))
        {
            DuplicateAndSetup();
        }
        GUI.enabled = true;

        GUI.enabled = _PreviewSkinnedMeshRenderer != null;
        if (GUILayout.Button(LocalizationEditor.GetLocalizedText("CalculateIslandsButton")))
        {
            CalculateIslands();
        }
        GUI.enabled = true;
    }

    private void RenderDescription()
    {
        //EditorGUILayout.Space();
        EditorGUILayout.HelpBox(LocalizationEditor.GetLocalizedText("description"), MessageType.Info);
        //EditorGUILayout.Space();
    }

    private void RenderVertexCount()
    {
        GUILayout.Label(LocalizationEditor.GetLocalizedText("SelectedTotalPolygonsLabel"), EditorStyles.boldLabel);
        GUILayout.Label($"{_Selected_Vertices_Count}/{_Total_Vertices_Count}");
    }

    private void RenderSelectionButtons()
    {
        GUILayout.BeginHorizontal();
    
        GUI.enabled = _islands.Count > 0 && _isselectionEnabled == true;
        if (GUILayout.Button(LocalizationEditor.GetLocalizedText("SelectAllButton")))
        {
            SelectAllIslands();
        }

        if (GUILayout.Button(LocalizationEditor.GetLocalizedText("UnselectAllButton")))
        {
            UnselectAllIslands();
        }

        if (GUILayout.Button(LocalizationEditor.GetLocalizedText("ReverseAllButton")))
        {
            ReverseAllIslands();
        }
        GUI.enabled = true;

        GUILayout.EndHorizontal();
    }

    private void RenderCreateModuleButtons()
    {
        GUI.enabled = _OriginskinnedMeshRenderer != null && _selected_Island_Indcies.Count > 0;
        
        // Create Selected Islands Module
        if (GUILayout.Button(LocalizationEditor.GetLocalizedText("CreateModuleButton")))
        {
            CreateModule(_selected_Island_Indcies);
            if (_isGameObjectContext) Close();
        }

        GUI.enabled = true;
    }

    private void process_options()
    {
        EditorGUILayout.Space();

        _mergeSamePosition = !EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("SplitMeshMoreToggle"), !_mergeSamePosition);
        EditorGUILayout.HelpBox(LocalizationEditor.GetLocalizedText("tooltip.SplitMeshMoreToggle"), MessageType.Info);
        _isAll = !EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("SelectAllInRangeToggle"), !_isAll);
        EditorGUILayout.HelpBox(LocalizationEditor.GetLocalizedText("tooltip.SelectAllInRangeToggle"), MessageType.Info);

        EditorGUILayout.Space();

    }

    private void RenderPhysBoneOptions()
    {
        EditorGUILayout.Space();

        _Settings.IncludePhysBone = EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("PhysBoneToggle"), _Settings.IncludePhysBone);

        GUI.enabled = _Settings.IncludePhysBone;
        _Settings.IncludePhysBoneColider = EditorGUILayout.Toggle(LocalizationEditor.GetLocalizedText("PhysBoneColiderToggle"), _Settings.IncludePhysBoneColider);
        GUI.enabled = true;
    }
    
    private void process_advanced_options()
    {   

        _showAdvancedOptions = EditorGUILayout.Foldout(_showAdvancedOptions, LocalizationEditor.GetLocalizedText("advancedoptions"));
        if (_showAdvancedOptions)
        {
            GUI.enabled = _Settings.IncludePhysBone;
            GUIContent content_at = new GUIContent(LocalizationEditor.GetLocalizedText("AdditionalTransformsToggle"), LocalizationEditor.GetLocalizedText("tooltip.AdditionalTransformsToggle"));
            _Settings.RemainAllPBTransforms = EditorGUILayout.Toggle(content_at, _Settings.RemainAllPBTransforms);

            GUIContent content_ii = new GUIContent(LocalizationEditor.GetLocalizedText("IncludeIgnoreTransformsToggle"), LocalizationEditor.GetLocalizedText("tooltip.IncludeIgnoreTransformsToggle"));
            _Settings.IncludeIgnoreTransforms = EditorGUILayout.Toggle(content_ii, _Settings.IncludeIgnoreTransforms);

            GUIContent content_rr = new GUIContent(
                LocalizationEditor.GetLocalizedText("RenameRootTransformToggle"),
                LocalizationEditor.GetLocalizedText("tooltip.RenameRootTransformToggle"));
            _Settings.RenameRootTransform = EditorGUILayout.Toggle(content_rr, _Settings.RenameRootTransform);

            GUI.enabled = true;

            GUIContent content_sr = new GUIContent(LocalizationEditor.GetLocalizedText("SpecifyRootObjectLabel"), LocalizationEditor.GetLocalizedText("tooltip.SpecifyRootObjectLabel"));
            _Settings.RootObject = (GameObject)EditorGUILayout.ObjectField(content_sr, _Settings.RootObject, typeof(GameObject), true);
            
            RenderIslandHashField();
            RenderModeoff();
            RenderUndoRedoButtons();
            RenderCreateBothModuleButtons();
        }

        EditorGUILayout.Space();
    }

    private void RenderIslandHashField()
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(LocalizationEditor.GetLocalizedText("EncodedIslandsLabel"));
        string newValue = EditorGUILayout.TextField(_textFieldValue);

        if (newValue != _textFieldValue)
        {
            ValidateAndApplyNewValue(newValue);
        }
        EditorGUILayout.EndHorizontal();

    }

    private void RenderModeoff()
    {
        if (GUILayout.Button(_isselectionEnabled == false ? LocalizationEditor.GetLocalizedText("EnableSelectionButton") : LocalizationEditor.GetLocalizedText("DisableSelectionButton")))
        {
            if (_isselectionEnabled == false)
            {
                ToggleSelectionMode(true);
            }
            else
            {
                ToggleSelectionMode(false);
            }
        }
    }

    private void RenderUndoRedoButtons()
    {
        GUILayout.BeginHorizontal();

        bool performUndo = false;
        bool performRedo = false;

        try
        {
            if (GUILayout.Button(LocalizationEditor.GetLocalizedText("UndoButton")))
            {
                performUndo = true;
            }

            if (GUILayout.Button(LocalizationEditor.GetLocalizedText("RedoButton")))
            {
                performRedo = true;
            }
        }
        finally
        {
            GUILayout.EndHorizontal();
        }

        if (performUndo)
        {
            Undo.PerformUndo();
        }

        if (performRedo)
        {
            Undo.PerformRedo();
        }
    }

    private void RenderCreateBothModuleButtons()
    {
        GUI.enabled = _OriginskinnedMeshRenderer != null && _selected_Island_Indcies.Count > 0;

        // Create Both Modules
        if (GUILayout.Button(LocalizationEditor.GetLocalizedText("CreateBothModulesButton")))
        {
            CreateModule(_selected_Island_Indcies);
            CreateModule(_unselected_Island_Indcies);
            processend();
            if (_isGameObjectContext) Close();
        }

        GUI.enabled = true;
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
        ModuleCreatorSettings settings = new ModuleCreatorSettings
        {
            IncludePhysBone = false,
            IncludePhysBoneColider = false
        };
        _PreviewSkinnedMeshRenderer = new ModuleCreator(settings).PreviewMesh(_OriginskinnedMeshRenderer.gameObject);

        ResetAllBlendShapes(_PreviewSkinnedMeshRenderer);
        _PreviewMeshObject = _PreviewSkinnedMeshRenderer.transform.parent.gameObject;
        float zOffset = _PreviewSkinnedMeshRenderer.bounds.size.z + minZOffset;
        _PreviewMeshObject.transform.position = _PreviewMeshObject.transform.position + new Vector3(0, 0, -zOffset);
        _PreviewMeshObject.name = "Preview Mesh";

        Vector3 parentScale = _PreviewMeshObject.transform.localScale;
        _PreviewSkinnedMeshRenderer.transform.localScale = new Vector3(1 / parentScale.x, 1 / parentScale.y, 1 / parentScale.z);

        _bakedMesh = new Mesh(); 
        _PreviewSkinnedMeshRenderer.BakeMesh(_bakedMesh);
        _BacksideMesh = MeshDeletionUtility.GenerateBacksideMesh(_bakedMesh);

        FocusCustomViewObject(_PreviewSkinnedMeshRenderer.transform, _bakedMesh, SceneView.lastActiveSceneView.rotation);

        Selection.activeGameObject = null;
    }

    private void CloseCustomSceneView()
    {
        if (_PreviewMeshObject != null)
        {
            DestroyImmediate(_PreviewMeshObject);
            _PreviewMeshObject = null;
        }
    }

    private void UpdateMesh()
    {
        List<int> vertices;
        Mesh previewMesh;

        if (_isPreviewSelected)
        {
            vertices = IslandUtility.GetVerticesFromIndices(_islands, _selected_Island_Indcies);
            _Selected_Vertices_Count = vertices.Count;
        }
        else
        {
            vertices = IslandUtility.GetVerticesFromIndices(_islands, _unselected_Island_Indcies);
            _Selected_Vertices_Count = _Total_Vertices_Count - vertices.Count;
        }

        previewMesh = MeshDeletionUtility.KeepVerticesUsingDegenerateTriangles(_OriginskinnedMeshRenderer.sharedMesh, vertices, true);
        _PreviewSkinnedMeshRenderer.sharedMesh = previewMesh;

        if (_isselectionEnabled)
        {
            if (vertices.Count >= 3)
            {
                Mesh colliderMesh = MeshDeletionUtility.KeepVerticesUsingDegenerateTriangles(_BacksideMesh, vertices, false);
                _PreviewMeshCollider.sharedMesh = colliderMesh;
            }
            else
            {
                _PreviewMeshCollider.sharedMesh = null;
            }
        }

        Repaint();
        UpdateEncodedString();
    }
}