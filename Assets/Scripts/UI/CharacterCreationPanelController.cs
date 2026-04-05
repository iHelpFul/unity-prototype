using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCreationPanelController : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TMP_InputField nicknameInput;
    [SerializeField] private TextMeshProUGUI headText;
    [SerializeField] private TextMeshProUGUI hairText;
    [SerializeField] private TextMeshProUGUI eyesText;
    [SerializeField] private TextMeshProUGUI mouthText;
    [SerializeField] private TextMeshProUGUI outfitText;
    [SerializeField] private TextMeshProUGUI weaponRightText;
    [SerializeField] private TextMeshProUGUI statusText;
    [Header("Color Picker")]
    [SerializeField] private CharacterCreationColorPickerPopup colorPickerPopup;
    [SerializeField] private Image hairColorPreview;
    [SerializeField] private Image skinColorPreview;
    [SerializeField] private Image weaponRightColorPreview;
    [SerializeField] private Image primaryColorPreview;
    [SerializeField] private Image secondaryColorPreview;

    private CharacterSelectionSnapshot activeSnapshot;
    private CharacterAppearanceData draftAppearance = new CharacterAppearanceData();
    private int targetSlotIndex = -1;
    private bool isOpen;

    private void Start()
    {
        SetPanelVisible(false);
        RefreshView();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<CharacterSelectionStateChangedEvent>(OnSelectionStateChanged);
        EventBus.Subscribe<CharacterFlowResultEvent>(OnFlowResult);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<CharacterSelectionStateChangedEvent>(OnSelectionStateChanged);
        EventBus.Unsubscribe<CharacterFlowResultEvent>(OnFlowResult);
    }

    public void OpenForSlot(int slotIndex)
    {
        targetSlotIndex = slotIndex;
        isOpen = true;
        SetPanelVisible(true);
        PublishPanelVisibilityChanged(true);
        ResetDraftState();
        ApplyDraftChange();
    }

    public void ClosePanel()
    {
        isOpen = false;
        targetSlotIndex = -1;
        CloseColorPicker();
        SetPanelVisible(false);
        PublishPanelVisibilityChanged(false);
    }

    public void CreateCharacter()
    {
        if (!isOpen || targetSlotIndex < 0)
            return;

        EventBus.Publish(new CharacterCreateRequestEvent
        {
            SlotIndex = targetSlotIndex,
            Nickname = nicknameInput != null ? nicknameInput.text : string.Empty,
            Appearance = draftAppearance != null ? draftAppearance.Clone() : new CharacterAppearanceData()
        });
    }

    public void PreviousHead() => CycleOption(CharacterAppearanceField.Head, -1);
    public void NextHead() => CycleOption(CharacterAppearanceField.Head, 1);
    public void PreviousHair() => CycleOption(CharacterAppearanceField.Hair, -1);
    public void NextHair() => CycleOption(CharacterAppearanceField.Hair, 1);
    public void PreviousEyes() => CycleOption(CharacterAppearanceField.Eyes, -1);
    public void NextEyes() => CycleOption(CharacterAppearanceField.Eyes, 1);
    public void PreviousMouth() => CycleOption(CharacterAppearanceField.Mouth, -1);
    public void NextMouth() => CycleOption(CharacterAppearanceField.Mouth, 1);
    public void PreviousOutfit() => CycleOption(CharacterAppearanceField.Outfit, -1);
    public void NextOutfit() => CycleOption(CharacterAppearanceField.Outfit, 1);
    public void PreviousWeaponRight() => CycleOption(CharacterAppearanceField.WeaponRight, -1);
    public void NextWeaponRight() => CycleOption(CharacterAppearanceField.WeaponRight, 1);
    public void PreviousHat() => CycleOption(CharacterAppearanceField.Hat, -1);
    public void NextHat() => CycleOption(CharacterAppearanceField.Hat, 1);
    public void PreviousCape() => CycleOption(CharacterAppearanceField.Cape, -1);
    public void NextCape() => CycleOption(CharacterAppearanceField.Cape, 1);
    public void PreviousHorns() => CycleOption(CharacterAppearanceField.Horns, -1);
    public void NextHorns() => CycleOption(CharacterAppearanceField.Horns, 1);
    public void PreviousAccessory() => CycleOption(CharacterAppearanceField.Accessory, -1);
    public void NextAccessory() => CycleOption(CharacterAppearanceField.Accessory, 1);
    public void PreviousNinjaMask() => CycleOption(CharacterAppearanceField.NinjaMask, -1);
    public void NextNinjaMask() => CycleOption(CharacterAppearanceField.NinjaMask, 1);
    public void PreviousMustache() => CycleOption(CharacterAppearanceField.Mustache, -1);
    public void NextMustache() => CycleOption(CharacterAppearanceField.Mustache, 1);

    public void PreviousHairColor() => OpenHairColorPicker();
    public void NextHairColor() => OpenHairColorPicker();
    public void PreviousEyeColor() => OpenSkinColorPicker();
    public void NextEyeColor() => OpenSkinColorPicker();
    public void PreviousSkinColor() => OpenSkinColorPicker();
    public void NextSkinColor() => OpenSkinColorPicker();
    public void PreviousPrimaryColor() => OpenPrimaryColorPicker();
    public void NextPrimaryColor() => OpenPrimaryColorPicker();
    public void PreviousSecondaryColor() => OpenSecondaryColorPicker();
    public void NextSecondaryColor() => OpenSecondaryColorPicker();
    public void PreviousWeaponRightColor() => OpenWeaponRightColorPicker();
    public void NextWeaponRightColor() => OpenWeaponRightColorPicker();

    public void OpenHairColorPicker() => OpenColorPicker(CharacterAppearanceColorField.Hair);
    public void OpenSkinColorPicker() => OpenColorPicker(CharacterAppearanceColorField.Skin);
    public void OpenWeaponRightColorPicker() => OpenColorPicker(CharacterAppearanceColorField.WeaponRight);
    public void OpenPrimaryColorPicker() => OpenColorPicker(CharacterAppearanceColorField.PrimaryClothing);
    public void OpenSecondaryColorPicker() => OpenColorPicker(CharacterAppearanceColorField.SecondaryClothing);

    private void OnSelectionStateChanged(CharacterSelectionStateChangedEvent e)
    {
        activeSnapshot = e.Snapshot;
        RefreshView();
    }

    private void OnFlowResult(CharacterFlowResultEvent e)
    {
        if (e.Action != CharacterFlowAction.Create)
            return;

        if (targetSlotIndex >= 0 && e.SlotIndex >= 0 && e.SlotIndex != targetSlotIndex)
            return;

        if (statusText != null)
            statusText.text = string.IsNullOrWhiteSpace(e.Message) ? string.Empty : e.Message;

        if (e.IsSuccess)
        {
            CloseColorPicker();
            ClosePanel();
        }
    }

    private void RefreshView()
    {
        if (titleText != null)
            titleText.text = targetSlotIndex >= 0
                ? $"Create Character - Slot {targetSlotIndex + 1}"
                : "Create Character";

        CharacterAppearanceCatalogAsset catalog = GetActiveCatalog();

        if (catalog == null)
            return;

        EnsureValidDraft(catalog);

        SetOptionLabel(headText, catalog.HeadOptions, draftAppearance.HeadId);
        SetOptionLabel(hairText, catalog.HairOptions, draftAppearance.HairId);
        SetOptionLabel(eyesText, catalog.EyeOptions, draftAppearance.EyesId);
        SetOptionLabel(mouthText, catalog.MouthOptions, draftAppearance.MouthId);
        SetOptionLabel(outfitText, catalog.OutfitOptions, draftAppearance.OutfitId);
        SetOptionLabel(weaponRightText, catalog.WeaponRightOptions, draftAppearance.WeaponRightId);
        SetColorPreview(hairColorPreview, draftAppearance.HairColor);
        SetColorPreview(skinColorPreview, draftAppearance.SkinColor);
        SetColorPreview(weaponRightColorPreview, draftAppearance.WeaponRightColor);
        SetColorPreview(primaryColorPreview, draftAppearance.PrimaryClothingColor);
        SetColorPreview(secondaryColorPreview, draftAppearance.SecondaryClothingColor);
    }

    private void CycleOption(CharacterAppearanceField field, int direction)
    {
        CharacterAppearanceCatalogAsset catalog = GetActiveCatalog();

        if (catalog == null)
            return;

        SetDraftOptionId(
            field,
            CycleOptionId(
                GetOptionsForField(catalog, field),
                GetDraftOptionId(field),
                direction));

        ApplyDraftChange();
    }

    private void OpenColorPicker(CharacterAppearanceColorField field)
    {
        CharacterAppearanceCatalogAsset catalog = GetActiveCatalog();

        if (catalog == null || catalog.PaletteOptions == null || catalog.PaletteOptions.Count == 0)
            return;

        if (colorPickerPopup == null)
            return;

        colorPickerPopup.Open(
            catalog.PaletteOptions,
            GetCurrentColor(field),
            selectedColor =>
            {
                SetCurrentColor(field, selectedColor);
                ApplyDraftChange();
            });
    }

    private void EnsureValidDraft(CharacterAppearanceCatalogAsset catalog)
    {
        draftAppearance.HeadId = EnsureValidOptionId(catalog.HeadOptions, draftAppearance.HeadId);
        draftAppearance.HairId = EnsureValidOptionId(catalog.HairOptions, draftAppearance.HairId);
        draftAppearance.EyesId = EnsureValidOptionId(catalog.EyeOptions, draftAppearance.EyesId);
        draftAppearance.MouthId = EnsureValidOptionId(catalog.MouthOptions, draftAppearance.MouthId);
        draftAppearance.OutfitId = EnsureValidOptionId(catalog.OutfitOptions, draftAppearance.OutfitId);
        draftAppearance.WeaponRightId = EnsureValidOptionId(catalog.WeaponRightOptions, draftAppearance.WeaponRightId);
        draftAppearance.HatId = EnsureValidOptionId(catalog.HatOptions, draftAppearance.HatId);
        draftAppearance.CapeId = EnsureValidOptionId(catalog.CapeOptions, draftAppearance.CapeId);
        draftAppearance.HornsId = EnsureValidOptionId(catalog.HornsOptions, draftAppearance.HornsId);
        draftAppearance.AccessoryId = EnsureValidOptionId(catalog.AccessoryOptions, draftAppearance.AccessoryId);
        draftAppearance.NinjaMaskId = EnsureValidOptionId(catalog.NinjaMaskOptions, draftAppearance.NinjaMaskId);
        draftAppearance.MustacheId = EnsureValidOptionId(catalog.MustacheOptions, draftAppearance.MustacheId);
        draftAppearance.HairColor = EnsureValidColor(catalog.PaletteOptions, draftAppearance.HairColor);
        draftAppearance.SkinColor = EnsureValidColor(catalog.PaletteOptions, draftAppearance.SkinColor);
        draftAppearance.WeaponRightColor = EnsureValidColor(catalog.PaletteOptions, draftAppearance.WeaponRightColor);
        draftAppearance.PrimaryClothingColor = EnsureValidColor(catalog.PaletteOptions, draftAppearance.PrimaryClothingColor);
        draftAppearance.SecondaryClothingColor = EnsureValidColor(catalog.PaletteOptions, draftAppearance.SecondaryClothingColor);
    }

    private static string EnsureValidOptionId(System.Collections.Generic.IReadOnlyList<CharacterAppearanceOption> options, string currentId)
    {
        if (options == null || options.Count == 0)
            return string.Empty;

        for (int index = 0; index < options.Count; index++)
        {
            CharacterAppearanceOption option = options[index];
            if (option != null && option.Id == currentId)
                return currentId;
        }

        return options[0] != null ? options[0].Id : string.Empty;
    }

    private static string CycleOptionId(System.Collections.Generic.IReadOnlyList<CharacterAppearanceOption> options, string currentId, int direction)
    {
        if (options == null || options.Count == 0)
            return string.Empty;

        int currentIndex = 0;

        for (int index = 0; index < options.Count; index++)
        {
            CharacterAppearanceOption option = options[index];
            if (option != null && option.Id == currentId)
            {
                currentIndex = index;
                break;
            }
        }

        int nextIndex = WrapIndex(currentIndex + direction, options.Count);
        return options[nextIndex] != null ? options[nextIndex].Id : string.Empty;
    }

    private static Color EnsureValidColor(System.Collections.Generic.IReadOnlyList<Color> palette, Color currentColor)
    {
        if (palette == null || palette.Count == 0)
            return currentColor;

        int closestIndex = FindClosestColorIndex(palette, currentColor);
        return palette[closestIndex];
    }

    private Color GetCurrentColor(CharacterAppearanceColorField field)
    {
        switch (field)
        {
            case CharacterAppearanceColorField.Hair:
                return draftAppearance.HairColor;
            case CharacterAppearanceColorField.Skin:
                return draftAppearance.SkinColor;
            case CharacterAppearanceColorField.WeaponRight:
                return draftAppearance.WeaponRightColor;
            case CharacterAppearanceColorField.PrimaryClothing:
                return draftAppearance.PrimaryClothingColor;
            case CharacterAppearanceColorField.SecondaryClothing:
                return draftAppearance.SecondaryClothingColor;
            default:
                return Color.white;
        }
    }

    private void SetCurrentColor(CharacterAppearanceColorField field, Color color)
    {
        switch (field)
        {
            case CharacterAppearanceColorField.Hair:
                draftAppearance.HairColor = color;
                break;
            case CharacterAppearanceColorField.Skin:
                draftAppearance.SkinColor = color;
                break;
            case CharacterAppearanceColorField.WeaponRight:
                draftAppearance.WeaponRightColor = color;
                break;
            case CharacterAppearanceColorField.PrimaryClothing:
                draftAppearance.PrimaryClothingColor = color;
                break;
            case CharacterAppearanceColorField.SecondaryClothing:
                draftAppearance.SecondaryClothingColor = color;
                break;
        }
    }

    private static int FindClosestColorIndex(System.Collections.Generic.IReadOnlyList<Color> palette, Color currentColor)
    {
        int bestIndex = 0;
        float bestDistance = float.MaxValue;

        for (int index = 0; index < palette.Count; index++)
        {
            float distance = Vector4.SqrMagnitude((Vector4)(palette[index] - currentColor));
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = index;
            }
        }

        return bestIndex;
    }

    private static int WrapIndex(int value, int count)
    {
        if (count <= 0)
            return 0;

        while (value < 0)
            value += count;

        while (value >= count)
            value -= count;

        return value;
    }

    private static void SetOptionLabel(TextMeshProUGUI label, System.Collections.Generic.IReadOnlyList<CharacterAppearanceOption> options, string id)
    {
        if (label == null)
            return;

        if (options == null)
        {
            label.text = "-";
            return;
        }

        for (int index = 0; index < options.Count; index++)
        {
            CharacterAppearanceOption option = options[index];
            if (option != null && option.Id == id)
            {
                label.text = option.DisplayName;
                return;
            }
        }

        label.text = "-";
    }

    private static void SetColorPreview(Image image, Color color)
    {
        if (image == null)
            return;

        image.color = color;
    }

    private void SetPanelVisible(bool isVisible)
    {
        if (panelRoot != null)
            panelRoot.SetActive(isVisible);
    }

    private CharacterAppearanceCatalogAsset GetActiveCatalog()
    {
        return activeSnapshot != null
            ? activeSnapshot.AppearanceCatalog
            : CharacterAppearanceCatalogDatabase.GetCatalog();
    }

    private void ResetDraftState()
    {
        draftAppearance = new CharacterAppearanceData();

        if (nicknameInput != null)
            nicknameInput.text = string.Empty;

        if (statusText != null)
            statusText.text = string.Empty;

        CloseColorPicker();
    }

    private void CloseColorPicker()
    {
        if (colorPickerPopup != null)
            colorPickerPopup.Close();
    }

    private void PublishPanelVisibilityChanged(bool isVisibleNow)
    {
        EventBus.Publish(new CharacterCreationPanelVisibilityChangedEvent
        {
            IsVisible = isVisibleNow
        });
    }

    private void ApplyDraftChange()
    {
        RefreshView();
        PublishPreview();
    }

    private IReadOnlyList<CharacterAppearanceOption> GetOptionsForField(
        CharacterAppearanceCatalogAsset catalog,
        CharacterAppearanceField field)
    {
        switch (field)
        {
            case CharacterAppearanceField.Head:
                return catalog.HeadOptions;
            case CharacterAppearanceField.Hair:
                return catalog.HairOptions;
            case CharacterAppearanceField.Eyes:
                return catalog.EyeOptions;
            case CharacterAppearanceField.Mouth:
                return catalog.MouthOptions;
            case CharacterAppearanceField.Outfit:
                return catalog.OutfitOptions;
            case CharacterAppearanceField.WeaponRight:
                return catalog.WeaponRightOptions;
            case CharacterAppearanceField.Hat:
                return catalog.HatOptions;
            case CharacterAppearanceField.Cape:
                return catalog.CapeOptions;
            case CharacterAppearanceField.Horns:
                return catalog.HornsOptions;
            case CharacterAppearanceField.Accessory:
                return catalog.AccessoryOptions;
            case CharacterAppearanceField.NinjaMask:
                return catalog.NinjaMaskOptions;
            case CharacterAppearanceField.Mustache:
                return catalog.MustacheOptions;
            default:
                return System.Array.Empty<CharacterAppearanceOption>();
        }
    }

    private string GetDraftOptionId(CharacterAppearanceField field)
    {
        switch (field)
        {
            case CharacterAppearanceField.Head:
                return draftAppearance.HeadId;
            case CharacterAppearanceField.Hair:
                return draftAppearance.HairId;
            case CharacterAppearanceField.Eyes:
                return draftAppearance.EyesId;
            case CharacterAppearanceField.Mouth:
                return draftAppearance.MouthId;
            case CharacterAppearanceField.Outfit:
                return draftAppearance.OutfitId;
            case CharacterAppearanceField.WeaponRight:
                return draftAppearance.WeaponRightId;
            case CharacterAppearanceField.Hat:
                return draftAppearance.HatId;
            case CharacterAppearanceField.Cape:
                return draftAppearance.CapeId;
            case CharacterAppearanceField.Horns:
                return draftAppearance.HornsId;
            case CharacterAppearanceField.Accessory:
                return draftAppearance.AccessoryId;
            case CharacterAppearanceField.NinjaMask:
                return draftAppearance.NinjaMaskId;
            case CharacterAppearanceField.Mustache:
                return draftAppearance.MustacheId;
            default:
                return string.Empty;
        }
    }

    private void SetDraftOptionId(CharacterAppearanceField field, string optionId)
    {
        switch (field)
        {
            case CharacterAppearanceField.Head:
                draftAppearance.HeadId = optionId;
                break;
            case CharacterAppearanceField.Hair:
                draftAppearance.HairId = optionId;
                break;
            case CharacterAppearanceField.Eyes:
                draftAppearance.EyesId = optionId;
                break;
            case CharacterAppearanceField.Mouth:
                draftAppearance.MouthId = optionId;
                break;
            case CharacterAppearanceField.Outfit:
                draftAppearance.OutfitId = optionId;
                break;
            case CharacterAppearanceField.WeaponRight:
                draftAppearance.WeaponRightId = optionId;
                break;
            case CharacterAppearanceField.Hat:
                draftAppearance.HatId = optionId;
                break;
            case CharacterAppearanceField.Cape:
                draftAppearance.CapeId = optionId;
                break;
            case CharacterAppearanceField.Horns:
                draftAppearance.HornsId = optionId;
                break;
            case CharacterAppearanceField.Accessory:
                draftAppearance.AccessoryId = optionId;
                break;
            case CharacterAppearanceField.NinjaMask:
                draftAppearance.NinjaMaskId = optionId;
                break;
            case CharacterAppearanceField.Mustache:
                draftAppearance.MustacheId = optionId;
                break;
        }
    }

    private void PublishPreview()
    {
        if (!isOpen)
            return;

        EventBus.Publish(new CharacterAppearancePreviewChangedEvent
        {
            Appearance = draftAppearance != null ? draftAppearance.Clone() : new CharacterAppearanceData()
        });
    }

    private enum CharacterAppearanceField
    {
        Head,
        Hair,
        Eyes,
        Mouth,
        Outfit,
        WeaponRight,
        Hat,
        Cape,
        Horns,
        Accessory,
        NinjaMask,
        Mustache
    }

    private enum CharacterAppearanceColorField
    {
        Hair,
        Skin,
        WeaponRight,
        PrimaryClothing,
        SecondaryClothing
    }
}
