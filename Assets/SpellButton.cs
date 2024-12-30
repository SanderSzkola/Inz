using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpellButton : MonoBehaviour
{
    private Image icon;
    private Button button;
    private CombatManager combatManager;
    private Spell spell;
    public Spell Spell => spell;
    private TextMeshProUGUI buttonText;
    private Transform frameTransform;

    private void Awake()
    {
        icon = GetComponentInChildren<Image>();
        button = GetComponent<Button>();
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void Initialize(Spell spell, CombatManager combatManager, bool canAct)
    {
        this.spell = spell;
        this.combatManager = combatManager;
        frameTransform = transform.Find("Frame");

        // Set icon and button state
        icon.sprite = spell.Graphic;
        // Assign click listener
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnSpellButtonClick());
        Refresh(false, canAct);
    }

    public void DisableButton()
    {
        icon.sprite = null;
        button.interactable = false;
        icon.color = Color.clear;
        buttonText.text = "";
    }

    public void Refresh(bool selected, bool canAct)
    {
        bool shouldBeActive = canAct && spell != null && !spell.IsOnCooldown();
        button.interactable = shouldBeActive;
        icon.color = shouldBeActive ? Color.white : Color.gray;

        if (spell != null && spell.RemainingCooldown > 0)
        {
            buttonText.text = spell.RemainingCooldown.ToString();
        }
        else
        {
            buttonText.text = "";
        }
        if (selected && shouldBeActive)
        {
            frameTransform.GetComponent<Image>().color = Color.green;
        }
        else
        {
            if (frameTransform != null && frameTransform.GetComponent<Image>() != null)
            {
                frameTransform.GetComponent<Image>().color = Color.white;
            }
        }
    }

    private void OnSpellButtonClick()
    {
        combatManager.SelectSpell(this);
    }
}
