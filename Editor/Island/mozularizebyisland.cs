using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Collections.Concurrent;

public class ModuleCreatorIsland : EditorWindow
{
    public SkinnedMeshRenderer skinnedMeshRenderer;
    private static ModuleCreatorSettings Settings;
    private const int MENU_PRIORITY = 49;
    private bool showAdvancedOptions = false;

    public List<int> Island_Index = new List<int>();
    private bool isRaycastEnabled = false;

    private List<Island> islands = new List<Island>();
    private Dictionary<int, Mesh> islandMeshes = new Dictionary<int, Mesh>();
    private int previousIslandIndex = -1;

    private HighlightEdgesManager highlightManager;

    private Stopwatch stopwatch = new Stopwatch();
    private const double raycastInterval = 0.01;
    private double lastUpdateTime = 0;

    [MenuItem("Window/Module Creator/Modularize Mesh by Island")]
    public static void ShowWindow()
    {
        GetWindow<ModuleCreatorIsland>("Module Creator");
    }

    private void OnEnable()
    {
        Settings = new ModuleCreatorSettings
        {
            IncludePhysBone = false,
            IncludePhysBoneColider = false
        };
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.update += Update;

    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        ClearPreviewMesh();
        if (highlightManager != null)
        {
            DestroyImmediate(highlightManager.gameObject);
        }
    }

