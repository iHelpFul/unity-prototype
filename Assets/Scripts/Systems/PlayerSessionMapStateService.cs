public class PlayerSessionMapStateService
{
    private PlayerRuntimeData data;

    public void SetRuntimeData(PlayerRuntimeData runtimeData)
    {
        data = runtimeData;
    }

    public void SetPendingTransition(string targetMapId, string targetSpawnId)
    {
        if (data == null)
            return;

        data.PendingMapId = MapRegistry.NormalizeMapId(targetMapId);
        data.PendingSpawnId = SceneSpawnPoint.NormalizeSpawnId(targetSpawnId);
    }

    public void CompleteTransition(string targetMapId, string targetSpawnId)
    {
        if (data == null)
            return;

        data.CurrentMapId = MapRegistry.NormalizeMapId(targetMapId);
        data.LastSpawnId = SceneSpawnPoint.NormalizeSpawnId(targetSpawnId);
        data.PendingMapId = string.Empty;
        data.PendingSpawnId = string.Empty;
    }

    public void ClearPendingTransition()
    {
        if (data == null)
            return;

        data.PendingMapId = string.Empty;
        data.PendingSpawnId = string.Empty;
    }
}
