using UnityEngine;

public class PlayerGaugeHudPresenter : MonoBehaviour
{
    [SerializeField] private PlayerGaugeHudView view;
    [SerializeField] private PlayerCharacter trackedPlayer;
    [SerializeField] private bool bindLocalPlayerOnStart = true;
    [SerializeField] private float gaugeSmoothingUnitsPerSecond = 18f;
    [SerializeField] private bool previewLockedGaugeSpendDuringHold = true;

    private int lastFlowStacks = -1;
    private int lastMaxFlowStacks = -1;
    private float lastGaugeNormalized = -1f;
    private float lastGauge = -1f;
    private float lastMaxGauge = -1f;
    private float lastFlowTimerNormalized = -1f;
    private float lastEmpoweredTimerNormalized = -1f;
    private bool lastHasEmpoweredBasicReady;
    private PlayerReadyStateType lastReadyStateType = (PlayerReadyStateType)(-1);
    private float lastReadyStateTimerNormalized = -1f;
    private bool hasDisplayedGauge;
    private float displayedGauge;

    public void BindTrackedPlayer(PlayerCharacter player)
    {
        trackedPlayer = player != null && player.IsLocalPlayer ? player : null;
        ResetCache();
        Refresh(force: true);
    }

    private void Awake()
    {
        if (view == null)
            view = GetComponent<PlayerGaugeHudView>();
    }

    private void Start()
    {
        if (!bindLocalPlayerOnStart || trackedPlayer != null)
            return;

        trackedPlayer = WorldRuntimeSceneUtility.FindLocalPlayer(FindObjectsInactive.Include);
        ResetCache();
        Refresh(force: true);
    }

    private void Update()
    {
        Refresh(force: false);
    }

    private void Refresh(bool force)
    {
        if (view == null || trackedPlayer == null)
            return;

        PlayerBasicAttackProfile basicAttackProfile = trackedPlayer.GetBasicAttackProfile();
        int maxFlowStacks = basicAttackProfile != null ? Mathf.Max(0, basicAttackProfile.MaxFlowStacks) : 0;
        int currentFlowStacks = trackedPlayer.CurrentFlowStacks;
        bool hasEmpoweredBasicReady = trackedPlayer.HasEmpoweredBasicReady;
        PlayerReadyStateType activeReadyStateType = trackedPlayer.ActiveReadyStateType;
        float currentGauge = ResolveTargetGaugeDisplayValue(trackedPlayer);
        float maxGauge = trackedPlayer.MaxGauge;
        if (!hasDisplayedGauge || force)
        {
            displayedGauge = Mathf.Clamp(currentGauge, 0f, Mathf.Max(0f, maxGauge));
            hasDisplayedGauge = true;
        }
        else
        {
            float smoothingSpeed = Mathf.Max(0.01f, gaugeSmoothingUnitsPerSecond);
            displayedGauge = Mathf.MoveTowards(
                displayedGauge,
                Mathf.Clamp(currentGauge, 0f, Mathf.Max(0f, maxGauge)),
                smoothingSpeed * Time.deltaTime);
        }

        float gaugeNormalized = maxGauge > 0f
            ? Mathf.Clamp01(displayedGauge / maxGauge)
            : 0f;
        float flowTimerNormalized = ResolveFlowTimerNormalized(trackedPlayer, basicAttackProfile);
        float empoweredTimerNormalized = ResolveEmpoweredTimerNormalized(trackedPlayer);
        float readyStateTimerNormalized = ResolveReadyStateTimerNormalized(trackedPlayer);

        if (force
            || !Mathf.Approximately(lastGaugeNormalized, gaugeNormalized)
            || !Mathf.Approximately(lastGauge, displayedGauge)
            || !Mathf.Approximately(lastMaxGauge, maxGauge))
        {
            view.SetGauge(gaugeNormalized, displayedGauge, maxGauge);
            lastGaugeNormalized = gaugeNormalized;
            lastGauge = displayedGauge;
            lastMaxGauge = maxGauge;
        }

        if (force
            || lastFlowStacks != currentFlowStacks
            || lastMaxFlowStacks != maxFlowStacks
            || !Mathf.Approximately(lastFlowTimerNormalized, flowTimerNormalized)
            || !Mathf.Approximately(lastEmpoweredTimerNormalized, empoweredTimerNormalized)
            || lastHasEmpoweredBasicReady != hasEmpoweredBasicReady)
        {
            view.SetFlow(
                currentFlowStacks,
                maxFlowStacks,
                flowTimerNormalized,
                hasEmpoweredBasicReady,
                empoweredTimerNormalized);
            lastFlowStacks = currentFlowStacks;
            lastMaxFlowStacks = maxFlowStacks;
            lastFlowTimerNormalized = flowTimerNormalized;
            lastEmpoweredTimerNormalized = empoweredTimerNormalized;
            lastHasEmpoweredBasicReady = hasEmpoweredBasicReady;
        }

        if (force
            || lastReadyStateType != activeReadyStateType
            || !Mathf.Approximately(lastReadyStateTimerNormalized, readyStateTimerNormalized))
        {
            view.SetReadyState(activeReadyStateType, readyStateTimerNormalized);
            lastReadyStateType = activeReadyStateType;
            lastReadyStateTimerNormalized = readyStateTimerNormalized;
        }
    }

    private void ResetCache()
    {
        lastFlowStacks = -1;
        lastMaxFlowStacks = -1;
        lastGaugeNormalized = -1f;
        lastGauge = -1f;
        lastMaxGauge = -1f;
        lastFlowTimerNormalized = -1f;
        lastEmpoweredTimerNormalized = -1f;
        lastHasEmpoweredBasicReady = false;
        lastReadyStateType = (PlayerReadyStateType)(-1);
        lastReadyStateTimerNormalized = -1f;
        hasDisplayedGauge = false;
        displayedGauge = 0f;
    }

    private static float ResolveFlowTimerNormalized(
        PlayerCharacter player,
        PlayerBasicAttackProfile basicAttackProfile)
    {
        if (player == null || basicAttackProfile == null || player.CurrentFlowStacks <= 0)
            return 0f;

        float activeWindowDuration = player.CurrentFlowStacks > 1
            ? Mathf.Max(0.05f, basicAttackProfile.ChainedFlowWindowDuration)
            : Mathf.Max(0.05f, basicAttackProfile.FirstFlowWindowDuration);

        return Mathf.Clamp01(player.CurrentFlowStackTimer / activeWindowDuration);
    }

    private static float ResolveEmpoweredTimerNormalized(PlayerCharacter player)
    {
        if (player == null || !player.HasEmpoweredBasicReady || player.EmpoweredBasicWindowDuration <= 0f)
            return 0f;

        return Mathf.Clamp01(player.EmpoweredBasicTimer / player.EmpoweredBasicWindowDuration);
    }

    private static float ResolveReadyStateTimerNormalized(PlayerCharacter player)
    {
        if (player == null || !player.HasReadyStateActive || player.ReadyStateWindowDuration <= 0f)
            return 0f;

        return Mathf.Clamp01(player.ReadyStateTimer / player.ReadyStateWindowDuration);
    }

    private float ResolveTargetGaugeDisplayValue(PlayerCharacter player)
    {
        if (player == null)
            return 0f;

        float currentGauge = Mathf.Max(0f, player.CurrentGauge);
        if (!previewLockedGaugeSpendDuringHold
            || !player.IsAnyChargeActive
            || player.LockedGaugeSpend <= 0f)
        {
            return currentGauge;
        }

        return Mathf.Max(0f, currentGauge - Mathf.Max(0f, player.LockedGaugeSpend));
    }
}