    private void OnGUI()
    {
        skinnedMeshRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Skinned Mesh Renderer", skinnedMeshRenderer, typeof(SkinnedMeshRenderer), true);
        if (skinnedMeshRenderer)
        {
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
            }
            GUI.enabled = true;
        }
    }

    private void EnsureHighlightManagerExists()
    {
        if (highlightManager == null)
        {
            highlightManager = skinnedMeshRenderer.gameObject.AddComponent<HighlightEdgesManager>();
            highlightManager.SkinnedMeshRenderer = skinnedMeshRenderer;
        }
    }

    private void ToggleRaycast()
    {
        isRaycastEnabled = !isRaycastEnabled;
        if (!isRaycastEnabled)
        {
            ClearPreviewMesh();
        }
        else
        {
            EnsureHighlightManagerExists();
        }
    }

    private void CalculateIslands()
    {
        stopwatch.Restart();
        islands = MeshIslandUtility.GetIslands(skinnedMeshRenderer);
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
    }

    private void ClearPreviewMesh()
    {
        if (highlightManager != null)
        {
            highlightManager.HighlightEdges(new HashSet<(int, int)>(), skinnedMeshRenderer);
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!isRaycastEnabled || skinnedMeshRenderer == null) return;

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        
        if (!isRaycastEnabled || skinnedMeshRenderer == null)
            return;

        double currentTime = EditorApplication.timeSinceStartup;
        if (currentTime - lastUpdateTime >= raycastInterval)
        {
            lastUpdateTime = currentTime;
            PerformRaycast();
        }

        //UnityEngine.Debug.Log($"click?");
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            //UnityEngine.Debug.Log($"click");
            if (!Island_Index.Contains(previousIslandIndex))
            {
                Island_Index.Add(previousIslandIndex);
            }
            Repaint();
        }

    }

    private void Update()
    {
    }

    private void HighlightIslandEdges(int islandIndex)
    {
        UnityEngine.Debug.Log($"raycast1");
        if (highlightManager == null) EnsureHighlightManagerExists();

        Island island = islands[islandIndex];
        HashSet<(int, int)> edgesToHighlight = new HashSet<(int, int)>();
        foreach (var edge in island.BoundaryVertices)
        {
            edgesToHighlight.Add((edge.Item1, edge.Item2));
        }

        UnityEngine.Debug.Log($"raycast");
        highlightManager.HighlightEdges(edgesToHighlight, skinnedMeshRenderer);
    }

    private void PerformRaycast()
    {
        if (EditorRaycastHelper.RaycastAgainstScene(out RaycastHit hit))
        {
            int index = MeshIslandUtility.GetIslandIndexFromTriangleIndex(skinnedMeshRenderer, hit.triangleIndex, islands);
            if (index != previousIslandIndex)
            {
                HighlightIslandEdges(index);
                previousIslandIndex = index;
            }
        }
    }

    private void SaveModule(List<int> vertices)
    {
        stopwatch.Restart();
        Mesh newMesh = MeshDeletionUtility.DeleteMesh(skinnedMeshRenderer, vertices);
        stopwatch.Stop();
        UnityEngine.Debug.Log($"Delete Mesh: {stopwatch.ElapsedMilliseconds} ms");

        stopwatch.Restart();
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Mesh/NewMesh.asset");
        AssetDatabase.CreateAsset(newMesh, path);
        AssetDatabase.SaveAssets();
        stopwatch.Stop();
        UnityEngine.Debug.Log($"Save NewMesh: {stopwatch.ElapsedMilliseconds} ms");

        Settings.newmesh = newMesh;
        stopwatch.Restart();
        new ModuleCreator(Settings).CheckAndCopyBones(skinnedMeshRenderer.gameObject);
        stopwatch.Stop();
    }


    private void porcess_options()
    {   
        EditorGUILayout.LabelField("Island Indices", string.Join(", ", Island_Index));

        EditorGUILayout.Space();

        // PhysBone Options
        Settings.IncludePhysBone = EditorGUILayout.Toggle("PhysBone ", Settings.IncludePhysBone);

        GUI.enabled = Settings.IncludePhysBone;
        Settings.IncludePhysBoneColider = EditorGUILayout.Toggle("PhysBoneColider", Settings.IncludePhysBoneColider);
        GUI.enabled = true;

        EditorGUILayout.Space();

        // Advanced Options
        showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Options");
        if (showAdvancedOptions)
        {
            GUI.enabled = Settings.IncludePhysBone;
            GUIContent content_at = new GUIContent("Additional Transforms", "Output Additional PhysBones Affected Transforms for exact PhysBone movement");
            Settings.RemainAllPBTransforms = EditorGUILayout.Toggle(content_at, Settings.RemainAllPBTransforms);

            GUIContent content_ii = new GUIContent("Include IgnoreTransforms", "Output PhysBone's IgnoreTransforms");
            Settings.IncludeIgnoreTransforms = EditorGUILayout.Toggle(content_ii, Settings.IncludeIgnoreTransforms);

            GUIContent content_rr = new GUIContent(
                "Rename RootTransform",
                "Not Recommended: Due to the specifications of modular avatar, costume-side physbones may be deleted in some cases, so renaming physbone RootTransform will ensure that the costume-side physbones are integrated. This may cause duplication.");
            Settings.RenameRootTransform = EditorGUILayout.Toggle(content_rr, Settings.RenameRootTransform);

            GUI.enabled = true;

            GUIContent content_sr = new GUIContent("Specify Root Object", "The default root object is the parent object of the specified skinned mesh renderer object");
            Settings.RootObject = (GameObject)EditorGUILayout.ObjectField(content_sr, Settings.RootObject, typeof(GameObject), true);
        }

        EditorGUILayout.Space();
    }


    /*
    private void Update()
    {
        if (!isRaycastEnabled || skinnedMeshRenderer == null || islands == null || islands.Count == 0)
            return;

        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        RaycastHit hit;

        if (EditorRaycastHelper.RaycastAgainstScene(out hit))
        {
            int triangleIndex = hit.triangleIndex;
            int currentIslandIndex = MeshIslandUtility.GetIslandIndexFromTriangleIndex(skinnedMeshRenderer, triangleIndex, islands);

            if (currentIslandIndex != previousIslandIndex)
            {
                previousIslandIndex = currentIslandIndex;

                if (!Island_Index.Contains(currentIslandIndex))
                {
                    Island_Index.Add(currentIslandIndex);
                    CreatePreviewMesh(islands[currentIslandIndex]);
                    Repaint();
                }
            }
        }
    }
    */
}