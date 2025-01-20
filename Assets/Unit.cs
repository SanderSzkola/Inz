using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Unit : MonoBehaviour
{
    public string unitName;
    public bool canAct = true;
    public bool isPlayerUnit = false;

    public int maxHP;
    public int currHP;
    public int maxMP;
    public int currMP;
    public int mpRegen;
    public int pAtk;
    public int pDef;
    public int mAtk;
    public int mDef;

    public List<Spell> AvailableSpells = new List<Spell>();

    public int fireRes;
    public int iceRes;

    public GameObject statusPanel;
    private GameObject selectionIndicator;

    private Slider hpSlider;
    private Slider mpSlider;
    private CombatManager combatManager;

    private SpriteRenderer maskSpriteRenderer;
    private float maskRed = 1f;
    private float maskGreen = 1f;
    private float maskBlue = 1f;

    public float ColliderOffsetX;
    public float ColliderOffsetY;
    public float ColliderSizeX;
    public float ColliderSizeY;

    public float SpritePosX;
    public float SpritePosY;
    public float SpritePosZ;
    public float SpriteWidth;
    public float SpriteHeight;

    public void InitializeFromPrefab(CombatManager manager, List<Spell> globalSpellList, GameObject selectionIndicator, UnitData data)
    {
        combatManager = manager;
        InitializeFromData(data, globalSpellList);

        this.selectionIndicator = selectionIndicator;
        this.selectionIndicator.SetActive(false);
        RefreshSelectorColor();

        // only for player unit
        Transform maskTransform = transform.Find("mage/mask");
        if (maskTransform != null)
        {
            maskSpriteRenderer = maskTransform.GetComponent<SpriteRenderer>();
            if (maskSpriteRenderer != null)
            {
                ApplyMask();
            }
        }
    }

    public void InitializeFromData(UnitData data, List<Spell> globalSpellList)
    {
        unitName = data.Name;
        isPlayerUnit = data.IsPlayerUnit;

        maxHP = data.MaxHP;
        currHP = data.CurrHP;
        maxMP = data.MaxMP;
        currMP = data.CurrMP;
        mpRegen = data.MPRegen;
        pAtk = data.PAtk;
        pDef = data.PDef;
        mAtk = data.MAtk;
        mDef = data.MDef;

        fireRes = data.FireRes;
        iceRes = data.IceRes;

        maskRed = data.maskRed;
        maskGreen = data.maskGreen;
        maskBlue = data.maskBlue;

        ColliderOffsetX = data.ColliderOffsetX;
        ColliderOffsetY = data.ColliderOffsetY;
        ColliderSizeX = data.ColliderSizeX;
        ColliderSizeY = data.ColliderSizeY;
        SpritePosX = data.SpritePosX;
        SpritePosY = data.SpritePosY;
        SpritePosZ = data.SpritePosZ;
        SpriteWidth = data.SpriteWidth;
        SpriteHeight = data.SpriteHeight;

        AvailableSpells.Clear();
        if (data.SpellNames == null)
        {
            Debug.LogError($"Unit '{unitName}' has no spells.");
            return;
        }
        string[] spellNames = data.SpellNames.Split(',');
        foreach (string spellName in spellNames)
        {
            string trimmedName = spellName.Trim();
            Spell matchingSpell = globalSpellList.FirstOrDefault(spell => spell.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase));
            if (matchingSpell != null)
            {
                AvailableSpells.Add(matchingSpell.Clone());
            }
            else
            {
                Debug.LogWarning($"Spell '{trimmedName}' not found in global spell list for unit {data.Name}.");
            }
        }
    }

    private void ApplyMask()
    {
        // Apply mask color based on the Unit parameters
        if (maskRed == 1f && maskGreen == 1f && maskBlue == 1f) // default detected, randomize
        {
            maskRed = UnityEngine.Random.Range(0f, 1f);
            maskGreen = UnityEngine.Random.Range(0f, 1f);
            maskBlue = UnityEngine.Random.Range(0f, 1f);
        }
        Color maskColor = new Color(maskRed, maskGreen, maskBlue, 1f);
        maskSpriteRenderer.color = maskColor;
    }

    public void RefreshSelectorColor()
    {
        Image selectorSpriteRenderer = selectionIndicator.GetComponentInChildren<Image>();
        Color colorGreen = new Color(0.4f, 1f, 0.4f);
        Color colorRed = new Color(1f, 0.4f, 0.4f);
        Color colorYellow = new Color(1f, 1f, 0.2f);
        if (isPlayerUnit)
        {
            if (canAct)
            {
                selectorSpriteRenderer.color = colorGreen;
            }
            else
            {
                selectorSpriteRenderer.color = colorYellow;
            }
        }
        else
        {
            selectorSpriteRenderer.color = colorRed;
        }
    }

    public void UpdateStatusPanel()
    {
        if (!hpSlider)
        {
            hpSlider = statusPanel.transform.Find("HPBar").GetComponent<Slider>();
            mpSlider = statusPanel.transform.Find("MPBar").GetComponent<Slider>();
        }
        hpSlider.value = (float)currHP / maxHP;
        mpSlider.value = (float)currMP / maxMP;
    }

    public void ChangeHPBy(int value)
    {
        currHP += value;
        currHP = Mathf.Clamp(currHP, 0, maxHP); // Prevent overflow or underflow.
        UpdateStatusPanel();
    }

    public void ChangeMPBy(int value)
    {
        currMP += value;
        currMP = Mathf.Clamp(currMP, 0, maxMP);
        UpdateStatusPanel();
    }

    public int GetResistance(Element element) // where is my dict :(
    {
        if (element == Element.Fire) return fireRes;
        if (element == Element.Ice) return iceRes;
        return 0;
    }

    public bool CanAffordCast(int value)
    {
        return currMP >= value;
    }

    public void ApplyDamage(int damage)
    {
        ChangeHPBy(-damage);
        StartCoroutine(TakeDamageAnim());
    }

    private void Die()
    {
        combatManager.RemoveUnit(this);
        Destroy(selectionIndicator);
        Destroy(statusPanel);
        Destroy(gameObject);
    }

    void OnMouseDown()
    {
        combatManager.SelectUnit(this);
    }

    public void RefreshSelection(Unit activePlayerUnit, Unit targetUnit)
    {
        if (isPlayerUnit)
        {
            selectionIndicator.SetActive(activePlayerUnit != null && activePlayerUnit == this);
        }
        else
        {
            selectionIndicator.SetActive(targetUnit != null && targetUnit == this);
        }
        RefreshSelectorColor();
    }

    public void ProcessNextTurn()
    {
        canAct = true;
        foreach (Spell spell in AvailableSpells)
        {
            spell.ReduceCooldown();
        }
        ChangeMPBy(mpRegen);
    }

    public IEnumerator TakeDamageAnim(float duration = 0.3f, float intensity = 1f)
    {
        // Animation
        float elapsedTime = 0f;
        Vector3 originalPosition = transform.position;

        while (elapsedTime < duration)
        {
            float offsetX = Mathf.Sin(Time.time * 50f) * intensity * (duration - elapsedTime) / duration;
            transform.position = originalPosition + new Vector3(offsetX, 0, 0);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPosition;

        // Logic that needs to happen AFTER animation or it errors
        if (currHP <= 0)
        {
            canAct = false;
            Die();
        }
    }

    public IEnumerator TakeActionAnim()
    {
        int direction = isPlayerUnit ? 1 : -1;
        float moveSpeed = 15f;
        Vector3 startPosition = transform.position;
        Vector3 actionPosition = transform.position + new Vector3(1 * direction, 0, 0);

        // Move to action position
        while (Vector3.Distance(transform.position, actionPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, actionPosition, moveSpeed * Time.deltaTime);
            yield return null;
        }

        // Move back to start position
        while (Vector3.Distance(transform.position, startPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, startPosition, moveSpeed * 0.5f * Time.deltaTime);
            yield return null;
        }
    }

}
