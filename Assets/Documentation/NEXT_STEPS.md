# Next Steps

This is the immediate work order for the stabilization phase.

## Step 1 - Lock Prototype Scene Flow

Current decision:

- `Boot_CharacterSelect` is the current intended boot/selection scene.
- `Map_00` is the current prototype validation map.
- `Map_00` is where systems are expected to run together.
- There is no official narrative/story flow yet.
- The priority is a stable prototype gameplay flow, not story content.

`Map_00` scene contract:

- Objects under `ENEMIES` are enemy/content test objects.
- Objects under `LEVEL 01` are level/layout content.
- Objects outside `ENEMIES` and `LEVEL 01` are currently treated as required runtime infrastructure.
- Do not delete or move those infrastructure objects until their responsibility is documented.

Current static findings:

- `Boot_CharacterSelect` currently has `GameBootstrap.defaultCharacterStartMapId = Map_00`.
- `Boot_CharacterSelect` currently has `GameBootstrap.defaultCharacterStartSpawnId = from_left`.
- `Map_00` has a `GameBootstrap` and points to `Map_00`.
- `Map_00_3D` has a `GameBootstrap` and points to `Map_00`.
- `Map_01` has a `GameBootstrap` and points to `Map_01`.
- `Map_01` has a portal target of `Map_02`, but no `Assets/Scenes/Map_02.unity` was found during the static scan.
- `Map_00` and `Map_00_3D` both have portal targets to `Map_01`.

Recommended next action:

Open `Boot_CharacterSelect`, enter Play Mode, and verify:

1. Character select opens.
2. Character can be selected or created.
3. Enter world loads `Map_00`.
4. Player spawns at `from_left`.
5. Required `Map_00` infrastructure is active.

## Step 2 - Run Tool Scans

Use:

- `Tools/Prototype Toolbox`
- `Project Health Dashboard`
- `Game Data Workbench`
- `World Authoring Workbench`

Recommended scan order:

1. Project Health Dashboard full scan.
2. Game Data Workbench health scan.
3. World Authoring scene scan on the official playable scene.
4. Balance Board quick review for enemies and active jobs.

## Step 3 - Lock The Prototype Gameplay Loop

Test this loop manually:

1. Create/select character.
2. Enter world.
3. Move in 3D.
4. Fight slime/turtle.
5. Use basic attack.
6. Use action bar skill.
7. Build gauge from hits and kills.
8. Use burst/tap/hold skill.
9. Trigger ready-state empower.
10. Kill enemy.
11. Pick up loot.
12. Gain EXP/currency/item.
13. Open inventory/progression/quest UI.
14. Interact with NPC.

This is not story flow yet. This is the minimum playable prototype loop that proves the existing systems are connected.

Do not polish numbers yet. First confirm the loop does not break.

## Step 4 - First Cleanup Targets

Likely cleanup targets based on current project shape:

- `Map_00` hierarchy/infrastructure documentation.
- Scene naming and map registry consistency around `Map_00`, `Map_00_3D`, `Map_01`, and missing `Map_02`.
- Stale/deleted helper references from the 3D transition.
- Enemy prefab setup consistency.
- Projectile visual rotation profile consistency.
- Action bar/input binding clarity.
- Quest/NPC data linking consistency.
- Damage number vs actual applied damage clarity.
- UI panel open/close behavior.

## Step 5 - Then Polish

Only after the core loop works:

- Enemy feel.
- Combat timing.
- Gauge pacing.
- Projectile feel.
- UI readability.
- Quest/loot pacing.

## Step 6 - Then Add

After stabilization:

- Building runtime.
- Survival loop.
- Minigames/waves.
- More quest content.
- Skill tree.
- Arcanist completion.
