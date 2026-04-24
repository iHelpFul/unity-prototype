using System.Collections.Generic;
using UnityEngine;

public enum CombatPresentationCueCondition
{
    Always = 0,
    OnHit = 1,
    OnMiss = 2,
    OnSurge = 3,
    OnNonSurgeHit = 4
}

[System.Serializable]
public class CombatPresentationCue
{
    [SerializeField] private CombatCuePhase phase = CombatCuePhase.HitImpact;
    [SerializeField] private CombatPresentationCueCondition condition = CombatPresentationCueCondition.Always;
    [SerializeField] private CombatSpawnTargetKind spawnTarget = CombatSpawnTargetKind.WorldPoint;
    [SerializeField] private Vector3 positionOffset;
    [SerializeField] private float delay;
    [SerializeField] private bool followTarget;

    [Header("VFX")]
    [SerializeField] private bool playVfx = true;
    [SerializeField] private VfxType vfxType = VfxType.EnemyHit;

    [Header("SFX")]
    [SerializeField] private bool playSfx;
    [SerializeField] private SfxType sfxType = SfxType.SwordHit;

    public CombatCuePhase Phase => phase;
    public CombatPresentationCueCondition Condition => condition;
    public CombatSpawnTargetKind SpawnTarget => spawnTarget;
    public Vector3 PositionOffset => positionOffset;
    public float Delay => delay;
    public bool FollowTarget => followTarget;
    public bool PlayVfx => playVfx;
    public VfxType VfxType => vfxType;
    public bool PlaySfx => playSfx;
    public SfxType SfxType => sfxType;

    public void Sanitize()
    {
        delay = Mathf.Max(0f, delay);
    }
}

[CreateAssetMenu(menuName = "Game Data/Combat/Presentation Cue Set")]
public class PresentationCueSet : ScriptableObject
{
    [SerializeField] private List<CombatPresentationCue> cues = new List<CombatPresentationCue>();

    public IReadOnlyList<CombatPresentationCue> Cues => cues;

    private void OnValidate()
    {
        cues ??= new List<CombatPresentationCue>();

        for (int index = 0; index < cues.Count; index++)
            cues[index]?.Sanitize();
    }
}
