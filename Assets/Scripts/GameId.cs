using UnityEngine;

public class GameId : MonoBehaviour
{
    public enum Interact
    {
        None, PlayAnimation, LiftOnject, InventoryItem
    }
    public Interact MyInteract;

    public enum HipsterInventory
    {
        None, coffeeBeansFromColobia, coffeeBeansFromBrazil, MokaPot, coffeeBeansFromIndonesia
    }
    public HipsterInventory MyHipsterInventory;
}
