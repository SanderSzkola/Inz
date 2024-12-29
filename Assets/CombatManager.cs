using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public enum TurnState { INIT, PLAYER, ENEMY, RESULT }

public class CombatManager : MonoBehaviour
{
    public TurnState turnState;

    public int pcCount = 3; // temp for testing, should be read from combat params / num of PlayerCharacterCount
    public GameObject playerPrefab;
    public GameObject playerStatusPanel;
    public RectTransform playerPosition;
    private List<Unit> playerUnits = new List<Unit>();

    public int enemyCount = 3; // temp for testing, should be read from combat params / num of enemies
    public GameObject enemyPrefab;
    public GameObject enemyStatusPanel;
    public RectTransform enemyPosition;
    private List<Unit> enemyUnits = new List<Unit>();

    public GameObject selectionIndicatorPrefab; // to pass on to each unit
    private readonly float statusPanelOffset = 0.5f; // for creating stPanel below unit, not on top
    public float selectionIndicatorOffset = -3f;

    private Unit activePlayerUnit = null; // does action
    private Unit targetUnit = null; // target of action
    private Spell selectedSpell = null;

    public SpellPanel spellPanel;
    private List<Spell> spells; // list of all spells, maybe move out?


    void Start()
    {
        turnState = TurnState.INIT;
        spells = SpellLoader.LoadSpells("Spells/spells"); // temp for tests, each unit should load their own spells

        SpawnUnits(
            count: pcCount,
            unitPrefab: playerPrefab,
            statusPanelPrefab: playerStatusPanel,
            position: playerPosition,
            unitList: playerUnits,
            direction: new Vector2(1, 1) // NE direction
        );

        SpawnUnits(
            count: enemyCount,
            unitPrefab: enemyPrefab,
            statusPanelPrefab: enemyStatusPanel,
            position: enemyPosition,
            unitList: enemyUnits,
            direction: new Vector2(-1, 1) // NW direction
        );

        turnState = TurnState.PLAYER;

        //if (playerUnits.Count > 0)
        //{
        //    activePlayerUnit = playerUnits[0];
        //}
        RefreshUnitSelections();
    }


    private void SpawnUnits(int count, GameObject unitPrefab, GameObject statusPanelPrefab, Transform position, List<Unit> unitList, Vector2 direction)
    {
        // Space units evenly based on count and direction
        float maxYDistance = 25f;
        float yInterval = maxYDistance / (count - 1);
        float xOffsetPerUnit = 14f;

        for (int i = 0; i < count; i++)
        {
            float yPosition = position.position.y + i * yInterval * direction.y;
            float xPosition = position.position.x + i * xOffsetPerUnit * direction.x;
            Vector3 spawnPosition = new Vector3(xPosition, yPosition, 0);

            // Instantiate the unit
            GameObject unitGameObject = Instantiate(unitPrefab, position);
            Unit unit = unitGameObject.GetComponent<Unit>();
            GameObject selectionIndicator = Instantiate(selectionIndicatorPrefab, position);

            unit.Initialize(this, spells, selectionIndicator);
            unit.transform.position = spawnPosition;

            // Instantiate the status panel below the unit
            GameObject statusPanelGO = Instantiate(statusPanelPrefab, position);
            statusPanelGO.transform.position = new Vector3(xPosition, yPosition - statusPanelOffset, 0);

            unit.statusPanel = statusPanelGO;
            unit.updateStatusPanel();

            selectionIndicator.transform.position = new Vector3(xPosition, yPosition - selectionIndicatorOffset, 0);

            // Add unit to the corresponding list
            unitList.Add(unit);
        }
    }


    public void SelectUnit(Unit unit)
    {
        targetUnit = null;

        // no spell chosen - select active unit
        if (activePlayerUnit == null || selectedSpell == null)
        {
            if (unit.isPlayerUnit)
            {
                activePlayerUnit = unit;
                spellPanel.LoadSpells(unit, this);
            }
            RefreshUnitSelections();
            return;
        }

        // spell chosen - select target
        if (selectedSpell.TargetingMode == TargetingMode.Enemy && !unit.isPlayerUnit)
        {
            targetUnit = unit;
        }
        else if (selectedSpell.TargetingMode == TargetingMode.Ally && unit.isPlayerUnit)
        {
            targetUnit = unit;
        }
        else if (selectedSpell.TargetingMode == TargetingMode.Self && unit == activePlayerUnit)
        {
            targetUnit = unit;
        }

        if (targetUnit != null)
        {
            // try casting spell
            Debug.Log("Active unit: " + activePlayerUnit.unitName + ", target unit: " + targetUnit.unitName + ", selected spell: " + selectedSpell.Name);
            CastSpell(selectedSpell);

            spellPanel.RefreshSpellSelection(null, activePlayerUnit.canAct);
            selectedSpell = null;
            targetUnit = null;

            // if there was animation, this would be good point to wait for it to finish
            RefreshUnitSelections();
            return;
        }

        // if this place was reached - wrong target was clicked - probably to select another activePlayerUnit
        if (unit.isPlayerUnit)
        {
            activePlayerUnit = unit;
            spellPanel.LoadSpells(unit, this);
        }
        RefreshUnitSelections();
        return;
    }

    public void SelectSpell(SpellButton spellButton)
    {
        selectedSpell = spellButton.Spell;
        spellPanel.RefreshSpellSelection(spellButton, activePlayerUnit.canAct);
    }

    private void CastSpell(Spell spell)
    {
        if (turnState != TurnState.PLAYER || activePlayerUnit == null || !activePlayerUnit.canAct || targetUnit == null) return;

        if (activePlayerUnit.canAffordCast(spell.MPCost))
        {
            spell.Execute(activePlayerUnit, targetUnit);
        }
        else
        {
            Debug.Log("Not enough MP to cast the spell!");
            // maybe set spells to be unclickabe?
        }
    }

    private void RefreshUnitSelections()
    {
        foreach (Unit unit in playerUnits)
            unit.RefreshSelection(activePlayerUnit, targetUnit);
        foreach (Unit unit in enemyUnits)
            unit.RefreshSelection(activePlayerUnit, targetUnit);
    }
}
