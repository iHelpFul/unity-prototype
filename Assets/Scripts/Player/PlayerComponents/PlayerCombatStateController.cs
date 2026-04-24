using UnityEngine;

public class PlayerCombatStateController : MonoBehaviour
{
    [Header("Combat Runtime")]
    [SerializeField] private int maxMomentumStacks = 3;

    private int momentumStacks;

    public int CurrentMomentumStacks => Mathf.Clamp(momentumStacks, 0, Mathf.Max(0, maxMomentumStacks));

    public void Initialize(PlayerCharacter ownerCharacter)
    {
        momentumStacks = 0;
    }

    public void GainMomentum(int amount)
    {
        int gainAmount = Mathf.Max(0, amount);
        if (gainAmount <= 0)
            return;

        momentumStacks = Mathf.Clamp(momentumStacks + gainAmount, 0, Mathf.Max(0, maxMomentumStacks));
    }

    public void ConsumeMomentum(int amount)
    {
        int consumeAmount = Mathf.Max(0, amount);
        if (consumeAmount <= 0)
            return;

        momentumStacks = Mathf.Clamp(momentumStacks - consumeAmount, 0, Mathf.Max(0, maxMomentumStacks));
    }

    public void ResetMomentum()
    {
        momentumStacks = 0;
    }
}
