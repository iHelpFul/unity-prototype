using UnityEngine;

public class PlayerAppearanceController : MonoBehaviour
{
    [SerializeField] private CharacterAppearanceVisualController visuals;
    [SerializeField] private GameBootstrap bootstrap;
    private CharacterAppearanceData runtimeAppearanceOverride;

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
        bootstrap = GameBootstrap.FindReadyBootstrap(bootstrap);
        return PlayerRuntimeIdentityUtility.MatchesTrackedCharacter(
            bootstrap,
            null,
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

        bootstrap = GameBootstrap.FindReadyBootstrap(bootstrap);
        CharacterAppearanceData appearance = bootstrap != null
            ? bootstrap.GetResolvedActiveCharacterAppearance()
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
