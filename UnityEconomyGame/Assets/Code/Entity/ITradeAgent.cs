public interface  ITradeAgent
{
    float Money { get; set; }

    bool AddItem(ItemType itemType, int amount);
    bool RemoveItem(ItemType itemType, int amount);
}
