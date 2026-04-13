
// Uncomment the following preprocessor directive to allow logging.
//#define TDX_DEBUG_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.SceneManagement;
using TDx.SpaceMouse.Navigation3D;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System;
using System.Text;
using System.Dynamic;


// TDxInput
using System.Runtime.InteropServices;
using System.Collections.Specialized;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Runtime.Versioning;


#if UNITY_EDITOR
namespace TDxUnity3D
{

    public class CameraController : INavigation3D1
    {
        private static Navigation3D navigation3D;

        private bool bAlreadyReadMenus = false;

        //GameObject selectedObject = null;
        GameObject pivotPtObject = null;
        GameObject floorPtObject = null;

        bool recordObject = false;

        Matrix4x4 selTransform = new Matrix4x4();

        // Hit calculations
        Vector3 HitLookFromPoint = new Vector3();
        Vector3 HitLookDirection = new Vector3();
        double HitAperture = 0.0;
        bool HitSetSelectionOnly = false;

        // Displayed PivotPoint
        Vector3 PivotPointPosition = new Vector3();
        Vector3 FloorPointPosition = new Vector3();

        // GetActiveScene doesn't represent what is being shown.  It's more or less just a flag on a Scene.
        // It does indicate where new GOs will be inserted.  And is the reason _3DxPivotPt is sometimes placed in the wrong scene.
        // Keep track of the currentScene manually based on picking (RayCast)
        Scene currentScene;
        public Scene CurrentScene() {
            return (currentScene.name != null) ? currentScene : SceneManager.GetActiveScene();
        }
        public void CurrentScene(Scene s) {
            currentScene = s;
        }

        public String PointToString(Point pt) { return String.Format("{0}, {1}, {2}", pt.X, pt.Y, pt.Z); }
        public String VectorToString(Vector v) { return String.Format("{0}, {1}, {2}", v.X, v.Y, v.Z); }
        public String BoxToString(Box b) { return String.Format("Min:({0}, {1}, {2}), Max:({3}, {4}, {5})", b.Min.X, b.Min.Y, b.Min.Z, b.Max.X, b.Max.Y, b.Max.Z); }
        public String PlaneToString(TDx.SpaceMouse.Navigation3D.Plane p) { return String.Format("({0}, {1}, {2}), {3}", p.X, p.Y, p.Z, p.D); }
        public String FrustumToString(Frustum f) { return String.Format("Bottom:{0}, Far:{1}, Left:{2}, Near:{3}, Right:{4}, Top:{5}", f.Bottom, f.Far, f.Left, f.Near, f.Right, f.Top); }
        public String MatrixToString(Matrix m) { return String.Format("\n[[{0}, {1}, {2}, {3}],\n [{4}, {5}, {6}, {7}],\n [{8}, {9}, {10}, {11}],\n [{12}, {13}, {14}, {15}]]",
            m.M11, m.M12, m.M13, m.M14,
            m.M21, m.M22, m.M23, m.M24,
            m.M31, m.M32, m.M33, m.M34,
            m.M41, m.M42, m.M43, m.M44); }
        public String SceneViewToString(SceneView sv) {
            return String.Format("\n  orthographic: {0}\n  pivot: {1}\n  rotation: {2}\n  cameraDistance: {3}\n  cameraToWorldMatrix: \n{4}\n  worldToCamera: \n{5}\n  camera.Transform.position:{6}",
                sv.orthographic, sv.pivot.ToString(), sv.rotation.ToString(), sv.cameraDistance, sv.camera.cameraToWorldMatrix.ToString(), sv.camera.worldToCameraMatrix.ToString(), sv.camera.transform.position.ToString()); ; ;
        }

        public void OnKeyDown(int keycode)
        {
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: OnKeyDown({0})", keycode));
#endif
        }

        public CameraController()
        {
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.Log("TDxController.cs: CameraController");
#endif
            // Initialize connection to the navlib
            navigation3D = new Navigation3D(this);
#if TDX_DEBUG_EDITOR
            if (navigation3D == null)
            {
                UnityEngine.Debug.Log("TDxController.cs: CameraController: this.navigation3D is null");
            }
            else
            {
                UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: this.navigation3D was created successfully {0}", navigation3D));
            }
#endif
           navigation3D.ExecuteCommand += OnExecuteCommand;
           navigation3D.SettingsChanged += SettingsChangedHandler;
           navigation3D.TransactionChanged += TransactionChangedHandler;
           navigation3D.MotionChanged += MotionChangedHandler;
           navigation3D.KeyUp += KeyUpHandler;
           navigation3D.KeyDown += KeyDownHandler;

            navigation3D.Open3DMouse("3DxUnity3D");
        }

        public void CloseConnection()
        {
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.Log(String.Format("TDxController.cs: CloseConnection"));
#endif
            if (navigation3D != null)
            {
                navigation3D.ExecuteCommand -= this.OnExecuteCommand;
                navigation3D.SettingsChanged -= this.SettingsChangedHandler;
                navigation3D.TransactionChanged -= this.TransactionChangedHandler;
                navigation3D.MotionChanged -= this.MotionChangedHandler;
                navigation3D.KeyUp -= this.KeyUpHandler;
                navigation3D.KeyDown -= this.KeyDownHandler;
                navigation3D.Close();
                navigation3D = null;
            }
        }

        ~CameraController()
        {
            if (navigation3D != null)
            {
#if TDX_DEBUG_EDITOR
                UnityEngine.Debug.Log(String.Format("TDxController.cs: ~CameraController n3d is {0}", navigation3D));
#endif
                CloseConnection();
                //UnityEngine.Debug.Log(String.Format("TDxController.cs: ~CameraController after Close()"));
            }
            else
            {
#if TDX_DEBUG_EDITOR
                UnityEngine.Debug.Log(String.Format("TDxController.cs: ~CameraController n3d is null"));
#endif
            }
        }

        public static void OnApplicationQuit()
        {
            // I don't think this gets called
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.Log(string.Format("In TDxController.cs: CameraController::OnApplicationQuit()"));
#endif
            //if (navigation3D != null)
            //{
            //    CloseConnection();
            //}
        }


