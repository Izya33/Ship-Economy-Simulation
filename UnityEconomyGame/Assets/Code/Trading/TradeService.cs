using System;
using UnityEngine;

public static class TradeService
{
    public static bool TryBuyFromTrader(ITradeAgent buyer, TraderData trader, TradeItem item, int quantity, out string error)
    {
        error = string.Empty;

        if (buyer == null || trader == null || item == null)
        {
            error = "Invalid trade data";
            return false;
        }

        if (quantity <= 0)
        {
            error = "Quantity must be greater than zero";
            return false;
        }

        // 0 means unavailable, -1 means unlimited.
        if (trader.saleLimit == 0)
        {
            error = "Trader has reached the sale limit";
            return false;
        }

        if (item.stock != -1 && item.stock < quantity)
        {
            error = "Trader does not have enough stock";
            return false;
        }

        if (item.currentPrice < 0)
        {
            error = "Invalid item price";
            return false;
        }

        float totalPrice = item.currentPrice * quantity;

        if (buyer.Money < totalPrice)
        {
            error = "Not enough money";
            return false;
        }

        if (!buyer.AddItem(item.Type, quantity))
        {
            error = "Inventory is full";
            return false;
        }

        buyer.Money -= totalPrice;
        trader.money += totalPrice;

        // -1 means unlimited stock, so we only decrease stock if it's not unlimited.
        if (item.stock > 0)
            item.stock -= quantity;

        if (trader.saleLimit > 0)
            trader.saleLimit--;

        if (buyer is ITradeProgression progression)
            progression.OnItemBought(item.Type, quantity, item.currentPrice);
        
        return true;
    }
    public static bool TrySellToTrader(ITradeAgent seller, TraderData trader, ItemType itemType, int quantity, ItemDatabase database, out float receivedMoney, out string error)
    {
        receivedMoney = 0f;
        error = string.Empty;

        if (seller == null || trader == null || database == null)
        {
            error = "Invalid trade data";
            return false;
        }

        if (itemType == ItemType.Empty)
        {
            error = "Cannot sell an empty slot";
            return false;
        }

        if (quantity <= 0)
        {
            error = "Quantity must be greater than zero";
            return false;
        }

        // 0 means unavailable, -1 means unlimited.
        if (trader.buyLimit == 0)
        {
            error = "Trader has reached the buy limit";
            return false;
        }

        TradeItem traderItem = trader.GetItem(itemType);

        if (traderItem == null)
        {
            error = "Trader does not support this item";
            return false;
        }

        float unitPrice = GetSellPrice(trader, itemType);

        if (unitPrice < 0f)
        {
            error = "Invalid sell price";
            return false;
        }

        float totalPrice = unitPrice * quantity;

        if (trader.money < totalPrice)
        {
            error = "Trader does not have enough money";
            return false;
        }

        if (!seller.RemoveItem(itemType, quantity))
        {
            error = "Seller does not have enough items";
            return false;
        }

        seller.Money += totalPrice;
        trader.money -= totalPrice;

        AddItemToTraderStock(trader, itemType, database, quantity);

        // Currently, the limit is reduced by one trading operation.
        if (trader.buyLimit > 0)
            trader.buyLimit--;

        receivedMoney = totalPrice;

        if (seller is ITradeProgression progression)
            progression.OnItemSold( itemType, quantity, unitPrice);

        return true;
    }

    private static void AddItemToTraderStock( TraderData trader, ItemType itemType, ItemDatabase database, int amount)
    {
        TradeItem item = trader.GetItem(itemType);

        if (item != null)
        {
            item.stock += amount;
            return;
        }
        if (amount <= 0)
            return;

        TradeItem existingItem = Array.Find(trader.inventory, item => item != null && item.Type == itemType);

        if (existingItem != null)
        {
            // Infinite stock remains infinite.
            if (existingItem.stock >= 0)
                existingItem.stock += amount;

            return;
        }

        int previousLength = trader.inventory?.Length ?? 0;
        Array.Resize(ref trader.inventory, previousLength + 1);

        trader.inventory[previousLength] = new TradeItem{Type = itemType,priceMultiplier = 1f,stock = amount};
    }

    public static float GetSellPrice( TraderData trader, ItemType itemType)
    {
        if (trader == null || itemType == ItemType.Empty)
            return -1f;

        TradeItem traderItem = trader.GetItem(itemType);

        if (traderItem == null)
            return -1f;

        if (traderItem.currentPrice < 0f)
            return -1f;
            
        return traderItem.currentPrice;
    }


}
