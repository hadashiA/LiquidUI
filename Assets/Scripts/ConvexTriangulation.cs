using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Vertex = DoublyConnectedEdgeList.Vertex;

public class ConvexTriangulation
{
    readonly Stack<Vertex> stack = new Stack<Vertex>();
    readonly IList<(Vertex, Vertex, Vertex)> result = new List<(Vertex, Vertex, Vertex)>();

    public IList<(Vertex, Vertex, Vertex)> Calculate(DoublyConnectedEdgeList convex)
    {
        stack.Clear();
        result.Clear();

        var sortedVertex = convex
            .Select(x => x.Start)
            .OrderByDescending(v => v.Position.y)
            .ThenBy(v => v.Position.x)
            .ToArray();

        var last = default(Vertex);

        var i = 0;
        foreach (var v in sortedVertex)
        {
            if (i++ < 2)
            {
                stack.Push(v);
                last = v;
                continue;
            }

            // 現在の頂点とスタックの頂点が別のエッジ
            var prev = stack.Peek();
            var vIsLeft = v.IsLeftHandSideOfPolygon();
            var prevIsLeft = prev.IsLeftHandSideOfPolygon();
            if (vIsLeft != prevIsLeft)
            {
                while (stack.Count > 0)
                {
                    var back = stack.Pop();
                    // スタックの最後の頂点はすでに連結されている
                    if (stack.Count > 0)
                    {
                        result.Add((v, back, stack.Peek()));
                    }
                }
                stack.Push(last);
                stack.Push(v);
            }
            // 同じエッジ
            else
            {
                var back = default(Vertex);
                // スタックの最初は同一辺上なので無視
                if (stack.Count > 0)
                {
                    back = stack.Pop();
                }
                while (stack.Count > 0)
                {
                    // ポリゴンの内側だったら対角線をひく
                    if (IsInnerPolygon(convex, v, stack.Peek()))
                    {
                        back = stack.Pop();
                        result.Add((v, back, prev));
                    }
                    else
                    {
                        break;
                    }
                }
                if (back != null)
                {
                    stack.Push(back);
                }
                stack.Push(v);
            }

            last = v;
        }

        for (var j = 1; j < stack.Count - 1; j++)
        {
            result.Add((last, stack.Pop(), stack.Peek()));
        }

        if (result.Count <= 0)
        {
            AddResult(last, last.Edge.Next.Start, last.Edge.Next.Next.Start);
        }
        else
        {
            AddResult(last, last.Edge.Prev.Start, last.Edge.Next.Start);
        }
        return result;
    }

    void AddResult(Vertex a, Vertex b, Vertex c)
    {
        if (a.Position.x >= b.Position.x)
        {
            result.Add((a, b, c));
        }
        else
        {
            result.Add((a, c, b));
        }
    }

    // 対角線がポリゴンの内側にあるならtrue
    bool IsInnerPolygon(DoublyConnectedEdgeList polygon, Vertex a, Vertex b)
    {
        // 外周であればfalse
        if (a.Edge.Next.Start.Id == b.Id || b.Edge.Next.Start.Id == a.Id)
        {
            return false;
        }

        // TODO: 計算量
        var leftExists = false;
        var rightExists = false;
        foreach (var edge in polygon)
        {
            if (edge.Start.Id == a.Id || edge.Start.Id == b.Id)
            {
                continue;
            }

            // 交差しているedgeが存在しているか調べる
            if (edge.LineSegmentsIntersection(a.Position, b.Position))
            {
                return false;
            }

            if (edge.Start.IsLeftHandSideOf(a.Position, b.Position))
            {
                leftExists = true;
            }
            else
            {
                rightExists = true;
            }
        }

        return leftExists && rightExists;
    }
}
