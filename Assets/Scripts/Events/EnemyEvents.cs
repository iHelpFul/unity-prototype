using UnityEngine;

public struct EnemyHealthChangedEvent
{
    public EnemyHealth Enemy;
    public int CurrentHP;
    public int MaxHP;
    public int ChangeAmount;
    public bool WasDamaged;
    public bool IsDead;
    public bool RevealOverhead;
}

public struct EnemyRespawnedEvent
{
    public EnemyHealth Enemy;
    public int CurrentHP;
    public int MaxHP;
}

public struct EnemyPressureChangedEvent
{
    public EnemyHealth Enemy;
    public float CurrentPressure;
    public float MaxPressure;
    public bool IsBroken;
}

public struct EnemyBreakStateChangedEvent
{
    public EnemyHealth Enemy;
    public bool IsBroken;
    public float RemainingDuration;
}

public struct EnemyDiedEvent
{
    public Transform Enemy;
    public PlayerCharacter Killer;
    public EnemyType Type;
    public int ExpReward;
    public float GaugeReward;
}
