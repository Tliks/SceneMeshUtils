using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class TransformPolygonUtility
{
    private static SkinnedMeshRenderer _originskinnedMeshRenderer;
    private static string _rootname;
    private static Mesh _originalMesh;
    private static TriangleSelectionManager _triangleSelectionManager;

    private static Vector3 position = Vector3.zero;
    private static Vector3 rotation = Vector3.zero;
    private static Vector3 scale = Vector3.one;

    private static Vector3 newPosition, newRotation, newScale;


    public static void Initialize(SkinnedMeshRenderer origSkinnedMeshRenderer, string rootname, Mesh originalMesh, TriangleSelectionManager triangleSelectionManager)
    {
        _originskinnedMeshRenderer = origSkinnedMeshRenderer;
        _rootname = rootname;
        _originalMesh = originalMesh;
        _triangleSelectionManager = triangleSelectionManager;
    }

    public static void Render()
    {
        EditorGUILayout.Space();
        GUI.enabled = _triangleSelectionManager.GetSelectedTriangles().Count > 0;
        RenderTransfrom();
        EditorGUILayout.Space();
        if (GUILayout.Button(LocalizationEditor.GetLocalizedText("Utility.TransformPolygon")))
        {
            MeshPreview.StopPreview();
            SaveMesh();
            MeshPreview.StartPreview(_originskinnedMeshRenderer);
        }
        GUI.enabled = true;
    }

    private static void RenderTransfrom()
    {
        GUILayout.Label("Transformation Parameters", EditorStyles.boldLabel);
        
        using (new GUILayout.VerticalScope())
        {
            using (new GUILayout.HorizontalScope("Box"))
            {
                GUILayout.Label("Position", EditorStyles.boldLabel);
                newPosition.x = EditorGUILayout.FloatField(position.x);
                newPosition.y = EditorGUILayout.FloatField(position.y);
                newPosition.z = EditorGUILayout.FloatField(position.z);
            }
            
            using (new GUILayout.HorizontalScope("Box"))
            {
                GUILayout.Label("Rotation", EditorStyles.boldLabel);
                newRotation.x = EditorGUILayout.FloatField(rotation.x);
                newRotation.y = EditorGUILayout.FloatField(rotation.y);
                newRotation.z = EditorGUILayout.FloatField(rotation.z);
            }
            
            using (new GUILayout.HorizontalScope("Box"))
            {
                GUILayout.Label("Scale", EditorStyles.boldLabel);
                newScale.x = EditorGUILayout.FloatField(scale.x);
                newScale.y = EditorGUILayout.FloatField(scale.y);
                newScale.z = EditorGUILayout.FloatField(scale.z);
            }
        }

        if (newPosition != position || newRotation != rotation || newScale != scale)
        {
            position = newPosition;
            rotation = newRotation;
            scale = newScale;
            UpdateMesh();
        }
    }

    private static Mesh UpdateMesh()
    {
        HashSet<int> vertexIndices = MeshUtility.GetPolygonVertexIndices(_originalMesh, _triangleSelectionManager.GetSelectedTriangles());
        Mesh newMesh = TransformVertices(_originalMesh, vertexIndices, position, rotation, scale);
        
        _originskinnedMeshRenderer.sharedMesh = newMesh;
        return newMesh;
    }

    private static void SaveMesh()
    {
        Mesh mesh = UpdateMesh();
        string path = AssetPathUtility.GenerateMeshPath(_rootname, "TransformedMesh");
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
    }

    private static Mesh TransformVertices(Mesh mesh, HashSet<int> vertexIndices, Vector3 position, Vector3 rotation, Vector3 scale)
    {
        Mesh newMesh = Object.Instantiate(mesh);
        Vector3[] vertices = newMesh.vertices;
        Quaternion rotationQuat = Quaternion.Euler(rotation);

        foreach (int index in vertexIndices)
        {
            Vector3 vertex = vertices[index];

            // 移動
            vertex += position;

            // 回転
            vertex = rotationQuat * vertex;

            // スケール
            vertex = Vector3.Scale(vertex, scale);

            vertices[index] = vertex;
        }

        // 変更した頂点をメッシュに設定
        newMesh.vertices = vertices;

        // メッシュを更新
        newMesh.RecalculateBounds();
        newMesh.RecalculateNormals();
        return newMesh;
    }
}