        static public CommandSet ReadMenus()
        {
            IntPtr hWnd = FindWindowByClassname("UnityContainerWndClass", IntPtr.Zero);  // Hopefully they are consistent with this.  OTOH, is it for this process instance?
            //IntPtr hWnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;  // doesn't work
            if (hWnd.ToInt32() != 0)
            {
                IntPtr hMenu = GetMenu(hWnd);
                CommandSet menuBar = new CommandSet("Default", "Unity3D Editor");
                //menuBar.Add(ReadMenu(hMenu, "", ""));
                ReadMenu(menuBar, hMenu, "", "");
                return menuBar;
            }
            else
            {
                return null;
            }
        }
        static public Category ReadMenu(CommandSet menuBar, IntPtr hMenuToRead, String menuName, String path)
        {
            //UnityEngine.Debug.Log(String.Format("ReadMenu: menuName:{0}, path:{1}", menuName, path));
            if (hMenuToRead.ToInt32() == 0)
                return null;

            Category thisMenuCategory = null;
            if (menuName.Length > 0)
                thisMenuCategory = new Category(menuName, menuName);

            int nMenus = GetMenuItemCount(hMenuToRead);
            for (uint i = 0; i < nMenus; i++)
            {
                // The item name
                StringBuilder itemNameSB = new StringBuilder(0x20);
                GetMenuString(hMenuToRead, i, itemNameSB, 0x20, MF_BYPOSITION);
                String itemName = itemNameSB.ToString();

                // Is it a submenu or a command?
                IntPtr hSubMenu = GetSubMenu(hMenuToRead, (int)i);
                if (hSubMenu.ToInt32() != 0)  // It's a submenu.
                {
                    if (thisMenuCategory == null)
                        menuBar.Add(ReadMenu(menuBar, hSubMenu, itemName, (path.Length == 0) ? itemName : path + "/" + itemName));
                    else
                        thisMenuCategory.Add(ReadMenu(menuBar, hSubMenu, itemName, (path.Length == 0) ? itemName : path + "/" + itemName));
                }

                else  // It's a simple command
                {
                    if (itemName.Length == 0) // separators don't have a name
                        continue;

                    // Strip shortcut
                    String label = itemName;
                    int leftLen = itemName.IndexOf('\t');
                    if (leftLen > 0)
                        label = itemName.Substring(0, leftLen); // Just the leaf name

                    // May need to remove ...

                    // Command needs to be the full path to this menu item. It is what gets returned that is executed by Unity
                    String command = label;
                    command = (path.Length == 0) ? command :  path + "/" + command;  // No leading slash

                    String description = label;  // ??
                    //UnityEngine.Debug.Log(String.Format("ReadMenu: adding command:{0}, label:{1}, description:{2}", command, label, description));
                    if (thisMenuCategory == null)
                        menuBar.Add(new Command(command, label, description));
                    else
                        thisMenuCategory.Add(new Command(command, label, description));
                }

            }

            return thisMenuCategory;
        }

            [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        // Find window by Caption only. Note you must pass IntPtr.Zero as the first parameter.
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

        // Find window by Classname only. Note you must pass IntPtr.Zero as the second parameter.
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindowByClassname(string lpClassName, IntPtr ZeroOnly);


        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr GetMenu(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr GetSubMenu(IntPtr hMenu, int nPos);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern int GetMenuItemCount(IntPtr hMenu);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern int GetMenuString(IntPtr hMenu, uint uIDItem, [System.Runtime.InteropServices.Out] System.Text.StringBuilder lpString, int nMaxCount, uint uFlag);
        internal const UInt32 MF_BYCOMMAND = 0x00000000;
        internal const UInt32 MF_BYPOSITION = 0x00000400;

        public void AddCommands(CommandTree commands)
        {
            navigation3D.AddCommands(commands);
        }

        /// <summary>
        /// Add a set of commands to the sets of commands.
        /// </summary>
        /// <param name="commands">The <see cref="CommandSet"/> to add.</param>
        public void AddCommandSet(CommandSet commands)
        {
            CommandTree tree = new CommandTree
            {
                commands
            };

            this.AddCommands(tree);
        }
        public string ActiveCommands
        {
            get
            {
                return navigation3D.ActiveCommandSet;
            }

            set
            {
                navigation3D.ActiveCommandSet = value;
            }
        }
        public bool LoadCommands()
        {
#if false
            // Try Markus' 3DxTestNL demo code
            // An CommandSet can also be considered to be a buttonbank, a menubar, or a set of toolbars
            CommandSet menuBar = new CommandSet("Default", "Unity3D Editor");

            // Create some categories / menus / tabs to the menu
            Category fileMenu = new Category("FileMenu", "File");
            fileMenu.Add(new Command("ID_OPEN", "OpenFile", "ToolTipOpenFile"));
            fileMenu.Add(new Command("ID_CLOSE", "CloseFile", "ToolTipCloseFile"));
            fileMenu.Add(new Command("ID_EXIT", "Exit", null));
            menuBar.Add(fileMenu);

            Category selectMenu = new Category("SelectMenu", "Selection");
            selectMenu.Add(new Command("ID_SELECTALL", "SelectAll", null));
            selectMenu.Add(new Command("ID_CLEARSELECTION", "ClearSelection", null));
            menuBar.Add(selectMenu);

            Category viewsMenu = new Category("ViewsMenu", "View");
            viewsMenu.Add(new Command("ID_PARALLEL", "ParallelView", "ToolTipParallelView"));
            viewsMenu.Add(new Command("ID_PERSPECTIVE", "PerspectiveView", "ToolTipPerspectiveView"));
            menuBar.Add(viewsMenu);

            Category helpMenu = new Category("HelpMenu", "Help");
            helpMenu.Add(new Command("ID_ABOUT", "About", "ToolTipAbout"));
            menuBar.Add(helpMenu);

            this.AddCommandSet(menuBar);

            this.ActiveCommands = menuBar.Id;  // Requires latest SDK (July 2020)
            return true;

#else
            CommandSet commands = ReadMenus();
            if (commands != null)
            {
                CommandTree tree = new CommandTree { commands };
                if (tree != null)
                {
                    navigation3D.AddCommands(tree);  // In 3DxTestNL, this is where the ActionSet gets added to the commands.xml file in %LAD%/3Dx/cfg/ext
                    navigation3D.ActiveCommandSet = commands.Id;  // Requires latest SDK (July 2020)
                    return true;
                }
            }
            return false;
#endif
        }

        public void Update()
        {
            if (navigation3D == null)
                return;

            if (bAlreadyReadMenus == false)
            {
                if (LoadCommands() == true)
                    bAlreadyReadMenus = true;
            }


#if false // old Input API
            if (Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift))
            {
                UnityEngine.Debug.Log("A shift key is down, flip Move Object");
            }
            if (Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl))
            {
                UnityEngine.Debug.Log("A control key is down, flip Constraint");
            }
            if (Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.LeftAlt))
            {
                UnityEngine.Debug.Log("An alt key is down, flip alternate usage");
            }
#else// Sets m_ButtonPressed as soon as a button is pressed on
            // any device.
            //bool m_ButtonPressed;
            //InputSystem.onEvent +=
            //    (eventPtr, device) =>
            //    {
            //        if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
            //            return;
            //        var controls = device.allControls;
            //        var buttonPressPoint = InputSytem.settings.defaultButtonPressPoint;
            //        for (var i = 0; i < controls.Count; ++i)
            //        {
            //            var control = controls[i] as ButtonControl;
            //            if (control == null || control.synthetic || control.noisy)
            //                continue;
            //            if (control.ReadValueFromEvent(eventPtr, out var value) && value >= buttonPressPoint)
            //            {
            //                m_ButtonPressed = true;
            //                break;
            //            }
            //        }
            //    };
#endif

            return;
        }

        private void OnExecuteCommand(object sender, CommandEventArgs eventArgs)
        {
            //this.dispatcher.InvokeIfRequired(() => this.ExecuteCommand?.Invoke(sender, eventArgs));
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.Log($"TDxController.cs: CameraController: OnExecuteCommand({eventArgs.Command})");
#endif
            EditorApplication.ExecuteMenuItem(eventArgs.Command);
        }

