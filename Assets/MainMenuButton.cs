using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuButton : MonoBehaviour
{
    public void B_Options()
    {
        Debug.Log("Clicked B_Options, temporarly redirects to map scene");
        SceneManager.LoadScene("MapScene");
    }

    public void B_Quit()
    {
        Application.Quit();
    }
}
