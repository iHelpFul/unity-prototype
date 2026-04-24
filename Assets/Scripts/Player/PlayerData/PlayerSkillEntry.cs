[System.Serializable]
public class PlayerSkillEntry
{
    public string SkillId;
    public int SkillLevel = 1;

    // One-time migration hook for saves created before ActionBarSlots became the source of truth.
    [UnityEngine.HideInInspector]
    [UnityEngine.Serialization.FormerlySerializedAs("AssignedSlotIndex")]
    public int LegacyAssignedSlotIndex = -1;
}

[System.Serializable]
public class PlayerPassiveEntry
{
    public string PassiveId;
    public int PassiveLevel = 1;
}
