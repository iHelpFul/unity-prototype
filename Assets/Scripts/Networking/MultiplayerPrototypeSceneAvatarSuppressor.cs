using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-2200)]
public class MultiplayerPrototypeSceneAvatarSuppressor : MonoBehaviour
{
    private void Awake()
    {
        if (!MultiplayerPrototypeRuntime.IsEnabled || TryGetComponent<NetworkObject>(out _))
            return;

        if (TryGetComponent(out PlayerCharacter playerCharacter))
            playerCharacter.SetRuntimeLocalPlayer(false);

        SetComponentEnabled<PlayerFacade>(false);
        SetComponentEnabled<PlayerMovementController>(false);
        SetComponentEnabled<PlayerCombatController>(false);
        SetComponentEnabled<PlayerInteractionController>(false);
        SetComponentEnabled<PlayerInputAdapter>(false);
        SetComponentEnabled<PlayerInput>(false);
        SetComponentEnabled<PlayerAppearanceController>(false);
        SetComponentEnabled<PlayerMotor>(false);

        if (TryGetComponent(out CharacterController controller))
            controller.enabled = false;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int index = 0; index < renderers.Length; index++)
        {
            Renderer renderer = renderers[index];
            if (renderer != null)
                renderer.enabled = false;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int index = 0; index < colliders.Length; index++)
        {
            Collider collider = colliders[index];
            if (collider != null)
                collider.enabled = false;
        }
    }

    private void SetComponentEnabled<T>(bool isEnabled) where T : Behaviour
    {
        T component = GetComponent<T>();
        if (component != null)
            component.enabled = isEnabled;
    }
}
