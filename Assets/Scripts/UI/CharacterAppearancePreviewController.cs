using UnityEngine;

public class CharacterAppearancePreviewController : MonoBehaviour
{
    [SerializeField] private CharacterAppearanceVisualController visuals;
    [SerializeField] private GameObject previewRoot;
    [SerializeField] private Vector3 hiddenLocalPosition = new Vector3(10000f, -10000f, 0f);

    private CharacterSelectionSnapshot activeSnapshot;
    private CharacterAppearanceData draftAppearance;
    private bool isCreationVisible;
    private Transform cachedPreviewTransform;
    private Vector3 visibleLocalPosition;
    private bool hasCachedVisiblePosition;

    private void Start()
    {
        CachePreviewTransform();
        RefreshPreview();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<CharacterSelectionStateChangedEvent>(OnSelectionStateChanged);
        EventBus.Subscribe<CharacterCreationPanelVisibilityChangedEvent>(OnCreationVisibilityChanged);
        EventBus.Subscribe<CharacterAppearancePreviewChangedEvent>(OnPreviewChanged);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<CharacterSelectionStateChangedEvent>(OnSelectionStateChanged);
        EventBus.Unsubscribe<CharacterCreationPanelVisibilityChangedEvent>(OnCreationVisibilityChanged);
        EventBus.Unsubscribe<CharacterAppearancePreviewChangedEvent>(OnPreviewChanged);
    }

    private void OnSelectionStateChanged(CharacterSelectionStateChangedEvent e)
    {
        activeSnapshot = e.Snapshot;
        if (!isCreationVisible)
            RefreshPreview();
    }

    private void OnCreationVisibilityChanged(CharacterCreationPanelVisibilityChangedEvent e)
    {
        isCreationVisible = e.IsVisible;
        RefreshPreview();
    }

    private void OnPreviewChanged(CharacterAppearancePreviewChangedEvent e)
    {
        draftAppearance = e.Appearance != null ? e.Appearance.Clone() : null;
        if (isCreationVisible)
            RefreshPreview();
    }

    private void RefreshPreview()
    {
        if (visuals == null)
            return;

        SetPreviewVisible(isCreationVisible);

        if (!isCreationVisible)
            return;

        CharacterAppearanceData appearance = draftAppearance;
        if (appearance == null)
            appearance = GetSelectedAppearance();

        if (appearance != null)
            visuals.ApplyAppearance(appearance);
    }

    private CharacterAppearanceData GetSelectedAppearance()
    {
        if (activeSnapshot?.Slots == null)
            return null;

        for (int index = 0; index < activeSnapshot.Slots.Count; index++)
        {
            CharacterSlotSnapshot slot = activeSnapshot.Slots[index];
            if (slot != null && slot.IsSelected && slot.Appearance != null)
                return slot.Appearance.Clone();
        }

        return null;
    }

    private void SetPreviewVisible(bool isVisible)
    {
        CachePreviewTransform();

        if (cachedPreviewTransform == null)
            return;

        cachedPreviewTransform.localPosition = isVisible ? visibleLocalPosition : hiddenLocalPosition;
    }

    private void CachePreviewTransform()
    {
        if (cachedPreviewTransform != null)
            return;

        GameObject target = previewRoot != null ? previewRoot : visuals.gameObject;
        cachedPreviewTransform = target != null ? target.transform : null;

        if (cachedPreviewTransform != null && !hasCachedVisiblePosition)
        {
            visibleLocalPosition = cachedPreviewTransform.localPosition;
            hasCachedVisiblePosition = true;
        }
    }
}
