using UnityEngine;
using System.Collections;

public class HitFeedbackSystem : MonoBehaviour
{
    [SerializeField] private bool enableHitStop;

    private Coroutine hitStopCoroutine;

    private void OnEnable()
    {
        EventBus.Subscribe<HitImpactEvent>(OnHitImpact);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<HitImpactEvent>(OnHitImpact);
        ResetTimeScale();
    }

    private void OnHitImpact(HitImpactEvent e)
    {
        if (!enableHitStop || e.Duration <= 0f || e.TimeScale <= 0f)
            return;

        if (hitStopCoroutine != null)
        {
            StopCoroutine(hitStopCoroutine);
        }
        hitStopCoroutine = StartCoroutine(HitStopRoutine(e.Duration, e.TimeScale));
    }

    private IEnumerator HitStopRoutine(float duration, float timeScale)
    {
        Time.timeScale = timeScale;
        Time.fixedDeltaTime = 0.02f * timeScale;

        yield return new WaitForSecondsRealtime(duration);

        ResetTimeScale(stopActiveCoroutine: false);
        hitStopCoroutine = null;
    }

    private void ResetTimeScale(bool stopActiveCoroutine = true)
    {
        if (stopActiveCoroutine && hitStopCoroutine != null)
        {
            StopCoroutine(hitStopCoroutine);
            hitStopCoroutine = null;
        }

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}
