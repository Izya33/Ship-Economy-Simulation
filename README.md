# Ship Guy — 2D Trading & Economy Simulation

A 2D trading and economic simulation game developed with **Unity and C#**.

The player takes control of a trading ship, travels between islands, buys and sells goods, completes delivery quests, and upgrades their vessel.

The game features a **dynamic economy**, autonomous NPC merchant ships, and a modular architecture designed to support additional islands, goods, and gameplay mechanics.

Developed as a university programming project at Hochschule Mittweida.

## Features

- **Dynamic Economy** — Goods are produced and consumed over time, with prices changing according to available stock.
- **Trading System** — Buy and sell goods at different islands, each with its own inventory, prices, and trading limits.
- **Autonomous NPC Merchants** — AI-controlled ships independently navigate between islands, select trading routes, and buy or sell goods.
- **Limited NPC Knowledge** — Merchant ships remember prices and stock levels from previously visited islands instead of having complete information about the economy.
- **Inventory Management** — Cargo storage with different item types and quantities.
- **Delivery Quests** — Transport requested goods between islands to earn money and experience.
- **Ship Upgrades** — Improve movement speed and cargo capacity.
- **Level and Experience System** — Progress through trading and completed quests.
- **Ship Movement** — Physics-based ship controls using Unity's Rigidbody2D.
- **Custom Sprites** — Hand-drawn 2D graphics created for the project.

## Technologies

- **Game Engine:** Unity
- **Programming Language:** C#
- **Physics:** Unity Rigidbody2D
- **Game Data:** ScriptableObjects
- **Architecture:** Object-oriented, modular game systems
- **Graphics:** Custom 2D sprites

## Game Mechanics

### Dynamic Economy

Each island trader has its own inventory, available money, production and consumption rates, and individual pricing.

An in-game day system periodically updates the economy:

1. Traders produce certain goods.
2. Other goods are consumed.
3. Available stock levels change.
4. Trading prices are recalculated.

This creates changing economic conditions and opportunities for profitable trading between islands.

### Autonomous Merchant AI

NPC merchant ships operate independently of the player.

Each merchant:

- Navigates between trading islands.
- Avoids obstacles while sailing.
- Maintains its own inventory and money.
- Remembers previously visited traders and their last known prices.
- Evaluates potential trading opportunities.
- Selects profitable routes based on available information.

When carrying goods, the NPC looks for a suitable destination to sell them. Otherwise, it searches for profitable opportunities to purchase goods.

If no suitable route is available or the merchant lacks sufficient funds, it can temporarily work to earn money before resuming trading.

### Trading System

The `TradeService` class provides shared buying and selling logic for both the player and NPC merchants.

Before completing a transaction, the system checks:

- Available money
- Inventory capacity
- Available goods
- Trader buying and selling limits

This allows different entities to use the same trading rules.

### Quests and Progression

Players can accept delivery quests requiring specific goods to be transported to designated islands.

Successfully completing a quest rewards the player with money and experience.

The game also includes ship upgrades that improve speed and cargo capacity, allowing the player to expand their trading activities.

## Software Architecture

The project is divided into several subsystems to keep gameplay mechanics organized and extensible.

| Subsystem | Main Responsibilities |
|---|---|
| Core | Camera tracking, in-game time, island management |
| Entities | Player ship, NPC merchant ships, progression |
| Items & Inventory | Item definitions, inventory slots, cargo management |
| Trading | Trader data, buying and selling, island interactions |
| Quests | Quest definitions, objectives, rewards, progression |
| UI | Trading interface, inventory, quests, upgrades |

### Selected Classes

- `DayManager` — Controls the in-game day cycle and economy updates.
- `NpcMerchantShip` — Handles autonomous trading decisions and navigation.
- `TradeService` — Centralizes buying and selling operations.
- `TraderData` — Stores trader-specific economic information.
- `QuestManager` — Manages delivery quests and rewards.
- `UIManager` — Coordinates the game's menus and interface updates.
- `CameraFollow` — Tracks the player ship.

Interfaces such as `ITradeAgent` allow the player and NPC merchants to share common trading functionality.

## Controls

| Key | Action |
|---|---|
| W | Increase thrust on both engines |
| Q | Increase left-engine thrust |
| E | Increase right-engine thrust |
| A / D | Turn the ship |
| Shift | Dash |
| F | Interact with a trader / open or close the trading menu |
| T | Open active quests |
| Mouse Wheel | Zoom in or out while the ship is stationary |
| Esc | Return to the main menu |

## Getting Started

### Requirements

- Unity Hub
- The Unity Editor version specified in `ProjectSettings/ProjectVersion.txt`

### Running the Project

1. Clone or download the repository.
2. Open Unity Hub.
3. Select **Add project from disk** and choose the folder containing `Assets`, `Packages`, and `ProjectSettings`.
4. Install the required Unity Editor version if necessary.
5. Open the project in Unity.
6. Open the game's main scene and press **Play**.

## Future Improvements

Potential extensions include:

- Additional islands and trading goods
- More delivery quests
- More advanced NPC behavior
- Pirates and combat mechanics
- Additional animations and visual improvements
