using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static class EdgeExtensions
{
    public static DelaunayTriangulation.Edge Next(this DelaunayTriangulation.Edge edge)
    {
        switch (edge)
        {
            case DelaunayTriangulation.Edge.AB:
                return DelaunayTriangulation.Edge.BC;
            case DelaunayTriangulation.Edge.BC:
                return DelaunayTriangulation.Edge.CA;
            case DelaunayTriangulation.Edge.CA:
                return DelaunayTriangulation.Edge.AB;
            default:
                throw new ArgumentOutOfRangeException(nameof(edge), edge, null);
        }
    }

    public static DelaunayTriangulation.Edge Prev(this DelaunayTriangulation.Edge edge)
    {
        switch (edge)
        {
            case DelaunayTriangulation.Edge.AB:
                return DelaunayTriangulation.Edge.CA;
            case DelaunayTriangulation.Edge.BC:
                return DelaunayTriangulation.Edge.AB;
            case DelaunayTriangulation.Edge.CA:
                return DelaunayTriangulation.Edge.BC;
            default:
                throw new ArgumentOutOfRangeException(nameof(edge), edge, null);
        }
    }
}

public class DelaunayTriangulation
{
    public enum Edge
    {
        AB,
        BC,
        CA,
    }

    public struct Triangle
    {
        // TODO
        public static readonly Triangle Super = new Triangle(
            new Vector2(-999999f, -999999),
            new Vector2( 999999f * 0.5f, 999999f),
            new Vector2( 999999f, -999999f));

        public Vector2 A;
        public Vector2 B;
        public Vector2 C;

        public bool HasSuperEdge => Mathf.Abs(A.x) >= 999998f ||
                                    Mathf.Abs(A.y) >= 999998f ||
                                    Mathf.Abs(B.x) >= 999998f ||
                                    Mathf.Abs(B.y) >= 999998f ||
                                    Mathf.Abs(C.x) >= 999998f ||
                                    Mathf.Abs(C.y) >= 999998f;

        public Triangle(Vector2 a, Vector2 b, Vector2 c)
        {
            A = a;
            B = b;
            C = c;
        }

        public override string ToString()
        {
            return $"{A},{B},{C}";
        }

        // 外接円を返す
        public Circle GetCircumscribedCircle()
        {
            var xa = A.x;
            var xb = B.x;
            var xc = C.x;
            var ya = A.y;
            var yb = B.y;
            var yc = C.y;

            var xa2 = xa * xa;
            var xb2 = xb * xb;
            var xc2 = xc * xc;
            var ya2 = ya * ya;
            var yb2 = yb * yb;
            var yc2 = yc * yc;

            var k = 2 * ((xb - xa) * (yc - ya) - (yb - ya) * (xc - xa));
            var cx = ((yc - ya) * (xb2 - xa2 + yb2 - ya2) + (ya - yb) * (xc2 - xa2 + yc2 - ya2)) / k;
            var cy = ((xa - xc) * (xb2 - xa2 + yb2 - ya2) + (xb - xa) * (xc2 - xa2 + yc2 - ya2)) / k;
            var center = new Vector2(cx, cy);
            var radius = Vector2.Distance(A, center);
            return new Circle(center, radius);
        }

        // 点が内側にあるか調べる
        public bool Contains(Vector2 p)
        {
            var ca = C - A;
            var ap = A - p;

            var bc = B - C;
            var cp = C - p;

            var ab = A - B;
            var bp = B - p;

            var cross1 = Vector3.Cross(new Vector3(ca.x, ca.y), new Vector3(ap.x, ap.y));
            var cross2 = Vector3.Cross(new Vector3(bc.x, bc.y), new Vector3(cp.x, cp.y));
            var cross3 = Vector3.Cross(new Vector3(ab.x, ab.y), new Vector3(bp.x, bp.y));

            return (cross1.z >= 0f && cross2.z >= 0f && cross3.z >= 0f) ||
                   (cross1.z < 0f && cross2.z < 0f && cross3.z < 0f);
        }

        public Vector2 GetIndependentPoint(Edge edge)
        {
            switch (edge)
            {
                case Edge.AB:
                    return C;
                case Edge.BC:
                    return A;
                case Edge.CA:
                    return B;
                default:
                    throw new ArgumentOutOfRangeException(nameof(edge), edge, null);
            }
        }

