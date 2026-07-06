# Map_00 Prototype Contract

`Map_00` is the current prototype validation map.

It is not just a level. It is the place where the existing project systems are proven to work together.

## Purpose

`Map_00` currently exists to validate:

- Bootstrap -> character selection -> world entry.
- Player movement in 3D.
- Camera setup.
- Player combat.
- Enemy runtime.
- Loot.
- Inventory/currency/EXP.
- Quest and NPC interaction.
- Shop interaction.
- UI panels and HUD.
- Portal/map transition experiments.

## Current Hierarchy Rule

Until the scene is intentionally refactored:

- Objects under `ENEMIES` are treated as enemy/content test objects.
- Objects under `LEVEL 01` are treated as level/layout content.
- Objects outside `ENEMIES` and outside `LEVEL 01` are treated as required runtime infrastructure.

Do not delete, move, or reorganize required runtime infrastructure casually.

## What Counts As Infrastructure

Likely infrastructure includes:

- `GameBootstrap`.
- Player prefab/scene player.
- Camera/Cinemachine setup.
- EventSystem.
- HUD canvas.
- UI panels.
- Runtime managers.
- Spawn points.
- Portals.
- Loot/runtime prefab links.
- NPC interaction UI.
- Quest/shop UI.

This list should be verified from the scene before cleanup.

## Current Flow

Current expected flow:

1. Start from `Boot_CharacterSelect`.
2. Select or create character.
3. Enter world.
4. Load `Map_00`.
5. Spawn at `from_left`.
6. Use `Map_00` to test the complete prototype gameplay loop.

## What `Map_00` Is Not Yet

`Map_00` is not yet:

- A final world map.
- A narrative/story map.
- A clean production level.
- A final content layout.

It is allowed to be messy while the flow is being stabilized.

## Refactor Rule

Before cleaning `Map_00`, classify each root object or group as one of:

- Required infrastructure.
- Test enemy/content.
- Level layout/content.
- Deprecated/unused.
- Unknown.

Only remove or move objects after they are classified.

