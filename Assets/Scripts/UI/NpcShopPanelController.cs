using System.Text;
using TMPro;
using UnityEngine;

public class NpcShopPanelController : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI vendorNameText;
    [SerializeField] private TextMeshProUGUI mesosText;
    [SerializeField] private TextMeshProUGUI buyListText;
    [SerializeField] private TextMeshProUGUI sellListText;
    [SerializeField] private TextMeshProUGUI statusText;

    private NpcShopStateSnapshot activeSnapshot;

    private void Start()
    {
        SetPanelVisible(false);
        RefreshView();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<NpcShopOpenedEvent>(OnShopOpened);
        EventBus.Subscribe<NpcShopStateChangedEvent>(OnShopStateChanged);
        EventBus.Subscribe<NpcShopClosedEvent>(OnShopClosed);
        EventBus.Subscribe<NpcShopTransactionResultEvent>(OnTransactionResult);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<NpcShopOpenedEvent>(OnShopOpened);
        EventBus.Unsubscribe<NpcShopStateChangedEvent>(OnShopStateChanged);
        EventBus.Unsubscribe<NpcShopClosedEvent>(OnShopClosed);
        EventBus.Unsubscribe<NpcShopTransactionResultEvent>(OnTransactionResult);
    }

    public void RequestBuy(string itemId)
    {
        if (activeSnapshot == null || string.IsNullOrWhiteSpace(activeSnapshot.CharacterId) || string.IsNullOrWhiteSpace(itemId))
            return;

        EventBus.Publish(new NpcShopBuyRequestEvent
        {
            Requester = activeSnapshot.Player,
            CharacterId = activeSnapshot.CharacterId,
            NpcId = activeSnapshot.NpcId,
            ItemId = itemId,
            Amount = 1
        });
    }

    public void RequestSell(string itemId)
    {
        if (activeSnapshot == null || string.IsNullOrWhiteSpace(activeSnapshot.CharacterId) || string.IsNullOrWhiteSpace(itemId))
            return;

        EventBus.Publish(new NpcShopSellRequestEvent
        {
            Requester = activeSnapshot.Player,
            CharacterId = activeSnapshot.CharacterId,
            NpcId = activeSnapshot.NpcId,
            ItemId = itemId,
            Amount = 1
        });
    }

    public void CloseShop()
    {
        if (activeSnapshot == null || string.IsNullOrWhiteSpace(activeSnapshot.CharacterId))
            return;

        EventBus.Publish(new NpcShopCloseRequestEvent
        {
            Requester = activeSnapshot.Player,
            CharacterId = activeSnapshot.CharacterId,
            NpcId = activeSnapshot.NpcId
        });
    }

    private void OnShopOpened(NpcShopOpenedEvent e)
    {
        activeSnapshot = e.Snapshot;
        SetStatus(string.Empty);
        SetPanelVisible(true);
        RefreshView();
    }

    private void OnShopStateChanged(NpcShopStateChangedEvent e)
    {
        if (!MatchesActiveShop(e.Snapshot))
            return;

        activeSnapshot = e.Snapshot;
        RefreshView();
    }

    private void OnShopClosed(NpcShopClosedEvent e)
    {
        if (!MatchesActiveShop(e.Player, e.CharacterId, e.NpcId))
            return;

        activeSnapshot = null;
        SetStatus(string.Empty);
        SetPanelVisible(false);
        RefreshView();
    }

    private void OnTransactionResult(NpcShopTransactionResultEvent e)
    {
        if (!MatchesActiveShop(e.Player, e.CharacterId, e.NpcId))
            return;

        SetStatus(e.Message);
    }

    private void RefreshView()
    {
        if (vendorNameText != null)
            vendorNameText.text = activeSnapshot != null ? activeSnapshot.VendorName : "Vendor";

        if (mesosText != null)
            mesosText.text = activeSnapshot != null ? $"Mesos: {activeSnapshot.PlayerMesos}" : "Mesos: 0";

        if (buyListText != null)
            buyListText.text = BuildBuyListText();

        if (sellListText != null)
            sellListText.text = BuildSellListText();
    }

    private string BuildBuyListText()
    {
        if (activeSnapshot == null || activeSnapshot.BuyEntries == null || activeSnapshot.BuyEntries.Count == 0)
            return "Buy:\n- No items";

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Buy:");

        foreach (NpcShopBuyEntry entry in activeSnapshot.BuyEntries)
            builder.AppendLine($"- {entry.DisplayName} | {entry.UnitPrice} Mesos | Owned {entry.OwnedCount}");

        return builder.ToString().TrimEnd();
    }

    private string BuildSellListText()
    {
        if (activeSnapshot == null || !activeSnapshot.BuysPlayerItems)
            return "Sell:\n- This vendor is not buying items";

        if (activeSnapshot.SellEntries == null || activeSnapshot.SellEntries.Count == 0)
            return "Sell:\n- No sellable items";

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Sell:");

        foreach (NpcShopSellEntry entry in activeSnapshot.SellEntries)
            builder.AppendLine($"- {entry.DisplayName} x{entry.OwnedCount} | {entry.UnitPrice} Mesos each");

        return builder.ToString().TrimEnd();
    }

    private bool MatchesActiveShop(NpcShopStateSnapshot snapshot)
    {
        if (activeSnapshot == null || snapshot == null)
            return false;

        return PlayerRuntimeIdentityUtility.MatchesCharacter(
                   activeSnapshot.Player,
                   activeSnapshot.CharacterId,
                   snapshot.Player,
                   snapshot.CharacterId)
               && snapshot.NpcId == activeSnapshot.NpcId;
    }

    private bool MatchesActiveShop(PlayerCharacter player, string characterId, string npcId)
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

    private void SetPanelVisible(bool isVisible)
    {
        if (panelRoot != null)
            panelRoot.SetActive(isVisible);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = string.IsNullOrWhiteSpace(message) ? string.Empty : message;
    }
}
