using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class CombatFloatingTextStyle
{
    [SerializeField] private Color topLeft;
    [SerializeField] private Color topRight;
    [SerializeField] private Color bottomLeft;
    [SerializeField] private Color bottomRight;
    [SerializeField] private float scale;
    [SerializeField] private float floatHeight;
    [SerializeField] private float horizontalDrift;
    [SerializeField] private float duration;

    public CombatFloatingTextStyle()
        : this(Color.white, Color.white, Color.white, Color.white, 1f, 1.25f, 0f, 0.8f)
    {
    }

    public CombatFloatingTextStyle(
        Color newTopLeft,
        Color newTopRight,
        Color newBottomLeft,
        Color newBottomRight,
        float newScale,
        float newFloatHeight,
        float newHorizontalDrift,
        float newDuration)
    {
        topLeft = newTopLeft;
        topRight = newTopRight;
        bottomLeft = newBottomLeft;
        bottomRight = newBottomRight;
        scale = newScale;
        floatHeight = newFloatHeight;
        horizontalDrift = newHorizontalDrift;
        duration = newDuration;
    }

    public Color TopLeft => topLeft;
    public Color TopRight => topRight;
    public Color BottomLeft => bottomLeft;
    public Color BottomRight => bottomRight;
    public float Scale => Mathf.Max(0.01f, scale);
    public float FloatHeight => floatHeight;
    public float HorizontalDrift => horizontalDrift;
    public float Duration => Mathf.Max(0.05f, duration);

    public VertexGradient ToVertexGradient()
    {
        return new VertexGradient(topLeft, topRight, bottomLeft, bottomRight);
    }

    public void CopyFrom(CombatFloatingTextStyle other)
    {
        if (other == null)
            return;

        topLeft = other.topLeft;
        topRight = other.topRight;
        bottomLeft = other.bottomLeft;
        bottomRight = other.bottomRight;
        scale = other.scale;
        floatHeight = other.floatHeight;
        horizontalDrift = other.horizontalDrift;
        duration = other.duration;
    }
}

public class DamageNumberSystem : MonoBehaviour
{
    [SerializeField] private GameObject damageTextPrefab;
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private bool useDefaultPalettes = true;
    [SerializeField] private float stackSpacing = 0.34f;
    [SerializeField] private CombatFloatingTextStyle damageStyle = CreateSupernovaStyle();
    [SerializeField] private CombatFloatingTextStyle surgeDamageStyle = CreateCyberPeachStyle();
    [SerializeField] private CombatFloatingTextStyle evadeStyle = CreateGhostLimeStyle();

    private readonly Dictionary<Transform, List<ActiveFloatingText>> activeByTarget = new Dictionary<Transform, List<ActiveFloatingText>>();

    private void OnEnable()
    {
        ApplyDefaultStylesIfNeeded();
        ApplyDefaultPalettesIfEnabled();
        EventBus.Subscribe<DamageNumberEvent>(OnDamageNumber);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<DamageNumberEvent>(OnDamageNumber);
    }

    private void OnValidate()
    {
        stackSpacing = Mathf.Max(0f, stackSpacing);
        ApplyDefaultStylesIfNeeded();
        ApplyDefaultPalettesIfEnabled();
    }

    private void OnDamageNumber(DamageNumberEvent e)
    {
        if (worldCanvas == null || damageTextPrefab == null)
            return;

        CombatFloatingTextStyle style = ResolveStyle(e.Kind);
        LiftExistingTargetTexts(e.Target);

        GameObject instance = Instantiate(damageTextPrefab, worldCanvas.transform);
        instance.transform.position = e.WorldPosition;
        instance.transform.localScale *= style.Scale * (e.Scale > 0f ? e.Scale : 1f);

        TextMeshProUGUI text = instance.GetComponent<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = !string.IsNullOrWhiteSpace(e.Text)
                ? e.Text
                : Mathf.Max(0, e.Damage).ToString();

            if (e.UseCustomColor)
                text.color = e.TextColor;
            else
                ApplyStyleGradient(text, style);
        }