        private void KeyDownHandler(object sender, KeyEventArgs eventArgs)
        {
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: KeyDownHandler(Key:{0}, IsDown:{1})", eventArgs.Key, eventArgs.IsDown));
#endif
            switch (eventArgs.Key)
            {
                case TDx.SpaceMouse.Navigation3D.Key.V3DK_1:
                    // Deselect all--amazing there isn't a standard way to do this
                    Selection.activeGameObject = null;
                    SceneView.lastActiveSceneView.Repaint();
                    break;

                case TDx.SpaceMouse.Navigation3D.Key.V3DK_2:
                    // Dump SceneView values
#if TDX_DEBUG_EDITOR
                    UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: SceneView: {0}", SceneViewToString(SceneView.lastActiveSceneView)));
#endif
                    break;

                case TDx.SpaceMouse.Navigation3D.Key.V3DK_3:
                    // Dump GameObjects
                    LogGameObjects();
                    break;

                case TDx.SpaceMouse.Navigation3D.Key.V3DK_4:
                    // PreFab
                    AddPivotPt();
                    break;

                case TDx.SpaceMouse.Navigation3D.Key.V3DK_5:
                    LoadCommands();
                    break;

                case TDx.SpaceMouse.Navigation3D.Key.V3DK_6:
                    ToggleFloorPt();
                    break;

                case TDx.SpaceMouse.Navigation3D.Key.V3DK_7:
                    CloseConnection();
                    break;

                default:
                    break;
            }

        }

        private void KeyUpHandler(object sender, KeyEventArgs eventArgs)
        {
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: KeyUpHandler(Key:{0}, IsUp:{1})", eventArgs.Key, eventArgs.IsUp));
#endif
        }

        private void MotionChangedHandler(object sender, MotionEventArgs eventArgs)
        {
            //UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: MotionChangedHandler(IsNavigating: {0})", eventArgs.IsNavigating));
            recordObject = eventArgs.IsNavigating; // When motion starts, allow to record any changes done on a selected object.
            selTransform = Matrix4x4.identity;
        }

        private void SettingsChangedHandler(object sender, System.EventArgs eventArgs)
        {
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.Log("TDxController.cs: CameraController: SettingsChangedHandler()");
#endif
        }

        private void TransactionChangedHandler(object sender, TransactionEventArgs eventArgs)
        {
            //UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: TransactionChangedHandler(IsBegin:{0}, IsEnd:{1}, Transation:{2})", eventArgs.IsBegin, eventArgs.IsEnd, eventArgs.Transaction));
        }

        public Matrix GetCoordinateSystem()
        {
            TDx.SpaceMouse.Navigation3D.Matrix m = new TDx.SpaceMouse.Navigation3D.Matrix
                (1,  0,  0,  0,
                 0,  1,  0,  0,
                 0,  0, -1,  0,   // left-handed
                 0,  0,  0,  1);
            //UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: GetCoordinateSystem(): returning {0}", MatrixToString(m)));
            return m;
        }

        public TDx.SpaceMouse.Navigation3D.Matrix GetFrontView()
        {
            // May be able to get this from the SceneView, if it is not a constant.
            TDx.SpaceMouse.Navigation3D.Matrix m = new TDx.SpaceMouse.Navigation3D.Matrix(
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1);
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: GetFrontView(): returning {0}", MatrixToString(m)));
#endif
            return m;
        }

        public TDx.SpaceMouse.Navigation3D.Matrix GetCameraMatrix()
        {
            TDx.SpaceMouse.Navigation3D.Matrix m = new TDx.SpaceMouse.Navigation3D.Matrix();

            SceneView sv = SceneView.lastActiveSceneView;
            Camera svCamera = sv.camera;
            //UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: GetCameraMatrix(): SceneView is {0}", SceneViewToString(sv)));

            // Reverse Z because this matrix is reversing it.  That is, it is doing the right-handed<-->left-handed conversion (also).
            // "Camera space" is right-handed.  Everything else is left-handed in Unity.
            // We've told the navlib (GetCoordinateSystem) that all numbers will be left-handed.
            Vector3 flipV = new Vector3(1f, 1f, -1f);
            Matrix4x4 flipZ = Matrix4x4.Scale(flipV);
            Matrix4x4 unFlippedCameraToWorld = new Matrix4x4();
            unFlippedCameraToWorld = svCamera.cameraToWorldMatrix * flipZ;

            // transposing while copying to double
            m.M11 = unFlippedCameraToWorld[0, 0]; // col 1 is the X vector
            m.M12 = unFlippedCameraToWorld[1, 0];
            m.M13 = unFlippedCameraToWorld[2, 0];
            m.M14 = unFlippedCameraToWorld[3, 0];

            m.M21 = unFlippedCameraToWorld[0, 1]; // col 2 is the Y vector
            m.M22 = unFlippedCameraToWorld[1, 1];
            m.M23 = unFlippedCameraToWorld[2, 1];
            m.M24 = unFlippedCameraToWorld[3, 1];

            m.M31 = unFlippedCameraToWorld[0, 2]; // col 3 is the Z vector
            m.M32 = unFlippedCameraToWorld[1, 2];
            m.M33 = unFlippedCameraToWorld[2, 2];
            m.M34 = unFlippedCameraToWorld[3, 2];

            // This is the actual camera position.  No need to use the pivot.
            m.M41 = unFlippedCameraToWorld[0, 3]; // col 4 is the translation
            m.M42 = unFlippedCameraToWorld[1, 3];
            m.M43 = unFlippedCameraToWorld[2, 3];
            m.M44 = unFlippedCameraToWorld[3, 3];
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: GetCameraMatrix(): returning {0}", MatrixToString(m)));
            //UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: GetCameraMatrix(): returning pos: {0}, {1}, {2}", m.M41, m.M42, m.M43));
#endif
            return m;
        }

        public Vector3 CameraPosToPivotPos(SceneView sv, Vector3 cameraPos)
        {
            // The camera position is cameraDistance away from the pivot point, along the vector indicated by sv.rotation.
            Vector3 cameraToPivot = sv.rotation * new Vector3(0f, 0f, sv.cameraDistance);
            Vector3 pivotPos = new Vector3();
            pivotPos = cameraPos + cameraToPivot;
            return pivotPos;
        }

        // This should be a TDx.SpaceMouse.Navigation3D.Matrix method, with Up, Right, Forward (sets and gets)
        public Vector GetPosition(TDx.SpaceMouse.Navigation3D.Matrix m)
        {
            return new Vector(m.M41, m.M42, m.M43);
        }

