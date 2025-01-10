using UnityEngine;

public class SavePanelManager : MonoBehaviour
{
    public GameObject savePanel;

    private void Start()
    {
        HidePanel();
    }

    public void ShowPanel()
    {
        savePanel.SetActive(true);
    }

    public void HidePanel()
    {
        savePanel.SetActive(false);
    }
}
