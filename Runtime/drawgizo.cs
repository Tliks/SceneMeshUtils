using System.Collections.Generic;
using UnityEngine;

public class HighlightEdgesManager : MonoBehaviour
{
    [HideInInspector]
    public SkinnedMeshRenderer SkinnedMeshRenderer;
    [HideInInspector]
    public Color HighlightColor ;
    //public Color highlightColor = new Color(255f / 255f, 50f / 255f, 0f / 255f, 1f);

    private HashSet<(int, int)> edgesToHighlight = new HashSet<(int, int)>();

    public void HighlightEdges(HashSet<(int, int)> edges, SkinnedMeshRenderer skinnedMeshRenderer, Color highlightColor)
    {
        HighlightColor = highlightColor;
        edgesToHighlight = edges;
        SkinnedMeshRenderer = skinnedMeshRenderer;
    }

    private void OnDrawGizmos()
    {
        if (SkinnedMeshRenderer == null) return;

        Mesh mesh = SkinnedMeshRenderer.sharedMesh;

        Vector3[] vertices = mesh.vertices;

        Gizmos.color = HighlightColor;
        foreach (var edge in edgesToHighlight)
        {
            DrawEdge(vertices, edge.Item1, edge.Item2);
        }
    }

    private void DrawEdge(Vector3[] vertices, int index1, int index2)
    {
        Vector3 v0 = SkinnedMeshRenderer.transform.TransformPoint(vertices[index1]);
        Vector3 v1 = SkinnedMeshRenderer.transform.TransformPoint(vertices[index2]);
        Gizmos.DrawLine(v0, v1);
    }
}