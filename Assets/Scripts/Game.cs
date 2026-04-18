using UnityEngine;

public class Game : MonoBehaviour
{
    public static Game Instance;
    
    public SceneController sceneController;
    public PlayerMovement playerMove;
   // public Health PlayerHeath;
   // public DeathCollider deathCollider;

   // public Inventory inventory;
   // public OpenPage openPage;
   // public DialogueMenu dialogueMenu;
   // public Hud hud;



    private void Awake()
    {
        if(Instance != null)
        {
            Debug.LogError("singelton violation");
        }
        Instance = this;
    }
    
}
