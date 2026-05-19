using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class NpcJobAdvancement : MonoBehaviour
{
    [SerializeField, Min(1)] private int requiredLevel = 10;
    [SerializeField] private List<PlayerJobType> offeredJobs = new List<PlayerJobType>
    {
        PlayerJobType.Vanguard,
        PlayerJobType.Shade,
        PlayerJobType.Arcanist
    };

    public int RequiredLevel => Mathf.Max(1, requiredLevel);
    public IReadOnlyList<PlayerJobType> OfferedJobs => offeredJobs;

    public bool OffersJob(PlayerJobType jobType)
    {
        if (jobType == PlayerJobType.Novice)
            return false;

        for (int index = 0; index < offeredJobs.Count; index++)
        {
            if (offeredJobs[index] == jobType)
                return true;
        }

        return false;
    }

    private void OnValidate()
    {
        requiredLevel = Mathf.Max(1, requiredLevel);

        if (offeredJobs == null)
            offeredJobs = new List<PlayerJobType>();

        HashSet<PlayerJobType> seenJobs = new HashSet<PlayerJobType>();
        List<PlayerJobType> normalizedJobs = new List<PlayerJobType>();

        for (int index = 0; index < offeredJobs.Count; index++)
        {
            PlayerJobType jobType = offeredJobs[index];
            if (jobType == PlayerJobType.Novice || !seenJobs.Add(jobType))
                continue;

            normalizedJobs.Add(jobType);
        }

        if (normalizedJobs.Count == 0)
        {
            normalizedJobs.Add(PlayerJobType.Vanguard);
            normalizedJobs.Add(PlayerJobType.Shade);
            normalizedJobs.Add(PlayerJobType.Arcanist);
        }

        offeredJobs = normalizedJobs;
    }
}

