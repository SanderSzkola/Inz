using UnityEngine;

public class SaveFileManager : MonoBehaviour
{
    public static SaveFileManager Instance;
    public string SaveFilePath;

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

    public void SetUpNewGame()
    {
        SaveFilePath = "Defs/newGamePlayerUnits.json";
    }

    public void LoadGame(int num = 0)
    {
        SaveFilePath = $"Saves/{num}.json";
    }
}
