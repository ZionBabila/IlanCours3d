# 3DxUnity3D
3DxUnity3D plugin for using 3Dconnexion devices in Unity3D

This plugin enables the 3Dconnexion 3D mouse in the Unity editor environment.

**SceneView** (editing/creating scenes) and **GameView** (playing the game in the editor) are supported.  

In the **SceneView**, the 3D mouse can be used to view the scene, or move GameObjects.
All current 3Dconnexion Navigation modes are supported.  The 3DxSmartUI program can be used to chose between them.  The default mode is "Walk".

**GameView** is limited to moving the main camera in the Active Scene.  This may or may not coincide with the player position.  When GameView is active (and playing) the SceneView window is disabled.  GameView is a work in progress.

The device is not at the moment supported properly by the New Input System. 

All Unity menu items are exported to the driver for assignment to device buttons in 3DxSmartUI.

##Navigation Modes
###Object Mode
The 3D mouse acts as if you are holding the current object in your hand.  This is useful for building individual models to be added to a scene, but not useful for navigating through a scene after it is built.

- You can choose the Rotation Center in the 3DxSmartUI Advanced Settings panel.
- You have options to show a small glyph indicating the position of the Rotation Center.
- You can have the Rotation Center auto-selected or chose to rotate about the currently selected object in the scene.

###Camera Mode
The 3D mouse acts as if you are holding a free-floating camera in your hand.  The Rotation Center is your position which is selected in the scene, as in Object Mode.

###Target Camera Mode
The 3D mouse rotates the camera about the Rotation Center.    

###Helicopter Mode
The 3D mouse flies the camera through the scene but altitude is controlled separately from the view direction.  You can look down and still fly at a constant altitude.   

###Walk Mode
The 3D mouse maintains a constant height above the terrain.  This makes it particularly easy to go up stairs in the  model.

###Lock Horizon
This prevents the camera from tilting to the side.   


## <a name="installation"></a>Installation
A standard 3DxWare installation is required to use this plugin.  There is an option for including Unity support.  This must be checked.
 
- After installing 3DxWare and including the Unity3D plugin, install the plugin from the installation directory.
  - Open the Package Manager in Unity (Window->Package Manager)
  - Use the "+" at the upper left corner of the Package Manager to Add packages from disk...  
  - Browse to your 3DxUnity3D plugin directory.  Typically C:\Program Files\3Dconnexion\3DxWare\3DxUnity3D\src.
  - Select the package.json file->Open.
  - Close the Package Manager.

- The plugin should now be added to your project and it should work in Edit and Play modes.  


## Known bugs and limitations
- There is an occasional crash when exiting Unity or any other time when exiting the plugin. 
- The New Input System package does not read the device correctly at this time.  It is independent of this plugin.
- There are times when the old Input Manager will try to map the 3Dx emulated joystick to default Actions.  This doesn't work.  It can either by disabled by 
  - Unmapping the erroneous joysticks in the Input Manager
  - Disabling the emulated joystick in the Device Manager
  - See [Disable Joystick Tip](#TipDisableJoystick) below

## <a name="TipDisableJoystick"></a>Tip : Disable 3Dx Emulated Joystick
If you have unexplained constant movement in a game, the most likely cause is the old Input Manager incorrectly categorizing our emulated Joystick.  This is often reported as a constant scrolling in control panels, or constantly staring up at the sky.

You can stop this by disabling this mapping if you have access to your InputManager.asset file, or by disabling the entire 3Dx KMJ joystick. 

- To disable the mapping, use the GUI in Unity.  Or, while Unity is not running, manually edit the InputManager.asset file.  Changing the joyNum:0 entries to joyNum: 10 (for example) seems to work.  

- To disable the KMJ joystick:
Open the Device Manager:

->Human Interface Devices
->HID-compliant game controller which has: Properties->Details->Device instance path=HID/3DXKMJ_HIDMINI&COL03... (check the Device instance path if you have more than 1)
->Properties->Driver->Disable Device

This disables the joystick system-wide, in case you were trying to use it in any other games.

##Feedback
Please provide feedback to us on our [forum](https://forum.3dconnexion.com/index.php).

## Credits
- Thanks to PatHightree for convincing us that this could be done

