using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CharacterAppearanceData
{
    public string HeadId = "Head01_Male";
    public string HairId = "Hair01";
    public string EyesId = "Eye01";
    public string MouthId = "Mouth01";
    public string OutfitId = "Body01";
    public string WeaponRightId = "None";
    public string HatId = "None";
    public string CapeId = "None";
    public string HornsId = "None";
    public string AccessoryId = "None";
    public string NinjaMaskId = "None";
    public string MustacheId = "None";
    public Color HairColor = Color.white;
    public Color SkinColor = Color.white;
    public Color WeaponRightColor = Color.white;
    public Color PrimaryClothingColor = Color.white;
    public Color SecondaryClothingColor = Color.white;

    public CharacterAppearanceData Clone()
    {
        return new CharacterAppearanceData
        {
            HeadId = HeadId,
            HairId = HairId,
            EyesId = EyesId,
            MouthId = MouthId,
            OutfitId = OutfitId,
            WeaponRightId = WeaponRightId,
            HatId = HatId,
            CapeId = CapeId,
            HornsId = HornsId,
            AccessoryId = AccessoryId,
            NinjaMaskId = NinjaMaskId,
            MustacheId = MustacheId,
            HairColor = HairColor,
            SkinColor = SkinColor,
            WeaponRightColor = WeaponRightColor,
            PrimaryClothingColor = PrimaryClothingColor,
            SecondaryClothingColor = SecondaryClothingColor
        };
    }
}

[System.Serializable]
public class CharacterAppearanceOption
{
    public string Id;
    public string DisplayName;
}

[CreateAssetMenu(menuName = "Game Data/Characters/Appearance Catalog")]
public class CharacterAppearanceCatalogAsset : ScriptableObject
{
    [SerializeField] private List<CharacterAppearanceOption> headOptions = new List<CharacterAppearanceOption>();
    [SerializeField] private List<CharacterAppearanceOption> hairOptions = new List<CharacterAppearanceOption>();
    [SerializeField] private List<CharacterAppearanceOption> eyeOptions = new List<CharacterAppearanceOption>();
    [SerializeField] private List<CharacterAppearanceOption> mouthOptions = new List<CharacterAppearanceOption>();
    [SerializeField] private List<CharacterAppearanceOption> outfitOptions = new List<CharacterAppearanceOption>();
    [SerializeField] private List<CharacterAppearanceOption> weaponRightOptions = new List<CharacterAppearanceOption>();
    [SerializeField] private List<CharacterAppearanceOption> hatOptions = new List<CharacterAppearanceOption>();
    [SerializeField] private List<CharacterAppearanceOption> capeOptions = new List<CharacterAppearanceOption>();
    [SerializeField] private List<CharacterAppearanceOption> hornsOptions = new List<CharacterAppearanceOption>();
    [SerializeField] private List<CharacterAppearanceOption> accessoryOptions = new List<CharacterAppearanceOption>();
    [SerializeField] private List<CharacterAppearanceOption> ninjaMaskOptions = new List<CharacterAppearanceOption>();
    [SerializeField] private List<CharacterAppearanceOption> mustacheOptions = new List<CharacterAppearanceOption>();
    [SerializeField] private List<Color> paletteOptions = new List<Color>();

    public IReadOnlyList<CharacterAppearanceOption> HeadOptions => headOptions;
    public IReadOnlyList<CharacterAppearanceOption> HairOptions => hairOptions;
    public IReadOnlyList<CharacterAppearanceOption> EyeOptions => eyeOptions;
    public IReadOnlyList<CharacterAppearanceOption> MouthOptions => mouthOptions;
    public IReadOnlyList<CharacterAppearanceOption> OutfitOptions => outfitOptions;
    public IReadOnlyList<CharacterAppearanceOption> WeaponRightOptions => weaponRightOptions;
    public IReadOnlyList<CharacterAppearanceOption> HatOptions => hatOptions;
    public IReadOnlyList<CharacterAppearanceOption> CapeOptions => capeOptions;
    public IReadOnlyList<CharacterAppearanceOption> HornsOptions => hornsOptions;
    public IReadOnlyList<CharacterAppearanceOption> AccessoryOptions => accessoryOptions;
    public IReadOnlyList<CharacterAppearanceOption> NinjaMaskOptions => ninjaMaskOptions;
    public IReadOnlyList<CharacterAppearanceOption> MustacheOptions => mustacheOptions;
    public IReadOnlyList<Color> PaletteOptions => paletteOptions;

