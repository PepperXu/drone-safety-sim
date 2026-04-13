# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**SafeSpect** — a mixed-reality drone inspection simulator studying how adaptive information visualization affects operator safety awareness and task performance. Published at CHI 2025. Forked from [UAVs-at-Berkeley/UnityDroneSim](https://github.com/UAVs-at-Berkeley/UnityDroneSim).

**Unity Version**: 6.0.0 (6000.4.0f1)  
**Target Platform**: Android — Meta Quest 3 only (OpenXR)  
**Company / Package**: Descartes / com.Descartes.SafeSpect  
**XR Requirement**: Meta Quest 3 with OpenXR loader + Meta Quest Support feature enabled

## Build & Development

This is a Unity project — use the Unity Editor (version 6000.0.40f1) to build and run. There are no CLI build commands in the repository.

- Open the project in Unity Hub with Unity 6000.4.0f1
- Main playable scene: `Assets/Scenes/DroneSim.unity`
- Post-flight analysis: `Assets/Scenes/DataProcessing.unity`
- Note: `ExperimentorMonitor.unity` scene is being removed; do not reference it in new code

For running tests: Unity Test Framework (`com.unity.test-framework`) is included. Run via **Window → General → Test Runner** in the Unity Editor.

## Architecture

### Data Flow

```
XR Input (Controllers)
  → InputControl → DroneManager (sets desired_vx, desired_vy, desired_yaw, desired_height)
  → VelocityControl (PID controller → Rigidbody forces)
  → StateFinder (reads pose) → Communication (global static data bus)

Sensors (read from Communication):
  - PositionalSensorSimulator → GPS signal state + drift
  - CollisionSensing → 16-ray obstacle distances
  - Battery → discharge simulation + RTH threshold

Visualization (reads from Communication):
  - UIUpdater → 2D HUD elements
  - WorldVisUpdater → 3D world-space overlays
  - VisType → active visualization mode (mission/safety/both/2D-only)

Experiment Logging:
  - ExperimentServer (TCP port 8052) → timestamps all state changes
```

### Key Patterns

1. **Static Communication Bus** — `Communication.cs` holds globally accessible structs (`RealPose`, `CollisionData`, `PositionData`, `Battery`, `Wind`). Sensor systems write to it; visualization systems read from it.
2. **Event-Driven State Management** — `DroneManager.cs` is the central state machine. It publishes `UnityEvent`s on state transitions instead of polling in `Update()`. All other systems subscribe to these events.
3. **Physics-Based Control** — Drone movement uses actual `Rigidbody` forces driven by PID, not animations or `Transform` manipulation.
4. **Visualization Abstraction** — `VisType.cs` controls which UI elements are active based on experimental condition, enabling the same scene to run different treatment arms.

### Mission State Machine (DroneManager.cs)

```
Planning → MovingToFlightZone → InFlightZone
  ├─ [autopilot] → Inspecting → [rth or done] → Returning
  └─ [manual rth] → Returning → MovingToFlightZone
```

### Flight State Machine (VelocityControl.cs)

```
Landed → TakingOff → Hovering/Navigating → Landing → Landed
```

## Script Organization (`Assets/Scripts/`)

| Folder | Responsibility |
|--------|---------------|
| `Core/` | `DroneManager` (mission state), `Communication` (global data bus), `StateFinder` (physics → pose) |
| `Controller/` | `VelocityControl` (PID flight), `AutopilotManager` (waypoint following), `ExperimentServer` (TCP logging), wind control |
| `SystemModules/` | `Battery`, `CollisionSensing`, `PositionalSensorSimulator`, `FlightPlanning` (inspection grid) |
| `InputModule/` | XR controller input mapping, ray interaction, event zone detection |
| `Visualization/` | HUD updating, 3D world overlays, control visualizations, adaptive vis mode switching |
| `Utilities/` | Billboard effects, auto-decay, text binding, debug helpers |
| `Editor/` | Custom Unity Inspector UIs for collision geometry, config selection, data visualization |

## Sensor Simulation

- **GPS**: `PositionalSensorSimulator` — signal strength 0 (lost) to 3 (normal), with realistic drift and offset
- **Collision**: 16 raycasts in circular pattern, Warning threshold <3 m, Caution <6 m; auto-disengages autopilot on warning
- **Battery**: Discharge model with dynamic RTH threshold based on remaining flight time and distance
- **Wind**: `RandomPulseNoise` applies random wind forces; `WindZone` prefabs define wind regions
- **Camera Latency**: Frame-buffer system (0–16 frame delay configurable) on FPV feed

## Experiment Infrastructure

- `ExperimentServer.cs` listens on **TCP port 8052** for legacy TCP commands (networking to be removed); currently handles visualization condition switching and logging
- `ConfigManager.cs` switches between multiple inspection scenarios (building configurations)
- Visualization condition is set via `ExperimentServer.UpdateVisCondition(int)`: 0 = 2D Only, 1 = All, 2 = Adaptive
- Known build issue: **OpenXR Android features must be configured** in Unity Editor (Meta Quest Support feature required); `androidApplicationEntry` should be Activity (not GameActivity)

## Key Prefabs

| Prefab | Notes |
|--------|-------|
| `DroneBase.prefab` | Full drone: motors, FPV camera, rigidbody, collision spheres |
| `waypoint.prefab` | Visual marker with states: Hidden / Neutral / Next / NextNext (color-coded) |
| `WorldAnchoredVisualization.prefab` | World-space 3D overlay system |
| `GPSWeakZone.prefab` / `WindZone.prefab` | Trigger volumes that degrade GPS or apply wind |
| `defects/` | 27 building defect prefabs for inspection tasks |

## Safety Thresholds & Constants

- Collision Warning: < 3 m | Caution: < 6 m
- Drone mass: ~1.2 kg, max pitch/roll: 10°
- Inspection grid: 4 horizontal × 12 vertical waypoints
- Traffic spawn interval: ~3 s
- Physics timestep: 0.02 s (Unity default FixedUpdate)
