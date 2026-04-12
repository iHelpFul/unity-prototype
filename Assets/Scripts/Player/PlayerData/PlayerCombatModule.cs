using UnityEngine;

public class PlayerCombatModule
{
    private int currentAnimationIndex;
    private int currentChainCount;
    private bool isAttacking;
    private bool isSkillAttack;
    private bool attackBuffered;
    private float attackTimer;
    private float activeAttackDuration;
    private float activeAttackAnimationSpeed = 1f;
    private int comboCounter;
    private float comboCounterTimer;
    private PlayerBasicAttackProfile basicAttackProfile =
        PlayerJobCombatProfiles.GetBasicAttackProfile(PlayerJobType.Drifter);
    private float lastAttackTime;

    public int ComboIndex => currentAnimationIndex;
    public bool IsAttacking => isAttacking;
    public bool IsSkillAttack => isSkillAttack;
    public int CurrentChainCount => currentChainCount;
    public int CurrentComboCounter => comboCounter;
    public int MaxBasicTargets => Mathf.Max(1, basicAttackProfile.MaxTargets);
    public float AttackAnimationSpeed => isSkillAttack
        ? activeAttackAnimationSpeed
        : basicAttackProfile.AttackAnimationSpeed;
    public bool SupportsComboCounter => basicAttackProfile.SupportsComboCounter;

    public void SetBasicAttackProfile(PlayerBasicAttackProfile profile)
    {
        if (profile == null)
            profile = PlayerJobCombatProfiles.GetBasicAttackProfile(PlayerJobType.Drifter);

        if (basicAttackProfile.JobType == profile.JobType
            && basicAttackProfile.AnimationVariantCount == profile.AnimationVariantCount
            && basicAttackProfile.MaxChainCount == profile.MaxChainCount
            && basicAttackProfile.SelectionMode == profile.SelectionMode
            && Mathf.Approximately(basicAttackProfile.AttackCooldown, profile.AttackCooldown)
            && Mathf.Approximately(basicAttackProfile.MaxAttackDuration, profile.MaxAttackDuration)
            && Mathf.Approximately(basicAttackProfile.AttackAnimationSpeed, profile.AttackAnimationSpeed)
            && basicAttackProfile.MaxTargets == profile.MaxTargets
            && Mathf.Approximately(basicAttackProfile.BasicDamageMultiplier, profile.BasicDamageMultiplier)
            && basicAttackProfile.MaxComboCounter == profile.MaxComboCounter
            && Mathf.Approximately(basicAttackProfile.ComboResetDelay, profile.ComboResetDelay)
            && Mathf.Approximately(basicAttackProfile.ComboDamageBonusPerStack, profile.ComboDamageBonusPerStack)
            && basicAttackProfile.SupportsComboCounter == profile.SupportsComboCounter)
        {
            return;
        }

        basicAttackProfile = profile;
        ForceReset();
        comboCounter = 0;
        comboCounterTimer = 0f;
    }

    public void Tick(float deltaTime)
    {
        TickComboCounter(deltaTime);

        if (!isAttacking)
            return;

        attackTimer += deltaTime;

        float attackDuration = isSkillAttack
            ? activeAttackDuration
            : basicAttackProfile.MaxAttackDuration;

        if (attackTimer >= attackDuration)
            ForceReset();
    }

    public void RequestAttack()
    {
        if (!isAttacking)
        {
            if (Time.time < lastAttackTime + basicAttackProfile.AttackCooldown)
                return;

            lastAttackTime = Time.time;

            isAttacking = true;
            currentChainCount = 1;
            currentAnimationIndex = ResolveNextAnimationIndex(0);
            attackBuffered = false;
            attackTimer = 0f;
        }
        else
        {
            attackBuffered = true;
        }
    }

