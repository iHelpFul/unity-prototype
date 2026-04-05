using UnityEngine;

public struct EnemyDiedEvent
{
    public Transform Enemy;
    public PlayerCharacter Killer;
    public EnemyType Type;
    public int ExpReward;
}
