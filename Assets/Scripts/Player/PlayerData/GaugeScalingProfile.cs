using UnityEngine;

[System.Serializable]
public class GaugeScaledFloatParameter
{
    [SerializeField] private bool enabled = true;
    [SerializeField] private float minValue;
    [SerializeField] private float maxValue = 1f;
    [SerializeField] private AnimationCurve normalizedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    public GaugeScaledFloatParameter()
    {
    }

    public GaugeScaledFloatParameter(bool enabled, float minValue, float maxValue)
    {
        this.enabled = enabled;
        this.minValue = minValue;
        this.maxValue = maxValue;
        normalizedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    }

    public bool Enabled => enabled;
    public float MinValue => minValue;
    public float MaxValue => maxValue;
    public AnimationCurve NormalizedCurve => normalizedCurve;

    public float Evaluate(float normalizedSpend)
    {
        if (!enabled)
            return minValue;

        float t = Mathf.Clamp01(normalizedSpend);
        return Mathf.Lerp(minValue, maxValue, normalizedCurve.Evaluate(t));
    }

    public void Sanitize()
    {
        maxValue = Mathf.Max(minValue, maxValue);
        normalizedCurve ??= AnimationCurve.Linear(0f, 0f, 1f, 1f);
    }
}

[System.Serializable]
public class GaugeScaledIntParameter
{
    [SerializeField] private bool enabled;
    [SerializeField] private int minValue = 1;
    [SerializeField] private int maxValue = 1;
    [SerializeField] private AnimationCurve normalizedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    public GaugeScaledIntParameter()
    {
    }

    public GaugeScaledIntParameter(bool enabled, int minValue, int maxValue)
    {
        this.enabled = enabled;
        this.minValue = minValue;
        this.maxValue = maxValue;
        normalizedCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    }

    public bool Enabled => enabled;
    public int MinValue => minValue;
    public int MaxValue => maxValue;
    public AnimationCurve NormalizedCurve => normalizedCurve;

    public int Evaluate(float normalizedSpend)
    {
        if (!enabled)
            return minValue;

        float t = Mathf.Clamp01(normalizedSpend);
        return Mathf.RoundToInt(Mathf.Lerp(minValue, maxValue, normalizedCurve.Evaluate(t)));
    }

    public void Sanitize(int minimumAllowedValue)
    {
        minValue = Mathf.Max(minimumAllowedValue, minValue);
        maxValue = Mathf.Max(minValue, maxValue);
        normalizedCurve ??= AnimationCurve.Linear(0f, 0f, 1f, 1f);
    }
}

[CreateAssetMenu(menuName = "Game Data/Combat/Gauge Scaling Profile")]
public class GaugeScalingProfile : ScriptableObject
{
    [Header("Gauge Spend")]
    [SerializeField] private float minimumGaugeSpend = 1f;
    [SerializeField] private float maximumGaugeSpend = 10f;
    [SerializeField] private AnimationCurve spendByHoldNormalized = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Scaled Outputs")]
    [SerializeField] private GaugeScaledFloatParameter damageMultiplierBySpend =
        new GaugeScaledFloatParameter(true, 1f, 1f);
    [SerializeField] private GaugeScaledIntParameter hitCountBySpend =
        new GaugeScaledIntParameter(false, 1, 1);
    [SerializeField] private GaugeScaledIntParameter maxTargetsBySpend =
        new GaugeScaledIntParameter(false, 1, 1);
    [SerializeField] private GaugeScaledIntParameter pierceCountBySpend =
        new GaugeScaledIntParameter(false, 0, 0);
    [SerializeField] private GaugeScaledIntParameter projectileCountBySpend =
        new GaugeScaledIntParameter(false, 1, 1);
    [SerializeField] private GaugeScaledFloatParameter radiusBySpend =
        new GaugeScaledFloatParameter(false, 1f, 1f);
    [SerializeField] private GaugeScaledFloatParameter rangeBySpend =
        new GaugeScaledFloatParameter(false, 1f, 1f);
    [SerializeField] private GaugeScaledFloatParameter breakPowerBySpend =
        new GaugeScaledFloatParameter(false, 1f, 1f);
    [SerializeField] private GaugeScaledFloatParameter surgeChanceBySpend =
        new GaugeScaledFloatParameter(false, 0f, 0f);
    [SerializeField] private GaugeScaledFloatParameter surgePowerBySpend =
        new GaugeScaledFloatParameter(false, 0f, 0f);

    public float MinimumGaugeSpend => minimumGaugeSpend;
    public float MaximumGaugeSpend => maximumGaugeSpend;
    public AnimationCurve SpendByHoldNormalized => spendByHoldNormalized;
    public GaugeScaledFloatParameter DamageMultiplierBySpend => damageMultiplierBySpend;
    public GaugeScaledIntParameter HitCountBySpend => hitCountBySpend;
    public GaugeScaledIntParameter MaxTargetsBySpend => maxTargetsBySpend;
    public GaugeScaledIntParameter PierceCountBySpend => pierceCountBySpend;
    public GaugeScaledIntParameter ProjectileCountBySpend => projectileCountBySpend;
    public GaugeScaledFloatParameter RadiusBySpend => radiusBySpend;
    public GaugeScaledFloatParameter RangeBySpend => rangeBySpend;
    public GaugeScaledFloatParameter BreakPowerBySpend => breakPowerBySpend;
    public GaugeScaledFloatParameter SurgeChanceBySpend => surgeChanceBySpend;
    public GaugeScaledFloatParameter SurgePowerBySpend => surgePowerBySpend;

    private void OnValidate()
    {
        minimumGaugeSpend = Mathf.Max(0f, minimumGaugeSpend);
        maximumGaugeSpend = Mathf.Max(minimumGaugeSpend, maximumGaugeSpend);
        spendByHoldNormalized ??= AnimationCurve.Linear(0f, 0f, 1f, 1f);
        damageMultiplierBySpend ??= new GaugeScaledFloatParameter();
        hitCountBySpend ??= new GaugeScaledIntParameter();
        maxTargetsBySpend ??= new GaugeScaledIntParameter();
        pierceCountBySpend ??= new GaugeScaledIntParameter();
        projectileCountBySpend ??= new GaugeScaledIntParameter();
        radiusBySpend ??= new GaugeScaledFloatParameter();
        rangeBySpend ??= new GaugeScaledFloatParameter();
        breakPowerBySpend ??= new GaugeScaledFloatParameter();
        surgeChanceBySpend ??= new GaugeScaledFloatParameter();
        surgePowerBySpend ??= new GaugeScaledFloatParameter();

        damageMultiplierBySpend.Sanitize();
        hitCountBySpend.Sanitize(1);
        maxTargetsBySpend.Sanitize(1);
        pierceCountBySpend.Sanitize(0);
        projectileCountBySpend.Sanitize(1);
        radiusBySpend.Sanitize();
        rangeBySpend.Sanitize();
        breakPowerBySpend.Sanitize();
        surgeChanceBySpend.Sanitize();
        surgePowerBySpend.Sanitize();
    }
}
