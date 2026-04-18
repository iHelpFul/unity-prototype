using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NpcQuestRewardEntryView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private TextMeshProUGUI amountText;

    public void Bind(NpcQuestLogRewardSnapshot reward)
    {
        if (labelText != null)
            labelText.text = reward != null && !string.IsNullOrWhiteSpace(reward.DisplayLabel) ? reward.DisplayLabel : "Reward";

        if (amountText != null)
        {
            int amount = reward != null ? Mathf.Max(0, reward.Amount) : 0;
            amountText.text = reward != null && reward.RewardType == NpcQuestLogRewardType.Item
                ? $"x{amount}"
                : amount.ToString();
        }

        if (iconImage != null)
        {
            Sprite icon = reward != null ? reward.Icon : null;
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }
    }
}
