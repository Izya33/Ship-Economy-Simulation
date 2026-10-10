using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SellSlotUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI countText;
    public TextMeshProUGUI priceText;
    public Button sellButton;
    private Slot currentSlot;
    private System.Action<Slot> onSell;

   public void Setup(Slot slot, System.Action<Slot> onSellClicked, Sprite sprite, float sellPrice)
    {
        currentSlot = slot;
        onSell = onSellClicked;

        bool hasItem = slot != null && !slot.IsEmpty;
        bool canSell = hasItem && sellPrice >= 0f;

        icon.enabled = hasItem;
        icon.sprite = sprite;

        countText.gameObject.SetActive(hasItem);
        priceText.gameObject.SetActive(hasItem);
        sellButton.gameObject.SetActive(hasItem);

        sellButton.onClick.RemoveAllListeners();

        if (!hasItem) return;

        countText.text = slot.Count > 1 ? slot.Count.ToString() : "";

        if (canSell)
        {
            priceText.text = $"${sellPrice:F2}";
            sellButton.interactable = true;

            sellButton.onClick.AddListener( () => onSell?.Invoke(currentSlot) );
        }
        else
        {
            priceText.text = "Cannot sell";
            sellButton.interactable = false;
        }
    }

    public void Clear()
    {
        currentSlot = null;
        onSell = null;

        icon.enabled = false;
        icon.sprite = null;

        countText.text = "";
        priceText.text = "";

        countText.gameObject.SetActive(false);
        priceText.gameObject.SetActive(false);
        sellButton.gameObject.SetActive(false);

        sellButton.onClick.RemoveAllListeners();
    }
}