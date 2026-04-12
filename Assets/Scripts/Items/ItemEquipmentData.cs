using UnityEngine;

[System.Serializable]
public class ItemStatModifierData
{
    public int Might;
    public int Precision;
    public int Arcane;
    public int Finesse;

    public int WeaponPower;
    public int MaxHP;
    public int MaxMP;
    public int HitRate;

    public ItemStatModifierData Clone()
    {
        return new ItemStatModifierData
        {
            Might = Might,
            Precision = Precision,
            Arcane = Arcane,
            Finesse = Finesse,
            WeaponPower = WeaponPower,
            MaxHP = MaxHP,
            MaxMP = MaxMP,
            HitRate = HitRate
        };
    }

    public void Add(ItemStatModifierData other)
    {
        if (other == null)
            return;

        Might += other.Might;
        Precision += other.Precision;
        Arcane += other.Arcane;
        Finesse += other.Finesse;
        WeaponPower += other.WeaponPower;
        MaxHP += other.MaxHP;
        MaxMP += other.MaxMP;
        HitRate += other.HitRate;
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
