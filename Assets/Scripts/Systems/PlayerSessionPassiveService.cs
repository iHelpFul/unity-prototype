using System.Collections.Generic;

public class PlayerSessionPassiveService
{
    private PlayerRuntimeData data;

    public void SetRuntimeData(PlayerRuntimeData runtimeData)
    {
        data = runtimeData;
        PlayerPassiveRuntimeUtility.EnsureCollections(data);
    }

    public IReadOnlyList<PlayerPassiveEntry> GetUnlockedPassives()
    {
        return PlayerPassiveRuntimeUtility.GetUnlockedPassives(data);
    }

    public bool TryUnlockOrSetPassiveLevel(PassiveDefinition passiveDefinition, int passiveLevel = 1)
    {
        return PlayerPassiveRuntimeUtility.TryUnlockOrSetPassiveLevel(data, passiveDefinition, passiveLevel);
    }

    public void EnsureDefaultPassivesForJob(PlayerJobType jobType)
    {
        PlayerPassiveRuntimeUtility.EnsureDefaultPassivesForJob(data, jobType);
    }
}
