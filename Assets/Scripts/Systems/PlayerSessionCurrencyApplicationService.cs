using System;

public class PlayerSessionCurrencyApplicationService
{
    private readonly PlayerSessionEventPublisher eventPublisher;
    private readonly Func<PlayerRuntimeData> getPlayerData;
    private readonly Action savePlayer;

    public PlayerSessionCurrencyApplicationService(
        PlayerSessionEventPublisher eventPublisher,
        Func<PlayerRuntimeData> getPlayerData,
        Action savePlayer)
    {
        this.eventPublisher = eventPublisher;
        this.getPlayerData = getPlayerData;
        this.savePlayer = savePlayer;
    }

    public bool HasActivePlayerData => ResolvePlayerData() != null;
    public int CurrentMesos => ResolvePlayerData()?.Mesos ?? 0;

    public void AddMesos(PlayerCharacter player, int amount)
    {
        if (amount <= 0)
            return;

        PlayerRuntimeData playerData = ResolvePlayerData();
        if (playerData == null)
            return;

        playerData.Mesos += amount;
        PersistCurrencyChange(playerData, player);
    }

    public bool TrySpendMesos(PlayerCharacter player, int amount)
    {
        if (amount <= 0)
            return false;

        PlayerRuntimeData playerData = ResolvePlayerData();
        if (playerData == null || playerData.Mesos < amount)
            return false;

        playerData.Mesos -= amount;
        PersistCurrencyChange(playerData, player);
        return true;
    }

    private PlayerRuntimeData ResolvePlayerData()
    {
        return getPlayerData != null ? getPlayerData() : null;
    }

    private void PersistCurrencyChange(PlayerRuntimeData playerData, PlayerCharacter player)
    {
        eventPublisher?.PublishCurrencyChanged(playerData, player);
        savePlayer?.Invoke();
    }
}
