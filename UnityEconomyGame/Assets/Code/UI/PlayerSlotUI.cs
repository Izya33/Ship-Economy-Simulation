using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSlotUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI countText;

    public void Setup(Slot slot, Sprite itemSprite)
    {
        bool hasItem = slot != null && !slot.IsEmpty;

        icon.enabled = hasItem;
        icon.sprite = itemSprite; // Set the icon sprite based on the item type
        countText.gameObject.SetActive(hasItem);

        if (hasItem)
        {
            countText.text = slot.Count > 1 ? slot.Count.ToString() : "";
        }
    }

    public void Clear()
    {
        icon.enabled = false;
        countText.gameObject.SetActive(false);
    }
}