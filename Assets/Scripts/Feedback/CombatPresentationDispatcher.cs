using System.Collections.Generic;
using UnityEngine;

public readonly struct CombatPresentationContext
{
    public CombatPresentationContext(
        Vector3 worldPosition,
        Transform source,
        Transform target,
        Transform projectile = null)
    {
        WorldPosition = worldPosition;
        Source = source;
        Target = target;
        Projectile = projectile;
    }

    public Vector3 WorldPosition { get; }
    public Transform Source { get; }
    public Transform Target { get; }
    public Transform Projectile { get; }
}

public readonly struct CombatPresentationRequest
{
    public CombatPresentationRequest(
        CombatPresentationContext context,
        bool didHit,
        bool didSurge,
        int damage,
        float impactDuration,
        PresentationCueSet cueSet = null,
        bool playImpactFeedback = true,
        bool playDamageNumber = true)
    {
        Context = context;
        DidHit = didHit;
        DidSurge = didSurge;
        Damage = damage;
        ImpactDuration = impactDuration;
        CueSet = cueSet;
        PlayImpactFeedback = playImpactFeedback;
        PlayDamageNumber = playDamageNumber;
    }

    public CombatPresentationContext Context { get; }
    public Vector3 WorldPosition => Context.WorldPosition;
    public Transform Source => Context.Source;
    public Transform Target => Context.Target;
    public Transform Projectile => Context.Projectile;
    public bool DidHit { get; }
    public bool DidSurge { get; }
    public int Damage { get; }
    public float ImpactDuration { get; }
    public PresentationCueSet CueSet { get; }
    public bool PlayImpactFeedback { get; }
    public bool PlayDamageNumber { get; }
}

public static class CombatPresentationDispatcher
{
    public static PresentationCueSet ResolveCueSet(AttackPayload payload)
    {
        if (payload == null)
            return null;

        switch (payload.ActionKind)
        {
            case AttackPayloadActionKind.Skill:
                PlayerSkillDefinition skillDefinition = PlayerSkillDatabase.GetDefinition(payload.ActionId);
                return skillDefinition != null ? skillDefinition.PresentationCueSet : null;

            case AttackPayloadActionKind.BurstLinkedSkill:
                PlayerSkillDefinition burstSkillDefinition = PlayerSkillDatabase.GetDefinition(payload.ActionId);
                return burstSkillDefinition != null
                    ? burstSkillDefinition.BurstPresentationCueSet
                    : null;

            case AttackPayloadActionKind.BasicAttack:
                PlayerJobDefinition jobDefinition = PlayerJobCombatProfiles.GetJobDefinition(payload.SourceJobType);
                return jobDefinition != null && jobDefinition.BasicAttackProfile != null
                    ? jobDefinition.BasicAttackProfile.PresentationCueSet
                    : null;
        }

        return null;
    }

    public static int PublishCuePhase(
        PresentationCueSet cueSet,
        CombatCuePhase phase,
        Vector3 worldPosition,
        Transform source = null,
        Transform target = null,
        Transform projectile = null,
        bool didHit = true,
        bool didSurge = false,
        int damage = 0)
    {
        CombatPresentationRequest request = new CombatPresentationRequest(
            new CombatPresentationContext(worldPosition, source, target, projectile),
            didHit,
            didSurge,
            damage,
            impactDuration: 0f,
            cueSet,
            playImpactFeedback: true,
            playDamageNumber: false);

        return PublishCuePhase(request, phase);
    }

    public static int StopCuePhase(
        PresentationCueSet cueSet,
        CombatCuePhase phase,
        Vector3 worldPosition,
        Transform source = null,
        Transform target = null,
        Transform projectile = null,
        bool didHit = true,
        bool didSurge = false,
        int damage = 0)
    {
        CombatPresentationRequest request = new CombatPresentationRequest(
            new CombatPresentationContext(worldPosition, source, target, projectile),
            didHit,
            didSurge,
            damage,
            impactDuration: 0f,
            cueSet,
            playImpactFeedback: false,
            playDamageNumber: false);

        return StopCuePhase(request, phase);
    }

    public static void PublishEnemyResult(CombatPresentationRequest request)
    {
        if (!request.DidHit)
        {
            PublishMissNumber(request);
            PublishCuePhase(request, CombatCuePhase.MissImpact);
            return;
        }

        PublishHitStop(request);

        if (request.PlayDamageNumber && request.Damage > 0)
            PublishHitNumber(request);

        if (!request.PlayImpactFeedback)
            return;

        int cuesPlayed = PublishCuePhase(request, CombatCuePhase.HitImpact);
        if (cuesPlayed > 0)
            return;

        EventBus.Publish(new PlaySfxEvent
        {
            Type = SfxType.SwordHit,
            Position = request.WorldPosition
        });

        EventBus.Publish(new PlayVfxEvent
        {
            Type = VfxType.EnemyHit,
            Position = request.WorldPosition + Vector3.up * 1f,
            Rotation = Quaternion.identity
        });
    }

    private static int PublishCuePhase(CombatPresentationRequest request, CombatCuePhase phase)
    {
        if (request.CueSet == null || request.CueSet.Cues == null)
            return 0;

        int cuesPlayed = 0;
        IReadOnlyList<CombatPresentationCue> cues = request.CueSet.Cues;
        for (int index = 0; index < cues.Count; index++)
        {
            CombatPresentationCue cue = cues[index];
            if (cue == null || cue.Phase != phase || !ShouldPlayCue(cue, request))
                continue;

            PublishCue(cue, request);
            cuesPlayed++;
        }

        return cuesPlayed;
    }

