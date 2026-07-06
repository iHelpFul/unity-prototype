# System Ownership Map

This document defines where each responsibility should live.

The goal is to avoid duplicated logic, unclear ownership, and "temporary" fields that become permanent.

## Session And Persistence

Owner:

- `GameBootstrap`
- `PlayerSession*Service`
- `SaveSystem`
- `LocalAccountRepository`

Responsibilities:

- Account data.
- Character data.
- Inventory/equipment/skills/passives/action bar state.
- Current map and spawn state.
- Saving and loading.

Rule:

Scene objects can read/apply session state, but session state should not be owned by scene-only objects.

## Character Selection

Owner:

- `CharacterFlowService`
- Character selection UI controllers
- Character appearance data/catalog/resolver

Responsibilities:

- Character slot selection.
- Character creation.
- Character preview.
- Enter-world request.

Rule:

Character selection should not own gameplay combat logic.

## Player Facade

Owner:

- `PlayerCharacter`

Responsibilities:

- Stable public API for the player.
- Runtime ownership state.
- Access to runtime state/action state/combat state.
- Session binding.

Rule:

Other systems should prefer talking to `PlayerCharacter` instead of directly reaching into many player components.

## Player Runtime State

Owner:

- `PlayerRuntimeStateController`
- `PlayerActionStateController`
- `PlayerCombatStateController`

Responsibilities:

- HP/MP/stats/job/progression runtime values.
- Action locks and state transitions.
- Gauge, flow, momentum, empowered basic, ready state.

Rule:

Combat execution should ask these controllers for state. It should not duplicate state timers elsewhere.

## Player Combat Execution

Owner:

- `PlayerCombatController`
- `PlayerBasicAttackRuntimeService`
- `PlayerSkillActionCoordinator`
- `PlayerSkillCastExecutionService`
- `PlayerDirectHitExecutionService`
- `PlayerProjectileExecutionService`
- `PlayerAttackSequenceScheduler`
- `PlayerChargeController`

Responsibilities:

- Basic attack flow.
- Skill tap/hold/burst flow.
- Direct hit and projectile execution.
- Animation-event hit frame handling.
- Sequence packet scheduling.

Rule:

Combat execution should stay runtime-only. Tuning belongs in ScriptableObjects.

## Combat Data

Owner:

- `PlayerSkillDefinition`
- `PlayerBasicAttackProfile`
- `BurstLinkedSkillProfile`
- `UtilitySkillProfile`
- `ProjectileProfile`
- `CombatFormulaProfile`
- `GaugeScalingProfile`
- `PresentationCueSet`
- `CombatElementRuleProfile`

Responsibilities:

- Authoring combat values.
- Skill/projectile/animation/cue configuration.
- Scaling and formula rules.

Rule:

If a value is intended to be tuned per job/skill/enemy, it belongs in a ScriptableObject, not a scene component.

## Enemy Data

Owner:

- `EnemyDefinition`
- `EnemyDefinitionDatabaseAsset`
- `EnemyStats`

Responsibilities:

- Enemy identity, role, stats, element, cadence, poise, drops, lifecycle.

Rule:

Enemy scene/prefab components should reference definitions or resolved stats. They should not become the primary stat source.

## Enemy Runtime

Owner:

- `EnemyHealth`
- `EnemyAI`
- `EnemyTouchDamage`
- `EnemyBreakController`
- `EnemyAnimationController`

Responsibilities:

- Movement/attack behavior.
- Damage receiving.
- Hit reaction threshold and movement lock.
- Break/poise.
- Death/respawn/loot.

Rule:

Enemy runtime should remain readable. Avoid complex AI until the basic loop feels good.

## World Runtime

Owner:

- `WorldRuntimeSceneUtility`
- `MapRegistry`
- `SceneSpawnPoint`
- `PortalTrigger`
- `WorldLootPickup`
- `FieldSpawnDirector`
- `MapTransitionService`

Responsibilities:

- Scene transition.
- Player placement.
- Portal requests.
- World loot pickup.
- Spawn/respawn constraints.

Rule:

Map and spawn IDs are data contracts. Tools should help validate them.

## UI

Owner:

- UI controllers under `Assets/Scripts/UI`

Responsibilities:

- Display state.
- Publish user requests.
- Avoid owning gameplay rules.

Rule:

UI should not calculate core gameplay. It should present state and publish intent.

## Networking

Owner:

- `Assets/Scripts/Networking`

Responsibilities:

- Existing multiplayer prototype support.

Current rule:

Treat networking as parked unless a change directly protects existing data/contracts. Do not expand multiplayer during single-player stabilization.

## Editor Tools

Owner:

- `Assets/Editor/*`

Responsibilities:

- Authoring convenience.
- Health checks.
- Data browsing.
- Scene placement.
- Dry previews.

Rule:

Tools may help find or edit data, but they should not hide runtime fields or silently mutate project state.

