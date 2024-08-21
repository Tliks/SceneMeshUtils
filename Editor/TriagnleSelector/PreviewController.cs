using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Stopwatch = System.Diagnostics.Stopwatch;

namespace com.aoyon.modulecreator
{
    
    public class PreviewController
    {
        public TriangleSelectionManager _triangleSelectionManager;
        private IslandUtility _islandUtility;

        private GameObject _selectedObject; 
        private GameObject _unselectedObject; 

        private SkinnedMeshRenderer _selectedMeshRenderer;
        private SkinnedMeshRenderer _unselectedMeshRenderer;

        private Mesh _originalMesh;
        private Mesh _bakedMesh;

        private CustomSceneView _customSceneView;

        private Dictionary<int, int> _selectedoldToNewIndexMap;
        private Dictionary<int, int> _unselectedoldToNewIndexMap;

        public void Initialize(SkinnedMeshRenderer renderer, HashSet<int> defaultselection)
        {
            AddpreviewObject(ref _unselectedObject, renderer.transform.position, renderer.transform.rotation);
            AddpreviewObject(ref _selectedObject, renderer.transform.position + new Vector3(100, 0, -100), renderer.transform.rotation);

            _originalMesh = renderer.sharedMesh;
            _bakedMesh = new Mesh(); 
            renderer.BakeMesh(_bakedMesh);

            _unselectedMeshRenderer = renderer;
            GameObject selectedroot;
            (selectedroot, _selectedMeshRenderer) = ModuleCreatorProcessor.PreviewMesh(renderer);
            selectedroot.transform.position += new Vector3(100, 0, -100);
            selectedroot.transform.SetParent(_selectedObject.transform, true);            

            OpenCustomSceneView();

            HashSet<int> allTriangleIndices = Enumerable.Range(0, _bakedMesh.triangles.Count() / 3).ToHashSet();    
            _triangleSelectionManager = new TriangleSelectionManager(allTriangleIndices, defaultselection);

            Stopwatch stopwatch = new();
            stopwatch.Start();
            _islandUtility = new IslandUtility(_bakedMesh);
            stopwatch.Stop();
            Debug.Log($"Islands Merged: {_islandUtility.GetMergedIslandCount()} of {_islandUtility.GetIslandCount()} - Elapsed Time: {stopwatch.ElapsedMilliseconds} ms");

            SceneRaycastUtility.AddCollider(_selectedObject, _unselectedObject);
            HighlightEdgesManager.AddComponent(_selectedObject, _unselectedObject);

            //CustomAnimationMode.StopAnimationMode();
            CustomAnimationMode.StartAnimationMode(renderer);

            UpdateMesh();
        }

        public void Dispose()
        {
            _customSceneView.Close();
            Object.DestroyImmediate(_selectedObject);
            Object.DestroyImmediate(_unselectedObject);
            CustomAnimationMode.StopAnimationMode();
        }

        private void AddpreviewObject(ref GameObject obj, Vector3 position, Quaternion rotation)
        {
            obj = new GameObject();
            obj.name = "AAU preview";
            obj.transform.position = position;
            obj.transform.rotation = rotation;
        }

        private void OpenCustomSceneView()
        {
            SceneView defaultSceneView = SceneView.sceneViews.Count > 0 ? (SceneView)SceneView.sceneViews[0] : null;
            defaultSceneView.drawGizmos = true;
            _customSceneView = CustomSceneView.ShowWindow(defaultSceneView);
            _customSceneView.drawGizmos = true;
            FocusCustomViewObject(_customSceneView, _bakedMesh, _selectedMeshRenderer.transform);
        }

        private void FocusCustomViewObject(SceneView sceneView, Mesh mesh, Transform origin)
        {
            Vector3 middleVertex = Vector3.zero;
            Vector3[] vertices = mesh.vertices;

            for (int i = 0; i < vertices.Length; i++)
            {
                middleVertex += origin.position + origin.rotation * vertices[i];
            }
            middleVertex /= vertices.Length;

            float cameraDistance = 0.3f;
            sceneView.LookAt(middleVertex, Quaternion.Euler(0, 180, 0), cameraDistance);
            sceneView.Repaint();
        }

