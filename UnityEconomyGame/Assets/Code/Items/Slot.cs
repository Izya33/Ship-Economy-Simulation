using System;
using UnityEngine;

[Serializable]
public class Slot
{
    [field: SerializeField] public ItemType Type { get; set; }
    [field: SerializeField] public int Count { get; set; }

    public Slot(ItemType type = ItemType.Empty, int count = 0)
    {
        Type = type;
        Count = count;
    }

    public bool IsEmpty => Type == ItemType.Empty || Count <= 0;
}
