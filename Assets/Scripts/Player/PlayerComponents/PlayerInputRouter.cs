using UnityEngine;

public class PlayerInputRouter : MonoBehaviour
{
    private PlayerCharacter playerCharacter;

    public void Initialize(PlayerCharacter ownerCharacter)
    {
        playerCharacter = ownerCharacter;
    }

    public void RouteMove(Vector2 direction)
    {
        if (!ShouldRouteInput())
            return;

        EventBus.Publish(new MoveInputEvent
        {
            Player = playerCharacter,
            CharacterId = ResolveCharacterId(),
            Direction = direction
        });
    }

    public void RoutePressed(PlayerInputActionId actionId)
    {
        if (!ShouldRouteInput())
            return;

        switch (actionId)
        {
            case PlayerInputActionId.Jump:
                EventBus.Publish(new JumpPressedEvent
                {
                    Player = playerCharacter,
                    CharacterId = ResolveCharacterId()
                });
                break;

            case PlayerInputActionId.BasicAttack:
                EventBus.Publish(new AttackPressedEvent
                {
                    Player = playerCharacter,
                    CharacterId = ResolveCharacterId()
                });
                break;

            case PlayerInputActionId.Interact:
                EventBus.Publish(new InteractPressedEvent
                {
                    Player = playerCharacter,
                    CharacterId = ResolveCharacterId()
                });
                break;

            case PlayerInputActionId.UseRedPotion:
                PublishConsumable(PlayerConsumableType.RedPotion);
                break;

            case PlayerInputActionId.UseBluePotion:
                PublishConsumable(PlayerConsumableType.BluePotion);
                break;

            case PlayerInputActionId.InventoryToggle:
                EventBus.Publish(new InventoryTogglePressedEvent
                {
                    Player = playerCharacter,
                    CharacterId = ResolveCharacterId()
                });
                break;

            case PlayerInputActionId.ProgressionToggle:
            case PlayerInputActionId.SkillAndPassiveWindowToggle:
                EventBus.Publish(new ProgressionTogglePressedEvent
                {
                    Player = playerCharacter,
                    CharacterId = ResolveCharacterId()
                });
                break;

            case PlayerInputActionId.QuestLogToggle:
                EventBus.Publish(new QuestLogTogglePressedEvent
                {
                    Player = playerCharacter,
                    CharacterId = ResolveCharacterId()
                });
                break;
        }
    }

    public void RouteReleased(PlayerInputActionId actionId)
    {
        if (!ShouldRouteInput())
            return;

        if (actionId == PlayerInputActionId.Jump)
        {
            EventBus.Publish(new JumpReleasedEvent
            {
                Player = playerCharacter,
                CharacterId = ResolveCharacterId()
            });
            return;
        }

    }

    public void RouteActionBarSlot(int slotIndex)
    {
        if (!ShouldRouteInput() || slotIndex <= 0)
            return;

        PlayerActionBarSlotEntry slot = PlayerInputBindingUtility.GetActionBarSlot(
            playerCharacter.RuntimeData,
            slotIndex);

        EventBus.Publish(new ActionBarSlotPressedEvent
        {
            Player = playerCharacter,
            CharacterId = ResolveCharacterId(),
            SlotIndex = slotIndex,
            AssignmentKind = slot != null ? slot.AssignmentKind : PlayerActionBarAssignmentKind.None,
            AssignedId = slot != null ? slot.AssignedId : string.Empty
        });

        if (slot == null || slot.AssignmentKind == PlayerActionBarAssignmentKind.None)
        {
            PublishSkillSlot(slotIndex);
            return;
        }

        switch (slot.AssignmentKind)
        {
            case PlayerActionBarAssignmentKind.ActiveSkill:
                PublishSkillSlot(slotIndex);
                break;

            case PlayerActionBarAssignmentKind.Consumable:
                PublishConsumable(slot.ConsumableType);
                break;

            case PlayerActionBarAssignmentKind.SystemAction:
                RoutePressed(slot.SystemAction);
                break;
        }
    }

    public void RouteActionBarSlotReleased(int slotIndex)
    {
        if (!ShouldRouteInput() || slotIndex <= 0)
            return;

        PlayerActionBarSlotEntry slot = PlayerInputBindingUtility.GetActionBarSlot(
            playerCharacter.RuntimeData,
            slotIndex);

        EventBus.Publish(new ActionBarSlotReleasedEvent
        {
            Player = playerCharacter,
            CharacterId = ResolveCharacterId(),
            SlotIndex = slotIndex,
            AssignmentKind = slot != null ? slot.AssignmentKind : PlayerActionBarAssignmentKind.None,
            AssignedId = slot != null ? slot.AssignedId : string.Empty
        });

        if (slot == null || slot.AssignmentKind != PlayerActionBarAssignmentKind.ActiveSkill)
            return;

        PublishSkillSlotReleased(slotIndex);
    }

    private void PublishSkillSlot(int slotIndex)
    {
        EventBus.Publish(new SkillSlotPressedEvent
        {
            Player = playerCharacter,
            CharacterId = ResolveCharacterId(),
            SlotIndex = slotIndex
        });
    }

    private void PublishSkillSlotReleased(int slotIndex)
    {
        EventBus.Publish(new SkillSlotReleasedEvent
        {
            Player = playerCharacter,
            CharacterId = ResolveCharacterId(),
            SlotIndex = slotIndex
        });
    }

    private void PublishConsumable(PlayerConsumableType consumableType)
    {
        EventBus.Publish(new UseConsumablePressedEvent
        {
            Player = playerCharacter,
            CharacterId = ResolveCharacterId(),
            ConsumableType = consumableType
        });
    }

    private bool ShouldRouteInput()
    {
        return enabled && playerCharacter != null && playerCharacter.IsLocalPlayer;
    }

    private string ResolveCharacterId()
    {
        return playerCharacter != null
            ? PlayerRuntimeIdentityUtility.NormalizeCharacterId(playerCharacter.CharacterId)
            : string.Empty;
    }
}
