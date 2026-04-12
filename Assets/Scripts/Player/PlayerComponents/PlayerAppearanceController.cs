using UnityEngine;

public class PlayerAppearanceController : MonoBehaviour
{
    [SerializeField] private CharacterAppearanceVisualController visuals;
    [SerializeField] private PlayerCharacter character;
    [SerializeField] private GameBootstrap bootstrap;
    private CharacterAppearanceData runtimeAppearanceOverride;

    public void BindBootstrap(GameBootstrap sessionBootstrap)
    {
        bootstrap = sessionBootstrap;
        ApplyActiveCharacterAppearance();
    }

    private void Awake()
    {
        if (character == null)
            character = GetComponent<PlayerCharacter>();

        if (bootstrap != null)
            BindBootstrap(bootstrap);
    }

    private void Start()
    {
        ApplyActiveCharacterAppearance();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<MapTransitionCompletedEvent>(OnMapTransitionCompleted);
        EventBus.Subscribe<PlayerEquipmentChangedEvent>(OnEquipmentChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<MapTransitionCompletedEvent>(OnMapTransitionCompleted);
        EventBus.Unsubscribe<PlayerEquipmentChangedEvent>(OnEquipmentChanged);
    }

    private void OnMapTransitionCompleted(MapTransitionCompletedEvent e)
    {
        if (!IsRelevantLocalEvent(e.Player, e.CharacterId))
            return;

        ApplyActiveCharacterAppearance();
    }

    private void OnEquipmentChanged(PlayerEquipmentChangedEvent e)
    {
        if (!IsRelevantLocalEvent(e.Target, e.CharacterId))
            return;

        ApplyActiveCharacterAppearance();
    }

    private bool IsRelevantLocalEvent(PlayerCharacter player, string characterId)
    {
        return PlayerRuntimeIdentityUtility.MatchesCharacter(
            character,
            character != null ? character.CharacterId : string.Empty,
            player,
            characterId);
    }

    public void ApplyActiveCharacterAppearance()
    {
        if (visuals == null)
            return;

        if (runtimeAppearanceOverride != null)
        {
            visuals.ApplyAppearance(runtimeAppearanceOverride);
            return;
        }

        PlayerSessionEquipmentApplicationService equipmentSession = bootstrap != null ? bootstrap.EquipmentSession : null;
        CharacterAppearanceData appearance = equipmentSession != null
            ? equipmentSession.GetResolvedActiveCharacterAppearance()
            : null;

        if (appearance != null)
            visuals.ApplyAppearance(appearance);
    }

    public void SetRuntimeAppearanceOverride(CharacterAppearanceData appearance)
    {
        runtimeAppearanceOverride = appearance != null ? appearance.Clone() : null;
        ApplyActiveCharacterAppearance();
    }

    public void ClearRuntimeAppearanceOverride()
    {
        runtimeAppearanceOverride = null;
        ApplyActiveCharacterAppearance();
    }
}
