# Quest NPC Flow Builder

Portable editor tool for seeing and editing quest/NPC links.

Open from:

```text
Tools/Quest NPC/Flow Builder
```

## What It Includes

- Browse `NpcQuestDefinition`, `NpcDefinition`, and `NpcVendorDefinition` assets.
- See quest starters, completion NPCs, objectives, rewards, and dialogue page counts.
- See NPC direct quest lists and reverse quest references.
- Explicitly link a selected NPC and quest together.
- Health checks for missing/duplicate quest IDs, NPC IDs, and quests with incomplete flow.

## Safety

The tool does not hide fields or auto-fix assets. Link buttons are explicit and use Unity Undo where possible.

