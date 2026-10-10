using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TradeSlotUI : MonoBehaviour
{
    public Image icon;
    public TextMeshProUGUI priceText;
    public Button actionButton;
    private TradeItem currentItem;
    private System.Action<TradeItem> onAction;

    public void Setup(TradeItem item, System.Action<TradeItem> onActionClicked, Sprite itemSprite)
    {
        currentItem = item;
        onAction = onActionClicked;

        bool hasItem = item != null && item.stock != 0;

        icon.enabled = hasItem;
        icon.sprite = itemSprite;
        priceText.enabled = hasItem;
        actionButton.gameObject.SetActive(hasItem);

        actionButton.onClick.RemoveAllListeners();

        if (!hasItem) return;

        priceText.text = item.stock < 0 ? $"${item.currentPrice}  Nan" : $"${item.currentPrice}  x{item.stock}";

        actionButton.onClick.AddListener( () => onAction?.Invoke(currentItem));
    }

    public void Clear()
    {
        currentItem = null;
        onAction = null;

        actionButton.onClick.RemoveAllListeners();
        icon.enabled = false;
        priceText.enabled = false;
        actionButton.gameObject.SetActive(false);
    }
}
