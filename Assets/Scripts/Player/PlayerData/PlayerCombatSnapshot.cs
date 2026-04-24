using UnityEngine;

public struct PlayerCombatSnapshot
{
    public PlayerJobType CurrentJob;
    public int Level;
    public PlayerProgressionStatType CoreStat;
    public PlayerProgressionStatType SecondaryStat;
    public WeaponType CurrentWeaponType;
    public int Might;
    public int Precision;
    public int Arcane;
    public int Finesse;
    public int HitRate;
    public int WeaponPower;
    public float SkillMastery;
    public int Avoidance;
    public int Defense;
    public float SurgeChance;
    public float SurgePower;
    public float RangeModifier;
    public float ProjectileSpeedModifier;
    public float CastSpeedModifier;
    public float AttackSpeedModifier;
    public int MomentumStacks;
    public int MomentumGainBonus;
    public float StateDurationModifier;
    public CombatElementType CurrentElement;
}
