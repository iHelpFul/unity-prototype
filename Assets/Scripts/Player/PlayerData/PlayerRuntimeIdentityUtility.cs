public static class PlayerRuntimeIdentityUtility
{
    public static string NormalizeCharacterId(string characterId)
    {
        return string.IsNullOrWhiteSpace(characterId)
            ? string.Empty
            : characterId.Trim();
    }

    public static string ResolveCharacterId(GameBootstrap bootstrap, PlayerCharacter player)
    {
        string playerCharacterId = player != null
            ? NormalizeCharacterId(player.CharacterId)
            : string.Empty;

        if (!string.IsNullOrWhiteSpace(playerCharacterId))
            return playerCharacterId;

        CharacterSaveData activeCharacter = bootstrap != null ? bootstrap.CharacterSession?.ActiveCharacter : null;
        if (activeCharacter != null)
            return NormalizeCharacterId(activeCharacter.CharacterId);

        return string.Empty;
    }

    public static bool CharacterIdsMatch(string firstCharacterId, string secondCharacterId)
    {
        string normalizedFirstCharacterId = NormalizeCharacterId(firstCharacterId);
        string normalizedSecondCharacterId = NormalizeCharacterId(secondCharacterId);

        return !string.IsNullOrWhiteSpace(normalizedFirstCharacterId)
            && !string.IsNullOrWhiteSpace(normalizedSecondCharacterId)
            && string.Equals(
                normalizedFirstCharacterId,
                normalizedSecondCharacterId,
                System.StringComparison.OrdinalIgnoreCase);
    }

    public static bool MatchesCharacter(
        PlayerCharacter firstPlayer,
        string firstCharacterId,
        PlayerCharacter secondPlayer,
        string secondCharacterId)
    {
        if (firstPlayer != null && secondPlayer != null && firstPlayer == secondPlayer)
            return true;

        return CharacterIdsMatch(firstCharacterId, secondCharacterId);
    }

    public static bool MatchesTrackedCharacter(
        GameBootstrap bootstrap,
        PlayerCharacter trackedPlayer,
        PlayerCharacter eventTarget,
        string eventCharacterId)
    {
        string trackedCharacterId = ResolveCharacterId(bootstrap, trackedPlayer);
        return MatchesCharacter(
            trackedPlayer,
            trackedCharacterId,
            eventTarget,
            eventCharacterId);
    }
}
