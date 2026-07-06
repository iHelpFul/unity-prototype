# Stabilization Checklist

This checklist defines the work order before adding new major features.

Use this as the project spine for the next phase.

## Phase 0 - Project Opens Cleanly

Goal:

Unity opens, compiles, and all editor tools are usable.

Checklist:

- Unity compiles with no errors.
- `Tools/Prototype Toolbox` opens.
- `Project Health Dashboard` scan can run.
- No missing scripts on core prefabs.
- No missing references on core ScriptableObjects.
- Open stale IDE tabs that point to deleted files are closed.

Done when:

The project can be opened and inspected without immediate repair work.

## Phase 1 - Prototype Launch Flow

Goal:

The current prototype player path is clear and repeatable.

Checklist:

- `Boot_CharacterSelect` is used as the current start scene.
- `Map_00` is treated as the current prototype validation map.
- `Map_00` infrastructure outside `ENEMIES` and `LEVEL 01` is preserved.
- `Map_00_3D` is not promoted over `Map_00` unless explicitly decided later.
- `GameBootstrap` exists in the boot flow.
- `CharacterFlowService` exists or runtime-creates correctly.
- Character selection can create/select a character.
- Enter world resolves map ID through `MapRegistry`.
- Player spawns at a valid `SceneSpawnPoint`.
- There is no requirement for story/narrative flow yet.

Done when:

From boot to `Map_00`, there is one prototype route that works every time.

## Phase 2 - Core 3D Movement And Camera

Goal:

The project feels like a 3D game, not a side-view project patched into 3D.

Checklist:

- Player movement plane is set correctly for `Map_00`.
- Player rotation mode is intentionally selected.
- Camera behavior is configured in the scene, not hidden in temporary helper scripts.
- Enemies move correctly in 3D without jitter.
- Projectiles face/move correctly with their profile settings.
- Ground alignment and collision feel predictable.

Done when:

Running, turning, attacking, and chasing feel stable in `Map_00`.

## Phase 3 - Player Combat Loop

Goal:

Combat has one understandable loop.

Checklist:

- Basic attack works for the active job.
- Basic attack builds gauge at an acceptable pace.
- Enemy death reward adds gauge correctly.
- Action bar slots trigger skills.
- Tap/hold/burst behavior works consistently.
- Ready-state empower appears only when active.
- Ready-state empower consumes on valid hit.
- Utility skill empower works where configured.
- MP cost, cooldown, recovery, hit frame, and animation end behavior are consistent.
- Direct hit and projectile skills both use the same combat contracts.

Done when:

One job can fight enemies for several minutes without state bugs, stuck animations, or confusing combat flow.

## Phase 4 - Enemy Runtime Loop

Goal:

Enemies are readable, fair, and support combat.

Checklist:

- Enemy stats come from `EnemyDefinition`.
- Enemy HP and poise UI appear and disappear correctly.
- Hit reaction animation only plays past threshold.
- Hit reaction movement lock only happens past threshold.
- Knockback behavior is either intentionally enabled or disabled per enemy.
- Sentinel/skirmisher/etc. roles behave predictably.
- Enemy attack animation does not slide/move in a broken way unless intended.
- Death, loot, gauge reward, EXP reward, and respawn work.
- `SceneEntityId` is valid for enemies in multiplayer-compatible scenes.

Done when:

Slime/turtle style enemies feel readable and not "super enemies".

## Phase 5 - World / Loot / Quest Loop

Goal:

The world has a small but complete RPG loop.

Checklist:

- Loot drops have correct prefabs.
- Manual pickup works.
- Inventory updates.
- Currency updates.
- EXP updates.
- Quest kill/collect progress updates.
- NPC quest offer/progress/completion works.
- Shop interaction works.
- Portal/map transition works.

Done when:

Player can enter world, fight, loot, progress a quest, and return to NPC/shop without data issues.

## Phase 6 - UI And Feedback

Goal:

The game communicates enough without clutter.

Checklist:

- HUD vitals are readable.
- Gauge HUD is readable and not noisy.
- Action bar shows the expected 6 slots.
- Enemy overhead UI is clean.
- Damage numbers match actual HP loss expectations.
- Element feedback is not ugly or over-explained.
- Quest/inventory/progression panels are usable.
- Notifications are useful and not spammy.

Done when:

The player understands what happened without the screen becoming a spreadsheet.

## Phase 7 - Data Tuning Pass

Goal:

Values feel roughly correct before content expansion.

Checklist:

- Use `Balance Board` to review enemies, skills, basics, projectiles, burst profiles.
- Use `Combat Test Runner` for skill/basic vs enemy sanity checks.
- Vanguard and Shade are not balanced perfectly, but are playable.
- Arcanist can remain incomplete if marked as not final.
- Enemy HP/defense/damage/poise are in reasonable ranges.
- Gauge gain from hits and kills feels good.

Done when:

Numbers are good enough for flow testing, not final balance.

## Phase 8 - Content Expansion Gate

Only start adding new major systems after phases 0-7 pass.

Allowed after stabilization:

- Survival/building runtime.
- Minigames/waves/missions.
- More quests.
- More maps.
- More enemies.
- Skill trees.
- Arcanist completion.
- New jobs.

Not allowed before stabilization:

- Major multiplayer expansion.
- New combat resource systems.
- Large AI rewrites.
- New UI frameworks.

## Working Rule

When something feels wrong:

1. Confirm whether it is data, prefab, animation, scene setup, or code.
2. Fix the smallest responsible layer.
3. Update this checklist or the ownership map if the decision changes.
