using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DialogWindowManager : MonoBehaviour
{
    public GameObject DialogWindow;
    public TextMeshProUGUI TextUnityObject;

    private void Start()
    {
        HideWindow();
    }

    public void ShowWindow()
    {
        DialogWindow.SetActive(true);
    }

    public void HideWindow()
    {
        DialogWindow.SetActive(false);
    }

    public void SetMessage(string message)
    {
        if (TextUnityObject != null)
        {
            TextUnityObject.text = message;
        }
        else
        {
            Debug.LogWarning("Tried to SetMessage on component with no TextUnityObject");
        }
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenuScene");
    }
}
