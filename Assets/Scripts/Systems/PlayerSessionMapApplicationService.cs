using System;

public class PlayerSessionMapApplicationService
{
    private readonly PlayerSessionMapStateService mapStateService;
    private readonly Func<PlayerRuntimeData> getPlayerData;
    private readonly Action savePlayer;

    public PlayerSessionMapApplicationService(
        PlayerSessionMapStateService mapStateService,
        Func<PlayerRuntimeData> getPlayerData,
        Action savePlayer)
    {
        this.mapStateService = mapStateService;
        this.getPlayerData = getPlayerData;
        this.savePlayer = savePlayer;
    }

    public void SetPendingMapTransition(string targetMapId, string targetSpawnId)
    {
        PersistMapStateChange(() => mapStateService.SetPendingTransition(targetMapId, targetSpawnId));
    }

    public void CompleteMapTransition(string targetMapId, string targetSpawnId)
    {
        PersistMapStateChange(() => mapStateService.CompleteTransition(targetMapId, targetSpawnId));
    }

    public void ClearPendingMapTransition()
    {
        PersistMapStateChange(() => mapStateService.ClearPendingTransition());
    }

    private void PersistMapStateChange(Action mutation)
    {
        if (ResolvePlayerData() == null || mutation == null)
            return;

        mutation();
        savePlayer?.Invoke();
    }

    private PlayerRuntimeData ResolvePlayerData()
    {
        return getPlayerData != null ? getPlayerData() : null;
    }
}
