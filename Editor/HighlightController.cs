using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class HighlightEdgesManager
{
    static List<Vector3> linePoints = new List<Vector3>();
    private static Color highlightColor = Color.cyan;

    public static void PrepareEdgeHighlights(HashSet<(int, int)> edges, Vector3[] vertices, Transform origin)
    {
        if (edges == null || vertices == null || origin == null) return;

        linePoints.Clear();

        foreach (var edge in edges)
        {
            Vector3 v0 = origin.TransformPoint(vertices[edge.Item1]);
            Vector3 v1 = origin.TransformPoint(vertices[edge.Item2]);
            linePoints.Add(v0);
            linePoints.Add(v1);
        }
    }

    public static void PrepareTriangleHighlights(int[] triangles, HashSet<int> triangleIndices, Vector3[] vertices, Transform origin)
    {
        if (triangles == null || triangleIndices == null || vertices == null || origin == null) return;

        linePoints.Clear();

        foreach (int triangleIndex in triangleIndices)
        {
            if (triangleIndex < 0 || triangleIndex >= triangles.Length / 3)
            {
                Debug.LogError("Invalid triangle index.");
                continue;
            }

            int index0 = triangles[triangleIndex * 3];
            int index1 = triangles[triangleIndex * 3 + 1];
            int index2 = triangles[triangleIndex * 3 + 2];

            Vector3 v0 = origin.TransformPoint(vertices[index0]);
            Vector3 v1 = origin.TransformPoint(vertices[index1]);
            Vector3 v2 = origin.TransformPoint(vertices[index2]);

            linePoints.Add(v0); linePoints.Add(v1);
            linePoints.Add(v1); linePoints.Add(v2);
            linePoints.Add(v2); linePoints.Add(v0);
        }
    }

    public static void ClearHighlights()
    {
        linePoints.Clear();
    }

    public static void SetHighlightColor(Color color)
    {
        highlightColor = color;
    }

    public static void DrawHighlights()
    {
        Handles.color = highlightColor;
        Handles.DrawLines(linePoints.ToArray());
    }
}
