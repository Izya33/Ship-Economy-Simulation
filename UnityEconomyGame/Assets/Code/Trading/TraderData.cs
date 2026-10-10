using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTrader", menuName = "Trading/Trader Data")]
public class TraderData : ScriptableObject
{
    public string traderId;
    public TradeItem[] inventory;

    public ProductionItem[] productionItems;
    public ConsumptionItem[] consumptionItems;
    public int saleLimit; // -1 = no limit
    public int buyLimit; // -1 = no limit
    public float money; // Trader's available money for buying items

    //Reputation

    public TradeItem GetItem(ItemType type)
    {
        for (int i = 0; i < inventory.Length; i++)
            if (inventory[i].Type == type) return inventory[i];
        return null;
    }
}

[System.Serializable]
public class TradeItem
{
    public ItemType Type;
    
    [Min(0.1f)]
    public float priceMultiplier = 1f;

    [HideInInspector]
    public float currentPrice;

    public int stock; // -1 = infinite stock
}

[System.Serializable]
public class ProductionItem
{
    public ItemType Type;
    public int amountPerDay;
}

[System.Serializable]
public class ConsumptionItem
{
    public ItemType Type;
    public int amountPerDay;
}