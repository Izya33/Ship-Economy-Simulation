using UnityEngine;
using UnityEngine.InputSystem;

public class CollisionManagerTrader : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;
    private Trader trader;
    private bool canInteract;


    private void Awake()
    {
        trader = GetComponent<Trader>();
    }

    private void Update()
    {
        if (uiManager == null || !canInteract) return;

        if (!uiManager.IsPortOpened())
            uiManager.ShowInteractionText();

        if (Keyboard.current != null &&
            Keyboard.current.fKey.wasPressedThisFrame)
        {
            if (uiManager.IsPortOpened())
                ClosePort();
            else
                OpenPort();
        }
}

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        canInteract = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        canInteract = false;
        uiManager.ClosePort();
        uiManager.HideInteractionText();
    }

    private void OpenPort()
    {
        if (!canInteract) return;
        if (trader == null || trader.data == null) return;

        uiManager.OpenPort(trader.data);
    }

    public void ClosePort()
    {
        uiManager.ClosePort();

        if (canInteract)
            uiManager.ShowInteractionText();
        else
            uiManager.HideInteractionText();
    }
}