using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SpellLoader
{
    public static List<Spell> LoadSpells(string path)
    {
        string json = File.ReadAllText(path);
        if (json == null)
        {
            Debug.LogError($"Spell JSON not found at {path}");
            return new List<Spell>();
        }

        List<SpellData> spellDataList = JsonUtility.FromJson<SpellDataList>($"{{\"spells\": {json}}}").spells;
        List<Spell> spells = new List<Spell>();

        foreach (SpellData data in spellDataList)
        {
            Sprite graphic = Resources.Load<Sprite>(data.graphicPath);
            if (graphic == null)
            {
                Debug.LogWarning($"Sprite not found at {data.graphicPath} for spell {data.name}. Using a placeholder.");
                graphic = Resources.Load<Sprite>("Spells/NoTexture"); // Default graphic.
            }

            if (data.targetingMode == "Enemy" || data.targetingMode == "Ally")
            {
                spells.Add(new AttackSpell(
                    data.name,
                    data.power,
                    data.mpCost,
                    data.cooldown,
                    (TargetingMode)System.Enum.Parse(typeof(TargetingMode), data.targetingMode),
                    (Element)System.Enum.Parse(typeof(Element), data.element),
                    graphic
                ));
            }
            else
            {
                Debug.LogWarning($"Unsupported targeting mode: {data.targetingMode} for spell {data.name}.");
            }
        }

        return spells;
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
