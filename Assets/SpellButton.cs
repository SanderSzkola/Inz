using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using System.IO;

public class SpellButton : MonoBehaviour
{
    private Image icon;
    private Button button;
    private CombatManager combatManager;
    private Spell spell;
    public Spell Spell => spell;
    private TextMeshProUGUI buttonText;
    private Transform frameTransform;
    private Transform coverTransform;

    private void Awake()
    {
        icon = GetComponentInChildren<Image>();
        button = GetComponent<Button>();
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
        frameTransform = transform.Find("Frame");
        coverTransform = transform.Find("Cover");
        coverTransform.gameObject.SetActive(false);
    }

    public void Initialize(Spell spell, CombatManager combatManager, bool canAct)
    {
        this.spell = spell;
        this.combatManager = combatManager;

        icon.sprite = spell.Graphic;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnSpellButtonClick());
        Refresh(false, canAct);
        AddHoverEvents();
    }

    public void Initialize(Spell spell)
    {
        this.spell = spell;
        icon.sprite = spell.Graphic;
        AddHoverEvents();
    }

    private void AddHoverEvents()
    {
        if (spell == null || coverTransform == null) return;

        // Add hover functionality
        var eventTrigger = gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();

        var entryEnter = new UnityEngine.EventSystems.EventTrigger.Entry
        {
            eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter
        };
        entryEnter.callback.AddListener((eventData) => ShowSpellDetails());

        var entryExit = new UnityEngine.EventSystems.EventTrigger.Entry
        {
            eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit
        };
        entryExit.callback.AddListener((eventData) => HideSpellDetails());

        eventTrigger.triggers.Add(entryEnter);
        eventTrigger.triggers.Add(entryExit);
    }

    private void ShowSpellDetails()
    {
        if (coverTransform == null || buttonText == null) return;

        coverTransform.gameObject.SetActive(true);
        buttonText.fontSize = 1.5f;
        buttonText.text = spell.Description();
    }

    private void HideSpellDetails()
    {
        if (coverTransform == null || buttonText == null) return;

        coverTransform.gameObject.SetActive(false);
        if (spell != null && spell.RemainingCooldown > 0)
        {
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.fontSize = 20;
            buttonText.text = spell.RemainingCooldown.ToString();
        }
        else
        {
            buttonText.text = "";
        }
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
            buttonText.alignment = TextAlignmentOptions.Center;
            buttonText.fontSize = 20;
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
        combatManager?.SelectSpell(this);
        HideSpellDetails();
    }
}
