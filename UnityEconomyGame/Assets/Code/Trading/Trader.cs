using UnityEngine;

public class Trader : MonoBehaviour
{
    [Header("Information")]
    [SerializeField] public TraderData data;

    [Header("References")]
    [SerializeField] DayManager dayManager;
    [SerializeField] ItemDatabase itemDatabase;
    [SerializeField] private Transform dockingPoint;

    public Vector2 GetDockingPosition()
    {
        if (dockingPoint != null) return dockingPoint.position;
        return transform.position;
    }

    private void Awake()
    {
        if (data == null)
        {
            Debug.LogError($"TraderData is not assigned on {name}.", this);
            return;
        }

        // Every trader receives its own runtime state. Two scene objects can now
        // use the same template asset without sharing money, stock and limits.
        data = Instantiate(data);
        data.name = $"{data.name} (Runtime - {name})";
        UpdatePrices();
    }


    private void OnEnable() => dayManager.OnNewDay += OnNewDay;
    private void OnDisable() => dayManager.OnNewDay -= OnNewDay;

    private void OnNewDay()
    {
        ProduceGoods();
        ConsumeGoods();
        UpdatePrices();
    }
    private void ProduceGoods()
    {
        // For optimisation Dictionary<ItemType, TradeItem> can be used
        for(int i = 0; i < data.productionItems.Length; i++)
        {
            for(int j = 0; j < data.inventory.Length; j++)
            {
                if (data.inventory[j].Type == data.productionItems[i].Type)
                {
                    data.inventory[j].stock += data.productionItems[i].amountPerDay;
                    break;
                }   
            }
        }
    }

    private void ConsumeGoods()
    {
        for(int i = 0; i < data.consumptionItems.Length; i++)
        {
            for(int j = 0; j < data.inventory.Length; j++)
            {
                if(data.inventory[j].Type == data.consumptionItems[i].Type)
                {
                    if(data.inventory[j].stock - data.consumptionItems[i].amountPerDay < 0)
                        data.inventory[j].stock = 0;
                    else
                        data.inventory[j].stock -= data.consumptionItems[i].amountPerDay;
                    Debug.Log("Consume was called "+ data.inventory[j].stock + " for item "+data.inventory[j].Type);
                    break;
                }
            }
        }
    }

    private void UpdatePrices()
    {
        for (int i = 0; i < data.inventory.Length; i++)
        {
            TradeItem item = data.inventory[i];

            float basePrice = itemDatabase.GetBasePrice(item.Type);
            float stockMultiplier;

            if (item.stock == -1)
                stockMultiplier = 1f;
            else if (item.stock == 0)
                stockMultiplier = 2f;
            else if (item.stock < 10)
                stockMultiplier = 1.5f;
            else if (item.stock > 40)
                stockMultiplier = 0.6f;
            else
                stockMultiplier = 1f;

            item.currentPrice = basePrice * item.priceMultiplier * stockMultiplier;
        }
    }
}
