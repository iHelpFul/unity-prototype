using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputAdapter : MonoBehaviour
{
    [SerializeField] private PlayerCharacter playerCharacter;
    [SerializeField] private PlayerInputRouter inputRouter;

    private void Awake()
    {
        if (playerCharacter == null)
            playerCharacter = GetComponent<PlayerCharacter>();

        if (playerCharacter == null)
        {
            Debug.LogError("PlayerInputAdapter requires a PlayerCharacter on the same GameObject.");
            enabled = false;
            return;
        }

        if (inputRouter == null)
            inputRouter = GetComponent<PlayerInputRouter>();

        if (inputRouter == null)
            inputRouter = gameObject.AddComponent<PlayerInputRouter>();

        inputRouter.Initialize(playerCharacter);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (!ShouldPublishInput())
            return;

        inputRouter.RouteMove(context.ReadValue<Vector2>());
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (!ShouldPublishInput())
            return;

        if (context.started)
            inputRouter.RoutePressed(PlayerInputActionId.Jump);

        if (context.canceled)
            inputRouter.RouteReleased(PlayerInputActionId.Jump);
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!ShouldPublishInput() || !context.performed)
            return;

        inputRouter.RoutePressed(PlayerInputActionId.BasicAttack);
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!ShouldPublishInput() || !context.started)
            return;

        inputRouter.RoutePressed(PlayerInputActionId.Interact);
    }

    public void OnPrevious(InputAction.CallbackContext context)
    {
        if (!ShouldPublishInput() || !context.performed)
            return;

        inputRouter.RoutePressed(PlayerInputActionId.UseRedPotion);
    }

    public void OnNext(InputAction.CallbackContext context)
    {
        if (!ShouldPublishInput() || !context.performed)
            return;

        inputRouter.RoutePressed(PlayerInputActionId.UseBluePotion);
    }

    public void OnSkillSlot1(InputAction.CallbackContext context) => PublishSkillSlot(context, 1);
    public void OnSkillSlot2(InputAction.CallbackContext context) => PublishSkillSlot(context, 2);
    public void OnSkillSlot3(InputAction.CallbackContext context) => PublishSkillSlot(context, 3);
    public void OnSkillSlot4(InputAction.CallbackContext context) => PublishSkillSlot(context, 4);
    public void OnSkillSlot5(InputAction.CallbackContext context) => PublishSkillSlot(context, 5);
    public void OnSkillSlot6(InputAction.CallbackContext context) => PublishSkillSlot(context, 6);
    public void OnSkillSlot7(InputAction.CallbackContext context) => PublishSkillSlot(context, 7);
    public void OnSkillSlot8(InputAction.CallbackContext context) => PublishSkillSlot(context, 8);
    public void OnSkillSlot9(InputAction.CallbackContext context) => PublishSkillSlot(context, 9);
    public void OnSkillSlot10(InputAction.CallbackContext context) => PublishSkillSlot(context, 10);
    public void OnSkillSlot11(InputAction.CallbackContext context) => PublishSkillSlot(context, 11);
    public void OnSkillSlot12(InputAction.CallbackContext context) => PublishSkillSlot(context, 12);
    public void OnSkillSlot13(InputAction.CallbackContext context) => PublishSkillSlot(context, 13);
    public void OnSkillSlot14(InputAction.CallbackContext context) => PublishSkillSlot(context, 14);
    public void OnSkillSlot15(InputAction.CallbackContext context) => PublishSkillSlot(context, 15);

    public void OnInventory(InputAction.CallbackContext context)
    {
        if (!ShouldPublishInput() || !context.performed)
            return;

        inputRouter.RoutePressed(PlayerInputActionId.InventoryToggle);
    }

    public void OnProgressionToggle(InputAction.CallbackContext context)
    {
        if (!ShouldPublishInput() || !context.performed)
            return;

        inputRouter.RoutePressed(PlayerInputActionId.ProgressionToggle);
    }

    public void OnQuestLogToggle(InputAction.CallbackContext context)
    {
        if (!ShouldPublishInput() || !context.performed)
            return;

        inputRouter.RoutePressed(PlayerInputActionId.QuestLogToggle);
    }

    private bool ShouldPublishInput()
    {
        return enabled && playerCharacter != null && playerCharacter.IsLocalPlayer;
    }

    private void PublishSkillSlot(InputAction.CallbackContext context, int slotIndex)
    {
        if (!ShouldPublishInput() || slotIndex <= 0)
            return;

        if (context.started)
            inputRouter.RouteActionBarSlot(slotIndex);

        if (context.canceled)
            inputRouter.RouteActionBarSlotReleased(slotIndex);
    }
}
