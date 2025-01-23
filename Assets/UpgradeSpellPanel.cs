using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeSpellPanel : MonoBehaviour
{
    private Dictionary<string, Spell> spellDefsCache;
    private SaveFileData saveData;
    private GameObject UpgradeSpellPanelUnity;
    private bool IsActive = true;
    private int ShownUnit = 0;
    private Unit initializedUnit;
    private const int MaxDisplayedSpells = 12;
    private Color LightBlue = new Color(0f, 0.7f, 1f);

    private void Start()
    {
        spellDefsCache = FileOperationsManager.Instance.LoadSpellDefs();
        saveData = FileOperationsManager.Instance.LoadSaveData();
        UpgradeSpellPanelUnity = GameObject.Find("UpgradeSpellPanel");
        InitializeUpgradeSpellPanel();
        ShowOrHide();
    }

    public void ShowOrHide()
    {
        if (UpgradeSpellPanelUnity != null)
        {
            IsActive = !IsActive;
            UpgradeSpellPanelUnity.SetActive(IsActive);
            if (IsActive)
            {
                ShowForUnit();
            }
        }
    }

    public void ShowNext()
    {
        ShownUnit++;
        ShownUnit %= saveData.playerUnits.Length;
        ShowForUnit();
    }

    public void ShowPrevious()
    {
        ShownUnit--;
        if (ShownUnit < 0)
        {
            ShownUnit += saveData.playerUnits.Length;
        }
        ShowForUnit();
    }

    private void InitializeUpgradeSpellPanel()
    {
        if (spellDefsCache == null || spellDefsCache.Count == 0)
        {
            Debug.LogWarning("Spell definitions are empty or not loaded.");
            return;
        }

        var spellsToDisplay = spellDefsCache.Values
            .Take(MaxDisplayedSpells)
            .ToList();

        int spellIndex = 0;

        for (int rowIndex = 0; rowIndex < 4; rowIndex++)
        {
            string rowName = rowIndex == 0 ? "UpgradeSpell_Row" : $"UpgradeSpell_Row ({rowIndex})";
            GameObject rowObject = GameObject.Find(rowName);

            if (rowObject == null)
            {
                Debug.LogError($"UpgradeSpell_Row '{rowName}' not found!");
                continue;
            }

            for (int slotIndex = 0; slotIndex < 3; slotIndex++)
            {
                if (spellIndex >= spellsToDisplay.Count) break;

                string slotName = slotIndex == 0 ? "SpellSlot" : $"SpellSlot ({slotIndex})";
                GameObject slotObject = rowObject.transform.Find(slotName)?.gameObject;

                if (slotObject == null)
                {
                    Debug.LogError($"SpellSlot '{slotName}' not found in {rowObject.name}!");
                    continue;
                }

                Spell spell = spellsToDisplay[spellIndex];
                var button = slotObject.GetComponentInChildren<Button>();

                if (button != null)
                {
                    SpellButton spellButton = button.GetComponentInChildren<SpellButton>();
                    spellButton.Initialize(spell);

                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => OnSpellUpgradeClicked(spell));
                }

                spellIndex++;
            }
        }
    }

    public void ShowForUnit()
    {
        GameObject unitGameObject = GameObject.Find("Player");
        initializedUnit = unitGameObject.GetComponent<Unit>();
        initializedUnit.InitializeFromData(saveData.playerUnits[ShownUnit], spellDefsCache);
        initializedUnit.ApplyMask();

        if (initializedUnit == null || initializedUnit.AvailableSpells == null)
        {
            Debug.LogError("Unit or Unit.AvailableSpells is null.");
            return;
        }

        int spellIndex = 0;
        foreach (var spell in spellDefsCache.Values.Take(MaxDisplayedSpells))
        {
            string rowName = spellIndex / 3 == 0 ? "UpgradeSpell_Row" : $"UpgradeSpell_Row ({spellIndex / 3})";
            GameObject rowObject = GameObject.Find(rowName);
            if (rowObject == null)
            {
                Debug.LogError($"rowObject not found for {rowName}.");
                continue;
            }; ;

            string slotName = spellIndex % 3 == 0 ? "SpellSlot" : $"SpellSlot ({spellIndex % 3})";
            GameObject slotObject = rowObject.transform.Find(slotName)?.gameObject;
            if (slotObject == null)
            {
                Debug.LogError($"slotObject not found for {slotName}.");
                continue;
            };

            var button = slotObject.GetComponentInChildren<Button>();
            if (button == null)
            {
                Debug.LogError($"Button not found for {slotObject.name}.");
                continue;
            }

            var frame = button.transform.Find("Frame")?.GetComponent<Image>();
            if (frame == null)
            {
                Debug.LogError($"Frame not found for {slotObject.name}.");
                continue;
            }

            AdjustSpellSlot(spell, button, frame);

            spellIndex++;
        }

        // text
        var statsGO = GameObject.Find("Stats");
        TextMeshProUGUI stats = statsGO.GetComponent<TextMeshProUGUI>();
        if (stats == null)
        {
            Debug.LogError($"Stats textPanel not found.");
            return;
        }

        stats.text = $"<b>Name:</b> {initializedUnit.unitName}\n";
        stats.text += $"<b>Skill points:</b> {initializedUnit.SkillPoints}, {initializedUnit.Exp} / {initializedUnit.ExpToNextLevel} to next skill\n";

        stats.text += $"<b>HP:</b> <color=red>{initializedUnit.CurrHP}</color> / <color=red>{initializedUnit.MaxHP}</color>\n";
        stats.text += $"<b>MP:</b> <color={ColorToHex(LightBlue)}>{initializedUnit.CurrMP}</color> / <color={ColorToHex(LightBlue)}>{initializedUnit.MaxMP}</color>\n";

        stats.text += $"<b>MP Regen:</b> {initializedUnit.MPRegen}\n";

        stats.text += $"<b>Attack:</b> P: <color=red>{initializedUnit.PAtk}</color>  M: <color={ColorToHex(LightBlue)}>{initializedUnit.MAtk}</color>\n";
        stats.text += $"<b>Defense:</b> P: <color=red>{initializedUnit.PDef}</color>  M: <color={ColorToHex(LightBlue)}>{initializedUnit.MDef}</color>\n";

        stats.text += "<b>Resistances:</b>\n";
        stats.text += $"    Fire: {initializedUnit.FireRes}%\n";
        stats.text += $"    Ice: {initializedUnit.IceRes}%\n";
    }

    private void AdjustSpellSlot(Spell currentSpell, Button button, Image frame)
    {
        string thisSpellName = currentSpell.Name;
        string unitSpellName = GetUnitSpellName(thisSpellName);

        if (IsSameOrWorse(thisSpellName, unitSpellName))
        {
            frame.color = LightBlue;
            button.interactable = true;
        }
        else if (IsOneTierBetter(thisSpellName, unitSpellName)) // Can be learned (one tier better)
        {
            if (initializedUnit.SkillPoints > 0)
            {
                frame.color = Color.green;
                button.interactable = true;
            }
            else
            {
                frame.color = Color.yellow;
                button.interactable = false;
            }
        }
        else if (unitSpellName == null
            && thisSpellName.Substring(thisSpellName.Length - 3).Count(c => c == 'I') == 1) // Base tier and not yet learned
        {
            if (initializedUnit.SkillPoints > 0)
            {
                frame.color = Color.green;
                button.interactable = true;
            }
            else
            {
                frame.color = Color.yellow;
                button.interactable = false;
            }
        }
        else // More than one tier better, cannot be learned yet
        {
            frame.color = Color.grey;
            button.interactable = false;
            button.image.color = Color.grey;
        }
    }

    private bool IsOneTierBetter(string betterSpell, string spell)
    {
        if (betterSpell == null || spell == null) return false;
        return spell + "I" == betterSpell;
    }

    private bool IsSameOrWorse(string worseSpell, string spell)
    {
        if (worseSpell == null || spell == null) return false;
        return spell.Contains(worseSpell);
    }

    private string GetUnitSpellName(string spellName)
    {
        string spellNameNoTier = spellName.Substring(0, spellName.Length - 3);
        Spell unitSpell = initializedUnit.AvailableSpells.FirstOrDefault(spell => spell.Name.Contains(spellNameNoTier));
        string unitSpellName = unitSpell?.Name;
        return unitSpellName;
    }

    private void OnSpellUpgradeClicked(Spell targetSpell)
    {
        string unitSpellName = GetUnitSpellName(targetSpell.Name);
        if (unitSpellName != null && IsSameOrWorse(targetSpell.Name, unitSpellName)) return;

        UnitData unitData = saveData.playerUnits[ShownUnit];
        unitData.SkillPoints -= 1;
        if (unitSpellName != null)
        {
            int index = unitData.SpellNames.IndexOf(unitSpellName);
            unitData.SpellNames[index] = targetSpell.Name;
        }
        else
        {
            unitData.SpellNames.Add(targetSpell.Name);
        }
        ShowForUnit();
    }

    private string ColorToHex(Color color)
    {
        return $"#{ColorUtility.ToHtmlStringRGB(color)}";
    }

}
