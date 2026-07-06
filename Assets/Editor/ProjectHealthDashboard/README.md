# Project Health Dashboard

Broad non-destructive project scanner.

Open from:

```text
Tools/Project Health/Dashboard
```

## What It Scans

- ScriptableObject missing references and duplicate common IDs.
- Prefab missing references, missing scripts, and enemy-like prefabs without `EnemyHealth`.
- Animation clips with empty animation event function names.
- Active scene enemies missing `SceneEntityId`.
- Active scene missing `SceneSpawnPoint`.

## Safety

The dashboard reports issues only. It does not auto-fix project data.