    private static int StopCuePhase(CombatPresentationRequest request, CombatCuePhase phase)
    {
        if (request.CueSet == null || request.CueSet.Cues == null)
            return 0;

        int cuesStopped = 0;
        IReadOnlyList<CombatPresentationCue> cues = request.CueSet.Cues;
        for (int index = 0; index < cues.Count; index++)
        {
            CombatPresentationCue cue = cues[index];
            if (cue == null
                || cue.Phase != phase
                || (!cue.PersistVfxUntilStopped && !cue.PersistSfxUntilStopped)
                || !ShouldPlayCue(cue, request))
            {
                continue;
            }

            StopCue(cue, request);
            cuesStopped++;
        }

        return cuesStopped;
    }

    private static bool ShouldPlayCue(CombatPresentationCue cue, CombatPresentationRequest request)
    {
        switch (cue.Condition)
        {
            case CombatPresentationCueCondition.OnHit:
                return request.DidHit;

            case CombatPresentationCueCondition.OnMiss:
                return !request.DidHit;

            case CombatPresentationCueCondition.OnSurge:
                return request.DidHit && request.DidSurge;

            case CombatPresentationCueCondition.OnNonSurgeHit:
                return request.DidHit && !request.DidSurge;

            default:
                return true;
        }
    }

    private static void PublishCue(CombatPresentationCue cue, CombatPresentationRequest request)
    {
        Transform anchor = ResolveAnchor(cue.SpawnTarget, request);
        Vector3 position = ResolveCuePosition(cue, request, anchor);

        if (cue.PlaySfx)
        {
            EventBus.Publish(new PlaySfxEvent
            {
                Type = cue.SfxType,
                Position = position,
                Delay = cue.Delay,
                FollowTarget = cue.FollowTarget ? anchor : null,
                FollowOffset = cue.PositionOffset,
                Persistent = cue.PersistSfxUntilStopped,
                TrackingTarget = ResolveTrackingTarget(cue, request, anchor, cue.PersistSfxUntilStopped)
            });
        }

        if (cue.PlayVfx)
        {
            EventBus.Publish(new PlayVfxEvent
            {
                Type = cue.VfxType,
                Position = position,
                Rotation = Quaternion.identity,
                Delay = cue.Delay,
                FollowTarget = cue.FollowTarget ? anchor : null,
                FollowOffset = cue.PositionOffset,
                Persistent = cue.PersistVfxUntilStopped,
                TrackingTarget = ResolveTrackingTarget(cue, request, anchor, cue.PersistVfxUntilStopped),
                OverrideLifetime = cue.OverrideVfxLifetime,
                Lifetime = cue.VfxLifetime
            });
        }
    }

    private static Transform ResolveAnchor(CombatSpawnTargetKind spawnTarget, CombatPresentationRequest request)
    {
        switch (spawnTarget)
        {
            case CombatSpawnTargetKind.Self:
            case CombatSpawnTargetKind.Weapon:
                return request.Source;

            case CombatSpawnTargetKind.Projectile:
                return request.Projectile;

            case CombatSpawnTargetKind.Target:
                return request.Target;

            default:
                return null;
        }
    }

    private static Vector3 ResolveCuePosition(
        CombatPresentationCue cue,
        CombatPresentationRequest request,
        Transform anchor)
    {
        if (anchor != null)
            return anchor.position + cue.PositionOffset;

        return request.WorldPosition + cue.PositionOffset;
    }

    private static Transform ResolveTrackingTarget(
        CombatPresentationCue cue,
        CombatPresentationRequest request,
        Transform anchor,
        bool isPersistent)
    {
        if (!isPersistent)
            return null;

        return anchor != null ? anchor : request.Source;
    }

    private static void StopCue(CombatPresentationCue cue, CombatPresentationRequest request)
    {
        Transform anchor = ResolveAnchor(cue.SpawnTarget, request);
        Transform trackingTarget = ResolveTrackingTarget(
            cue,
            request,
            anchor,
            cue.PersistVfxUntilStopped || cue.PersistSfxUntilStopped);
        if (trackingTarget == null)
            return;

        if (cue.PlayVfx && cue.PersistVfxUntilStopped)
        {
            EventBus.Publish(new StopVfxEvent
            {
                Type = cue.VfxType,
                TrackingTarget = trackingTarget
            });
        }

        if (cue.PlaySfx && cue.PersistSfxUntilStopped)
        {
            EventBus.Publish(new StopSfxEvent
            {
                Type = cue.SfxType,
                TrackingTarget = trackingTarget
            });
        }
    }

    private static void PublishMissNumber(CombatPresentationRequest request)
    {
        EventBus.Publish(new DamageNumberEvent
        {
            WorldPosition = request.WorldPosition + Vector3.up * 1.2f,
            Target = request.Target,
            Kind = CombatFloatingTextKind.Evade,
            Text = "Evade"
        });
    }

    private static void PublishHitNumber(CombatPresentationRequest request)
    {
        EventBus.Publish(new DamageNumberEvent
        {
            WorldPosition = request.WorldPosition + Vector3.up * 1.2f,
            Target = request.Target,
            Kind = request.DidSurge ? CombatFloatingTextKind.SurgeDamage : CombatFloatingTextKind.Damage,
            Damage = request.Damage
        });
    }

    private static void PublishHitStop(CombatPresentationRequest request)
    {
        EventBus.Publish(new HitImpactEvent
        {
            Duration = request.DidSurge ? request.ImpactDuration * 1.25f : request.ImpactDuration,
            TimeScale = request.DidSurge ? 0.08f : 0.1f,
            Damage = Mathf.Max(0, request.Damage)
        });
    }
}
