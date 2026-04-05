using System.Collections.Generic;

public enum NpcPromptType
{
    Talk,
    Shop,
    Quest
}

public struct NpcPromptShownEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
    public NpcInteractable Npc;
    public string PrimaryText;
    public string SecondaryText;
}

public struct NpcPromptHiddenEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
    public string NpcId;
}

public struct NpcInteractionRequestEvent
{
    public PlayerCharacter Requester;
    public string CharacterId;
    public NpcInteractable Npc;
}

public struct NpcShopOpenedEvent
{
    public NpcShopStateSnapshot Snapshot;
}

public struct NpcShopStateChangedEvent
{
    public NpcShopStateSnapshot Snapshot;
}

public struct NpcShopClosedEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
    public string NpcId;
}

public struct NpcShopBuyRequestEvent
{
    public PlayerCharacter Requester;
    public string CharacterId;
    public string NpcId;
    public string ItemId;
    public int Amount;
}

public struct NpcShopSellRequestEvent
{
    public PlayerCharacter Requester;
    public string CharacterId;
    public string NpcId;
    public string ItemId;
    public int Amount;
}

public struct NpcShopCloseRequestEvent
{
    public PlayerCharacter Requester;
    public string CharacterId;
    public string NpcId;
}

public struct NpcJobAdvancementOpenedEvent
{
    public NpcJobAdvancementSnapshot Snapshot;
}

public struct NpcJobAdvancementClosedEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
    public string NpcId;
}

public struct NpcJobAdvancementSelectRequestEvent
{
    public PlayerCharacter Requester;
    public string CharacterId;
    public string NpcId;
    public PlayerJobType TargetJob;
}

public struct NpcJobAdvancementCloseRequestEvent
{
    public PlayerCharacter Requester;
    public string CharacterId;
    public string NpcId;
}

public struct NpcJobAdvancementResultEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
    public string NpcId;
    public PlayerJobType TargetJob;
    public bool IsSuccess;
    public string Message;
}

public enum NpcShopTransactionKind
{
    Buy,
    Sell
}

public struct NpcShopTransactionResultEvent
{
    public PlayerCharacter Player;
    public string CharacterId;
    public string NpcId;
    public string ItemId;
    public int Amount;
    public int MesosDelta;
    public NpcShopTransactionKind Kind;
    public bool IsSuccess;
    public string Message;
}

public sealed class NpcShopBuyEntry
{
    public string ItemId { get; }
    public string DisplayName { get; }
    public ItemCategory Category { get; }
    public int UnitPrice { get; }
    public int OwnedCount { get; }

    public NpcShopBuyEntry(
        string itemId,
        string displayName,
        ItemCategory category,
        int unitPrice,
        int ownedCount)
    {
        ItemId = itemId;
        DisplayName = displayName;
        Category = category;
        UnitPrice = unitPrice;
        OwnedCount = ownedCount;
    }
}

public sealed class NpcShopSellEntry
{
    public string ItemId { get; }
    public string DisplayName { get; }
    public ItemCategory Category { get; }
    public int OwnedCount { get; }
    public int UnitPrice { get; }

    public NpcShopSellEntry(
        string itemId,
        string displayName,
        ItemCategory category,
        int ownedCount,
        int unitPrice)
    {
        ItemId = itemId;
        DisplayName = displayName;
        Category = category;
        OwnedCount = ownedCount;
        UnitPrice = unitPrice;
    }
}

public sealed class NpcShopStateSnapshot
{
    public PlayerCharacter Player { get; }
    public string CharacterId { get; }
    public string NpcId { get; }
    public string VendorName { get; }
    public int PlayerMesos { get; }
    public bool BuysPlayerItems { get; }
    public IReadOnlyList<NpcShopBuyEntry> BuyEntries { get; }
    public IReadOnlyList<NpcShopSellEntry> SellEntries { get; }

    public NpcShopStateSnapshot(
        PlayerCharacter player,
        string characterId,
        string npcId,
        string vendorName,
        int playerMesos,
        bool buysPlayerItems,
        IReadOnlyList<NpcShopBuyEntry> buyEntries,
        IReadOnlyList<NpcShopSellEntry> sellEntries)
    {
        Player = player;
        CharacterId = PlayerRuntimeIdentityUtility.NormalizeCharacterId(characterId);
        NpcId = npcId;
        VendorName = vendorName;
        PlayerMesos = playerMesos;
        BuysPlayerItems = buysPlayerItems;
        BuyEntries = buyEntries;
        SellEntries = sellEntries;
    }
}

public sealed class NpcJobAdvancementOption
{
    public PlayerJobType JobType { get; }
    public string DisplayName { get; }
    public bool IsAvailable { get; }

    public NpcJobAdvancementOption(PlayerJobType jobType, string displayName, bool isAvailable)
    {
        JobType = jobType;
        DisplayName = displayName;
        IsAvailable = isAvailable;
    }
}

public sealed class NpcJobAdvancementSnapshot
{
    public PlayerCharacter Player { get; }
    public string CharacterId { get; }
    public string NpcId { get; }
    public string NpcName { get; }
    public int RequiredLevel { get; }
    public PlayerJobType CurrentJob { get; }
    public bool CanAdvance { get; }
    public string StatusMessage { get; }
    public IReadOnlyList<NpcJobAdvancementOption> Options { get; }

    public NpcJobAdvancementSnapshot(
        PlayerCharacter player,
        string characterId,
        string npcId,
        string npcName,
        int requiredLevel,
        PlayerJobType currentJob,
        bool canAdvance,
        string statusMessage,
        IReadOnlyList<NpcJobAdvancementOption> options)
    {
        Player = player;
        CharacterId = PlayerRuntimeIdentityUtility.NormalizeCharacterId(characterId);
        NpcId = npcId;
        NpcName = npcName;
        RequiredLevel = requiredLevel;
        CurrentJob = currentJob;
        CanAdvance = canAdvance;
        StatusMessage = statusMessage;
        Options = options;
    }
}
