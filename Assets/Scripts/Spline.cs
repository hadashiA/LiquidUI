using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public abstract class Spline
{
    [SerializeField]
    public List<Vector3> ControlPoints = new List<Vector3>
    {
        new Vector3(1f, 0f, 0f),
        new Vector3(2f, 0f, 0f),
        new Vector3(3f, 0f, 0f),
        new Vector3(4f, 0f, 0f),
    };

    [SerializeField]
    public bool Closed;

    public int CurveCount => (ControlPoints.Count - 1) / 3;

    public Spline(bool closed)
    {
        Closed = closed;
    }

    public abstract Vector3 GetPoint(float t);
    public abstract Vector3 GetTangent(float t);

    public void AddControlPoint(Vector3 point)
    {
        ControlPoints.Add(Vector3.zero);
        SetControlPoint(ControlPoints.Count - 1, point);
    }

    public void Clear()
    {
        ControlPoints.Clear();
    }

    public Vector3 GetControlPoint(int i)
    {
        return ControlPoints[i];
    }

    public void SetControlPoint(int i, Vector3 point)
    {
       ControlPoints[i] = point;
    }
}
