using UnityEngine;

public class GameId : MonoBehaviour
{
    public enum Interact
    {
        None, PlayAnimation, LiftOnject,
    }
    public Interact MyInteract;

    public enum HipsterInventorty
    {
        None, coffeeBeansFromColobia, coffeeBeansFromBrazil, MokaPot, coffeeBeansFromIndonesia
    }
    public HipsterInventorty MyHipsterInventorty;
}
