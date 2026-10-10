using TMPro;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using Unity.VisualScripting;

public class UIManager : MonoBehaviour
{
    
    [Header("Port Panels")]
    [SerializeField] private GameObject mainPortPanel;
    [SerializeField] private GameObject tradePanel;
    [SerializeField] private TextMeshProUGUI traderNameText;
    [SerializeField] private GameObject questsPanel;
    [SerializeField] private GameObject upgradesPanel;
    [SerializeField] private GameObject islandInfoPanel;
    [SerializeField] private GameObject playerInventoryPanel;
    [SerializeField] private GameObject playerQuestPanel;
    [SerializeField] private TextMeshProUGUI suppliesPriceText;
    [SerializeField] private float suppliesBasePrice = 5f;

    [Header("Inventory UI")]
    public TextMeshProUGUI[] infoText;
    public PlayerSlotUI[] slotButtons;

    [Header("Player Quests UI")]
    public TextMeshProUGUI[] questText;

    [Header("Trade UI")]
    public TradeSlotUI[] traderBuyButtons;
    public SellSlotUI[] traderSellButtons;

    [Header("Quest UI")]
    [SerializeField] private QuestSlotUI[] questSlots;

    [Header("Upgrade UI")]
    [SerializeField] private TextMeshProUGUI speedUpgradeLevelText;
    [SerializeField] private TextMeshProUGUI speedUpgradePriceText;
    [SerializeField] private TextMeshProUGUI cargoUpgradeLevelText;
    [SerializeField] private TextMeshProUGUI cargoUpgradePriceText;

    [Header("Upgrade Settings")]
    [SerializeField] private float speedUpgradeBasePrice = 200f;
    [SerializeField] private float cargoUpgradeBasePrice = 300f;
    [SerializeField] private float speedIncreasePerUpgrade = 0.1f;
    [SerializeField] private int cargoSlotsPerUpgrade = 1;

    [Header("Info UI")]
    [SerializeField] private TextMeshProUGUI traderMoney;
    [SerializeField] private TextMeshProUGUI itemsStock;

    [Header("Guide")]
    [SerializeField] private GameObject pressFText;

    [Header("GAME END UI")]
    [SerializeField] private GameObject deathPanel;



    [Header("References")]
    [SerializeField] private Player playerShip;
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private QuestManager questManager;
    private TraderData currentTrader;

    private void Awake()
    {
        ClosePort();
        HideInteractionText();
        if (deathPanel != null)
            deathPanel.SetActive(false);
        if(playerQuestPanel != null)
            playerQuestPanel.SetActive(false);

        RefreshText();
    }

    private void Update()
    {
        RefreshText();
    }

    public void ClosePort()
    {
        HideAllPortPanels();
        currentTrader = null;
    }

    public void HideInteractionText()
    {
        if (pressFText != null) pressFText.SetActive(false);
    }

    private void RefreshText()
    {
        if (infoText == null || playerShip == null) return;

        if (infoText.Length > 0 && infoText[0] != null) infoText[0].text = $"Money: ${playerShip.money:F2}";
        if (infoText.Length > 1 && infoText[1] != null) infoText[1].text = $"Level {playerShip.level}";
        if (infoText.Length > 2 && infoText[2] != null) infoText[2].text = $"{playerShip.experience}/{playerShip.experienceToNextLevel}";
        if (infoText.Length > 3 && infoText[3] != null) infoText[3].text = $"Inventory Size: {playerShip.inventorySize}";
        if (infoText.Length > 4 && infoText[4] != null) infoText[4].text = $"Current supplies: {playerShip.supplies}";
        RefreshSuppliesUI();
    }

    private void HideAllPortPanels()
    {
        if (mainPortPanel != null)
            mainPortPanel.SetActive(false);

        if (tradePanel != null)
            tradePanel.SetActive(false);

        if (questsPanel != null)
            questsPanel.SetActive(false);

        if (upgradesPanel != null)
            upgradesPanel.SetActive(false);

        if (islandInfoPanel != null)
            islandInfoPanel.SetActive(false);

        if (playerInventoryPanel != null)
            playerInventoryPanel.SetActive(false);

        if (playerQuestPanel != null)
            playerQuestPanel.SetActive(false);

    }

