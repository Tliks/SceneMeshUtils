using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using UnityEditor;
using UnityEngine;


public class ModuleCreatorIsland : EditorWindow
{
    
    private List<List<Island>> islands = new List<List<Island>>();
    [SerializeField] private List<int> unselected_Island_Indcies = new List<int>();
    [SerializeField] private List<int> selected_Island_Indcies = new List<int>();

    public SkinnedMeshRenderer OriginskinnedMeshRenderer;
    private SkinnedMeshRenderer PreviewSkinnedMeshRenderer;
    private Mesh PreviewMesh;
    private GameObject PreviewMeshObject;
    private List<int> PreviousIslandIndices = new List<int>();

    private static ModuleCreatorSettings _Settings;

    private const int MENU_PRIORITY = 49;
    private double lastUpdateTime = 0;
    private const double raycastInterval = 0.01;

    private bool showAdvancedOptions = false;
    private bool isRaycastEnabled = false;
    private bool isGameObjectContext = false;

    private HighlightEdgesManager highlightManager;

    private Stopwatch stopwatch = new Stopwatch();

    private static string textFieldValue = "-1";

    public bool mergeSamePosition = true;
    private MeshCollider PreviewMeshCollider;
    private bool isPreviewSelected = false;
    private int total_islands_index = 0;
    List<int> Selected_Vertices = new List<int>();
    List<int> Total_Vertices = new List<int>();

