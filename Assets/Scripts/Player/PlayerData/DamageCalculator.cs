using UnityEngine;

public static class DamageCalculator
{
    public static int CalculateDamage(
        int strength,
        int dexterity,
        int weaponAttack,
        float mastery)
    {
        float primary = strength * 4.0f;
        float secondary = dexterity;

        float max =
            ((primary + secondary) * weaponAttack) / 100f;

        float min =
            (((primary * 0.9f) * mastery + secondary) * weaponAttack) / 100f;

        float damage = Random.Range(min, max);

        return Mathf.Max(1, Mathf.RoundToInt(damage));
    }
}