using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace TDxUnity3D
{
    public class PivotPt : MonoBehaviour
    {
#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        //UnityEngine.Debug.LogFormat("PivotPt::OnDrawGizmos");
        string customName = "Packages/com.3dconnexion.tdxunity3d/Assets/Gizmos/3dx_pivot.png";
        Gizmos.DrawIcon(transform.position, customName, false);
    }
#endif

        // Start is called before the first frame update
        void Start()
        {
            //UnityEngine.Debug.LogFormat("PivotPt::Start");
        }

        // Update is called once per frame
        void Update()
        {
            //UnityEngine.Debug.LogFormat("PivotPt::Update");
        }

        public static void OnApplicationQuit()
        {
            //UnityEngine.Debug.Log(string.Format("In PivotPt.cs: PivotPt::OnApplicationQuit()"));
        }
    }
}