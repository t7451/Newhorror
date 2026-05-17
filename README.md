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

## Linux Unity Hub Installation
Use Unity Hub to install and manage the Unity Editor version listed in
`ProjectSettings/ProjectVersion.txt`. On Linux, install Unity Hub through Unity's
package repositories so regular system updates can keep it current.

### Ubuntu
Install `curl` if it is not already available:

```bash
sudo apt install curl
```

Add the Unity Hub signing key and stable repository (amd64 only):

```bash
sudo install -d /etc/apt/keyrings
curl -fsSL https://hub.unity3d.com/linux/keys/public | sudo gpg --dearmor -o /etc/apt/keyrings/unityhub.gpg
echo "deb [arch=amd64 signed-by=/etc/apt/keyrings/unityhub.gpg] https://hub.unity3d.com/linux/repos/deb stable main" | sudo tee /etc/apt/sources.list.d/unityhub.list
```

Update the package cache and install Unity Hub:

```bash
sudo apt update
sudo apt install unityhub
```

To install the Linux beta channel instead, use the `unstable` distribution in a
separate source list entry:

```bash
sudo install -d /etc/apt/keyrings
curl -fsSL https://hub.unity3d.com/linux/keys/public | sudo gpg --dearmor -o /etc/apt/keyrings/unityhub.gpg
echo "deb [arch=amd64 signed-by=/etc/apt/keyrings/unityhub.gpg] https://hub.unity3d.com/linux/repos/deb unstable main" | sudo tee /etc/apt/sources.list.d/unityhub-beta.list
sudo apt update
sudo apt install unityhub
```

Confirm `/etc/apt/keyrings` exists, the installing user or group can write to it,
and `/etc/apt/keyrings/unityhub.gpg` is readable after creation.

### RHEL or CentOS
Add Unity's RPM signing key and stable repository:

```bash
sudo rpm --import https://hub.unity3d.com/linux/keys/public
sudo tee /etc/yum.repos.d/unityhub.repo >/dev/null <<'EOF'
[unityhub]
name=Unity Hub
baseurl=https://hub.unity3d.com/linux/repos/rpm/stable
enabled=1
gpgcheck=1
gpgkey=https://hub.unity3d.com/linux/keys/public
EOF
```

Install Unity Hub with the package manager available on your distribution:

```bash
sudo dnf install unityhub
# or
sudo yum install unityhub
```

For beta builds, replace `stable` with `unstable` in the repository `baseurl`.

Use the same package manager to update or remove Unity Hub:

```bash
sudo dnf update unityhub
sudo dnf remove unityhub
# or replace dnf with yum on older systems
```

On Ubuntu, remove Unity Hub with:

```bash
sudo apt remove unityhub
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

### Phase 8 — Game UI &amp; Logic
- `Assets/HorrorCoopGame/Scripts/Game/GamePhase.cs` (Lobby / Playing / Victory / Defeat enum)
- `Assets/HorrorCoopGame/Scripts/Game/GameManager.cs` (server-authoritative phase + win/loss detection)
- `Assets/HorrorCoopGame/Scripts/UI/GameHUD.cs` (health/stamina/sanity bars, repair progress, clock, phase banner, panel toggles)
- `Assets/HorrorCoopGame/Scripts/UI/PauseMenuUI.cs` (host start, resume, leave session)

## Game UI &amp; Logic Setup (Phase 8)
1. Spawn a single `GameManager` `NetworkObject` in the gameplay scene (host spawns it; clients receive it via replication).
2. Add a `GameHUD` Canvas (use `ResponsiveCanvasScaler`) with:
   - Three filled `Image`s wired to `healthBarFill`, `staminaBarFill`, `sanityBarFill`.
   - A full-screen black `Image` inside a `CanvasGroup` wired to `sanityVignette`.
   - TMP text fields for `repairProgressText`, `clockText`, `phaseBannerText`, and `endScreenText`.
   - Child `GameObject`s for `inventoryPanel` (hosting `InventoryGridUI`), `pausePanel` (hosting `PauseMenuUI`), and `endScreenPanel`.
3. Add `Inventory` and `Pause` actions to your `InputActions` (e.g. `Tab` and `Escape`) and bind them to `GameHUD.OnInventory` / `GameHUD.OnPause` via `PlayerInput` send-messages, or call `ToggleInventory()` / `TogglePause()` from on-screen buttons for mobile.
4. The host clicks **Start Game** on `PauseMenuUI` to transition the round from `Lobby` to `Playing`. Victory triggers when `VehicleRepair.IsRepaired` becomes true; defeat triggers when every connected player has 0 health (after a short grace period).

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
