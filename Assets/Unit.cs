using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class Unit : MonoBehaviour
{
    public string unitName;
    public bool canAct = true;
    public bool isPlayerUnit = false;
    public int maxHP = 500;
    public int currHP = 490;
    public int maxMP = 500;
    public int currMP = 490;
    public int mpRegen = 25;
    public int pAtk = 40;
    public int pDef = 20;
    public int mAtk = 40;
    public int mDef = 20;
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

    public void Initialize(CombatManager manager, List<Spell> spells, GameObject selectionIndicator)
    {
        combatManager = manager;
        AvailableSpells = spells;

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

        if (!canAffordCast(spell.MPCost))
        {
            Debug.Log("Not enough MP!");
            return;
        }

        changeMPBy(-spell.MPCost);
        spell.Execute(this, target);
    }

    public void updateStatusPanel()
    {
        if (!hpSlider)
        {
            hpSlider = statusPanel.transform.Find("HPBar").GetComponent<Slider>();
            mpSlider = statusPanel.transform.Find("MPBar").GetComponent<Slider>();
        }
        hpSlider.value = (float)currHP / maxHP;
        mpSlider.value = (float)currMP / maxMP;
    }

    public void changeHPBy(int value)
    {
        currHP += value;
        currHP = Mathf.Clamp(currHP, 0, maxHP); // Prevent overflow or underflow.
        updateStatusPanel();
    }

    public void changeMPBy(int value)
    {
        currMP += value;
        currMP = Mathf.Clamp(currMP, 0, maxMP);
        updateStatusPanel();
    }

    public int GetResistance(Element element)
    {
        return Resistances.TryGetValue(element, out int resistance) ? resistance : 0;
    }

    public bool canAffordCast(int value)
    {
        return currMP >= value;
    }

    public void ApplyDamage(int damage)
    {
        changeHPBy(-damage);
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
