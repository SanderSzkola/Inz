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

        int spellIndex = 0;

        // Loop through UpgradeSpell_Row objects
        for (int rowIndex = 0; rowIndex < 4; rowIndex++)
        {
            // Get reference to UpgradeSpell_Row
            string rowName = rowIndex == 0 ? "UpgradeSpell_Row" : $"UpgradeSpell_Row ({rowIndex})";
            GameObject rowObject = GameObject.Find(rowName);
            if (rowObject == null)
            {
                Debug.LogError($"UpgradeSpell_Row '{rowName}' not found!");
                continue;
            }

            // Loop through SpellSlot objects in this row
            for (int slotIndex = 0; slotIndex < 3; slotIndex++)
            {
                // Ensure we don't exceed the spell list
                if (spellIndex >= spellDefsCache.Count) break;

                // Get reference to SpellSlot
                string slotName = slotIndex == 0 ? "SpellSlot" : $"SpellSlot ({slotIndex})";
                GameObject slotObject = rowObject.transform.Find(slotName)?.gameObject;
                if (slotObject == null)
                {
                    Debug.LogError($"SpellSlot '{slotName}' not found in {rowObject.name}!");
                    continue;
                }

                // Set button properties for the SpellSlot
                var button = slotObject.GetComponentInChildren<Button>();
                var spell = spellDefsCache[spellIndex];

                if (button != null)
                {
                    // Set button image and onClick behavior
                    var image = button.GetComponent<Image>();
                    if (image != null) image.sprite = spell.Graphic;

                    button.onClick.AddListener(() => OnSpellUpgradeClicked(spell));
                }

                spellIndex++;
            }
        }
    }

    public void ShowForUnit()
    {
        // unit
        GameObject unitGameObject = GameObject.Find("Player");
        initializedUnit = unitGameObject.GetComponent<Unit>();
        initializedUnit.InitializeFromData(saveData.playerUnits[ShownUnit], spellDefsCache);
        initializedUnit.ApplyMask();

        // spell
        if (initializedUnit == null || initializedUnit.AvailableSpells == null)
        {
            Debug.LogError("Unit or Unit.AvailableSpells is null.");
            return;
        }

        int spellIndex = 0;

        for (int rowIndex = 0; rowIndex < 4; rowIndex++)
        {
            string rowName = rowIndex == 0 ? "UpgradeSpell_Row" : $"UpgradeSpell_Row ({rowIndex})";
            GameObject rowObject = GameObject.Find(rowName);
            if (rowObject == null) continue;

            for (int slotIndex = 0; slotIndex < 3; slotIndex++)
            {
                if (spellIndex >= spellDefsCache.Count) break;

                string slotName = slotIndex == 0 ? "SpellSlot" : $"SpellSlot ({slotIndex})";
                GameObject slotObject = rowObject.transform.Find(slotName)?.gameObject;
                if (slotObject == null) continue;

                Spell currentSpell = spellDefsCache[spellIndex];

                var button = slotObject.GetComponentInChildren<Button>();
                if (button == null)
                {
                    Debug.LogError($"Button not found for {slotObject.name}.");
                    continue;
                }

                var frame = button.transform.Find("Frame");
                if (frame == null)
                {
                    Debug.LogError($"Frame not found for {slotObject.name}.");
                    continue;
                }

                var frameImage = frame.GetComponent<Image>();
                if (frameImage == null)
                {
                    Debug.LogError($"FrameImage not found for {slotObject.name}.");
                    continue;
                }

                AdjustSpellSlot(initializedUnit, currentSpell, button, frameImage);

                spellIndex++;
            }
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
        stats.text += $"<b>MP:</b> <color=blue>{initializedUnit.CurrMP}</color> / <color=blue>{initializedUnit.MaxMP}</color>\n";

        stats.text += $"<b>MP Regen:</b> {initializedUnit.MPRegen}\n";

        stats.text += $"<b>Attack:</b> P: <color=red>{initializedUnit.PAtk}</color>  M: <color=blue>{initializedUnit.MAtk}</color>\n";
        stats.text += $"<b>Defense:</b> P: <color=red>{initializedUnit.PDef}</color>  M: <color=blue>{initializedUnit.MDef}</color>\n";

        stats.text += "<b>Resistances:</b>\n";
        stats.text += $"    Fire: {initializedUnit.FireRes}%\n";
        stats.text += $"    Ice: {initializedUnit.IceRes}%\n";
    }

    private void AdjustSpellSlot(Unit unit, Spell currentSpell, Button button, Image frame)
    {
        Spell worseSpell = null;
        Spell betterSpell = null;

        foreach (Spell availableSpell in unit.AvailableSpells)
        {
            if (IsEqualOrWorse(availableSpell, currentSpell))
            {
                worseSpell = availableSpell;
            }
            else if (IsOneTierBetter(currentSpell, availableSpell))
            {
                betterSpell = availableSpell;
            }
        }

        if (worseSpell != null) // Equal or worse, already learned
        {
            frame.color = Color.blue;
            button.interactable = false;
        }
        else if (betterSpell != null) // One tier better, can be larned
        {
            if (unit.SkillPoints > 0)
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
        else if (!currentSpell.Name.EndsWith("I")) // Not on list, base tier, can be learned
        {
            if (unit.SkillPoints > 0)
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
        else // More than one tier better, cant be learned yet
        {
            frame.color = Color.grey;
            button.interactable = false;
        }
    }

    private bool IsEqualOrWorse(Spell existingSpell, Spell targetSpell)
    {
        if (existingSpell.Name == targetSpell.Name) return true;

        if (existingSpell.Name.StartsWith(targetSpell.Name) &&
            existingSpell.Name.EndsWith("I") &&
            targetSpell.Name.EndsWith("II")) return true;

        return false;
    }

    private bool IsOneTierBetter(Spell betterSpell, Spell worseSpell)
    {
        if (betterSpell.Name == worseSpell.Name + " II") return true;
        if (betterSpell.Name == worseSpell.Name + " III" && !betterSpell.Name.EndsWith("III")) return true;
        return false;
    }

    private Spell GetLowerTierSpell(Spell spell)
    {

    }

    private void OnSpellUpgradeClicked(Spell targetSpell)
    {

        if (initializedUnit.AvailableSpells.Contains(targetSpell))
        {
        }
    }

}
