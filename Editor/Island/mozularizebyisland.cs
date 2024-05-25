using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ModuleCreatorIsland : EditorWindow
{
    public SkinnedMeshRenderer OriginskinnedMeshRenderer;
    private SkinnedMeshRenderer UnselectedSkinnedMeshRenderer;
    private SkinnedMeshRenderer SelectedSkinnedMeshRenderer;
    private Mesh Unselectemesh;
    private Mesh SelectedMesh;

    private static ModuleCreatorSettings _Settings;

    private const int MENU_PRIORITY = 49;
    private double lastUpdateTime = 0;
    private const double raycastInterval = 0.01;

    private bool showAdvancedOptions = false;
    private bool isRaycastEnabled = false;
    private bool isGameObjectContext = false;

    private List<int> Island_Index = new List<int>();
    private List<int> unselected_Island_Index = new List<int>();

    private List<Island> islands = new List<Island>();

    private int unselectedpreviousIslandIndex = -1;
    private int selectedpreviousIslandIndex = -1;

    private HighlightEdgesManager highlightManager;

    private Stopwatch stopwatch = new Stopwatch();

    private SceneView defaultsceneView;
    private SceneView customsceneView;
    private GameObject customViewObject;

    private static string textFieldValue = "-1";



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


    // Separate initialization for Window menu item
    private void InitializeFromMenuItem()
    {
        OriginskinnedMeshRenderer= null;
        islands.Clear();
        Unselectemesh.Clear();
        unselected_Island_Index.Clear(); // 初期化
        isGameObjectContext = false;
        Repaint();
    }

    // Separate initialization for GameObject menu item
    private void InitializeFromGameObject()
    {
        islands.Clear();
        Unselectemesh.Clear();
        unselected_Island_Index.Clear(); // 初期化
        isGameObjectContext = true;
        Repaint();
    }

    private void OnGUI()
    {
        if (!isGameObjectContext)
        {
            OriginskinnedMeshRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Skinned Mesh Renderer", OriginskinnedMeshRenderer, typeof(SkinnedMeshRenderer), true);

            if (OriginskinnedMeshRenderer && GUILayout.Button("Duplicate and Setup Skinned Mesh"))
            {
                DuplicateAndSetup();
            }

            if (UnselectedSkinnedMeshRenderer && GUILayout.Button("Calculate Islands"))
            {
                CalculateIslands();
            }
        }
        
        if (islands.Count > 0 && GUILayout.Button(isRaycastEnabled ? "Disable Raycast" : "Enable Raycast"))
        {
            ToggleRaycast();
        }

        GUILayout.Label("Island Hash:", EditorStyles.boldLabel);
        string newValue = EditorGUILayout.TextField(textFieldValue);

        if (newValue != textFieldValue)
        {
            int[] decodedArray = IntArrayConverter.Decode(newValue);
            if (decodedArray != null && decodedArray[0] == islands.Count) // 最初の要素が島の数と一致するか確認
            {
                Island_Index = new List<int>(decodedArray.Skip(1)); // 最初の要素を除外
                unselected_Island_Index = unselected_Island_Index.Except(Island_Index).ToList();
                UpdateMesh();
                textFieldValue = newValue;
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

        string encodedString = IntArrayConverter.Encode(Island_Index.Prepend(islands.Count).ToArray()); // 最初に数を追加
        if (encodedString != textFieldValue)
        {
            textFieldValue = encodedString;
        }

        porcess_options();

        GUI.enabled = OriginskinnedMeshRenderer != null && Island_Index.Count > 0;
        if (GUILayout.Button("Create Module"))
        {
            CreateModule();
            FocusCustomViewObject(defaultsceneView, OriginskinnedMeshRenderer);
            processend();
            if (isGameObjectContext) Close();
        }
        GUI.enabled = true;
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
            IncludePhysBone = false,
            IncludePhysBoneColider = false
        };
        
        if (Unselectemesh == null)
        {
            Unselectemesh = new Mesh();
        }


        SceneView.duringSceneGui += OnSceneGUI;
        isRaycastEnabled = false;

        defaultsceneView = SceneView.sceneViews.Count > 0 ? (SceneView)SceneView.sceneViews[0] : null;
        processend();

    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.RepaintAll();
        processend();
        FocusCustomViewObject(defaultsceneView, OriginskinnedMeshRenderer);
    }

    private void processend()
    {
        if (highlightManager != null)
        {
            DestroyImmediate(highlightManager);
        }
        if (UnselectedSkinnedMeshRenderer != null)
        {
            DestroyImmediate(UnselectedSkinnedMeshRenderer.transform.parent.gameObject);
        }
        if (SelectedSkinnedMeshRenderer != null)
        {
            DestroyImmediate(SelectedSkinnedMeshRenderer.transform.parent.gameObject);
        }
        isRaycastEnabled = false;
        RemoveHighlight();
        CloseCustomSceneView();
    }
    
    private void FocusCustomViewObject(SceneView sceneView, SkinnedMeshRenderer customRenderer)
    {
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

    float cameraDistance = 0.2f;
    Vector3 direction = sceneView.camera.transform.forward;
    Vector3 newCameraPosition = middleVertex - direction * cameraDistance;

    //UnityEngine.Debug.Log(middleVertex);
    sceneView.LookAt(middleVertex, Quaternion.Euler(0, 180, 0), cameraDistance);

    sceneView.Repaint();
}

    private void EnsureHighlightManagerExists()
    {
        if (highlightManager == null)
        {
            highlightManager = UnselectedSkinnedMeshRenderer.gameObject.AddComponent<HighlightEdgesManager>();
            highlightManager.SkinnedMeshRenderer = UnselectedSkinnedMeshRenderer;
        }
    }

    private void ToggleRaycast()
    {
        isRaycastEnabled = !isRaycastEnabled;
        if (isRaycastEnabled)
        {
            EnsureHighlightManagerExists();
        }
        else
        {
            RemoveHighlight();
        }
    }

    private void CalculateIslands()
    {
        stopwatch.Restart();
        islands = MeshIslandUtility.GetIslands(UnselectedSkinnedMeshRenderer);
        stopwatch.Stop();
        UnityEngine.Debug.Log($"Calculate Islands: {stopwatch.ElapsedMilliseconds} ms");
        UnityEngine.Debug.Log($"Islands count: {islands.Count}");
        Island_Index.Clear();
        unselected_Island_Index = Enumerable.Range(0, islands.Count).ToList(); 
    }

    private void CreateModule()
    {
        if (Island_Index.Count > 0)
        {
            var allVertices = new HashSet<int>(Island_Index.SelectMany(index => islands[index].Vertices));
            SaveModule(allVertices.ToList());
        }
        isRaycastEnabled = false;
        Island_Index.Clear();
        processend();
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
            if (EditorRaycastHelper.IsHitObjectSpecified(extendedHit, UnselectedSkinnedMeshRenderer.gameObject))
            {   
                int triangleIndex = extendedHit.triangleIndex;
                int index = MeshIslandUtility.GetIslandIndexFromTriangleIndex(UnselectedSkinnedMeshRenderer, triangleIndex, islands);
                if (index != unselectedpreviousIslandIndex)
                {
                    HighlightIslandEdges(UnselectedSkinnedMeshRenderer, index);
                    unselectedpreviousIslandIndex = index;
                }
            }
            else if (EditorRaycastHelper.IsHitObjectSpecified(extendedHit, SelectedSkinnedMeshRenderer.gameObject))
            {
                int triangleIndex = extendedHit.triangleIndex;
                int index = MeshIslandUtility.GetIslandIndexFromTriangleIndex(UnselectedSkinnedMeshRenderer, triangleIndex, islands);
                if (index != selectedpreviousIslandIndex)
                {
                    HighlightIslandEdges(SelectedSkinnedMeshRenderer, index);
                    selectedpreviousIslandIndex = index;
                }
            }
        }
        else
        {
            unselectedpreviousIslandIndex = -1;
            selectedpreviousIslandIndex = -1;
            HighlightIslandEdges(UnselectedSkinnedMeshRenderer);
            HighlightIslandEdges(SelectedSkinnedMeshRenderer);
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!isRaycastEnabled || UnselectedSkinnedMeshRenderer == null) return;

        double currentTime = EditorApplication.timeSinceStartup;
        if (currentTime - lastUpdateTime >= raycastInterval)
        {
            lastUpdateTime = currentTime;
            PerformRaycast();
        }

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            //unselected
            if (!Island_Index.Contains(unselectedpreviousIslandIndex) && unselectedpreviousIslandIndex != -1)
            {
                Island_Index.Add(unselectedpreviousIslandIndex);
                unselected_Island_Index.Remove(unselectedpreviousIslandIndex);
            }
            else if (!unselected_Island_Index.Contains(selectedpreviousIslandIndex) && selectedpreviousIslandIndex != -1)
            {
                unselected_Island_Index.Add(selectedpreviousIslandIndex);
                Island_Index.Remove(selectedpreviousIslandIndex);
            }


            UpdateMesh();
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

    private void SaveModule(List<int> vertices)
    {
        stopwatch.Restart();
        Mesh newMesh = MeshDeletionUtility.DeleteMeshByKeepVertices(OriginskinnedMeshRenderer, vertices);
        stopwatch.Stop();
        UnityEngine.Debug.Log($"Delete Mesh: {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Mesh/NewMesh.asset");
        AssetDatabase.CreateAsset(newMesh, path);
        AssetDatabase.SaveAssets();
        stopwatch.Stop();
        UnityEngine.Debug.Log($"Save NewMesh: {stopwatch.ElapsedMilliseconds} ms");

        _Settings.newmesh = newMesh;
        stopwatch.Restart();
        new ModuleCreator(_Settings).CheckAndCopyBones(OriginskinnedMeshRenderer.gameObject);
        stopwatch.Stop();
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

    private void DuplicateAndSetup()
    {
        defaultsceneView = SceneView.sceneViews.Count > 0 ? (SceneView)SceneView.sceneViews[0] : null;

        ModuleCreatorSettings settings = new ModuleCreatorSettings
        {
            IncludePhysBone = false,
            IncludePhysBoneColider = false
        };
        UnselectedSkinnedMeshRenderer = new ModuleCreator(settings).PreciewMesh(OriginskinnedMeshRenderer.gameObject);

        Mesh originalMesh = UnselectedSkinnedMeshRenderer.sharedMesh;
        Unselectemesh = Instantiate(originalMesh);
        UnselectedSkinnedMeshRenderer.sharedMesh = Unselectemesh;

        FocusCustomViewObject(defaultsceneView, UnselectedSkinnedMeshRenderer);

        OpenCustomSceneView();
    }

    private void CloseCustomSceneView()
    {
        //UnityEngine.Debug.Log("aaa?");
        if (customsceneView != null)
        {
            customsceneView.Close();
            customsceneView = null;
            //UnityEngine.Debug.Log("aaa");
        }

        if (customViewObject != null)
        {
            DestroyImmediate(customViewObject);
            customViewObject = null;
        }
    }

    public void OpenCustomSceneView()
    {
        customsceneView = CreateInstance<SceneView>();
        customsceneView.title = "Selected Mesh PureView";
        customsceneView.Show();
        //customsceneView.Focus();

        if (UnselectedSkinnedMeshRenderer != null)
        {
            customViewObject = Instantiate(UnselectedSkinnedMeshRenderer.transform.parent.gameObject, UnselectedSkinnedMeshRenderer.transform.parent.transform.position + Vector3.right * 10, Quaternion.identity);
            SelectedSkinnedMeshRenderer = customViewObject.GetComponentInChildren<SkinnedMeshRenderer>();
            FocusCustomViewObject(customsceneView, SelectedSkinnedMeshRenderer);

            var emptyVerticesList = new List<int>(); // Start with an empty list to disable all vertices initially
            SelectedMesh = MeshDeletionUtility.KeepVerticesUsingDegenerateTriangles(UnselectedSkinnedMeshRenderer, emptyVerticesList);
            SelectedSkinnedMeshRenderer.sharedMesh = SelectedMesh;

            // Focus the camera on the customViewObject's bounds
        }
    }

    private void UpdateMesh()
    {
        Unselectemesh = MeshDeletionUtility.KeepVerticesUsingDegenerateTriangles(OriginskinnedMeshRenderer, GetVerticesFromIndices(unselected_Island_Index));
        SelectedMesh = MeshDeletionUtility.KeepVerticesUsingDegenerateTriangles(OriginskinnedMeshRenderer, GetVerticesFromIndices(Island_Index));

        UnselectedSkinnedMeshRenderer.sharedMesh = Unselectemesh;
        SelectedSkinnedMeshRenderer.sharedMesh = SelectedMesh;
        Repaint();
}

}