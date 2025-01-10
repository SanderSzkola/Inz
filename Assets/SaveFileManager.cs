using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveFileManager : MonoBehaviour
{
    public static SaveFileManager Instance;
    public string SaveFilePath;
    public int SaveSlot;

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
        SaveFilePath = "Defs/newGamePlayerUnits.json";
        SceneManager.LoadScene("BattleScene");
    }


    private string GetSavePath(int num)
    {
        //return Path.Combine(Application.persistentDataPath, $"Saves/{num}.json");
        return $"Saves/{num}.json";
    }

    public bool DoesSaveExist(int num)
    {
        string path = GetSavePath(num);
        return File.Exists(path);
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

    public void SaveGame(UnitDataList unitDataList)
    {
        SaveFilePath = GetSavePath(SaveSlot);
        string json = JsonUtility.ToJson(unitDataList, true);
        File.WriteAllText(SaveFilePath, json);
    }

    public void DeleteSave(int num)
    {
        File.Delete(GetSavePath(num));
    }
}