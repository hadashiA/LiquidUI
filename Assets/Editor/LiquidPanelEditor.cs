using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LiquidPanel))]
public class LiquidPanelEditor : Editor
{
    const float HandleSize = 0.04f;
    const float PickSize = 0.06f;

    LiquidPanel panel;
    Transform handleTransform;
    Quaternion handleRotation;
    int selectedIndex = -1;

    void OnEnable()
    {
        panel = target as LiquidPanel;
        handleTransform = panel.transform;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        using (var scope = new EditorGUI.ChangeCheckScope())
        {
            var segmentsProp = serializedObject.FindProperty("controlSegments");
            EditorGUILayout.PropertyField(segmentsProp);

            if (scope.changed)
            {
                serializedObject.ApplyModifiedProperties();
                panel.ResetSpline();
            }
        }

        using (var scope = new EditorGUI.ChangeCheckScope())
        {
            var vertexSizeProp = serializedObject.FindProperty("vertexCount");
            EditorGUILayout.PropertyField(vertexSizeProp);

            if (scope.changed)
            {
                serializedObject.ApplyModifiedProperties();
                panel.ResetVertices(vertexSizeProp.intValue);
            }
        }

        using (var scope = new EditorGUI.ChangeCheckScope())
        {
            var material = (Material)EditorGUILayout.ObjectField("Material overriden",
                panel.material,
                typeof(Material),
                false);

            if (scope.changed)
            {
                Undo.RecordObject(panel, "Set material");
                panel.material = material;
                EditorUtility.SetDirty(panel);
            }
        }

        if (selectedIndex >= 0 && selectedIndex < panel.ControlPointCount)
        {
            GUILayout.Label($"Selected Point ({selectedIndex})");
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                var point = EditorGUILayout.Vector3Field("Position", panel.GetControlPoint(selectedIndex));
                if (scope.changed)
                {
                    Undo.RecordObject(panel, "Move Point");
                    panel.SetControlPoint(selectedIndex, point);
                    EditorUtility.SetDirty(panel);
                }
            }
        }
    }

    void OnSceneGUI()
    {
        handleRotation = Tools.pivotRotation == PivotRotation.Local
            ? handleTransform.rotation
            : Quaternion.identity;

        var p0 = ShowControlPoint(0);
        for (var i = 1; i < panel.ControlPointCount; i += 3)
        {
            var p1 = ShowControlPoint(i);
            var p2 = ShowControlPoint(i + 1);
            var p3 = ShowControlPoint(i + 2);

            Handles.color = Color.gray;
            Handles.DrawLine(p0, p1);
            Handles.DrawLine(p2, p3);
            p0 = p3;
        }
    }

    Vector3 ShowControlPoint(int i)
    {
        if (i >= panel.ControlPointCount || i < 0)
            return Vector3.zero;

        var p = panel.GetControlPoint(i);
        var size = HandleUtility.GetHandleSize(p);

        if (Handles.Button(p, handleRotation, HandleSize * size, PickSize * size, Handles.DotHandleCap))
        {
            selectedIndex = i;
            Repaint();
        }

        if (selectedIndex == i)
        {
            using (var scope = new EditorGUI.ChangeCheckScope())
            {
                p = Handles.DoPositionHandle(p, handleRotation);
                if (scope.changed)
                {
                    Undo.RecordObject(panel, "Move Point");
                    panel.SetControlPoint(i, p);
                    EditorUtility.SetDirty(panel);
                }
            }
        }
        return p;
    }
}
