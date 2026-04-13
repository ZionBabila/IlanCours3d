using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;

    public float WalkSpeed = 2;
    public float SprintSpeed = 8;
    public float Gravity = 20;
    public float JumpPower = 20;
    public float JumpForSec = 1.5f;
    private float Speed = 0;
    private Vector3 jumpVectorUp;
    private float jumpTimer;

    [Header("_____________________")]
    public Transform Camera;
    public Vector3 CamPosition = new Vector3(0, 0, -3.5f);
    public Transform CamAncor;
    private float RotateV;
    private float RotateH;
    public float CamRotationSpeed = 20;

    [Header("_____________________")]
    public InputActionAsset actions;
    private InputAction moveAction;
    private InputAction sprintAction;
    private InputAction LookAction;

    [Header("_____________________")]
    public Transform Character;
    public float CharacterRotateSpeed = 15;
    private Vector3 direction;

    Vector3 move;
    Vector3 forward;
    Vector3 side;


    void Awake()
    {
        controller = GetComponent<CharacterController>();

        // find the "move" action, and keep the reference to it, for use in Update
        moveAction = actions.FindActionMap("Player").FindAction("Move");
        sprintAction = actions.FindActionMap("Player").FindAction("Sprint");
        LookAction = actions.FindActionMap("Player").FindAction("Look");

        // for the "jump" action, we add a callback method for when it is performed
        actions.FindActionMap("Player").FindAction("Jump").performed += OnJump;
    }
    void OnEnable()
    {
        actions.FindActionMap("Player").Enable();
    }
    void OnDisable()
    {
        actions.FindActionMap("Player").Disable();
    }
    private void OnJump(InputAction.CallbackContext context)
    {
        if (controller != null && controller.isGrounded)
        {
            jumpTimer = JumpForSec;
        }
    }
    private void Update()
    {
        if (controller != null && controller.enabled == true)
        {
            //sprint
            if (sprintAction.IsPressed())
            {
                Speed = SprintSpeed;
            }
            else
            {
                Speed = WalkSpeed;
            }

            //jump
            if (jumpTimer > 0)
            {
                jumpVectorUp = JumpPower * jumpTimer * Time.deltaTime * Vector3.up;
                jumpTimer -= Time.deltaTime;
            }
            else
            {
                jumpVectorUp = Vector3.zero;
            }

            //move on ground
            if (controller.isGrounded)
            {
                forward = Speed * Time.deltaTime * transform.forward * moveAction.ReadValue<Vector2>().y;
                side = Speed * Time.deltaTime * transform.right * moveAction.ReadValue<Vector2>().x;
                move = forward + side;
            }

            //final movement
            Vector3 gravity = new Vector3(0, -Gravity, 0) * Time.deltaTime;
            Vector3 movement = (move.normalized * Speed * Time.deltaTime) + gravity + jumpVectorUp;
            controller.Move(movement);
        }

        //position camera
        if (Camera)
        {
            Camera.transform.localPosition = CamPosition;
        }

        //move camera input
        RotateH = CamRotationSpeed * LookAction.ReadValue<Vector2>().x * Time.deltaTime;
        RotateV = CamRotationSpeed * -LookAction.ReadValue<Vector2>().y * Time.deltaTime;

        //rotate the character controller and the camera together on Y
        transform.Rotate(0, RotateH, 0);

        //rotate camera on X
        if (CamAncor != null)
        {
            //rotate the camera ancor on x is clamp to min max value
            float Xrotation = CamAncor.transform.localEulerAngles.x + RotateV;
            if (Xrotation > 80 && Xrotation < 180)
            {
                Xrotation = 80;
            }
            else
            {
                if (Xrotation > 180 && Xrotation < 300)
                {
                    Xrotation = 300;
                }
            }
            CamAncor.transform.localEulerAngles = new Vector3(Xrotation, 0, 0);
        }

        //attach the character to the player(camera rig)
        if (Character != null)
        {
            //attach position
            Character.transform.position = transform.position;

            //character rotate toward camera rig direction only on movement
            if (controller != null)
            {
                //when zero velocity, direction = 0 , and nothing will happen
                direction = new Vector3(controller.velocity.x, 0, controller.velocity.z);
                if (direction != Vector3.zero)
                {
                    Character.rotation = Quaternion.Lerp(Character.rotation, Quaternion.LookRotation(direction), Time.deltaTime * CharacterRotateSpeed);
                }
            }
        }
    }
    public void TeleportPlayer(Transform point)
    {
        controller.enabled = false;
        transform.position = point.position;
        controller.enabled = true;

    }
}