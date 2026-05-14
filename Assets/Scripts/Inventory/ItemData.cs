using UnityEngine;

[CreateAssetMenu(fileName = "New ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    public GameId.HipsterInventory Item;
    public int Weight = 1;
    public int Price = 2;
    public Sprite Pic;
}