        private void UpdateMesh()
        {   
            HashSet<int> selectedtriangleIndices = _triangleSelectionManager.GetSelectedTriangles();
            HashSet<int> unselectedtriangleIndices = _triangleSelectionManager.GetUnselectedTriangles();

            Mesh selectedPreviewMesh = MeshUtility.RemoveTriangles(_originalMesh, unselectedtriangleIndices);
            Mesh unselectedPreviewMesh = MeshUtility.RemoveTriangles(_originalMesh, selectedtriangleIndices);

            _selectedMeshRenderer.sharedMesh = selectedPreviewMesh;
            _unselectedMeshRenderer.sharedMesh = unselectedPreviewMesh;

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

            SceneRaycastUtility.UpdateColider(selectedcolliderMesh, true);
            SceneRaycastUtility.UpdateColider(unselectedcolliderMesh, false);
        }


        public void HandleClick(bool isUpdate, bool isIsland, bool mergeSamePosition, float scale)
        {
            if (SceneRaycastUtility.TryRaycast(out RaycastHit hitInfo))
            {
                bool IsSelected = SceneRaycastUtility.IsSelected(hitInfo);
                Transform origin = IsSelected ? _selectedMeshRenderer.transform : _unselectedMeshRenderer.transform;
                var indexmap = IsSelected ? _selectedoldToNewIndexMap : _unselectedoldToNewIndexMap;

                int triangleIndex = hitInfo.triangleIndex;
                triangleIndex = MeshUtility.ConvertNewTriangleIndexToOld(triangleIndex, indexmap);

                HashSet<int> TriangleIndices;
                if (isIsland)
                {
                    TriangleIndices = _islandUtility.GetIslandtrianglesFromTriangleIndex(triangleIndex, mergeSamePosition);
                }
                else
                {
                    TriangleIndices = _islandUtility.GetTrianglesNearPositionInIsland(triangleIndex, hitInfo.point, scale, origin);
                }
                TriangleIndices = _triangleSelectionManager.GetUniqueTriangles(TriangleIndices, IsSelected);

                if (isUpdate)
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

        public void HandleDrag(bool isUpdate, bool isIsland, bool mergeSamePosition, bool checkAll, Vector2 startpos, Vector2 endpos)
        {
            if (startpos.x == endpos.x || startpos.y == endpos.y) return;

            bool IsSelected = CustomSceneView.IsSelected();
            Transform origin = IsSelected ? _selectedMeshRenderer.transform : _unselectedMeshRenderer.transform;
            
            MeshCollider meshCollider = GenerateColider(startpos, endpos);

            HashSet<int> TriangleIndices;
            if (isIsland)
            {
                TriangleIndices = _islandUtility.GetIslandTrianglesInCollider(meshCollider, mergeSamePosition, checkAll, origin);
            }
            else
            {
                TriangleIndices = _islandUtility.GetTrianglesInsideCollider(meshCollider, origin);
            }
            Object.DestroyImmediate(meshCollider.gameObject);
            TriangleIndices = _triangleSelectionManager.GetUniqueTriangles(TriangleIndices, IsSelected);

            if (isUpdate)
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
            _triangleSelectionManager.UpdateSelection(TriangleIndices, IsSelected);
            UpdateMesh();
            HighlightEdgesManager.ClearHighlights();
        }

        private void HighlightPreview(HashSet<int> TriangleIndices, bool IsSelected)
        {
            Color color = IsSelected ? Color.red : Color.cyan;
            HighlightEdgesManager.SetHighlightColor(color);

            Transform origin = IsSelected ? _selectedMeshRenderer.transform : _unselectedMeshRenderer.transform;
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
            _triangleSelectionManager.Undo();
            UpdateMesh();
        }

        public void PerformRedo()
        {
            _triangleSelectionManager.Redo();
            UpdateMesh();       
        }

        public void SelectAll()
        {
            _triangleSelectionManager.SelectAllTriangles();
            UpdateMesh();
        }

        public void UnselectAll()
        {
            _triangleSelectionManager.UnselectAllTriangles();
            UpdateMesh();
        }

        public void ReverseAll()
        {
            _triangleSelectionManager.ReverseAllTriangles();
            UpdateMesh();
        }


    }
}