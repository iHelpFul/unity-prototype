using UnityEngine;

public class EnemyBreakController : MonoBehaviour
{
    [Header("Break Tuning")]
    [SerializeField] private float baseBreakDuration = 0.35f;
    [SerializeField] private float extraDurationPerBreakPower = 0.2f;
    [SerializeField] private float minimumBreakDuration = 0.15f;
    [SerializeField] private float poiseThreshold;
    [SerializeField] private float pressureDecayDelay = 0.9f;
    [SerializeField] private float pressureDecayPerSecond = 0.85f;
    [SerializeField] private float pressureTakenMultiplier = 1f;

    private EnemyHealth enemyHealth;
    private EnemyAI enemyAI;
    private EnemyAnimationController animationController;
    private float breakTimer;
    private float currentPressure;
    private float pressureDecayLockTimer;

    public bool IsBroken => breakTimer > 0f;
    public float RemainingBreakDuration => Mathf.Max(0f, breakTimer);
    public float CurrentPressure => Mathf.Max(0f, currentPressure);
    public float PressureThreshold => Mathf.Max(0f, poiseThreshold);
    public float PressureNormalized => PressureThreshold > 0.01f
        ? Mathf.Clamp01(CurrentPressure / PressureThreshold)
        : 0f;

    public void Initialize(
        EnemyHealth ownerHealth,
        EnemyAI ownerAI,
        EnemyAnimationController ownerAnimationController)
    {
        enemyHealth = ownerHealth;
        enemyAI = ownerAI;
        animationController = ownerAnimationController;
        RefreshConfiguredStats();
    }

    private void Awake()
    {
        enemyHealth ??= GetComponent<EnemyHealth>();
        enemyAI ??= GetComponent<EnemyAI>();
        animationController ??= GetComponent<EnemyAnimationController>();
        Sanitize();
        RefreshConfiguredStats();
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime <= 0f)
            return;

        if (breakTimer > 0f)
        {
            breakTimer = Mathf.Max(0f, breakTimer - deltaTime);
            if (breakTimer <= 0f)
                PublishBreakStateChanged(false, 0f);
            return;
        }

        if (currentPressure <= 0f || PressureThreshold <= 0.01f)
            return;

        if (pressureDecayLockTimer > 0f)
        {
            pressureDecayLockTimer = Mathf.Max(0f, pressureDecayLockTimer - deltaTime);
            return;
        }

        float previousPressure = currentPressure;
        currentPressure = Mathf.Max(0f, currentPressure - pressureDecayPerSecond * deltaTime);
        if (!Mathf.Approximately(previousPressure, currentPressure))
            PublishPressureChanged();
    }

    public bool ApplyBreakFromPower(float breakPower, PlayerCharacter sourceCharacter = null)
    {
        if (enemyHealth == null || enemyHealth.IsDead)
            return false;

        float resolvedBreakPower = Mathf.Max(0f, breakPower) * Mathf.Max(0.01f, pressureTakenMultiplier);
        if (resolvedBreakPower <= 0f)
            return false;

        if (PressureThreshold <= 0.01f)
        {
            float immediateDuration = Mathf.Max(
                minimumBreakDuration,
                baseBreakDuration + resolvedBreakPower * extraDurationPerBreakPower);
            return ApplyBreak(immediateDuration, sourceCharacter);
        }

        currentPressure += resolvedBreakPower;
        pressureDecayLockTimer = Mathf.Max(0f, pressureDecayDelay);
        PublishPressureChanged();

        if (currentPressure < PressureThreshold)
            return false;

        float resolvedDuration = Mathf.Max(
            minimumBreakDuration,
            baseBreakDuration + resolvedBreakPower * extraDurationPerBreakPower);
        currentPressure = 0f;
        pressureDecayLockTimer = 0f;
        PublishPressureChanged();
        return ApplyBreak(resolvedDuration, sourceCharacter);
    }

    public bool ApplyBreak(float duration, PlayerCharacter sourceCharacter = null)
    {
        if (enemyHealth == null || enemyHealth.IsDead)
            return false;

        float resolvedDuration = Mathf.Max(minimumBreakDuration, duration);
        breakTimer = Mathf.Max(breakTimer, resolvedDuration);
        enemyAI?.NotifyHitReactionLock(resolvedDuration);
        animationController?.PlayBreak();
        PublishBreakStateChanged(true, breakTimer);
        return true;
    }

    public void ClearBreak()
    {
        breakTimer = 0f;
        currentPressure = 0f;
        pressureDecayLockTimer = 0f;
        PublishPressureChanged();
        PublishBreakStateChanged(false, 0f);
    }

    public void RefreshConfiguredStats()
    {
        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();

        if (enemyHealth == null || enemyHealth.Stats == null)
            return;

        EnemyPoiseProfile profile = enemyHealth.Stats.PoiseProfile;
        poiseThreshold = profile.PoiseThreshold;
        pressureDecayDelay = profile.PressureDecayDelay;
        pressureDecayPerSecond = profile.PressureDecayPerSecond;
        baseBreakDuration = profile.BaseBreakDuration;
        extraDurationPerBreakPower = profile.ExtraDurationPerBreakPower;
        minimumBreakDuration = profile.MinimumBreakDuration;
        pressureTakenMultiplier = profile.PressureTakenMultiplier;
        Sanitize();
    }

    private void OnValidate()
    {
        Sanitize();
    }

    private void Sanitize()
    {
        baseBreakDuration = Mathf.Max(0f, baseBreakDuration);
        extraDurationPerBreakPower = Mathf.Max(0f, extraDurationPerBreakPower);
        minimumBreakDuration = Mathf.Max(0.01f, minimumBreakDuration);
        poiseThreshold = Mathf.Max(0f, poiseThreshold);
        pressureDecayDelay = Mathf.Max(0f, pressureDecayDelay);
        pressureDecayPerSecond = Mathf.Max(0f, pressureDecayPerSecond);
        pressureTakenMultiplier = Mathf.Max(0.01f, pressureTakenMultiplier);
    }

    private void PublishPressureChanged()
    {
        if (enemyHealth == null)
            return;

        EventBus.Publish(new EnemyPressureChangedEvent
        {
            Enemy = enemyHealth,
            CurrentPressure = CurrentPressure,
            MaxPressure = PressureThreshold,
            IsBroken = IsBroken
        });
    }

    private void PublishBreakStateChanged(bool isBroken, float remainingDuration)
    {
        if (enemyHealth == null)
            return;

        EventBus.Publish(new EnemyBreakStateChangedEvent
        {
            Enemy = enemyHealth,
            IsBroken = isBroken,
            RemainingDuration = Mathf.Max(0f, remainingDuration)
        });
    }
}