        // Unity3D uses a left-handed Y-up world coordinate system
        // Maybe want a WCS subclass for these and a CCS/CameraCoordinateSystem?
        // GetCoordinateSystem alleviates the need for this conversion.
        public class WCS
        {
            static public Vector3 NavlibToHost(Vector navlibVector)
            {
                return new Vector3((float)navlibVector.X, (float)navlibVector.Y, (float)navlibVector.Z);
            }
            static public Vector3 NavlibToHost(Point navlibPoint)
            {
                return new Vector3((float)navlibPoint.X, (float)navlibPoint.Y, (float)navlibPoint.Z);
            }
            static public Vector HostToNavlib(Vector3 hostVector)
            {
                return new Vector(hostVector.x, hostVector.y, hostVector.z);
            }
            static public Matrix HostToNavlib(Matrix4x4 m)
            {
                // Transposing this during the assignment
                return new Matrix(
                    m[0, 0], m[1, 0], m[2, 0], m[3, 0],
                    m[0, 1], m[1, 1], m[2, 1], m[3, 1],
                    m[0, 2], m[1, 2], m[2, 2], m[3, 2],
                    m[0, 3], m[1, 3], m[2, 3], m[3, 3]);
            }
            static public Matrix4x4 NavlibToHost(TDx.SpaceMouse.Navigation3D.Matrix m)
            {
                var host = new Matrix4x4();
                host.SetColumn(0, new Vector4((float)m.M11, (float)m.M12, (float)m.M13, (float)m.M14));
                host.SetColumn(1, new Vector4((float)m.M21, (float)m.M22, (float)m.M23, (float)m.M24));
                host.SetColumn(2, new Vector4((float)m.M31, (float)m.M32, (float)m.M33, (float)m.M34));
                host.SetColumn(3, new Vector4((float)m.M41, (float)m.M42, (float)m.M43, (float)m.M44));

                return host;
            }
        }

