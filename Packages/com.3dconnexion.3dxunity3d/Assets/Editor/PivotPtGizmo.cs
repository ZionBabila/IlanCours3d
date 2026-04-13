
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TDxUnity3D
{
    public class PivotPtGizmo
    {
#if false // no BoxCollider ATM.  Drawing an icon in every state.
    [DrawGizmo(GizmoType.NonSelected)]
    static void DrawPivotPt(PivotPt zone, GizmoType gizmoType)
    {
        UnityEngine.Debug.LogFormat("PivotPtGizmo::DrawPivotPt");
        BoxCollider collider = zone.gameObject.GetComponent<BoxCollider>();
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(zone.transform.position, collider.size);
    }

    [DrawGizmo(GizmoType.Selected)]
    static void DrawSelectedPivotPt(PivotPt zone, GizmoType gizmoType)
    {
        UnityEngine.Debug.LogFormat("PivotPtGizmo::DrawSelectedPivotPt");
        BoxCollider collider = zone.gameObject.GetComponent<BoxCollider>();
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(zone.transform.position, collider.size);
    }
#endif
    }
}