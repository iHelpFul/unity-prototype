using UnityEngine;

public class EnemyAnimationController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private EnemyAI enemyAI;

    private readonly int speedHash = Animator.StringToHash("Speed");
    private readonly int attackHash = Animator.StringToHash("Attack");
    private readonly int hitHash = Animator.StringToHash("Hit");
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
}
