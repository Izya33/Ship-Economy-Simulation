using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Trading/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [System.Serializable]
    public class ItemEntry
    {
        public ItemType type;
        public Sprite sprite;
        public float basePrice;
    }

    public ItemEntry[] entries;

    private Dictionary<ItemType, ItemEntry> lookup;

    // Dictionaries are clearly more efficient than arrays
    private void BuildLookup()
    {
        if (lookup != null) return;
        lookup = new Dictionary<ItemType, ItemEntry>();
        foreach (var entry in entries)
            lookup[entry.type] = entry;
    }

    public Sprite GetSprite(ItemType type)
    {
        BuildLookup();
        return lookup.TryGetValue(type, out var entry) ? entry.sprite : null;
    }

    public float GetBasePrice(ItemType type)
    {
        BuildLookup();
        return lookup.TryGetValue(type, out var entry) ? entry.basePrice : 0;
    }
}