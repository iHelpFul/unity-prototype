using UnityEngine;

[CreateAssetMenu(menuName = "Game Data/Animation/Animation Profile")]
public class AnimationProfile : ScriptableObject
{
    public const string DefaultRespawnStateName = "Idle_Battle_SingleSword";
    public const string DefaultDeathStateName = "Die01Stay_SingleSword";

    [Header("Lifecycle States")]
    [SerializeField] private string respawnStateName = DefaultRespawnStateName;
    [SerializeField] private string deathStateName = DefaultDeathStateName;

    [Header("Skill Playback")]
    [SerializeField] private int skillAnimationLayerIndex = 1;
    [SerializeField] private float skillCrossFadeDuration = 0.04f;
    [SerializeField] private float skillStartNormalizedTime;

    public string RespawnStateName => respawnStateName;
    public string DeathStateName => deathStateName;
    public int SkillAnimationLayerIndex => skillAnimationLayerIndex;
    public float SkillCrossFadeDuration => skillCrossFadeDuration;
    public float SkillStartNormalizedTime => skillStartNormalizedTime;

    private void OnValidate()
    {
        respawnStateName = string.IsNullOrWhiteSpace(respawnStateName)
            ? DefaultRespawnStateName
            : respawnStateName.Trim();
        deathStateName = string.IsNullOrWhiteSpace(deathStateName)
            ? DefaultDeathStateName
            : deathStateName.Trim();
        skillAnimationLayerIndex = Mathf.Max(0, skillAnimationLayerIndex);
        skillCrossFadeDuration = Mathf.Max(0f, skillCrossFadeDuration);
        skillStartNormalizedTime = Mathf.Clamp01(skillStartNormalizedTime);
    }
}