    public void Initialize(
        IReadOnlyList<CharacterAppearanceOption> newHeadOptions,
        IReadOnlyList<CharacterAppearanceOption> newHairOptions,
        IReadOnlyList<CharacterAppearanceOption> newEyeOptions,
        IReadOnlyList<CharacterAppearanceOption> newMouthOptions,
        IReadOnlyList<CharacterAppearanceOption> newOutfitOptions,
        IReadOnlyList<CharacterAppearanceOption> newWeaponRightOptions,
        IReadOnlyList<CharacterAppearanceOption> newHatOptions,
        IReadOnlyList<CharacterAppearanceOption> newCapeOptions,
        IReadOnlyList<CharacterAppearanceOption> newHornsOptions,
        IReadOnlyList<CharacterAppearanceOption> newAccessoryOptions,
        IReadOnlyList<CharacterAppearanceOption> newNinjaMaskOptions,
        IReadOnlyList<CharacterAppearanceOption> newMustacheOptions,
        IReadOnlyList<Color> newPaletteOptions)
    {
        headOptions = CloneOptions(newHeadOptions);
        hairOptions = CloneOptions(newHairOptions);
        eyeOptions = CloneOptions(newEyeOptions);
        mouthOptions = CloneOptions(newMouthOptions);
        outfitOptions = CloneOptions(newOutfitOptions);
        weaponRightOptions = CloneOptions(newWeaponRightOptions);
        hatOptions = CloneOptions(newHatOptions);
        capeOptions = CloneOptions(newCapeOptions);
        hornsOptions = CloneOptions(newHornsOptions);
        accessoryOptions = CloneOptions(newAccessoryOptions);
        ninjaMaskOptions = CloneOptions(newNinjaMaskOptions);
        mustacheOptions = CloneOptions(newMustacheOptions);
        paletteOptions = newPaletteOptions != null ? new List<Color>(newPaletteOptions) : new List<Color>();
        Sanitize();
    }

    private void OnValidate()
    {
        Sanitize();
    }

    private void Sanitize()
    {
        headOptions = NormalizeOptions(headOptions);
        hairOptions = NormalizeOptions(hairOptions);
        eyeOptions = NormalizeOptions(eyeOptions);
        mouthOptions = NormalizeOptions(mouthOptions);
        outfitOptions = NormalizeOptions(outfitOptions);
        weaponRightOptions = NormalizeOptions(weaponRightOptions);
        hatOptions = NormalizeOptions(hatOptions);
        capeOptions = NormalizeOptions(capeOptions);
        hornsOptions = NormalizeOptions(hornsOptions);
        accessoryOptions = NormalizeOptions(accessoryOptions);
        ninjaMaskOptions = NormalizeOptions(ninjaMaskOptions);
        mustacheOptions = NormalizeOptions(mustacheOptions);

        if (paletteOptions == null)
            paletteOptions = new List<Color>();
    }

    private static List<CharacterAppearanceOption> CloneOptions(IReadOnlyList<CharacterAppearanceOption> source)
    {
        List<CharacterAppearanceOption> copy = new List<CharacterAppearanceOption>();

        if (source == null)
            return copy;

        for (int index = 0; index < source.Count; index++)
        {
            CharacterAppearanceOption option = source[index];
            if (option == null)
                continue;

            copy.Add(new CharacterAppearanceOption
            {
                Id = option.Id,
                DisplayName = option.DisplayName
            });
        }

        return copy;
    }

    private static List<CharacterAppearanceOption> NormalizeOptions(List<CharacterAppearanceOption> options)
    {
        if (options == null)
            return new List<CharacterAppearanceOption>();

        HashSet<string> uniqueIds = new HashSet<string>();
        List<CharacterAppearanceOption> normalized = new List<CharacterAppearanceOption>();

        for (int index = 0; index < options.Count; index++)
        {
            CharacterAppearanceOption option = options[index];
            if (option == null || string.IsNullOrWhiteSpace(option.Id))
                continue;

            string normalizedId = option.Id.Trim();
            if (!uniqueIds.Add(normalizedId))
                continue;

            normalized.Add(new CharacterAppearanceOption
            {
                Id = normalizedId,
                DisplayName = string.IsNullOrWhiteSpace(option.DisplayName) ? normalizedId : option.DisplayName.Trim()
            });
        }

        return normalized;
    }
}