    [MenuItem("Window/Module Creator/Modularize Mesh by Island")]
    public static void ShowWindow()
    {
        var existingWindow = GetWindow<ModuleCreatorIsland>("Module Creator", false);
        if (existingWindow != null)
        {
            existingWindow.Close();
        }
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

        RenderRaycastButton();
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
    //labelStyle.fontStyle = UnityEngine.FontStyle.Bold; // ボールドにして明瞭にする

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
            FocusCustomViewObject(OriginskinnedMeshRenderer);
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

    private void RenderRaycastButton()
    {   
        GUI.enabled = islands.Count > 0;
        if (GUILayout.Button(isRaycastEnabled ? "Disable Raycast" : "Enable Raycast"))
        {
            ToggleRaycast();
        }
        GUI.enabled = true;
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
        SaveUndoState();
        if (islandIndices.Count > 0)
        {
            var allVertices = GetVerticesFromIndices(islandIndices);
            SaveModule(allVertices.ToList());
        }

        isRaycastEnabled = false;
    }
    private void SaveModule(List<int> vertices)
    {
        stopwatch.Restart();
        Mesh newMesh = MeshDeletionUtility.DeleteMeshByKeepVertices(OriginskinnedMeshRenderer, vertices);
        stopwatch.Stop();
        //UnityEngine.Debug.Log($"Delete Mesh: {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        string path = GenerateVariantPath(OriginskinnedMeshRenderer.transform.parent.gameObject, "newMesh");
        AssetDatabase.CreateAsset(newMesh, path);
        AssetDatabase.SaveAssets();
        stopwatch.Stop();
        UnityEngine.Debug.Log($"Save newMesh: {stopwatch.ElapsedMilliseconds} ms");

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
        GUILayout.Label("Encoded islands:", EditorStyles.boldLabel);
        string newValue = EditorGUILayout.TextField(textFieldValue);
        int total_islands_count = total_islands_index+1;

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
                    UnityEngine.Debug.LogError("Island Hashのデコードに失敗しました。無効な形式です。");
                }
                else if (decodedArray[decodedArray.Length - 1] != total_islands_count)
                {
                    UnityEngine.Debug.LogError($"Island Hashのデコード成功しましたが、島の数が一致しません。デコードされた島の数 (最後の要素): {decodedArray[decodedArray.Length - 1]}, 現在の島の数: {total_islands_count}");
                }
            }
        }

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
        ToggleRaycast();
    }

    private void OnEnable()
    {
        _Settings = new ModuleCreatorSettings
        {
            IncludePhysBone = true,
            IncludePhysBoneColider = true
        };
        
        if (PreviewMesh == null)
        {
            PreviewMesh = new Mesh();
        }


        SceneView.duringSceneGui += OnSceneGUI;
        isRaycastEnabled = false;

        processend();
        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.RepaintAll();
        processend();
        FocusCustomViewObject(OriginskinnedMeshRenderer);
        Undo.undoRedoPerformed -= OnUndoRedo;
    }

    private void OnUndoRedo()
    {
        UpdateMesh();
        Repaint();
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
        isRaycastEnabled = false;
        RemoveHighlight();
        CloseCustomSceneView();
        //Close();
    }
    
    private void FocusCustomViewObject(SkinnedMeshRenderer customRenderer)
    {
        if (!customRenderer) return;
        Mesh mesh = customRenderer.sharedMesh;
        Vector3 middleVertex = Vector3.zero;

        if (mesh != null)
        {
            // 頂点座標を取得
            Vector3[] vertices = mesh.vertices;

            // スキニングの影響を考慮して頂点を移動
            stopwatch.Restart();
            middleVertex = vertices
                .Select(v => customRenderer.transform.TransformPoint(v))
                .Aggregate((acc, v) => acc + v) / vertices.Length;
            stopwatch.Stop();
            //UnityEngine.Debug.Log($"middleVertex: {stopwatch.ElapsedMilliseconds} ms");
        }

        float cameraDistance = 0.3f;
        Vector3 direction = SceneView.lastActiveSceneView.camera.transform.forward;
        Vector3 newCameraPosition = middleVertex - direction * cameraDistance;

        //UnityEngine.Debug.Log(middleVertex);
        SceneView.lastActiveSceneView.LookAt(middleVertex, SceneView.lastActiveSceneView.rotation, cameraDistance);

        SceneView.lastActiveSceneView.Repaint();
    }


    private MeshCollider AddCollider(SkinnedMeshRenderer skinnedMeshRenderer, List<int> Island_Index)
    {
        MeshCollider meshCollider;
        meshCollider = skinnedMeshRenderer.GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            meshCollider = skinnedMeshRenderer.gameObject.AddComponent<MeshCollider>();
            meshCollider.convex = false;  
            if (Island_Index.Count > 0) meshCollider.sharedMesh = skinnedMeshRenderer.sharedMesh;

        }
        return meshCollider;
    }

    private void DelateCollider(MeshCollider meshCollider)
    {
        DestroyImmediate(meshCollider);
    }

    private void EnsureHighlightManagerExists()
    {
        if (highlightManager == null)
        {
            highlightManager = PreviewSkinnedMeshRenderer.gameObject.AddComponent<HighlightEdgesManager>();
            highlightManager.SkinnedMeshRenderer = PreviewSkinnedMeshRenderer;
        }
    }

    private void ToggleRaycast()
    {
        SaveUndoState();
        isRaycastEnabled = !isRaycastEnabled;
        if (isRaycastEnabled)
        {
            PreviewMeshCollider = AddCollider(PreviewSkinnedMeshRenderer, unselected_Island_Indcies);
            EnsureHighlightManagerExists();
            UpdateMesh(); // コライダーのメッシュを更新
            SceneView.lastActiveSceneView.drawGizmos = true;
        }
        else
        {
            DelateCollider(PreviewMeshCollider);
            RemoveHighlight();
        }
    }

    private void CalculateIslands()
    {
        stopwatch.Restart();
        islands = MeshIslandUtility.GetIslands(PreviewSkinnedMeshRenderer);
        stopwatch.Stop();
        UnityEngine.Debug.Log($"Calculate Islands: {stopwatch.ElapsedMilliseconds} ms");
        total_islands_index = GetTotalElementCount(islands);
        UnityEngine.Debug.Log($"Islands count: {islands.Count}/{total_islands_index+1}");
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

    private void PerformRaycast()
    {
        if (SceneRaycastUtility.TryRaycast(out SceneRaycastHitInfo extendedHit))
        {
            if (SceneRaycastUtility.IsHitObject(PreviewSkinnedMeshRenderer.gameObject, extendedHit))
            {
                int triangleIndex = extendedHit.hitTriangleIndex;
                List<int> indices = MeshIslandUtility.GetIslandIndexFromTriangleIndex(PreviewSkinnedMeshRenderer, triangleIndex, islands, mergeSamePosition);
                if (indices.Count > 0 && indices != PreviousIslandIndices)
                {
                    PreviousIslandIndices = indices;
                    if (isPreviewSelected)
                        HighlightIslandEdges(PreviewSkinnedMeshRenderer, UnityEngine.Color.red, indices);
                    else
                        HighlightIslandEdges(PreviewSkinnedMeshRenderer, UnityEngine.Color.cyan, indices);
                }
            }
        }
        else
        {
            PreviousIslandIndices.Clear();
            HighlightIslandEdges(PreviewSkinnedMeshRenderer);
        }
    }
    private void DontActiveSKin()
    {
        if (Event.current != null && Selection.activeGameObject != null)
        {
            GameObject currentActiveObject = Selection.activeGameObject;
            if (currentActiveObject == PreviewSkinnedMeshRenderer.gameObject)
            {
                Selection.activeGameObject = null;
            }
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!isRaycastEnabled || PreviewSkinnedMeshRenderer == null) return;

        DontActiveSKin();

        double currentTime = EditorApplication.timeSinceStartup;
        if (isRaycastEnabled && currentTime - lastUpdateTime >= raycastInterval)
        {
            lastUpdateTime = currentTime;
            PerformRaycast();
        }

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {   
            SaveUndoState();
            foreach (var index in PreviousIslandIndices)
            {
                //unselected
                if (!isPreviewSelected)
                {
                    selected_Island_Indcies.Add(index);
                    unselected_Island_Indcies.Remove(index);
                }
                else
                {
                    unselected_Island_Indcies.Add(index);
                    selected_Island_Indcies.Remove(index);
                }
            }
            UpdateMesh();
        }

        if (Event.current.type == EventType.KeyDown && (Event.current.control || Event.current.command))
        {
            if (Event.current.keyCode == KeyCode.Z) // Ctrl/Cmd + Z
            {
                Undo.PerformUndo();
                Event.current.Use();
            }
            else if (Event.current.keyCode == KeyCode.Y) // Ctrl/Cmd + Y
            {
                Undo.PerformRedo();
                Event.current.Use();
            }
        }

    }

private List<int> GetVerticesFromIndices(List<int> indices)
{
    var vertices = new List<int>();
    foreach (var index in indices)
    {
        if (index < 0)
        {
            Console.WriteLine($"Index out of range: {index}. Valid index should be non-negative.");
            continue;
        }

        foreach (var islandList in islands)
        {
            foreach (var island in islandList)
            {
                if (island.Index == index)
                {
                    vertices.AddRange(island.Vertices);
                }
            }
        }
    }

    return vertices.Distinct().ToList();
}
   private void HighlightIslandEdges(SkinnedMeshRenderer skinnedMeshRenderer)
    {
        HashSet<(int, int)> edgesToHighlight = new HashSet<(int, int)>();
        highlightManager.HighlightEdges(edgesToHighlight, skinnedMeshRenderer, UnityEngine.Color.cyan);
    }

private void HighlightIslandEdges(SkinnedMeshRenderer skinnedMeshRenderer, UnityEngine.Color color, List<int> islandIndices)
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

    highlightManager.HighlightEdges(edgesToHighlight, skinnedMeshRenderer, color);
}
    private void porcess_options()
    {   
        //EditorGUILayout.LabelField("Island Indices", string.Join(", ", Island_Index));

        EditorGUILayout.Space();

        mergeSamePosition = !EditorGUILayout.Toggle("Split More", !mergeSamePosition);

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
        PreviewSkinnedMeshRenderer = new ModuleCreator(settings).PreciewMesh(OriginskinnedMeshRenderer.gameObject);

        ResetAllBlendShapes(PreviewSkinnedMeshRenderer);
        PreviewMeshObject = PreviewSkinnedMeshRenderer.transform.parent.gameObject;
        PreviewMeshObject.transform.position = OriginskinnedMeshRenderer.transform.parent.position + new Vector3(0, 0, -5);
        PreviewMeshObject.transform.rotation = OriginskinnedMeshRenderer.transform.parent.rotation;

        PreviewSkinnedMeshRenderer.gameObject.transform.localPosition = Vector3.zero; 
        PreviewSkinnedMeshRenderer.gameObject.transform.localScale = new Vector3(1, 1, 1);
        
        PreviewMeshObject.name = "Preview Mesh";

        Mesh bakedMesh = new Mesh();
        PreviewSkinnedMeshRenderer.BakeMesh(bakedMesh);
        //PreviewSkinnedMeshRenderer.sharedMesh = bakedMesh;

        //Mesh originalMesh = PreviewSkinnedMeshRenderer.sharedMesh;
        //PreviewMesh = Instantiate(originalMesh);
        //PreviewSkinnedMeshRenderer.sharedMesh = PreviewMesh;

        FocusCustomViewObject(PreviewSkinnedMeshRenderer);
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
        //UnityEngine.Debug.Log($"update");
        Selected_Vertices = GetVerticesFromIndices(selected_Island_Indcies);
        if (isPreviewSelected)
        {
            PreviewMesh = MeshDeletionUtility.KeepVerticesUsingDegenerateTriangles(OriginskinnedMeshRenderer, Selected_Vertices);
            if (selected_Island_Indcies.Count > 0) PreviewMeshCollider.sharedMesh = PreviewMesh;
        }
        else
        {
            List<int> Vertices = GetVerticesFromIndices(unselected_Island_Indcies);
            PreviewMesh = MeshDeletionUtility.KeepVerticesUsingDegenerateTriangles(OriginskinnedMeshRenderer, Vertices);
            if (unselected_Island_Indcies.Count > 0) PreviewMeshCollider.sharedMesh = PreviewMesh;
        }

        PreviewSkinnedMeshRenderer.sharedMesh = PreviewMesh;

        Repaint();
    }
}