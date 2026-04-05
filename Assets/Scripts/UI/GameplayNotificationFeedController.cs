using System.Collections.Generic;
using UnityEngine;

public class GameplayNotificationFeedController : MonoBehaviour
{
    [SerializeField] private RectTransform entriesRoot;
    [SerializeField] private GameplayNotificationEntryView entryTemplate;
    [SerializeField] private GameBootstrap bootstrap;
    [SerializeField] private PlayerCharacter trackedPlayer;
    [SerializeField] private int maxVisibleEntries = 8;
    [SerializeField] private float entrySpacing = 42f;
    [SerializeField] private Color itemColor = new Color(0.92f, 0.98f, 1f);
    [SerializeField] private Color mesosColor = new Color(1f, 0.86f, 0.32f);
    [SerializeField] private Color experienceColor = new Color(0.6f, 1f, 0.72f);
    [SerializeField] private Color killColor = new Color(1f, 0.76f, 0.48f);
    [SerializeField] private Color systemColor = new Color(1f, 0.9f, 0.75f);

    private readonly List<GameplayNotificationEntryView> activeEntries = new List<GameplayNotificationEntryView>();
    private Vector2 baseEntryPosition;

    private void Awake()
    {
        if (entryTemplate != null)
        {
            RectTransform templateRect = entryTemplate.transform as RectTransform;
            baseEntryPosition = templateRect != null ? templateRect.anchoredPosition : Vector2.zero;
            entryTemplate.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        EventBus.Subscribe<GameplayNotificationEvent>(OnNotificationReceived);
        EventBus.Subscribe<MapTransitionCompletedEvent>(OnMapTransitionCompleted);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<GameplayNotificationEvent>(OnNotificationReceived);
        EventBus.Unsubscribe<MapTransitionCompletedEvent>(OnMapTransitionCompleted);
    }

    private void LateUpdate()
    {
        if (CleanupMissingEntries())
            RebuildLayout(true);
    }

    private void OnNotificationReceived(GameplayNotificationEvent e)
    {
        if (string.IsNullOrWhiteSpace(e.Message))
            return;

        ResolveRuntimeContext();
        if (!PlayerRuntimeIdentityUtility.MatchesTrackedCharacter(
                bootstrap,
                trackedPlayer,
                e.Target,
                e.CharacterId))
            return;

        if (entriesRoot == null || entryTemplate == null)
            return;

        CleanupMissingEntries();

        GameplayNotificationEntryView entry = Instantiate(entryTemplate, entriesRoot);
        entry.gameObject.SetActive(true);
        entry.Initialize(e.Message, GetCategoryColor(e.Category));
        activeEntries.Add(entry);

        TrimOverflow();
        RebuildLayout(false);
    }

    private void OnMapTransitionCompleted(MapTransitionCompletedEvent e)
    {
        ResolveRuntimeContext();

        if (e.Player != null && e.Player.IsLocalPlayer)
            trackedPlayer = e.Player;
        else if (!PlayerRuntimeIdentityUtility.MatchesTrackedCharacter(
                     bootstrap,
                     trackedPlayer,
                     e.Player,
                     e.CharacterId))
            return;

        CleanupMissingEntries();
    }

    private Color GetCategoryColor(GameplayNotificationCategory category)
    {
        return category switch
        {
            GameplayNotificationCategory.Mesos => mesosColor,
            GameplayNotificationCategory.Experience => experienceColor,
            GameplayNotificationCategory.Kill => killColor,
            GameplayNotificationCategory.System => systemColor,
            _ => itemColor
        };
    }

    private void TrimOverflow()
    {
        while (activeEntries.Count > maxVisibleEntries)
        {
            GameplayNotificationEntryView oldestEntry = activeEntries[0];
            activeEntries.RemoveAt(0);

            if (oldestEntry != null)
                Destroy(oldestEntry.gameObject);
        }
    }

    private bool CleanupMissingEntries()
    {
        bool didRemoveAny = false;

        for (int index = activeEntries.Count - 1; index >= 0; index--)
        {
            if (activeEntries[index] == null)
            {
                activeEntries.RemoveAt(index);
                didRemoveAny = true;
            }
        }

        return didRemoveAny;
    }

    private void RebuildLayout(bool instant)
    {
        for (int index = 0; index < activeEntries.Count; index++)
        {
            GameplayNotificationEntryView entry = activeEntries[index];
            if (entry == null)
                continue;

            int stackIndexFromBottom = activeEntries.Count - 1 - index;
            Vector2 targetPosition = baseEntryPosition + Vector2.up * (stackIndexFromBottom * entrySpacing);
            entry.SetTargetPosition(targetPosition, instant);
        }
    }

    private void ResolveRuntimeContext()
    {
        bootstrap = GameBootstrap.FindReadyBootstrap(bootstrap);

        if (trackedPlayer != null && trackedPlayer.IsLocalPlayer && trackedPlayer.gameObject.scene.IsValid())
            return;

        trackedPlayer = WorldRuntimeSceneUtility.FindLocalPlayer(FindObjectsInactive.Include);
    }
}
