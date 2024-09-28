using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using nadena.dev.ndmf.preview;
using Stopwatch = System.Diagnostics.Stopwatch;
using Object = UnityEngine.Object;

namespace com.aoyon.scenemeshutils
{
    
    public class PreviewController
    {
        public TriangleSelectionManager TriangleSelectionManager;
        private IslandUtility _islandUtility;
        private MouseEvents _mouseEvents;
        private SceneRaycastUtility _sceneRaycastUtility;

        private SceneView _defaultSceneView;
        private CustomSceneView _customSceneView;

        private GameObject _selectedObject; 
        private GameObject _unselectedObject; 

        private SkinnedMeshRenderer _selectedMeshRenderer;
        public SkinnedMeshRenderer UnselectedMeshRenderer;

        private Mesh _originalMesh;
        private Mesh _bakedMesh;

        private Dictionary<int, int> _selectedoldToNewIndexMap;
        private Dictionary<int, int> _unselectedoldToNewIndexMap;


        public void Initialize(
            SkinnedMeshRenderer renderer,
            IReadOnlyCollection<int> defaultselection,
            string defaultSelectionName)
        {
            UnselectedMeshRenderer = renderer;

            _originalMesh = renderer.sharedMesh;
            _bakedMesh = new Mesh(); 
            renderer.BakeMesh(_bakedMesh);

            HashSet<int> allTriangleIndices = Enumerable.Range(0, _bakedMesh.triangles.Count() / 3).ToHashSet();    
            TriangleSelectionManager = new TriangleSelectionManager(allTriangleIndices, defaultselection);

            Stopwatch stopwatch = new();
            stopwatch.Start();
            _islandUtility = new IslandUtility(_bakedMesh);
            stopwatch.Stop();
            Debug.Log($"Islands Merged: {_islandUtility.GetMergedIslandCount()} of {_islandUtility.GetIslandCount()} - Elapsed Time: {stopwatch.ElapsedMilliseconds} ms");

            _sceneRaycastUtility = new();

            _mouseEvents = new MouseEvents(this);

            TriangleSelectorOverlay.Initialize(this, defaultSelectionName);
        }

        public void ShowWindow()
        {
            NDMFPreview.DisablePreviewDepth += 1;
            CustomAnimationMode.StartAnimationMode(UnselectedMeshRenderer);

            Selection.activeObject = null; 
            Selection.activeGameObject = UnselectedMeshRenderer.gameObject;

            AddpreviewObject(ref _unselectedObject, UnselectedMeshRenderer.transform.position, UnselectedMeshRenderer.transform.rotation);
            AddpreviewObject(ref _selectedObject, UnselectedMeshRenderer.transform.position + new Vector3(100, 0, -100), UnselectedMeshRenderer.transform.rotation);

            GameObject selectedroot;
            (selectedroot, _selectedMeshRenderer) = ModuleCreatorProcessor.PreviewMesh(UnselectedMeshRenderer);
            selectedroot.transform.position += new Vector3(100, 0, -100);
            selectedroot.transform.SetParent(_selectedObject.transform, true);

            _customSceneView = OpenCustomSceneView();
            TriangleSelectorOverlay.ShowOverlay(_customSceneView);
          
            _sceneRaycastUtility.AddCollider(_selectedObject, _unselectedObject);
            HighlightEdgesManager.AddComponent(_selectedObject, _unselectedObject);

            SceneView.duringSceneGui += _mouseEvents.OnSceneGUI;

            UpdateMesh();
        }

        public void Dispose()
        {
            CustomSceneView.Dispose();
            Object.DestroyImmediate(_selectedObject);
            Object.DestroyImmediate(_unselectedObject);
            SceneView.duringSceneGui -= _mouseEvents.OnSceneGUI;
            CustomAnimationMode.StopAnimationMode();
            
            if (NDMFPreview.DisablePreviewDepth < 0)
                throw new InvalidCastException($"invaild NDMFPreview.DisablePreviewDepth: {NDMFPreview.DisablePreviewDepth}");
            NDMFPreview.DisablePreviewDepth -= 1;
        }

        public void Apply()
        {
            TriangleSelectorResult result = new()
            {
                SelectedTriangleIndices = TriangleSelectionManager.GetSelectedTriangles().ToList(),
                UnSelectedTriangleIndices = TriangleSelectionManager.GetUnselectedTriangles().ToList(),

                SelectionName = TriangleSelectorOverlay.Options?.SelectionName,
                SaveMode = TriangleSelectorOverlay.Options?.SaveMode
            };

            TriangleSelector.InvokeAndDispose(result);
        }

        private void AddpreviewObject(ref GameObject obj, Vector3 position, Quaternion rotation)
        {
            obj = new GameObject();
            obj.name = "SMU preview";
            obj.transform.position = position;
            obj.transform.rotation = rotation;
        }

        private CustomSceneView OpenCustomSceneView()
        {
            _defaultSceneView = SceneView.lastActiveSceneView;
            _defaultSceneView.drawGizmos = true;
            var customSceneView = CustomSceneView.ShowWindow(_defaultSceneView);
            customSceneView.drawGizmos = true;
            CustomSceneView.FocusCustomViewObject(customSceneView, _bakedMesh, _selectedMeshRenderer.transform);

            return customSceneView;
        }