    /* For another Classes */
    public void TogglePlayerInventory()
    {
        if (playerInventoryPanel == null)
            return;

        bool isOpened = playerInventoryPanel.activeSelf;

        playerInventoryPanel.SetActive(!isOpened);

        if (!isOpened)
            playerShip.RefreshInventoryUI();
    }

    public void TogglePlayerQuest()
    {
        if (playerQuestPanel == null)
            return;
        
        bool isOpened = playerQuestPanel.activeSelf;

        playerQuestPanel.SetActive(!isOpened);

        if (!isOpened)
            RefreshPlayerQuestUI();

    }

    private void RefreshPlayerQuestUI()
    {
        if(questText == null) return;
        questText[0].text = "";
        questText[1].text = "";
        if(questManager.ActiveQuests.Count == 0)
            questText[0].text = "No Quests... You can find them by Trader. ";
        int i = 0;
        foreach(var quest in questManager.ActiveQuests)
        {
            if(i <= 7)
            {
                questText[0].text +="____________________________________________________________\n";
                questText[0].text += $"The {quest.data.destinationTraderId}"+
                $" needs {quest.data.requiredAmount} of {quest.data.itemType}."+
                $" Reward: {quest.data.moneyReward}$ , {quest.data.experienceReward} Exp \n";
                questText[0].text +="____________________________________________________________\n";
            }
            else
            {
                questText[1].text +="____________________________________________________________\n";
                questText[1].text += $"The {quest.data.destinationTraderId}"+
                $" needs {quest.data.requiredAmount} of {quest.data.itemType}."+
                $" Reward: {quest.data.moneyReward}$ , {quest.data.experienceReward} Exp \n";
                questText[1].text +="____________________________________________________________\n";
            }

            i++;
        }
    }

    public void RefreshPlayerInventory(Slot[] slots)
    {
        if (slots == null) return;
        for (int i = 0; i < slotButtons.Length; i++)
        {
            if (i < slots.Length && !slots[i].IsEmpty)
            {
                Sprite sprite = itemDatabase.GetSprite(slots[i].Type);
                slotButtons[i].Setup(slots[i], sprite);
            }
            else
            {
                slotButtons[i].Clear();
            }
        }
    }

    public void OpenPort(TraderData data)
    {
        if (data == null) return; 

        currentTrader = data;
        traderNameText.text = currentTrader.traderId;
        HideInteractionText();
        HideAllPortPanels();
        mainPortPanel.SetActive(true);
    }
    
    public void OpenMainPortPanel()
    {
        HideAllPortPanels();
        mainPortPanel.SetActive(true);
    }
    
    public void OpenTradePanel()
    {
        if (currentTrader == null) return;

        HideAllPortPanels();
        tradePanel.SetActive(true);
        RefreshTradeWindow();
    }

    public void OpenQuestsPanel()
    {
        if (currentTrader == null)return;

        HideAllPortPanels();
        questsPanel.SetActive(true);
        RefreshQuestWindow();
    }

    private void RefreshQuestWindow()
    {
        for (int i = 0; i < questSlots.Length; i++)
        {
            questSlots[i].Clear();
        }

        if (currentTrader == null || questManager == null) return;

        List<Quest> available =questManager.GetAvailableQuests(currentTrader.traderId);
        List<Quest> completable = questManager.GetCompletableQuests(currentTrader.traderId);

        int slotIndex = 0;

        foreach (Quest quest in completable)
        {
            if (slotIndex >= questSlots.Length) break;

            questSlots[slotIndex].Setup(quest, QuestSlotAction.Complete, this);
            slotIndex++;
        }

        foreach (Quest quest in available)
        {
            if (slotIndex >= questSlots.Length) break;

            questSlots[slotIndex].Setup(quest, QuestSlotAction.Accept, this);
            slotIndex++;
        }
    }

