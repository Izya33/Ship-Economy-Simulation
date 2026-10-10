using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour, ITradeAgent, ITradeProgression
{
    [Header("Movement")]
    public float maxThrust = 5f;
    public float thrustChangeSpeed = 2f;
    public float turnSpeed = 60f;
    public float SpeedMultiplier = 1f;
    [SerializeField] private bool inverseInput;
    
    [Header("Dash")]
    [SerializeField] private float dashMultiplier = 2.5f;
    [SerializeField] private float dashDuration = 0.7f;
    [SerializeField] private float dashCooldown = 3f;
    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;

    [Header("Level")]
    [SerializeField] public int level = 1;
    [SerializeField] public float experience;
    [SerializeField] public float experienceToNextLevel = 100f;

    [Header("Upgrades")]
    public int speedUpgradeLevel;
    public int cargoUpgradeLevel;
    public int maxSpeedUpgradeLevel = 5;
    public int maxCargoUpgradeLevel = 5;

    [Header("Economy")]
    public float money = 100f;
    [Min(1)] public int inventorySize = 1;
    [Min(1)] public int maxStackSize = 64; // May be next possible Upgrade 
    [SerializeField] private List<PlayerTradeItem> tradeItems = new();
    public Slot[] inventorySlots;
    public float supplies;
    [HideInInspector] public float maxSupplies;

    [Header("Supplies Consumption")]
    [SerializeField] private float baseSupplyConsumption = 0.02f;
    [SerializeField] private float maxSpeedSupplyConsumption = 0.14f;
    [SerializeField] private float dashSupplyMultiplier = 1.5f;

    [Header("Engine Particles")]
    [SerializeField] private ParticleSystem leftEngineParticles;
    [SerializeField] private ParticleSystem rightEngineParticles;
    [SerializeField] private float idleEmission = 0f;
    [SerializeField] private float movingEmission = 5f;
    [SerializeField] private float maxEmission = 35f;

    [Header("Engine Positions")]
    [SerializeField] private Transform leftEngine;
    [SerializeField] private Transform rightEngine;
    [SerializeField] private Vector2[] leftEnginePositions;
    [SerializeField] private Vector2[] rightEnginePositions;
    [SerializeField] private float[] engineRotations;
    [SerializeField] private BoxCollider2D shipCollider;
    [SerializeField] private Vector2[] colliderSizes;
    [SerializeField] private Vector2[] colliderOffsets;


    [SerializeField] private UIManager uiManager;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] directionSprites;
    private Rigidbody2D rb;

    [HideInInspector] public float leftThrust;
    [HideInInspector] public float rightThrust;
    private float currentTurnAngle;
    private bool isDead = false;


    public float Money { get => money; set => money = value; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        InitializeInventory();
        leftEngineParticles.Play();
        rightEngineParticles.Play();
        maxSupplies = supplies;
    }

    private void Update()
    {
        UpdateDash();
        HandlePaddling();
        UpdateSupplies();
        HandleInventoryInput();
        HandleQuestInput();
    }
    
    private void InitializeInventory()
    {
        inventorySlots = new Slot[inventorySize];
        for (int i = 0; i < inventorySlots.Length; i++)
            inventorySlots[i] = new Slot();
    }

    private void UpdateDash()
    {
        if (Keyboard.current == null) return;

        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)isDashing = false;
            return;
        }

        if (Keyboard.current.leftShiftKey.wasPressedThisFrame && dashCooldownTimer <= 0f && rb.linearVelocity.magnitude > 0.1f)
        {
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
        }
    }

    private void HandlePaddling()
    {
        if (Keyboard.current == null)
            return;

        float thrustDelta = thrustChangeSpeed * Time.deltaTime;

        if (Keyboard.current.wKey.isPressed)
        {
            leftThrust += thrustDelta;
            rightThrust += thrustDelta;
        }
        else if (Keyboard.current.sKey.isPressed)
        {
            leftThrust -= thrustDelta;
            rightThrust -= thrustDelta;
        }
        else
        {
            if (Keyboard.current.qKey.isPressed)
                leftThrust += thrustDelta;

            if (Keyboard.current.aKey.isPressed)
                leftThrust -= thrustDelta;

            if (Keyboard.current.eKey.isPressed)
                rightThrust += thrustDelta;

            if (Keyboard.current.dKey.isPressed)
                rightThrust -= thrustDelta;
        }

        leftThrust = Mathf.Clamp(leftThrust, 0f, maxThrust);
        rightThrust = Mathf.Clamp(rightThrust, 0f, maxThrust);

        float totalThrust = (leftThrust / maxThrust) - (rightThrust / maxThrust);
        currentTurnAngle += totalThrust * turnSpeed * Time.deltaTime;
        currentTurnAngle = (currentTurnAngle + 360f) % 360f;

        Vector3 direction = new( Mathf.Sin(currentTurnAngle * Mathf.Deg2Rad), Mathf.Cos(currentTurnAngle * Mathf.Deg2Rad), 0f);

        if (inverseInput)
            direction = new Vector3(Mathf.Cos(currentTurnAngle * Mathf.Deg2Rad), Mathf.Sin(currentTurnAngle * Mathf.Deg2Rad), 0f);

        float currentSpeedMultiplier = SpeedMultiplier;

        if (isDashing)
            currentSpeedMultiplier *= dashMultiplier;

        rb.linearVelocity =
            direction * (leftThrust + rightThrust) * currentSpeedMultiplier;

        UpdateShipSprite();
        UpdateEngineParticles();
    }

    private void UpdateSupplies()
    {
        if (isDead) return;

        float currentSpeed = rb.linearVelocity.magnitude;
        float normalMaxSpeed = maxThrust * 2f * SpeedMultiplier;
        float speedFactor = Mathf.Clamp01(currentSpeed / normalMaxSpeed);
        speedFactor *= speedFactor;

        float consumption = Mathf.Lerp(baseSupplyConsumption, maxSpeedSupplyConsumption, speedFactor);

        if (isDashing)
            consumption *= dashSupplyMultiplier;

        supplies -= consumption * Time.deltaTime;
        supplies = Mathf.Clamp(supplies, 0f, 100f);

        if (supplies <= maxSupplies*0.5)
        {
            leftThrust = Mathf.Min(leftThrust, maxThrust * 0.5f);
            rightThrust = Mathf.Min(rightThrust, maxThrust * 0.5f);
        }

        CheckSupplies();
    }

    private void CheckSupplies()
    {
        if (isDead)
            return;

        if (supplies <= 0f)
        {
            supplies = 0f;
            StartCoroutine(Die());
        }
    }

    private IEnumerator Die()
    {
        isDead = true;

        enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        uiManager.ClosePort();
        uiManager.ClosePlayerInventory();
        uiManager.ShowDeathScreen();

        yield return new WaitForSeconds(4f);

        SceneManager.LoadScene("Menu");
    }

    private void UpdateShipSprite()
    {
        float angle = (currentTurnAngle + 22.5f) % 360f;
        int index = Mathf.FloorToInt(angle / 45f);

        spriteRenderer.sprite = directionSprites[index];
        shipCollider.size = colliderSizes[index];
        shipCollider.offset = colliderOffsets[index];

        if (leftEnginePositions.Length > index)
            leftEngine.localPosition = leftEnginePositions[index];

        if (rightEnginePositions.Length > index)
            rightEngine.localPosition = rightEnginePositions[index];
        
        if (engineRotations.Length > index)
        {
            leftEngine.localRotation = Quaternion.Euler(0f, 0f, engineRotations[index]);
            rightEngine.localRotation = Quaternion.Euler(0f, 0f, engineRotations[index]);
        }
    }

    private void UpdateEngineParticles()
    {
        if (leftEngineParticles == null || rightEngineParticles == null) return;

        float speed = rb.linearVelocity.magnitude;
        float speedFactor = Mathf.Clamp01(speed / (maxThrust * 2f * SpeedMultiplier));
        float baseEmission = Mathf.Lerp(idleEmission, maxEmission, speedFactor);

        float leftEmission = 0f;
        float rightEmission = 0f;

        if (speed > 0.05f)
        {
            leftEmission = movingEmission;
            rightEmission = movingEmission;
        }

        if (Keyboard.current.qKey.isPressed) leftEmission = baseEmission;
        if (Keyboard.current.eKey.isPressed) rightEmission = baseEmission;
        if (Keyboard.current.wKey.isPressed)
        {
            leftEmission = baseEmission;
            rightEmission = baseEmission;
        }

        if(Keyboard.current.aKey.isPressed) leftEmission = 0;
        if(Keyboard.current.dKey.isPressed) rightEmission = 0;
        if (Keyboard.current.sKey.isPressed)
        {
            leftEmission = 0;
            rightEmission = 0;
        }

        if (isDashing)
        {
            leftEmission = maxEmission * 1.5f;
            rightEmission = maxEmission * 1.5f;
        }

        var left = leftEngineParticles.emission;
        left.rateOverTime = leftEmission;

        var right = rightEngineParticles.emission;
        right.rateOverTime = rightEmission;
    }

    private void HandleInventoryInput()
    {
        if (Keyboard.current == null || uiManager == null) return;
        if (uiManager.IsPortOpened() || uiManager.IsPlayerQuestOpened()) return;
        if (Keyboard.current.tabKey.wasPressedThisFrame )
            uiManager.TogglePlayerInventory();
    }
    
    private void HandleQuestInput()
    {
        if(Keyboard.current == null || uiManager == null ) return;
        if(uiManager.IsPortOpened() || uiManager.IsInventoryOpened() ) return;
        if(Keyboard.current.tKey.wasPressedThisFrame)
            uiManager.TogglePlayerQuest();
    }

    /* For another Classes */
    public void AddExperience(float amount)
    {
        experience += amount;
        while (experienceToNextLevel <= experience)
        {
            experience -= experienceToNextLevel;
            LevelUp();
            CalculateExperienceForNextLevel();
        }
    }

    private void LevelUp()
    {
        level++;
        PlayerLevelRewards.Apply(this,level);
    }

    private void CalculateExperienceForNextLevel() => experienceToNextLevel = 100 * level;
    
    public void OnItemBought(ItemType type, int quantity, float unitPrice) => RememberPurchase(type, quantity, unitPrice);

    public void OnItemSold(ItemType type, int quantity, float unitPrice)
    {
        PlayerTradeItem tradeItem = GetTradeItem(type);
        if (tradeItem == null) return;

        float profitPerItem = unitPrice - tradeItem.averageBuyPrice;
        float totalProfit = profitPerItem * quantity;

        if (totalProfit > 0f) AddExperience(totalProfit);
        Debug.Log($"Sold {quantity} {type} for {unitPrice}. Average buy: {tradeItem.averageBuyPrice}, Profit: {totalProfit}");

        RemovePurchaseData(type, quantity);
    }

    public void AddInventorySlots(int amount)
    {
        if (amount <= 0) return;

        Slot[] newInventory = new Slot[inventorySlots.Length + amount];
        System.Array.Copy(inventorySlots, newInventory, inventorySlots.Length); //Without this we will just remove whole inventory, and create new  empty array

        for (int i = inventorySlots.Length; i < newInventory.Length; i++)
            newInventory[i] = new Slot();

        inventorySlots = newInventory;
        inventorySize = inventorySlots.Length;

        RefreshInventoryUI();
    }

    public void IncreaseSpeed(float amount) => SpeedMultiplier += amount;

    public bool AddItem(ItemType type, int amount)
    {
        if (type == ItemType.Empty || amount <= 0) return false;

        // Check capacity before changing the inventory so the operation is atomic.
        if (GetFreeCapacityFor(type) < amount)
            return false;

        for (int i = 0; i < inventorySlots.Length && amount > 0; i++)
        {
            Slot slot = inventorySlots[i];

            if (slot.Type != type || slot.Count >= maxStackSize) continue;

            int toAdd = Mathf.Min(maxStackSize - slot.Count, amount);
            slot.Count += toAdd;
            amount -= toAdd;
        }

        for (int i = 0; i < inventorySlots.Length && amount > 0; i++)
        {
            Slot slot = inventorySlots[i];
            if (!slot.IsEmpty) continue;

            int toAdd = Mathf.Min(maxStackSize, amount);
            slot.Type = type;
            slot.Count = toAdd;
            amount -= toAdd;
        }

        RefreshInventoryUI();
        return amount == 0;
    }

    public bool RemoveItem(ItemType type, int amount)
    {
        if (amount <= 0) return false;
        if (!HasItem(type,amount)) return false;

        int amountLeft = amount;

        foreach (Slot slot in inventorySlots)
        {
            if (slot.Type != type) continue;

            int removeAmount = Mathf.Min(slot.Count,amountLeft);

            slot.Count -= removeAmount;
            amountLeft -= removeAmount;

            if (slot.Count <= 0)
            {
                slot.Type = ItemType.Empty;
                slot.Count = 0;
            }

            if (amountLeft <= 0) return true;
        }
        return false;
    }

    public bool HasItem(ItemType itemType,int amount)
    {
        int totalAmount = 0;

        foreach (Slot slot in inventorySlots)
        {
            if (slot.Type != itemType) continue;

            totalAmount += slot.Count;
        }
        return totalAmount >= amount;
    }

    private void RememberPurchase(ItemType type, int amount, float price)
    {
        if (type == ItemType.Empty || amount <= 0) return;
        PlayerTradeItem item = GetTradeItem(type);
        if(item == null)
        {
            item = new PlayerTradeItem{averageBuyPrice = price, itemType = type,quantity = amount};
            tradeItems.Add(item);
            return;
        }
            
        float oldCost  = item.averageBuyPrice*item.quantity;
        float newCost  = amount*price;
        item.quantity += amount;
        item.averageBuyPrice = (oldCost + newCost)/item.quantity;
        Debug.Log($"Bought {amount} {type}. Total: {item.quantity}, Average: {item.averageBuyPrice}");
    }

    private PlayerTradeItem GetTradeItem(ItemType type)
    {
        foreach (PlayerTradeItem item in tradeItems)
        {
            if (item.itemType == type) return item;
        }
        return null;
    }

    public int GetItemCount(ItemType type)
    {
        int total = 0;

        foreach (Slot slot in inventorySlots)
        {
            if (slot.Type == type) total += slot.Count;
        }
        return total;
    }

    private bool RemovePurchaseData(ItemType type, int amount)
    {
        PlayerTradeItem item = GetTradeItem(type);
        if (item != null)
        {
            int expectedAmount = item.quantity - amount;
            if(expectedAmount < 0) return false;
            if(expectedAmount == 0) 
            {
                tradeItems.Remove(item);
                return true;
            }
            item.quantity = expectedAmount;
            return true;
        }
        return false;
    }

    public void RefreshInventoryUI()
    {
        if (uiManager != null) uiManager.RefreshPlayerInventory(inventorySlots);
    }

    private int GetFreeCapacityFor(ItemType type)
    {
        int capacity = 0;

        foreach (Slot slot in inventorySlots)
        {
            if (slot.IsEmpty)
            {
                capacity += maxStackSize;
            }
            else if (slot.Type == type)
            {
                capacity += maxStackSize - slot.Count;
            }
        }

        return capacity;
    }
}
[System.Serializable]
public class PlayerTradeItem
{
    public ItemType itemType;
    public int quantity;
    public float averageBuyPrice;
}
