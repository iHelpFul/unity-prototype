using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal enum CombatTestActionMode
{
    Skill,
    BasicAttack
}

internal sealed class CombatTestResult
{
    public int HitCount;
    public int ProjectileCount;
    public int MaxTargets;
    public float DamageCoefficient;
    public int RawMin;
    public int RawMax;
    public float ElementMultiplier;
    public int MitigatedMinPerHit;
    public int MitigatedMaxPerHit;
    public int TotalMin;
    public int TotalMax;
    public int EnemyHp;
    public int EnemyDefense;
    public float BreakPower;
    public int EstimatedCastsToKill;
}

public class CombatTestRunnerWindow : EditorWindow
{
    private CombatTestActionMode actionMode = CombatTestActionMode.Skill;
    private UnityEngine.Object skillDefinition;
    private UnityEngine.Object basicAttackProfile;
    private UnityEngine.Object enemyDefinition;
    private UnityEngine.Object elementRuleProfile;
    private int skillLevel = 1;
    private int playerLevel = 1;
    private int baseMinDamage = 8;
    private int baseMaxDamage = 12;
    private int hitRate = 100;
    private float surgeChance;
    private float surgePower = 1.5f;
    private int sourceElementIndex;
    private float elementPower = 1f;
    private bool includeSurgeAverage;
    private Vector2 scroll;

    [MenuItem("Tools/Combat/Test Runner")]
    public static void Open()
    {
        GetWindow<CombatTestRunnerWindow>("Combat Test Runner");
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Combat Test Runner", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Dry combat preview for fast tuning. This is not a full runtime simulation, but it is useful for checking damage scale, defense, elements, hit count, target count, and break power.",
            MessageType.Info);

        DrawInputs();
        DrawResult();

