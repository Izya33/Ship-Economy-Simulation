using UnityEngine;
using UnityEngine.InputSystem;

public class IslandManager : MonoBehaviour
{
    [SerializeField] private Trader[] traders;

    public Trader[] Traders => traders;

    // Optional Debug
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) DebugEconomy();
        if (Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame) DebugPrice();
    }
    public void DebugEconomy()
    {
        Debug.Log("===== Economy =====");
        foreach (Trader trader in traders)
        {
            Debug.Log($"--- {trader.name} ---");
            foreach (TradeItem item in trader.data.inventory)
            {
                Debug.Log($"{item.Type} | Stock: {item.stock} | Price: {item.currentPrice:F2}");
            }
        }
        Debug.Log("===================");
    }

    public void DebugPrice()
    {
        foreach (Trader trader in traders)
            Debug.Log($"{trader.name} have {trader.data.money} ");
    }
}
