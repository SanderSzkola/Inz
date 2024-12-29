using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

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

    public Dictionary<Element, int> Resistances = new Dictionary<Element, int>
    {
        { Element.Fire, 0 },
        { Element.Ice, 0 },
    };

    public GameObject statusPanel;
    private GameObject selectionIndicator;

    private Slider hpSlider;
    private Slider mpSlider;
    private CombatManager combatManager;

    private SpriteRenderer maskSpriteRenderer;
    [Range(0f, 1f)] private float maskRed = 1f;
    [Range(0f, 1f)] private float maskGreen = 1f;
    [Range(0f, 1f)] private float maskBlue = 1f;

public void InitializeFromPrefab(CombatManager manager, List<Spell> spells, GameObject selectionIndicator, UnitData data)
    {
        combatManager = manager;
        AvailableSpells = spells;
        InitializeFromData(data);

        this.selectionIndicator = selectionIndicator;
        this.selectionIndicator.SetActive(false);
        RefreshSelectorColor();

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

    public void InitializeFromData(UnitData data)
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
        Resistances = data.Resistances;
    }

    private void ApplyMask()
    {
        // Apply mask color based on the Unit parameters
        // temp
        maskRed = Random.Range(0f, 1f);
        maskGreen = Random.Range(0f, 1f);
        maskBlue = Random.Range(0f, 1f);
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

    public void CastSpell(Spell spell, Unit target)
    {
        if (spell.IsOnCooldown())
        {
            Debug.Log($"{spell.Name} is on cooldown!");
            return;
        }

        if (!CanAffordCast(spell.MPCost))
        {
            Debug.Log("Not enough MP!");
            return;
        }

        ChangeMPBy(-spell.MPCost);
        spell.Execute(this, target);
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

    public int GetResistance(Element element)
    {
        return Resistances.TryGetValue(element, out int resistance) ? resistance : 0;
    }

    public bool CanAffordCast(int value)
    {
        return currMP >= value;
    }

    public void ApplyDamage(int damage)
    {
        ChangeHPBy(-damage);
        if (currHP <= 0)
        {
            canAct = false;
            Debug.Log($"{unitName} has been defeated!");
        }
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
}

[System.Serializable]
public class UnitData
{
    public string Name;
    public bool IsPlayerUnit;
    public int MaxHP;
    public int CurrHP;
    public int MaxMP;
    public int CurrMP;
    public int MPRegen;
    public int PAtk;
    public int PDef;
    public int MAtk;
    public int MDef;
    public Vector2 Position;
    public Dictionary<Element, int> Resistances;
    public List<string> Spells;
}