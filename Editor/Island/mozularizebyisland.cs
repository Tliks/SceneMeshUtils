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
    private Dictionary<int, Mesh> islandMeshes = new Dictionary<int, Mesh>();
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

    [MenuItem("Window/Module Creator/Modularize Mesh by Island")]
    public static void ShowWindow()
    {
        GetWindow<ModuleCreatorIsland>("Module Creator");
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
    
    private void OnGUI()
    {
        skinnedMeshRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Skinned Mesh Renderer", skinnedMeshRenderer, typeof(SkinnedMeshRenderer), true);
        if (skinnedMeshRenderer)
        {
            if (GUILayout.Button("Duplicate and Setup Skinned Mesh"))
            {
                DuplicateAndSetup();
            }

            if (GUILayout.Button("Calculate Islands"))
            {
                CalculateIslands();
            }
    
            if (GUILayout.Button(isRaycastEnabled ? "Disable Raycast" : "Enable Raycast"))
            {
                ToggleRaycast();
            }

            porcess_options();

            GUI.enabled = skinnedMeshRenderer != null && islands.Count > 0;
            if (GUILayout.Button("Create Module"))
            {
                CreateModule();
                FocusCustomViewObject(defaultsceneView, skinnedMeshRenderer);
                processend();
            }
            GUI.enabled = true;
        }
    }

    private static void FocusCustomViewObject(SceneView sceneView, SkinnedMeshRenderer customRenderer)
    {
        Bounds bounds = customRenderer.bounds;

        float cameraDistance = 0.3f;
        //UnityEngine.Debug.Log(sceneView);
        Vector3 direction = sceneView.camera.transform.forward;
        Vector3 newCameraPosition = bounds.center - direction * cameraDistance;

        sceneView.LookAt(bounds.center, sceneView.rotation, cameraDistance);

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
            OpenCustomSceneView();
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

                UpdateCustomViewMesh(previousIslandIndex);
            }
            Repaint();
        }
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
        EditorGUILayout.LabelField("Island Indices", string.Join(", ", Island_Index));

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
        customsceneView.title = "Custom Mesh View";
        customsceneView.Show();

        if (duplicatedSkinnedMeshRenderer != null)
        {
            customViewObject = Instantiate(duplicatedSkinnedMeshRenderer.transform.parent.gameObject, duplicatedSkinnedMeshRenderer.transform.parent.transform.position + Vector3.right * 10, Quaternion.identity);
            customRenderer = customViewObject.GetComponentInChildren<SkinnedMeshRenderer>();

            var emptyVerticesList = new List<int>(); // Start with an empty list to disable all vertices initially
            customViewMesh = MeshDeletionUtility.KeepVerticesUsingDegenerateTriangles(duplicatedSkinnedMeshRenderer, emptyVerticesList);
            customRenderer.sharedMesh = customViewMesh;

            // Focus the camera on the customViewObject's bounds
            FocusCustomViewObject(customsceneView, customRenderer);
        }
    }

    private void UpdateCustomViewMesh(int islandIndex)
    {
        var allVertices = new List<int>(Island_Index.SelectMany(index => islands[index].Vertices));

        customViewMesh = MeshDeletionUtility.KeepVerticesUsingDegenerateTriangles(skinnedMeshRenderer, allVertices);

        customRenderer.sharedMesh = customViewMesh;

        SceneView.RepaintAll();
}

}