using UnityEngine;
using UnityEngine.UI;

public class LiquidPanel : Graphic
{
    [SerializeField]
    Vector2Int controlSegments = new Vector2Int(2, 2);

    [SerializeField]
    int vertexCount = 100;

    [SerializeField]
    CatMullRommSpline spline = new CatMullRommSpline(true);

    public int ControlPointCount => spline.ControlPoints.Count;

    readonly MonotonePartitioning partitioning = new MonotonePartitioning();
    readonly ConvexTriangulation triangulation = new ConvexTriangulation();

    Vector2[] points;

    void Awake()
    {
        points = new Vector2[vertexCount];
    }

    void OnValidate()
    {
        UpdateGeometry();
    }

    void OnDrawGizmos()
    {
        var currentPoint = spline.GetPoint(0f);
        var segments = 200;
        for (var i = 0; i < segments; i++)
        {
            var t = i / (float)segments;
            var nextPoint = spline.GetPoint(t);
            Debug.DrawLine(currentPoint, nextPoint);
            currentPoint = nextPoint;
        }

//        var rect = GetPixelAdjustedRect();
//        var points = new Vector2[vertexCount];
//        for (var i = 0; i < vertexCount; i++)
//        {
//            var t = i / (float)vertexCount;
//            points[i] = Spline.GetPoint(t);
//        }
//
//        var polygon = new DoublyConnectedEdgeList(points);
//        foreach (var e in polygon)
//        {
//            Debug.DrawLine(ConvertVertex(e.Start), ConvertVertex(e.Next.Start), Color.cyan);
//            UnityEditor.Handles.Label(ConvertVertex(e.Start), e.Start.Id.ToString());
//        }
//
//        var partitions = partitioning.Calculate(points);
//        foreach (var x in partitions)
//        {
//            foreach (var e in x)
//            {
//                var v = e.Start;
//                Debug.DrawLine(
//                    ConvertVertex(v),
//                    ConvertVertex(v.Edge.Next.Start),
//                    Color.red);
//            }
//
//            var triangulations = triangulation.Calculate(x);
//            foreach (var (a, b, c) in triangulations)
//            {
//                Debug.DrawLine(ConvertVertex(a), ConvertVertex(b), Color.yellow);
//            }
//        }
//
//        Vector3 ConvertVertex(DoublyConnectedEdgeList.Vertex v)
//        {
//            var localPos = new Vector3(rect.xMin + v.Position.x * rect.width, rect.yMin + v.Position.y * rect.height);
//            return transform.TransformPoint(localPos);
//        }
    }

    public Vector3 GetPoint(float t)
    {
        var rect = GetPixelAdjustedRect();
        var p = spline.GetPoint(t);
        var localPos = new Vector3(rect.xMin + p.x * rect.width, rect.yMin + p.y * rect.height);
        return transform.TransformPoint(localPos);
    }

    public Vector3 GetControlPoint(int i)
    {
        var rect = GetPixelAdjustedRect();
        var p = spline.GetControlPoint(i);
        var localPos = new Vector3(rect.xMin + p.x * rect.width, rect.yMin + p.y * rect.height);
        return transform.TransformPoint(localPos);
    }

    public void SetControlPoint(int i, Vector3 worldPos)
    {
        var rect = GetPixelAdjustedRect();
        var localPos = transform.InverseTransformPoint(worldPos);
        var p = new Vector3((localPos.x - rect.xMin) / rect.width, (localPos.y - rect.yMin) / rect.height);
        spline.SetControlPoint(i, p);
        UpdateGeometry();
    }

    public void ResetSpline()
    {
        spline.Clear();

        for (var y = controlSegments.y; y >= 0; y--)
        {
            spline.AddControlPoint(new Vector3(0f, (float)y / controlSegments.y));
        }

        for (var x = 1; x <= controlSegments.x; x++)
        {
            spline.AddControlPoint(new Vector3((float)x / controlSegments.x, 0f));
        }

        for (var y = 1; y <= controlSegments.y; y++)
        {
            spline.AddControlPoint(new Vector3(1f, (float)y / controlSegments.y));
        }

        for (var x = controlSegments.x - 1; x >= 1; x--)
        {
            spline.AddControlPoint(new Vector3((float)x / controlSegments.x, 1f));
        }
        UpdateGeometry();
    }

    public void ResetVertices(int v)
    {
        vertexCount = v;
        points = new Vector2[v];
        UpdateGeometry();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (points == null)
            points = new Vector2[vertexCount];

        var rect = GetPixelAdjustedRect();
        for (var i = 0; i < vertexCount; i++)
        {
            var t = i / (float)vertexCount;
            var p = spline.GetPoint(t);
            points[i] = p;

            vh.AddVert(
                new Vector3(rect.xMin + p.x * rect.width, rect.yMin + p.y * rect.height),
                Color.white,
                p);
        }

        var partitions = partitioning.Calculate(points);
        foreach (var convex in partitions)
        {
            var triangulations = triangulation.Calculate(convex);
            foreach (var (a, b, c) in triangulations)
            {
                vh.AddTriangle(a.Id, b.Id, c.Id);
            }
        }
    }
}
