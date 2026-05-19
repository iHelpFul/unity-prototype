using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class PlayerCombatGizmoDrawer : MonoBehaviour
{
    [SerializeField] private PlayerFacade facade;
    [SerializeField] private PlayerCharacter character;
    [SerializeField] private Transform visual;

    [Header("Preview")]
    [SerializeField] private bool useRuntimeJobWhilePlaying = true;
    [SerializeField] private PlayerJobType previewJob = PlayerJobType.Vanguard;
    [SerializeField] private int previewSkillLevel = 1;
    [Range(0f, 1f)]
    [SerializeField] private float previewChargeRatio = 1f;
    [SerializeField] private bool drawLabels = true;

    [Header("Selection")]
    [SerializeField] private int previewTapSkillIndex;
    [SerializeField] private int previewBurstSkillIndex;
    [SerializeField] private PlayerSkillDefinition previewTapSkillOverride;
    [SerializeField] private PlayerSkillDefinition previewBurstSkillOverride;

    [Header("Gizmos")]
    [SerializeField] private bool drawBasicAttack = true;
    [SerializeField] private bool drawRegularSkills = true;
    [SerializeField] private bool drawBurstTapVariant = true;
    [SerializeField] private bool drawBurstChargedVariant = true;

    [Header("Colors")]
    [SerializeField] private Color basicAttackColor = new Color(1f, 0.25f, 0.25f, 0.3f);
    [SerializeField] private Color skillTapColor = new Color(1f, 0.78f, 0.2f, 0.35f);
    [SerializeField] private Color burstColor = new Color(1f, 0.45f, 0.2f, 0.38f);

    private struct AttackPreviewDescriptor
    {
        public string Title;
        public CombatTargetingKind TargetingKind;
        public CombatExecutionKind ExecutionKind;
        public ProjectileProfile ProjectileProfile;
        public int ProjectileCount;
        public float ProjectileSpreadAngle;
        public float ProjectileHitBoxRange;
        public CombatHitBoxDefinition ProjectileHitBox;
        public float Range;
        public int HitCount;
        public int MaxTargets;
        public int PierceCount;
        public float DamageFactor;
        public float BreakPower;
        public float SurgeChanceBonus;
        public float SurgePowerBonus;
        public float GaugeSpend;
        public float GaugeSpendNormalized;
        public float RadiusScale;
        public CombatHitBoxDefinition HitBox;

        public bool UsesProjectile =>
            ExecutionKind == CombatExecutionKind.Projectile;
    }

    private void Reset()
    {
        AssignReferences();
    }

    private void OnValidate()
    {
        previewSkillLevel = Mathf.Max(1, previewSkillLevel);
        previewChargeRatio = Mathf.Clamp01(previewChargeRatio);
        previewTapSkillIndex = Mathf.Max(0, previewTapSkillIndex);
        previewBurstSkillIndex = Mathf.Max(0, previewBurstSkillIndex);
        AssignReferences();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        AssignReferences();

        PlayerJobDefinition jobDefinition = ResolvePreviewJobDefinition();
        if (jobDefinition == null)
            return;

        Transform facingTransform = ResolveFacingTransform();
        if (facingTransform == null)
            return;

        if (drawBasicAttack)
        {
            PlayerBasicAttackProfile basicAttackProfile = ResolvePreviewBasicAttackProfile(jobDefinition);
            if (basicAttackProfile != null)
                DrawBasicAttackGizmo(facingTransform, BuildBasicAttackDescriptor(basicAttackProfile));
        }

        if (drawRegularSkills)
        {
            PlayerSkillDefinition tapSkill = ResolvePreviewTapSkill(jobDefinition);
            if (tapSkill != null)
                DrawSkillVariantGizmo(facingTransform, tapSkill, BuildTapSkillDescriptor(tapSkill), skillTapColor, "Tap");
        }

        if (drawBurstTapVariant || drawBurstChargedVariant)
        {
            PlayerSkillDefinition burstSkill = ResolvePreviewBurstSkill(jobDefinition);
            if (burstSkill != null)
            {
                if (drawBurstTapVariant)
                    DrawSkillVariantGizmo(facingTransform, burstSkill, BuildTapSkillDescriptor(burstSkill), skillTapColor, "Burst Tap");

                if (drawBurstChargedVariant && burstSkill.BurstLinkedSkillProfile != null)
                {
                    AttackPreviewDescriptor burstDescriptor = BuildBurstSkillDescriptor(
                        burstSkill,
                        burstSkill.BurstLinkedSkillProfile,
                        ResolvePreviewGaugeCapacity(jobDefinition),
                        previewChargeRatio);
                    DrawSkillVariantGizmo(facingTransform, burstSkill, burstDescriptor, burstColor, "Burst Charged");
                }
            }
        }

    }
#endif

    private void AssignReferences()
    {
        facade ??= GetComponent<PlayerFacade>();
        character ??= GetComponent<PlayerCharacter>();

        if (facade != null && facade.VisualTransform != null)
        {
            visual = facade.VisualTransform;
            return;
        }

        if (visual == null)
            visual = transform;
    }

    private Transform ResolveFacingTransform()
    {
        if (visual != null)
            return visual;

        if (facade != null && facade.VisualTransform != null)
            return facade.VisualTransform;

        return transform;
    }

    private PlayerJobDefinition ResolvePreviewJobDefinition()
    {
        PlayerJobType jobType = previewJob;
        if (Application.isPlaying && useRuntimeJobWhilePlaying && character != null)
            jobType = character.CurrentJob;

        return PlayerJobCombatProfiles.GetJobDefinition(jobType);
    }

    private PlayerBasicAttackProfile ResolvePreviewBasicAttackProfile(PlayerJobDefinition jobDefinition)
    {
        if (Application.isPlaying && useRuntimeJobWhilePlaying && character != null)
            return character.GetBasicAttackProfile();

        return jobDefinition != null ? jobDefinition.BasicAttackProfile : null;
    }

    private PlayerSkillDefinition ResolvePreviewTapSkill(PlayerJobDefinition jobDefinition)
    {
        if (previewTapSkillOverride != null)
            return previewTapSkillOverride;

        if (jobDefinition == null)
            return null;

        IReadOnlyList<PlayerSkillDefinition> skills = PlayerJobCombatProfiles.GetDefaultSkillsForJob(jobDefinition.JobType);
        List<PlayerSkillDefinition> tapSkills = new List<PlayerSkillDefinition>();

        for (int index = 0; index < skills.Count; index++)
        {
            PlayerSkillDefinition skill = skills[index];
            if (skill == null || skill.SkillType != PlayerSkillType.Attack || skill.HasBurstLinkedSkillProfile)
                continue;

            tapSkills.Add(skill);
        }

        if (tapSkills.Count == 0)
            return null;

        int resolvedIndex = Mathf.Clamp(previewTapSkillIndex, 0, tapSkills.Count - 1);
        return tapSkills[resolvedIndex];
    }

    private PlayerSkillDefinition ResolvePreviewBurstSkill(PlayerJobDefinition jobDefinition)
    {
        if (previewBurstSkillOverride != null)
            return previewBurstSkillOverride;

        if (jobDefinition == null)
            return null;

        IReadOnlyList<PlayerSkillDefinition> skills = PlayerJobCombatProfiles.GetDefaultSkillsForJob(jobDefinition.JobType);
        List<PlayerSkillDefinition> burstSkills = new List<PlayerSkillDefinition>();

        for (int index = 0; index < skills.Count; index++)
        {
            PlayerSkillDefinition skill = skills[index];
            if (skill == null || skill.SkillType != PlayerSkillType.Attack || !skill.HasBurstLinkedSkillProfile)
                continue;

            burstSkills.Add(skill);
        }

        if (burstSkills.Count == 0)
            return null;

        int resolvedIndex = Mathf.Clamp(previewBurstSkillIndex, 0, burstSkills.Count - 1);
        return burstSkills[resolvedIndex];
    }

    private void DrawBasicAttackGizmo(Transform facingTransform, AttackPreviewDescriptor descriptor)
    {
        DrawDescriptor(
            facingTransform,
            descriptor,
            basicAttackColor,
            BuildDescriptorLabel(descriptor));
    }

    private void DrawSkillVariantGizmo(
        Transform facingTransform,
        PlayerSkillDefinition definition,
        AttackPreviewDescriptor descriptor,
        Color color,
        string variantLabel)
    {
        if (definition == null)
            return;

        string label = BuildDescriptorLabel(descriptor, variantLabel);

        DrawDescriptor(facingTransform, descriptor, color, label);
    }

    private void DrawDescriptor(Transform facingTransform, AttackPreviewDescriptor descriptor, Color fillColor, string label)
    {
        if (descriptor.UsesProjectile)
        {
            DrawProjectileDescriptor(facingTransform, descriptor, fillColor, label);
            return;
        }
        DrawHitBoxDescriptor(facingTransform, descriptor, fillColor, label);
    }

#if UNITY_EDITOR
    private void DrawHitBoxDescriptor(Transform facingTransform, AttackPreviewDescriptor descriptor, Color fillColor, string label)
    {
        CombatHitBoxWorldQuery query = descriptor.HitBox.BuildWorldQuery(facingTransform, descriptor.Range);
        Matrix4x4 previousMatrix = Gizmos.matrix;

        Gizmos.color = fillColor;
        Gizmos.matrix = Matrix4x4.TRS(query.Center, query.Rotation, Vector3.one);
        Gizmos.DrawCube(Vector3.zero, query.HalfExtents * 2f);
        Gizmos.color = ToWireColor(fillColor);
        Gizmos.DrawWireCube(Vector3.zero, query.HalfExtents * 2f);
        Gizmos.matrix = previousMatrix;

        Gizmos.color = ToWireColor(fillColor);
        Gizmos.DrawLine(facingTransform.position, query.Center);
        if (drawLabels)
            Handles.Label(query.Center + facingTransform.up * (query.HalfExtents.y + 0.18f), label);
    }

    private void DrawProjectileDescriptor(Transform facingTransform, AttackPreviewDescriptor descriptor, Color fillColor, string label)
    {
        Vector3 origin = facingTransform.position;
        Vector3 forward = facingTransform.forward;
        float range = Mathf.Max(0.1f, descriptor.Range);
        int projectileCount = Mathf.Max(1, descriptor.ProjectileCount);
        float spreadAngle = Mathf.Max(0f, descriptor.ProjectileSpreadAngle);
        float lateralSpacing = projectileCount > 1 ? 0.16f : 0f;
        Vector3 spawnBasePosition = facingTransform.position
            + forward * ProjectileProfileUtility.ResolveSpawnForwardOffset(descriptor.ProjectileProfile)
            + Vector3.up * ProjectileProfileUtility.ResolveSpawnUpOffset(descriptor.ProjectileProfile);

        Gizmos.color = fillColor;
        Gizmos.DrawLine(origin, spawnBasePosition);

        Handles.color = ToWireColor(fillColor);

        for (int projectileIndex = 0; projectileIndex < projectileCount; projectileIndex++)
        {
            float normalizedIndex = projectileCount == 1
                ? 0.5f
                : projectileIndex / (float)(projectileCount - 1);
            float yawOffset = projectileCount == 1
                ? 0f
                : Mathf.Lerp(-spreadAngle * 0.5f, spreadAngle * 0.5f, normalizedIndex);
            float lateralOffset = projectileCount == 1
                ? 0f
                : Mathf.Lerp(-lateralSpacing * 0.5f, lateralSpacing * 0.5f, normalizedIndex);

            Vector3 spawnPosition = spawnBasePosition + facingTransform.right * lateralOffset;
            Vector3 direction = Quaternion.AngleAxis(yawOffset, Vector3.up) * forward;
            CombatHitBoxWorldQuery query = descriptor.ProjectileHitBox.BuildWorldQuery(
                spawnPosition,
                Quaternion.LookRotation(direction, Vector3.up),
                Mathf.Max(0.05f, descriptor.Range + descriptor.ProjectileHitBoxRange));
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.color = fillColor;
            Gizmos.matrix = Matrix4x4.TRS(query.Center, query.Rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, query.HalfExtents * 2f);
            Gizmos.color = ToWireColor(fillColor);
            Gizmos.DrawWireCube(Vector3.zero, query.HalfExtents * 2f);
            Gizmos.matrix = previousMatrix;
            Handles.DrawLine(spawnPosition, spawnPosition + direction * range);
        }

        if (drawLabels)
            Handles.Label(spawnBasePosition + Vector3.up * 0.2f, label);
    }
#endif

    private AttackPreviewDescriptor BuildBasicAttackDescriptor(PlayerBasicAttackProfile profile)
    {
        ProjectileProfile projectileProfile = profile.DefaultProjectileProfile;
        int projectileTargets = projectileProfile != null
            ? Mathf.Max(1, projectileProfile.MaxTargets)
            : Mathf.Max(1, profile.MaxTargets);

        return new AttackPreviewDescriptor
        {
            Title = profile.DisplayName,
            TargetingKind = profile.TargetingKind,
            ExecutionKind = profile.ExecutionKind,
            HitBox = profile.HitBox,
            ProjectileProfile = projectileProfile,
            ProjectileCount = 1,
            ProjectileSpreadAngle = 0f,
            ProjectileHitBoxRange = ProjectileProfileUtility.ResolveCollisionRange(projectileProfile),
            ProjectileHitBox = ProjectileProfileUtility.ResolveCollisionHitBox(projectileProfile),
            Range = Mathf.Max(0.1f, profile.BaseRange),
            HitCount = Mathf.Max(1, profile.HitCount),
            MaxTargets = projectileTargets,
            DamageFactor = Mathf.Max(0.05f, profile.BasicDamageMultiplier * Mathf.Max(0.05f, profile.DamageCoefficient))
        };
    }

    private AttackPreviewDescriptor BuildTapSkillDescriptor(PlayerSkillDefinition definition)
    {
        int skillLevel = Mathf.Max(1, previewSkillLevel);
        PlayerSkillLevelDefinition levelDefinition = definition.GetLevelDefinition(skillLevel);
        float levelDamageCoefficient = levelDefinition != null
            ? Mathf.Max(0.05f, levelDefinition.DamageCoefficient)
            : 1f;
        ProjectileProfile projectileProfile = definition.GetResolvedProjectileProfile(skillLevel);

        return new AttackPreviewDescriptor
        {
            Title = definition.DisplayName,
            TargetingKind = definition.CombatTargetingKind,
            ExecutionKind = definition.ExecutionKind,
            HitBox = definition.HitBox,
            ProjectileProfile = projectileProfile,
            ProjectileCount = Mathf.Max(1, definition.GetResolvedProjectileCount(skillLevel)),
            ProjectileSpreadAngle = Mathf.Max(0f, definition.ProjectileSpreadAngle),
            ProjectileHitBoxRange = Mathf.Max(0.05f, definition.GetResolvedProjectileHitBoxRange(skillLevel)),
            ProjectileHitBox = definition.GetResolvedProjectileHitBox(skillLevel),
            Range = Mathf.Max(0.1f, definition.GetResolvedRange(skillLevel)),
            HitCount = Mathf.Max(1, definition.GetResolvedHitCount(skillLevel)),
            MaxTargets = Mathf.Max(1, definition.GetResolvedMaxTargets(skillLevel)),
            DamageFactor = Mathf.Max(0.1f, definition.DamageMultiplier * levelDamageCoefficient)
        };
    }

    private AttackPreviewDescriptor BuildBurstSkillDescriptor(
        PlayerSkillDefinition definition,
        BurstLinkedSkillProfile profile,
        float availableGauge,
        float holdRatio)
    {
        int skillLevel = Mathf.Max(1, previewSkillLevel);
        PlayerSkillLevelDefinition levelDefinition = definition.GetLevelDefinition(skillLevel);
        ProjectileProfile projectileProfile = definition.GetResolvedProjectileProfile(skillLevel);
        float gaugeSpent = ResolveChargedGaugeSpend(
            profile.MinimumGaugeToStart,
            profile.AllowPartialGaugeSpend,
            profile.GaugeScalingProfile,
            availableGauge,
            holdRatio);
        float normalizedGaugeSpend = ResolveGaugeSpendNormalized(profile.GaugeScalingProfile, gaugeSpent);
        float gaugeDamageMultiplier = ResolveScaledFloat(
            profile.GaugeScalingProfile != null ? profile.GaugeScalingProfile.DamageMultiplierBySpend : null,
            normalizedGaugeSpend,
            1f);
        float gaugeRangeMultiplier = ResolveScaledFloat(
            profile.GaugeScalingProfile != null ? profile.GaugeScalingProfile.RangeBySpend : null,
            normalizedGaugeSpend,
            1f);
        float gaugeBreakPower = ResolveScaledFloat(
            profile.GaugeScalingProfile != null ? profile.GaugeScalingProfile.BreakPowerBySpend : null,
            normalizedGaugeSpend,
            0f,
            useFallbackWhenDisabled: false);
        int gaugeHits = ResolveScaledInt(
            profile.GaugeScalingProfile != null ? profile.GaugeScalingProfile.HitCountBySpend : null,
            normalizedGaugeSpend,
            definition.GetResolvedHitCount(skillLevel),
            1);
        int gaugeTargets = ResolveScaledInt(
            profile.GaugeScalingProfile != null ? profile.GaugeScalingProfile.MaxTargetsBySpend : null,
            normalizedGaugeSpend,
            definition.GetResolvedMaxTargets(skillLevel),
            1);
        int pierceCount = ResolveScaledInt(
            profile.GaugeScalingProfile != null ? profile.GaugeScalingProfile.PierceCountBySpend : null,
            normalizedGaugeSpend,
            0,
            0);
        int projectileCount = ResolveScaledInt(
            profile.GaugeScalingProfile != null ? profile.GaugeScalingProfile.ProjectileCountBySpend : null,
            normalizedGaugeSpend,
            definition.GetResolvedProjectileCount(skillLevel),
            1);
        float radiusScale = ResolveScaledFloat(
            profile.GaugeScalingProfile != null ? profile.GaugeScalingProfile.RadiusBySpend : null,
            normalizedGaugeSpend,
            1f);
        float surgeChanceBonus = ResolveScaledFloat(
            profile.GaugeScalingProfile != null ? profile.GaugeScalingProfile.SurgeChanceBySpend : null,
            normalizedGaugeSpend,
            0f,
            useFallbackWhenDisabled: false);
        float surgePowerBonus = ResolveScaledFloat(
            profile.GaugeScalingProfile != null ? profile.GaugeScalingProfile.SurgePowerBySpend : null,
            normalizedGaugeSpend,
            0f,
            useFallbackWhenDisabled: false);
        CombatHitBoxDefinition resolvedHitBox = profile.ChargedHitBox.GetRadiusScaled(radiusScale);
        float levelDamageCoefficient = levelDefinition != null
            ? Mathf.Max(0.05f, levelDefinition.DamageCoefficient)
            : 1f;

        return new AttackPreviewDescriptor
        {
            Title = definition.DisplayName,
            TargetingKind = definition.CombatTargetingKind,
            ExecutionKind = definition.ExecutionKind,
            HitBox = resolvedHitBox,
            ProjectileProfile = projectileProfile,
            ProjectileCount = projectileCount,
            ProjectileSpreadAngle = Mathf.Max(0f, definition.ProjectileSpreadAngle),
            ProjectileHitBoxRange = Mathf.Max(0.05f, definition.GetResolvedProjectileHitBoxRange(skillLevel)),
            ProjectileHitBox = definition.GetResolvedProjectileHitBox(skillLevel),
            Range = Mathf.Max(0.1f, definition.GetResolvedRange(skillLevel) * gaugeRangeMultiplier),
            HitCount = gaugeHits,
            MaxTargets = gaugeTargets,
            PierceCount = pierceCount,
            DamageFactor = Mathf.Max(0.1f, definition.DamageMultiplier * levelDamageCoefficient * gaugeDamageMultiplier),
            BreakPower = Mathf.Max(0f, gaugeBreakPower),
            SurgeChanceBonus = surgeChanceBonus,
            SurgePowerBonus = surgePowerBonus,
            GaugeSpend = gaugeSpent,
            GaugeSpendNormalized = normalizedGaugeSpend,
            RadiusScale = radiusScale,
        };
    }

    private string BuildDescriptorLabel(AttackPreviewDescriptor descriptor, string variantLabel = "")
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(descriptor.Title);

        if (!string.IsNullOrWhiteSpace(variantLabel))
            builder.Append(" [").Append(variantLabel).Append(']');

        builder.AppendLine();
        builder.Append("Range ").Append(descriptor.Range.ToString("0.00"));
        builder.Append(" | Hits ").Append(descriptor.HitCount);
        builder.Append(" | Targets ").Append(descriptor.MaxTargets);

        if (descriptor.GaugeSpend > 0f)
        {
            builder.AppendLine();
            builder.Append("Gauge ").Append(descriptor.GaugeSpend.ToString("0.00"));
            builder.Append(" | Norm ").Append(descriptor.GaugeSpendNormalized.ToString("0.00"));
        }

        if (!descriptor.UsesProjectile)
        {
            float forwardExtent = descriptor.HitBox.GetForwardExtent(descriptor.Range);
            float backwardExtent = descriptor.HitBox.GetBackwardExtent(descriptor.Range);
            builder.AppendLine();
            builder.Append("Box F ").Append(forwardExtent.ToString("0.00"));
            builder.Append(" | B ").Append(backwardExtent.ToString("0.00"));
            builder.Append(" | Up ").Append(descriptor.HitBox.UpExtent.ToString("0.00"));
            builder.Append(" | Down ").Append(descriptor.HitBox.DownExtent.ToString("0.00"));
            builder.Append(" | Depth ").Append(descriptor.HitBox.DepthExtent.ToString("0.00"));
        }

        builder.AppendLine();
        builder.Append("Dmg x").Append(descriptor.DamageFactor.ToString("0.00"));

        if (descriptor.BreakPower > 0f)
            builder.Append(" | Break ").Append(descriptor.BreakPower.ToString("0.00"));

        if (descriptor.PierceCount > 0)
            builder.Append(" | Pierce ").Append(descriptor.PierceCount);

        if (descriptor.UsesProjectile)
            builder.Append(" | Proj ").Append(descriptor.ProjectileCount);

        if (!Mathf.Approximately(descriptor.RadiusScale, 1f))
            builder.Append(" | Radius x").Append(descriptor.RadiusScale.ToString("0.00"));

        if (descriptor.SurgeChanceBonus > 0f)
            builder.Append(" | Surge% +").Append(descriptor.SurgeChanceBonus.ToString("0.00"));

        if (descriptor.SurgePowerBonus > 0f)
            builder.Append(" | SurgePow +").Append(descriptor.SurgePowerBonus.ToString("0.00"));

        return builder.ToString();
    }

    private float ResolvePreviewGaugeCapacity(PlayerJobDefinition jobDefinition)
    {
        if (Application.isPlaying && useRuntimeJobWhilePlaying && character != null && character.MaxGauge > 0f)
            return Mathf.Max(0f, character.MaxGauge);

        return jobDefinition != null
            ? Mathf.Max(0f, jobDefinition.BaseMaxGauge)
            : 0f;
    }

    private float ResolveChargedGaugeSpend(
        float minimumGaugeToStart,
        bool allowPartialGaugeSpend,
        GaugeScalingProfile scalingProfile,
        float availableGauge,
        float holdRatio)
    {
        float clampedAvailableGauge = Mathf.Max(0f, availableGauge);
        float minimumGaugeSpend = Mathf.Clamp(Mathf.Max(0f, minimumGaugeToStart), 0f, clampedAvailableGauge);
        float configuredMaximumGaugeSpend = scalingProfile != null
            ? Mathf.Max(minimumGaugeSpend, scalingProfile.MaximumGaugeSpend)
            : clampedAvailableGauge;

        if (!allowPartialGaugeSpend)
            return Mathf.Clamp(configuredMaximumGaugeSpend, 0f, clampedAvailableGauge);

        AnimationCurve spendCurve = scalingProfile != null ? scalingProfile.SpendByHoldNormalized : null;
        float spendNormalizedByHold = spendCurve != null
            ? Mathf.Clamp01(spendCurve.Evaluate(Mathf.Clamp01(holdRatio)))
            : Mathf.Clamp01(holdRatio);
        float desiredSpend = Mathf.Lerp(minimumGaugeSpend, configuredMaximumGaugeSpend, spendNormalizedByHold);
        return Mathf.Clamp(desiredSpend, minimumGaugeSpend, clampedAvailableGauge);
    }

    private float ResolveGaugeSpendNormalized(GaugeScalingProfile scalingProfile, float gaugeSpent)
    {
        if (scalingProfile == null)
            return 0f;

        float minimumSpend = Mathf.Max(0f, scalingProfile.MinimumGaugeSpend);
        float maximumSpend = Mathf.Max(minimumSpend, scalingProfile.MaximumGaugeSpend);
        if (maximumSpend <= minimumSpend)
            return gaugeSpent > 0f ? 1f : 0f;

        return Mathf.Clamp01((gaugeSpent - minimumSpend) / (maximumSpend - minimumSpend));
    }

    private static float ResolveScaledFloat(
        GaugeScaledFloatParameter parameter,
        float normalizedGaugeSpend,
        float fallbackValue,
        bool useFallbackWhenDisabled = true)
    {
        if (parameter == null || !parameter.Enabled)
            return useFallbackWhenDisabled ? fallbackValue : 0f;

        return parameter.Evaluate(normalizedGaugeSpend);
    }

    private static int ResolveScaledInt(
        GaugeScaledIntParameter parameter,
        float normalizedGaugeSpend,
        int fallbackValue,
        int minimumValue)
    {
        int clampedFallback = Mathf.Max(minimumValue, fallbackValue);
        if (parameter == null || !parameter.Enabled)
            return clampedFallback;

        return Mathf.Max(clampedFallback, parameter.Evaluate(normalizedGaugeSpend));
    }

    private static Color ToWireColor(Color fillColor)
    {
        return new Color(fillColor.r, fillColor.g, fillColor.b, 1f);
    }
}
