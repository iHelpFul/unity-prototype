using UnityEngine;

public class EnemyAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private EnemyAI enemyAI;

    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int attackHash = Animator.StringToHash("Attack");
    private readonly int hitHash = Animator.StringToHash("Hit");
    private readonly int breakHash = Animator.StringToHash("Break");
    private readonly int dieHash = Animator.StringToHash("Die");

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (enemyAI == null)
            enemyAI = GetComponent<EnemyAI>();
    }

    public void SetSpeed(float speed)
    {
        if (animator != null)
            animator.SetFloat(speedHash, speed);
    }

    public void PlayAttack()
    {
        if (animator != null)
            animator.SetTrigger(attackHash);
    }

    public void PlayHit()
    {
        if (animator != null)
            animator.SetTrigger(hitHash);
    }

    public void PlayBreak()
    {
        if (animator == null)
            return;

        if (HasTriggerParameter(breakHash))
        {
            animator.SetTrigger(breakHash);
            return;
        }

        animator.SetTrigger(hitHash);
    }

    public void PlayDie()
    {
        if (animator != null)
            animator.SetTrigger(dieHash);
    }

    public void ResetToIdle()
    {
        if (animator == null)
            return;

        animator.Rebind();
        animator.Update(0f);
    }

    public void OnAttackHitFrame()
    {
        enemyAI?.OnAttackHitFrame();
    }

    public void OnAttackAnimationComplete()
    {
        enemyAI?.OnAttackAnimationComplete();
    }

    private bool HasTriggerParameter(int parameterHash)
    {
        if (animator == null)
            return false;

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int index = 0; index < parameters.Length; index++)
        {
            AnimatorControllerParameter parameter = parameters[index];
            if (parameter.type == AnimatorControllerParameterType.Trigger
                && parameter.nameHash == parameterHash)
            {
                return true;
            }
        }

        return false;
    }
}
