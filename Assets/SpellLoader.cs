using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SpellLoader
{
    public static List<Spell> LoadSpells(string path)
    {
        string json = File.ReadAllText(path);
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError($"Spell JSON not found or empty at {path}");
            return new List<Spell>();
        }

        List<SpellData> spellDataList = JsonUtility.FromJson<SpellDataList>(json).spells;
        List<Spell> spells = new List<Spell>();

        foreach (SpellData data in spellDataList)
        {
            Sprite graphic = Resources.Load<Sprite>(data.graphicPath);
            if (graphic == null)
            {
                Debug.LogWarning($"Sprite not found at {data.graphicPath} for spell {data.name}. Using a placeholder.");
                graphic = Resources.Load<Sprite>("Spells/NoTexture");
            }

            Spell spell = CreateSpell(data, graphic);
            if (spell != null)
            {
                spells.Add(spell);
            }
        }

        return spells;
    }
    private static Spell CreateSpell(SpellData data, Sprite graphic)
    {
        TargetingMode targetingMode = (TargetingMode)System.Enum.Parse(typeof(TargetingMode), data.targetingMode);
        Element element = (Element)System.Enum.Parse(typeof(Element), data.element);

        switch (targetingMode)
        {
            case TargetingMode.Enemy:
                return new AttackSpell(data.name, data.power, data.mpCost, data.cooldown, targetingMode, element, graphic);

            case TargetingMode.Self:
                return new RestoreSpell(data.name, data.power, data.mpCost, data.cooldown, targetingMode, element, graphic);

            // TODO: more spell types would go here

            default:
                Debug.LogWarning($"Unknown targeting mode {data.targetingMode} for spell {data.name}. Skipping.");
                return null;
        }
    }
}




[System.Serializable]
public class SpellData
{
    public string name;
    public int power;
    public int mpCost;
    public int cooldown;
    public string targetingMode;
    public string element;
    public string graphicPath;
}

[System.Serializable]
public class SpellDataList
{
    public List<SpellData> spells;
}
