using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuButton : MonoBehaviour
{
    public void B_NewGame()
    {
        Debug.Log("Clicked B_NewGame");
        SaveFileManager.Instance.SetUpNewGame();
        SceneManager.LoadScene("BattleScene");
    }

    public void B_Continue()
    {
        Debug.Log("Clicked B_Continue");
        SaveFileManager.Instance.LoadGame(0); // TODO: add more save slots and a way to pick them
        SceneManager.LoadScene("BattleScene");
    }

    public void B_Options()
    {
        Debug.Log("Clicked B_Options");
    }

    public void B_Quit()
    {
        Debug.Log("Clicked B_Quit");
    }
}
