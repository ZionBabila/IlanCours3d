using UnityEngine;

public class PlayerAnim : MonoBehaviour
{

    public CharacterController controller;
    public Animator anim;
    public float Speed;


    // private void Start()
    // {
    //     if(Game.Instance)
    //     {
    //        // PlayerWeapon playerWeapon = Game.Instance.playerMove.gameObject.GetComponentInChildren<PlayerWeapon>();
    //         if(playerWeapon != null)
    //         {
    //             playerWeapon.playerAnim = this;
    //         }
    //     }
    // }
    private void Update()
    {
        if(anim && controller)
        {
            Speed = controller.velocity.magnitude;
            if(Game.Instance != null && Game.Instance.playerMove.enabled == false || Game.Instance != null )//talking or teleportingll
            {
                anim.SetFloat("speed", 0);
                anim.SetBool("jump", false);
            }
            else
            {
                if(controller.isGrounded)
                {
                    anim.SetFloat("speed", controller.velocity.magnitude);
                    anim.SetBool("jump", false);
                }
                else
                {
                    anim.SetFloat("speed", 0);
                    anim.SetBool("jump", true);
                }
            }
        }
    }
    public void SwordAttack()
    {
        anim.SetBool("attack", true);
    }
}
