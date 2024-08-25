using UnityEngine;
using System.Collections.Generic;

namespace com.aoyon.scenemeshutils
{
    public class MeshMaskGenerator
    {
        private int _textureSize;
        private int _expansion;

        public MeshMaskGenerator(int textureSize, int expansion)
        {
            _textureSize = textureSize;
            _expansion= expansion;

        }

        public Dictionary<string, Texture2D> GenerateMaskTextures(SkinnedMeshRenderer skinnedMeshRenderer, HashSet<int> triangleIndices, Color[] baseColors, Color[] targetColors, Mesh mesh)
        {
            Material[] materials = skinnedMeshRenderer.sharedMaterials;
            Dictionary<string, Texture2D> maskTextures = new Dictionary<string, Texture2D>();

            int[] allTriangles = mesh.triangles;
            Dictionary<int, int> vertexToTriangleIndexMap = new Dictionary<int, int>();
            for (int i = 0; i < allTriangles.Length; i += 3)
            {
                vertexToTriangleIndexMap[allTriangles[i]] = i / 3;
                vertexToTriangleIndexMap[allTriangles[i + 1]] = i / 3;
                vertexToTriangleIndexMap[allTriangles[i + 2]] = i / 3;
            }

            for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
            {
                List<int> triangles = new List<int>();
                int[] subMeshTriangles = mesh.GetTriangles(subMeshIndex);

                for (int i = 0; i < subMeshTriangles.Length; i += 3)
                {
                    int triangleIndex = vertexToTriangleIndexMap[subMeshTriangles[i]];

                    if (triangleIndices.Contains(triangleIndex))
                    {
                        triangles.Add(subMeshTriangles[i]);
                        triangles.Add(subMeshTriangles[i + 1]);
                        triangles.Add(subMeshTriangles[i + 2]);
                    }
                }

                if (triangles.Count > 0)
                {
                    Texture2D maskTexture = new Texture2D(_textureSize, _textureSize);
                    Color[] colors = (Color[])baseColors.Clone();

                    for (int i = 0; i < triangles.Count; i += 3)
                    {
                        Vector2 uv1 = mesh.uv[triangles[i]];
                        Vector2 uv2 = mesh.uv[triangles[i + 1]];
                        Vector2 uv3 = mesh.uv[triangles[i + 2]];

                        DrawTriangle(ref colors, uv1, uv2, uv3, targetColors);
                    }

                    maskTexture.SetPixels(colors);
                    maskTexture.Apply();
                    string materialName = materials[subMeshIndex].name;
                    maskTextures[materialName] = maskTexture;
                }
            }
            return maskTextures;
        }

        private void DrawTriangle(ref Color[] colors, Vector2 uv1, Vector2 uv2, Vector2 uv3, Color[] targetColors)
        {
            uv1 = new Vector2(uv1.x * _textureSize, uv1.y * _textureSize);
            uv2 = new Vector2(uv2.x * _textureSize, uv2.y * _textureSize);
            uv3 = new Vector2(uv3.x * _textureSize, uv3.y * _textureSize);

            int minX = Mathf.Clamp(Mathf.Min((int)uv1.x, (int)uv2.x, (int)uv3.x) - _expansion, 0, _textureSize - 1);
            int maxX = Mathf.Clamp(Mathf.Max((int)uv1.x, (int)uv2.x, (int)uv3.x) + _expansion, 0, _textureSize - 1);
            int minY = Mathf.Clamp(Mathf.Min((int)uv1.y, (int)uv2.y, (int)uv3.y) - _expansion, 0, _textureSize - 1);
            int maxY = Mathf.Clamp(Mathf.Max((int)uv1.y, (int)uv2.y, (int)uv3.y) + _expansion, 0, _textureSize - 1);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 pixel = new Vector2(x, y);
                    if (IsPointInTriangle(pixel, uv1, uv2, uv3) || IsPointNearTriangle(pixel, uv1, uv2, uv3))
                    {
                        colors[y * _textureSize + x] = targetColors[y * _textureSize + x];
                    }
                }
            }
        }


        private bool IsPointNearTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            return PointDistanceToSegment(p, a, b) < _expansion 
                || PointDistanceToSegment(p, b, c) < _expansion 
                || PointDistanceToSegment(p, c, a) < _expansion;
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
}