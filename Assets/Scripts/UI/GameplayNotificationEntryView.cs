using TMPro;
using UnityEngine;

public class GameplayNotificationEntryView : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float lifetime = 2.4f;
    [SerializeField] private float fadeInDuration = 0.12f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float moveSmoothTime = 0.12f;

    private float elapsedTime;
    private Vector2 basePosition;
    private Vector2 targetPosition;
    private Vector2 positionVelocity;

    private void Awake()
    {
        rectTransform ??= transform as RectTransform;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (messageText == null)
            messageText = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private void OnEnable()
    {
        elapsedTime = 0f;
        positionVelocity = Vector2.zero;
        basePosition = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
        targetPosition = basePosition;
        canvasGroup.alpha = 0f;
    }

    public void Initialize(string message, Color color)
    {
        if (messageText != null)
        {
            messageText.text = message;
            messageText.color = color;
        }

        elapsedTime = 0f;
        positionVelocity = Vector2.zero;

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = basePosition;
            targetPosition = basePosition;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    public void SetTargetPosition(Vector2 position, bool instant = false)
    {
        targetPosition = position;

        if (rectTransform == null)
            return;

        if (instant)
        {
            rectTransform.anchoredPosition = position;
            positionVelocity = Vector2.zero;
        }
    }

    private void Update()
    {
        if (canvasGroup == null)
            return;

        elapsedTime += Time.unscaledDeltaTime;

        float alpha = 1f;
        if (elapsedTime < fadeInDuration)
        {
            alpha = fadeInDuration > 0f ? elapsedTime / fadeInDuration : 1f;
        }
        else if (elapsedTime > lifetime - fadeOutDuration)
        {
            float fadeOutTime = lifetime - elapsedTime;
            alpha = fadeOutDuration > 0f ? Mathf.Clamp01(fadeOutTime / fadeOutDuration) : 0f;
        }

        canvasGroup.alpha = alpha;

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = Vector2.SmoothDamp(
                rectTransform.anchoredPosition,
                targetPosition,
                ref positionVelocity,
                moveSmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
        }

        if (elapsedTime >= lifetime)
            Destroy(gameObject);
    }
}
