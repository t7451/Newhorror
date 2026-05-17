# Newhorror

## Prototype Scope Implemented
This repository now includes **Phase 1** and **Phase 2** foundations for a Unity co-op survival horror prototype targeting WebGL + Mobile.

## Folder Structure
```text
Assets/
└── HorrorCoopGame/
    ├── Scenes/
    ├── Scripts/
    │   ├── Networking/
    │   ├── Player/
    │   ├── Interaction/
    │   ├── Building/
    │   └── AI/
    ├── Prefabs/
    ├── ScriptableObjects/
    ├── UI/
    └── TouchControls/
```

## Included Scripts
### Phase 1 — Networking
- `Assets/HorrorCoopGame/Scripts/Networking/NetworkWebSocketSetup.cs`
- `Assets/HorrorCoopGame/Scripts/Networking/RelayManager.cs`
- `Assets/HorrorCoopGame/Scripts/Networking/NetworkMenuUI.cs`
- `Assets/HorrorCoopGame/Scripts/Networking/NetworkStatusUI.cs`

### Phase 2 — Player
- `Assets/HorrorCoopGame/Scripts/Player/PlayerController.cs`
- `Assets/HorrorCoopGame/Scripts/Player/PlayerStats.cs`

### Phase 3 — Inventory & Interaction (object pooled)
- `Assets/HorrorCoopGame/Scripts/Interaction/ItemData.cs` (ScriptableObject)
- `Assets/HorrorCoopGame/Scripts/Interaction/IInteractable.cs`
- `Assets/HorrorCoopGame/Scripts/Interaction/NetworkedPoolManager.cs`
- `Assets/HorrorCoopGame/Scripts/Interaction/ScrapPile.cs`
- `Assets/HorrorCoopGame/Scripts/Interaction/InventorySystem.cs`
- `Assets/HorrorCoopGame/Scripts/Interaction/PlayerInteractionRaycast.cs`
- `Assets/HorrorCoopGame/Scripts/Interaction/InventoryGridUI.cs`

### Phase 4 — Scrap-metal Building
- `Assets/HorrorCoopGame/Scripts/Building/BuildableData.cs` (ScriptableObject)
- `Assets/HorrorCoopGame/Scripts/Building/BuildingManager.cs` (snap-to-grid ghost + ServerRpc place)
- `Assets/HorrorCoopGame/Scripts/Building/StructureHealth.cs`

### Phase 5 — Escape Mechanic
- `Assets/HorrorCoopGame/Scripts/Vehicle/VehicleRepair.cs` (CarBattery + Alternator + SparkPlugs)

### Phase 6 — AI & Sanity
- `Assets/HorrorCoopGame/Scripts/AI/EnemyAI.cs` (state machine + throttled NavMesh updates)
- `Assets/HorrorCoopGame/Scripts/AI/SanityDrain.cs` (darkness drain + audio hallucinations)

### Phase 7 — Polish
- `Assets/HorrorCoopGame/Scripts/Environment/DayNightCycle.cs` (baked-lighting friendly, throttled+lerped lighting samples)
- `Assets/HorrorCoopGame/Scripts/Environment/PerformantFlashlight.cs` (shadowless spotlight)
- `Assets/HorrorCoopGame/Scripts/Environment/PerformanceBootstrap.cs` (runtime quality/FPS auto-tuner, mobile-first WebGL-aware)
- `Assets/HorrorCoopGame/Scripts/UI/ResponsiveCanvasScaler.cs` (1920x1080 reference, adaptive match)

## Browser / Mobile Performance & Smoothing
The prototype is tuned for WebGL in a browser with mobile as the priority target.

### PerformanceBootstrap
Add `PerformanceBootstrap` to a GameObject in your first-loaded scene. It auto-detects mobile (including mobile browsers via `SystemInfo.deviceType == Handheld`) and applies:
- `Application.targetFrameRate` (60 desktop / 60 mobile by default; configurable)
- `QualitySettings.vSyncCount = 0` so the target FPS actually takes effect (WebGL relies on browser rAF)
- `Screen.sleepTimeout = NeverSleep` so phones don't lock mid-session
- `Time.maximumDeltaTime` cap so a long browser frame doesn't snowball into a physics death-spiral
- Mobile: shadows disabled, low shadow resolution + distance, reduced pixel-light count, reduced LOD bias, mipmap streaming bias, no anisotropy/MSAA, no realtime reflection probes, cheaper fixed timestep

### Gameplay Smoothing
- `PlayerController` low-pass filters move + look input and exponentially smooths camera pitch/yaw. Touch-stick jitter and one-off WebGL frame spikes no longer translate directly into camera/character snapping. Smooth times are per-platform inspector fields.
- `DayNightCycle` evaluates lighting gradients on a throttled interval and smoothly lerps the displayed sun/ambient color and sun rotation between samples — cheap on mobile, visually smoother than per-frame evaluation.
- `EnemyAI` adaptively throttles destination updates (slower when no players are in detection range), uses `sqrMagnitude` to avoid per-tick `sqrt`, and caches its transform.
- `PlayerInteractionRaycast` throttles physics raycasts/overlap queries (~12 Hz default) while still refreshing instantly on interact press, so prompts stay responsive without per-frame physics queries.

## On-Screen Touch Setup (Phase 2)
1. Install packages:
   - **Input System**
   - **Netcode for GameObjects**
   - **Relay** + **Lobby** (services)
   - **Input System On-Screen Controls**
2. Create `InputActions` with actions: `Move (Vector2)`, `Look (Vector2)`, `Sprint`, `Jump`, `Interact`, `BuildMode`.
3. Add `PlayerInput` to the player prefab and set behavior to **Invoke Unity Events** or **Send Messages** matching methods in `PlayerController`.
4. Create a `TouchControlsCanvas` (Canvas Scaler: **Scale With Screen Size**, Reference Resolution **1920x1080**).
5. Add controls under `TouchControlsCanvas`:
   - Left: `On-Screen Stick` bound to `Move`
   - Right: drag zone (`OnScreenStick` or custom delta binding) bound to `Look`
   - Buttons: `On-Screen Button` for `Jump`, `Interact`, `BuildMode`, and optional `Sprint`
6. Assign the `TouchControlsCanvas` reference on `PlayerController`.
7. At runtime, `PlayerController` auto-enables touch UI only on mobile (`Application.isMobilePlatform`) and keeps desktop/WebGL keyboard+mouse flow active.

## Networking Setup Notes (Phase 1)
1. Create a `NetworkManager` prefab with `UnityTransport`.
2. Add `NetworkWebSocketSetup` on the same object to force `UnityTransport.UseWebSockets = true` (required for WebGL browser clients).
3. Add `RelayManager` to a bootstrap scene object and wire `NetworkManager` reference.
4. Build menu UI and attach `NetworkMenuUI` with Host/Join buttons, join code input, status label, and optional join-code/player-count/disconnect fields.
5. Add `NetworkStatusUI` to an in-game HUD if you want connection status, relay join code, player count, and disconnect controls visible after the menu closes.