        public void LogGameObjects()
        {
            UnityEngine.SceneManagement.Scene scene = CurrentScene();
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                UnityEngine.Debug.Log(String.Format("TDxController.cs: LogGameObjects: go.name = {0}", go.name));
            }
        }

        public void Test1()
        {
            UnityEngine.Debug.Log(String.Format("TDxController.cs: Test1: "));
        }

        public void AddPivotPt()
        {
            // Called from a Key press (for debugging)
            var ppt = Resources.Load("Prefabs/_3DxPivotPt") as GameObject;
            var pptInst = GameObject.Instantiate(ppt, new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f));
            pptInst.SetActive(true);
            pptInst.hideFlags = HideFlags.HideAndDontSave;

            var cpptInst = GameObject.Instantiate(pptInst);
            cpptInst.transform.parent = pptInst.transform;
            cpptInst.hideFlags = HideFlags.DontSave;
        }

        public void ToggleFloorPt()
        {
            // Called from a Key press (for debugging)

            // If it exists, destroy it, otherwise make it
            if (Find3DxFloorPt() != null)
            {
                Delete3DxFloorPt();
            }
            else
            {
                FindAdd3DxFloorPt();

                //var fpt = Resources.Load("Prefabs/_3DxFloorPt") as GameObject;
                //var fptInst = GameObject.Instantiate(fpt, new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f));
                //Instantiate(floorObject, Vector3(0f, 0f, 0f), Quaternion.Euler(0f,0f,0f));
            }
        }

        public GameObject FindAdd3DxPivotPt()
        {
            //int nScenes = UnityEngine.SceneManagement.SceneManager.sceneCount;
            UnityEngine.SceneManagement.Scene scene = CurrentScene();
            //String name = scene.name;
            //bool loaded = scene.isLoaded;
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name == "_3DxPivotPt") // probably should tag it instead of using the name
                {
                    return go;
                }
            }

            // Doesn't exist?  Create it and add it to the main scene.

            // This is a Prefab that draws an icon.  It is a MonoBehaviour.
            var pptPrefab = Resources.Load("Prefabs/_3DxPivotPt") as GameObject;
            // May want to create it in the right place, and/or visible/invisible
            pivotPtObject = GameObject.Instantiate(pptPrefab, new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 0f, 0f));
            pivotPtObject.name = "_3DxPivotPt";  // Change the name so I can find it
            pivotPtObject.SetActive(true); // set active, so only child visibility will be changed
            pivotPtObject.hideFlags = HideFlags.HideAndDontSave; // Not shown in the Hierarchy, not saved to Scene

            // workaround the fact that gameObject with hideFlags = HideFlags.HideAndDontSave is not being rendered,
            // but its children are.
            var childPivotPtObject = GameObject.Instantiate(pivotPtObject);
            childPivotPtObject.transform.parent = pivotPtObject.transform;
            childPivotPtObject.hideFlags = HideFlags.DontSave; // not saved to Scene

            // It will get created in the ActiveScene.  Move it to the "currentScene"
            SceneManager.MoveGameObjectToScene(pivotPtObject, scene);
            return pivotPtObject;
        }

        public GameObject FindAdd3DxFloorPt()
        {
            UnityEngine.SceneManagement.Scene scene = CurrentScene();
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name == "_3DxFloorPt") // probably should tag it instead of using the name
                {
                    return go;
                }
            }

            // Doesn't exist?  Create it and add it to the main scene.

            floorPtObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            floorPtObject.AddComponent<MeshFilter>();  // Is this really the only way to change the color?
            MeshRenderer nRenderer = floorPtObject.GetComponent<MeshRenderer>();
            if (nRenderer == null)
                return null;

            Material nMat = new Material(Shader.Find("Standard")); // "Universal Render Pipeline/Lit"));
            if (nMat == null)
                return null;

            nMat.color = Color.blue;
            nRenderer.material = nMat;
            LayerMask layerMask = LayerMask.GetMask("Ignore Raycast");
            floorPtObject.layer = 2; // layerMask;

            floorPtObject.name = "_3DxFloorPt";  // Change the name so I can find it

            // It will get created in the ActiveScene.  Move it to the "currentScene"
            SceneManager.MoveGameObjectToScene(floorPtObject, scene);
            return floorPtObject;
        }

        public GameObject Find3DxPivotPt()
        {
            UnityEngine.SceneManagement.Scene scene = CurrentScene();
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name == "_3DxPivotPt") // probably should tag it instead of using the name
                {
                    return go;
                }
            }
            return null;
        }

        public GameObject Find3DxFloorPt()
        {
            UnityEngine.SceneManagement.Scene scene = CurrentScene();
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.name == "_3DxFloorPt") // probably should tag it instead of using the name
                {
                    return go;
                }
            }
            return null;
        }

        public void Move3DxPivotPt()
        {
            GameObject pivotPt = FindAdd3DxPivotPt();
            if (pivotPt != null)
            {
                pivotPt.transform.position = PivotPointPosition;
            }
        }

        public void Move3DxFloorPt(SceneView sv)
        {
            GameObject floorPt = FindAdd3DxFloorPt();
            if (floorPt != null)
            {
#if TDX_DEBUG_EDITOR
                UnityEngine.Debug.LogFormat("TDxController.cs: Move3DxFloorPt({0})", FloorPointPosition.ToString());
#endif
                floorPt.transform.position = FloorPointPosition;
            }
        }

        public void Delete3DxPivotPt()
        {
            GameObject ppvo = Find3DxPivotPt();
            if (ppvo != null)
            {
                GameObject cppvo = ppvo.transform.GetChild(0).gameObject;
                if (cppvo != null)
                {
                    UnityEngine.Object.DestroyImmediate(cppvo);
                }

                UnityEngine.Object.DestroyImmediate(ppvo);
                ppvo = null;
            }
        }

        public void Delete3DxFloorPt()
        {
            GameObject fpvo = Find3DxFloorPt();
            if (fpvo != null)
            {
                UnityEngine.Object.DestroyImmediate(fpvo);
                fpvo = null;
            }
        }

        public void SetCameraMatrix(TDx.SpaceMouse.Navigation3D.Matrix m)
        {
            //UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: SetCameraMatrix({0})", MatrixToString(m)));
            if (EditorWindow.focusedWindow.ToString().Contains("SceneView"))
            {

                // Can't set this matrix on the SceneView.  Convert to transform values.
                // When can move GameObjects (including Camera GameObjects), this will have to change.
                // There should be a way of integrating those two. Possibly with a generic "current" Transform.
                SceneView sv = SceneView.lastActiveSceneView;
#if TDX_DEBUG_EDITOR
                UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: SetCameraMatrix(): SceneView is {0}", SceneViewToString(sv)));
#endif
                //UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: SetCameraMatrix(): pos: {0} {1} {2}", m.M41, m.M42, m.M43));

                // transposing during the copy to float
                Matrix4x4 svm = new Matrix4x4();
                svm[0, 0] = (float)m.M11;
                svm[1, 0] = (float)m.M12;
                svm[2, 0] = (float)m.M13;
                svm[3, 0] = (float)m.M14;

                svm[0, 1] = (float)m.M21;
                svm[1, 1] = (float)m.M22;
                svm[2, 1] = (float)m.M23;
                svm[3, 1] = (float)m.M24;

                svm[0, 2] = (float)m.M31;
                svm[1, 2] = (float)m.M32;
                svm[2, 2] = (float)m.M33;
                svm[3, 2] = (float)m.M34;

                svm[0, 3] = (float)0.0;  // zero the position, only want the rotation in the quaternion
                svm[1, 3] = (float)0.0;
                svm[2, 3] = (float)0.0;
                svm[3, 3] = (float)1.0;
                sv.rotation = svm.rotation; // convert from the matrix to a quaternion

                // pivot is cameraDistance in front of the camera
                // Set pivot after changing the orientation
                sv.pivot = CameraPosToPivotPos(sv, new Vector3((float)m.M41, (float)m.M42, (float)m.M43));

                //sv.Repaint();  // necessary?
#if TDX_DEBUG_EDITOR
                UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: SetCameraMatrix: new sv.pivot is {0} sv.rotation is {1}", sv.pivot.ToString(), sv.rotation.ToString()));
#endif
            }
        }

        public Box GetViewExtents()
        {
            // This is for an orthographics projection
            Box b = new Box(-10, -10, 0, 10, 10, 10);
            //UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: GetViewExtents(): returning {0}", BoxToString(b)));
            return b;
        }

        public void SetViewExtents(Box extents)
        {
            // This is for an orthographics projection
            //UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: SetViewExtents({0})",BoxToString(extents)));
        }

        public double GetViewFOV()
        {
            // camera.fieldOfView is the full vertical vield of view in degrees.
            // Navlib wants the horizontal field of view, in radians.

            SceneView sv = SceneView.lastActiveSceneView;
            Camera svCamera = sv.camera;
            double horizontalFOV_rads = (svCamera.fieldOfView * Math.PI / 180.0f) * svCamera.aspect;  // convert to rads and switch to horizontal direction
            //UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: GetViewFOV(): returning {0}", horizontalFOV_rads));
            return horizontalFOV_rads;
        }

        public void SetViewFOV(double fov)
        {
            // Can not be changed.  Unity calculates this based on the near distance
            //UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: SetViewFOV({0}): NOT IMPLEMENTED", fov));
        }

        public Frustum GetViewFrustum()
        {
            SceneView sv = SceneView.lastActiveSceneView;
            Camera svCamera = sv.camera;
            float aspectRatio = svCamera.aspect;      // width/height
            float nearClip = svCamera.nearClipPlane;  // in world space units
            float farClip = svCamera.farClipPlane;    // in world space units
            float verticalFOV_rad = (float)(svCamera.fieldOfView * Math.PI / 180.0f);  // vertical FOV degrees -> rads
            float tanHalfVerticalFOV = (float)Math.Abs(Math.Tan(verticalFOV_rad/2.0));
            float nearFrustumPlaneHalfHeight = nearClip * tanHalfVerticalFOV;
            float nearFrustumPlaneHalfWidth = nearFrustumPlaneHalfHeight * aspectRatio;
            double top    =  nearFrustumPlaneHalfHeight;
            double bottom = -nearFrustumPlaneHalfHeight;
            double left   = -nearFrustumPlaneHalfWidth;
            double right  =  nearFrustumPlaneHalfWidth;
            Frustum f = new Frustum(left, right, bottom, top, nearClip, farClip);
            //UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: GetViewFrustum(): returning {0}", FrustumToString(f)));
            return f;
        }

        public void SetViewFrustum(Frustum frustum)
        {
            //UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: SetViewFrustum({0})", FrustumToString(frustum)));
        }

        public bool IsViewPerspective()
        {
            SceneView sv = SceneView.lastActiveSceneView;
            Camera svCamera = sv.camera;
            bool isPerspective = !svCamera.orthographic;
            //UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: IsViewPerspective(): returning {0}", isPerspective));
            return isPerspective;
        }

        public Point GetCameraTarget()
        {
            throw new TDx.SpaceMouse.Navigation3D.NoDataException("This camera does not have a target");

#if false
            // I assume this is in world space
            // Not sure what this is for
            GameObject selectedObj = null;
            if (Selection.gameObjects.Length != 0)
            {
                foreach (GameObject go in Selection.gameObjects)
                {
                    selectedObj = go;
                }

                // (uses the last)
                Point p = new Point(selectedObj.transform.position.x, selectedObj.transform.position.y, selectedObj.transform.position.z);
                UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: GetCameraTarget(): returning selectedObj: {0}, pt: {1}",selectedObj.name, PointToString(p)));
                return p;
            }

            else // else, nothing selected, use SceneView pivot pt
            {
                SceneView sv = SceneView.lastActiveSceneView;
                Point p = new Point(sv.pivot.x, sv.pivot.y, sv.pivot.z);
                UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: GetCameraTarget(): returning SceneView.pivot: {0}",PointToString(p)));
                return p;
            }
#endif
        }

        public void SetCameraTarget(Point target)
        {
            throw new System.InvalidOperationException("This camera does not have a target");
#if false
            // Don't know what this is supposed to do.
            // Display a point (gizmo) in the view?
            // Follow another object?
            SceneView sv = SceneView.lastActiveSceneView;
            sv.pivot = new Vector3((float)target.X, (float)target.Y, (float)target.Z);
            UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: SetCameraTarget({0}): UNIMPLEMENTED", PointToString(target)));
#endif
        }

        public TDx.SpaceMouse.Navigation3D.Plane GetViewConstructionPlane()
        {
            // Plane of the front of the viewing frustum in world coords?
            SceneView sv = SceneView.lastActiveSceneView;
            TDx.SpaceMouse.Navigation3D.Vector v = new TDx.SpaceMouse.Navigation3D.Vector(0, 0, 1);
            TDx.SpaceMouse.Navigation3D.Plane p = new TDx.SpaceMouse.Navigation3D.Plane(ref v, 1);
            //UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: GetViewConstructionPlane(): returning {0}", PlaneToString(p)));
            return p;
        }

        public bool IsViewRotatable()
        {
            //UnityEngine.Debug.Log("TDxController.cs: CameraController: IsViewRotatable(): returning true");
            return true;
        }

        public void SetPointerPosition(Point position)
        {
            // Can probably create a Gizmo for this but that is a MonoBehaviour thing.  There may be a workaround or two.
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: SetPointerPosition({0})", PointToString(position)));
#endif
        }

        public Point GetPointerPosition()
        {
            Point p = new Point(0, 0, 0);
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: GetPointerPosition(): returning {0}",PointToString(p)));
#endif
            return p;
        }

        public Box GetModelExtents()
        {
            UnityEngine.SceneManagement.Scene scene = CurrentScene();
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.LogFormat("TDxController.cs: CameraController: GetModelExtents(): ActiveScene={0}",scene.name);
#endif
            Box b = new Box(Double.MaxValue, Double.MaxValue, Double.MaxValue,  // Min
                            Double.MinValue, Double.MinValue, Double.MinValue); // Max

            // That's a lot of objects
            bool bSomethingFound = false;
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                Component[] components = go.GetComponentsInChildren(typeof(/*MeshFilter*/Collider));
                if (components.Length > 0)
                {
                    foreach (/*MeshFilter*/Collider comp in components)
                    {
                        var obj = /*comp.mesh;  */ comp;
                        //UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: GetModelExtents(): name: {0} extents: {1}", go.name, obj.bounds.extents));
                        if (obj.bounds.min.x < b.Min.X) b.Min.X = obj.bounds.min.x;
                        if (obj.bounds.min.y < b.Min.Y) b.Min.Y = obj.bounds.min.y;
                        if (obj.bounds.min.z < b.Min.Z) b.Min.Z = obj.bounds.min.z;
                        if (obj.bounds.max.x > b.Max.X) b.Max.X = obj.bounds.max.x;
                        if (obj.bounds.max.y > b.Max.Y) b.Max.Y = obj.bounds.max.y;
                        if (obj.bounds.max.z > b.Max.Z) b.Max.Z = obj.bounds.max.z;
                        bSomethingFound = true;
                    }
                }
            }
            if (bSomethingFound == false)
            {
#if TDX_DEBUG_EDITOR
                UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: GetModelExtents(): No Components found for extents calculation in scene {0}", scene.name));
#endif
                // Let's use something -- just a guess
                b.Min.X = -100.0;
                b.Min.Y = -100.0;
                b.Min.Z = -100.0;
                b.Max.X =  100.0;
                b.Max.Y =  100.0;
                b.Max.Z =  100.0;
            }
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: GetModelExtents(): returning {0}",BoxToString(b)));
#endif
            return b;
        }

        public Box GetSelectionExtents()
        {
            Box b = new Box(Double.MaxValue, Double.MaxValue, Double.MaxValue,  // Min
                            Double.MinValue, Double.MinValue, Double.MinValue); // Max

            foreach (GameObject go in Selection.gameObjects)
            {
                Component[] colliders = go.GetComponentsInChildren(typeof(Collider));
                if (colliders.Length > 0)
                {
                    foreach (Collider obj in colliders)
                    {
                        //UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: GetSelectionExtents(): name: {0} extents: {1}", selectedObject.name, obj.bounds.extents));
                        if (obj.bounds.min.x < b.Min.X) b.Min.X = obj.bounds.min.x;
                        if (obj.bounds.min.y < b.Min.Y) b.Min.Y = obj.bounds.min.y;
                        if (obj.bounds.min.z < b.Min.Z) b.Min.Z = obj.bounds.min.z;
                        if (obj.bounds.max.x > b.Max.X) b.Max.X = obj.bounds.max.x;
                        if (obj.bounds.max.y > b.Max.Y) b.Max.Y = obj.bounds.max.y;
                        if (obj.bounds.max.z > b.Max.Z) b.Max.Z = obj.bounds.max.z;
                    }
                    //UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: GetSelectionExtents(): returning collider {0}", BoxToString(b)));
                    continue;
                }

                Component[] renderers = go.GetComponentsInChildren(typeof(Renderer));
                if (renderers.Length > 0)
                {
                    foreach (Renderer obj in renderers)
                    {
                        //UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: GetSelectionExtents(): name: {0} extents: {1}", selectedObject.name, obj.bounds.extents));
                        if (obj.bounds.min.x < b.Min.X) b.Min.X = obj.bounds.min.x;
                        if (obj.bounds.min.y < b.Min.Y) b.Min.Y = obj.bounds.min.y;
                        if (obj.bounds.min.z < b.Min.Z) b.Min.Z = obj.bounds.min.z;
                        if (obj.bounds.max.x > b.Max.X) b.Max.X = obj.bounds.max.x;
                        if (obj.bounds.max.y > b.Max.Y) b.Max.Y = obj.bounds.max.y;
                        if (obj.bounds.max.z > b.Max.Z) b.Max.Z = obj.bounds.max.z;
                    }
                    //UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: GetSelectionExtents(): returning renderer {0}", BoxToString(b)));
                    continue;
                }

                Component[] meshRenderers = go.GetComponentsInChildren(typeof(MeshRenderer));
                if (meshRenderers.Length > 0)
                {
                    foreach (MeshRenderer obj in meshRenderers)
                    {
                        //UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: GetSelectionExtents(): name: {0} extents: {1}", selectedObject.name, obj.bounds.extents));
                        if (obj.bounds.min.x < b.Min.X) b.Min.X = obj.bounds.min.x;
                        if (obj.bounds.min.y < b.Min.Y) b.Min.Y = obj.bounds.min.y;
                        if (obj.bounds.min.z < b.Min.Z) b.Min.Z = obj.bounds.min.z;
                        if (obj.bounds.max.x > b.Max.X) b.Max.X = obj.bounds.max.x;
                        if (obj.bounds.max.y > b.Max.Y) b.Max.Y = obj.bounds.max.y;
                        if (obj.bounds.max.z > b.Max.Z) b.Max.Z = obj.bounds.max.z;
                    }
                    //UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: GetSelectionExtents(): returning MeshRenderer {0}", BoxToString(b)));
                    continue;
                }

                // ?? Mesh ?? MeshFilter ??
                Component[] meshes = go.GetComponentsInChildren(typeof(MeshFilter));
                if (meshes.Length > 0)
                {
                    foreach (MeshFilter meshfilter in meshes)
                    {
                        var obj = meshfilter.mesh;
#if TDX_DEBUG_EDITOR
                        UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: GetSelectionExtents(): name: {0} extents: {1}", selectedObject.name, obj.bounds.extents));
#endif
                        if (obj.bounds.min.x < b.Min.X) b.Min.X = obj.bounds.min.x;
                        if (obj.bounds.min.y < b.Min.Y) b.Min.Y = obj.bounds.min.y;
                        if (obj.bounds.min.z < b.Min.Z) b.Min.Z = obj.bounds.min.z;
                        if (obj.bounds.max.x > b.Max.X) b.Max.X = obj.bounds.max.x;
                        if (obj.bounds.max.y > b.Max.Y) b.Max.Y = obj.bounds.max.y;
                        if (obj.bounds.max.z > b.Max.Z) b.Max.Z = obj.bounds.max.z;
                    }
                    continue;
                }

                // If nothing found so far, just check against the object's position.
                var pos = go.transform.position;
                if (pos.x < b.Min.X) b.Min.X = pos.x;
                if (pos.y < b.Min.Y) b.Min.Y = pos.y;
                if (pos.z < b.Min.Z) b.Min.Z = pos.z;
                if (pos.x > b.Max.X) b.Max.X = pos.x;
                if (pos.y > b.Max.Y) b.Max.Y = pos.y;
                if (pos.z > b.Max.Z) b.Max.Z = pos.z;
            }

            return b;
        }

        public bool IsSelectionEmpty()
        {
            return Selection.gameObjects.Length == 0;
        }

        public void SetSelectionTransform(Matrix matrix)
        {
            // note: delta * selTransform = WCS.NavlibToHost(matrix) // unity uses column major
            var delta = WCS.NavlibToHost(matrix) * selTransform.inverse;
            selTransform = WCS.NavlibToHost(matrix);

            // Records any changes done on the object after the RecordObject function.
            if (recordObject)
            {
                Undo.RecordObjects(Selection.transforms, "Transform selection");
                recordObject = false;
            }

            foreach (GameObject go in Selection.gameObjects)
            {
                var trans = go.transform.localToWorldMatrix;
                trans = delta * trans;

                go.transform.rotation = trans.rotation;
                go.transform.position = trans.GetPosition();
            }
        }

        public Matrix GetSelectionTransform()
        {
            return WCS.HostToNavlib(Matrix4x4.identity);
        }

        public void ManualSetPivotPosition(Vector3 pos)
        {
            // THIS CAN BE THE DESIRED CENTER OF ROTATION but isn't the way it is supposed to work
            // Have to explicitly set this Property:
            Point p3D = new Point(pos.x, pos.y, pos.z);
            navigation3D.Properties.WriteAsync(PropertyNames.PivotPosition, p3D);
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: ManuallySetPivotPosition(): setting {0}", pos.ToString()));
#endif
        }
        public Point GetPivotPosition()
        {
            // THIS CAN BE THE DESIRED CENTER OF ROTATION but isn't the way it is supposed to work
            // And this doesn't get called.  Have to explicitly set this property:
            // this.navigation3D.Properties.WriteAsync(PropertyNames.PivotPosition, value);

            Point p = new Point(0, 0, 0);
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: GetPivotPosition(): returning {0}", PointToString(p)));
#endif
            return p;
        }

        public void SetPivotPosition(Point position)
        {
            // This is for display
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: SetPivotPosition({0})", PointToString(position)));
#endif
            PivotPointPosition.x = (float)position.X;
            PivotPointPosition.y = (float)position.Y;
            PivotPointPosition.z = (float)position.Z;

            Move3DxPivotPt();
        }

        public void SetPivotVisible(bool visible)
        {
            // Turns on/off the PivotPt based on navlib settings
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: SetPivotVisible({0})", visible));
#endif
            GameObject ppvo = FindAdd3DxPivotPt();
            if (ppvo != null)
            {
                GameObject cppvo = ppvo.transform.GetChild(0).gameObject;
                if (cppvo != null)
                {
                    cppvo.SetActive(visible);
                }
                if (visible == false)
                {
                    // Don't destroy it.  Destroy it in OnSceneSaving.
                    //UnityEngine.Object.DestroyImmediate(ppvo);
                    //ppvo = null;
                }
            }

        }

        public bool IsUserPivot()
        {
            //UnityEngine.Debug.Log("TDxController.cs: CameraController: IsUserPivot(): returning true");
            return false;
        }

        public double GetUnitsToMeters()
        {
            // The default world coordinate system unit is meters.
            // https://answers.unity.com/questions/45892/unity-unit-scale.html
            // There are references to being able to change it, but I've not found a way to do it.
            // There are physics engine implications to changing it.
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.Log("TDxController.cs: CameraController: UnitsToMeter(): returning 1.0");
#endif
            return 1.0;
        }

        public TDx.SpaceMouse.Navigation3D.Plane GetFloorPlane()
        {
            //UnityEngine.Debug.Log("TDxController.cs: CameraController: GetFloorPlane()");

            RaycastHit hit;
            SceneView sv = SceneView.lastActiveSceneView;
            Vector3 cameraPos = sv.camera.transform.position;
            //LayerMask layerMask = LayerMask.GetMask("Ignore Raycast");
            //layerMask.value = ~layerMask.value;
            if (Physics.Raycast(cameraPos, Vector3.down, out hit) == true)
            {
                CurrentScene(hit.collider.gameObject.scene);
#if TDX_DEBUG_EDITOR
                UnityEngine.Debug.LogFormat("TDxController.cs: CameraController: GetFloorPlane(): hit object {0} id: {1}", hit.collider.name, hit.collider.GetInstanceID());
#endif
                if (Find3DxFloorPt())
                {
                    FloorPointPosition = hit.point;
                    Move3DxFloorPt(sv);
                }

                // hit is the point.  Send the normal (ABC) and distance (D) to the world coordinate system origin along that normal vector.
                // This is the form of the plane equation: https://mathworld.wolfram.com/Plane.html
                // Generally D will be negative because the WCS will be below the floor plane.
                Vector3 hitPointTowardOrigin = Vector3.zero - hit.point; // WCS origin is 0, really no need to subtract it
                double len = hitPointTowardOrigin.magnitude;
                double distanceToPlane = Vector3.Dot(hit.point, hit.normal);

                // this doesn't work well at all when it hits tilted triangles while walking around
                //double[] planeABCD = { hit.normal.x, hit.normal.y, hit.normal.z, -distanceToPlane };

                double[] planeABCD = { Vector3.up.x, Vector3.up.y, Vector3.up.z, -hit.point.y};
#if TDX_DEBUG_EDITOR
                UnityEngine.Debug.LogFormat("TDxController.cs: CameraController: GetFloorPlane({0}, {1}, {2}, {3}) hit triangleIndex {4}", planeABCD[0], planeABCD[1], planeABCD[2], planeABCD[3], hit.triangleIndex);
#endif
                return new TDx.SpaceMouse.Navigation3D.Plane(planeABCD);
            }
            else
            {
                // It can hit nothing if the camera is not over anything other than the construction plane
                // Possibly the construction plane info needs to be sent.  Can it change orientation?
                double cplane_distance = 0; // is this available?
                double[] planeABCD = { Vector3.up.x, Vector3.up.y, Vector3.up.z, cplane_distance };
#if TDX_DEBUG_EDITOR
                UnityEngine.Debug.LogFormat("TDxController.cs: CameraController: (no hit) GetFloorPlane({0}, {1}, {2}, {3})", planeABCD[0], planeABCD[1], planeABCD[2], planeABCD[3]);
#endif
                return new TDx.SpaceMouse.Navigation3D.Plane(planeABCD);
            }
        }

        public double GetViewFocusDistance()
        {
            // not used
            throw new TDx.SpaceMouse.Navigation3D.NoDataException("GetViewFocusDistance: not implemented");
        }

