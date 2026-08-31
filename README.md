# TotalDeck — Unity3D Project

A card-based RTS game combining deck-building resource management with Total War-style real-time regiment combat.

Converted from the original HTML5 demo (`index.html`) to a full Unity3D (Tuanjie Engine) project.

## Game Overview

- **Core Loop**: Alternating Planning Phase (20s, card play & economy) and Combat Phase (60s, RTS combat)
- **Economy**: Base income ($100/turn) + kill bounty ($2/soldier) - regiment upkeep ($15/turn)
- **Scaling Draw Cost**: Card draw costs $50, $100, $150... resetting each turn
- **Bankruptcy**: Negative treasury triggers soldier desertion
- **Combat**: 50-soldier regiment formations, auto-melee via OverlapSphere

## Project Structure

```
Assets/
├── Scripts/
│   ├── GameEnums.cs          — Enums (GamePhase, CardType, Team) + GameConfig constants
│   ├── CardData.cs           — ScriptableObject for card definitions
│   ├── GameManager.cs        — Singleton: phase timer, economy, bankruptcy logic
│   ├── CardManager.cs        — Singleton: hand management, scaling draw cost, card playing
│   ├── RTSInputController.cs  — Singleton: mouse drag selection, right-click move/attack
│   ├── Regiment.cs           — Macro control: 50-soldier formation, movement, targeting
│   ├── Soldier.cs            — Micro combat: HP, attack, OverlapSphere melee
│   ├── EnemyAI.cs            — Auto-targets nearest player regiment
│   ├── RegimentVisual.cs     — Selection ring, health bar, troop count
│   ├── CameraController.cs   — Overhead RTS camera
│   ├── DragSelectionVisual.cs— On-screen drag box rendering
│   ├── BattlefieldZone.cs    — Deployment zone divider & labels
│   ├── BattleInitializer.cs  — Spawns starting regiments
│   ├── GameBootstrap.cs      — Ensures prefabs exist before battle init
│   ├── CardUIElement.cs      — Individual card UI component
│   ├── GameUI.cs             — Full UI: phase/timer/treasury/hand/draw button
│   ├── SoldierPrefabFactory.cs   — Runtime soldier prefab creation
│   ├── RegimentPrefabFactory.cs  — Runtime regiment prefab creation
│   └── TotalDeckSceneSetup.cs    — Editor tool: auto-creates scene & assets
├── Scenes/
│   └── TotalDeck.unity       — Main game scene (auto-generated)
├── Prefabs/                  — Auto-generated prefabs (Soldier, Regiment, CardUI)
└── ScriptableObjects/        — Auto-generated card assets
```

## Getting Started

### Option A: Auto-Generated Scene (Recommended)

The scene is **automatically created** when you first open the project. The `TotalDeckSceneSetup.cs` script runs on project load via `[InitializeOnLoad]`.

1. Open the project in Tuanjie Hub
2. Wait for script compilation to finish
3. The scene `Assets/Scenes/TotalDeck.unity` is created automatically
4. Double-click the scene to open it
5. Press **Play** to start

### Option B: Manual Scene Setup

If auto-setup didn't trigger:

1. Open the project in Tuanjie Hub
2. Wait for script compilation to finish
3. Menu: **Tools → TotalDeck → Setup Scene**
4. The scene is created at `Assets/Scenes/TotalDeck.unity`
5. Press **Play** to start

## Controls

### Planning Phase
- **Draw Card button** (bottom-left): Draw a card (cost scales: $50→$100→$150)
- **Click a card** in hand to select it
- **Click on your half of the battlefield** to deploy a unit card
- **Click on a friendly regiment** to target it with a spell card
- **Engage! button** (top-right): Skip to combat immediately

### Combat Phase
- **Left-click**: Select a regiment
- **Left-drag**: Box-select multiple regiments
- **Right-click on ground**: Move selected regiments
- **Right-click on enemy**: Charge/lock onto enemy regiment

## Architecture (MVC + Component-based)

### Data Layer
- `CardData` (ScriptableObject): Card definitions stored as `.asset` files

### Manager Layer (Singletons on empty GameObjects)
- `GameManager`: Phase state machine, economy (treasury, bounty, upkeep, draw cost), bankruptcy
- `CardManager`: Hand list, draw card logic, play card (deploy/spell) logic
- `RTSInputController`: Mouse raycasting, drag selection, move/attack commands

### Entity Components
- `Regiment`: Formation grid (10×5), smooth movement, target tracking, `ModifySoldiers()`
- `Soldier`: HP/attack/cooldown, OverlapSphere melee, SetActive(false) on death

### Performance Optimizations
- ❌ No NavMeshAgent on soldiers — pathfinding only on Regiment root
- ❌ No Instantiate/Destroy for soldier deaths — uses SetActive(false)
- ✅ Enemy search: filter by Regiment first, then iterate that regiment's soldiers
- ✅ OverlapSphere for melee range detection

## Economy Formulas

| Component         | Formula                              |
|-------------------|--------------------------------------|
| Base Income       | $100 per planning phase              |
| Kill Bounty      | $2 per enemy soldier killed          |
| Upkeep           | $15 per surviving player regiment    |
| Draw Cost        | $50 + ($50 × draws this turn)        |
| Bankruptcy       | Treasury < 0 → soldiers desert       |
| Desertion        | Each regiment loses |deficit| soldiers |

## Card Types

| Card          | Type  | Cost  | Effect                              |
|---------------|-------|-------|-------------------------------------|
| Infantry      | Unit  | $60   | Deploy 50-man infantry regiment     |
| Field Medic   | Spell | $40   | Heal +15 soldiers to a regiment     |

## Technical Details

- **Engine**: Tuanjie Engine 2022.3.62t14 (Unity-compatible)
- **Render Pipeline**: Built-in (Standard shader)
- **UI**: Unity UI (uGUI) with Canvas
- **Camera**: Orthographic-style overhead perspective

## Original Source

This project is a Unity3D conversion of the HTML5 demo in `index.html`.
The original demo uses Canvas2D for rendering and implements the same game mechanics.
