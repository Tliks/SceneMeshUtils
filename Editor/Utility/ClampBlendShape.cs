using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ClampBlendShapeUtility
{
    private readonly SkinnedMeshRenderer _skinnedMeshRenderer;
    private readonly string _rootname;
    private readonly HashSet<int> _triangleIndices;

    public ClampBlendShapeUtility(SkinnedMeshRenderer _OriginskinnedMeshRenderer, string _rootname, HashSet<int> _SelectedTriangleIndices)
    {
        this._skinnedMeshRenderer = _OriginskinnedMeshRenderer;
        this._rootname = _rootname;
        this._triangleIndices = _SelectedTriangleIndices;
    }

    private static Mesh GenerateClampBlendShape(Mesh originalMesh, HashSet<int> triangleIndices)
    {   
        Mesh newMesh = Object.Instantiate(originalMesh);
        Vector3[] vertices = newMesh.vertices;
        int[] triangles = newMesh.triangles;

        Vector3 centroid = Vector3.zero;
        foreach (int triIndex in triangleIndices)
        {
            for (int i = 0; i < 3; i++)
            {
                int vertexIndex = triangles[triIndex * 3 + i];
                centroid += vertices[vertexIndex];
            }
        }

        centroid /= triangleIndices.Count * 3;

        Vector3[] blendShapeVertices = new Vector3[vertices.Length];
        foreach (int triIndex in triangleIndices)
        {
            for (int i = 0; i < 3; i++)
            {
                int vertexIndex = triangles[triIndex * 3 + i];
                blendShapeVertices[vertexIndex] = centroid - vertices[vertexIndex];
            }
        }

        string blendShapeName = "ClampBlendShape";
        List<string> blendShapeNames = GetBlendShapeNames(newMesh);
        string uniqueBlendShapeName = GetUniqueBlendShapeName(blendShapeNames, blendShapeName);

        newMesh.AddBlendShapeFrame(uniqueBlendShapeName, 100.0f, blendShapeVertices, null, null);
        Debug.Log($"Blend shape '{uniqueBlendShapeName}' has been added.");
        return newMesh;
    }

    private static List<string> GetBlendShapeNames(Mesh mesh)
    {
        List<string> blendShapeNames = new List<string>();
        int blendShapeCount = mesh.blendShapeCount;
        for (int i = 0; i < blendShapeCount; i++)
        {
            blendShapeNames.Add(mesh.GetBlendShapeName(i));
        }
        return blendShapeNames;
    }

    private static string GetUniqueBlendShapeName(List<string> blendShapeNames, string baseName)
    {
        string uniqueName = baseName;
        int counter = 1;
        while (blendShapeNames.Contains(uniqueName))
        {
            uniqueName = baseName + "_" + counter++;
        }

        return uniqueName;
    }

    private void ReplaceMesh()
    {
        Mesh newMesh = GenerateClampBlendShape(_skinnedMeshRenderer.sharedMesh, _triangleIndices);

        string path = AssetPathUtility.GenerateMeshPath(_rootname, "ClampMesh");
        AssetDatabase.CreateAsset(newMesh, path);
        AssetDatabase.SaveAssets();

        _skinnedMeshRenderer.sharedMesh = newMesh;
    }

    public void RendergenerateClamp()
    {
        EditorGUILayout.Space();
        GUI.enabled = _triangleIndices.Count > 0;
        if (GUILayout.Button(LocalizationEditor.GetLocalizedText("Utility.BlendShape")))
        {
            ReplaceMesh();
        }
        GUI.enabled = true;
    }
}
