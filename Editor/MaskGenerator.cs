using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class MeshMaskGenerator
{
private int _textureSize;

public MeshMaskGenerator(int textureSize)
{
    _textureSize = textureSize;
}

public Dictionary<string, Texture2D> GenerateMaskTextures(SkinnedMeshRenderer skinnedMeshRenderer, List<int> vertexIndices, int colorindex)
{
    Mesh mesh = skinnedMeshRenderer.sharedMesh;
    Material[] materials = skinnedMeshRenderer.sharedMaterials;
    Dictionary<string, Texture2D> maskTextures = new Dictionary<string, Texture2D>();

    Color color = (colorindex == 1) ? Color.white : Color.black;
    Color drawColor = (colorindex == 1) ? Color.black : Color.white;

    for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
    {
        List<int> triangles = new List<int>();

        int[] subMeshTriangles = mesh.GetTriangles(subMeshIndex);
        for (int i = 0; i < subMeshTriangles.Length; i += 3)
        {
            int v1 = subMeshTriangles[i];
            int v2 = subMeshTriangles[i + 1];
            int v3 = subMeshTriangles[i + 2];

            if (vertexIndices.Contains(v1) && vertexIndices.Contains(v2) && vertexIndices.Contains(v3))
            {
                triangles.Add(v1);
                triangles.Add(v2);
                triangles.Add(v3);
            }
        }

        if (triangles.Count > 0)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Texture2D maskTexture = new Texture2D(_textureSize, _textureSize);
            Color[] colors = new Color[_textureSize * _textureSize];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = color;
            }

            for (int i = 0; i < triangles.Count; i += 3)
            {
                Vector2 uv1 = mesh.uv[triangles[i]];
                Vector2 uv2 = mesh.uv[triangles[i + 1]];
                Vector2 uv3 = mesh.uv[triangles[i + 2]];

                DrawTriangle(maskTexture, ref colors, uv1, uv2, uv3, drawColor);
            }

            maskTexture.SetPixels(colors);
            maskTexture.Apply();
            string materialName = materials[subMeshIndex].name;
            maskTextures[materialName] = maskTexture;

            stopwatch.Stop();
            //Debug.Log(stopwatch.ElapsedMilliseconds);
        }
    }
    return maskTextures;
}

private void DrawTriangle(Texture2D texture, ref Color[] colors, Vector2 uv1, Vector2 uv2, Vector2 uv3, Color color)
{
    uv1 = new Vector2(uv1.x * _textureSize, uv1.y * _textureSize);
    uv2 = new Vector2(uv2.x * _textureSize, uv2.y * _textureSize);
    uv3 = new Vector2(uv3.x * _textureSize, uv3.y * _textureSize);

    int expansion = 2;  // 拡張範囲
    int minX = Mathf.Clamp(Mathf.Min((int)uv1.x, (int)uv2.x, (int)uv3.x) - expansion, 0, _textureSize - 1);
    int maxX = Mathf.Clamp(Mathf.Max((int)uv1.x, (int)uv2.x, (int)uv3.x) + expansion, 0, _textureSize - 1);
    int minY = Mathf.Clamp(Mathf.Min((int)uv1.y, (int)uv2.y, (int)uv3.y) - expansion, 0, _textureSize - 1);
    int maxY = Mathf.Clamp(Mathf.Max((int)uv1.y, (int)uv2.y, (int)uv3.y) + expansion, 0, _textureSize - 1);
    for (int y = minY; y <= maxY; y++)
    {
        for (int x = minX; x <= maxX; x++)
        {
            Vector2 pixel = new Vector2(x, y);
            if (IsPointInTriangle(pixel, uv1, uv2, uv3) || IsPointNearTriangle(pixel, uv1, uv2, uv3, expansion))
            {
                colors[y * _textureSize + x] = color;
            }
        }
    }
}

private bool IsPointNearTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c, int expansion)
{
    return PointDistanceToSegment(p, a, b) < expansion 
        || PointDistanceToSegment(p, b, c) < expansion 
        || PointDistanceToSegment(p, c, a) < expansion;
}

private float PointDistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
{
    Vector2 ap = p - a;
    Vector2 ab = b - a;
    float ab2 = ab.x * ab.x + ab.y * ab.y;
    float ap_ab = ap.x * ab.x + ap.y * ab.y;
    float t = Mathf.Clamp(ap_ab / ab2, 0.0f, 1.0f);
    Vector2 point = a + ab * t;
    return Vector2.Distance(p, point);
}

private bool IsPointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
{
    float area = 0.5f * (-b.y * c.x + a.y * (-b.x + c.x) + a.x * (b.y - c.y) + b.x * c.y);
    float s = 1 / (2 * area) * (a.y * c.x - a.x * c.y + (c.y - a.y) * p.x + (a.x - c.x) * p.y);
    float t = 1 / (2 * area) * (a.x * b.y - a.y * b.x + (a.y - b.y) * p.x + (b.x - a.x) * p.y);

    return s >= 0 && t >= 0 && (s + t) <= 1;
}

}