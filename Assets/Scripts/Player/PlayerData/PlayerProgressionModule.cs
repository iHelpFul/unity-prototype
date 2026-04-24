using UnityEngine;

public class PlayerProgressionModule
{
    private readonly PlayerRuntimeData data;
    private readonly PlayerCharacter owner;
    private readonly GameBootstrap bootstrap;

    public PlayerProgressionModule(
        PlayerRuntimeData runtimeData,
        PlayerCharacter ownerCharacter,
        GameBootstrap sessionBootstrap)
    {
        data = runtimeData;
        owner = ownerCharacter;
        bootstrap = sessionBootstrap;
    }

    public int GetUnspentStatPoints()
    {
        return data != null ? data.UnspentStatPoints : 0;
    }

    public bool TrySpendStatPoints(PlayerProgressionStatType statType, int points)
    {
        if (data == null || points <= 0)
            return false;

        if (data.UnspentStatPoints < points)
            return false;

        if (!TryAddToStat(statType, points))
            return false;

        data.UnspentStatPoints -= points;
        PublishProgressionState();
        bootstrap?.CharacterSession?.Save();

        return true;
    }

    public void AddExp(int amount)
    {
        if (data == null || amount <= 0)
            return;

        PlayerProgressionRules.RefreshDerivedState(data);

        string characterId = owner != null
            ? owner.CharacterId
            : PlayerRuntimeIdentityUtility.ResolveCharacterId(bootstrap, null);

        data.CurrentExp += amount;
        bool didLevelUp = false;

        while (data.CurrentExp >= data.RequiredExp)
        {
            data.CurrentExp -= data.RequiredExp;
            LevelUp();
            didLevelUp = true;
        }

        EventBus.Publish(new PlayerExpChangedEvent
        {
            Target = owner,
            CharacterId = characterId,
            CurrentExp = data.CurrentExp,
            RequiredExp = data.RequiredExp,
            UnspentStatPoints = data.UnspentStatPoints,
            UnspentSkillPoints = data.UnspentSkillPoints
        });

        if (didLevelUp)
            bootstrap?.JobSession?.PublishJobState(owner);

        bootstrap?.CharacterSession?.Save();
    }

    private void LevelUp()
    {
        data.Level++;
        PlayerProgressionRules.RefreshDerivedState(data);
        int awardedStatPoints = PlayerProgressionRules.GetStatPointsAwardedPerLevelUp();
        int awardedSkillPoints = PlayerProgressionRules.GetSkillPointsAwardedPerLevelUp();
        data.UnspentStatPoints += awardedStatPoints;
        data.UnspentSkillPoints += awardedSkillPoints;

        ApplyLevelGrowth();

        EventBus.Publish(new PlayerLevelUpEvent
        {
            Target = owner,
            CharacterId = owner != null
                ? owner.CharacterId
                : PlayerRuntimeIdentityUtility.ResolveCharacterId(bootstrap, null),
            NewLevel = data.Level,
            UnspentStatPoints = data.UnspentStatPoints,
            UnspentSkillPoints = data.UnspentSkillPoints,
            StatPointsAwarded = awardedStatPoints,
            SkillPointsAwarded = awardedSkillPoints
        });

        PublishProgressionState();
    }

    private void ApplyLevelGrowth()
    {
        data.MaxHP += PlayerProgressionRules.GetMaxHpGainPerLevel();
        data.MaxMP += PlayerProgressionRules.GetMaxMpGainPerLevel();

        if (bootstrap != null)
        {
            ItemStatModifierData equipmentBonuses = bootstrap.EquipmentSession != null
                ? bootstrap.EquipmentSession.GetEquipmentStatBonuses()
                : new ItemStatModifierData();
            data.CurrentHP = data.MaxHP + equipmentBonuses.MaxHP;
            data.CurrentMP = data.MaxMP + equipmentBonuses.MaxMP;
        }
        else
        {
            data.CurrentHP = data.MaxHP;
            data.CurrentMP = data.MaxMP;
        }

        if (bootstrap != null && owner != null)
        {
            bootstrap.CharacterSession?.PublishSessionState(owner);
            return;
        }

        EventBus.Publish(new PlayerHealthChangedEvent
        {
            Target = owner,
            CharacterId = owner != null
                ? owner.CharacterId
                : PlayerRuntimeIdentityUtility.ResolveCharacterId(bootstrap, null),
            CurrentHP = data.CurrentHP,
            MaxHP = data.MaxHP
        });

        EventBus.Publish(new PlayerManaChangedEvent
        {
            Target = owner,
            CharacterId = owner != null
                ? owner.CharacterId
                : PlayerRuntimeIdentityUtility.ResolveCharacterId(bootstrap, null),
            CurrentMP = data.CurrentMP,
            MaxMP = data.MaxMP
        });
    }

    private void PublishProgressionState()
    {
        if (data == null || owner == null)
            return;

        EventBus.Publish(new PlayerExpChangedEvent
        {
            Target = owner,
            CharacterId = owner != null ? owner.CharacterId : PlayerRuntimeIdentityUtility.ResolveCharacterId(bootstrap, null),
            UnspentStatPoints = data.UnspentStatPoints,
            UnspentSkillPoints = data.UnspentSkillPoints,
            CurrentExp = data.CurrentExp,
            RequiredExp = data.RequiredExp
        });
    }

    private bool TryAddToStat(PlayerProgressionStatType statType, int points)
    {
        switch (statType)
        {
            case PlayerProgressionStatType.Might:
                data.Might += points;
                return true;
            case PlayerProgressionStatType.Precision:
                data.Precision += points;
                return true;
            case PlayerProgressionStatType.Arcane:
                data.Arcane += points;
                return true;
            case PlayerProgressionStatType.Finesse:
                data.Finesse += points;
                return true;
            case PlayerProgressionStatType.HitRate:
                data.HitRate += points;
                return true;
            default:
                return false;
        }
    }
}
