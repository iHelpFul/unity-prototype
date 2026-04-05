using UnityEngine;
using System.Collections;

public class HitFeedbackSystem : MonoBehaviour
{
    private Coroutine hitStopCoroutine;
    private bool isWaiting;
    private void OnEnable()
    {
        EventBus.Subscribe<HitImpactEvent>(OnHitImpact);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<HitImpactEvent>(OnHitImpact);
    }

    private void OnHitImpact(HitImpactEvent e)
    {
        if (hitStopCoroutine != null)
        {
            StopCoroutine(hitStopCoroutine);
        }
        hitStopCoroutine = StartCoroutine(HitStopRoutine(e.Duration, e.TimeScale));
    }

    private IEnumerator HitStopRoutine(float duration, float timeScale)
    {
        //float originalTimeScale = Time.timeScale;

        Time.timeScale = timeScale;
        Time.fixedDeltaTime = 0.02f * timeScale;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = 0.02f;
        hitStopCoroutine = null;
    }
}
