using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DoublyConnectedEdgeList : IEnumerable<DoublyConnectedEdgeList.Edge>
{
    public class Vertex
    {
        public readonly int Id;
        public Vector2 Position;
        public Edge Edge;

        public Edge ReverseEdge => Edge.Opposite.Next;

        public Vertex(int id)
        {
            Id = id;
        }

        public override string ToString()
        {
            return $"{Id} ({Position})";
        }

        // 線分の右手側にあるか左手側にあるか調べる。左ならtrue
        public bool IsLeftHandSideOf(Vector2 a, Vector2 b)
        {
            if (a.y > b.y)
            {
                (a, b) = (b, a);
            }
            var p = Position;

            var ab = b - a;
            var ap = p - a;

            var cross = Vector3.Cross(new Vector3(ab.x, ab.y), new Vector3(ap.x, ap.y));
            return cross.z >= 0f;
        }

        public bool IsLeftHandSideOf(Edge edge)
        {
            return IsLeftHandSideOf(edge.Start.Position, edge.Next.Start.Position);
        }

        // ポリゴン全体の左の辺上にあるか
        // TODO: この判定は点が反時計まわりであることを仮定してる
        public bool IsLeftHandSideOfPolygon()
        {
            var prev = Edge.Opposite.Next.Next.Start.Position;
            var next = Edge.Next.Start.Position;
            return ((prev.y > Position.y) || (prev.y == Position.y && prev.x < Position.x)) &&
                    ((next.y < Position.y) || (next.y == Position.y && next.x > Position.x));
        }

        // この頂点を中心にした二辺のなす角が180度より大きい場合にtrue
        public bool IsOver180Degrees
        {
            get
            {
                var a = Edge.Opposite.Next.Next.Start.Position;
                var b = Position;
                var c = Edge.Next.Start.Position;

                var signedTriangleArea = 0.5f * (a.x * b.y + b.x * c.y + c.x * a.y - a.y * b.x- b.y * c.x - c.y * a.x);
                return signedTriangleArea < 0f;
            }
        }
    }

    public class Edge
    {
        public Vertex Start;
        public Edge Next;
        public Edge Opposite;

        public Edge Prev => Opposite.Next.Opposite;

        public override string ToString()
        {
            return $"{Start} -> {Next.Start}";
        }

        // 2点を結ぶ線分が交差しているかどうか
        public bool LineSegmentsIntersection(Vector2 b1, Vector2 b2)
        {
            var a1 = Start.Position;
            var a2 = Next.Start.Position;

            var ta = (b1.x - b2.x) * (a1.y - b1.y) + (b1.y - b2.y) * (b1.x - a1.x);
            var tb = (b1.x - b2.x) * (a2.y - b1.y) + (b1.y - b2.y) * (b1.x - a2.x);
            var tc = (a1.x - a2.x) * (b1.y - a1.y) + (a1.y - a2.y) * (a1.x - b1.x);
            var td = (a1.x - a2.x) * (b2.y - a1.y) + (a1.y - a2.y) * (a1.x - b2.x);
            return tc * td < 0 && ta * tb < 0;
        }
    }

    readonly struct EdgeIterator : IEnumerable<Edge>
    {
        readonly Edge root;

        public EdgeIterator(Edge root)
        {
            this.root = root;
        }

        public IEnumerator<Edge> GetEnumerator()
        {
            yield return root;
            var current = root.Next;
            while (current.Start.Id != root.Start.Id)
            {
                yield return current;
                current = current.Next;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    readonly Edge root;

    public DoublyConnectedEdgeList(IReadOnlyList<Vector2> points)
    {
        // TODO: 一次配列つくらないで初期化したい
        var edges = new Edge[points.Count];

        var lastVert = new Vertex(points.Count - 1) { Position = points.Last() };
        var lastEdge = new Edge { Start = lastVert };

        for (var i = 0; i < points.Count; i++)
        {
            var vert = new Vertex(i) { Position = points[i] };
            var edge = new Edge { Start = vert };
            vert.Edge = edge;

            lastEdge.Next = edge;
            lastEdge = edge;

            edges[i] = edge;
        }

        lastEdge.Next = edges.First();

        // 反対まわりの参照をつくる
        var nextOppositeVert = new Vertex(0) { Position = points.First() };
        var nextOppositeEdge = new Edge { Start = nextOppositeVert };

        for (var i = 0; i < edges.Length; i++)
        {
            var edge = edges[i];
            var nextIndex = i + 1 < edges.Length ? i + 1 : 0;
            var nextEdge = edges[nextIndex];

            var oppositeVert = new Vertex(nextIndex) { Position = nextEdge.Start.Position };
            var oppositeEdge = new Edge { Start = oppositeVert };
            oppositeVert.Edge = oppositeEdge;
            oppositeEdge.Next = nextOppositeEdge;

            oppositeEdge.Opposite = edge;
            edge.Opposite = oppositeEdge;

            nextOppositeEdge = oppositeEdge;
        }

        root = edges.First();
        root.Opposite.Next = nextOppositeEdge;
    }

    public DoublyConnectedEdgeList(Edge root)
    {
        this.root = root;
    }

    public IEnumerator<Edge> GetEnumerator()
    {
        return new EdgeIterator(root).GetEnumerator();
    }

    public IEnumerable<Edge> Reverse()
    {
        return new EdgeIterator(root.Opposite);
    }

    // 2つに分割する
    public static (DoublyConnectedEdgeList, DoublyConnectedEdgeList) Partition(Vertex a, Vertex b)
    {
        var i = 0;
        foreach (var e in new DoublyConnectedEdgeList(a.Edge))
        {
            if (i++ > 1000)
            {
                Debug.Log(e);
            }
        }
        i = 0;
        foreach (var e in new DoublyConnectedEdgeList(a.Edge.Opposite))
        {
            if (i++ > 1000)
            {
                Debug.Log(e);
            }
        }
        foreach (var e in new DoublyConnectedEdgeList(b.Edge))
        {
            if (i++ > 1000)
            {
                Debug.Log(e);
            }
        }
        i = 0;
        foreach (var e in new DoublyConnectedEdgeList(b.Edge.Opposite))
        {
            if (i++ > 1000)
            {
                Debug.Log(e);
            }
        }

        var toLowerB = b.Edge.Opposite.Next.Opposite;
        var fromLowerB = b.Edge.Opposite.Next;

        // ---- 上半分

        // New vert of B
        var newBUpper = new Vertex(b.Id) { Position = b.Position };
        newBUpper.Edge = new Edge { Start = newBUpper };
        newBUpper.Edge.Next = b.Edge.Next;

        // New vert of A
        var newAUpper = new Vertex(a.Id) { Position = a.Position };
        newAUpper.Edge = new Edge { Start = newAUpper };
        newAUpper.Edge.Next = newBUpper.Edge;

        // Reverse of A
        var newAUpperReverse = new Vertex(a.Id) { Position = a.Position };
        newAUpperReverse.Edge = new Edge { Start = newAUpperReverse };
        newAUpperReverse.Edge.Next = a.Edge.Opposite.Next.Next;
        newAUpperReverse.Edge.Opposite = a.Edge.Opposite.Next.Opposite;
        newAUpperReverse.Edge.Opposite.Opposite = newAUpperReverse.Edge;
        newAUpperReverse.Edge.Opposite.Next = newAUpper.Edge;

        // Reverse of B
        var newBUpperReverse = new Vertex(b.Id) { Position = b.Position };
        newBUpperReverse.Edge = new Edge { Start = newBUpperReverse };
        newBUpperReverse.Edge.Next = newAUpperReverse.Edge;
        newBUpperReverse.Edge.Opposite = a.Edge;
        newBUpperReverse.Edge.Opposite = newBUpperReverse.Edge;

        // Opposite of newAUpper
        newAUpper.Edge.Opposite = newBUpperReverse.Edge;
        newAUpper.Edge.Opposite.Opposite = newAUpper.Edge.Opposite;

        // Opposite of newBUpper
        newBUpper.Edge.Opposite = b.Edge.Opposite;
        newBUpper.Edge.Opposite.Opposite = newBUpper.Edge.Opposite;

        // Refer to newAUpper
        a.Edge.Opposite.Next = a.Edge;

        // Refer to newBUpper
        b.Edge.Opposite.Next = newBUpperReverse.Edge;

        // ---- 下半分

        b.Edge = new Edge { Start = b };
        b.Edge.Next = a.Edge;

        b.Edge.Opposite = new Edge { Start = a.Edge.Opposite.Next.Start };
        b.Edge.Opposite.Next = fromLowerB;
        b.Edge.Opposite.Opposite = b.Edge;

        toLowerB.Next = b.Edge;
        a.Edge.Opposite.Next = b.Edge.Opposite;

        i = 0;
        foreach (var e in new DoublyConnectedEdgeList(newAUpper.Edge))
        {
            if (i++ > 1000)
            {
                Debug.Log(e);
            }
        }
        i = 0;
        foreach (var e in new DoublyConnectedEdgeList(newAUpper.Edge.Opposite))
        {
            if (i++ > 1000)
            {
                Debug.Log(e);
            }
        }
        i = 0;
        foreach (var e in new DoublyConnectedEdgeList(b.Edge))
        {
            if (i++ > 1000)
            {
                Debug.Log(e);
            }
        }
        i = 0;
        foreach (var e in new DoublyConnectedEdgeList(b.Edge.Opposite))
        {
            if (i++ > 1000)
            {
                Debug.Log(e);
            }
        }

        return (new DoublyConnectedEdgeList(newAUpper.Edge), new DoublyConnectedEdgeList(b.Edge));
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}