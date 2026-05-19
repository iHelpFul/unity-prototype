using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerProgressionPanelController : MonoBehaviour
{
    [Header("Runtime Context")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private GameBootstrap bootstrap;
    [SerializeField] private PlayerCharacter trackedPlayer;

    [Header("Summary")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI unspentPointsText;
    [SerializeField] private TextMeshProUGUI mightStatText;
    [SerializeField] private TextMeshProUGUI precisionStatText;
    [SerializeField] private TextMeshProUGUI arcaneStatText;
    [SerializeField] private TextMeshProUGUI finesseStatText;
    [SerializeField] private TextMeshProUGUI hitRateStatText;
    [SerializeField] private TextMeshProUGUI attackRangeText;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Allocate")]
    [SerializeField] private Button addMightButton;
    [SerializeField] private Button addPrecisionButton;
    [SerializeField] private Button addArcaneButton;
    [SerializeField] private Button addFinesseButton;
    [SerializeField] private Button addHitRateButton;

    private const int PointsPerClick = 1;

    public void BindRuntimeContext(GameBootstrap sessionBootstrap, PlayerCharacter player)
    {
        bootstrap = sessionBootstrap;

        if (player != null && player.IsLocalPlayer)
            trackedPlayer = player;

        RefreshView();
    }

    private void Start()
    {
        RegisterButton(addMightButton, () => TrySpendStatPoint(PlayerProgressionStatType.Might));
        RegisterButton(addPrecisionButton, () => TrySpendStatPoint(PlayerProgressionStatType.Precision));
        RegisterButton(addArcaneButton, () => TrySpendStatPoint(PlayerProgressionStatType.Arcane));
        RegisterButton(addFinesseButton, () => TrySpendStatPoint(PlayerProgressionStatType.Finesse));
        RegisterButton(addHitRateButton, () => TrySpendStatPoint(PlayerProgressionStatType.HitRate));

        RefreshView();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerExpChangedEvent>(OnExpChanged);
        EventBus.Subscribe<PlayerLevelUpEvent>(OnLevelChanged);
        EventBus.Subscribe<PlayerEquipmentChangedEvent>(OnEquipmentChanged);
        EventBus.Subscribe<PlayerJobStateChangedEvent>(OnJobChanged);
        EventBus.Subscribe<ProgressionTogglePressedEvent>(OnProgressionTogglePressed);
        EventBus.Subscribe<MapTransitionCompletedEvent>(OnMapTransitionCompleted);
        RefreshView();
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerExpChangedEvent>(OnExpChanged);
        EventBus.Unsubscribe<PlayerLevelUpEvent>(OnLevelChanged);
        EventBus.Unsubscribe<PlayerEquipmentChangedEvent>(OnEquipmentChanged);
        EventBus.Unsubscribe<PlayerJobStateChangedEvent>(OnJobChanged);
        EventBus.Unsubscribe<ProgressionTogglePressedEvent>(OnProgressionTogglePressed);
        EventBus.Unsubscribe<MapTransitionCompletedEvent>(OnMapTransitionCompleted);
    }

    public void ShowPanel()
    {
        SetPanelVisible(true);
    }

    public void HidePanel()
    {
        SetPanelVisible(false);
    }

    public void TogglePanel()
    {
        bool currentlyVisible = panelRoot != null ? panelRoot.activeSelf : gameObject.activeSelf;
        SetPanelVisible(!currentlyVisible);
    }

    private void RegisterButton(Button button, Action action)
    {
        if (button == null || action == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action.Invoke);
    }

    private void OnExpChanged(PlayerExpChangedEvent e)
    {
        if (!IsTrackedPlayer(e.Target, e.CharacterId))
            return;

        RefreshView();
    }

    private void OnLevelChanged(PlayerLevelUpEvent e)
    {
        if (!IsTrackedPlayer(e.Target, e.CharacterId))
            return;

        RefreshView();
    }

    private void OnEquipmentChanged(PlayerEquipmentChangedEvent e)
    {
        if (!IsTrackedPlayer(e.Target, e.CharacterId))
            return;

        RefreshView();
    }

    private void OnJobChanged(PlayerJobStateChangedEvent e)
    {
        if (!IsTrackedPlayer(e.Target, e.CharacterId))
            return;

        RefreshView();
    }

    private void OnProgressionTogglePressed(ProgressionTogglePressedEvent e)
    {
        if (!IsTrackedPlayer(e.Player, e.CharacterId))
            return;

        TogglePanel();
    }

    private void OnMapTransitionCompleted(MapTransitionCompletedEvent e)
    {
        if (e.Player != null && e.Player.IsLocalPlayer)
            trackedPlayer = e.Player;
        else if (!IsTrackedPlayer(e.Player, e.CharacterId))
            return;

        RefreshView();
    }

    private void SetPanelVisible(bool isVisible)
    {
        if (panelRoot != null)
            panelRoot.SetActive(isVisible);
        else
            gameObject.SetActive(isVisible);

        if (isVisible)
            RefreshView();
    }

    private void TrySpendStatPoint(PlayerProgressionStatType statType)
    {
        if (trackedPlayer == null || PointsPerClick <= 0)
            return;

        bool spent = trackedPlayer.TrySpendStatPoints(statType, PointsPerClick);
        if (!spent)
        {
            SetStatus("Not enough unspent stat points.");
            return;
        }

        SetStatus(string.Empty);
        RefreshView();
    }

    private void RefreshView()
    {
        PlayerSessionCharacterApplicationService characterSession = bootstrap != null ? bootstrap.CharacterSession : null;
        PlayerSessionEquipmentApplicationService equipmentSession = bootstrap != null ? bootstrap.EquipmentSession : null;
        PlayerSessionSkillApplicationService skillSession = bootstrap != null ? bootstrap.SkillSession : null;

        if (characterSession == null || trackedPlayer == null || characterSession.PlayerData == null)
        {
            SetStatus("No character data available.");
            return;
        }

        PlayerRuntimeData data = characterSession.PlayerData;
        PlayerCombatSnapshot snapshot = trackedPlayer.GetCombatSnapshot();

        if (snapshot.CurrentJob == default)
            snapshot.CurrentJob = data.CurrentJob;

        ItemStatModifierData equipmentBonuses = equipmentSession != null
            ? equipmentSession.GetEquipmentStatBonuses()
            : new ItemStatModifierData();

        int baseMight = Mathf.Max(0, data.Might);
        int basePrecision = Mathf.Max(0, data.Precision);
        int baseArcane = Mathf.Max(0, data.Arcane);
        int baseFinesse = Mathf.Max(0, data.Finesse);
        int baseHitRate = Mathf.Max(0, data.HitRate);

        if (levelText != null)
            levelText.text = $"Lv. {data.Level}";

        if (unspentPointsText != null)
            unspentPointsText.text = $"Unspent Points: {data.UnspentStatPoints}";

        if (mightStatText != null)
            mightStatText.text = FormatStatLine("Might", baseMight, equipmentBonuses.Might);

        if (precisionStatText != null)
            precisionStatText.text = FormatStatLine("Precision", basePrecision, equipmentBonuses.Precision);

        if (arcaneStatText != null)
            arcaneStatText.text = FormatStatLine("Arcane", baseArcane, equipmentBonuses.Arcane);

        if (finesseStatText != null)
            finesseStatText.text = FormatStatLine("Finesse", baseFinesse, equipmentBonuses.Finesse);

        if (hitRateStatText != null)
            hitRateStatText.text = FormatStatLine("Hit Rate", baseHitRate, equipmentBonuses.HitRate);

        if (attackRangeText != null)
            attackRangeText.text = ResolveAttackRangeText(snapshot, skillSession);

        RefreshAllocateControls(data.UnspentStatPoints);
        SetStatus(string.Empty);
    }

    private void RefreshAllocateControls(int unspentPoints)
    {
        bool canSpend = unspentPoints > 0 && trackedPlayer != null && trackedPlayer.IsLocalPlayer;

        SetButtonState(addMightButton, canSpend);
        SetButtonState(addPrecisionButton, canSpend);
        SetButtonState(addArcaneButton, canSpend);
        SetButtonState(addFinesseButton, canSpend);
        SetButtonState(addHitRateButton, canSpend);
    }

    private void SetButtonState(Button button, bool isInteractable)
    {
        if (button != null)
            button.interactable = isInteractable;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = string.IsNullOrWhiteSpace(message) ? string.Empty : message;
    }

    private bool IsTrackedPlayer(PlayerCharacter player, string characterId)
    {
        return PlayerRuntimeIdentityUtility.MatchesCharacter(
            trackedPlayer,
            trackedPlayer != null ? trackedPlayer.CharacterId : string.Empty,
            player,
            characterId);
    }

    private static string FormatStatLine(string label, int baseValue, int bonus)
    {
        if (bonus == 0)
            return $"{label}: {baseValue}";

        int total = baseValue + bonus;
        return $"{label}: {total} ({baseValue} + {bonus})";
    }

    private string ResolveAttackRangeText(PlayerCombatSnapshot snapshot, PlayerSessionSkillApplicationService skillSession)
    {
        if (snapshot.CurrentJob == PlayerJobType.Shade || snapshot.CurrentJob == PlayerJobType.Arcanist)
        {
            PlayerSkillDefinition sampleSkill = ResolvePrimaryAttackSkill(snapshot.CurrentJob, skillSession);
            if (sampleSkill != null)
            {
                DamageCalculator.DamageRange range = DamageCalculator.CalculateDamageRange(
                    snapshot,
                    isSkillDamage: true,
                    damageMultiplier: sampleSkill.DamageMultiplier);

                return $"{sampleSkill.DisplayName}: {range.MinDamage} - {range.MaxDamage}";
            }
        }

        DamageCalculator.DamageRange basicRange = DamageCalculator.CalculateDamageRange(
            snapshot,
            isSkillDamage: false,
            damageMultiplier: 1f);

        return $"Basic Attack: {basicRange.MinDamage} - {basicRange.MaxDamage}";
    }

    private static PlayerSkillDefinition ResolvePrimaryAttackSkill(
        PlayerJobType jobType,
        PlayerSessionSkillApplicationService skillSession)
    {
        PlayerSkillDefinition bestAssigned = null;

        if (skillSession != null)
        {
            for (int slotIndex = 1; slotIndex <= 15; slotIndex++)
            {
                PlayerSkillDefinition definition = skillSession.GetAssignedSkillDefinition(slotIndex);
                if (definition == null || definition.SkillType != PlayerSkillType.Attack)
                    continue;

                if (bestAssigned == null || definition.DefaultSlotIndex < bestAssigned.DefaultSlotIndex)
                    bestAssigned = definition;
            }
        }

        if (bestAssigned != null)
            return bestAssigned;

        return FindDefaultAttackSkill(jobType);
    }

    private static PlayerSkillDefinition FindDefaultAttackSkill(PlayerJobType jobType)
    {
        PlayerJobDefinition definition = PlayerJobCombatProfiles.GetJobDefinition(jobType);
        if (definition == null)
            return null;

        PlayerSkillDefinition best = null;
        foreach (PlayerSkillDefinition skill in definition.DefaultSkills)
        {
            if (skill == null || skill.SkillType != PlayerSkillType.Attack)
                continue;

            if (best == null || skill.DefaultSlotIndex < best.DefaultSlotIndex)
                best = skill;
        }

        return best;
    }
}
