using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

public class ModuleCreatorIsland : EditorWindow
{
    public SkinnedMeshRenderer skinnedMeshRenderer;
    private static ModuleCreatorSettings Settings;
    private const int MENU_PRIORITY = 49;
    private bool showAdvancedOptions = false;

    public int Island_Index = -1;
    public int count = 10;
    private bool isRaycastEnabled = false;

    private List<Island> islands = new List<Island>(); // islandsのキャッシュ

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
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        skinnedMeshRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField("Skinned Mesh Renderer", skinnedMeshRenderer, typeof(SkinnedMeshRenderer), true);

        if (skinnedMeshRenderer)
        {
            // Calculate Islands ボタン
            if (GUILayout.Button("Calculate Islands"))
            {
                CalculateIslands();
            }

            // Raycast ボタン
            if (GUILayout.Button(isRaycastEnabled ? "Disable Raycast" : "Enable Raycast"))
            {
                isRaycastEnabled = !isRaycastEnabled;
            }

            // Island インデックスと Count の設定
            Island_Index = EditorGUILayout.IntField("Island Index", Island_Index);
            count = EditorGUILayout.IntField("Count", count);

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

            GUI.enabled = skinnedMeshRenderer != null && islands.Count > 0;
            if (GUILayout.Button("Create Module"))
            {
                CreateModule();
                isRaycastEnabled = false;
            }
            GUI.enabled = true;
        }
    }

    private void CalculateIslands()
    {
        Stopwatch stopwatch = new Stopwatch();
        Profiler.BeginSample("calculate islands");

        stopwatch.Start();
        islands = MeshIslandUtility.GetIslands(skinnedMeshRenderer);
        Profiler.EndSample();
        stopwatch.Stop();

        UnityEngine.Debug.Log("Calculate Islands: " + stopwatch.ElapsedMilliseconds + " ms");
        UnityEngine.Debug.Log("Islands count: " + islands.Count);
    }

    private void CreateModule()
    {
        if (Island_Index != -1)
        {
            processmain(islands[Island_Index]);
        }
        else
        {
            var sortedIslands = islands.OrderByDescending(island => island.Area).ToList();
            for (int i = 0; i < sortedIslands.Count && i < count; i++)
            {
                Island island = sortedIslands[i];
                processmain(island);
            }
        }
    }

    private void processmain(Island island)
    {
        Stopwatch stopwatch = new Stopwatch();

        Profiler.BeginSample("Delete");
        stopwatch.Start();
        Mesh newmesh = MeshDeletionUtility.DeleteMesh(skinnedMeshRenderer, island.Vertices);
        Profiler.EndSample();
        stopwatch.Stop();
        UnityEngine.Debug.Log("Delete Mesh: " + stopwatch.ElapsedMilliseconds + " ms");

        Profiler.BeginSample("saveasset");
        stopwatch.Start();
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Mesh/NewMesh.asset");
        AssetDatabase.CreateAsset(newmesh, path);
        AssetDatabase.SaveAssets();
        Profiler.EndSample();
        stopwatch.Stop();
        UnityEngine.Debug.Log("Save NewMesh: " + stopwatch.ElapsedMilliseconds + " ms");

        Settings.newmesh = newmesh;

        Profiler.BeginSample("create");
        stopwatch.Start();
        ModuleCreator moduleCreator = new ModuleCreator(Settings);
        moduleCreator.CheckAndCopyBones(skinnedMeshRenderer.gameObject);
        Profiler.EndSample();
        stopwatch.Stop();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!isRaycastEnabled || skinnedMeshRenderer == null)
            return;

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0) // 左クリックの検出
        {
            RaycastHit hit;
            if (EditorRaycastHelper.RaycastAgainstScene(out hit))
            {
                Mesh mesh = skinnedMeshRenderer.sharedMesh;

                int triangleIndex = hit.triangleIndex;

                Island_Index = MeshIslandUtility.GetIslandIndexFromTriangleIndex(skinnedMeshRenderer, triangleIndex, islands);
                //UnityEngine.Debug.Log("Updated Island Index: " + Island_Index);
                
                Repaint(); // エディタウィンドウの表示を更新

                UnityEngine.Debug.Log(triangleIndex);

                // 三角形の頂点インデックスを取得
                int vertexIndex1 = mesh.triangles[triangleIndex * 3];
                int vertexIndex2 = mesh.triangles[triangleIndex * 3 + 1];
                int vertexIndex3 = mesh.triangles[triangleIndex * 3 + 2];

                // 頂点のローカル座標を取得
                Vector3 vertex1 = mesh.vertices[vertexIndex1];
                Vector3 vertex2 = mesh.vertices[vertexIndex2];
                Vector3 vertex3 = mesh.vertices[vertexIndex3];

                // barycentric coordinates を使用して交点のローカル座標を計算
                Vector3 barycentricCoordinates = hit.barycentricCoordinate;
                Vector3 localHitPoint = vertex1 * barycentricCoordinates.x + vertex2 * barycentricCoordinates.y + vertex3 * barycentricCoordinates.z;

                // ローカル座標をワールド座標に変換
                Vector3 worldHitPoint = skinnedMeshRenderer.transform.TransformPoint(localHitPoint);

                // 交点にマーカーを表示
                //Handles.color = Color.red;
                //Handles.DrawWireCube(worldHitPoint, Vector3.one * 1000f);
                //UnityEngine.Debug.Log("Hit point: " + worldHitPoint);
                //Gizmos.DrawFrustum(worldHitPoint, 60f, 100, 1, 0.6f);

                Color highlightColor = Color.red;
                float highlightSize = 0.01f;

                // ユーティリティを使用して頂点をハイライト
                MeshIslandUtility.HighlightVertices(skinnedMeshRenderer, islands[Island_Index].BoundaryVertices, highlightColor, highlightSize);

                //UnityEngine.Debug.Log("gigi");


                CreateModule();

            }
        }
    }
}