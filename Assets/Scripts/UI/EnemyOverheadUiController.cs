using UnityEngine;

[DisallowMultipleComponent]
public class EnemyOverheadUiController : MonoBehaviour
{
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private EnemyOverheadUiView view;
    [SerializeField] private Transform anchorOverride;
    [SerializeField] private Vector3 fallbackWorldOffset = new Vector3(0f, 2.15f, 0f);
    [SerializeField] private float rendererTopPadding = 0.2f;
    [SerializeField] private float visibleDurationAfterHit = 3.5f;

    private Renderer[] cachedRenderers = System.Array.Empty<Renderer>();
    private float visibleTimer;
    private float currentPressure;
    private float maxPressure;
    private bool isBroken;

    private void Awake()
    {
        enemyHealth ??= GetComponentInParent<EnemyHealth>();
        view ??= GetComponentInChildren<EnemyOverheadUiView>(true);
        RefreshRendererCache();
        RefreshPresentationState();
        RefreshHealth();
        RefreshPoise();
        SetVisible(false);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<EnemyHealthChangedEvent>(OnEnemyHealthChanged);
        EventBus.Subscribe<EnemyRespawnedEvent>(OnEnemyRespawned);
        EventBus.Subscribe<EnemyPressureChangedEvent>(OnEnemyPressureChanged);
        EventBus.Subscribe<EnemyBreakStateChangedEvent>(OnEnemyBreakStateChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<EnemyHealthChangedEvent>(OnEnemyHealthChanged);
        EventBus.Unsubscribe<EnemyRespawnedEvent>(OnEnemyRespawned);
        EventBus.Unsubscribe<EnemyPressureChangedEvent>(OnEnemyPressureChanged);
        EventBus.Unsubscribe<EnemyBreakStateChangedEvent>(OnEnemyBreakStateChanged);
    }

    private void Update()
    {
        if (visibleTimer > 0f)
            visibleTimer = Mathf.Max(0f, visibleTimer - Time.deltaTime);

        RefreshVisibility();
    }

    private void LateUpdate()
    {
        if (view == null || !ShouldRemainVisible())
            return;

        UpdateWorldPosition();
        FaceCamera();
    }

    public void ForceShowForDuration(float duration)
    {
        visibleTimer = Mathf.Max(visibleTimer, Mathf.Max(0f, duration));
        RefreshVisibility();
    }

    private void OnEnemyHealthChanged(EnemyHealthChangedEvent e)
    {
        if (e.Enemy != enemyHealth || enemyHealth == null)
            return;

        view?.SetHealth(e.CurrentHP, e.MaxHP);

        if (e.IsDead)
        {
            visibleTimer = 0f;
            currentPressure = 0f;
            isBroken = false;
            RefreshPresentationState();
            RefreshPoise();
            RefreshVisibility();
            return;
        }

        if (e.RevealOverhead)
            ForceShowForDuration(visibleDurationAfterHit);
    }

    private void OnEnemyRespawned(EnemyRespawnedEvent e)
    {
        if (e.Enemy != enemyHealth || enemyHealth == null)
            return;

        visibleTimer = 0f;
        currentPressure = 0f;
        isBroken = false;
        RefreshPresentationState();
        RefreshHealth();
        RefreshPoise();
        SetVisible(false);
    }

    private void OnEnemyPressureChanged(EnemyPressureChangedEvent e)
    {
        if (e.Enemy != enemyHealth || enemyHealth == null)
            return;

        currentPressure = Mathf.Max(0f, e.CurrentPressure);
        maxPressure = Mathf.Max(0f, e.MaxPressure);
        isBroken = e.IsBroken;
        RefreshPresentationState();
        RefreshPoise();
        RefreshVisibility();
    }

    private void OnEnemyBreakStateChanged(EnemyBreakStateChangedEvent e)
    {
        if (e.Enemy != enemyHealth || enemyHealth == null)
            return;

        isBroken = e.IsBroken;
        if (!isBroken && enemyHealth.BreakController != null)
            currentPressure = enemyHealth.BreakController.CurrentPressure;

        RefreshPresentationState();
        RefreshPoise();
        RefreshVisibility();
    }

    private void RefreshHealth()
    {
        if (enemyHealth == null)
            return;

        view?.SetHealth(enemyHealth.CurrentHP, enemyHealth.MaxHP);
    }

    private void RefreshPoise()
    {
        if (enemyHealth == null)
            return;

        if (enemyHealth.BreakController != null)
        {
            currentPressure = enemyHealth.BreakController.CurrentPressure;
            maxPressure = enemyHealth.BreakController.PressureThreshold;
            isBroken = enemyHealth.BreakController.IsBroken;
        }
        else
        {
            currentPressure = 0f;
            maxPressure = enemyHealth.Stats != null
                ? Mathf.Max(0f, enemyHealth.Stats.PoiseProfile.PoiseThreshold)
                : 0f;
            isBroken = false;
        }

        RefreshPresentationState();
        view?.SetPoise(currentPressure, maxPressure, isBroken);
    }

    private bool ShouldRemainVisible()
    {
        return enemyHealth != null
            && !enemyHealth.IsDead
            && (visibleTimer > 0f || currentPressure > 0f || isBroken);
    }

    private void SetVisible(bool isVisible)
    {
        view?.SetVisible(isVisible);
    }

    private void RefreshVisibility()
    {
        SetVisible(ShouldRemainVisible());
    }

    private void RefreshPresentationState()
    {
        if (enemyHealth == null)
            return;

        view?.SetPresentationState(enemyHealth.IsElite, isBroken);
    }

    private void RefreshRendererCache()
    {
        Transform target = anchorOverride != null ? anchorOverride : enemyHealth != null ? enemyHealth.transform : transform;
        cachedRenderers = target != null
            ? target.GetComponentsInChildren<Renderer>(true)
            : System.Array.Empty<Renderer>();
    }

    private void UpdateWorldPosition()
    {
        if (view == null)
            return;

        Transform anchor = anchorOverride != null
            ? anchorOverride
            : enemyHealth != null
                ? enemyHealth.transform
                : transform;

        Vector3 position = anchor != null ? anchor.position + fallbackWorldOffset : transform.position + fallbackWorldOffset;
        float highestY = float.MinValue;

        for (int index = 0; index < cachedRenderers.Length; index++)
        {
            Renderer renderer = cachedRenderers[index];
            if (renderer == null || !renderer.enabled)
                continue;

            highestY = Mathf.Max(highestY, renderer.bounds.max.y);
        }

        if (highestY > float.MinValue)
            position.y = highestY + Mathf.Max(0f, rendererTopPadding);

        view.AnchorRoot.position = position;
    }

    private void FaceCamera()
    {
        if (view == null)
            return;

        Camera activeCamera = Camera.main;
        if (activeCamera == null)
            return;

        Transform anchor = view.AnchorRoot;
        anchor.rotation = activeCamera.transform.rotation;
    }
}