        private void UpdateMesh()
        {   
            HashSet<int> selectedtriangleIndices = TriangleSelectionManager.GetSelectedTriangles();
            HashSet<int> unselectedtriangleIndices = TriangleSelectionManager.GetUnselectedTriangles();

            Mesh selectedPreviewMesh = MeshUtility.RemoveTriangles(_originalMesh, unselectedtriangleIndices);
            Mesh unselectedPreviewMesh = MeshUtility.RemoveTriangles(_originalMesh, selectedtriangleIndices);

            _selectedMeshRenderer.sharedMesh = selectedPreviewMesh;
            UnselectedMeshRenderer.sharedMesh = unselectedPreviewMesh;

            Mesh selectedcolliderMesh = null;
            Mesh unselectedcolliderMesh = null;

            if (selectedtriangleIndices.Count > 0)
            {
                (selectedcolliderMesh, _selectedoldToNewIndexMap) = MeshUtility.ProcesscolliderMesh(_bakedMesh, selectedtriangleIndices);
            }
            if (unselectedtriangleIndices.Count > 0)
            {
                (unselectedcolliderMesh,  _unselectedoldToNewIndexMap) = MeshUtility.ProcesscolliderMesh(_bakedMesh, unselectedtriangleIndices);
            }

            _sceneRaycastUtility.UpdateColider(selectedcolliderMesh, true);
            _sceneRaycastUtility.UpdateColider(unselectedcolliderMesh, false);

            HandleUtility.Repaint();
        }


        public void HandleClick(bool updateMesh)
        {
            var options = TriangleSelectorOverlay.Options;
            if (_sceneRaycastUtility.TryRaycast(out RaycastHit hitInfo))
            {
                bool IsSelected = _sceneRaycastUtility.IsSelected(hitInfo);
                Transform origin = IsSelected ? _selectedMeshRenderer.transform : UnselectedMeshRenderer.transform;
                var indexmap = IsSelected ? _selectedoldToNewIndexMap : _unselectedoldToNewIndexMap;

                int triangleIndex = hitInfo.triangleIndex;
                triangleIndex = MeshUtility.ConvertNewTriangleIndexToOld(triangleIndex, indexmap);

                HashSet<int> TriangleIndices;
                if (options.SelectionMode == SelectionModes.Island)
                {
                    TriangleIndices = _islandUtility.GetIslandtrianglesFromTriangleIndex(triangleIndex, options.MergeSamePosition);
                }
                else if (options.SelectionMode == SelectionModes.Polygon)
                {
                    TriangleIndices = _islandUtility.GetTrianglesNearPositionInIsland(triangleIndex, hitInfo.point, options.Scale, origin);
                }
                else
                {
                    throw new InvalidOperationException();
                }
                TriangleIndices = TriangleSelectionManager.GetUniqueTriangles(TriangleIndices, IsSelected);

                if (updateMesh)
                {
                    UpdatePreview(TriangleIndices, IsSelected);
                }
                else
                {
                    HighlightPreview(TriangleIndices, IsSelected);
                }
            }
            else
            {
                HighlightEdgesManager.ClearHighlights();
            }
        }

        public void HandleDrag(bool updateMesh, Vector2 startpos, Vector2 endpos)
        {
            if (startpos.x == endpos.x || startpos.y == endpos.y) return;

            var options = TriangleSelectorOverlay.Options;
            bool IsSelected = CustomSceneView.IsSelected();
            Transform origin = IsSelected ? _selectedMeshRenderer.transform : UnselectedMeshRenderer.transform;
            
            MeshCollider meshCollider = GenerateColider(startpos, endpos);

            HashSet<int> TriangleIndices;
            if (options.SelectionMode == SelectionModes.Island)
            {
                TriangleIndices = _islandUtility.GetIslandTrianglesInCollider(meshCollider, options.MergeSamePosition, options.CheckAll, origin);
            }
            else if (options.SelectionMode == SelectionModes.Polygon)
            {
                TriangleIndices = _islandUtility.GetTrianglesInsideCollider(meshCollider, origin);
            }
            else
            {
                throw new InvalidOperationException();
            }

            Object.DestroyImmediate(meshCollider.gameObject);
            TriangleIndices = TriangleSelectionManager.GetUniqueTriangles(TriangleIndices, IsSelected);

            if (updateMesh)
            {
                UpdatePreview(TriangleIndices, IsSelected);
            }
            else
            {
                HighlightPreview(TriangleIndices, IsSelected);
            }
        }

        private void UpdatePreview(HashSet<int> TriangleIndices, bool IsSelected)
        {
            TriangleSelectionManager.UpdateSelection(TriangleIndices, IsSelected);
            UpdateMesh();
            HighlightEdgesManager.ClearHighlights();
        }

        private void HighlightPreview(HashSet<int> TriangleIndices, bool IsSelected)
        {
            Color color = IsSelected ? Color.red : Color.cyan;
            HighlightEdgesManager.SetHighlightColor(color);

            Transform origin = IsSelected ? _selectedMeshRenderer.transform : UnselectedMeshRenderer.transform;
            HighlightEdgesManager.PrepareTriangleHighlights(_bakedMesh.triangles, TriangleIndices, _bakedMesh.vertices, origin);
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

                meshCollider.convex = true;
            }
            catch
            {
                Debug.LogWarning("MeshColliderの設定中にエラーが発生しました: ");
            }

            return meshCollider;
        }

        public void PerformUndo()
        {
            TriangleSelectionManager.Undo();
            UpdateMesh();
        }

        public void PerformRedo()
        {
            TriangleSelectionManager.Redo();
            UpdateMesh();       
        }

        public void SelectAll()
        {
            TriangleSelectionManager.SelectAllTriangles();
            UpdateMesh();
        }

        public void UnselectAll()
        {
            TriangleSelectionManager.UnselectAllTriangles();
            UpdateMesh();
        }

        public void ReverseAll()
        {
            TriangleSelectionManager.ReverseAllTriangles();
            UpdateMesh();
        }


    }
}