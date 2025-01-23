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

    public int MaxHP;
    public int CurrHP;
    public int MaxMP;
    public int CurrMP;
    public int MPRegen;
    public int PAtk;
    public int PDef;
    public int MAtk;
    public int MDef;

    public int Exp;
    public int ExpToNextLevel;
    public int SkillPoints;

    public List<Spell> AvailableSpells = new List<Spell>();

    public int FireRes;
    public int IceRes;

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

    public void InitializeFromPrefab(CombatManager manager, Dictionary<string, Spell> globalSpellDict, GameObject selectionIndicator, UnitData data)
    {
        combatManager = manager;
        InitializeFromData(data, globalSpellDict);

        this.selectionIndicator = selectionIndicator;
        this.selectionIndicator.SetActive(false);
        RefreshSelectorColor();
        ApplyMask();
    }

    public void InitializeFromData(UnitData data, Dictionary<string, Spell> globalSpellDict)
    {
        unitName = data.Name;
        isPlayerUnit = data.IsPlayerUnit;

        MaxHP = data.MaxHP;
        CurrHP = data.CurrHP;
        MaxMP = data.MaxMP;
        CurrMP = data.CurrMP;
        MPRegen = data.MPRegen;
        PAtk = data.PAtk;
        PDef = data.PDef;
        MAtk = data.MAtk;
        MDef = data.MDef;

        Exp = data.Exp;
        ExpToNextLevel = data.ExpToNextLevel == 0 ? 50 : data.ExpToNextLevel;
        SkillPoints = data.SkillPoints;

        FireRes = data.FireRes;
        IceRes = data.IceRes;

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
        if (data.SpellNames == null || data.SpellNames.Count == 0)
        {
            Debug.LogError($"Unit '{unitName}' has no spells.");
            return;
        }

        foreach (string spellName in data.SpellNames)
        {
            string trimmedName = spellName.Trim();
            if (globalSpellDict.TryGetValue(trimmedName, out Spell matchingSpell))
            {
                AvailableSpells.Add(matchingSpell.Clone());
            }
            else
            {
                Debug.LogWarning($"Spell '{trimmedName}' not found in global spell dictionary for unit {data.Name}.");
            }
        }
    }


    public void ApplyMask()
    {
        // only for player unit
        Transform maskTransform = transform.Find("mage/mask");
        if (maskTransform != null)
        {
            maskSpriteRenderer = maskTransform.GetComponent<SpriteRenderer>();
            if (maskSpriteRenderer != null)
            {
                Color maskColor = new Color(maskRed, maskGreen, maskBlue, 1f);
                maskSpriteRenderer.color = maskColor;
            }
        }
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
        hpSlider.value = (float)CurrHP / MaxHP;
        mpSlider.value = (float)CurrMP / MaxMP;
    }

    public void ChangeHPBy(int value)
    {
        CurrHP += value;
        CurrHP = Mathf.Clamp(CurrHP, 0, MaxHP); // Prevent overflow or underflow.
        UpdateStatusPanel();
    }

    public void ChangeMPBy(int value)
    {
        CurrMP += value;
        CurrMP = Mathf.Clamp(CurrMP, 0, MaxMP);
        UpdateStatusPanel();
    }

    public int GetResistance(Element element) // where is my dict :(
    {
        if (element == Element.Fire) return FireRes;
        if (element == Element.Ice) return IceRes;
        return 0;
    }

    public bool CanAffordCast(int value)
    {
        return CurrMP >= value;
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
        if (combatManager != null)
        {
            combatManager.SelectUnit(this);
        }
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
        ChangeMPBy(MPRegen);
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
        if (CurrHP <= 0)
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

    public void ProcessCampfire()
    {
        while (Exp > ExpToNextLevel)
        {
            MaxHP += 50;
            MaxMP += 20;
            SkillPoints += 1;
            PAtk += 5;
            MAtk += 5;
            PDef += 2;
            MDef += 2;
            FireRes += 5;
            IceRes += 5;
            Exp -= ExpToNextLevel;
            ExpToNextLevel *= 2;
        }
    }
}
