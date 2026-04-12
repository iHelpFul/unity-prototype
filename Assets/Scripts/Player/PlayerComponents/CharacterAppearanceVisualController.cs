using System.Collections.Generic;
using UnityEngine;

public enum CharacterAppearanceColorChannel
{
    Hair,
    Skin,
    WeaponRight,
    PrimaryClothing,
    SecondaryClothing
}

[System.Serializable]
public enum CharacterAppearanceMaterialColorSlot
{
    None,
    Color01,
    Color02,
    Color03,
    Color04,
    Color05,
    Color06,
    Color07,
    Color08,
    Color09Skin
}

[System.Serializable]
public class CharacterAppearanceOptionBinding
{
    public string OptionId;
    public GameObject Root;
    public CharacterAppearanceMaterialColorSlot HairColorSlot;
    public CharacterAppearanceMaterialColorSlot SkinColorSlot;
    public CharacterAppearanceMaterialColorSlot WeaponRightColorSlot;
    public CharacterAppearanceMaterialColorSlot PrimaryColorSlot;
    public CharacterAppearanceMaterialColorSlot SecondaryColorSlot;
}

public class CharacterAppearanceVisualController : MonoBehaviour
{
    [Header("Option Roots")]
    [SerializeField] private CharacterAppearanceOptionBinding[] headOptions;
    [SerializeField] private CharacterAppearanceOptionBinding[] hairOptions;
    [SerializeField] private CharacterAppearanceOptionBinding[] eyeOptions;
    [SerializeField] private CharacterAppearanceOptionBinding[] mouthOptions;
    [SerializeField] private CharacterAppearanceOptionBinding[] outfitOptions;
    [SerializeField] private CharacterAppearanceOptionBinding[] weaponRightOptions;
    [SerializeField] private CharacterAppearanceOptionBinding[] hatOptions;
    [SerializeField] private CharacterAppearanceOptionBinding[] capeOptions;
    [SerializeField] private CharacterAppearanceOptionBinding[] hornsOptions;
    [SerializeField] private CharacterAppearanceOptionBinding[] accessoryOptions;
    [SerializeField] private CharacterAppearanceOptionBinding[] ninjaMaskOptions;
    [SerializeField] private CharacterAppearanceOptionBinding[] mustacheOptions;

    private readonly Dictionary<Renderer, Material[]> runtimeMaterials = new Dictionary<Renderer, Material[]>();

    public void ApplyAppearance(CharacterAppearanceData appearance)
    {
        if (appearance == null)
            return;

        CharacterAppearanceOptionBinding selectedHead = ApplyOptionSet(headOptions, appearance.HeadId);
        CharacterAppearanceOptionBinding selectedHair = ApplyOptionSet(hairOptions, appearance.HairId);
        ApplyOptionSet(eyeOptions, appearance.EyesId);
        ApplyOptionSet(mouthOptions, appearance.MouthId);
        CharacterAppearanceOptionBinding selectedOutfit = ApplyOptionSet(outfitOptions, appearance.OutfitId);
        CharacterAppearanceOptionBinding selectedWeaponRight = ApplyOptionSet(weaponRightOptions, appearance.WeaponRightId);
        ApplyOptionSet(hatOptions, appearance.HatId);
        ApplyOptionSet(capeOptions, appearance.CapeId);
        ApplyOptionSet(hornsOptions, appearance.HornsId);
        ApplyOptionSet(accessoryOptions, appearance.AccessoryId);
        ApplyOptionSet(ninjaMaskOptions, appearance.NinjaMaskId);
        ApplyOptionSet(mustacheOptions, appearance.MustacheId);

        ApplyColorToSelectedBinding(selectedHead, CharacterAppearanceColorChannel.Skin, appearance.SkinColor);
        ApplyColorToSelectedBinding(selectedOutfit, CharacterAppearanceColorChannel.Skin, appearance.SkinColor);
        ApplyColorToSelectedBinding(selectedHair, CharacterAppearanceColorChannel.Hair, appearance.HairColor);
        ApplyColorToSelectedBinding(selectedWeaponRight, CharacterAppearanceColorChannel.WeaponRight, appearance.WeaponRightColor);
        ApplyColorToSelectedBinding(selectedOutfit, CharacterAppearanceColorChannel.PrimaryClothing, appearance.PrimaryClothingColor);
        ApplyColorToSelectedBinding(selectedOutfit, CharacterAppearanceColorChannel.SecondaryClothing, appearance.SecondaryClothingColor);
    }

    public void ClearMaterialCache()
    {
        runtimeMaterials.Clear();
    }

    private CharacterAppearanceOptionBinding ApplyOptionSet(CharacterAppearanceOptionBinding[] bindings, string selectedOptionId)
    {
        if (bindings == null || bindings.Length == 0)
            return null;

        string normalizedSelectedId = NormalizeId(selectedOptionId);
        bool shouldDisableAll = IsNoneSelection(normalizedSelectedId);
        int selectedIndex = -1;

        for (int index = 0; index < bindings.Length; index++)
        {
            CharacterAppearanceOptionBinding binding = bindings[index];
            if (binding == null || binding.Root == null)
                continue;

            if (selectedIndex < 0 && MatchesOption(binding, normalizedSelectedId))
                selectedIndex = index;
        }

        if (!shouldDisableAll && selectedIndex < 0)
        {
            for (int index = 0; index < bindings.Length; index++)
            {
                if (bindings[index] != null && bindings[index].Root != null)
                {
                    selectedIndex = index;
                    break;
                }
            }
        }

        for (int index = 0; index < bindings.Length; index++)
        {
            CharacterAppearanceOptionBinding binding = bindings[index];
            if (binding?.Root != null)
                binding.Root.SetActive(index == selectedIndex);
        }

        return selectedIndex >= 0 ? bindings[selectedIndex] : null;
    }

