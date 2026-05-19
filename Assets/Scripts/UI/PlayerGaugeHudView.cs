using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerGaugeHudView : MonoBehaviour
{
    [Header("Gauge")]
    [SerializeField] private Slider gaugeSlider;
    [SerializeField] private Image gaugeFill;
    [SerializeField] private TextMeshProUGUI gaugeText;

    [Header("Flow Stacks")]
    [SerializeField] private Image[] flowStackIndicators = new Image[0];
    [SerializeField] private TextMeshProUGUI flowText;

    [Header("Ready State")]
    [SerializeField] private Image readyStateIcon;
    [SerializeField] private Image readyStateTimerFill;
    [SerializeField] private Sprite breachReadySprite;
    [SerializeField] private Sprite markReadySprite;

    [Header("Visuals")]
    [SerializeField] private Color inactiveFlowColor = new Color(1f, 1f, 1f, 0.18f);
    [SerializeField] private Color activeFlowColor = new Color(1f, 0.83f, 0.36f, 0.95f);
    [SerializeField] private Color expiringFlowColor = new Color(1f, 0.45f, 0.24f, 1f);
    [SerializeField] private Color empoweredFlowColor = new Color(0.38f, 1f, 0.78f, 1f);
    [SerializeField] private Color inactiveReadyStateColor = new Color(1f, 1f, 1f, 0.18f);
    [SerializeField] private Color breachReadyActiveColor = new Color(1f, 0.55f, 0.24f, 1f);
    [SerializeField] private Color markReadyActiveColor = new Color(0.38f, 1f, 0.78f, 1f);
    [SerializeField] private Color expiringReadyStateColor = new Color(1f, 0.82f, 0.4f, 1f);
    [SerializeField] private float activeFlowScale = 1.08f;
    [SerializeField] private float empoweredFlowScale = 1.16f;
    [SerializeField] private float expiringFlowThresholdNormalized = 0.33f;
    [SerializeField] private int gaugeDecimalPlaces = 1;

    public void SetGauge(float normalizedValue, float currentGauge, float maxGauge)
    {
        if (gaugeSlider != null)
        {
            gaugeSlider.minValue = 0f;
            gaugeSlider.maxValue = 1f;
            gaugeSlider.value = Mathf.Clamp01(normalizedValue);
        }

        if (gaugeText != null)
            gaugeText.text = $"{FormatGaugeValue(currentGauge)}/{FormatGaugeValue(maxGauge)}";
    }

    public void SetFlow(
        int currentStacks,
        int maxStacks,
        float timerNormalized,
        bool hasEmpoweredBasicReady,
        float empoweredTimerNormalized)
    {
        int safeMaxStacks = Mathf.Max(0, maxStacks);
        int safeCurrentStacks = Mathf.Clamp(currentStacks, 0, safeMaxStacks);
        float safeTimerNormalized = Mathf.Clamp01(timerNormalized);
        float safeEmpoweredTimerNormalized = Mathf.Clamp01(empoweredTimerNormalized);

        if (flowText != null)
            flowText.text = hasEmpoweredBasicReady
                ? "READY"
                : safeMaxStacks > 0 ? $"{safeCurrentStacks}/{safeMaxStacks}" : "0/0";

        if (flowStackIndicators == null || flowStackIndicators.Length == 0)
            return;

        for (int index = 0; index < flowStackIndicators.Length; index++)
        {
            Image indicator = flowStackIndicators[index];
            if (indicator == null)
                continue;

            bool isWithinConfiguredRange = index < safeMaxStacks;
            bool isActive = isWithinConfiguredRange && (hasEmpoweredBasicReady || index < safeCurrentStacks);

            indicator.enabled = isWithinConfiguredRange;
            if (!isWithinConfiguredRange)
                continue;

            indicator.color = ResolveFlowColor(isActive, safeTimerNormalized, hasEmpoweredBasicReady, safeEmpoweredTimerNormalized);
            indicator.transform.localScale = isActive
                ? Vector3.one * Mathf.Max(1f, hasEmpoweredBasicReady ? empoweredFlowScale : activeFlowScale)
                : Vector3.one;
        }
    }

    public void SetReadyState(PlayerReadyStateType readyStateType, float timerNormalized)
    {
        Sprite resolvedSprite = ResolveReadyStateSprite(readyStateType);
        Color resolvedColor = ResolveReadyStateColor(readyStateType, timerNormalized);
        bool isActive = readyStateType != PlayerReadyStateType.None;

        if (readyStateIcon != null)
        {
            readyStateIcon.enabled = resolvedSprite != null;
            if (resolvedSprite != null)
            {
                readyStateIcon.sprite = resolvedSprite;
                readyStateIcon.color = resolvedColor;
            }
            else
            {
                readyStateIcon.color = inactiveReadyStateColor;
            }
        }

        if (readyStateTimerFill != null)
        {
            readyStateTimerFill.enabled = isActive;
            readyStateTimerFill.fillAmount = isActive ? Mathf.Clamp01(timerNormalized) : 0f;
            readyStateTimerFill.color = resolvedColor;
        }
    }

    private Color ResolveFlowColor(
        bool isActive,
        float timerNormalized,
        bool hasEmpoweredBasicReady,
        float empoweredTimerNormalized)
    {
        if (!isActive)
            return inactiveFlowColor;

        if (hasEmpoweredBasicReady)
            return Color.Lerp(expiringFlowColor, empoweredFlowColor, empoweredTimerNormalized);

        if (timerNormalized <= expiringFlowThresholdNormalized)
            return expiringFlowColor;

        return activeFlowColor;
    }

    private string FormatGaugeValue(float value)
    {
        int safeDecimalPlaces = Mathf.Clamp(gaugeDecimalPlaces, 0, 3);
        float safeValue = Mathf.Max(0f, value);
        return safeValue.ToString($"F{safeDecimalPlaces}");
    }

    private Sprite ResolveReadyStateSprite(PlayerReadyStateType readyStateType)
    {
        return readyStateType switch
        {
            PlayerReadyStateType.Breach => breachReadySprite,
            PlayerReadyStateType.Mark => markReadySprite,
            _ => null
        };
    }

    private Color ResolveReadyStateColor(PlayerReadyStateType readyStateType, float timerNormalized)
    {
        if (readyStateType == PlayerReadyStateType.None)
            return inactiveReadyStateColor;

        Color activeColor = readyStateType == PlayerReadyStateType.Breach
            ? breachReadyActiveColor
            : markReadyActiveColor;

        return Color.Lerp(expiringReadyStateColor, activeColor, Mathf.Clamp01(timerNormalized));
    }
}
