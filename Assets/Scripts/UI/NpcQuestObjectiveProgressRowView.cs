using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NpcQuestObjectiveProgressRowView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Slider progressSlider;

    public void Bind(NpcQuestLogObjectiveProgressSnapshot objective)
    {
        if (titleText != null)
            titleText.text = ResolveTitle(objective);

        if (hintText != null)
        {
            string hint = objective != null ? objective.DisplayHint : string.Empty;
            hintText.text = string.IsNullOrWhiteSpace(hint) ? string.Empty : hint;
            hintText.gameObject.SetActive(!string.IsNullOrWhiteSpace(hint));
        }

        if (progressText != null)
        {
            int current = objective != null ? Mathf.Max(0, objective.CurrentAmount) : 0;
            int required = objective != null ? Mathf.Max(1, objective.RequiredAmount) : 1;
            progressText.text = $"{current}/{required}";
        }

        if (progressSlider != null)
        {
            int current = objective != null ? Mathf.Max(0, objective.CurrentAmount) : 0;
            int required = objective != null ? Mathf.Max(1, objective.RequiredAmount) : 1;
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value = required > 0 ? Mathf.Clamp01((float)current / required) : 0f;
        }

        if (iconImage != null)
        {
            Sprite icon = objective != null ? objective.Icon : null;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }
    }

    private static string ResolveTitle(NpcQuestLogObjectiveProgressSnapshot objective)
    {
        if (objective == null)
            return "Objective";

        if (!string.IsNullOrWhiteSpace(objective.DisplayLabel))
            return objective.DisplayLabel;

        if (!string.IsNullOrWhiteSpace(objective.TargetId))
            return objective.TargetId;

        return "Objective";
    }
}
