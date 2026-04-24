using System;
using System.Collections.Generic;

public class PlayerSessionPassiveApplicationService
{
    private readonly PlayerSessionPassiveService passiveService;
    private readonly Action savePlayer;

    public PlayerSessionPassiveApplicationService(
        PlayerSessionPassiveService passiveService,
        Action savePlayer)
    {
        this.passiveService = passiveService;
        this.savePlayer = savePlayer;
    }

    public IReadOnlyList<PlayerPassiveEntry> GetUnlockedPassives()
    {
        return passiveService.GetUnlockedPassives();
    }

    public bool TryUnlockOrSetPassiveLevel(PassiveDefinition passiveDefinition, int passiveLevel = 1)
    {
        if (!passiveService.TryUnlockOrSetPassiveLevel(passiveDefinition, passiveLevel))
            return false;

        savePlayer?.Invoke();
        return true;
    }
}