        public Edge GetIndependentEdge(Vector2 p)
        {
            if (A == p) return Edge.BC;
            if (B == p) return Edge.CA;
            if (C == p) return Edge.AB;
            throw new ArgumentOutOfRangeException(nameof(p), p, "An argument is not in this triangle");
        }

        // 時計周りに次の点を返す
        public Vector2 Next(Vector2 p)
        {
            if (A == p) return B;
            if (B == p) return C;
            if (C == p) return A;
            throw new ArgumentOutOfRangeException(nameof(p), p, "An argument is not in this triangle");
        }
    }

    public struct Circle
    {
        public Vector2 Center;
        public float Radius;

        public Circle(Vector2 center, float radius)
        {
            Center = center;
            Radius = radius;
        }

        public bool Contains(Vector2 point)
        {
            return (point - Center).sqrMagnitude <= Radius * Radius;
        }
    }

    public class TriangleGraphNode : IEnumerable<TriangleGraphNode>
    {
        // このNodeが表現する三角形
        public Triangle Triangle { get; }

        // 再構築後のNodeへのポインタ
        public TriangleGraphNode[] Children { get; private set; }

        // AB辺を共有するNodeへのポインタ
        public TriangleGraphNode FacedNodeAB { get; private set; }

        // BC辺を共有するNodeへのポインタ
        public TriangleGraphNode FacedNodeBC { get; private set; }

        // CA辺を共有するNodeへのポインタ
        public TriangleGraphNode FacedNodeCA { get; private set; }

        // 葉節点か
        public bool HasChild => Children != null;

        public TriangleGraphNode(Triangle triangle)
        {
            Triangle = triangle;
        }

        public TriangleGraphNode(Vector2 a, Vector2 b, Vector2 c)
        {
            Triangle = new Triangle(a, b, c);
        }

        public TriangleGraphNode Find(Vector2 point)
        {
            if (!HasChild)
            {
                return this;
            }

            foreach (var child in Children)
            {
                if (child.Triangle.Contains(point))
                {
                    return child.Find(point);
                }
            }
            throw new InvalidOperationException($"No such triangles with point {point}");
        }

        public void Split(Vector2 p)
        {
            // TODO: 点が三角形の辺上にある場合は2つに分割する

            // 点を含む三角形を3つに分割する
            var child0 = new TriangleGraphNode(Triangle.A, p, Triangle.C);
            var child1 = new TriangleGraphNode(Triangle.B, p, Triangle.A);
            var child2 = new TriangleGraphNode(Triangle.C, p, Triangle.B);

            // 辺を共有する三角形をメモしておく
            child0.FacedNodeAB = child1;
            child0.FacedNodeBC = child2;
            if (FacedNodeCA != null)
            {
                child0.FacedNodeCA = FacedNodeCA;
                var edge = FacedNodeCA.GetFacedEdge(this);
                FacedNodeCA.SetFacedNode(edge, child0);
            }

            child1.FacedNodeAB = child2;
            child1.FacedNodeBC = child0;
            child1.FacedNodeCA = FacedNodeAB;
            if (FacedNodeAB != null)
            {
                child1.FacedNodeCA = FacedNodeAB;
                var edge = FacedNodeAB.GetFacedEdge(this);
                FacedNodeAB.SetFacedNode(edge, child1);
            }

            child2.FacedNodeAB = child0;
            child2.FacedNodeBC = child1;
            if (FacedNodeBC != null)
            {
                child2.FacedNodeCA = FacedNodeBC;
                var edge = FacedNodeBC.GetFacedEdge(this);
                FacedNodeBC.SetFacedNode(edge, child2);
            }

            // Debug.Log("+3");
            Children = new[] {child0, child1, child2};
        }

