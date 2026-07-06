# Current Project Flow

This document defines the current official flow for the project before adding new systems.

## Current Direction

The project is now treated as a single-player 3D fantasy RPG adventure prototype with combat, jobs, progression, quests, loot, enemies, and future room for survival/building/minigames.

The immediate goal is not to add more systems or story. The immediate goal is to turn the existing prototype systems into one clear, stable gameplay flow.

There is currently no official narrative flow. `Map_00` is the current prototype validation map: it is where systems are tested together and where the project proves that the game can run.

Important `Map_00` rule:

- Objects under `ENEMIES` are test/content enemies.
- Objects under `LEVEL 01` are level/content layout.
- Objects not under `ENEMIES` and not under `LEVEL 01` should be treated as required runtime infrastructure unless proven otherwise.
- Do not delete or reorganize those infrastructure objects casually.

## Official Runtime Flow

1. Boot / session setup

`GameBootstrap` is the persistent session owner.

It owns:

- Account / character session loading.
- Inventory, equipment, skills, passives, action bar, map state, and currency services.
- Runtime prefab catalog access.
- Binding scene players and runtime services after scene loads.

2. Character selection

`CharacterFlowService` owns character selection flow.

It handles:

- Refreshing character slots.
- Selecting a character.
- Creating a character.
- Entering the world.

The current expected prototype flow is:

`Boot_CharacterSelect` -> select/create character -> enter world -> resolve `Map_00` -> load `Map_00`.

3. World entry

`WorldRuntimeSceneUtility`, `MapRegistry`, and `SceneSpawnPoint` own map placement.

Expected prototype flow:

- Character has `CurrentMapId` and `LastSpawnId`.
- `MapRegistry` resolves map ID to scene name.
- Scene loads.
- Player is moved to matching `SceneSpawnPoint`.
- Bootstrap binds scene runtime context.

Current world target:

- `Map_00` is the main prototype map.
- `Map_00_3D` should be treated as experimental/secondary until explicitly promoted.
- `Map_01` can exist as another map/test map, but it is not the current prototype source of truth.

4. Player runtime

`PlayerCharacter` is the player facade.

It owns stable access to:

- Runtime state.
- Action state.
- Combat state.
- Session binding.
- Combat snapshot.
- Basic attack profile.
- HP / MP / stats / job access.

5. Player combat

`PlayerCombatController` owns runtime combat orchestration.

Expected combat flow:

- Input event arrives from input routing.
- Basic attack or skill request starts an action state.
- Animation event reaches hit frame.
- Hit execution service resolves direct hit or projectile.
- `AttackPayloadBuilder` creates payloads.
- `CombatResolver` resolves hit/damage rules.
- Enemy receives damage via `EnemyHealth`.
- Combat feedback, gauge, flow, ready state, and loot events publish through existing runtime systems.

6. Enemy runtime

`EnemyDefinition` is the enemy data source of truth.

Expected enemy flow:

- Enemy prefab has `EnemyHealth`, `EnemyAI`, animation, and optional touch damage/break components.
- `EnemyStats` resolves values from `EnemyDefinition`.
- `EnemyAI` moves and attacks.
- `EnemyHealth` receives damage, publishes feedback, death, loot, respawn.
- `EnemyBreakController` handles poise/break.
- `EnemyOverheadUiController` shows HP/poise only when relevant.

7. World systems

Expected world loop:

- Enemies die.
- Loot drops through world loot systems and runtime prefab catalog.
- Player picks up loot.
- Inventory, currency, EXP, gauge rewards, and quests react through events/services.
- Portals request map transitions.

8. UI

Current UI should support:

- HUD vitals and gauge.
- Action bar.
- Inventory.
- Progression.
- Quest panel/log.
- Shop panel.
- Enemy overhead HP/poise.
- Character selection/creation.

## Current Jobs

Official job enum:

- Novice
- Vanguard
- Shade
- Arcanist

Current stabilization rule:

Only polish what already exists. Do not expand into more jobs until the shared flow is stable.

## Current Enemy Scope

Enemies should remain readable and not over-complex.

Current enemy focus:

- Role.
- Cadence.
- Poise / break.
- Element profile.
- Elite variants.
- Clean animation behavior.
- Clean death/loot/respawn loop.

## Current Editor Tooling

Use `Tools/Prototype Toolbox` as the entry point.

Primary tools:

- Game Data Workbench
- World Authoring Workbench
- Balance Board
- Quest NPC Flow Builder
- Character Job Preview Lab
- Combat Test Runner
- Building Placement Library
- Project Health Dashboard

## Do Not Add Yet

Until stabilization is done, avoid adding:

- New jobs.
- New large combat mechanics.
- New multiplayer work.
- New complex AI.
- Runtime building/survival systems.
- Large minigame frameworks.

These are allowed only after the stabilization checklist passes.
