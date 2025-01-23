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

    private SaveFileData SaveData;
    private Dictionary<string, UnitData> enemyDefsCache;
    private List<string> levelDefsCache;
    private Dictionary<string, Spell> spellDefsCache;
    private Dictionary<EncounterType, Sprite> mapSprites;

    private readonly string newGameSaveFilePath = "Defs/newGameSaveFile.json";
    private readonly string enemyDefsFilePath = "Defs/enemyDefs.json";
    private readonly string levelDefsFilePath = "Defs/levelDefs.json";
    private readonly string spellDefsFilePath = "Defs/spellDefs.json";
    private readonly string mapSpritesFilePath = "MapSpritesWhite";

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
        SaveFilePath = newGameSaveFilePath;
        NodeMapGenerator.Instance.LoadMapFromData(LoadSaveData().mapData);

        //SceneManager.LoadScene("BattleScene");
        SceneManager.LoadScene("MapScene");
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
        NodeMapGenerator.Instance.LoadMapFromData(LoadSaveData().mapData);
        SceneManager.LoadScene("MapScene");
    }

    public void SaveGame(SaveFileData saveFileData)
    {
        SaveFilePath = GetSavePath(SaveSlot);
        saveFileData.mapData = NodeMapGenerator.Instance.GetMapData();
        string json = JsonUtility.ToJson(saveFileData, true);
        File.WriteAllText(SaveFilePath, json);
        SaveData = null;
    }

    public void DeleteSave(int num)
    {
        File.Delete(GetSavePath(num));
    }

    public SaveFileData LoadSaveData() 
    {
        if (SaveData == null)
        {
            string json = File.ReadAllText(SaveFilePath);
            SaveData = JsonUtility.FromJson<SaveFileData>(json);
        }
        return SaveData;
    }

    public Dictionary<string, UnitData> LoadEnemyDefs()
    {
        if (enemyDefsCache == null)
        {
            string json = File.ReadAllText(enemyDefsFilePath);
            EnemyDefsList enemyDefsList = JsonUtility.FromJson<EnemyDefsList>(json);
            enemyDefsCache = enemyDefsList.enemyDefs.ToDictionary(e => e.key, e => e.unitData);
        }
        return enemyDefsCache;
    }

    public List<string> LoadLevelDefs()
    {
        if (levelDefsCache == null)
        {
            string json = File.ReadAllText(levelDefsFilePath);
            LevelDefsList levelDefsList = JsonUtility.FromJson<LevelDefsList>(json);
            levelDefsCache = new List<string>(levelDefsList.levelDefs);
        }
        return levelDefsCache;
    }

    public Dictionary<string, Spell> LoadSpellDefs()
    {
        if (spellDefsCache == null)
        {
            spellDefsCache = new Dictionary<string, Spell>();
            string json = File.ReadAllText(spellDefsFilePath);
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
                    spellDefsCache[data.name] = spell; // Add to dictionary with spell.Name as key
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
public Dictionary<EncounterType, Sprite> LoadMapSprites()
{
    if (mapSprites == null)
    {
        mapSprites = new Dictionary<EncounterType, Sprite>();

        Sprite[] sprites = Resources.LoadAll<Sprite>(mapSpritesFilePath);
        foreach (Sprite sprite in sprites)
        {
            if (Enum.TryParse(sprite.name, true, out EncounterType encounterType))
            {
                if (!mapSprites.ContainsKey(encounterType))
                {
                    mapSprites.Add(encounterType, sprite);
                }
            }
            else
            {
                Debug.LogWarning($"Sprite name '{sprite.name}' does not match any EncounterType.");
            }
        }
    }

    return mapSprites;
}

}