        EditorGUILayout.EndScrollView();
    }

    private void DrawInputs()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Action", EditorStyles.boldLabel);
            actionMode = (CombatTestActionMode)EditorGUILayout.EnumPopup("Mode", actionMode);

            if (actionMode == CombatTestActionMode.Skill)
            {
                skillDefinition = EditorGUILayout.ObjectField("Skill", skillDefinition, ResolveObjectFieldType("PlayerSkillDefinition"), false);
                skillLevel = Mathf.Max(1, EditorGUILayout.IntField("Skill Level", skillLevel));
            }
            else
            {
                basicAttackProfile = EditorGUILayout.ObjectField("Basic Attack", basicAttackProfile, ResolveObjectFieldType("PlayerBasicAttackProfile"), false);
            }

            enemyDefinition = EditorGUILayout.ObjectField("Enemy", enemyDefinition, ResolveObjectFieldType("EnemyDefinition"), false);
            elementRuleProfile = EditorGUILayout.ObjectField("Element Rules", elementRuleProfile, ResolveObjectFieldType("CombatElementRuleProfile"), false);
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Player Test Stats", EditorStyles.boldLabel);
            playerLevel = Mathf.Max(1, EditorGUILayout.IntField("Level", playerLevel));
            baseMinDamage = Mathf.Max(1, EditorGUILayout.IntField("Base Min Damage", baseMinDamage));
            baseMaxDamage = Mathf.Max(baseMinDamage, EditorGUILayout.IntField("Base Max Damage", baseMaxDamage));
            hitRate = Mathf.Max(0, EditorGUILayout.IntField("Hit Rate", hitRate));
            surgeChance = Mathf.Clamp(EditorGUILayout.FloatField("Surge Chance %", surgeChance), 0f, 100f);
            surgePower = Mathf.Max(1f, EditorGUILayout.FloatField("Surge Power", surgePower));
            includeSurgeAverage = EditorGUILayout.Toggle("Include Surge Average", includeSurgeAverage);
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Element Override", EditorStyles.boldLabel);
            sourceElementIndex = EditorGUILayout.Popup("Attack Element", sourceElementIndex, GetElementNames());
            elementPower = Mathf.Max(0f, EditorGUILayout.FloatField("Element Power", elementPower));
        }
    }

    private void DrawResult()
    {
        UnityEngine.Object actionAsset = actionMode == CombatTestActionMode.Skill
            ? skillDefinition
            : basicAttackProfile;

        if (actionAsset == null || enemyDefinition == null)
        {
            EditorGUILayout.HelpBox("Pick an action and enemy to calculate a preview.", MessageType.Info);
            return;
        }

        CombatTestResult result = CalculateResult(actionAsset, enemyDefinition);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Preview Result", EditorStyles.boldLabel);
            DrawMetric("Raw damage range", $"{result.RawMin} - {result.RawMax}");
            DrawMetric("Enemy HP / DEF", $"{result.EnemyHp} / {result.EnemyDefense}");
            DrawMetric("Element multiplier", result.ElementMultiplier.ToString("0.###"));
            DrawMetric("Mitigated per hit", $"{result.MitigatedMinPerHit} - {result.MitigatedMaxPerHit}");
            DrawMetric("Hits x Projectiles", $"{result.HitCount} x {result.ProjectileCount}");
            DrawMetric("Max targets", result.MaxTargets.ToString());
            DrawMetric("Total per cast", $"{result.TotalMin} - {result.TotalMax}");
            DrawMetric("Break power", result.BreakPower.ToString("0.##"));
            DrawMetric("Estimated casts to kill", result.EstimatedCastsToKill <= 0 ? "N/A" : result.EstimatedCastsToKill.ToString());
        }

        DrawActionDiagnostics(actionAsset);
    }

    private CombatTestResult CalculateResult(UnityEngine.Object actionAsset, UnityEngine.Object enemy)
    {
        SerializedObject actionObject = new SerializedObject(actionAsset);
        SerializedObject enemyObject = new SerializedObject(enemy);

        float damageCoefficient = ResolveDamageCoefficient(actionObject);
        int hitCount = ResolveHitCount(actionObject);
        int projectileCount = ResolveProjectileCount(actionObject);
        int maxTargets = Mathf.Max(1, GetInt(actionObject, "maxTargets", 1));
        float breakPower = GetFloat(actionObject, "baseBreakPower", 0f);
        int enemyHp = Mathf.Max(1, GetInt(enemyObject, "maxHP", 1));
        int enemyDefense = Mathf.Max(0, GetInt(enemyObject, "defense", 0));
        int targetElement = GetEnumIndex(enemyObject, "elementType", 0);
        float elementMultiplier = ResolveElementMultiplier(sourceElementIndex, targetElement) * Mathf.Max(0f, elementPower);

        if (Mathf.Approximately(elementMultiplier, 0f))
            elementMultiplier = 1f;

        float surgeAverageMultiplier = includeSurgeAverage
            ? Mathf.Lerp(1f, Mathf.Max(1f, surgePower), Mathf.Clamp01(surgeChance / 100f))
            : 1f;

        int rawMin = Mathf.Max(1, Mathf.RoundToInt(baseMinDamage * damageCoefficient * surgeAverageMultiplier));
        int rawMax = Mathf.Max(rawMin, Mathf.RoundToInt(baseMaxDamage * damageCoefficient * surgeAverageMultiplier));
        int elementalMin = Mathf.Max(1, Mathf.RoundToInt(rawMin * elementMultiplier));
        int elementalMax = Mathf.Max(elementalMin, Mathf.RoundToInt(rawMax * elementMultiplier));
        int mitigatedMin = Mathf.Max(1, elementalMin - enemyDefense);
        int mitigatedMax = Mathf.Max(mitigatedMin, elementalMax - enemyDefense);
        int packets = Mathf.Max(1, hitCount) * Mathf.Max(1, projectileCount);
        int totalMin = mitigatedMin * packets;
        int totalMax = mitigatedMax * packets;
        int castsToKill = totalMax > 0 ? Mathf.CeilToInt(enemyHp / (float)totalMax) : 0;

        return new CombatTestResult
        {
            HitCount = hitCount,
            ProjectileCount = projectileCount,
            MaxTargets = maxTargets,
            DamageCoefficient = damageCoefficient,
            RawMin = rawMin,
            RawMax = rawMax,
            ElementMultiplier = elementMultiplier,
            MitigatedMinPerHit = mitigatedMin,
            MitigatedMaxPerHit = mitigatedMax,
            TotalMin = totalMin,
            TotalMax = totalMax,
            EnemyHp = enemyHp,
            EnemyDefense = enemyDefense,
            BreakPower = breakPower,
            EstimatedCastsToKill = castsToKill
        };
    }

    private float ResolveDamageCoefficient(SerializedObject actionObject)
    {
        if (actionMode == CombatTestActionMode.BasicAttack)
        {
            float coefficient = GetFloat(actionObject, "damageCoefficient", 1f);
            float multiplier = GetFloat(actionObject, "basicDamageMultiplier", 1f);
            return Mathf.Max(0.01f, coefficient * multiplier);
        }

        float skillMultiplier = GetFloat(actionObject, "damageMultiplier", 1f);
        float levelCoefficient = ResolveSkillLevelDamageCoefficient(actionObject, skillLevel);
        return Mathf.Max(0.01f, skillMultiplier * levelCoefficient);
    }

    private int ResolveHitCount(SerializedObject actionObject)
    {
        if (actionMode == CombatTestActionMode.BasicAttack)
            return Mathf.Max(1, GetInt(actionObject, "hitCount", 1));

        return Mathf.Max(1, ResolveSkillLevelInt(actionObject, skillLevel, "hitCount", GetInt(actionObject, "hitCount", 1)));
    }

    private int ResolveProjectileCount(SerializedObject actionObject)
    {
        if (actionMode == CombatTestActionMode.BasicAttack)
            return 1;

        return Mathf.Max(1, GetInt(actionObject, "projectileCount", 1));
    }

    private float ResolveSkillLevelDamageCoefficient(SerializedObject actionObject, int requestedLevel)
    {
        SerializedProperty levels = actionObject.FindProperty("levels");
        if (levels == null || !levels.isArray || levels.arraySize == 0)
            return 1f;

        SerializedProperty best = null;
        int bestLevel = 0;
        for (int index = 0; index < levels.arraySize; index++)
        {
            SerializedProperty element = levels.GetArrayElementAtIndex(index);
            int level = GetRelativeInt(element, "level", 1);
            if (level > requestedLevel || level < bestLevel)
                continue;

            best = element;
            bestLevel = level;
        }

        return best != null ? Mathf.Max(0.01f, GetRelativeFloat(best, "damageCoefficient", 1f)) : 1f;
    }

    private int ResolveSkillLevelInt(SerializedObject actionObject, int requestedLevel, string propertyPath, int fallback)
    {
        SerializedProperty levels = actionObject.FindProperty("levels");
        if (levels == null || !levels.isArray || levels.arraySize == 0)
            return fallback;

        SerializedProperty best = null;
        int bestLevel = 0;
        for (int index = 0; index < levels.arraySize; index++)
        {
            SerializedProperty element = levels.GetArrayElementAtIndex(index);
            int level = GetRelativeInt(element, "level", 1);
            if (level > requestedLevel || level < bestLevel)
                continue;

            best = element;
            bestLevel = level;
        }

        return best != null ? GetRelativeInt(best, propertyPath, fallback) : fallback;
    }

    private float ResolveElementMultiplier(int attackElement, int targetElement)
    {
        if (attackElement <= 0 || targetElement <= 0)
            return 1f;

        if (elementRuleProfile == null)
            return attackElement == targetElement ? 0.75f : 1f;

        SerializedObject serializedObject = new SerializedObject(elementRuleProfile);
        float neutral = Mathf.Max(0f, GetFloat(serializedObject, "neutralMultiplier", 1f));
        float weakness = Mathf.Max(0f, GetFloat(serializedObject, "weaknessMultiplier", 1.25f));
        float resistance = Mathf.Max(0f, GetFloat(serializedObject, "resistanceMultiplier", 0.75f));

        if (attackElement == targetElement)
            return resistance;

        SerializedProperty affinities = serializedObject.FindProperty("affinities");
        if (affinities != null && affinities.isArray)
        {
            for (int index = 0; index < affinities.arraySize; index++)
            {
                SerializedProperty entry = affinities.GetArrayElementAtIndex(index);
                int source = GetRelativeEnumIndex(entry, "sourceElement", 0);
                if (source != attackElement)
                    continue;

                if (GetRelativeEnumIndex(entry, "advantagedAgainst", 0) == targetElement)
                    return weakness;
                if (GetRelativeEnumIndex(entry, "disadvantagedAgainst", 0) == targetElement)
                    return resistance;
            }
        }

        return neutral;
    }

    private void DrawActionDiagnostics(UnityEngine.Object actionAsset)
    {
        SerializedObject serializedObject = new SerializedObject(actionAsset);
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Action Diagnostics", EditorStyles.boldLabel);
            DrawReferenceStatus(serializedObject, "presentationCueSet", "Cue Set");
            DrawReferenceStatus(serializedObject, "burstPresentationCueSet", "Burst Cue Set");
            DrawReferenceStatus(serializedObject, "defaultProjectileProfile", "Projectile Profile");
            DrawReferenceStatus(serializedObject, "utilitySkillProfile", "Utility Profile");
            DrawReferenceStatus(serializedObject, "burstLinkedSkillProfile", "Burst Profile");
        }
    }

    private void DrawReferenceStatus(SerializedObject serializedObject, string propertyPath, string label)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyPath);
        if (property == null)
            return;

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(label, GUILayout.Width(140f));
            EditorGUILayout.ObjectField(property.objectReferenceValue, typeof(UnityEngine.Object), false);
        }
    }

    private static void DrawMetric(string label, string value)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(label, GUILayout.Width(160f));
            EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
        }
    }

    private string[] GetElementNames()
    {
        Type elementType = ResolveEnumType("CombatElementType");
        return elementType != null ? Enum.GetNames(elementType) : new[] { "None" };
    }

    private static int GetInt(SerializedObject serializedObject, string propertyPath, int fallback)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyPath);
        return property != null && property.propertyType == SerializedPropertyType.Integer ? property.intValue : fallback;
    }

    private static float GetFloat(SerializedObject serializedObject, string propertyPath, float fallback)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyPath);
        return property != null && property.propertyType == SerializedPropertyType.Float ? property.floatValue : fallback;
    }

    private static int GetEnumIndex(SerializedObject serializedObject, string propertyPath, int fallback)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyPath);
        return property != null && property.propertyType == SerializedPropertyType.Enum ? property.enumValueIndex : fallback;
    }

    private static int GetRelativeInt(SerializedProperty root, string relativePath, int fallback)
    {
        SerializedProperty property = root.FindPropertyRelative(relativePath);
        return property != null && property.propertyType == SerializedPropertyType.Integer ? property.intValue : fallback;
    }

    private static float GetRelativeFloat(SerializedProperty root, string relativePath, float fallback)
    {
        SerializedProperty property = root.FindPropertyRelative(relativePath);
        return property != null && property.propertyType == SerializedPropertyType.Float ? property.floatValue : fallback;
    }

    private static int GetRelativeEnumIndex(SerializedProperty root, string relativePath, int fallback)
    {
        SerializedProperty property = root.FindPropertyRelative(relativePath);
        return property != null && property.propertyType == SerializedPropertyType.Enum ? property.enumValueIndex : fallback;
    }

    private static Type ResolveScriptableObjectType(string typeName)
    {
        foreach (Type type in TypeCache.GetTypesDerivedFrom<ScriptableObject>())
        {
            if (string.Equals(type.Name, typeName, StringComparison.Ordinal)
                || string.Equals(type.FullName, typeName, StringComparison.Ordinal))
            {
                return type;
            }
        }

        return null;
    }

    private static Type ResolveEnumType(string typeName)
    {
        foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(typeName);
            if (type != null && type.IsEnum)
                return type;
        }

        return null;
    }

    private static Type ResolveObjectFieldType(string typeName)
    {
        Type type = ResolveScriptableObjectType(typeName);
        return type ?? typeof(UnityEngine.Object);
    }
}