    public void OnComboWindow()
    {
        if (isSkillAttack)
            return;

        if (!attackBuffered)
            return;

        if (currentChainCount >= basicAttackProfile.MaxChainCount)
        {
            attackBuffered = false;
            return;
        }

        currentChainCount++;
        currentAnimationIndex = ResolveNextAnimationIndex(currentAnimationIndex);
        attackBuffered = false;
        attackTimer = 0f;
    }

    public void EndAttack()
    {
        ForceReset();
    }

    public bool TryStartSkillAttack(PlayerSkillDefinition skill)
    {
        if (skill == null || isAttacking)
            return false;

        isAttacking = true;
        isSkillAttack = true;
        currentChainCount = 1;
        currentAnimationIndex = Mathf.Max(1, skill.AnimationVariantIndex);
        attackBuffered = false;
        attackTimer = 0f;
        activeAttackDuration = Mathf.Max(0.1f, skill.AttackDuration);
        activeAttackAnimationSpeed = Mathf.Max(0.1f, skill.AnimationSpeed);
        lastAttackTime = Time.time;
        return true;
    }

    private void ForceReset()
    {
        isAttacking = false;
        isSkillAttack = false;
        currentAnimationIndex = 0;
        currentChainCount = 0;
        attackBuffered = false;
        attackTimer = 0f;
        activeAttackDuration = 0f;
        activeAttackAnimationSpeed = 1f;
    }

    public int CalculateDamage(PlayerCombatSnapshot snapshot)
    {
        return DamageCalculator.CalculateDamage(snapshot, isSkillDamage: false);
    }

    public int CalculateDamage(PlayerCombatSnapshot snapshot, bool isSkillDamage)
    {
        return DamageCalculator.CalculateDamage(snapshot, isSkillDamage);
    }

    public int CalculateBasicDamage(PlayerCombatSnapshot snapshot)
    {
        int baseDamage = CalculateDamage(snapshot);
        float comboMultiplier = 1f;

        if (basicAttackProfile.SupportsComboCounter && comboCounter > 0)
            comboMultiplier += comboCounter * basicAttackProfile.ComboDamageBonusPerStack;

        return Mathf.Max(
            1,
            Mathf.RoundToInt(baseDamage * basicAttackProfile.BasicDamageMultiplier * comboMultiplier));
    }

    public bool RegisterSuccessfulBasicHit()
    {
        if (!basicAttackProfile.SupportsComboCounter || basicAttackProfile.MaxComboCounter <= 0)
            return false;

        int previousComboCounter = comboCounter;
        comboCounter = Mathf.Clamp(comboCounter + 1, 0, basicAttackProfile.MaxComboCounter);
        comboCounterTimer = basicAttackProfile.ComboResetDelay;
        return comboCounter != previousComboCounter;
    }

    public bool ResetComboCounter()
    {
        if (comboCounter <= 0)
            return false;

        comboCounter = 0;
        comboCounterTimer = 0f;
        return true;
    }

    private int ResolveNextAnimationIndex(int previousAnimationIndex)
    {
        int variantCount = Mathf.Max(1, basicAttackProfile.AnimationVariantCount);

        if (variantCount == 1)
            return 1;

        if (basicAttackProfile.SelectionMode == PlayerBasicAttackSelectionMode.Sequential)
        {
            int nextSequentialIndex = previousAnimationIndex + 1;
            if (nextSequentialIndex <= 0 || nextSequentialIndex > variantCount)
                nextSequentialIndex = 1;

            return nextSequentialIndex;
        }

        int nextRandomIndex = Random.Range(1, variantCount + 1);

        if (variantCount > 1 && nextRandomIndex == previousAnimationIndex)
            nextRandomIndex = (nextRandomIndex % variantCount) + 1;

        return nextRandomIndex;
    }

    private void TickComboCounter(float deltaTime)
    {
        if (!basicAttackProfile.SupportsComboCounter || comboCounter <= 0)
            return;

        comboCounterTimer -= deltaTime;
        if (comboCounterTimer <= 0f)
        {
            comboCounter = 0;
            comboCounterTimer = 0f;
        }
    }
}

