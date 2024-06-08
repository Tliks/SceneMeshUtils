using System.Collections.Generic;
using UnityEngine;

public class HighlightEdgesManager : MonoBehaviour
{
    private Color HighlightColor;
    //public Color highlightColor = new Color(255f / 255f, 50f / 255f, 0f / 255f, 1f);

    private HashSet<(int, int)> edgesToHighlight = new HashSet<(int, int)>();
    private Vector3[] Vertices;
    private Transform Origin;

    public void HighlightEdges(HashSet<(int, int)> edges, Vector3[] vertices, Color highlightColor, Transform origin)
    {
        HighlightColor = highlightColor;
        edgesToHighlight = edges;
        Vertices = vertices;
        Origin = origin;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = HighlightColor;
        foreach (var edge in edgesToHighlight)
        {
            DrawEdge(Vertices, edge.Item1, edge.Item2);
        }
    }

    private void DrawEdge(Vector3[] vertices, int index1, int index2)
    {
        Vector3 v0 = Origin.TransformPoint(vertices[index1]);
        Vector3 v1 = Origin.TransformPoint(vertices[index2]);
        Gizmos.DrawLine(v0, v1);
    }
}