#region Hit Testing
        //************** HIT TESTING ******************
        public Point GetLookAt()
        {
            // Return the position hit by the lookFrom, lookDirection ray
            //throw new TDx.SpaceMouse.Navigation3D.NoDataException("No hit testing");
            Point p = new Point(0, 0, 0);
            RaycastHit hit;
            if (Physics.Raycast(HitLookFromPoint, HitLookDirection, out hit) == true)
            {
                CurrentScene(hit.collider.gameObject.scene);
                Vector3 hitpoint = new Vector3();
                //hitpoint = hit.collider.transform.position;
                hitpoint = hit.point;
#if TDX_DEBUG_EDITOR
                UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: GetLookAt(): hit at {0} GO {1}", hitpoint.ToString(), hit.collider.name));
#endif
                p.X = hitpoint.x;
                p.Y = hitpoint.y;
                p.Z = hitpoint.z;
            }
            else
            {
#if TDX_DEBUG_EDITOR
                UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: GetLookAt(): nothing hit with HitLookFromPoint({0}), HitLookDirection({1}).  Reusing PivotPointPosition {2}", HitLookFromPoint.ToString(), HitLookDirection.ToString(), PivotPointPosition.ToString()));
#endif
                p.X = PivotPointPosition.x;
                p.Y = PivotPointPosition.y;
                p.Z = PivotPointPosition.z;
            }
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: GetLookAt(): returning {0}", PointToString(p)));
#endif
            return p;
        }

        public void SetLookFrom(Point eye)
        {
            // This would be the camera position, which I already know.
            HitLookFromPoint.x = (float)eye.X;
            HitLookFromPoint.y = (float)eye.Y;
            HitLookFromPoint.z = (float)eye.Z;
            //throw new TDx.SpaceMouse.Navigation3D.NoDataException("No hit testing");
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: SetLookFrom({0})", PointToString(eye)));
#endif
        }

        public void SetLookDirection(Vector direction)
        {
            // This would be the camera gaze direction, which I already know.
            HitLookDirection.x = (float)direction.X;
            HitLookDirection.y = (float)direction.Y;
            HitLookDirection.z = (float)direction.Z;
            //throw new TDx.SpaceMouse.Navigation3D.NoDataException("No hit testing");
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: SetLookDirection({0})", VectorToString(direction)));
#endif
        }

        public void SetLookAperture(double aperture)
        {
            HitAperture = aperture;
            //throw new TDx.SpaceMouse.Navigation3D.NoDataException("No hit testing");
            //UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: SetLookAperture({0})", aperture));
        }

        public void SetSelectionOnly(bool onlySelection)
        {
            HitSetSelectionOnly = onlySelection;
            throw new TDx.SpaceMouse.Navigation3D.NoDataException("No hit testing");
            //UnityEngine.Debug.Log(String.Format("TDxController.cs: CameraController: SetSelectionOnly({0})", onlySelection));
        }
