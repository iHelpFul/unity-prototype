public class PlayerProgressionModule
{
    private readonly PlayerRuntimeData data;
    private readonly PlayerCharacter owner;
    private readonly GameBootstrap bootstrap;

    public PlayerProgressionModule(PlayerRuntimeData runtimeData, PlayerCharacter ownerCharacter, GameBootstrap sessionBootstrap)
    {
        data = runtimeData;
        owner = ownerCharacter;
        bootstrap = sessionBootstrap;
    }

    public void AddExp(int amount)
    {
        if (data == null || amount <= 0)
            return;

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
            RequiredExp = data.RequiredExp
        });

        if (didLevelUp)
            bootstrap?.PublishJobState(owner);

        bootstrap?.SavePlayer();
    }

    private void LevelUp()
    {
        data.Level++;
        data.RequiredExp = CalculateRequiredExp(data.Level);
        data.HasPendingJobAdvancement = PlayerJobCombatProfiles.IsJobAdvancementAvailable(data);

        ApplyLevelGrowth();

        EventBus.Publish(new PlayerLevelUpEvent
        {
            Target = owner,
            CharacterId = owner != null
                ? owner.CharacterId
                : PlayerRuntimeIdentityUtility.ResolveCharacterId(bootstrap, null),
            NewLevel = data.Level
        });
    }

    private void ApplyLevelGrowth()
    {
        data.Strength += 2;
        data.Dexterity += 1;

        data.MaxHP += 20;
        data.MaxMP += 10;

        if (bootstrap != null)
        {
            ItemStatModifierData equipmentBonuses = bootstrap.GetEquipmentStatBonuses();
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
            bootstrap.PublishSessionState(owner);
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

    private int CalculateRequiredExp(int level)
    {
        if (level <= 1)
            return 50;

        return 50 + ((level - 1) * 25);
    }
}
