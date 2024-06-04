using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ModuleCreatorIsland : EditorWindow
{
    private List<Island> islands = new List<Island>();
    private List<int> unselected_Island_Index = new List<int>();
    private List<int> selected_Island_Index = new List<int>();

    public SkinnedMeshRenderer OriginskinnedMeshRenderer;
    private SkinnedMeshRenderer PreviewSkinnedMeshRenderer;
    private Mesh PreviewMesh;
    private GameObject PreviewMeshObject;
    private int PreviousIslandIndex = -1;

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
        unselected_Island_Index.Clear();
        selected_Island_Index.Clear();
    }

// GUI Rendering Method
    private void OnGUI()
    {
        if (!isGameObjectContext)
        {
            RenderGUI();
            RenderSetupButtons();
        }

        RenderRaycastButton();
        
        RenderSelectionButtons();
        RenderIslandHashField();

        porcess_options();

        RenderPreviewSelectedToggle();
        RenderCreateModuleButtons();
    }

    private void RenderPreviewSelectedToggle()
    {
        if (GUILayout.Button(isPreviewSelected ? "UnPreview Output" : "Preview Output"))
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

        mergeSamePosition = EditorGUILayout.Toggle("MergeSamePosition", mergeSamePosition);
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
        GUI.enabled = OriginskinnedMeshRenderer != null && selected_Island_Index.Count > 0;

        GUILayout.BeginHorizontal();
        
        // Create Selected Islands Module
        if (GUILayout.Button("Create Selected Module"))
        {
            CreateModule(selected_Island_Index, "SelectedIslandsModule");
            if (isGameObjectContext) Close();
        }

        // Create Both Modules
        if (GUILayout.Button("Create Both Modules"))
        {
            CreateModule(selected_Island_Index, "SelectedIslandsModule");
            CreateModule(unselected_Island_Index, "UnselectedIslandsModule");
            processend();
            if (isGameObjectContext) Close();
        }

        GUILayout.EndHorizontal();

        GUI.enabled = true;
    }

    private void CreateModule(List<int> islandIndices, string moduleName)
    {
        if (islandIndices.Count > 0)
        {
            var allVertices = new HashSet<int>(islandIndices.SelectMany(index => islands[index].Vertices));
            SaveModule(allVertices.ToList(), moduleName);
        }
        
        isRaycastEnabled = false;
    }

    private void SaveModule(List<int> vertices, string moduleName)
    {
        stopwatch.Restart();
        Mesh newMesh = MeshDeletionUtility.DeleteMeshByKeepVertices(OriginskinnedMeshRenderer, vertices);
        stopwatch.Stop();
        UnityEngine.Debug.Log($"Delete Mesh: {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        string path = AssetDatabase.GenerateUniqueAssetPath($"Assets/Mesh/{moduleName}.asset");
        AssetDatabase.CreateAsset(newMesh, path);
        AssetDatabase.SaveAssets();
        stopwatch.Stop();
        UnityEngine.Debug.Log($"Save newMesh: {stopwatch.ElapsedMilliseconds} ms");

        _Settings.newmesh = newMesh;
        stopwatch.Restart();
        new ModuleCreator(_Settings).CheckAndCopyBones(OriginskinnedMeshRenderer.gameObject);
        stopwatch.Stop();
    }
// Render Island Hash Field
    private void RenderIslandHashField()
    {
        GUILayout.Label("Encoded islands:", EditorStyles.boldLabel);
        string newValue = EditorGUILayout.TextField(textFieldValue);

        if (newValue != textFieldValue)
        {
            int[] decodedArray = IntArrayConverter.Decode(newValue);
            if (decodedArray != null && decodedArray[0] == islands.Count) // 最初の要素が島の数と一致するか確認
            {
                selected_Island_Index = new List<int>(decodedArray.Skip(1)); // 最初の要素を除外
                unselected_Island_Index = unselected_Island_Index.Except(selected_Island_Index).ToList();
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
                else if (decodedArray[0] != islands.Count)
                {
                    UnityEngine.Debug.LogError($"Island Hashのデコード成功しましたが、島の数が一致しません。デコードされた島の数 (最初の要素): {decodedArray[0]}, 現在の島の数: {islands.Count}");
                }
            }
        }

        string encodedString = IntArrayConverter.Encode(selected_Island_Index.Prepend(islands.Count).ToArray()); // 最初に数を追加
        if (encodedString != textFieldValue)
        {
            textFieldValue = encodedString;
            GUI.FocusControl(""); 
        }
    }

    private void SelectAllIslands()
    {
        selected_Island_Index = Enumerable.Range(0, islands.Count).ToList();
        unselected_Island_Index.Clear();
        UpdateMesh();
    }

    private void UnselectAllIslands()
    {
        unselected_Island_Index = Enumerable.Range(0, islands.Count).ToList();
        selected_Island_Index.Clear();
        UpdateMesh();
    }

    private void ReverseAllIslands()
    {
        var temp = new List<int>(selected_Island_Index);
        selected_Island_Index = new List<int>(unselected_Island_Index);
        unselected_Island_Index = temp;
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

    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.RepaintAll();
        processend();
        FocusCustomViewObject(OriginskinnedMeshRenderer);
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
        SceneView.lastActiveSceneView.LookAt(middleVertex, Quaternion.Euler(0, 180, 0), cameraDistance);

        SceneView.lastActiveSceneView.Repaint();
    }


    private MeshCollider AddCollider(SkinnedMeshRenderer skinnedMeshRenderer, List<int> Island_Index)
    {
        MeshCollider meshCollider;
        meshCollider = skinnedMeshRenderer.GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            meshCollider = skinnedMeshRenderer.gameObject.AddComponent<MeshCollider>();
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
        isRaycastEnabled = !isRaycastEnabled;
        if (isRaycastEnabled)
        {
            PreviewMeshCollider = AddCollider(PreviewSkinnedMeshRenderer, unselected_Island_Index);
            EnsureHighlightManagerExists();
            UpdateMesh(); // コライダーのメッシュを更新
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
        islands = MeshIslandUtility.GetIslands(PreviewSkinnedMeshRenderer, mergeSamePosition);
        stopwatch.Stop();
        UnityEngine.Debug.Log($"Calculate Islands: {stopwatch.ElapsedMilliseconds} ms");
        UnityEngine.Debug.Log($"Islands count: {islands.Count}");
        selected_Island_Index.Clear();
        unselected_Island_Index = Enumerable.Range(0, islands.Count).ToList(); 
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
        if (EditorRaycastHelper.RaycastAgainstScene(out ExtendedRaycastHit extendedHit))
        {
            if (EditorRaycastHelper.IsHitObjectSpecified(extendedHit, PreviewSkinnedMeshRenderer.gameObject))
            {   
                int triangleIndex = extendedHit.triangleIndex;
                int index = MeshIslandUtility.GetIslandIndexFromTriangleIndex(PreviewSkinnedMeshRenderer, triangleIndex, islands);
                if (index != PreviousIslandIndex)
                {
                    PreviousIslandIndex = index;
                    HighlightIslandEdges(PreviewSkinnedMeshRenderer, index);
                }
            }
        }
        else
        {   
            PreviousIslandIndex = -1;
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
        if (currentTime - lastUpdateTime >= raycastInterval)
        {
            lastUpdateTime = currentTime;
            PerformRaycast();
        }

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            //unselected
            if (!selected_Island_Index.Contains(PreviousIslandIndex) && PreviousIslandIndex != -1)
            {
                selected_Island_Index.Add(PreviousIslandIndex);
                unselected_Island_Index.Remove(PreviousIslandIndex);
                UpdateMesh(); // メッシュとコライダーを更新
                //selectedsceneView.Focus();
            }
            else if (!unselected_Island_Index.Contains(PreviousIslandIndex) && PreviousIslandIndex != -1)
            {
                unselected_Island_Index.Add(PreviousIslandIndex);
                selected_Island_Index.Remove(PreviousIslandIndex);
                UpdateMesh(); // メッシュとコライダーを更新
                //unselectedsceneView.Focus();
            }
        }
    }

    private List<int> GetVerticesFromIndices(List<int> indices)
    {
        return indices.SelectMany(index => islands[index].Vertices).Distinct().ToList();
    }


    private void HighlightIslandEdges(SkinnedMeshRenderer skinnedMeshRenderer)
    {
        HashSet<(int, int)> edgesToHighlight = new HashSet<(int, int)>();
        highlightManager.HighlightEdges(edgesToHighlight, skinnedMeshRenderer);
    }

    private void HighlightIslandEdges(SkinnedMeshRenderer skinnedMeshRenderer, int islandIndex)
    {
        Island island = islands[islandIndex];
        HashSet<(int, int)> edgesToHighlight = new HashSet<(int, int)>();
        foreach (var edge in island.AllEdges)
        {
            edgesToHighlight.Add((edge.Item1, edge.Item2));
        }
        highlightManager.HighlightEdges(edgesToHighlight, skinnedMeshRenderer);
    }


    private void porcess_options()
    {   
        //EditorGUILayout.LabelField("Island Indices", string.Join(", ", Island_Index));

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
        if (isPreviewSelected)
        {
            PreviewMesh = MeshDeletionUtility.KeepVerticesUsingDegenerateTriangles(OriginskinnedMeshRenderer, GetVerticesFromIndices(selected_Island_Index));
            if (selected_Island_Index.Count > 0) PreviewMeshCollider.sharedMesh = PreviewMesh;
        }
        else
        {
            PreviewMesh = MeshDeletionUtility.KeepVerticesUsingDegenerateTriangles(OriginskinnedMeshRenderer, GetVerticesFromIndices(unselected_Island_Index));
            if (unselected_Island_Index.Count > 0) PreviewMeshCollider.sharedMesh = PreviewMesh;
        }

        PreviewSkinnedMeshRenderer.sharedMesh = PreviewMesh;

        Repaint();
    }
}