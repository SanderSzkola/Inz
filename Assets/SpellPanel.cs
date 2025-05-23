using System.Collections.Generic;
using UnityEngine;

public class SpellPanel : MonoBehaviour
{
    public GameObject spellSlotPrefab;
    public int maxSpellSlots = 5;
    private List<GameObject> spellSlots = new List<GameObject>();
    private float scale = 0.8f;


    public void LoadSpells(Unit unit, CombatManager combatManager)
    {
        // Clear previous slots.
        foreach (GameObject slot in spellSlots)
        {
            Destroy(slot);
        }
        spellSlots.Clear();

        // Get panel dimensions and calculate slot size and spacing.
        RectTransform panelRect = GetComponent<RectTransform>();
        float panelWidth = panelRect.rect.width;
        float panelHeight = panelRect.rect.height;
        float slotSize = panelHeight * scale;
        float slotSpacing = (panelWidth - (maxSpellSlots * slotSize)) / (maxSpellSlots + 1);

        if (slotSpacing < 0)
        {
            Debug.LogWarning("Too tight, something went wrong");
            slotSpacing = 0;
        }

        float startX = -panelWidth / 2 + slotSpacing + slotSize / 2;

        for (int i = 0; i < maxSpellSlots; i++)
        {
            GameObject slot = Instantiate(spellSlotPrefab, transform);
            spellSlots.Add(slot);

            RectTransform slotRect = slot.GetComponent<RectTransform>();
            if (slotRect == null)
            {
                Debug.LogError("SpellSlotPrefab missing RectTransform");
                continue;
            }
            // Set slot size and position.
            slotRect.sizeDelta = new Vector2(slotSize, slotSize);
            slotRect.pivot = new Vector2(0.5f, 0.5f);
            slotRect.anchoredPosition = new Vector2(startX + i * (slotSize + slotSpacing), 0); // Set position relative to the parent RectTransform.

            SpellButton spellButton = slot.GetComponentInChildren<SpellButton>();
            if (i < unit.AvailableSpells.Count)
            {
                spellButton.Initialize(unit.AvailableSpells[i], combatManager, unit.canAct);
            }
            else
            {
                spellButton.DisableButton();
            }
        }
    }

    public void RefreshSpellSelection(SpellButton selected, bool canAct)
    {
        foreach (GameObject slot in spellSlots)
        {
            SpellButton spellButton = slot.transform.GetComponentInChildren<SpellButton>();
            if (spellButton != null)
            {
                spellButton.Refresh(selected == spellButton, canAct);
            }
        }
    }
}
