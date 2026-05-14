using UnityEngine;
using UnityEngine.InputSystem;
public class Inventory : MonoBehaviour
{
    public InvItem[] invItems;
    public InvSlot[] invSlots;
    public AudioSource TakeSound;
    public GameObject inventoryMenu;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        invItems = GetComponentsInChildren<InvItem>();
        invSlots = inventoryMenu.GetComponentsInChildren<InvSlot>();
        inventoryMenu.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        var key = Keyboard.current;
        if (key != null)
        {
            if (key.tabKey.wasPressedThisFrame)
            {
                if (inventoryMenu.activeSelf == true)
                {
                    inventoryMenu.SetActive(false);
                }
                else
                {
                    inventoryMenu.SetActive(true);
                }
                SortInventory();
            }
        }
    }
        public void SortInventory()
    {
        foreach (var invSlot in invSlots)
        {
            invSlot.CountText.text = string.Empty;
            invSlot.itemPic.color = Color.clear;
        }
        if (invItems != null && invItems.Length > 0)
        {
            int num = 0;
            for (int i = 0; i < invItems.Length; i++)
            {
                if (invItems[i].Data && invItems[i].count > 0 && num < invSlots.Length)
                {
                    invSlots[num].itemPic.color = Color.white;
                    invSlots[num].itemPic.sprite = invItems[i].pic;
                    invSlots[num].CountText.text = invItems[i].count.ToString();
                    num++;

                }

            }
        }

    }
    public void CloseInventory()
    {
        inventoryMenu.SetActive(false);
    }
    public void AddItem(GameId.HipsterInventory item, int amount)
    {
        if (invItems != null && invItems.Length > 0)
        {
            for (int i = 0; i < invItems.Length; i++)
            {
                if (invItems[i].Data && invItems[i].Data.Item == item)
                {
                    invItems[i].count += amount;
                    TakeSound.Play();
                    return;
                }
            }
        }
    }
    public int ItemCount(GameId.HipsterInventory item)
    {
        if (invItems != null && invItems.Length > 0)
        {
            for (int i = 0; i < invItems.Length; i++)
            {
                if (invItems[i].Data && invItems[i].Data.Item == item)
                {
                    return invItems[i].count;
                }
            }
        }
        return 0;
    }
}
