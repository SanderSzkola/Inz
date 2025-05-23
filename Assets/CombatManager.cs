using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

public enum TurnState { INIT, PLAYER, ENEMY, ENEMYPROCESSING, RESULT }

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
    private readonly float selectionIndicatorOffset = -30f;

    private Unit activePlayerUnit = null; // does action
    private Unit targetUnit = null; // target of action
    private Spell selectedSpell = null; // spell to execute

    public SpellPanel spellPanel;
    private Dictionary<string, Spell> spells;

    private MessageLog messageLog;
    private int turnNumber = 1;

    private Dictionary<string, UnitData> enemyDefs;
    private SaveFileData SaveFileData;

    private DialogWindowManager DialogWindowManager;
    private int totalExp = 0;
    private bool victory;

    void Start()
    {
        turnState = TurnState.INIT;
        messageLog = FindAnyObjectByType<MessageLog>();

        spells = FileOperationsManager.Instance.LoadSpellDefs();

        // Load player units
        SaveFileData = FileOperationsManager.Instance.LoadSaveData();
        List<UnitData> playerData = new List<UnitData>(SaveFileData.playerUnits);

        foreach (var playerUnit in playerData)
        {
            SpawnUnit(playerPrefab,
                playerStatusPanel,
                playerPosition,
                playerUnits,
                new Vector2(1, 1),
                playerData.Count,
                playerData.IndexOf(playerUnit),
                playerUnit);
        }

        // Load enemy and level definitions
        enemyDefs = FileOperationsManager.Instance.LoadEnemyDefs();
        List<LevelVariationsList> levelDefs = FileOperationsManager.Instance.LoadLevelDefs();

        // Prepare enemies for the current level
        MapNode node = NodeMapGenerator.Instance.CurrentNode;
        int currentLevel = node.EncounterType == EncounterType.HARDBATTLE ? node.X + 2 : node.X;
        currentLevel = Mathf.Min(currentLevel, levelDefs.Count - 2);
        if (node.EncounterType == EncounterType.BOSS) currentLevel = levelDefs.Count - 1;
        UnityEngine.Random.InitState(FileOperationsManager.Instance.GetRandomSeedFromSave() + currentLevel); // constant seed to make loading previous save deterministic
        List<UnitData> enemyData = PrepareEnemiesForLevel(levelDefs[currentLevel]);

        for (int i = 0; i < enemyData.Count; i++)
        {
            SpawnUnit(enemyPrefab,
                enemyStatusPanel,
                enemyPosition,
                enemyUnits,
                new Vector2(-1, 1),
                enemyData.Count,
                i,
                enemyData[i]);
        }

        turnState = TurnState.PLAYER;
        SelectUnit(playerUnits[0]);

        messageLog.AddMessage($"Starting turn {turnNumber}.");
    }

    private void Update()
    {
        if (turnState == TurnState.ENEMY) // yes, it triggers exactly once and when needed, already checked that
        {
            StartCoroutine(HandleEnemyTurn());
        }
    }
    private List<UnitData> PrepareEnemiesForLevel(LevelVariationsList levelDef)
    {
        List<UnitData> enemyData = new List<UnitData>();
        string levelVariation = levelDef.levelVariations[UnityEngine.Random.Range(0, levelDef.levelVariations.Count)];
        foreach (string enemyType in levelVariation.Split(','))
        {
            string type = enemyType.Trim();
            if (enemyDefs.TryGetValue(type, out UnitData enemyUnitData))
            {
                enemyData.Add(enemyUnitData);
            }
            else
            {
                Debug.LogError($"Enemy type '{type}' not in enemyDefs");
            }
        }
        return enemyData;
    }

    private void SpawnUnit(GameObject unitPrefab, GameObject statusPanelPrefab, Transform position, List<Unit> unitList, Vector2 direction, int count, int positionNum, UnitData unitData)
    {
        if (unitData.CurrHP <= 0) return;

        // Space units evenly based on count and direction
        float maxYDistance = 25f;
        float yInterval = (count > 1) ? maxYDistance / (count - 1) : maxYDistance / 2;
        float xOffsetPerUnit = 18f;

        if (count == 1) positionNum = 1; // one enemy, spawn it at center 
        float yPosition = position.position.y + positionNum * yInterval * direction.y;
        float xPosition = position.position.x + positionNum * xOffsetPerUnit * direction.x;
        Vector3 spawnPosition = new Vector3(xPosition, yPosition, 0);

        // Instantiate the unit
        GameObject unitGameObject = Instantiate(unitPrefab, position);
        Unit unit = unitGameObject.GetComponent<Unit>();
        GameObject selectionIndicator = Instantiate(selectionIndicatorPrefab, position);

        unit.InitializeFromPrefab(this, spells, selectionIndicator, unitData);
        unit.transform.position = spawnPosition;

        // Graphic nonsense, Only Enemies edition
        Transform spriteTransform = unitGameObject.transform.Find("Sprite");
        if (spriteTransform != null)
        {
            SpriteRenderer spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = 100 - 1 * positionNum;
                string spritePath = $"UnitSprites/{unitData.Name}";
                Sprite sprite = Resources.Load<Sprite>(spritePath);
                if (sprite != null)
                {
                    spriteRenderer.sprite = sprite;
                }
                else
                {
                    Debug.LogError($"Sprite not found at path: {spritePath}");
                }
            }

            BoxCollider2D collider = unitGameObject.GetComponent<BoxCollider2D>();
            if (collider != null)
            {
                collider.offset = new Vector2(unitData.ColliderOffsetX, unitData.ColliderOffsetY);
                collider.size = new Vector2(unitData.ColliderSizeX, unitData.ColliderSizeY);
            }

            RectTransform spriteRect = spriteTransform.GetComponent<RectTransform>();
            if (spriteRect != null)
            {
                spriteRect.localPosition = new Vector3(unitData.SpritePosX, unitData.SpritePosY, unitData.SpritePosZ);
                spriteRect.sizeDelta = new Vector2(unitData.SpriteWidth, unitData.SpriteHeight);
            }
        }

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
            CastSpell(selectedSpell);

            spellPanel.RefreshSpellSelection(null, activePlayerUnit.canAct);
            selectedSpell = null;
            targetUnit = null;

            RefreshUnitSelections();
            return;
        }

        // if this place was reached - wrong target for spell was clicked - probably to select another activePlayerUnit
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
        StartCoroutine(activePlayerUnit.TakeActionAnim());
        spell.Execute(activePlayerUnit, targetUnit, messageLog); // checking if spell can be cast moved to spell class
    }

    private void RefreshUnitSelections()
    {
        foreach (Unit unit in playerUnits)
            unit.RefreshSelection(activePlayerUnit, targetUnit);
        foreach (Unit unit in enemyUnits)
            unit.RefreshSelection(activePlayerUnit, targetUnit);
        spellPanel.RefreshSpellSelection(null, activePlayerUnit.canAct);
        if (turnState == TurnState.PLAYER)
        {
            turnState = CheckTurnProgress();
        }
    }

    public void RemoveUnit(Unit unit)
    {
        if (playerUnits.Contains(unit))
        {
            messageLog.AddMessage($"<color=yellow>{unit.unitName} has fainted.</color>");
            playerUnits.Remove(unit);
            UnitData unitData = SaveFileData.playerUnits.FirstOrDefault(data => data.Name == unit.unitName);
            unitData.CurrHP = 0;
            unitData.CurrMP = 0;
        }
        else if (enemyUnits.Contains(unit))
        {
            float expMult = NodeMapGenerator.Instance.CurrentNode.EncounterType == EncounterType.HARDBATTLE ? 1.2f : 1f;
            int calculatedExp = (int)(unit.ExpOnDeath * expMult);
            messageLog.AddMessage($"<color=yellow>{unit.unitName} has been defeated.</color> All units gained {unit.ExpOnDeath} exp. ");
            foreach (Unit playerUnit in playerUnits)
            {
                playerUnit.Exp += calculatedExp;
            }
            enemyUnits.Remove(unit);
            totalExp += calculatedExp;
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
            victory = false;
            ShowDialogWindow();
        }
        else if (enemyUnits.Count == 0)
        {
            victory = true;
            ShowDialogWindow();
        }
    }

    private IEnumerator HandleEnemyTurn()
    {
        if (turnState != TurnState.ENEMY) yield break;
        turnState = TurnState.ENEMYPROCESSING;
        foreach (Unit enemy in enemyUnits)
        {
            enemy.ProcessNextTurn();
        }

        // maybe some additional logic here?

        yield return new WaitForSeconds(0.7f);
        foreach (Unit enemy in enemyUnits)
        {
            if (!enemy.canAct) continue;

            Spell chosenSpell = ChooseSpellForEnemy(enemy);
            Unit target = ChooseRandomTarget(playerUnits);

            if (chosenSpell != null && target != null)
            {
                StartCoroutine(enemy.TakeActionAnim());
                chosenSpell.Execute(enemy, target, messageLog);
                yield return new WaitForSeconds(0.5f);
            }
            RefreshUnitSelections();
            if (playerUnits.Count == 0 || enemyUnits.Count == 0)
            {
                yield break;
            }
        }
        StartPlayerTurn();
    }

    private Spell ChooseSpellForEnemy(Unit enemy)
    {
        List<Spell> availableSpells = enemy.AvailableSpells;
        Spell randomSpell = availableSpells[UnityEngine.Random.Range(0, availableSpells.Count)];
        if (randomSpell.IsReady() && enemy.CanAffordCast(randomSpell.MPCost))
        {
            return randomSpell;
        }
        foreach (Spell spell in availableSpells)
        {
            if (spell.IsReady() && enemy.CanAffordCast(spell.MPCost))
            {
                return spell;
            }
        }
        Debug.LogWarning($"Enemy {enemy.unitName} ran out of castable spells.");
        return null;
    }

    private Unit ChooseRandomTarget(List<Unit> targets)
    {
        if (targets.Count == 0) return null;
        return targets[UnityEngine.Random.Range(0, targets.Count)];
    }

    private void StartPlayerTurn()
    {
        foreach (Unit unit in playerUnits)
        {
            unit.ProcessNextTurn();
        }
        turnState = TurnState.PLAYER;
        RefreshUnitSelections();
        turnNumber += 1;
        messageLog.AddMessage($"Starting turn {turnNumber}.");
    }

    public void PrepareDataToSave()
    {
        foreach (Unit unit in playerUnits)
        {
            UnitData unitData = SaveFileData.playerUnits.FirstOrDefault(data => data.Name == unit.unitName);
            if (unitData != null)
            {
                unitData.CurrHP = unit.CurrHP;
                unitData.CurrMP = unit.CurrMP;
                unitData.Exp = unit.Exp;
            }
        }
    }

    public void CleanupBeforeSceneLoad()
    {
        // idk if neccesary, better to be safe
        playerUnits.Clear();
        enemyUnits.Clear();
        SaveFileData = null;
        messageLog.ClearMessages();
    }

    private void ConfirmEndBattle()
    {
        if (victory)
        {
            if (NodeMapGenerator.Instance.CurrentNode.EncounterType == EncounterType.BOSS)
            {
                CleanupBeforeSceneLoad();
                SceneManager.LoadScene("MainMenuScene");
            }
            else
            {
                PrepareDataToSave();
                FileOperationsManager.Instance.SaveGame();
                CleanupBeforeSceneLoad();
                SceneManager.LoadScene("MapScene");
            }
        }
        else
        {
            CleanupBeforeSceneLoad();
            SceneManager.LoadScene("MainMenuScene");
        }
    }

    public void ShowDialogWindow()
    {
        if (DialogWindowManager == null)
        {
            GameObject dialogCanvas = GameObject.Find("DialogCanvas");
            Transform dialogPanelTransform = dialogCanvas.transform.Find("DialogPanel");
            DialogWindowManager = dialogPanelTransform.GetComponent<DialogWindowManager>();
        }
        if (victory)
        {
            if (NodeMapGenerator.Instance.CurrentNode.EncounterType == EncounterType.BOSS)
            {
                DialogWindowManager.SetMessage($"Victory!\nYou have defeated the final boss! Your quest is done.");
            }
            else
            {
                DialogWindowManager.SetMessage($"Victory!\nAll units gained {totalExp} exp.\nGame saved.");
            }
        }
        else
        {
            DialogWindowManager.SetMessage($"Defeat!\nYou can try again or start a new game.");
        }
        DialogWindowManager.ShowWindow();
    }
}
