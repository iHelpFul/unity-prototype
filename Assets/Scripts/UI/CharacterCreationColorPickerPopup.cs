using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCreationColorPickerPopup : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Transform swatchContainer;
    [SerializeField] private Button swatchButtonTemplate;

    private readonly List<Button> swatchButtons = new List<Button>();
    private Action<Color> onColorSelected;

    private void Awake()
    {
        if (swatchButtonTemplate != null)
            swatchButtonTemplate.gameObject.SetActive(false);

        SetVisible(false);
    }

    public void Open(IReadOnlyList<Color> palette, Color selectedColor, Action<Color> selectionCallback)
    {
        if (palette == null || palette.Count == 0)
            return;

        onColorSelected = selectionCallback;

        EnsureButtonCount(palette.Count);

        for (int index = 0; index < swatchButtons.Count; index++)
        {
            Button button = swatchButtons[index];
            bool isActive = index < palette.Count;
            button.gameObject.SetActive(isActive);

            if (!isActive)
                continue;

            Color paletteColor = palette[index];
            ConfigureButton(button, paletteColor, AreColorsClose(paletteColor, selectedColor));
        }

        SetVisible(true);
    }

    public void Close()
    {
        onColorSelected = null;
        SetVisible(false);
    }

    private void EnsureButtonCount(int targetCount)
    {
        if (swatchButtonTemplate == null || swatchContainer == null)
            return;

        while (swatchButtons.Count < targetCount)
        {
            Button clone = Instantiate(swatchButtonTemplate, swatchContainer);
            clone.gameObject.SetActive(true);
            swatchButtons.Add(clone);
        }
    }

    private void ConfigureButton(Button button, Color paletteColor, bool isSelected)
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SelectColor(paletteColor));

        Image buttonImage = button.targetGraphic as Image;
        if (buttonImage == null)
            buttonImage = button.GetComponent<Image>();
        if (buttonImage == null)
            buttonImage = button.GetComponentInChildren<Image>(true);

        if (buttonImage != null)
            buttonImage.color = paletteColor;

        button.transform.localScale = isSelected ? Vector3.one * 1.08f : Vector3.one;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.selectedColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = Color.white * 0.9f;
        button.colors = colors;
    }

    private void SelectColor(Color color)
    {
        Action<Color> callback = onColorSelected;
        Close();
        callback?.Invoke(color);
    }

    private void SetVisible(bool isVisible)
    {
        if (panelRoot != null)
            panelRoot.SetActive(isVisible);
        else
            gameObject.SetActive(isVisible);
    }

    private static bool AreColorsClose(Color left, Color right)
    {
        return Vector4.SqrMagnitude((Vector4)(left - right)) < 0.0001f;
    }
}
