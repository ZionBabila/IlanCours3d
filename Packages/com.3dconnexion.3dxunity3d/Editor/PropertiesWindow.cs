using UnityEngine;
using UnityEditor;


#if UNITY_EDITOR
namespace TDxUnity3D
{

    public class Settings : EditorWindow
    {
        string GameObjectName = "Main Camera";
        bool groupEnabled;
        bool playModeExclusive = true; // Whether the Editor view is active or not during PlayMode (not implemented yet)
        //float myFloat = 1.23f;

        // Add menuitem named "Settings" to the 3Dconnexion menu
        [MenuItem("Tools/3Dconnexion/Settings")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            Settings window = (Settings)EditorWindow.GetWindow(typeof(Settings));
            window.Show();
        }

        void OnGUI()
        {
            GUILayout.Label("3D Mouse Settings", EditorStyles.boldLabel);
            GUILayout.Space(10);
            GameObjectName = EditorGUILayout.TextField("PlayMode GameObject", GameObjectName);

            //groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", groupEnabled);
            playModeExclusive = EditorGUILayout.Toggle("PlayMode exclusive", playModeExclusive);
            //myFloat = EditorGUILayout.Slider("Slider", myFloat, -3, 3);
            //EditorGUILayout.EndToggleGroup();
        }
    }

    public class AboutBox : EditorWindow
    {
        [MenuItem("Tools/3Dconnexion/About")]
        static void Init()
        {
            AboutBox window = ScriptableObject.CreateInstance<AboutBox>();
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 350, 200);
            window.ShowPopup();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("TDxUnity3D plugin for using 3Dconnexion devices in Unity3D.", EditorStyles.wordWrappedLabel);
            GUILayout.Space(30);

            EditorGUILayout.LabelField("Refer to the \"LICENSE.md\" file for the terms and conditions governing the use of the 3Dconnexion software.", EditorStyles.wordWrappedLabel);
            GUILayout.Space(30);
            if (GUILayout.Button("Close")) this.Close();
        }

    }
}
#endif
