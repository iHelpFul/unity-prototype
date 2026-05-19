using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EnemyOverheadUiView : MonoBehaviour
{
    private const string PoiseDisplayFormat = "0.0";

    [SerializeField] private GameObject root;
    [SerializeField] private Transform anchorRoot;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image healthFill;
    [SerializeField] private GameObject poiseRoot;
    [SerializeField] private Slider poiseSlider;
    [SerializeField] private Image poiseFill;
    [SerializeField] private TMP_Text poiseValueText;
    [SerializeField] private Image elementIcon;
    [SerializeField] private Image[] statusIcons = new Image[0];

    [Header("Presentation")]
    [SerializeField] private Color normalHealthColor = new Color(1f, 0.24f, 0.24f, 1f);
    [SerializeField] private Color eliteHealthColor = new Color(1f, 0.77f, 0.22f, 1f);
    [SerializeField] private Color normalPoiseColor = new Color(0.4f, 0.9f, 1f, 1f);
    [SerializeField] private Color elitePoiseColor = new Color(1f, 0.88f, 0.35f, 1f);
    [SerializeField] private Color brokenPoiseColor = new Color(1f, 0.42f, 0.18f, 1f);
    [SerializeField] private Color normalPoiseTextColor = new Color(1f, 1f, 1f, 0.92f);
    [SerializeField] private Color elitePoiseTextColor = new Color(1f, 0.94f, 0.62f, 1f);
    [SerializeField] private Color brokenPoiseTextColor = new Color(1f, 0.6f, 0.38f, 1f);

    public Transform AnchorRoot => anchorRoot != null ? anchorRoot : transform;

    private bool isElite;
    private bool isBroken;

    private void Awake()
    {
        if (root == null)
            root = gameObject;

        HideAuxiliaryIndicators();
    }

    public void SetVisible(bool isVisible)
    {
        if (root == null)
            root = gameObject;

        HideAuxiliaryIndicators();

        if (root.activeSelf != isVisible)
            root.SetActive(isVisible);
    }

    public void SetHealth(int currentHp, int maxHp)
    {
        float normalized = maxHp > 0 ? Mathf.Clamp01((float)currentHp / maxHp) : 0f;

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = Mathf.Max(1f, maxHp);
            healthSlider.value = Mathf.Clamp(currentHp, 0, maxHp);
        }

        if (healthFill != null)
        {
            healthFill.fillAmount = normalized;
            healthFill.color = isElite ? eliteHealthColor : normalHealthColor;
        }
    }

    public void SetPoise(float currentPressure, float maxPressure, bool isBroken)
    {
        this.isBroken = isBroken;
        bool shouldShow = maxPressure > 0.01f;
        SetPoiseVisible(shouldShow);

        if (!shouldShow)
        {
            ResetPoiseVisuals();
            return;
        }

        float remainingPoise = isBroken
            ? 0f
            : Mathf.Clamp(Mathf.Max(0f, maxPressure) - Mathf.Max(0f, currentPressure), 0f, Mathf.Max(0f, maxPressure));
        float normalizedRemaining = isBroken
            ? 0f
            : Mathf.Clamp01(1f - (Mathf.Max(0f, currentPressure) / Mathf.Max(0.01f, maxPressure)));

        if (poiseSlider != null)
        {
            poiseSlider.minValue = 0f;
            poiseSlider.maxValue = 1f;
            poiseSlider.value = normalizedRemaining;
        }

        if (poiseFill != null)
        {
            poiseFill.fillAmount = normalizedRemaining;
            poiseFill.color = ResolvePoiseColor();
        }

        if (poiseValueText != null)
        {
            poiseValueText.text = remainingPoise.ToString(PoiseDisplayFormat);
            poiseValueText.color = ResolvePoiseTextColor();
        }
    }

    public void SetPresentationState(bool isElite, bool isBroken)
    {
        this.isElite = isElite;
        this.isBroken = isBroken;

        if (healthFill != null)
            healthFill.color = isElite ? eliteHealthColor : normalHealthColor;

        if (poiseFill != null)
            poiseFill.color = ResolvePoiseColor();

        if (poiseValueText != null && !string.IsNullOrEmpty(poiseValueText.text))
            poiseValueText.color = ResolvePoiseTextColor();
    }

    private void SetPoiseVisible(bool shouldShow)
    {
        if (poiseRoot != null)
        {
            SetGameObjectVisible(poiseRoot, shouldShow);
            return;
        }

        SetGameObjectVisible(poiseSlider != null ? poiseSlider.gameObject : null, shouldShow);
        if (poiseFill != null && poiseFill.gameObject != poiseSlider?.gameObject)
            SetGameObjectVisible(poiseFill.gameObject, shouldShow);

        if (poiseValueText != null
            && poiseValueText.gameObject != poiseSlider?.gameObject
            && poiseValueText.gameObject != poiseFill?.gameObject)
        {
            SetGameObjectVisible(poiseValueText.gameObject, shouldShow);
        }
    }

    private void ResetPoiseVisuals()
    {
        if (poiseSlider != null)
        {
            poiseSlider.minValue = 0f;
            poiseSlider.maxValue = 1f;
            poiseSlider.value = 0f;
        }

        if (poiseFill != null)
        {
            poiseFill.fillAmount = 0f;
            poiseFill.color = ResolvePoiseColor();
        }

        if (poiseValueText != null)
        {
            poiseValueText.text = string.Empty;
            poiseValueText.color = ResolvePoiseTextColor();
        }
    }

    private static void SetGameObjectVisible(GameObject target, bool shouldShow)
    {
        if (target != null && target.activeSelf != shouldShow)
            target.SetActive(shouldShow);
    }

    private void HideAuxiliaryIndicators()
    {
        if (elementIcon != null)
            SetGameObjectVisible(elementIcon.gameObject, false);

        if (statusIcons == null)
            return;

        for (int index = 0; index < statusIcons.Length; index++)
        {
            Image image = statusIcons[index];
            if (image != null)
                SetGameObjectVisible(image.gameObject, false);
        }
    }

    private Color ResolvePoiseColor()
    {
        if (isBroken)
            return brokenPoiseColor;

        return isElite ? elitePoiseColor : normalPoiseColor;
    }

    private Color ResolvePoiseTextColor()
    {
        if (isBroken)
            return brokenPoiseTextColor;

        return isElite ? elitePoiseTextColor : normalPoiseTextColor;
    }
}
