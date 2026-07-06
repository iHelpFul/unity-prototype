# Balance Board

Portable Unity editor table for fast ScriptableObject tuning.

Open from:

```text
Tools/Combat/Balance Board
```

## What It Includes

- **Enemies** - core stat tuning for `EnemyDefinition` assets.
- **Skills** - fast view/edit for `PlayerSkillDefinition` combat values.
- **Basic Attacks** - flow, gauge, cooldown, and damage tuning.
- **Projectiles** - travel, hit mode, speed, range, lifetime, spawn offsets, and visual rotation.
- **Burst Profiles** - gauge and hold behavior tuning.

## Design

The board discovers ScriptableObject types by name and edits fields through `SerializedObject`.

This keeps it easy to move to another project:

```text
Assets/Editor/BalanceBoard
```

If a type or field does not exist, the board skips it or shows `N/A` instead of hiding inspector fields or generating custom validation rules.

## Safety

Changes are explicit edits to visible table fields. Use Unity Undo / Save Assets as usual.

