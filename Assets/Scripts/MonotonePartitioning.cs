using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vertex = DoublyConnectedEdgeList.Vertex;
using Edge = DoublyConnectedEdgeList.Edge;

public class MonotonePartitioning
{
    enum VertexType
    {
        Regular,
        Start,
        End,
        Split,
        Merge,
    }

    // top to down
    readonly struct VertexComparer : IComparer<Vertex>
    {
        public int Compare(Vertex a, Vertex b)
        {
            if (a.Position.y == b.Position.y)
            {
                return a.Position.x < b.Position.x ? -1 : 1;
            }
            return a.Position.y > b.Position.y ? -1 : 1;
        }
    }

    // right to left
    readonly struct EdgeComparer : IComparer<Edge>
    {
        public int Compare(Edge a, Edge b)
        {
            var a0 = a.Start.Position;
            var a1 = a.Next.Start.Position;

            var b0 = b.Start.Position;
            var b1 = b.Next.Start.Position;

            var order = 1;
            if(a0.y > b0.y)
            {
                order = -1;
                (a0, b0) = (b0, a0);
                (a1, b1) = (b1, a1);
            }

            var midx = b0.x + (b1.x - b0.x) * (b0.y - a0.y) / (b0.y - b1.y);

            if (Math.Abs(midx - a0.x) < 0.0001)
            {
                var th1 = Mathf.Atan2(a1.y - a0.y,a1.x - a0.x);
                var th2 = Mathf.Atan2(b1.y - b0.y,b1.x - b0.x);

                if(Mathf.Abs(th1 - th2) < 0.001)
                {
                    return 0;
                }

                return (th1 > th2 ? -1 : 1) * order;
            }
            return (midx > a0.x ? 1 : -1) * order;
        }
    }

    readonly IComparer<Vertex> vertexComparer = new VertexComparer();
    readonly IComparer<Edge> edgeComparer = new EdgeComparer();

    // 走査線と交差する左手のEdgeを記憶しておく
    readonly SortedSet<Edge> crossEdges;

    readonly IDictionary<Edge, Vertex> helperVertices = new Dictionary<Edge, Vertex>();
    readonly IList<(Vertex, Vertex)> partitions = new List<(Vertex, Vertex)>();
    readonly IList<DoublyConnectedEdgeList> result = new List<DoublyConnectedEdgeList>();

    public MonotonePartitioning()
    {
        crossEdges = new SortedSet<Edge>(edgeComparer);
    }

    public IList<DoublyConnectedEdgeList> Calculate(IReadOnlyList<Vector2> points)
    {
        var polygon = new DoublyConnectedEdgeList(points);

        crossEdges.Clear();
        helperVertices.Clear();
        partitions.Clear();

        var sortedVertices = polygon
            .Select(edge => edge.Start)
            .OrderBy(v => v, vertexComparer);

        foreach (var v in sortedVertices)
        {
            var type = DetectVertexType(v);
            switch (type)
            {
                case VertexType.Start:
                    HandleStartVertex(v);
                    break;
                case VertexType.End:
                    HandleEndVertex(v);
                    break;
                case VertexType.Split:
                    HandleSplitVertex(v);
                    break;
                case VertexType.Merge:
                    HandleMergeVertex(v);
                    break;
                default:
                    HandleRegularVertex(v);
                    break;
            }
        }

        // 分割結果を元に複数のポリゴン表現をつくって返す
        result.Clear();

        // 新しいポリゴンのペアをつくる
        foreach (var (a, b) in partitions)
        {
            var (p1, p2) = DoublyConnectedEdgeList.Partition(a, b);
            AddResult(p1);
            AddResult(p2);
        }

        if (result.Count <= 0) result.Add(polygon);
        return result;
    }

    void HandleStartVertex(Vertex v)
    {
        crossEdges.Add(v.Edge);
        helperVertices[v.Edge] = v;
    }

    void HandleEndVertex(Vertex v)
    {
        var prevEdge = v.Edge.Prev;
        var helperVert = helperVertices[prevEdge];
        if (DetectVertexType(helperVert) == VertexType.Merge)
        {
            partitions.Add((v, helperVert));
        }
        crossEdges.Remove(prevEdge);
    }

    void HandleSplitVertex(Vertex v)
    {
        var leftEdge = FindLeftSideCrossEdge(v);
        var helperVert = helperVertices[leftEdge];
        partitions.Add((v, helperVert));

        crossEdges.Add(leftEdge);
        helperVertices[leftEdge] = v;

        // ?
        crossEdges.Add(v.Edge);
        helperVertices[v.Edge] = v;
    }

    void HandleMergeVertex(Vertex v)
    {
        var prevEdge = v.Edge.Prev;
        var helperVertPrev = helperVertices[prevEdge];
        if (DetectVertexType(helperVertPrev) == VertexType.Merge)
        {
            partitions.Add((v, helperVertPrev));
        }

        var edge = FindLeftSideCrossEdge(v);
        var helperVert = helperVertices[edge];
        if (DetectVertexType(helperVert) == VertexType.Merge)
        {
            partitions.Add((v, helperVert));
        }

        helperVertices[edge] = v;
    }

    void HandleRegularVertex(Vertex v)
    {
        // 多角形全体が右側にあるか
        if (v.IsLeftHandSideOfPolygon())
        {
            var prevEdge = v.Edge.Prev;
            var helperVert = helperVertices[prevEdge];
            if (DetectVertexType(helperVert) == VertexType.Merge)
            {
                partitions.Add((v, helperVert));
            }
            crossEdges.Remove(prevEdge);

            crossEdges.Add(v.Edge);
            helperVertices[v.Edge] = v;
        }
        else
        {
            var edge = FindLeftSideCrossEdge(v);
            var helperVert = helperVertices[edge];
            if (DetectVertexType(helperVert) == VertexType.Merge)
            {
                partitions.Add((v, helperVert));
            }
            helperVertices[edge] = v;
        }
    }

    VertexType DetectVertexType(Vertex v)
    {
        var left = v.Edge.Opposite.Next.Next.Start;
        var right = v.Edge.Next.Start;

        var leftCompared = vertexComparer.Compare(v, left);
        var rightCompared = vertexComparer.Compare(v, right);

        if (leftCompared < 0 && rightCompared < 0)
        {
            return v.IsOver180Degrees ? VertexType.Split : VertexType.Start;
        }

        if (leftCompared > 0 && rightCompared > 0)
        {
            return v.IsOver180Degrees ? VertexType.Merge : VertexType.End;
        }

        return VertexType.Regular;
    }

    // 走査線と重なっているなかでの頂点のすぐ左のEdgeを探す
    Edge FindLeftSideCrossEdge(Vertex v)
    {
        foreach (var e in crossEdges)
        {
            if (!v.IsLeftHandSideOf(e))
            {
                return e;
            }
        }
        throw new InvalidOperationException($"No such left side cross edge {v}");
    }

    void AddResult(DoublyConnectedEdgeList value)
    {
        var root = value.First();
        foreach (var convex in result)
        {
            foreach (var e in convex)
            {
                if (e == root) return;
            }
        }
        result.Add(value);
    }
}
