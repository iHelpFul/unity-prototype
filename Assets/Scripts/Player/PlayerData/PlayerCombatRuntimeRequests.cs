public sealed class PendingSkillCastRequest
{
    public PlayerSkillDefinition Definition;
    public EnemyHealth LockedTarget;
    public AttackPayload Payload;
    public int ResolvedSkillLevel;
    public CommittedEnemyHitPacket[] CommittedHitPackets;
    public bool CommitAsBurstSkill;
}

public sealed class PendingBurstReleaseRequest
{
    public PlayerSkillDefinition Definition;
    public int ResolvedSkillLevel;
    public AttackPayload Payload;
}
