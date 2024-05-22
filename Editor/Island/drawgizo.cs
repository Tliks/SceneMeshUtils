using System.Collections.Generic;
using UnityEngine;

public class HighlightEdgesGizmo : MonoBehaviour
{
    public SkinnedMeshRenderer skinnedMeshRenderer;
    public Color highlightColor = new Color(255f / 255f, 50f / 255f, 0f / 255f, 1f);

    // 指定されたエッジをハイライト
    public void HighlightEdges(HashSet<(int, int)> edgesToHighlight)
    {
        if (skinnedMeshRenderer == null)
            return;

        Mesh mesh = new Mesh();
        skinnedMeshRenderer.BakeMesh(mesh);

        Vector3[] vertices = mesh.vertices;

        Gizmos.color = highlightColor;
        foreach (var edge in edgesToHighlight)
        {
            DrawEdge(vertices, edge.Item1, edge.Item2);
        }
    }

    private void DrawEdge(Vector3[] vertices, int index1, int index2)
    {
        Vector3 v0 = skinnedMeshRenderer.transform.TransformPoint(vertices[index1]);
        Vector3 v1 = skinnedMeshRenderer.transform.TransformPoint(vertices[index2]);
        Gizmos.DrawLine(v0, v1);
    }
}