    private void ApplyColorToSelectedBinding(CharacterAppearanceOptionBinding binding, CharacterAppearanceColorChannel channel, Color color)
    {
        if (binding?.Root == null)
            return;

        CharacterAppearanceMaterialColorSlot slot = ResolveSelectedBindingSlot(binding, channel);
        if (slot == CharacterAppearanceMaterialColorSlot.None)
            return;

        Renderer[] renderers = binding.Root.GetComponentsInChildren<Renderer>(true);
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Renderer renderer = renderers[rendererIndex];
            if (renderer == null)
                continue;

            Material[] materials = GetRuntimeMaterials(renderer);
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null)
                    continue;

                string propertyName = ResolveMaterialPropertyName(material, slot);
                if (material.HasProperty(propertyName))
                    material.SetColor(propertyName, color);
            }
        }
    }

    private static CharacterAppearanceMaterialColorSlot ResolveSelectedBindingSlot(CharacterAppearanceOptionBinding binding, CharacterAppearanceColorChannel channel)
    {
        switch (channel)
        {
            case CharacterAppearanceColorChannel.Hair:
                return binding.HairColorSlot;
            case CharacterAppearanceColorChannel.Skin:
                return binding.SkinColorSlot;
            case CharacterAppearanceColorChannel.WeaponRight:
                return binding.WeaponRightColorSlot;
            case CharacterAppearanceColorChannel.PrimaryClothing:
                return binding.PrimaryColorSlot;
            case CharacterAppearanceColorChannel.SecondaryClothing:
                return binding.SecondaryColorSlot;
            default:
                return CharacterAppearanceMaterialColorSlot.None;
        }
    }

    private Material[] GetRuntimeMaterials(Renderer renderer)
    {
        if (runtimeMaterials.TryGetValue(renderer, out Material[] cachedMaterials) && cachedMaterials != null)
            return cachedMaterials;

        Material[] materials = renderer.materials;
        runtimeMaterials[renderer] = materials;
        return materials;
    }

    private static string NormalizeId(string rawId)
    {
        return string.IsNullOrWhiteSpace(rawId) ? string.Empty : rawId.Trim();
    }

    private static bool IsNoneSelection(string normalizedId)
    {
        return string.IsNullOrWhiteSpace(normalizedId)
            || string.Equals(normalizedId, "None", System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesOption(CharacterAppearanceOptionBinding binding, string normalizedSelectedId)
    {
        if (binding == null)
            return false;

        string bindingId = NormalizeId(binding.OptionId);
        string rootName = binding.Root != null ? NormalizeId(binding.Root.name) : string.Empty;

        if (EquivalentId(bindingId, normalizedSelectedId))
            return true;

        if (EquivalentId(rootName, normalizedSelectedId))
            return true;

        return false;
    }

    private static bool EquivalentId(string left, string right)
    {
        if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
            return false;

        if (string.Equals(left, right, System.StringComparison.OrdinalIgnoreCase))
            return true;

        //if (ExtractTrailingNumber(left, out int leftNumber) && ExtractTrailingNumber(right, out int rightNumber))
           // return leftNumber == rightNumber;

        return false;
    }

    private static bool ExtractTrailingNumber(string value, out int number)
    {
        number = -1;
        if (string.IsNullOrEmpty(value))
            return false;

        int endIndex = value.Length - 1;
        while (endIndex >= 0 && !char.IsDigit(value[endIndex]))
            endIndex--;

        if (endIndex < 0)
            return false;

        int startIndex = endIndex;
        while (startIndex >= 0 && char.IsDigit(value[startIndex]))
            startIndex--;

        string numeric = value.Substring(startIndex + 1, endIndex - startIndex);
        return int.TryParse(numeric, out number);
    }

    private static string ResolveMaterialPropertyName(Material material, CharacterAppearanceMaterialColorSlot slot)
    {
        string[] candidates = GetShaderPropertyCandidates(slot);
        for (int index = 0; index < candidates.Length; index++)
        {
            string candidate = candidates[index];
            if (!string.IsNullOrWhiteSpace(candidate) && material != null && material.HasProperty(candidate))
                return candidate;
        }

        return string.Empty;
    }

    private static string[] GetShaderPropertyCandidates(CharacterAppearanceMaterialColorSlot slot)
    {
        switch (slot)
        {
            case CharacterAppearanceMaterialColorSlot.Color01:
                return new[] { "Color_E64BA0E", "_Color01" };
            case CharacterAppearanceMaterialColorSlot.Color02:
                return new[] { "Color_31457282", "_Color02" };
            case CharacterAppearanceMaterialColorSlot.Color03:
                return new[] { "Color_22DC93C2", "_Color03" };
            case CharacterAppearanceMaterialColorSlot.Color04:
                return new[] { "Color_B672769A", "_Color04" };
            case CharacterAppearanceMaterialColorSlot.Color05:
                return new[] { "Color_C5D962E7", "_Color05" };
            case CharacterAppearanceMaterialColorSlot.Color06:
                return new[] { "Color_593132F8", "_Color06" };
            case CharacterAppearanceMaterialColorSlot.Color07:
                return new[] { "_Color07" };
            case CharacterAppearanceMaterialColorSlot.Color08:
                return new[] { "_Color08" };
            case CharacterAppearanceMaterialColorSlot.Color09Skin:
                return new[] { "_Color09_Skin", "_Color09_SKIN" };
            case CharacterAppearanceMaterialColorSlot.None:
            default:
                return System.Array.Empty<string>();
        }
    }
}
