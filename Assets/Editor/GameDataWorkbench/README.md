# Game Data Workbench

Portable Unity editor tools for browsing, authoring, checking, and testing game data.

Open from:

```text
Tools/Game Data/Workbench
```

You can also right-click an asset in the Project window:

```text
Assets/Open in Game Data Workbench
```

## What It Includes

- **Overview** - quick entry point and project summary.
- **Jobs / Skills / Combat / Enemies / Items / NPC / Characters** - browses known ScriptableObject assets, lets you inspect them, duplicate them, create new ones, and see linked assets.
- **Asset Library** - quick browser for prefabs, VFX, audio clips, textures, and materials.
- **Scenes** - quick browser for scene assets.
- **Sandbox** - small test-scene helper and a place to pin assets while tuning combat.
- **Health** - non-destructive checks for missing references and duplicate common IDs.

## Portable Design

The workbench is intentionally self-contained under:

```text
Assets/Editor/GameDataWorkbench
```

To move it into another Unity project, copy this folder. The tool discovers ScriptableObject types by class name, so missing types are skipped instead of breaking the window.

## Safety

The workbench does not hide inspector fields and does not modify assets automatically.

Actions that change the project are explicit:

- Create asset
- Duplicate asset
- Create basic 3D test setup in the open scene