    public void OpenUpgradesPanel()
    {
        HideAllPortPanels();
        upgradesPanel.SetActive(true);

        RefreshUpgradeWindow();
    }

    private void RefreshUpgradeWindow()
    {
        if (playerShip == null) return;

        RefreshSpeedUpgradeUI();
        RefreshCargoUpgradeUI();
        RefreshText();
    }

    private void RefreshSpeedUpgradeUI()
    {
        int level = playerShip.speedUpgradeLevel;
        int maxLevel = playerShip.maxSpeedUpgradeLevel;

        speedUpgradeLevelText.text = $"Level: {level}/{maxLevel}";

        if (level >= maxLevel)
        {
            speedUpgradePriceText.text = "MAX";
            return;
        }

        float price = GetSpeedUpgradePrice();
        speedUpgradePriceText.text = $"Price: ${price:F2}";
    }

    private void RefreshCargoUpgradeUI()
    {
        int level = playerShip.cargoUpgradeLevel;
        int maxLevel = playerShip.maxCargoUpgradeLevel;

        cargoUpgradeLevelText.text = $"Level: {level}/{maxLevel}";

        if (level >= maxLevel)
        {
            cargoUpgradePriceText.text = "MAX";
            return;
        }

        float price = GetCargoUpgradePrice();
        cargoUpgradePriceText.text = $"Price: ${price:F2}";
    }

    private void RefreshSuppliesUI()
    {
        if (suppliesPriceText == null) return;

        suppliesPriceText.text = $"Price: ${GetSuppliesPrice():F0}";
    }

    private float GetSpeedUpgradePrice() => speedUpgradeBasePrice * (playerShip.speedUpgradeLevel + 1);

    private float GetCargoUpgradePrice() => cargoUpgradeBasePrice *(playerShip.cargoUpgradeLevel + 1);

    private float GetSuppliesPrice()
    {
        float missing = playerShip.maxSupplies - playerShip.supplies;
        float levelMultiplier = 1f + (Mathf.Sqrt(playerShip.level) - 1f) * 0.6f;
        float pricePerSupply = suppliesBasePrice * levelMultiplier;

        return Mathf.Ceil(missing * pricePerSupply);
    }

    public void BuySpeedUpgrade()
    {
        if (playerShip.speedUpgradeLevel >= playerShip.maxSpeedUpgradeLevel)
        {
            Debug.Log("Speed upgrade is at maximum level.");
            return;
        }

        float price = GetSpeedUpgradePrice();

        if (playerShip.money < price)
        {
            Debug.Log("Not enough money for speed upgrade.");
            return;
        }

        playerShip.money -= price;
        playerShip.speedUpgradeLevel++;
        playerShip.IncreaseSpeed(speedIncreasePerUpgrade);

        Debug.Log($"Speed upgraded. Level: {playerShip.speedUpgradeLevel}, Multiplier: {playerShip.SpeedMultiplier}");
        RefreshUpgradeWindow();
    }

    public void BuyCargoUpgrade()
    {
        if (playerShip.cargoUpgradeLevel >=
            playerShip.maxCargoUpgradeLevel)
        {
            Debug.Log("Cargo upgrade is at maximum level.");
            return;
        }

        float price = GetCargoUpgradePrice();

        if (playerShip.money < price)
        {
            Debug.Log("Not enough money for cargo upgrade.");
            return;
        }

        playerShip.money -= price;
        playerShip.cargoUpgradeLevel++;
        playerShip.AddInventorySlots(cargoSlotsPerUpgrade);

        Debug.Log($"Cargo upgraded. Level: {playerShip.cargoUpgradeLevel}, Slots: {playerShip.inventorySlots.Length}");
        RefreshUpgradeWindow();
    }
    public void OpenIslandInfoPanel()
    {
        HideAllPortPanels();
        islandInfoPanel.SetActive(true);
        RefreshIsladInfoPanel();
    }

