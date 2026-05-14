using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class InvSlot : MonoBehaviour
{
 public Image itemPic;
 public TextMeshProUGUI CountText;
 private void Awake()
    {
        CountText.text = string.Empty;
        itemPic.color = Color.clear;
    }

}