#endregion

        void OnDrawGizmos()
        {
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.LogFormat("CameraController::OnDrawGizmos");
#endif
        }
    };



    [InitializeOnLoad]
    public class TDxController
    {

        private static CameraController cameraController;

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode)
            {
                cameraController.Delete3DxPivotPt();
                cameraController.Delete3DxFloorPt();
            }
        }

        static void OnQuitting()
        {
            cameraController.CloseConnection();
        }

        static void OnRegisteringPackages(UnityEditor.PackageManager.PackageRegistrationEventArgs args)
        {
            foreach (var removedPackage in args.removed)
            {
                if (removedPackage.name == "com.3dconnexion.3dxunity3d")
                {
                    cameraController.CloseConnection();
                    return;
                }
            }
        }

        /// Static initializer.  [InitializeOnLoad] causes this to be called.
        static TDxController()
        {
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.Log(string.Format("In TDxController.cs: TDxController()"));
#endif
            cameraController = new CameraController();

            // Eliminates the massive Editor.log where every log message is accompanied by a stacktrace
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);

            // Setup editor callbacks
            EditorApplication.update += Update;
            EditorApplication.quitting += OnQuitting;

            // Scene Save events
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += OnSceneSaving;
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += OnSceneSaved;

            // Event raised before applying changes to the registered packages list.
            UnityEditor.PackageManager.Events.registeringPackages += OnRegisteringPackages;

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        ~TDxController()
        {
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.Log(string.Format("In TDxController.cs: ~TDxController()"));
#endif
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            UnityEditor.PackageManager.Events.registeringPackages -= OnRegisteringPackages;
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= OnSceneSaving;
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaved -= OnSceneSaved;
            EditorApplication.update -= Update;
            EditorApplication.quitting -= OnQuitting;
            cameraController = null;
        }

        // Start is called before the first frame update (but only for a MonoBehaviour-which this is not)
        void Start()
        {
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.Log("In TDxController.cs: Start()");
#endif
        }


        // Update is called once per frame
        static void Update()
        {
            //UnityEngine.Debug.LogFormat("In TDxController.cs: Update() isPlaying: {0}, EditorWindow.focusedWindow: {1}, mouseOverWindow: {2}",
            //    Application.isPlaying.ToString(), EditorWindow.focusedWindow.ToString(), EditorWindow.mouseOverWindow.ToString());
            if (EditorWindow.focusedWindow == null)
                return;

            if (EditorWindow.focusedWindow.ToString().Contains("SceneView"))
            {
                if (cameraController != null)
                    cameraController.Update();
            }
            else
            {
                //UnityEngine.Debug.Log("Not SceneView");
            }

        }

        static void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path)
        {
            // Destroy the PivotPt GO when saving the scene, so it doesn't get saved into user's scenes
            //UnityEngine.Debug.LogFormat("Saving scene '{0}' to {1}", scene.name, path);
            cameraController.Delete3DxPivotPt();
            cameraController.Delete3DxFloorPt();
        }

        static void OnSceneSaved(UnityEngine.SceneManagement.Scene scene)
        {
            //UnityEngine.Debug.LogFormat("Saved scene '{0}'", scene.name);
        }

        void OnDrawGizmos()
        {
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.LogFormat("TDxController::OnDrawGizmos");
#endif
        }

        public static void OnApplicationQuit()
        {
#if TDX_DEBUG_EDITOR
            UnityEngine.Debug.Log(string.Format("In TDxController.cs: TDxController::OnApplicationQuit()"));
#endif
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= OnSceneSaving;
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaved -= OnSceneSaved;
            EditorApplication.update -= Update;
            cameraController = null;

        }
    }

}
#endif