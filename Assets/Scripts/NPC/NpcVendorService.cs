using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-970)]
public class NpcVendorService : MonoBehaviour
{
    [SerializeField] private GameBootstrap bootstrap;

    private NpcInteractable activeNpc;
    private NpcVendor activeVendor;
    private PlayerCharacter activePlayer;
    private string activeCharacterId;

    public bool IsShopOpen => activeNpc != null && activeVendor != null && activePlayer != null;

    public void BindBootstrap(GameBootstrap sessionBootstrap)
    {
        bootstrap = sessionBootstrap;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeInstance()
    {
        NpcVendorService[] services = FindObjectsByType<NpcVendorService>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        if (services.Length > 0)
            return;

        GameObject vendorServiceObject = new GameObject("[NpcVendorService]");
        vendorServiceObject.AddComponent<NpcVendorService>();
    }

    private void Awake()
    {
        if (ShouldDestroyDuplicate())
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        if (bootstrap != null)
            BindBootstrap(bootstrap);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<NpcInteractionRequestEvent>(OnNpcInteractionRequested);
        EventBus.Subscribe<NpcShopBuyRequestEvent>(OnBuyRequested);
        EventBus.Subscribe<NpcShopSellRequestEvent>(OnSellRequested);
        EventBus.Subscribe<NpcShopCloseRequestEvent>(OnCloseRequested);
        EventBus.Subscribe<MapTransitionStartedEvent>(OnMapTransitionStarted);
        EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<NpcInteractionRequestEvent>(OnNpcInteractionRequested);
        EventBus.Unsubscribe<NpcShopBuyRequestEvent>(OnBuyRequested);
        EventBus.Unsubscribe<NpcShopSellRequestEvent>(OnSellRequested);
        EventBus.Unsubscribe<NpcShopCloseRequestEvent>(OnCloseRequested);
        EventBus.Unsubscribe<MapTransitionStartedEvent>(OnMapTransitionStarted);
        EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
        ResetActiveShop();
    }

    private void OnNpcInteractionRequested(NpcInteractionRequestEvent e)
    {
        if (e.Requester == null || !e.Requester.IsLocalPlayer || e.Npc == null)
            return;

        NpcVendor vendor = e.Npc.GetComponent<NpcVendor>();
        if (vendor == null)
            return;

        if (IsSameActiveShop(e.Requester, e.CharacterId, e.Npc))
        {
            CloseShop();
            return;
        }

        OpenShop(e.Requester, e.CharacterId, e.Npc, vendor);
    }

    private void OnBuyRequested(NpcShopBuyRequestEvent e)
    {
        if (!ValidateActiveShopRequest(e.Requester, e.CharacterId, e.NpcId))
            return;

        int amount = Mathf.Max(1, e.Amount);
        string itemId = NormalizeItemId(e.ItemId);

        if (!activeVendor.SellsItem(itemId))
        {
            PublishTransactionResult(itemId, amount, 0, NpcShopTransactionKind.Buy, false, "That item is not sold by this vendor.");
            return;
        }

        ItemDefinition definition = ItemDatabase.GetDefinition(itemId);
        if (definition == null || definition.BuyPrice <= 0)
        {
            PublishTransactionResult(itemId, amount, 0, NpcShopTransactionKind.Buy, false, "That item cannot be purchased right now.");
            return;
        }

        int totalCost = definition.BuyPrice * amount;
        PlayerSessionCurrencyApplicationService currencySession = bootstrap != null ? bootstrap.CurrencySession : null;
        PlayerSessionInventoryApplicationService inventorySession = bootstrap != null ? bootstrap.InventorySession : null;

        if (currencySession == null || inventorySession == null || !currencySession.TrySpendMesos(activePlayer, totalCost))
        {
            PublishTransactionResult(itemId, amount, 0, NpcShopTransactionKind.Buy, false, "You do not have enough Mesos.");
            return;
        }

        if (!inventorySession.AddInventoryItem(activePlayer, itemId, amount))
        {
            currencySession.AddMesos(activePlayer, totalCost);
            PublishTransactionResult(itemId, amount, 0, NpcShopTransactionKind.Buy, false, "The purchase could not be completed.");
            return;
        }

        PublishTransactionResult(itemId, amount, -totalCost, NpcShopTransactionKind.Buy, true, "Purchase completed.");
        PublishShopStateChanged();
    }

    private void OnSellRequested(NpcShopSellRequestEvent e)
    {
        if (!ValidateActiveShopRequest(e.Requester, e.CharacterId, e.NpcId))
            return;

        if (!activeVendor.BuysPlayerItems)
        {
            PublishTransactionResult(e.ItemId, e.Amount, 0, NpcShopTransactionKind.Sell, false, "This vendor is not buying items.");
            return;
        }

        int amount = Mathf.Max(1, e.Amount);
        string itemId = NormalizeItemId(e.ItemId);

        ItemDefinition definition = ItemDatabase.GetDefinition(itemId);
        if (definition == null || definition.SellPrice <= 0)
        {
            PublishTransactionResult(itemId, amount, 0, NpcShopTransactionKind.Sell, false, "That item cannot be sold.");
            return;
        }

        PlayerSessionCurrencyApplicationService currencySession = bootstrap != null ? bootstrap.CurrencySession : null;
        PlayerSessionInventoryApplicationService inventorySession = bootstrap != null ? bootstrap.InventorySession : null;

        if (currencySession == null || inventorySession == null || !inventorySession.HasInventoryItem(itemId, amount))
        {
            PublishTransactionResult(itemId, amount, 0, NpcShopTransactionKind.Sell, false, "You do not have enough of that item.");
            return;
        }

        if (!inventorySession.TryRemoveInventoryItem(activePlayer, itemId, amount))
        {
            PublishTransactionResult(itemId, amount, 0, NpcShopTransactionKind.Sell, false, "The sale could not be completed.");
            return;
        }

        int totalPayout = definition.SellPrice * amount;
        currencySession.AddMesos(activePlayer, totalPayout);

        PublishTransactionResult(itemId, amount, totalPayout, NpcShopTransactionKind.Sell, true, "Sale completed.");
        PublishShopStateChanged();
    }

    private void OnCloseRequested(NpcShopCloseRequestEvent e)
    {
        if (!ValidateActiveShopRequest(e.Requester, e.CharacterId, e.NpcId))
            return;

        CloseShop();
    }

    private void OnMapTransitionStarted(MapTransitionStartedEvent e)
    {
        if (IsShopOpen && MatchesActivePlayer(e.Requester, e.CharacterId))
            CloseShop();
    }

    private void OnPlayerDied(PlayerDiedEvent e)
    {
        if (IsShopOpen && MatchesActivePlayer(e.Target, e.CharacterId))
            CloseShop();
    }

    private void OpenShop(PlayerCharacter player, string characterId, NpcInteractable npc, NpcVendor vendor)
    {
        activePlayer = player;
        activeCharacterId = ResolveCharacterId(player, characterId);
        activeNpc = npc;
        activeVendor = vendor;

        NpcShopStateSnapshot snapshot = BuildSnapshot();
        if (snapshot == null)
        {
            ResetActiveShop();
            return;
        }

        EventBus.Publish(new NpcShopOpenedEvent
        {
            Snapshot = snapshot
        });
    }

    private void CloseShop()
    {
        if (!IsShopOpen)
            return;

        EventBus.Publish(new NpcShopClosedEvent
        {
            Player = activePlayer,
            CharacterId = activeCharacterId,
            NpcId = activeNpc != null ? activeNpc.NpcId : string.Empty
        });

        ResetActiveShop();
    }

    private void PublishShopStateChanged()
    {
        NpcShopStateSnapshot snapshot = BuildSnapshot();
        if (snapshot == null)
            return;

        EventBus.Publish(new NpcShopStateChangedEvent
        {
            Snapshot = snapshot
        });
    }

    private void PublishTransactionResult(
        string itemId,
        int amount,
        int mesosDelta,
        NpcShopTransactionKind kind,
        bool isSuccess,
        string message)
    {
        EventBus.Publish(new NpcShopTransactionResultEvent
        {
            Player = activePlayer,
            CharacterId = activeCharacterId,
            NpcId = activeNpc != null ? activeNpc.NpcId : string.Empty,
            ItemId = itemId,
            Amount = amount,
            MesosDelta = mesosDelta,
            Kind = kind,
            IsSuccess = isSuccess,
            Message = message
        });
    }

    private NpcShopStateSnapshot BuildSnapshot()
    {
        if (!IsShopOpen)
            return null;

        if (bootstrap == null)
            return null;

        PlayerSessionInventoryApplicationService inventorySession = bootstrap.InventorySession;
        PlayerSessionCurrencyApplicationService currencySession = bootstrap.CurrencySession;
        if (inventorySession == null || currencySession == null)
            return null;

        List<NpcShopBuyEntry> buyEntries = new List<NpcShopBuyEntry>();
        foreach (string itemId in activeVendor.StockedItemIds)
        {
            if (!ItemDatabase.TryGetDefinition(itemId, out ItemDefinition definition))
                continue;

            if (definition.BuyPrice <= 0)
                continue;

            buyEntries.Add(new NpcShopBuyEntry(
                itemId,
                definition.DisplayName,
                definition.Category,
                definition.BuyPrice,
                inventorySession.GetInventoryCount(itemId)));
        }

        List<NpcShopSellEntry> sellEntries = new List<NpcShopSellEntry>();
        if (activeVendor.BuysPlayerItems)
        {
            IReadOnlyList<InventoryEntry> inventoryEntries = inventorySession.GetInventoryEntries();
            foreach (InventoryEntry entry in inventoryEntries)
            {
                if (entry == null || entry.Count <= 0)
                    continue;

                if (!ItemDatabase.TryGetDefinition(entry.ItemId, out ItemDefinition definition))
                    continue;

                if (definition.SellPrice <= 0)
                    continue;

                sellEntries.Add(new NpcShopSellEntry(
                    entry.ItemId,
                    definition.DisplayName,
                    definition.Category,
                    entry.Count,
                    definition.SellPrice));
            }
        }

        return new NpcShopStateSnapshot(
            activePlayer,
            activeCharacterId,
            activeNpc.NpcId,
            activeNpc.DisplayName,
            currencySession.CurrentMesos,
            activeVendor.BuysPlayerItems,
            buyEntries,
            sellEntries);
    }

    private bool ValidateActiveShopRequest(PlayerCharacter requester, string characterId, string npcId)
    {
        if (!IsShopOpen)
            return false;

        if (requester != null && !requester.IsLocalPlayer)
            return false;

        if (!MatchesActivePlayer(requester, characterId))
            return false;

        if (!string.IsNullOrWhiteSpace(npcId) && npcId.Trim() != activeNpc.NpcId)
            return false;

        return true;
    }

    private bool IsSameActiveShop(PlayerCharacter requester, string characterId, NpcInteractable npc)
    {
        return IsShopOpen && MatchesActivePlayer(requester, characterId) && npc == activeNpc;
    }

    private void ResetActiveShop()
    {
        activeNpc = null;
        activeVendor = null;
        activePlayer = null;
        activeCharacterId = string.Empty;
    }

    private bool ShouldDestroyDuplicate()
    {
        NpcVendorService[] services = FindObjectsByType<NpcVendorService>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        NpcVendorService keeper = this;

        foreach (NpcVendorService service in services)
        {
            if (service == null)
                continue;

            if (service.GetInstanceID() < keeper.GetInstanceID())
                keeper = service;
        }

        return keeper != this;
    }

    private static string NormalizeItemId(string itemId)
    {
        return string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId.Trim();
    }

    private bool MatchesActivePlayer(PlayerCharacter player, string characterId)
    {
        return PlayerRuntimeIdentityUtility.MatchesCharacter(
            activePlayer,
            activeCharacterId,
            player,
            characterId);
    }

    private static string ResolveCharacterId(PlayerCharacter player, string characterId)
    {
        if (player != null)
            return PlayerRuntimeIdentityUtility.NormalizeCharacterId(player.CharacterId);

        return PlayerRuntimeIdentityUtility.NormalizeCharacterId(characterId);
    }
}
