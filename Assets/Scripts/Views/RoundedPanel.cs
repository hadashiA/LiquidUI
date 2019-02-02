using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoundedPanel : Graphic
{
    [SerializeField]
    int Roundness = 1;

    [SerializeField]
    Vector2Int Segments = new Vector2Int(20, 20);

    void Update()
    {
        UpdateGeometry();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        var rect = GetPixelAdjustedRect();

        vh.Clear();

        vh.AddVert(rect.center, Color.white, new Vector2(0.5f, 0.5f));

        for (var y = Segments.y; y >= 0; y--)
        {
            CreateVertex(0, y, vh, rect);
        }

        for (var x = 1; x <= Segments.x; x++)
        {
            CreateVertex(x, 0, vh, rect);
        }

        for (var y = 1; y <= Segments.y; y++)
        {
            CreateVertex(Segments.x, y, vh, rect);
        }

        for (var x = Segments.x - 1; x >= 1; x--)
        {
            CreateVertex(x, Segments.y, vh, rect);
        }

        var vertexCount = (Segments.y + 1) * 2 + (Segments.x - 1) * 2;
        for (var i = 1; i < vertexCount; i++)
        {
            vh.AddTriangle(i - 1, 0, i);
        }
        vh.AddTriangle(vertexCount - 1, 0, 1);
    }

    void CreateVertex(int x, int y, VertexHelper vh, Rect rect)
    {
        var inner = new Vector3(
            Mathf.Clamp(x, Roundness, Segments.x - Roundness),
            Mathf.Clamp(y, Roundness, Segments.y - Roundness));

        var normal = (new Vector3(x, y) - inner).normalized;
        var p = inner + normal * Roundness;

        var uv = new Vector2(p.x / Segments.x, p.y / Segments.y);
        var localPos = new Vector3(rect.xMin + uv.x * rect.width, rect.yMin + uv.y * rect.height);
        vh.AddVert(localPos, Color.white, uv);
    }
}
