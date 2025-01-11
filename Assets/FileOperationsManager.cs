using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FileOperationsManager : MonoBehaviour
{
    public static FileOperationsManager Instance;
    public string SaveFilePath;
    public int SaveSlot;

    private Dictionary<string, UnitData> enemyDefsCache;
    private List<string> levelDefsCache;
    private List<Spell> spellDefsCache;

    private readonly string newGameSaveFile = "Defs/newGameSaveFile.json";
    private readonly string enemyDefsFile = "Defs/enemyDefs.json";
    private readonly string levelDefsFile = "Defs/levelDefs.json";
    private readonly string spellDefsFile = "Defs/spellDefs.json";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetUpNewGame(int num)
    {
        SaveSlot = num;
        SaveFilePath = newGameSaveFile;
        SceneManager.LoadScene("BattleScene");
    }

    private string GetSavePath(int num)
    {
        // thats how normal save path would look like
        // return Path.Combine(Application.persistentDataPath, $"Saves/{num}.json");
        // thats how it look like if i want it in project folder
        return $"Saves/{num}.json";
    }

    public bool DoesSaveExist(int num)
    {
        return File.Exists(GetSavePath(num));
    }

    public string GetSaveDate(int num)
    {
        string path = GetSavePath(num);
        if (File.Exists(path))
        {
            DateTime creationTime = File.GetLastWriteTime(path);
            return creationTime.ToString("yyyy-MM-dd HH:mm:ss");
        }
        return "No Save";
    }

    public void LoadGame(int num)
    {
        SaveSlot = num;
        SaveFilePath = GetSavePath(SaveSlot);
        SceneManager.LoadScene("BattleScene");
    }

    public void SaveGame(SaveFileData unitDataList)
    {
        SaveFilePath = GetSavePath(SaveSlot);
        string json = JsonUtility.ToJson(unitDataList, true);
        File.WriteAllText(SaveFilePath, json);
    }

    public void DeleteSave(int num)
    {
        File.Delete(GetSavePath(num));
    }

    public SaveFileData LoadPlayerData() // no cache bc it needs to be reloaded after each level
    {
        string json = File.ReadAllText(SaveFilePath);
        return JsonUtility.FromJson<SaveFileData>(json);
    }

    public Dictionary<string, UnitData> LoadEnemyDefs()
    {
        if (enemyDefsCache == null)
        {
            string json = File.ReadAllText(enemyDefsFile);
            EnemyDefsList enemyDefsWrapper = JsonUtility.FromJson<EnemyDefsList>(json);
            enemyDefsCache = enemyDefsWrapper.enemyDefs.ToDictionary(e => e.key, e => e.unitData);
        }
        return enemyDefsCache;
    }

    public List<string> LoadLevelDefs()
    {
        if (levelDefsCache == null)
        {
            string json = File.ReadAllText(levelDefsFile);
            LevelDefsList levelDefsList = JsonUtility.FromJson<LevelDefsList>(json);
            levelDefsCache = new List<string>(levelDefsList.levelDefs);
        }
        return levelDefsCache;
    }

    public List<Spell> LoadSpellDefs()
    {
        if (spellDefsCache == null)
        {
            spellDefsCache = new List<Spell>();
            string json = File.ReadAllText(spellDefsFile);
            List<SpellData> spellDataList = JsonUtility.FromJson<SpellDataList>(json).spells;

            foreach (SpellData data in spellDataList)
            {
                Sprite graphic = Resources.Load<Sprite>(data.graphicPath);
                if (graphic == null)
                {
                    Debug.LogWarning($"Sprite not found at {data.graphicPath} for spell {data.name}. Using a placeholder.");
                    graphic = Resources.Load<Sprite>("Spells/NoTexture");
                }

                Spell spell = CreateSpell(data, graphic);
                if (spell != null)
                {
                    spellDefsCache.Add(spell);
                }
            }
        }

        return spellDefsCache;
    }

    private Spell CreateSpell(SpellData data, Sprite graphic)
    {
        TargetingMode targetingMode = (TargetingMode)System.Enum.Parse(typeof(TargetingMode), data.targetingMode);
        Element element = (Element)System.Enum.Parse(typeof(Element), data.element);

        switch (targetingMode)
        {
            case TargetingMode.Enemy:
                return new AttackSpell(data.name, data.power, data.mpCost, data.cooldown, targetingMode, element, graphic);

            case TargetingMode.Self:
                return new RestoreSpell(data.name, data.power, data.mpCost, data.cooldown, targetingMode, element, graphic);

            // TODO: more spell types would go here

            default:
                Debug.LogWarning($"Unknown targeting mode {data.targetingMode} for spell {data.name}. Skipping.");
                return null;
        }
    }
}