    private void RefreshIsladInfoPanel()
    {
        traderMoney.text = $"Money: {currentTrader.money}";
        itemsStock.text = "";
        foreach(var item in currentTrader.inventory)
        {
            itemsStock.text += $"{item.Type}: {item.stock} \n";
        }
    }

    private void OnBuyClicked(TradeItem item)
    {
        if (currentTrader == null) return;

        if (!TradeService.TryBuyFromTrader(playerShip, currentTrader, item, 1, out string error))
        {
            Debug.Log(error);
            return;
        }
        
        RefreshTradeWindow();
    }

    private void OnSellClicked(Slot slot)
    {
        if (currentTrader == null || slot == null || slot.IsEmpty) return;

        if (!TradeService.TrySellToTrader(playerShip, currentTrader, slot.Type,1, itemDatabase, out float receivedMoney, out string error))
        {
            Debug.Log(error);
            return;
        }

        Debug.Log($"Sold {slot.Type} for ${receivedMoney}.");
        RefreshTradeWindow();
    }

    private void RefreshTradeWindow()
    {
        if (currentTrader == null) return;

        playerShip.RefreshInventoryUI();
        RefreshBuySlots(currentTrader);
        RefreshSellSlots();
        RefreshText();
    }

    private void RefreshBuySlots(TraderData data)
    {
        for (int i = 0; i < traderBuyButtons.Length; i++)
        {
            if (i < data.inventory.Length && data.inventory[i] != null)
            {
                TradeItem item = data.inventory[i];
                Sprite sprite = itemDatabase.GetSprite(item.Type);
                traderBuyButtons[i].Setup(item, OnBuyClicked, sprite);
            }
            else
            {
                traderBuyButtons[i].Clear();
            }
        }
    }

    private void RefreshSellSlots()
    {
        Slot[] playerSlots = playerShip.inventorySlots;

        for (int i = 0; i < traderSellButtons.Length; i++)
        {
            if (i >= playerSlots.Length ||
                playerSlots[i] == null ||
                playerSlots[i].IsEmpty)
            {
                traderSellButtons[i].Clear();
                continue;
            }

            Slot slot = playerSlots[i];
            Sprite sprite = itemDatabase.GetSprite(slot.Type);
            float sellPrice = TradeService.GetSellPrice(currentTrader,slot.Type);

            traderSellButtons[i].Setup(slot, OnSellClicked, sprite, sellPrice );
        }
    }

    public void ShowInteractionText()
    {
        if (pressFText != null) pressFText.SetActive(true);
    }

    public void ClosePlayerInventory()  
    {
        if (playerInventoryPanel != null) playerInventoryPanel.SetActive(false);
    }

    public bool IsPortOpened() =>  mainPortPanel.activeSelf || tradePanel.activeSelf || questsPanel.activeSelf || upgradesPanel.activeSelf || islandInfoPanel.activeSelf;

    public bool IsInventoryOpened() => playerInventoryPanel.activeSelf;

    public bool IsPlayerQuestOpened() => playerQuestPanel.activeSelf;

    public void AcceptQuest(Quest quest)
    {
        if (quest == null) return;

        questManager.AcceptQuest(quest);
        RefreshQuestWindow();
    }

    public void CompleteQuest(Quest quest)
    {
        if (quest == null || currentTrader == null) return;

        bool completed = questManager.TryCompleteDelivery(playerShip, currentTrader, quest);

        if (!completed)
        {
            Debug.Log("Quest cannot be completed.");
            return;
        }

        playerShip.RefreshInventoryUI();
        RefreshText();
        RefreshQuestWindow();
    }

    public void BuySupplies()
    {
        float price = GetSuppliesPrice();

        if (playerShip.money < price)
        {
            Debug.Log("Not enough money for supplies.");
            return;
        }

        playerShip.money -= price;
        playerShip.supplies = playerShip.maxSupplies;

        RefreshText();
    }

    public void ShowDeathScreen()
    {
        deathPanel.SetActive(true);
    }


}
