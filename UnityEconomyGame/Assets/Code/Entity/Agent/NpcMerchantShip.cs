using System.Collections.Generic;
using UnityEngine;

public class NpcMerchantShip : MonoBehaviour, ITradeAgent
{

    [Header("References")]
    [SerializeField] private IslandManager islandManager;
    [SerializeField] private ItemDatabase itemDatabase;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float moveSpeed = 3f;
    [SerializeField, Min(0f)] private float arrivalDistance = 2.5f;

    [Header("Economy")]
    [SerializeField, Min(0f)] private float money = 100f;

    [Header("Inventory")]
    [SerializeField, Min(1)] private int inventorySize = 10;
    [SerializeField, Min(1)] private int cargoCapacity = 10;

    [Header("Idle")]
    [SerializeField, Min(0.1f)] private float routeRetryDelay = 5f;

    [Header("Work")]
    [SerializeField, Min(0.1f)] private float workDuration = 5f;
    [SerializeField, Min(0f)] private float workPayment = 10f;

    [Header("Knowledge")]
    [SerializeField, Min(0f)] private float departureDelayAfterBuying = 3f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] directionSprites = new Sprite[8];

    [Header("Obstacle Avoidance")]
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField, Min(0.1f)] private float visionDistance = 8f;
    [SerializeField, Range(5f,90f)] private float sideVisionAngle = 45f;
    [SerializeField, Range(0f,1f)] private float targetDirectionStrength = 0.5f;
    [SerializeField, Min(0.1f)] private float visionRadius = 0.7f;

    [Header("Avoidance Stability")]
    [SerializeField, Min(0.1f)] private float avoidanceDecisionDuration = 1.5f;
    private int avoidanceSide;
    private float avoidanceDecisionTimer;

    [Header("Debug")]
    [SerializeField] private State currentState = State.Idle;
    private Vector2 debugRayOrigin;
    private Vector2 debugForwardDirection;
    private Vector2 debugLeftDirection;
    private Vector2 debugRightDirection;

    private bool debugForwardBlocked;
    private bool debugLeftBlocked;
    private bool debugRightBlocked;
    private State stateAfterWaiting;

    private Rigidbody2D rb;

    private Trader currentTrader;
    private Trader targetTrader;

    private Slot[] inventory;
    private readonly List<KnownTrader> knownTraders = new();
    private TradeOpportunity tradeOpportunity;

    private float stateTimer;

    public float Money
    {
        get => money;
        set => money = value;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        initializeInventory();
        ChangeState(State.ChoosinRoute);
    }

    private void initializeInventory()
    {
        inventory = new Slot[inventorySize];
        for (int i = 0; i < inventorySize; i++) inventory[i] = new Slot();
    }
    private void ChangeState(State newState)
    {
        Debug.Log($"State: {currentState} -> {newState}");

        currentState = newState;
        stateTimer = 0f;

    }

    private void Update()
    {
        switch (currentState)
        {
            case State.Idle:
                Idle();
                break;
            case State.ChoosinRoute:
                ChoosinRoute();
                break;
            case State.Buying:
                Buy();
                break;
            case State.Selling:
                Sell();
                break;
            case State.PreparingToSail:
                PreparingToSail();
                break;
            case State.Working:
                Work();
                break;
        }
    }
    private void FixedUpdate()
    {
        if (currentState == State.Sailing) Sail();
        if (currentState == State.SailingToWork) SailToWork();
        if (currentState == State.SailingToExplore) SailToExplore();
    }
    private void Idle()
    {
        rb.linearVelocity = Vector2.zero;
        stateTimer += Time.deltaTime;

        if (stateTimer >= routeRetryDelay)
            ChangeState(State.ChoosinRoute);
    }
    private void ChoosinRoute()
    {

        if (knownTraders.Count < 2)
        {
            if (StartExploring()) return;

            StartWorking();
            return;
        }

        rb.linearVelocity = Vector2.zero;

        int traderCount = islandManager.Traders.Length;

        float progress = traderCount > 0 ? (float)knownTraders.Count / traderCount : 1f;
        float chance = Mathf.Lerp(0.25f,0.05f,Mathf.Clamp01(progress));

        if (knownTraders.Count >= 2 && knownTraders.Count < traderCount && Random.value < chance)
        {
            if (StartExploring()) return;
        }

        if (TryGetCargo(out ItemType cargoType,out int cargoQuantity))
        {
            tradeOpportunity = FindBestSellOpportunity(cargoType,cargoQuantity);

            if (tradeOpportunity != null)
            {
                targetTrader = tradeOpportunity.sellTrader;
                currentTrader = null;

                Debug.Log($"NPC carrying {cargoQuantity} {cargoType}. Best buyer: {targetTrader.name}, quantity: {tradeOpportunity.quantity}, price: {tradeOpportunity.sellPrice}");

                ChangeState(State.Sailing);
                return;
            }

            if (knownTraders.Count < traderCount && Random.value < chance)
            {
                if (StartExploring()) return;
            }

            Debug.Log($"No known trader can currently buy {cargoQuantity} {cargoType}");

            ChangeState(State.Idle);
            return;
        }

        tradeOpportunity = FindBestTrade();

        if (tradeOpportunity != null)
        {
            targetTrader = tradeOpportunity.buyTrader;
            currentTrader = null;

            Debug.Log($"NPC found trade: buy {tradeOpportunity.quantity} {tradeOpportunity.itemType} from {tradeOpportunity.buyTrader.name}, sell to {tradeOpportunity.sellTrader.name}");

            ChangeState(State.Sailing);
            return;
        }

        if (knownTraders.Count < 2)
        {
            StartWorking();
            return;
        }

        if (knownTraders.Count < traderCount && Random.value < chance)
        {
            if (StartExploring()) return;
        }

        if (!CanAffordKnownItem())
        {
            StartWorking();
            return;
        }

        ChangeState(State.Idle);
    }
    private void Buy()
    {
        if(currentTrader == null)
        {
            ChangeState(State.Sailing);
            return;
        }
        if (GetFreeCargoSpace() <= 0)
        {
            ChangeState(State.ChoosinRoute);
            return;
        }

        int quantity = Mathf.Min(tradeOpportunity.quantity, GetFreeCargoSpace());

        if (quantity <= 0)
        {
            tradeOpportunity = null;
            ChangeState(State.ChoosinRoute);
            return;
        }

        TradeItem item = currentTrader.data.GetItem(tradeOpportunity.itemType);
        if (item == null)
        {
            tradeOpportunity = null;
            ChangeState(State.ChoosinRoute);
            return;
        }

        if(!TradeService.TryBuyFromTrader(this, currentTrader.data, item, quantity, out string error))
        {
            Debug.Log(error);
            tradeOpportunity = null;
            ChangeState(State.ChoosinRoute);
            return;
        }
        Debug.Log($"NPC bought {tradeOpportunity.quantity} {tradeOpportunity.itemType} from {currentTrader.name}");
        Debug.Log($"Cargo: {GetCargoAmount()}/{cargoCapacity}, Money: {Money}");

        tradeOpportunity.quantity = quantity;
        targetTrader = tradeOpportunity.sellTrader;
        RememberTraderData(currentTrader);

        WaitAtPortThen(State.Sailing);
    }
    private void Sell()
    {
        if(currentTrader == null)
        {
            ChangeState(State.Sailing);
            return;
        }

        if (tradeOpportunity == null)
        {
            ChangeState(State.ChoosinRoute);
            return;
        }

        TradeItem traderItem = currentTrader.data.GetItem(tradeOpportunity.itemType);

        if (traderItem == null || traderItem.currentPrice <= 0)
        {
            tradeOpportunity = null;
            ChangeState(State.ChoosinRoute);
            return;
        }

        int affordableQuantity = Mathf.FloorToInt(currentTrader.data.money / traderItem.currentPrice);
        int quantityToSell = Mathf.Min(tradeOpportunity.quantity, affordableQuantity);

        if (quantityToSell <= 0)
        {
            tradeOpportunity = null;
            ChangeState(State.ChoosinRoute);
            return;
        }

        if (!TradeService.TrySellToTrader(this, currentTrader.data, tradeOpportunity.itemType, quantityToSell, itemDatabase, out float receivedMoney, out string error))
        {
            Debug.Log(error);
            tradeOpportunity = null;
            ChangeState(State.ChoosinRoute);
            return;
        }   
        Debug.Log($"NPC sold {quantityToSell} {tradeOpportunity.itemType} to {currentTrader.name} for {receivedMoney}");
        Debug.Log($"Cargo: {GetCargoAmount()}/{cargoCapacity}, Money: {Money}");

        RememberTraderData(currentTrader);

        tradeOpportunity = null;
        targetTrader = null;

        WaitAtPortThen(State.ChoosinRoute);
    }
    private void PreparingToSail()
    {
        rb.linearVelocity = Vector2.zero;

        stateTimer += Time.deltaTime;

        if (stateTimer < departureDelayAfterBuying)
            return;

        State nextState = stateAfterWaiting;

        if (nextState == State.Sailing || nextState == State.SailingToWork || nextState == State.SailingToExplore)
        {
            currentTrader = null;
        }

        ChangeState(nextState);
    }
    private void Work()
    {
        rb.linearVelocity = Vector2.zero;

        if (currentTrader == null)
        {
            StartWorking();
            return;
        }

        stateTimer += Time.deltaTime;

        if (stateTimer < workDuration) return;

        Money += workPayment;

        Debug.Log($"NPC worked at {currentTrader.name}, earned {workPayment}. Money: {Money}");

        if (!CanAffordKnownItem())
        {
            stateTimer = 0;
            return;
        }

        WaitAtPortThen(State.ChoosinRoute);
    }
    private void Sail()
    {
        if (targetTrader == null || tradeOpportunity == null)
        {
            rb.linearVelocity = Vector2.zero;
            WaitAtPortThen(State.ChoosinRoute);
            return;
        }

        if (!MoveToTrader(targetTrader)) return;

        currentTrader = targetTrader;
        targetTrader = null;

        RememberTraderData(currentTrader);

        if (currentTrader == tradeOpportunity.buyTrader)
        {
            ChangeState(State.Buying);
            return;
        }

        if (currentTrader == tradeOpportunity.sellTrader)
        {
            ChangeState(State.Selling);
            return;
        }

        ChangeState(State.ChoosinRoute);
    }
    private void SailToWork()
    {
        if (targetTrader == null)
        {
            rb.linearVelocity = Vector2.zero;
            ChangeState(State.Idle);
            return;
        }

        if (!MoveToTrader(targetTrader)) return;

        currentTrader = targetTrader;
        targetTrader = null;

        RememberTraderData(currentTrader);

        Debug.Log($"NPC arrived at {currentTrader.name} to work");

        ChangeState(State.Working);
    }
    private void SailToExplore()
    {
        if (targetTrader == null)
        {
            rb.linearVelocity = Vector2.zero;
            WaitAtPortThen(State.ChoosinRoute);
            return;
        }

        if (!MoveToTrader(targetTrader)) return;

        currentTrader = targetTrader;
        targetTrader = null;

        RememberTraderData(currentTrader);

        Debug.Log($"NPC explored {currentTrader.name}");

        WaitAtPortThen(State.ChoosinRoute);
    }
    private bool StartExploring()
    {
        Trader unknownTrader = FindNearestUnknownTrader();

        if (unknownTrader == null) return false;

        tradeOpportunity = null;
        targetTrader = unknownTrader;

        if (currentTrader != targetTrader) currentTrader = null;
        Debug.Log($"NPC is exploring {targetTrader.name}");

        ChangeState(State.SailingToExplore);
        return true;
    }
    private void StartWorking()
    {
        tradeOpportunity = null;

        if (currentTrader != null)
        {
            ChangeState(State.Working);
            return;
        }

        targetTrader = FindNearestTrader();

        if (targetTrader == null)
        {
            ChangeState(State.Idle);
            return;
        }

        ChangeState(State.SailingToWork);
    }
    private bool TryGetCargo(out ItemType itemType, out int quantity)
    {
        foreach (Slot slot in inventory)
        {
            if (slot.Type != ItemType.Empty && slot.Count > 0)
            {
                itemType = slot.Type;
                quantity = slot.Count;
                return true;
            }
        }

        itemType = ItemType.Empty;
        quantity = 0;
        return false;
    }
    private TradeOpportunity FindBestSellOpportunity(ItemType itemType, int cargoQuantity)
    {
        Trader bestTrader = null;
        float bestTotalRevenue = 0f;
        float bestSellPrice = 0f;
        int bestQuantity = 0;

        foreach (KnownTrader knownTrader in knownTraders)
        {
            if (knownTrader == null || knownTrader.trader == null) continue;
            if (knownTrader.trader == currentTrader) continue;

            KnownTradeItem item = knownTrader.GetItem(itemType);

            if (item == null || item.price <= 0) continue;

            int quantity = cargoQuantity;

            if (quantity <= 0) continue;

            float totalRevenue = item.price * quantity;

            if (totalRevenue > bestTotalRevenue)
            {
                bestTotalRevenue = totalRevenue;
                bestTrader = knownTrader.trader;
                bestSellPrice = item.price;
                bestQuantity = quantity;
            }
        }

        if (bestTrader == null) return null;

        return new TradeOpportunity(null, bestTrader, itemType, 0f, bestSellPrice, bestQuantity);
    }
    private TradeOpportunity FindBestTrade() // das hier, hat VIEEEELLL Zeit und Leid gebraucht, kann man das mit Dictionary Optimieren(ja, klar), werde ich das machen?
    {
        if (knownTraders.Count < 2) return null;
        KnownTrader[] traders = new KnownTrader[knownTraders.Count];
        knownTraders.CopyTo(traders);

        TradeOpportunity bestOpportunity = null;
        float totalBestProfit = 0f;

        for (int b = 0; b < traders.Length; b++)
        {
            if (traders[b] == null || traders[b].trader == null) continue;

            for (int s = 0; s < traders.Length; s++)
            {
                if (traders[s] == null || traders[s].trader == null) continue;
                if (b == s) continue;

                for (int k = 0; k < traders[b].inventory.Count; k++)
                {
                    KnownTradeItem buyItem = traders[b].inventory[k];

                    if (buyItem.stock <= 0) continue;

                    KnownTradeItem sellItem = traders[s].GetItem(buyItem.itemType);
                    if (sellItem == null) continue;

                    float sellPrice = sellItem.price;
                    float buyPrice = buyItem.price;

                    float profitPerUnit = sellPrice - buyPrice;
                    if (profitPerUnit <= 0) continue;

                    int stockAvailable = buyItem.stock;
                    int affordableAmount = Mathf.FloorToInt(money / buyPrice);
                    int cargoSpace = GetFreeCargoSpace();

                    int amount = Mathf.Min(stockAvailable, affordableAmount, cargoSpace);
                    if (amount <= 0) continue;

                    float totalProfit = profitPerUnit * amount;

                    if (totalProfit > totalBestProfit)
                    {
                        totalBestProfit = totalProfit;
                        bestOpportunity = new TradeOpportunity(traders[b].trader, traders[s].trader, buyItem.itemType, buyPrice, sellPrice, amount);
                    }
                }
            }
        }

        return bestOpportunity;
    }
    private bool CanAffordKnownItem()
    {
        foreach (KnownTrader knownTrader in knownTraders)
        {
            if (knownTrader == null || knownTrader.trader == null) continue;

            foreach (KnownTradeItem item in knownTrader.inventory)
            {
                if (item == null) continue;
                if (item.stock <= 0 || item.price <= 0) continue;

                if (Money >= item.price) return true;
            }
        }

        return false;
    }
    private int GetFreeCargoSpace() => cargoCapacity - GetCargoAmount();
    private int GetCargoAmount()
    {
        int amount = 0;
        foreach (Slot slot in inventory) amount += slot.Count;
        return amount;
    }
    private void RememberTraderData(Trader trader)
    {
        if (trader == null)return;

        KnownTrader knownTrader = knownTraders.Find(t => t.trader == trader);

        if (knownTrader == null)
        {
            knownTrader = new KnownTrader();
            knownTrader.trader = trader;
            knownTraders.Add(knownTrader);
        }

        knownTrader.inventory.Clear();

        foreach (TradeItem item in trader.data.inventory)
        {
            knownTrader.inventory.Add(new KnownTradeItem
            {
                itemType = item.Type,
                price = item.currentPrice,
                stock = item.stock
            });
        }

        Debug.Log($"Updated knowledge about {trader.name}");
    }
    private void WaitAtPortThen(State nextState)
    {
        stateAfterWaiting = nextState;
        ChangeState(State.PreparingToSail);
    }
    private bool MoveToTrader(Trader trader)
    {
        if (trader == null)
        {
            rb.linearVelocity = Vector2.zero;
            return false;
        }

        Vector2 targetPosition = trader.GetDockingPosition();
        Vector2 direction = targetPosition - rb.position;
        float distance = direction.magnitude;

        if (distance <= arrivalDistance)
        {
            rb.linearVelocity = Vector2.zero;
            return true;
        }

        Vector2 desiredDirection = direction.normalized;
        Vector2 movementDirection = GetAvoidanceDirection(desiredDirection);

        rb.linearVelocity = movementDirection * moveSpeed;
        UpdateShipSprite(movementDirection);

        return false;
    }
    private Trader FindNearestUnknownTrader()
    {
        Trader nearestTrader = null;
        float nearestDistanceSqr = float.MaxValue;

        foreach (Trader trader in islandManager.Traders)
        {
            if (trader == null || trader.data == null) continue;

            bool alreadyKnown = knownTraders.Exists(knownTrader => knownTrader.trader == trader);
            if (alreadyKnown) continue;

            float distanceSqr = (trader.GetDockingPosition() - rb.position).sqrMagnitude;

            if (distanceSqr >= nearestDistanceSqr) continue;

            nearestDistanceSqr = distanceSqr;
            nearestTrader = trader;
        }

        return nearestTrader;
    }
    private Trader FindNearestTrader()
    {
        Trader nearestTrader = null;
        float nearestDistanceSqr = float.MaxValue;

        foreach (Trader trader in islandManager.Traders)
        {
            if (trader == null) continue;

            float distanceSqr = (trader.GetDockingPosition() - rb.position).sqrMagnitude;

            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestTrader = trader;
            }
        }

        return nearestTrader;
    }
    private Vector2 GetAvoidanceDirection(Vector2 desiredDirection)
    {
        if (avoidanceDecisionTimer > 0f)
        {
            avoidanceDecisionTimer -= Time.fixedDeltaTime;
        }
        else
        {
            avoidanceDecisionTimer = 0f;
            avoidanceSide = 0;
        }

        Vector2 rayOrigin = rb.position;

        Vector2 leftDirection =
            RotateDirection(desiredDirection, sideVisionAngle);

        Vector2 rightDirection =
            RotateDirection(desiredDirection, -sideVisionAngle);

        bool forwardBlocked = TryFindObstacle(
            rayOrigin,
            desiredDirection,
            out RaycastHit2D forwardHit
        );

        bool leftBlocked = TryFindObstacle(
            rayOrigin,
            leftDirection,
            out RaycastHit2D leftHit
        );

        bool rightBlocked = TryFindObstacle(
            rayOrigin,
            rightDirection,
            out RaycastHit2D rightHit
        );

        debugRayOrigin = rayOrigin;

        debugForwardDirection = desiredDirection;
        debugLeftDirection = leftDirection;
        debugRightDirection = rightDirection;

        debugForwardBlocked = forwardBlocked;
        debugLeftBlocked = leftBlocked;
        debugRightBlocked = rightBlocked;

        if (!forwardBlocked &&
            !leftBlocked &&
            !rightBlocked)
        {
            avoidanceSide = 0;
            avoidanceDecisionTimer = 0f;

            return desiredDirection;
        }

        Vector2 avoidanceDirection;

        if (!leftBlocked && rightBlocked)
        {
            avoidanceSide = 1;
            avoidanceDecisionTimer = avoidanceDecisionDuration;

            avoidanceDirection = leftDirection;
        }
        else if (!rightBlocked && leftBlocked)
        {
            avoidanceSide = -1;
            avoidanceDecisionTimer = avoidanceDecisionDuration;

            avoidanceDirection = rightDirection;
        }
        else if (!leftBlocked && !rightBlocked)
        {
            if (avoidanceSide == 0)
            {
                // Всегда обходим вправо.
                avoidanceSide = -1;
                avoidanceDecisionTimer = avoidanceDecisionDuration;
            }

            if (avoidanceSide == 1)
            {
                avoidanceDirection = leftDirection;
            }
            else
            {
                avoidanceDirection = rightDirection;
            }
        }
        else
        {
            if (avoidanceSide == 0)
            {
                float leftDistance = leftHit.distance;
                float rightDistance = rightHit.distance;

                if (leftDistance > rightDistance)
                {
                    avoidanceSide = 1;
                }
                else
                {
                    avoidanceSide = -1;
                }

                avoidanceDecisionTimer = avoidanceDecisionDuration;
            }

            if (avoidanceSide == 1)
            {
                avoidanceDirection = leftDirection;
            }
            else
            {
                avoidanceDirection = rightDirection;
            }
        }

        float nearestDistance = visionDistance;

        if (forwardBlocked)
        {
            nearestDistance =
                Mathf.Min(nearestDistance, forwardHit.distance);
        }

        if (leftBlocked)
        {
            nearestDistance =
                Mathf.Min(nearestDistance, leftHit.distance);
        }

        if (rightBlocked)
        {
            nearestDistance =
                Mathf.Min(nearestDistance, rightHit.distance);
        }

        float distancePercent =
            Mathf.Clamp01(nearestDistance / visionDistance);

        float targetStrength =
            targetDirectionStrength * distancePercent;

        Vector2 finalDirection =
            avoidanceDirection +
            desiredDirection * targetStrength;

        return finalDirection.normalized;
    }
    private void UpdateShipSprite(Vector2 direction)
    {
        if (spriteRenderer == null) return;
        if (directionSprites == null || directionSprites.Length < 8) return;
        if (direction.sqrMagnitude < 0.001f) return;

        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;

        if (angle < 0f)
            angle += 360f;

        int index = Mathf.FloorToInt((angle + 22.5f) / 45f) % 8;

        if (directionSprites[index] == null) return;
        spriteRenderer.sprite = directionSprites[index];
    }
    private Vector2 RotateDirection(Vector2 direction, float angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(direction.x * cos - direction.y * sin, direction.x * sin + direction.y * cos );
    }
    private bool TryFindObstacle(Vector2 origin, Vector2 direction, out RaycastHit2D nearestHit)
    {
        RaycastHit2D[] hits = Physics2D.CircleCastAll(
            origin,
            visionRadius,
            direction,
            visionDistance,
            obstacleLayer
        );

        nearestHit = new RaycastHit2D();

        float nearestDistance = float.MaxValue;
        bool foundObstacle = false;

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null)
                continue;

            if (hit.collider.attachedRigidbody == rb)
                continue;

            if (hit.transform == transform)
                continue;

            if (hit.transform.IsChildOf(transform))
                continue;

            if (hit.distance >= nearestDistance)
                continue;

            nearestDistance = hit.distance;
            nearestHit = hit;
            foundObstacle = true;
        }

        return foundObstacle;
    }
    public bool AddItem(ItemType item, int amount)
    {
        if (amount <= 0 || amount > GetFreeCargoSpace()) return false;

        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i].Type.Equals(item))
            {
                inventory[i].Count += amount;
                return true;
            }    
        }
        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i].Type.Equals(ItemType.Empty))
            {
                inventory[i].Type = item;
                inventory[i].Count = amount;
                return true;
            }  
        }
        return false;
    }
    public bool RemoveItem(ItemType item, int amount)
    {
        if(amount <= 0) return false;

        for (int i = 0; i < inventory.Length; i++)
        {
            if (inventory[i].Type.Equals(item))
            {
                if(inventory[i].Count < amount) return false;
                
                inventory[i].Count -=amount;
                if (inventory[i].Count <= 0)
                {
                    inventory[i].Type = ItemType.Empty;
                    inventory[i].Count = 0;
                }
                return true;
            }    
        }
        return false;
    }
    private void OnDrawGizmosSelected() //Debug
    {
        Vector3 origin = debugRayOrigin;

        Gizmos.color = debugForwardBlocked
            ? Color.red
            : Color.green;

        Gizmos.DrawLine(
            origin,
            origin + (Vector3)debugForwardDirection * visionDistance
        );

        Gizmos.DrawWireSphere(
            origin + (Vector3)debugForwardDirection * visionDistance,
            visionRadius
        );

        Gizmos.color = debugLeftBlocked
            ? Color.red
            : Color.yellow;

        Gizmos.DrawLine(
            origin,
            origin + (Vector3)debugLeftDirection * visionDistance
        );

        Gizmos.DrawWireSphere(
            origin + (Vector3)debugLeftDirection * visionDistance,
            visionRadius
        );

        Gizmos.color = debugRightBlocked
            ? Color.red
            : Color.yellow;

        Gizmos.DrawLine(
            origin,
            origin + (Vector3)debugRightDirection * visionDistance
        );

        Gizmos.DrawWireSphere(
            origin + (Vector3)debugRightDirection * visionDistance,
            visionRadius
        );
    }

}

[System.Serializable]
public class TradeOpportunity
{
    public Trader buyTrader;
    public Trader sellTrader;
    public ItemType itemType;
    public float buyPrice;
    public float sellPrice;
    public int quantity;
    public TradeOpportunity(Trader buyTrader, Trader sellTrader, ItemType itemType, float buyPrice, float sellPrice, int quantity = 0)
    {
        this.buyTrader = buyTrader;
        this.sellTrader = sellTrader;
        this.itemType = itemType;
        this.buyPrice = buyPrice;
        this.sellPrice = sellPrice;
        this.quantity = quantity;
    }
}

[System.Serializable]
public class KnownTrader
{
    public Trader trader;
    public List<KnownTradeItem> inventory = new();

    public KnownTradeItem GetItem(ItemType itemType)
    {
        return inventory.Find(item => item.itemType == itemType);
    }
}

[System.Serializable]
public class KnownTradeItem
{
    public ItemType itemType;
    public float price;
    public int stock;
}
