public interface ITradeProgression
{
    void OnItemBought(ItemType type, int quantity, float unitPrice);
    void OnItemSold(ItemType type, int quantity, float unitPrice);
}