        ActiveFloatingText activeText = new ActiveFloatingText(instance, e.Target);
        RegisterActiveText(activeText);
        StartCoroutine(Animate(activeText, e.WorldPosition, style));
    }

    private IEnumerator Animate(ActiveFloatingText activeText, Vector3 basePosition, CombatFloatingTextStyle style)
    {
        float timer = 0f;

        Vector3 start = basePosition;
        Vector3 end = start + Vector3.up * style.FloatHeight + Vector3.right * style.HorizontalDrift;

        CanvasGroup group = activeText.Instance.GetComponent<CanvasGroup>();

        while (timer < style.Duration && activeText.Instance != null)
        {
            timer += Time.deltaTime;
            float t = timer / style.Duration;

            activeText.Instance.transform.position = Vector3.Lerp(start, end, t) + Vector3.up * activeText.StackLift;

            if (group != null)
                group.alpha = 1f - t;

            yield return null;
        }

        UnregisterActiveText(activeText);

        if (activeText.Instance != null)
            Destroy(activeText.Instance);
    }

    private void LiftExistingTargetTexts(Transform target)
    {
        if (target == null || !activeByTarget.TryGetValue(target, out List<ActiveFloatingText> activeTexts))
            return;

        for (int i = activeTexts.Count - 1; i >= 0; i--)
        {
            ActiveFloatingText activeText = activeTexts[i];
            if (activeText == null || activeText.Instance == null)
            {
                activeTexts.RemoveAt(i);
                continue;
            }

            activeText.StackLift += stackSpacing;
        }
    }

    private void RegisterActiveText(ActiveFloatingText activeText)
    {
        if (activeText.Target == null)
            return;

        if (!activeByTarget.TryGetValue(activeText.Target, out List<ActiveFloatingText> activeTexts))
        {
            activeTexts = new List<ActiveFloatingText>();
            activeByTarget.Add(activeText.Target, activeTexts);
        }

        activeTexts.Add(activeText);
    }

    private void UnregisterActiveText(ActiveFloatingText activeText)
    {
        if (activeText.Target == null || !activeByTarget.TryGetValue(activeText.Target, out List<ActiveFloatingText> activeTexts))
            return;

        activeTexts.Remove(activeText);

        if (activeTexts.Count == 0)
            activeByTarget.Remove(activeText.Target);
    }

    private CombatFloatingTextStyle ResolveStyle(CombatFloatingTextKind kind)
    {
        return kind switch
        {
            CombatFloatingTextKind.SurgeDamage => surgeDamageStyle,
            CombatFloatingTextKind.Evade => evadeStyle,
            _ => damageStyle
        };
    }

    private static void ApplyStyleGradient(TextMeshProUGUI text, CombatFloatingTextStyle style)
    {
        text.enableVertexGradient = true;
        text.colorGradient = style.ToVertexGradient();
    }

    private void ApplyDefaultStylesIfNeeded()
    {
        if (damageStyle == null)
            damageStyle = CreateSupernovaStyle();

        if (surgeDamageStyle == null)
            surgeDamageStyle = CreateCyberPeachStyle();

        if (evadeStyle == null)
            evadeStyle = CreateGhostLimeStyle();
    }

    private void ApplyDefaultPalettesIfEnabled()
    {
        if (!useDefaultPalettes)
            return;

        damageStyle.CopyFrom(CreateSupernovaStyle());
        surgeDamageStyle.CopyFrom(CreateCyberPeachStyle());
        evadeStyle.CopyFrom(CreateGhostLimeStyle());
    }

    private static CombatFloatingTextStyle CreateSupernovaStyle()
    {
        return new CombatFloatingTextStyle(
            new Color32(0x00, 0xFF, 0xE0, 0xFF),
            new Color32(0x00, 0xFF, 0xFF, 0xFF),
            new Color32(0x9D, 0x00, 0xFF, 0xFF),
            new Color32(0x6E, 0x00, 0xFF, 0xFF),
            1f,
            1.25f,
            0f,
            0.8f);
    }

    private static CombatFloatingTextStyle CreateCyberPeachStyle()
    {
        return new CombatFloatingTextStyle(
            new Color32(0xFF, 0xCC, 0xBB, 0xFF),
            new Color32(0xFF, 0xDA, 0xB9, 0xFF),
            new Color32(0xFF, 0x99, 0xCC, 0xFF),
            new Color32(0xFF, 0x66, 0xB2, 0xFF),
            1.28f,
            1.45f,
            0.04f,
            0.92f);
    }

    private static CombatFloatingTextStyle CreateGhostLimeStyle()
    {
        return new CombatFloatingTextStyle(
            new Color32(0xBF, 0xFF, 0x00, 0xFF),
            new Color32(0xEE, 0xFF, 0x00, 0xFF),
            new Color32(0xFF, 0xFF, 0xFF, 0xFF),
            new Color32(0xCC, 0xFF, 0x00, 0xFF),
            0.92f,
            1.05f,
            0.28f,
            0.72f);
    }

    private sealed class ActiveFloatingText
    {
        public ActiveFloatingText(GameObject instance, Transform target)
        {
            Instance = instance;
            Target = target;
        }

        public GameObject Instance { get; }
        public Transform Target { get; }
        public float StackLift { get; set; }
    }
}
