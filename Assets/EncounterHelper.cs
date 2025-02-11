using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

public static class EncounterHelper
{
    public static string GetNameFromType(EncounterType encounterType)
    {
        var type = encounterType.GetType();
        var memberInfo = type.GetMember(encounterType.ToString());
        var attributes = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
        return attributes.Length > 0 ? ((DescriptionAttribute)attributes[0]).Description : encounterType.ToString();
    }

    public static Sprite GetSpriteFromType(EncounterType encounterType, Dictionary<EncounterType, Sprite> sprites)
    {
        Sprite sprite = null;
        bool success = sprites.TryGetValue(encounterType, out sprite);
        if (!success)
        {
            Debug.LogError($"Sprite for {encounterType} is missing.");
        }
        return sprite;
    }

    private static readonly Dictionary<EncounterType, float> EncounterChances = new Dictionary<EncounterType, float>
    {
        { EncounterType.BATTLE, 0.5f },
        { EncounterType.HARDBATTLE, 0.2f },
        { EncounterType.SKILL, 0.15f },
        { EncounterType.REST, 0.15f }
    };

    public static EncounterType GetRandomEncounterType()
    {
        float totalWeight = EncounterChances.Values.Sum();
        float randomValue = UnityEngine.Random.Range(0f, totalWeight);
        float cumulativeWeight = 0f;

        foreach (var pair in EncounterChances)
        {
            cumulativeWeight += pair.Value;
            if (randomValue <= cumulativeWeight)
            {
                return pair.Key;
            }
        }

        // default if it somehow fails
        return EncounterType.BATTLE;
    }
}

public enum EncounterType
{
    [Description("Battle")]
    BATTLE,
    [Description("Hard Battle")]
    HARDBATTLE,
    [Description("Learn Skill")]
    SKILL,
    [Description("Rest Stop")]
    REST,
    [Description("Boss Fight")]
    BOSS
}
