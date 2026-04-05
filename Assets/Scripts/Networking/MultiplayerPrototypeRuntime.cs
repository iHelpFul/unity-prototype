using System;

public static class MultiplayerPrototypeRuntime
{
    public const string DefaultGameplaySceneName = "Map_01";
    public const string DefaultAddress = "127.0.0.1";
    public const ushort DefaultPort = 7777;

    public static bool IsEnabled { get; private set; }
    public static string GameplaySceneName { get; private set; } = DefaultGameplaySceneName;

    public static void Enable(string gameplaySceneName)
    {
        IsEnabled = true;
        GameplaySceneName = string.IsNullOrWhiteSpace(gameplaySceneName)
            ? DefaultGameplaySceneName
            : gameplaySceneName.Trim();
    }
}
