using UnityEngine;
using UnityEngine.InputSystem;

public class AddForce : MonoBehaviour
{
    [Header("Connect a rigidbody and press space")]
    public Rigidbody Rigid;
    public float Force = 20;

    private void Start()
    {
        Rigid = GetComponent<Rigidbody>();
    }
    private void FixedUpdate()
    {
        var key = Keyboard.current;
        if (key != null)
        {
            if (key.spaceKey.IsPressed())// .wasPressedThisFrame)//  
            {
                Rigid.AddForce(Force * Vector3.up);
            }
        }
    }

}