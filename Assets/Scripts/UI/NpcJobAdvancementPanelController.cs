using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NpcJobAdvancementPanelController : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI npcNameText;
    [SerializeField] private TextMeshProUGUI currentJobText;
    [SerializeField] private TextMeshProUGUI requirementText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button warriorButton;
    [SerializeField] private Button thiefButton;
    [SerializeField] private Button mageButton;
    [SerializeField] private TextMeshProUGUI warriorLabel;
    [SerializeField] private TextMeshProUGUI thiefLabel;
    [SerializeField] private TextMeshProUGUI mageLabel;

    private NpcJobAdvancementSnapshot activeSnapshot;

    private void Start()
    {
        SetPanelVisible(false);
        RefreshView();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<NpcJobAdvancementOpenedEvent>(OnAdvancementOpened);
        EventBus.Subscribe<NpcJobAdvancementClosedEvent>(OnAdvancementClosed);
        EventBus.Subscribe<NpcJobAdvancementResultEvent>(OnAdvancementResult);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<NpcJobAdvancementOpenedEvent>(OnAdvancementOpened);
        EventBus.Unsubscribe<NpcJobAdvancementClosedEvent>(OnAdvancementClosed);
        EventBus.Unsubscribe<NpcJobAdvancementResultEvent>(OnAdvancementResult);
    }

    public void RequestWarrior() => RequestAdvance(PlayerJobType.Warrior);
    public void RequestThief() => RequestAdvance(PlayerJobType.Thief);
    public void RequestMage() => RequestAdvance(PlayerJobType.Mage);

    public void ClosePanel()
    {
        if (activeSnapshot == null || string.IsNullOrWhiteSpace(activeSnapshot.CharacterId))
            return;

        EventBus.Publish(new NpcJobAdvancementCloseRequestEvent
        {
            Requester = activeSnapshot.Player,
            CharacterId = activeSnapshot.CharacterId,
            NpcId = activeSnapshot.NpcId
        });
    }

    private void RequestAdvance(PlayerJobType targetJob)
    {
        if (activeSnapshot == null || string.IsNullOrWhiteSpace(activeSnapshot.CharacterId))
            return;

        EventBus.Publish(new NpcJobAdvancementSelectRequestEvent
        {
            Requester = activeSnapshot.Player,
            CharacterId = activeSnapshot.CharacterId,
            NpcId = activeSnapshot.NpcId,
            TargetJob = targetJob
        });
    }

    private void OnAdvancementOpened(NpcJobAdvancementOpenedEvent e)
    {
        activeSnapshot = e.Snapshot;
        SetPanelVisible(true);
        RefreshView();
    }

    private void OnAdvancementClosed(NpcJobAdvancementClosedEvent e)
    {
        if (!MatchesActiveAdvancement(e.Player, e.CharacterId, e.NpcId))
            return;

        activeSnapshot = null;
        SetPanelVisible(false);
        RefreshView();
    }

    private void OnAdvancementResult(NpcJobAdvancementResultEvent e)
    {
        if (!MatchesActiveAdvancement(e.Player, e.CharacterId, e.NpcId))
            return;

        if (statusText != null)
            statusText.text = string.IsNullOrWhiteSpace(e.Message) ? string.Empty : e.Message;
    }

    private void RefreshView()
    {
        if (npcNameText != null)
            npcNameText.text = activeSnapshot != null ? activeSnapshot.NpcName : "Job Instructor";

        if (currentJobText != null)
            currentJobText.text = activeSnapshot != null
                ? $"Current Job: {GameBootstrap.FormatJobName(activeSnapshot.CurrentJob)}"
                : "Current Job: Novice";

        if (requirementText != null)
            requirementText.text = activeSnapshot != null
                ? $"Required Level: {activeSnapshot.RequiredLevel}"
                : "Required Level: 10";

        if (statusText != null)
            statusText.text = activeSnapshot != null && !string.IsNullOrWhiteSpace(activeSnapshot.StatusMessage)
                ? activeSnapshot.StatusMessage
                : string.Empty;

        RefreshOption(PlayerJobType.Warrior, warriorButton, warriorLabel);
        RefreshOption(PlayerJobType.Thief, thiefButton, thiefLabel);
        RefreshOption(PlayerJobType.Mage, mageButton, mageLabel);
    }

    private void RefreshOption(PlayerJobType jobType, Button button, TextMeshProUGUI label)
    {
        NpcJobAdvancementOption option = FindOption(jobType);
        bool isVisible = option != null;
        bool isAvailable = option != null && option.IsAvailable;
        string displayName = option != null ? option.DisplayName : GameBootstrap.FormatJobName(jobType);

        if (button != null)
        {
            button.gameObject.SetActive(isVisible);
            button.interactable = isAvailable;
        }

        if (label != null)
            label.text = displayName;
    }

    private NpcJobAdvancementOption FindOption(PlayerJobType jobType)
    {
        if (activeSnapshot?.Options == null)
            return null;

        for (int index = 0; index < activeSnapshot.Options.Count; index++)
        {
            NpcJobAdvancementOption option = activeSnapshot.Options[index];
            if (option != null && option.JobType == jobType)
                return option;
        }

        return null;
    }

    private void SetPanelVisible(bool isVisible)
    {
        if (panelRoot != null)
            panelRoot.SetActive(isVisible);
    }

    private bool MatchesActiveAdvancement(PlayerCharacter player, string characterId, string npcId)
    {
        if (activeSnapshot == null)
            return false;

        if (!PlayerRuntimeIdentityUtility.MatchesCharacter(
                activeSnapshot.Player,
                activeSnapshot.CharacterId,
                player,
                characterId))
            return false;

        return string.IsNullOrWhiteSpace(npcId) || npcId == activeSnapshot.NpcId;
    }
}
