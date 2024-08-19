#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace com.aoyon.modulecreator
{

    [CustomEditor(typeof(TransformPolygonUtility))]
    public class TransformPolygonUtilityEditor : Editor
    {

        public static void Initialize(SkinnedMeshRenderer orignalSkinnedMeshRenderer, HashSet<int> triangleIndices)
        {   
            CustomAnimationMode.StopAnimationMode();
            TransformPolygonUtility transformPolygonUtility = orignalSkinnedMeshRenderer.gameObject.AddComponent<TransformPolygonUtility>();
            Mesh bakedMesh = new Mesh();
            orignalSkinnedMeshRenderer.BakeMesh(bakedMesh);

            Vector3 middleVertex = Vector3.zero;
            Vector3[] vertices = bakedMesh.vertices;

            Transform origin = orignalSkinnedMeshRenderer.transform;
            for (int i = 0; i < vertices.Length; i++)
            {
                middleVertex += origin.position + origin.rotation * vertices[i];
            }
            middleVertex /= vertices.Length;

            string rootname = CheckUtility.CheckRoot(orignalSkinnedMeshRenderer.gameObject).name;

            Mesh newMesh = Object.Instantiate(orignalSkinnedMeshRenderer.sharedMesh);
            transformPolygonUtility.Initialize(orignalSkinnedMeshRenderer, rootname, newMesh, triangleIndices, middleVertex);
        }
        
        private void OnEnable()
        {
            Undo.undoRedoPerformed += UpdateMesh;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= UpdateMesh;
        }
        
        private void UpdateMesh()
        {
            TransformPolygonUtility targetScript = (TransformPolygonUtility)target;
            HashSet<int> vertexIndices = MeshUtility.GetPolygonVertexIndices(targetScript.originalMesh, targetScript.triangleIndices);
            Mesh newMesh = TransformVertices(targetScript.originalMesh, vertexIndices, targetScript.position, targetScript.rotation, targetScript.scale);
            targetScript.origSkinnedMeshRenderer.sharedMesh = newMesh;
        }

        /*
        private void SaveMesh(TransformPolygonUtility targetScript)
        {
            UpdateMesh();
            string path = AssetPathUtility.GenerateMeshPath(targetScript.rootname, "TransformedMesh");
            AssetDatabase.CreateAsset(targetScript.origSkinnedMeshRenderer.sharedMesh, path);
            AssetDatabase.SaveAssets();
        }
        */

        private Mesh TransformVertices(Mesh mesh, HashSet<int> vertexIndices, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            Mesh newMesh = Object.Instantiate(mesh);
            Vector3[] vertices = newMesh.vertices;
            Quaternion rotationQuat = Quaternion.Euler(rotation);

            foreach (int index in vertexIndices)
            {
                Vector3 vertex = vertices[index];

                vertex += position;
                //vertex = rotationQuat * vertex;
                //vertex = Vector3.Scale(vertex, scale);

                vertices[index] = vertex;
            }

            // 変更した頂点をメッシュに設定
            newMesh.vertices = vertices;

            // メッシュを更新
            newMesh.RecalculateBounds();
            newMesh.RecalculateNormals();
            return newMesh;
        }

        protected virtual void OnSceneGUI()
        {
            TransformPolygonUtility targetScript = (TransformPolygonUtility)target;

            EditorGUI.BeginChangeCheck();

            switch (Tools.current)
            {
                case Tool.Move:
                    Vector3 newPosition = Handles.PositionHandle(targetScript.position, Quaternion.Euler(targetScript.rotation));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(targetScript, "Move Handle Change");
                        targetScript.position = newPosition;
                        UpdateMesh();
                    }
                    break;

                case Tool.Rotate:
                    Quaternion newRotation = Handles.RotationHandle(Quaternion.Euler(targetScript.rotation), targetScript.position);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(targetScript, "Rotate Handle Change");
                        targetScript.rotation = newRotation.eulerAngles;
                        UpdateMesh();
                    }
                    break;

                case Tool.Scale:
                    Vector3 newScale = Handles.ScaleHandle(targetScript.scale, targetScript.position, Quaternion.Euler(targetScript.rotation));
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(targetScript, "Scale Handle Change");
                        targetScript.scale = newScale;
                        UpdateMesh();
                    }
                    break;
            }
        }
    }
    #endif
}