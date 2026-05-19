using System.Collections.Generic;
using UnityEngine;

public enum CombatElementInteractionType
{
    Neutral = 0,
    Weakness = 1,
    Resistance = 2
}

[System.Serializable]
public class CombatElementAffinityEntry
{
    [SerializeField] private CombatElementType sourceElement = CombatElementType.Fire;
    [SerializeField] private CombatElementType advantagedAgainst = CombatElementType.None;
    [SerializeField] private CombatElementType disadvantagedAgainst = CombatElementType.None;

    public CombatElementType SourceElement => sourceElement;
    public CombatElementType AdvantagedAgainst => advantagedAgainst;
    public CombatElementType DisadvantagedAgainst => disadvantagedAgainst;

    public void Sanitize()
    {
        if (!System.Enum.IsDefined(typeof(CombatElementType), sourceElement))
            sourceElement = CombatElementType.Fire;

        if (!System.Enum.IsDefined(typeof(CombatElementType), advantagedAgainst))
            advantagedAgainst = CombatElementType.None;

        if (!System.Enum.IsDefined(typeof(CombatElementType), disadvantagedAgainst))
            disadvantagedAgainst = CombatElementType.None;
    }
}

[CreateAssetMenu(menuName = "Game Data/Combat/Combat Element Rule Profile")]
public class CombatElementRuleProfile : ScriptableObject
{
    [SerializeField] private string profileId = "GlobalElements";
    [SerializeField] private string displayName = "Global Element Rules";
    [SerializeField] private float neutralMultiplier = 1f;
    [SerializeField] private float weaknessMultiplier = 1.25f;
    [SerializeField] private float resistanceMultiplier = 0.75f;
    [SerializeField] private List<CombatElementAffinityEntry> affinities = new List<CombatElementAffinityEntry>();

    public string ProfileId => profileId;
    public string DisplayName => displayName;
    public float NeutralMultiplier => Mathf.Max(0f, neutralMultiplier);
    public float WeaknessMultiplier => Mathf.Max(0f, weaknessMultiplier);
    public float ResistanceMultiplier => Mathf.Max(0f, resistanceMultiplier);
    public IReadOnlyList<CombatElementAffinityEntry> Affinities => affinities;

    public CombatElementInteractionType ResolveInteraction(
        CombatElementType attackElement,
        CombatElementType targetElement)
    {
        if (attackElement == CombatElementType.None || targetElement == CombatElementType.None)
            return CombatElementInteractionType.Neutral;

        if (attackElement == targetElement)
            return CombatElementInteractionType.Resistance;

        if (affinities == null || affinities.Count == 0)
            return CombatElementInteractionType.Neutral;

        for (int index = 0; index < affinities.Count; index++)
        {
            CombatElementAffinityEntry entry = affinities[index];
            if (entry == null || entry.SourceElement != attackElement)
                continue;

            if (entry.AdvantagedAgainst == targetElement)
                return CombatElementInteractionType.Weakness;

            if (entry.DisadvantagedAgainst == targetElement)
                return CombatElementInteractionType.Resistance;
        }

        return CombatElementInteractionType.Neutral;
    }

    public float ResolveMultiplier(
        CombatElementType attackElement,
        CombatElementType targetElement)
    {
        return ResolveInteraction(attackElement, targetElement) switch
        {
            CombatElementInteractionType.Weakness => WeaknessMultiplier,
            CombatElementInteractionType.Resistance => ResistanceMultiplier,
            _ => NeutralMultiplier
        };
    }

    private void OnValidate()
    {
        profileId = string.IsNullOrWhiteSpace(profileId) ? "GlobalElements" : profileId.Trim();
        displayName = string.IsNullOrWhiteSpace(displayName) ? "Global Element Rules" : displayName.Trim();
        neutralMultiplier = Mathf.Max(0f, neutralMultiplier);
        weaknessMultiplier = Mathf.Max(0f, weaknessMultiplier);
        resistanceMultiplier = Mathf.Max(0f, resistanceMultiplier);
        affinities ??= new List<CombatElementAffinityEntry>();

        for (int index = affinities.Count - 1; index >= 0; index--)
        {
            CombatElementAffinityEntry entry = affinities[index];
            if (entry == null)
            {
                affinities.RemoveAt(index);
                continue;
            }

            entry.Sanitize();
        }
    }
}

public static class CombatElementRuleDatabase
{
    private const string ResourcePath = "GameData/Combat/CombatElementRuleProfile";

    private static CombatElementRuleProfile cachedProfile;

    public static CombatElementRuleProfile GetProfile()
    {
        if (cachedProfile != null)
            return cachedProfile;

        cachedProfile = Resources.Load<CombatElementRuleProfile>(ResourcePath);
        return cachedProfile;
    }

    public static void ResetCache()
    {
        cachedProfile = null;
    }
}
