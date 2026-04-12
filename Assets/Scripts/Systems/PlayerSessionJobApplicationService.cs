using System;
using UnityEngine;

public class PlayerSessionJobApplicationService
{
    private readonly PlayerSessionSkillService skillService;
    private readonly PlayerSessionEventPublisher eventPublisher;
    private readonly Func<PlayerRuntimeData> getPlayerData;
    private readonly Action savePlayer;

    public PlayerSessionJobApplicationService(
        PlayerSessionSkillService skillService,
        PlayerSessionEventPublisher eventPublisher,
        Func<PlayerRuntimeData> getPlayerData,
        Action savePlayer)
    {
        this.skillService = skillService;
        this.eventPublisher = eventPublisher;
        this.getPlayerData = getPlayerData;
        this.savePlayer = savePlayer;
    }

    public bool HasActivePlayerData => ResolvePlayerData() != null;

    public bool TrySetPlayerJob(
        PlayerCharacter player,
        PlayerJobType newJobType)
    {
        PlayerRuntimeData playerData = ResolvePlayerData();
        if (playerData == null || playerData.CurrentJob == newJobType)
            return false;

        playerData.CurrentJob = newJobType;
        PlayerProgressionRules.RefreshDerivedState(playerData);
        skillService.EnsureDefaultSkillsForJob(playerData.CurrentJob);
        PublishJobState(player);
        savePlayer?.Invoke();
        return true;
    }

    public bool CanAdvanceToJob(
        PlayerJobType targetJob,
        int requiredLevel,
        out string message)
    {
        PlayerRuntimeData playerData = ResolvePlayerData();
        message = string.Empty;

        if (playerData == null)
        {
            message = "Player session is not ready.";
            return false;
        }

        if (targetJob == PlayerJobType.Drifter)
        {
            message = "Please choose a valid job.";
            return false;
        }

        if (playerData.CurrentJob != PlayerJobType.Drifter)
        {
            message = $"You are already a {FormatJobName(playerData.CurrentJob)}.";
            return false;
        }

        int normalizedRequiredLevel = Mathf.Max(1, requiredLevel);
        if (playerData.Level < normalizedRequiredLevel)
        {
            message = $"Reach level {normalizedRequiredLevel} as a {FormatJobName(PlayerJobType.Drifter)} first.";
            return false;
        }

        message = $"Choose your path: {FormatJobName(targetJob)}.";
        return true;
    }

    public bool TryAdvanceToJob(
        PlayerCharacter player,
        PlayerJobType targetJob,
        int requiredLevel,
        out string message)
    {
        if (!CanAdvanceToJob(targetJob, requiredLevel, out message))
            return false;

        if (!TrySetPlayerJob(player, targetJob))
        {
            message = "The job advancement could not be completed.";
            return false;
        }

        message = $"You are now a {FormatJobName(targetJob)}.";
        return true;
    }

    public void PublishJobState(PlayerCharacter player)
    {
        PlayerRuntimeData playerData = ResolvePlayerData();
        if (playerData == null || player == null)
            return;

        PlayerProgressionRules.RefreshDerivedState(playerData);
        eventPublisher.PublishJobState(playerData, player);
    }

    private PlayerRuntimeData ResolvePlayerData()
    {
        return getPlayerData != null ? getPlayerData() : null;
    }

    private static string FormatJobName(PlayerJobType jobType)
    {
        return PlayerJobCombatProfiles.GetDisplayName(jobType);
    }
}

