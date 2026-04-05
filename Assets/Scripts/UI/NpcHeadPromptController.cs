using TMPro;
using UnityEngine;

public class NpcHeadPromptController : MonoBehaviour
{
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private RectTransform promptRoot;
    [SerializeField] private TextMeshProUGUI primaryText;
    [SerializeField] private TextMeshProUGUI secondaryText;
    [SerializeField] private Vector2 screenOffset = new Vector2(0f, 40f);

    private NpcInteractable activeNpc;
    private PlayerCharacter activePlayer;
    private string activeCharacterId;

    private void Awake()
    {
        SetPromptVisible(false);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<NpcPromptShownEvent>(OnPromptShown);
        EventBus.Subscribe<NpcPromptHiddenEvent>(OnPromptHidden);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<NpcPromptShownEvent>(OnPromptShown);
        EventBus.Unsubscribe<NpcPromptHiddenEvent>(OnPromptHidden);
    }

    private void LateUpdate()
    {
        PlayerCharacter resolvedPlayer = ResolveActivePlayer();
        if (activeNpc == null || resolvedPlayer == null || !resolvedPlayer.IsLocalPlayer)
        {
            SetPromptVisible(false);
            return;
        }

        activePlayer = resolvedPlayer;

        if (promptRoot == null)
            return;

        Camera renderCamera = ResolveRenderCamera();
        Vector3 worldPosition = activeNpc.GetPromptWorldPosition();

        if (renderCamera != null)
        {
            Vector3 viewportPoint = renderCamera.WorldToViewportPoint(worldPosition);
            if (viewportPoint.z <= 0f)
            {
                SetPromptVisible(false);
                return;
            }
        }

        SetPromptVisible(true);

        RectTransform canvasRect = parentCanvas != null
            ? parentCanvas.transform as RectTransform
            : null;

        if (canvasRect == null)
            return;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(renderCamera, worldPosition);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPoint,
            renderCamera,
            out Vector2 localPoint))
        {
            promptRoot.anchoredPosition = localPoint + screenOffset;
        }
    }

    private void OnPromptShown(NpcPromptShownEvent e)
    {
        if (e.Npc == null || !MatchesLocalPlayer(e.Player, e.CharacterId))
            return;

        activePlayer = ResolveActivePlayer();
        activeCharacterId = ResolveCharacterId(e.Player, e.CharacterId);
        activeNpc = e.Npc;

        if (primaryText != null)
            primaryText.text = e.PrimaryText;

        if (secondaryText != null)
            secondaryText.text = e.SecondaryText;

        SetPromptVisible(true);
    }

    private void OnPromptHidden(NpcPromptHiddenEvent e)
    {
        if (activeNpc == null)
            return;

        if (!PlayerRuntimeIdentityUtility.MatchesCharacter(
                activePlayer,
                activeCharacterId,
                e.Player,
                e.CharacterId))
            return;

        if (!string.IsNullOrWhiteSpace(e.NpcId) && e.NpcId != activeNpc.NpcId)
            return;

        activeNpc = null;
        activePlayer = null;
        activeCharacterId = string.Empty;
        SetPromptVisible(false);
    }

    private PlayerCharacter ResolveActivePlayer()
    {
        if (activePlayer != null && activePlayer.IsLocalPlayer && activePlayer.gameObject.scene.IsValid())
            return activePlayer;

        PlayerCharacter localPlayer = WorldRuntimeSceneUtility.FindLocalPlayer(FindObjectsInactive.Include);
        if (localPlayer == null)
            return activePlayer;

        if (string.IsNullOrWhiteSpace(activeCharacterId)
            || PlayerRuntimeIdentityUtility.MatchesCharacter(
                localPlayer,
                localPlayer.CharacterId,
                activePlayer,
                activeCharacterId))
        {
            activePlayer = localPlayer;
        }

        return activePlayer;
    }

    private bool MatchesLocalPlayer(PlayerCharacter player, string characterId)
    {
        PlayerCharacter localPlayer = WorldRuntimeSceneUtility.FindLocalPlayer(FindObjectsInactive.Include);
        return localPlayer != null
            && PlayerRuntimeIdentityUtility.MatchesCharacter(
                localPlayer,
                localPlayer.CharacterId,
                player,
                characterId);
    }

    private static string ResolveCharacterId(PlayerCharacter player, string characterId)
    {
        if (player != null)
            return PlayerRuntimeIdentityUtility.NormalizeCharacterId(player.CharacterId);

        return PlayerRuntimeIdentityUtility.NormalizeCharacterId(characterId);
    }

    private Camera ResolveRenderCamera()
    {
        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>();

        if (parentCanvas == null)
            return Camera.main;

        if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return parentCanvas.worldCamera != null ? parentCanvas.worldCamera : Camera.main;
    }

    private void SetPromptVisible(bool isVisible)
    {
        if (promptRoot != null)
            promptRoot.gameObject.SetActive(isVisible);
    }
}
