using UnityEngine;

[System.Serializable]
public class ItemStatModifierData
{
    public int Strength;
    public int Dexterity;
    public int WeaponAttack;
    public int MaxHP;
    public int MaxMP;

    public ItemStatModifierData Clone()
    {
        return new ItemStatModifierData
        {
            Strength = Strength,
            Dexterity = Dexterity,
            WeaponAttack = WeaponAttack,
            MaxHP = MaxHP,
            MaxMP = MaxMP
        };
    }

    public void Add(ItemStatModifierData other)
    {
        if (other == null)
            return;

        Strength += other.Strength;
        Dexterity += other.Dexterity;
        WeaponAttack += other.WeaponAttack;
        MaxHP += other.MaxHP;
        MaxMP += other.MaxMP;
    }
}

[System.Serializable]
public class ItemEquipmentAppearanceData
{
    public string OutfitId;
    public string WeaponRightId;
    public string HatId;
    public string CapeId;
    public string HornsId;
    public string AccessoryId;
    public string NinjaMaskId;
    public string MustacheId;
    public bool ApplyWeaponRightColor;
    public Color WeaponRightColor = Color.white;
    public bool ApplyPrimaryClothingColor;
    public Color PrimaryClothingColor = Color.white;
    public bool ApplySecondaryClothingColor;
    public Color SecondaryClothingColor = Color.white;

    public ItemEquipmentAppearanceData Clone()
    {
        return new ItemEquipmentAppearanceData
        {
            OutfitId = OutfitId,
            WeaponRightId = WeaponRightId,
            HatId = HatId,
            CapeId = CapeId,
            HornsId = HornsId,
            AccessoryId = AccessoryId,
            NinjaMaskId = NinjaMaskId,
            MustacheId = MustacheId,
            ApplyWeaponRightColor = ApplyWeaponRightColor,
            WeaponRightColor = WeaponRightColor,
            ApplyPrimaryClothingColor = ApplyPrimaryClothingColor,
            PrimaryClothingColor = PrimaryClothingColor,
            ApplySecondaryClothingColor = ApplySecondaryClothingColor,
            SecondaryClothingColor = SecondaryClothingColor
        };
    }

    public void Merge(ItemEquipmentAppearanceData other)
    {
        if (other == null)
            return;

        if (!string.IsNullOrWhiteSpace(other.OutfitId))
            OutfitId = other.OutfitId.Trim();

        if (!string.IsNullOrWhiteSpace(other.WeaponRightId))
            WeaponRightId = other.WeaponRightId.Trim();

        if (!string.IsNullOrWhiteSpace(other.HatId))
            HatId = other.HatId.Trim();

        if (!string.IsNullOrWhiteSpace(other.CapeId))
            CapeId = other.CapeId.Trim();

        if (!string.IsNullOrWhiteSpace(other.HornsId))
            HornsId = other.HornsId.Trim();

        if (!string.IsNullOrWhiteSpace(other.AccessoryId))
            AccessoryId = other.AccessoryId.Trim();

        if (!string.IsNullOrWhiteSpace(other.NinjaMaskId))
            NinjaMaskId = other.NinjaMaskId.Trim();

        if (!string.IsNullOrWhiteSpace(other.MustacheId))
            MustacheId = other.MustacheId.Trim();

        if (other.ApplyWeaponRightColor)
        {
            ApplyWeaponRightColor = true;
            WeaponRightColor = other.WeaponRightColor;
        }

        if (other.ApplyPrimaryClothingColor)
        {
            ApplyPrimaryClothingColor = true;
            PrimaryClothingColor = other.PrimaryClothingColor;
        }

        if (other.ApplySecondaryClothingColor)
        {
            ApplySecondaryClothingColor = true;
            SecondaryClothingColor = other.SecondaryClothingColor;
        }
    }

    public void ApplyTo(CharacterAppearanceData appearance)
    {
        if (appearance == null)
            return;

        if (!string.IsNullOrWhiteSpace(OutfitId))
            appearance.OutfitId = OutfitId.Trim();

        if (!string.IsNullOrWhiteSpace(WeaponRightId))
            appearance.WeaponRightId = WeaponRightId.Trim();

        if (!string.IsNullOrWhiteSpace(HatId))
            appearance.HatId = HatId.Trim();

        if (!string.IsNullOrWhiteSpace(CapeId))
            appearance.CapeId = CapeId.Trim();

        if (!string.IsNullOrWhiteSpace(HornsId))
            appearance.HornsId = HornsId.Trim();

        if (!string.IsNullOrWhiteSpace(AccessoryId))
            appearance.AccessoryId = AccessoryId.Trim();

        if (!string.IsNullOrWhiteSpace(NinjaMaskId))
            appearance.NinjaMaskId = NinjaMaskId.Trim();

        if (!string.IsNullOrWhiteSpace(MustacheId))
            appearance.MustacheId = MustacheId.Trim();

        if (ApplyWeaponRightColor)
            appearance.WeaponRightColor = WeaponRightColor;

        if (ApplyPrimaryClothingColor)
            appearance.PrimaryClothingColor = PrimaryClothingColor;

        if (ApplySecondaryClothingColor)
            appearance.SecondaryClothingColor = SecondaryClothingColor;
    }

    public void Sanitize()
    {
        OutfitId = Normalize(OutfitId);
        WeaponRightId = Normalize(WeaponRightId);
        HatId = Normalize(HatId);
        CapeId = Normalize(CapeId);
        HornsId = Normalize(HornsId);
        AccessoryId = Normalize(AccessoryId);
        NinjaMaskId = Normalize(NinjaMaskId);
        MustacheId = Normalize(MustacheId);
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