        public void Flip(Edge edge)
        {
            var pairNode = GetFacedNode(edge);
            if (pairNode == null) throw new ArgumentOutOfRangeException(nameof(edge), edge, "No such faced node");

            var pairEdge = pairNode.GetFacedEdge(this);

            var i0 = Triangle.GetIndependentPoint(edge);
            var i1 = pairNode.Triangle.GetIndependentPoint(pairEdge);

            var i0Next = Triangle.Next(i0);
            var i1Next = pairNode.Triangle.Next(i1);

            var child0 = new TriangleGraphNode(i0, i0Next, i1);
            var child1 = new TriangleGraphNode(i1, i1Next, i0);

            // 隣接(辺を共有)するNodeへのポインタをセットし直す
            child0.SetFacedNode(Edge.AB, GetFacedNode(edge.Prev()));
            child0.SetFacedNode(Edge.BC, pairNode.GetFacedNode(pairEdge.Next()));
            child0.SetFacedNode(Edge.CA, child1);

            child0.FacedNodeAB?.SetFacedNode(
                child0.FacedNodeAB.GetFacedEdge(this),
                child0);
            child0.FacedNodeBC?.SetFacedNode(
                child0.FacedNodeBC.GetFacedEdge(pairNode),
                child0);

            child1.SetFacedNode(Edge.AB, pairNode.GetFacedNode(pairEdge.Prev()));
            child1.SetFacedNode(Edge.BC, GetFacedNode(edge.Next()));
            child1.SetFacedNode(Edge.CA, child0);

            child1.FacedNodeAB?.SetFacedNode(
                child1.FacedNodeAB.GetFacedEdge(pairNode),
                child1);
            child1.FacedNodeBC?.SetFacedNode(
                child1.FacedNodeBC.GetFacedEdge(this),
                child1);

            var children = new[] {child0, child1};
            // Debug.Log("+2");
            Children = children;
            pairNode.Children = children;
        }

        public TriangleGraphNode GetFacedNode(Edge edge)
        {
            switch (edge)
            {
                case Edge.AB:
                    return FacedNodeAB;
                case Edge.BC:
                    return FacedNodeBC;
                case Edge.CA:
                    return FacedNodeCA;
                default:
                    throw new ArgumentOutOfRangeException(nameof(edge), edge, "No such faced node");
            }
        }

        public Edge GetFacedEdge(TriangleGraphNode node)
        {
            if (FacedNodeAB == node) return Edge.AB;
            if (FacedNodeBC == node) return Edge.BC;
            if (FacedNodeCA == node) return Edge.CA;
            throw new ArgumentOutOfRangeException(nameof(node), node, "No such faced edge");
        }

        public IEnumerator<TriangleGraphNode> GetEnumerator()
        {
            // 深さ優先探索
            // var queue = new Queue<TriangleGraphNode>();
            var stack = new Stack<TriangleGraphNode>();
            var visited = new HashSet<TriangleGraphNode>();

            stack.Push(this);
            //queue.Enqueue(this);
            visited.Add(this);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                yield return node;

                if (node.HasChild)
                {
                    foreach (var child in node.Children)
                    {
                        if (!visited.Contains(child))
                        {
                            stack.Push(child);
                            visited.Add(child);
                        }
                    }
                }
            }
        }

        public void Clear()
        {
            FacedNodeAB = null;
            FacedNodeBC = null;
            FacedNodeCA = null;
            Children = null;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void SetFacedNode(Edge edge, TriangleGraphNode node)
        {
            switch (edge)
            {
                case Edge.AB:
                    FacedNodeAB = node;
                    break;
                case Edge.BC:
                    FacedNodeBC = node;
                    break;
                case Edge.CA:
                    FacedNodeCA = node;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(edge), edge, null);
            }
        }
    }

    public IReadOnlyList<Vector2> Points => points;

    readonly List<Vector2> points = new List<Vector2>();
    readonly TriangleGraphNode rootNode = new TriangleGraphNode(Triangle.Super);

    public void AddPoint(Vector2 p)
    {
        var node = rootNode.Find(p);
        node.Split(p);

        foreach (var child in node.Children)
        {
            var illegalEdge = child.Triangle.GetIndependentEdge(p);
            LegalizeEdge(child, illegalEdge);
        }
        points.Add(p);
    }

    public void Clear()
    {
        points.Clear();
        rootNode.Clear();
    }

    public IEnumerable<TriangleGraphNode> GetNodes()
    {
        return rootNode;
    }

    void LegalizeEdge(TriangleGraphNode node, Edge edge)
    {
        var pairNode = node.GetFacedNode(edge);
        if (pairNode == null) return;

        var pairEdge = pairNode.GetFacedEdge(node);
        var p = node.Triangle.GetIndependentPoint(edge);
        var q = pairNode.Triangle.GetIndependentPoint(pairEdge);

        var circle = node.Triangle.GetCircumscribedCircle();
        if (circle.Contains(q))
        {
            // 辺が正当ではない
            node.Flip(edge);

            // フリップした結果、正当ではなくなったかもしれない辺について再帰的に処理する
            foreach (var child in node.Children)
            {
                var illegalEdge = child.Triangle.GetIndependentEdge(p);
                LegalizeEdge(child, illegalEdge);
            }
        }
    }
}
