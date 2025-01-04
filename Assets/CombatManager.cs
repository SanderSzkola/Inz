using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public enum TurnState { INIT, PLAYER, ENEMY, RESULT }

public class CombatManager : MonoBehaviour
{
    public TurnState turnState;

    public GameObject playerPrefab;
    public GameObject playerStatusPanel;
    public RectTransform playerPosition;
    private List<Unit> playerUnits = new List<Unit>();

    public GameObject enemyPrefab;
    public GameObject enemyStatusPanel; // why do i have separate status panel for enemy?? no time to check
    public RectTransform enemyPosition;
    private List<Unit> enemyUnits = new List<Unit>();

    public GameObject selectionIndicatorPrefab; // to pass on to each unit
    private readonly float statusPanelOffset = 0.5f; // for creating stPanel below unit, not on top
    public float selectionIndicatorOffset = -3f;

    private Unit activePlayerUnit = null; // does action
    private Unit targetUnit = null; // target of action
    private Spell selectedSpell = null; // spell to execute

    public SpellPanel spellPanel;
    private List<Spell> spells; // list of all spells, maybe move out?

    private MessageLog messageLog;

    private Dictionary<string, UnitData> enemyDefs;
    private List<string> levelDefs;
    [Range(0, 2)] public int currentLevel = 0;

    private string playerSaveFile = "SaveData/playerUnits.json";
    private string enemyDefsFile = "Defs/enemyDefs.json";
    private string levelDefsFile = "Defs/levelDefs.json";
    private string spellDefsFile = "Defs/spellDefs.json";

    void Start()
    {
        turnState = TurnState.INIT;
        spells = SpellLoader.LoadSpells(spellDefsFile);
        messageLog = FindAnyObjectByType<MessageLog>();

        // Load player units from save file
        string json = File.ReadAllText(playerSaveFile);
        UnitDataList unitDataList = JsonUtility.FromJson<UnitDataList>(json);
        List<UnitData> playerData = new List<UnitData>(unitDataList.playerUnits);

        for (int i = 0; i < playerData.Count; i++)
        {
            SpawnUnit(
                unitPrefab: playerPrefab,
                statusPanelPrefab: playerStatusPanel,
                position: playerPosition,
                direction: new Vector2(1, 1), // NE direction
                unitList: playerUnits,
                count: playerData.Count,
                positionNum: i,
                unitData: playerData[i]
            );
        }

        // Load enemy defs
        json = File.ReadAllText(enemyDefsFile);
        EnemyDefsWrapper enemyDefsWrapper = JsonUtility.FromJson<EnemyDefsWrapper>(json);
        enemyDefs = new Dictionary<string, UnitData>();
        foreach (var enemyDef in enemyDefsWrapper.enemyDefs)
        {
            enemyDefs.Add(enemyDef.key, enemyDef.unitData);
        }

        // Load level defs
        json = File.ReadAllText(levelDefsFile);
        LevelDefsList levelDefsList = JsonUtility.FromJson<LevelDefsList>(json);
        levelDefs = new List<string>(levelDefsList.levelDefs);

        // Prepare enemy data for the current level
        List<UnitData> enemyData = new List<UnitData>();
        string currentLevelConfig = levelDefs[currentLevel];

        foreach (string enemyType in currentLevelConfig.Split(','))
        {
            string trimmedType = enemyType.Trim();
            if (enemyDefs.TryGetValue(trimmedType, out UnitData enemyUnitData))
            {
                enemyData.Add(enemyUnitData);
            }
            else
            {
                Debug.LogError($"Enemy type '{trimmedType}' not in enemyDefs");
            }
        }

        for (int i = 0; i < enemyData.Count; i++)
        {
            SpawnUnit(
                unitPrefab: enemyPrefab,
                statusPanelPrefab: enemyStatusPanel,
                position: enemyPosition,
                direction: new Vector2(-1, 1), // NW direction
                unitList: enemyUnits,
                count: enemyData.Count,
                positionNum: i,
                unitData: enemyData[i]
            );
        }

        turnState = TurnState.PLAYER;

        if (playerUnits.Count > 0)
        {
            activePlayerUnit = playerUnits[0];
        }
        RefreshUnitSelections();
    }


    private void SpawnUnit(GameObject unitPrefab, GameObject statusPanelPrefab, Transform position, List<Unit> unitList, Vector2 direction, int count, int positionNum, UnitData unitData)
    {
        // Space units evenly based on count and direction
        float maxYDistance = 25f;
        float yInterval = (count > 1) ? maxYDistance / (count - 1) : 0;
        float xOffsetPerUnit = 14f;

        float yPosition = position.position.y + positionNum * yInterval * direction.y;
        float xPosition = position.position.x + positionNum * xOffsetPerUnit * direction.x;
        Vector3 spawnPosition = new Vector3(xPosition, yPosition, 0);

        // Instantiate the unit
        GameObject unitGameObject = Instantiate(unitPrefab, position);
        Unit unit = unitGameObject.GetComponent<Unit>();
        GameObject selectionIndicator = Instantiate(selectionIndicatorPrefab, position);

        unit.InitializeFromPrefab(this, spells, selectionIndicator, unitData);
        unit.transform.position = spawnPosition;

        // Instantiate the status panel below the unit
        GameObject statusPanelGO = Instantiate(statusPanelPrefab, position);
        statusPanelGO.transform.position = new Vector3(xPosition, yPosition - statusPanelOffset, 0);

        unit.statusPanel = statusPanelGO;
        unit.UpdateStatusPanel();

        selectionIndicator.transform.position = new Vector3(xPosition, yPosition - selectionIndicatorOffset, 0);

        // Add unit to the corresponding list
        unitList.Add(unit);
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
            // Debug.Log("Active unit: " + activePlayerUnit.unitName + ", target unit: " + targetUnit.unitName + ", selected spell: " + selectedSpell.Name);
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
        spell.Execute(activePlayerUnit, targetUnit, messageLog); // checking if spell can be cast moved to spell class
    }

    private void RefreshUnitSelections()
    {
        foreach (Unit unit in playerUnits)
            unit.RefreshSelection(activePlayerUnit, targetUnit);
        foreach (Unit unit in enemyUnits)
            unit.RefreshSelection(activePlayerUnit, targetUnit);
        spellPanel.RefreshSpellSelection(null, activePlayerUnit.canAct);
        turnState = CheckTurnProgress();
    }

    public void RemoveUnit(Unit unit)
    {
        if (playerUnits.Contains(unit))
        {
            messageLog.AddMessage($"<color=yellow>{unit.unitName} has fainted.</color>");
            playerUnits.Remove(unit);
        }
        else if (enemyUnits.Contains(unit))
        {
            messageLog.AddMessage($"<color=yellow>{unit.unitName} has been defeated.</color>");
            enemyUnits.Remove(unit);
        }

        CheckBattleEndConditions();
    }

    private TurnState CheckTurnProgress()
    {
        foreach (Unit unit in playerUnits)
        {
            if (unit.canAct)
            {
                return TurnState.PLAYER;
            }
        }
        return TurnState.ENEMY;
    }

    private void CheckBattleEndConditions()
    {
        if (playerUnits.Count == 0)
        {
            Debug.Log("All player units have been defeated. Game Over!");
            // Trigger defeat logic
        }
        else if (enemyUnits.Count == 0)
        {
            Debug.Log("All enemies have been defeated. Victory!");
            // Trigger victory logic
        }
    }
}
