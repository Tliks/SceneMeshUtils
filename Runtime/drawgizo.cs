using System.Collections.Generic;
using UnityEngine;

public class HighlightEdgesManager : MonoBehaviour
{
    private Color _highlightColor;
    private HashSet<(int, int)> _edges = new HashSet<(int, int)>();
    private Vector3[] _vertices;
    private Transform _origin;

    public void HighlightEdges(HashSet<(int, int)> edges, Vector3[] vertices, Color highlightColor, Transform origin)
    {
        _highlightColor = highlightColor;
        _origin = origin;
        _vertices = vertices;

        _edges = edges;
    }

    public void HighlighttriangleIndices(int[] triangles, List<int> triangleIndices, Vector3[] vertices, Color highlightColor, Transform origin)
    {
        _highlightColor = highlightColor;
        _origin = origin;
        _vertices = vertices;

        _edges = GetMeshEdges(triangles, triangleIndices);

    }

    public HashSet<(int, int)> GetMeshEdges(int[] triangles, List<int> triangleIndices)
    {
        HashSet<(int, int)> edges = new HashSet<(int, int)>();

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

            edges.Add((Mathf.Min(index0, index1), Mathf.Max(index0, index1)));
            edges.Add((Mathf.Min(index1, index2), Mathf.Max(index1, index2)));
            edges.Add((Mathf.Min(index2, index0), Mathf.Max(index2, index0)));
             
        }
        return edges;
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = _highlightColor;
        foreach (var edge in _edges)
        {
            Vector3 v0 = _origin.TransformPoint(_vertices[edge.Item1]);
            Vector3 v1 = _origin.TransformPoint(_vertices[edge.Item2]);
            Gizmos.DrawLine(v0, v1);
        }
    }
}