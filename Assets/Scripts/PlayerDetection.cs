
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class PlayerDetection : MonoBehaviour
{

    [Header("raycast settings")]
    public float radius = 1f;
    public float Distance = 3;

    [Header("Attach camera ancor")]
    public Transform Origin;

    RaycastHit hit;
    [Header("the object player found")]
    public GameObject IHit;
    public GameObject IlIft;


    private void Update()
    {
        if (Physics.SphereCast(Origin.position , radius, Origin.forward, out hit, Distance))
        {
            IHit = hit.collider.gameObject;
            GameId gameId = IHit.GetComponent<GameId>();
            Interact(gameId);
        }
        else
        {
            IHit = null;
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(Origin.position + Origin.forward * Distance, radius);
        Gizmos.DrawLine(Origin.position, Origin.position + Origin.forward * Distance);
    }
    private void Interact(GameId gameId)
    {
        if(gameId != null)
        {
            var key = Keyboard.current;
            var mouse = Mouse.current;
            if (key != null)
            {
                if (key.eKey.wasPressedThisFrame)
                {
                    if (gameId.MyInteract == GameId.Interact.LiftOnject)
                    {
                        if(IlIft == null)
                        {
                            IHit.transform.SetParent(Origin.transform);
                            IlIft = IHit;
                        }
                        else
                        {
                            IlIft.transform.SetParent(null);
                            IlIft = null;
                        }
                    }
                    if (gameId.MyInteract == GameId.Interact.PlayAnimation)
                    {
                        interactPlayAnimation playAnim = gameId.gameObject.GetComponent<interactPlayAnimation>();
                        if(playAnim != null)
                        {
                            playAnim.PlayAnimator();
                            return;
                        }
                    }
                }
            }
        }
    }
}
