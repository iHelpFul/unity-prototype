using System.Collections;
using UnityEngine;

public class KnockbackSystem : MonoBehaviour
{
    private void OnEnable()
    {
        EventBus.Subscribe<CharacterKnockbackEvent>(OnKnockback);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<CharacterKnockbackEvent>(OnKnockback);
    }

    private void OnKnockback(CharacterKnockbackEvent e)
    {
        StartCoroutine(ApplyKnockback(e));
    }

    private IEnumerator ApplyKnockback(CharacterKnockbackEvent e)
    {
        CharacterController controller = e.Target.GetComponent<CharacterController>();
        EnemyAI enemyAI = e.Target.GetComponent<EnemyAI>();

        if (controller == null)
            yield break;

        enemyAI?.NotifyExternalKnockback(e.Duration);

        Vector3 startPosition = e.Target.position;
        float duration = Mathf.Max(0.01f, e.Duration);
        float height = 0.4f;
        float timer = 0f;
        float direction = e.DirectionX;
        float force = e.Force;

        while (timer < duration)
        {
            if (controller == null)
                yield break;

            timer += Time.deltaTime;
            float t = timer / duration;

            float xOffset = Mathf.Lerp(0f, direction * force, t);
            float yOffset = 4f * height * t * (1f - t);
            float targetX = startPosition.x + xOffset;

            if (enemyAI != null)
                targetX = enemyAI.ClampHorizontalToMovementBounds(targetX);

            Vector3 targetPosition = new Vector3(
                targetX,
                startPosition.y + yOffset,
                startPosition.z);

            Vector3 move = targetPosition - e.Target.position;
            controller.Move(move);

            yield return null;
        }

        if (controller == null)
            yield break;

        float finalX = startPosition.x + direction * force;
        if (enemyAI != null)
            finalX = enemyAI.ClampHorizontalToMovementBounds(finalX);

        Vector3 finalPosition = new Vector3(
            finalX,
            startPosition.y,
            startPosition.z);

        Vector3 correction = finalPosition - e.Target.position;
        controller.Move(correction);
    }
}
