using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ModuleCreatorIsland : EditorWindow
{
    public SkinnedMeshRenderer skinnedMeshRenderer;
    private static ModuleCreatorSettings _Settings;
    private const int MENU_PRIORITY = 49;
    private bool showAdvancedOptions = false;

    private List<int> Island_Index = new List<int>();
    private bool isRaycastEnabled = false;

    private List<Island> islands = new List<Island>();
    private int previousIslandIndex = -1;
    private SkinnedMeshRenderer duplicatedSkinnedMeshRenderer;
    private HighlightEdgesManager highlightManager;

    private Stopwatch stopwatch = new Stopwatch();
    private const double raycastInterval = 0.01;
    private double lastUpdateTime = 0;

    private Mesh previewmesh;
    private GameObject customViewObject;
    private Mesh customViewMesh;

    private SceneView defaultsceneView;
    private SkinnedMeshRenderer customRenderer;

    private SceneView customsceneView;
    private bool isGameObjectContext = false;

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
            window.skinnedMeshRenderer = Selection.activeGameObject.GetComponent<SkinnedMeshRenderer>();
            if (window.skinnedMeshRenderer != null)
            {
                window.InitializeFromGameObject();
                window.AutoSetup();
            }
        }
    }


    // Separate initialization for Window menu item
    private void InitializeFromMenuItem()
    {
        skinnedMeshRenderer= null;
        islands.Clear();
        previewmesh.Clear();
        isGameObjectContext = false;
        Repaint();
    }

    // Separate initialization for GameObject menu item
    private void InitializeFromGameObject()
    {
        islands.Clear();
        previewmesh.Clear();
        isGameObjectContext = true;
        Repaint();
    }

    private void OnGUI()
    {
        if (!isGameObjectContext)
        {
            skinnedMeshRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Skinned Mesh Renderer", skinnedMeshRenderer, typeof(SkinnedMeshRenderer), true);

            if (skinnedMeshRenderer && GUILayout.Button("Duplicate and Setup Skinned Mesh"))
            {
                DuplicateAndSetup();
            }

            if (duplicatedSkinnedMeshRenderer && GUILayout.Button("Calculate Islands"))
            {
                CalculateIslands();
            }
        }
        
        if (islands.Count > 0 && GUILayout.Button(isRaycastEnabled ? "Disable Raycast" : "Enable Raycast"))
        {
            ToggleRaycast();
        }

        porcess_options();

        GUI.enabled = skinnedMeshRenderer != null && Island_Index.Count > 0;
        if (GUILayout.Button("Create Module"))
        {
            CreateModule();
            FocusCustomViewObject(defaultsceneView, skinnedMeshRenderer);
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
        
        if (previewmesh == null)
        {
            previewmesh = new Mesh();
        }


        SceneView.duringSceneGui += OnSceneGUI;
        isRaycastEnabled = false;

        if (skinnedMeshRenderer != null)
        {
            duplicatedSkinnedMeshRenderer = Instantiate(skinnedMeshRenderer);
        }

        defaultsceneView = SceneView.sceneViews.Count > 0 ? (SceneView)SceneView.sceneViews[0] : null;
        processend();

    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        SceneView.RepaintAll();
        processend();
        FocusCustomViewObject(defaultsceneView, skinnedMeshRenderer);
    }

    private void processend()
    {
        if (highlightManager != null)
        {
            DestroyImmediate(highlightManager);
        }
        if (duplicatedSkinnedMeshRenderer != null)
        {
            DestroyImmediate(duplicatedSkinnedMeshRenderer.transform.parent.gameObject);
        }
        if (customRenderer != null)
        {
            DestroyImmediate(customRenderer.transform.parent.gameObject);
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
            UnityEngine.Debug.Log($"middleVertex: {stopwatch.ElapsedMilliseconds} ms");
        }

    float cameraDistance = 0.2f;
    Vector3 direction = sceneView.camera.transform.forward;
    Vector3 newCameraPosition = middleVertex - direction * cameraDistance;

    UnityEngine.Debug.Log(middleVertex);
    sceneView.LookAt(middleVertex, Quaternion.Euler(0, 180, 0), cameraDistance);

    sceneView.Repaint();
}

    private void EnsureHighlightManagerExists()
    {
        if (highlightManager == null)
        {
            highlightManager = duplicatedSkinnedMeshRenderer.gameObject.AddComponent<HighlightEdgesManager>();
            highlightManager.SkinnedMeshRenderer = duplicatedSkinnedMeshRenderer;
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
        islands = MeshIslandUtility.GetIslands(duplicatedSkinnedMeshRenderer);
        stopwatch.Stop();
        UnityEngine.Debug.Log($"Calculate Islands: {stopwatch.ElapsedMilliseconds} ms");
        UnityEngine.Debug.Log($"Islands count: {islands.Count}");
        Island_Index.Clear();
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
            if (EditorRaycastHelper.IsHitObjectSpecified(extendedHit, duplicatedSkinnedMeshRenderer.gameObject))
            {   
                int triangleIndex = extendedHit.triangleIndex;
                int index = MeshIslandUtility.GetIslandIndexFromTriangleIndex(duplicatedSkinnedMeshRenderer, triangleIndex, islands);
                if (index != previousIslandIndex)
                {
                    HighlightIslandEdges(index);
                    previousIslandIndex = index;
                }
            }
        }
        else
        {
            previousIslandIndex = -1;
            HighlightIslandEdges();
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        //FocusCustomSceneView(customsceneView);
        if (!isRaycastEnabled || duplicatedSkinnedMeshRenderer == null) return;

        //HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        double currentTime = EditorApplication.timeSinceStartup;
        if (currentTime - lastUpdateTime >= raycastInterval)
        {
            lastUpdateTime = currentTime;
            PerformRaycast();
        }

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            if (!Island_Index.Contains(previousIslandIndex) && previousIslandIndex != -1)
            {
                Island_Index.Add(previousIslandIndex);

                Stopwatch stopwatch = new Stopwatch();

                stopwatch.Start();
                var verticesToremove = islands[previousIslandIndex].Vertices.ToList();
                stopwatch.Stop();
                //UnityEngine.Debug.Log("verticesToRemove: " + stopwatch.ElapsedMilliseconds + " ms");

                stopwatch.Start();
                previewmesh = MeshDeletionUtility.RemoveVerticesUsingDegenerateTriangles(duplicatedSkinnedMeshRenderer, verticesToremove);
                stopwatch.Stop();
                //UnityEngine.Debug.Log("updatedMesh: " + stopwatch.ElapsedMilliseconds + " ms");

                stopwatch.Start();
                duplicatedSkinnedMeshRenderer.sharedMesh = previewmesh;
                stopwatch.Stop();
                //UnityEngine.Debug.Log("sharedMesh: " + stopwatch.ElapsedMilliseconds + " ms");

                UpdateCustomViewMesh();
            }
            Repaint();
            //FocusCustomSceneView(customsceneView);   
        }
        //HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

    }


    private void HighlightIslandEdges()
    {
        HashSet<(int, int)> edgesToHighlight = new HashSet<(int, int)>();
        highlightManager.HighlightEdges(edgesToHighlight, duplicatedSkinnedMeshRenderer);
    }

    private void HighlightIslandEdges(int islandIndex)
    {
        Island island = islands[islandIndex];
        HashSet<(int, int)> edgesToHighlight = new HashSet<(int, int)>();
        foreach (var edge in island.AllEdges)
        {
            edgesToHighlight.Add((edge.Item1, edge.Item2));
        }
        highlightManager.HighlightEdges(edgesToHighlight, duplicatedSkinnedMeshRenderer);
    }

    private void SaveModule(List<int> vertices)
    {
        stopwatch.Restart();
        Mesh newMesh = MeshDeletionUtility.DeleteMeshByKeepVertices(skinnedMeshRenderer, vertices);
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
        new ModuleCreator(_Settings).CheckAndCopyBones(skinnedMeshRenderer.gameObject);
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
        duplicatedSkinnedMeshRenderer = new ModuleCreator(settings).PreciewMesh(skinnedMeshRenderer.gameObject);

        Mesh originalMesh = duplicatedSkinnedMeshRenderer.sharedMesh;
        previewmesh = Instantiate(originalMesh);
        duplicatedSkinnedMeshRenderer.sharedMesh = previewmesh;

        FocusCustomViewObject(defaultsceneView, duplicatedSkinnedMeshRenderer);

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

        if (duplicatedSkinnedMeshRenderer != null)
        {
            customViewObject = Instantiate(duplicatedSkinnedMeshRenderer.transform.parent.gameObject, duplicatedSkinnedMeshRenderer.transform.parent.transform.position + Vector3.right * 10, Quaternion.identity);
            customRenderer = customViewObject.GetComponentInChildren<SkinnedMeshRenderer>();
            FocusCustomViewObject(customsceneView, customRenderer);

            var emptyVerticesList = new List<int>(); // Start with an empty list to disable all vertices initially
            customViewMesh = MeshDeletionUtility.KeepVerticesUsingDegenerateTriangles(duplicatedSkinnedMeshRenderer, emptyVerticesList);
            customRenderer.sharedMesh = customViewMesh;

            // Focus the camera on the customViewObject's bounds
        }
    }

    private void UpdateCustomViewMesh()
    {
        var allVertices = new List<int>(Island_Index.SelectMany(index => islands[index].Vertices));

        customViewMesh = MeshDeletionUtility.KeepVerticesUsingDegenerateTriangles(skinnedMeshRenderer, allVertices);

        customRenderer.sharedMesh = customViewMesh;

        SceneView.RepaintAll();
}

}