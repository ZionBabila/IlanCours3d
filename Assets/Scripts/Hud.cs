
using UnityEngine;
using UnityEngine.UI;



public class Hud : MonoBehaviour
{
    public Image Pointer;
    public Sprite Point;
    public Sprite Interact;

    //public Slider PlayerHp;
    //public Slider EnemyHp;
    //float timer = 0;


    private void Update()
    {
        // if(timer < 0)
        // {
        //     EnemyHp.gameObject.SetActive(false);
        // }
        // else
        // {
        //     timer -= Time.deltaTime;
        // }
        // if (Game.Instance != null && Game.Instance.PlayerHeath != null)
        // {
        //     SetPlayerHp(Game.Instance.PlayerHeath.CurrentHp);
        // }
    }
    // public void SetPlayerHp(int hp)
    // {
    //     PlayerHp.gameObject.SetActive(true);
    //     PlayerHp.value = hp;
    // }

    // public void SetEnemyHp(int hp)
    // {
    //     EnemyHp.gameObject.SetActive(true);
    //     EnemyHp.value = hp;
    //     timer = 1;
    // }
}
