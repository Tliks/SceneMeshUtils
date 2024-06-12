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
    
    private List<List<Island>> islands = new List<List<Island>>();
    [SerializeField] private List<int> unselected_Island_Indcies = new List<int>();
    [SerializeField] private List<int> selected_Island_Indcies = new List<int>();

    public SkinnedMeshRenderer OriginskinnedMeshRenderer;
    private SkinnedMeshRenderer PreviewSkinnedMeshRenderer;
    private Mesh bakedMesh;
    private GameObject PreviewMeshObject;
    private List<int> PreviousIslandIndices = new List<int>();

    private static ModuleCreatorSettings _Settings;

    private const int MENU_PRIORITY = 49;
    private double lastUpdateTime = 0;
    private const double raycastInterval = 0.01;

    private bool showAdvancedOptions = false;
    private bool selectionMode;
    private bool isGameObjectContext = false;

    private HighlightEdgesManager highlightManager;

    private Stopwatch stopwatch = new Stopwatch();

    private static string textFieldValue;

    public bool mergeSamePosition = true;
    private MeshCollider PreviewMeshCollider;
    private bool isPreviewSelected = false;
    private int total_islands_index = 0;
    List<int> Selected_Vertices = new List<int>();
    List<int> Total_Vertices = new List<int>();
    private Vector2 startPoint;
    private Rect selectionRect = new Rect();
    private bool isSelecting = false;
    private const float dragThreshold = 10f;
    private bool isAll = true;


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
            window.OriginskinnedMeshRenderer = Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>();
            if (window.OriginskinnedMeshRenderer != null)
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
        OriginskinnedMeshRenderer = null;
        isGameObjectContext = false;
        Repaint();
    }

    private void InitializeFromGameObject()
    {
        ResetState();
        isGameObjectContext = true;
        Repaint();
    }

    private void ResetState()
    {
        islands.Clear();
        unselected_Island_Indcies.Clear();
        selected_Island_Indcies.Clear();
    }

    private void OnGUI()
    {
        if (!isGameObjectContext)
        {
            RenderGUI();
            RenderSetupButtons();
        }

        EditorGUILayout.Space();
        RenderVertexCount();
        EditorGUILayout.Space();

        RenderSelectionButtons();

        porcess_options();

        RenderPreviewSelectedToggle();
        EditorGUILayout.Space();
        RenderCreateModuleButtons();
    }

    private void RenderUndoRedoButtons()
    {
        GUILayout.BeginHorizontal();

        bool performUndo = false;
        bool performRedo = false;

        try
        {
            if (GUILayout.Button("Undo (Ctrl+Z)"))
            {
                performUndo = true;
            }

            if (GUILayout.Button("Redo (Ctrl+Y)"))
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

    private void RenderPreviewSelectedToggle()
    {
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 15; // 文字のサイズを大きくする
        //labelStyle.fontStyle = FontStyle.Bold; // ボールドにして明瞭にする

        GUILayout.Label("Preview Mode: " + (isPreviewSelected ? "Selected Mesh" : "Unselected Mesh"), labelStyle);

        if (GUILayout.Button("Switch Preview Mode"))
        {
            isPreviewSelected = !isPreviewSelected;
            UpdateMesh();
        }
    }

    private void RenderGUI()
    {
        SkinnedMeshRenderer newskinnedMeshRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Skinned Mesh Renderer", OriginskinnedMeshRenderer, typeof(SkinnedMeshRenderer), true);
        if (OriginskinnedMeshRenderer == newskinnedMeshRenderer || OriginskinnedMeshRenderer == null)
        {
            OriginskinnedMeshRenderer = newskinnedMeshRenderer;
        }
        else
        {
            FocusCustomViewObject(OriginskinnedMeshRenderer.transform, OriginskinnedMeshRenderer.sharedMesh);
            processend();
            OriginskinnedMeshRenderer = newskinnedMeshRenderer;
        }
    }

    private void RenderSetupButtons()
    {
        GUI.enabled = OriginskinnedMeshRenderer != null;
        if (GUILayout.Button("Preview Mesh"))
        {
            DuplicateAndSetup();
        }
        GUI.enabled = true;

        GUI.enabled = PreviewSkinnedMeshRenderer != null;
        if (GUILayout.Button("Calculate Islands"))
        {
            CalculateIslands();
        }
        GUI.enabled = true;
    }


    private void RenderModeoff()
    {
        if (GUILayout.Button(selectionMode == false ? "Enable Selection" : "Disable Selection"))
        {
            if (selectionMode == false)
            {
                ToggleSelectionMode(true);
            }
            else
            {
                ToggleSelectionMode(false);
            }
        }
    }

    private void RenderVertexCount()
    {
        GUILayout.Label("Selected/Total Polygons", EditorStyles.boldLabel);
        GUILayout.Label($"{Selected_Vertices.Count}/{Total_Vertices.Count}");
    }

    private void RenderSelectionButtons()
    {
        GUILayout.BeginHorizontal();

        GUI.enabled = islands.Count > 0;
        if (GUILayout.Button("Select All"))
        {
            SelectAllIslands();
        }

        if (GUILayout.Button("Unselect All"))
        {
            UnselectAllIslands();
        }

        if (GUILayout.Button("Reverse All"))
        {
            ReverseAllIslands();
        }
        GUI.enabled = true;

        GUILayout.EndHorizontal();
    }

    private void RenderCreateModuleButtons()
    {
        GUI.enabled = OriginskinnedMeshRenderer != null && selected_Island_Indcies.Count > 0;
        
        // Create Selected Islands Module
        if (GUILayout.Button("Create Module"))
        {
            CreateModule(selected_Island_Indcies);
            if (isGameObjectContext) Close();
        }

        GUI.enabled = true;
    }

    private void RenderCreateBothModuleButtons()
    {
        GUI.enabled = OriginskinnedMeshRenderer != null && selected_Island_Indcies.Count > 0;

        // Create Both Modules
        if (GUILayout.Button("Create Both Modules"))
        {
            CreateModule(selected_Island_Indcies);
            CreateModule(unselected_Island_Indcies);
            processend();
            if (isGameObjectContext) Close();
        }

        GUI.enabled = true;
    }

    private void CreateModule(List<int> islandIndices)
    {
        Debug.Log(textFieldValue);
        SaveUndoState();
        if (islandIndices.Count > 0)
        {
            var allVertices = GetVerticesFromIndices(islandIndices);
            SaveModule(allVertices.ToList());
        }

        ToggleSelectionMode(false);
    }
    private void SaveModule(List<int> vertices)
    {
        stopwatch.Restart();
        Mesh newMesh = MeshDeletionUtility.DeleteMeshByKeepVertices(OriginskinnedMeshRenderer, vertices);
        stopwatch.Stop();
        //Debug.Log($"Delete Mesh: {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        string path = GenerateVariantPath(OriginskinnedMeshRenderer.transform.parent.gameObject, "newMesh");
        AssetDatabase.CreateAsset(newMesh, path);
        AssetDatabase.SaveAssets();
        stopwatch.Stop();
        //Debug.Log($"Save newMesh: {stopwatch.ElapsedMilliseconds} ms");

        _Settings.newmesh = newMesh;
        stopwatch.Restart();
        new ModuleCreator(_Settings).CheckAndCopyBones(OriginskinnedMeshRenderer.gameObject);
        stopwatch.Stop();

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

    private void RenderIslandHashField()
    {
        int total_islands_count = total_islands_index + 1;
        GUILayout.Label("Encoded islands:", EditorStyles.boldLabel);
        string newValue = EditorGUILayout.TextField(textFieldValue);

        if (newValue != textFieldValue)
        {
            int[] decodedArray = IntArrayConverter.Decode(newValue);
            if (decodedArray != null && decodedArray[decodedArray.Length - 1] == total_islands_count) // 最後の要素が島の数と一致するか確認
            {
                selected_Island_Indcies = new List<int>(decodedArray.Take(decodedArray.Length - 1)); // 最後の要素を除外
                unselected_Island_Indcies = unselected_Island_Indcies.Except(selected_Island_Indcies).ToList();
                UpdateMesh();
                textFieldValue = newValue;
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
    }

    private void UpdateEncodedString()
    {   
        int total_islands_count = total_islands_index + 1;
        selected_Island_Indcies.Sort();
        string encodedString = IntArrayConverter.Encode(selected_Island_Indcies.Append(total_islands_count).ToArray());
        if (encodedString != textFieldValue)
        {
            textFieldValue = encodedString;
            GUI.FocusControl(""); 
        }
    }

    private void SelectAllIslands()
    {
        SaveUndoState();
        selected_Island_Indcies = Enumerable.Range(0, total_islands_index).ToList();
        unselected_Island_Indcies.Clear();
        UpdateMesh();
    }

    private void UnselectAllIslands()
    {
        SaveUndoState();
        unselected_Island_Indcies = Enumerable.Range(0, total_islands_index).ToList();
        selected_Island_Indcies.Clear();
        UpdateMesh();
    }

    private void ReverseAllIslands()
    {
        SaveUndoState();
        var temp = new List<int>(selected_Island_Indcies);
        selected_Island_Indcies = new List<int>(unselected_Island_Indcies);
        unselected_Island_Indcies = temp;
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
        FocusCustomViewObject(OriginskinnedMeshRenderer.transform, OriginskinnedMeshRenderer.sharedMesh);
        Undo.undoRedoPerformed -= OnUndoRedo;
    }

    private void OnUndoRedo()
    {
        UpdateMesh();
    }

    private void processend()
    {
        if (highlightManager != null)
        {
            DestroyImmediate(highlightManager);
        }
        if (PreviewSkinnedMeshRenderer != null)
        {
            DestroyImmediate(PreviewMeshObject);
        }
        ToggleSelectionMode(false);
        RemoveHighlight();
        CloseCustomSceneView();
        //Close();
    }
    
    private void FocusCustomViewObject(Transform transform, Mesh mesh)
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
        SceneView.lastActiveSceneView.LookAt(middleVertex, SceneView.lastActiveSceneView.rotation, cameraDistance);

        SceneView.lastActiveSceneView.Repaint();
    }



    private void EnsureHighlightManagerExists()
    {
        if (highlightManager == null)
        {
            highlightManager = PreviewSkinnedMeshRenderer.gameObject.AddComponent<HighlightEdgesManager>();
        }
    }

    private void ToggleSelectionMode(bool newMode)
    {
        if (selectionMode == newMode)
        {
            //Debug.LogWarning("current mode is already specofed mode");
        }
        else
        {
            selectionMode = newMode;
        }

        if (selectionMode == true)
        {   
            PreviewMeshCollider = SceneRaycastUtility.AddCollider(PreviewSkinnedMeshRenderer);
            EnsureHighlightManagerExists();
            UpdateMesh(); // コライダーのメッシュを更新
            SceneView.lastActiveSceneView.drawGizmos = true;
        }
        else
        {
            SceneRaycastUtility.DeleteCollider(PreviewMeshCollider);
            RemoveHighlight();
        }

    }

    private void CalculateIslands()
    {
        stopwatch.Restart();
        islands = IslandUtility.GetIslands(bakedMesh);
        stopwatch.Stop();
        total_islands_index = GetTotalElementCount(islands);
        Debug.Log($"Island Count: {islands.Count}/{total_islands_index + 1} - Time: {stopwatch.ElapsedMilliseconds} ms");
        selected_Island_Indcies.Clear();
        unselected_Island_Indcies = Enumerable.Range(0, total_islands_index).ToList(); 
        Total_Vertices = GetVerticesFromIndices(unselected_Island_Indcies);
    }

    private int GetTotalElementCount<T>(List<List<T>> lists)
    {
        return lists.Sum(innerList => innerList.Count);
    }

    private void RemoveHighlight()
    {
        if (highlightManager != null)
        {
            DestroyImmediate(highlightManager);
            SceneView.RepaintAll();
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (PreviewSkinnedMeshRenderer == null) Close();
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        Event e = Event.current;
        DontActiveSKin(e);
        HandleUndoRedoEvent(e);
        HandleMouseEvents(e, sceneView);
        DrawSelectionRectangle();
    }

    private void DontActiveSKin(Event e)
    {
        if (e != null && Selection.activeGameObject != null)
        {
            GameObject currentActiveObject = Selection.activeGameObject;
            if (currentActiveObject == PreviewSkinnedMeshRenderer.gameObject)
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
        Rect sceneViewRect = new Rect(0, 0, sceneView.position.width, sceneView.position.height);

        //左クリック
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            startPoint = mousePos;
        }
        //左クリック解放
        else if (e.type == EventType.MouseUp && e.button == 0)
        {
            //クリック
            if (!isSelecting)
            {
                HandleClick();
            }
            //ドラッグ解放
            else
            {
                Vector2 endPoint = mousePos;
                HandleDrag(startPoint, endPoint);
            }
            
            isSelecting = false;
            selectionRect = new Rect();
            DrawSelectionRectangle();

        }
        //ドラッグ中
        else if (e.type == EventType.MouseDrag && e.button == 0 && Vector2.Distance(startPoint, mousePos) >= dragThreshold)
        {
            isSelecting = true;
            HighlightNull();
            selectionRect = new Rect(startPoint.x, startPoint.y, mousePos.x - startPoint.x, mousePos.y - startPoint.y);
            HandleUtility.Repaint();
        }
        //ドラッグしていないとき
        else if (!isSelecting)
        {
            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime - lastUpdateTime >= raycastInterval)
            {
                lastUpdateTime = currentTime;
                PerformRaycast();
            }
        }

        //sceneviewの外側にある場合の初期化処理
        if (!sceneViewRect.Contains(mousePos))
        {
            HighlightNull();
            if (isSelecting)
            {
                isSelecting = false;
                selectionRect = new Rect();
                HandleUtility.Repaint();
                DrawSelectionRectangle();
            }
        }
    }

    private void DrawSelectionRectangle()
    {
        Handles.BeginGUI();
        Color selectionColor = isPreviewSelected ? new Color(1, 0, 0, 0.2f) : new Color(0, 1, 1, 0.2f);
        GUI.color = selectionColor;
        GUI.DrawTexture(selectionRect, EditorGUIUtility.whiteTexture);
        Handles.EndGUI();
    }

    private void HandleClick()
    {
        UpdateSelection(PreviousIslandIndices);
        HighlightNull();
    }

    private void PerformRaycast()
    {
        if (SceneRaycastUtility.TryRaycast(out RaycastHit hitInfo))
        {
            if (SceneRaycastUtility.IsHitObject(PreviewSkinnedMeshRenderer.gameObject, hitInfo))
            {
                int triangleIndex = hitInfo.triangleIndex;
                List<int> indices = IslandUtility.GetIslandIndexFromTriangleIndex(bakedMesh, triangleIndex, islands, mergeSamePosition);
                if (indices.Count > 0 && indices != PreviousIslandIndices)
                {
                    PreviousIslandIndices = indices;
                    if (isPreviewSelected)
                        HighlightIslandEdges(PreviewSkinnedMeshRenderer.transform, bakedMesh.vertices, Color.red, indices);
                    else
                        HighlightIslandEdges(PreviewSkinnedMeshRenderer.transform, bakedMesh.vertices, Color.cyan, indices);
                }
            }
        }
        else
        {
            HighlightNull();
        }
    }

    private void HighlightNull()
    {
        PreviousIslandIndices.Clear();
        HighlightIslandEdges(PreviewSkinnedMeshRenderer.transform, bakedMesh.vertices);
    }

    private void HandleDrag(Vector2 startpos, Vector2 endpos)
    {
        MeshCollider meshCollider = GenerateColider(startpos, endpos);
        List<int> indices = IslandUtility.GetIslandIndicesInColider(bakedMesh, meshCollider, islands, mergeSamePosition, isAll, PreviewSkinnedMeshRenderer.transform);
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

        float depth = 10.0f;

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
            // Front face
            0, 1, 2, 0, 2, 3,
            // Back face
            4, 6, 5, 4, 7, 6,
            // Top face
            1, 5, 6, 1, 6, 2,
            // Bottom face
            0, 3, 7, 0, 7, 4,
            // Left face
            0, 4, 1, 1, 4, 5,
            // Right face
            3, 2, 6, 3, 6, 7
        };

        GameObject coliderObject = new GameObject();
        MeshCollider meshCollider = coliderObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
        meshCollider.convex = true;

        return meshCollider;
    }

    private List<int> GetVerticesFromIndices(List<int> indices)
    {
        var vertices = new HashSet<int>();
        var indexToIslandMap = new Dictionary<int, List<int>>();

        foreach (var islandList in islands)
        {
            foreach (var island in islandList)
            {
                if (!indexToIslandMap.ContainsKey(island.Index))
                {
                    indexToIslandMap[island.Index] = island.Vertices;
                }
            }
        }

        foreach (var index in indices)
        {
            if (index < 0)
            {
                Console.WriteLine($"Index out of range: {index}. Valid index should be non-negative.");
                continue;
            }

            if (indexToIslandMap.TryGetValue(index, out var islandVertices))
            {
                foreach (var vertex in islandVertices)
                {
                    vertices.Add(vertex);
                }
            }
        }

        return vertices.ToList();
    }

    private void UpdateSelection(List<int> indices)
    {
        SaveUndoState();
        foreach (var index in indices)
        {
            if (isPreviewSelected)
            {
                unselected_Island_Indcies.Add(index);
                selected_Island_Indcies.Remove(index);
            }
            else
            {
                selected_Island_Indcies.Add(index);
                unselected_Island_Indcies.Remove(index);
            }
        }
        UpdateMesh();
    }

   private void HighlightIslandEdges(Transform transform, Vector3[] vertices)
    {
        HashSet<(int, int)> edgesToHighlight = new HashSet<(int, int)>();
        highlightManager.HighlightEdges(edgesToHighlight, vertices, Color.cyan, transform);
    }

    private void HighlightIslandEdges(Transform transform, Vector3[] vertices, Color color, List<int> islandIndices)
    {
        HashSet<(int, int)> edgesToHighlight = new HashSet<(int, int)>();

        foreach (int childIndex in islandIndices)
        {
            foreach (var mergedIsland in islands)
            {
                Island island = mergedIsland.FirstOrDefault(i => i.Index == childIndex);
                if (island != null)
                {
                    foreach (var edge in island.AllEdges)
                    {
                        edgesToHighlight.Add((edge.Item1, edge.Item2));
                    }
                    break; // 1つ見つけたらループ抜けて次のchildIndexへ
                }
            }
        }

        highlightManager.HighlightEdges(edgesToHighlight, vertices, color, transform);
    }

    private void porcess_options()
    {   
        //EditorGUILayout.LabelField("Island Indices", string.Join(", ", Island_Index));

        mergeSamePosition = !EditorGUILayout.Toggle("Separate mesh more", !mergeSamePosition);
        isAll = !EditorGUILayout.Toggle("Select all in range ", !isAll);

        EditorGUILayout.Space();

        _Settings.IncludePhysBone = EditorGUILayout.Toggle("PhysBone ", _Settings.IncludePhysBone);

        GUI.enabled = _Settings.IncludePhysBone;
        _Settings.IncludePhysBoneColider = EditorGUILayout.Toggle("PhysBoneColider", _Settings.IncludePhysBoneColider);
        GUI.enabled = true;

        EditorGUILayout.Space();

        showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Options");
        if (showAdvancedOptions)
        {

            GUI.enabled = _Settings.IncludePhysBone;
            GUIContent content_at = new GUIContent("Additional Transforms", "Output Additional PhysBones Affected Transforms for exact PhysBone movement");
            _Settings.RemainAllPBTransforms = EditorGUILayout.Toggle(content_at, _Settings.RemainAllPBTransforms);

            GUIContent content_ii = new GUIContent("Include IgnoreTransforms", "Output PhysBone's IgnoreTransforms");
            _Settings.IncludeIgnoreTransforms = EditorGUILayout.Toggle(content_ii, _Settings.IncludeIgnoreTransforms);

            GUIContent content_rr = new GUIContent(
                "Rename RootTransform",
                "Not Recommended: Due to the specifications of modular avatar, costume-side physbones may be deleted in some cases, so renaming physbone RootTransform will ensure that the costume-side physbones are integrated. This may cause duplication.");
            _Settings.RenameRootTransform = EditorGUILayout.Toggle(content_rr, _Settings.RenameRootTransform);

            GUI.enabled = true;

            GUIContent content_sr = new GUIContent("Specify Root Object", "The default root object is the parent object of the specified skinned mesh renderer object");
            _Settings.RootObject = (GameObject)EditorGUILayout.ObjectField(content_sr, _Settings.RootObject, typeof(GameObject), true);

            RenderIslandHashField();
            RenderModeoff();
            RenderUndoRedoButtons();
            RenderCreateBothModuleButtons();
        }

        EditorGUILayout.Space();
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
        PreviewSkinnedMeshRenderer = new ModuleCreator(settings).PreviewMesh(OriginskinnedMeshRenderer.gameObject);

        ResetAllBlendShapes(PreviewSkinnedMeshRenderer);
        PreviewMeshObject = PreviewSkinnedMeshRenderer.transform.parent.gameObject;
        PreviewMeshObject.transform.position = PreviewMeshObject.transform.position + new Vector3(0, 0, -5);
        PreviewMeshObject.name = "Preview Mesh";

        Vector3 parentScale = PreviewMeshObject.transform.localScale;
        PreviewSkinnedMeshRenderer.transform.localScale = new Vector3(1 / parentScale.x, 1 / parentScale.y, 1 / parentScale.z);

        bakedMesh = new Mesh(); 
        PreviewSkinnedMeshRenderer.BakeMesh(bakedMesh);
        bakedMesh = MeshDeletionUtility.GenerateBacksideMesh(bakedMesh);

        FocusCustomViewObject(PreviewSkinnedMeshRenderer.transform, bakedMesh);
    }

    private void CloseCustomSceneView()
    {
        if (PreviewMeshObject != null)
        {
            DestroyImmediate(PreviewMeshObject);
            PreviewMeshObject = null;
        }
    }

    private void UpdateMesh()
    {

        if (isPreviewSelected)
        {
            Selected_Vertices = GetVerticesFromIndices(selected_Island_Indcies);

            Mesh PreviewMesh = MeshDeletionUtility.KeepVerticesUsingDegenerateTriangles(OriginskinnedMeshRenderer.sharedMesh, Selected_Vertices);
            PreviewSkinnedMeshRenderer.sharedMesh = PreviewMesh;

            if (Selected_Vertices.Count >= 3){
                Mesh ColliderMesh = MeshDeletionUtility.KeepVerticesUsingDegenerateTriangles(bakedMesh, Selected_Vertices);
                PreviewMeshCollider.sharedMesh = ColliderMesh;
            }
            else PreviewMeshCollider.sharedMesh = null;
        }
        else
        {
            List<int> Unselected_Vertices = GetVerticesFromIndices(unselected_Island_Indcies);

            Mesh PreviewMesh = MeshDeletionUtility.KeepVerticesUsingDegenerateTriangles(OriginskinnedMeshRenderer.sharedMesh, Unselected_Vertices);
            PreviewSkinnedMeshRenderer.sharedMesh = PreviewMesh;
            
            if (Unselected_Vertices.Count >= 3){
                Mesh ColliderMesh = MeshDeletionUtility.KeepVerticesUsingDegenerateTriangles(bakedMesh, Unselected_Vertices);
                PreviewMeshCollider.sharedMesh = ColliderMesh;
            }
            else PreviewMeshCollider.sharedMesh = null;
        }

        Repaint();
        UpdateEncodedString();
    }
}