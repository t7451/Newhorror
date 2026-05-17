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
- `Assets/HorrorCoopGame/Scripts/Networking/NetworkWebSocketSetup.cs`
- `Assets/HorrorCoopGame/Scripts/Networking/RelayManager.cs`
- `Assets/HorrorCoopGame/Scripts/Networking/NetworkMenuUI.cs`
- `Assets/HorrorCoopGame/Scripts/Player/PlayerController.cs`
- `Assets/HorrorCoopGame/Scripts/Player/PlayerStats.cs`

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
4. Build menu UI and attach `NetworkMenuUI` with Host/Join buttons, join code input, and status label.
