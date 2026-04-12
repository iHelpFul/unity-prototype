using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUDController : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider hpSlider;
    [SerializeField] private Slider mpSlider;
    [SerializeField] private Slider expSlider;

    [Header("Fill Images")]
    [SerializeField] private Image hpFill;
    [SerializeField] private Image mpFill;
    [SerializeField] private Image expFill;

    [Header("TextMeshPRO")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI mpText;
    [SerializeField] private TextMeshProUGUI expText;
    [SerializeField] private TextMeshProUGUI lvlText;

    [Header("Roots")]
    [SerializeField] private GameObject hpRoot;
    [SerializeField] private GameObject mpRoot;
    [SerializeField] private GameObject expRoot;
    [SerializeField] private PlayerCharacter trackedPlayer;
    [SerializeField] private GameBootstrap bootstrap;

    private Coroutine hpRoutine;
    private Coroutine mpRoutine;

    private bool lowHpActive;
    private Color currentHpBaseColor;
    private int currentLevel;
    private int currentMesos;
    private int currentRedPotions;
    private int currentBluePotions;

    public void BindRuntimeContext(GameBootstrap sessionBootstrap, PlayerCharacter player)
    {
        bootstrap = sessionBootstrap;

        if (player != null && player.IsLocalPlayer)
            trackedPlayer = player;

        RefreshFromRuntimeData();
    }

    private void Start()
    {
        RefreshFromRuntimeData();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerHealthChangedEvent>(OnHealthChanged);
        EventBus.Subscribe<PlayerManaChangedEvent>(OnManaChanged);
        EventBus.Subscribe<PlayerExpChangedEvent>(OnExpChanged);
        EventBus.Subscribe<PlayerLevelUpEvent>(OnLevelChange);
        EventBus.Subscribe<PlayerCurrencyChangedEvent>(OnCurrencyChanged);
        EventBus.Subscribe<PlayerConsumablesChangedEvent>(OnConsumablesChanged);
        EventBus.Subscribe<MapTransitionCompletedEvent>(OnMapTransitionCompleted);
        RefreshFromRuntimeData();
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<PlayerHealthChangedEvent>(OnHealthChanged);
        EventBus.Unsubscribe<PlayerManaChangedEvent>(OnManaChanged);
        EventBus.Unsubscribe<PlayerExpChangedEvent>(OnExpChanged);
        EventBus.Unsubscribe<PlayerLevelUpEvent>(OnLevelChange);
        EventBus.Unsubscribe<PlayerCurrencyChangedEvent>(OnCurrencyChanged);
        EventBus.Unsubscribe<PlayerConsumablesChangedEvent>(OnConsumablesChanged);
        EventBus.Unsubscribe<MapTransitionCompletedEvent>(OnMapTransitionCompleted);
    }

    private void RefreshFromRuntimeData()
    {
        PlayerSessionCharacterApplicationService characterSession = bootstrap != null ? bootstrap.CharacterSession : null;
        PlayerSessionEquipmentApplicationService equipmentSession = bootstrap != null ? bootstrap.EquipmentSession : null;
        PlayerSessionInventoryApplicationService inventorySession = bootstrap != null ? bootstrap.InventorySession : null;
        PlayerSessionCurrencyApplicationService currencySession = bootstrap != null ? bootstrap.CurrencySession : null;

        if (characterSession == null
            || equipmentSession == null
            || inventorySession == null
            || currencySession == null
            || trackedPlayer == null
            || characterSession.PlayerData == null)
            return;

        PlayerRuntimeData data = characterSession.PlayerData;
        ItemStatModifierData equipmentBonuses = equipmentSession.GetEquipmentStatBonuses();
        int effectiveMaxHP = Mathf.Max(1, data.MaxHP + equipmentBonuses.MaxHP);
        int effectiveMaxMP = Mathf.Max(0, data.MaxMP + equipmentBonuses.MaxMP);
        float hpPercent = effectiveMaxHP > 0 ? (float)data.CurrentHP / effectiveMaxHP : 0f;
        float mpPercent = effectiveMaxMP > 0 ? (float)data.CurrentMP / effectiveMaxMP : 0f;
        float expPercent = data.RequiredExp > 0 ? (float)data.CurrentExp / data.RequiredExp : 0f;
        currentLevel = data.Level;
        currentMesos = currencySession.CurrentMesos;
        currentRedPotions = inventorySession.GetInventoryCount(ItemDatabase.RedPotionId);
        currentBluePotions = inventorySession.GetInventoryCount(ItemDatabase.BluePotionId);

        hpSlider.value = hpPercent;
        hpFill.color = EvaluateColor(hpPercent, BarType.HP);
        hpText.text = $"[{data.CurrentHP}/{effectiveMaxHP}]";
        hpRoot.SetActive(true);
        currentHpBaseColor = hpFill.color;
        lowHpActive = hpPercent <= 0.25f;

        mpSlider.value = mpPercent;
        mpFill.color = EvaluateColor(mpPercent, BarType.MP);
        mpText.text = $"[{data.CurrentMP}/{effectiveMaxMP}]";
        mpRoot.SetActive(true);

        expSlider.value = expPercent;
        expFill.color = EvaluateExpColor(expPercent);
        expText.text = $"[{data.CurrentExp}/{data.RequiredExp}]";
        expRoot.SetActive(true);

        RefreshLevelAndCurrencyDisplay();
    }

    private void OnHealthChanged(PlayerHealthChangedEvent e)
    {
        if (!IsTrackedPlayer(e.Target, e.CharacterId))
            return;

        float target = e.MaxHP > 0 ? (float)e.CurrentHP / e.MaxHP : 0f;
        hpText.text = $"[{e.CurrentHP}/{e.MaxHP}]";
        hpRoot.SetActive(true);

        if (hpRoutine != null)
            StopCoroutine(hpRoutine);

        hpRoutine = StartCoroutine(SmoothSlider(hpSlider, hpFill, target, 0.2f, BarType.HP));
        StartCoroutine(HitBarFlash());
        lowHpActive = target <= 0.25f;
    }

    private void OnManaChanged(PlayerManaChangedEvent e)
    {
        if (!IsTrackedPlayer(e.Target, e.CharacterId))
            return;

        float target = e.MaxMP > 0 ? (float)e.CurrentMP / e.MaxMP : 0f;
        mpText.text = $"[{e.CurrentMP}/{e.MaxMP}]";
        mpRoot.SetActive(true);

        if (mpRoutine != null)
            StopCoroutine(mpRoutine);

        mpRoutine = StartCoroutine(SmoothSlider(mpSlider, mpFill, target, 0.2f, BarType.MP));
    }

    private void OnExpChanged(PlayerExpChangedEvent e)
    {
        if (!IsTrackedPlayer(e.Target, e.CharacterId))
            return;

        float percent = e.RequiredExp > 0 ? (float)e.CurrentExp / e.RequiredExp : 0f;
        expText.text = $"[{e.CurrentExp}/{e.RequiredExp}]";
        expRoot.SetActive(true);

        expSlider.value = percent;
        expFill.color = EvaluateExpColor(percent);
    }

    private void OnLevelChange(PlayerLevelUpEvent e)
    {
        if (!IsTrackedPlayer(e.Target, e.CharacterId))
            return;

        currentLevel = e.NewLevel;
        RefreshLevelAndCurrencyDisplay();
    }

    private void OnCurrencyChanged(PlayerCurrencyChangedEvent e)
    {
        if (!IsTrackedPlayer(e.Target, e.CharacterId))
            return;

        currentMesos = e.Mesos;
        RefreshLevelAndCurrencyDisplay();
    }

    private void OnConsumablesChanged(PlayerConsumablesChangedEvent e)
    {
        if (!IsTrackedPlayer(e.Target, e.CharacterId))
            return;

        currentRedPotions = e.RedPotions;
        currentBluePotions = e.BluePotions;
        RefreshLevelAndCurrencyDisplay();
    }

    private void OnMapTransitionCompleted(MapTransitionCompletedEvent e)
    {
        if (e.Player != null && e.Player.IsLocalPlayer)
            trackedPlayer = e.Player;
        else if (!IsTrackedPlayer(e.Player, e.CharacterId))
            return;

        RefreshFromRuntimeData();
    }

    private void RefreshLevelAndCurrencyDisplay()
    {
        if (lvlText == null)
            return;

        lvlText.text = $"Lv. {currentLevel}  |  {currentMesos} Mesos  |  Red x{currentRedPotions}  |  Blue x{currentBluePotions}";
    }

    private IEnumerator SmoothSlider(
        Slider slider,
        Image fill,
        float target,
        float duration,
        BarType type)
    {
        float start = slider.value;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;

            float value = Mathf.Lerp(start, target, t);
            slider.value = value;

            Color c = EvaluateColor(value, type);
            fill.color = c;

            if (type == BarType.HP)
                currentHpBaseColor = c;

            yield return null;
        }

        slider.value = target;

        Color finalColor = EvaluateColor(target, type);
        fill.color = finalColor;

        if (type == BarType.HP)
            currentHpBaseColor = finalColor;
    }

    private void Update()
    {
        if (!lowHpActive)
            return;

        float pulse = Mathf.PingPong(Time.time * 3f, 1f);
        hpFill.color = Color.Lerp(
            currentHpBaseColor,
            Color.red,
            pulse * 0.6f);
    }

    private IEnumerator HitBarFlash()
    {
        Color original = hpFill.color;

        hpFill.color = Color.white;
        yield return new WaitForSeconds(0.05f);

        hpFill.color = original;
    }

    private Color EvaluateColor(float percent, BarType type)
    {
        switch (type)
        {
            case BarType.HP:
                return Color.Lerp(
                    Hex("#B33A3A"),
                    Hex("#F27A3D"),
                    percent);

            case BarType.MP:
                return Color.Lerp(
                    Hex("#1E3A6B"),
                    Hex("#58B6D6"),
                    percent);

            default:
                return Color.white;
        }
    }

    private Color EvaluateExpColor(float percent)
    {
        return Color.Lerp(
            Hex("#4A8F3B"),
            Hex("#A8F07A"),
            percent);
    }

    private Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out Color color);
        return color;
    }

    private enum BarType
    {
        HP,
        MP
    }

    private bool IsTrackedPlayer(PlayerCharacter target, string characterId)
    {
        return PlayerRuntimeIdentityUtility.MatchesCharacter(
            trackedPlayer,
            trackedPlayer != null ? trackedPlayer.CharacterId : string.Empty,
            target,
            characterId);
    }
}
