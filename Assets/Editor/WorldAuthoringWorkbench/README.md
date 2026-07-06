# World Authoring Workbench

Portable Unity editor tools for arranging scenes and building test spaces quickly.

Open from:

```text
Tools/World Authoring/Workbench
```

## What It Includes

- **Prefab Library** - browse prefabs by folder, category, and search text.
- **Placement Settings** - parent container, grid snapping, ground alignment, offsets, scale, random yaw, and facing yaw.
- **Scene Health** - non-destructive scene scan for enemies, spawn points, duplicate spawn IDs, and enemy scene IDs.
- **Selection Tools** - snap selection, align to ground, parent selection to a container, and place the selected prefab at every selected object.

## Portable Design

The tool is intentionally self-contained under:

```text
Assets/Editor/WorldAuthoringWorkbench
```

Project-specific scene checks use reflection by component name, so missing components are skipped instead of breaking compilation.

## Explicit Scene Changes

The tool only changes the scene when you press an action button:

- Place prefab
- Create spawn point
- Assign missing enemy scene IDs
- Snap / align / parent current selection

All supported scene changes use Unity Undo where possible.

