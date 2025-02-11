using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveFileMenuButton : MonoBehaviour
{
    public int SaveSlotNumber;

    private Button loadButton;
    private Button deleteButton;
    private TextMeshProUGUI saveText;
    private TextMeshProUGUI saveData;
    private bool saveExists;
    private bool awaitingConfirmation = false;
    private Coroutine confirmationCoroutine;

    private void Awake()
    {
        loadButton = transform.Find("B_Save").GetComponent<Button>();
        deleteButton = transform.Find("B_Delete").GetComponent<Button>();
        saveText = transform.Find("B_Save/SaveText").GetComponent<TextMeshProUGUI>();
        saveData = transform.Find("Background/SaveData").GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        UpdateButtonState();
        loadButton.onClick.RemoveAllListeners();
        loadButton.onClick.AddListener(() => OnLoadButtonClicked());
        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(() => OnDeleteButtonClicked());
    }

    public void UpdateButtonState()
    {
        saveExists = FileOperationsManager.Instance.DoesSaveExist(SaveSlotNumber);
        if (saveExists)
        {
            saveText.text = $"Load {SaveSlotNumber}";
            saveData.text = FileOperationsManager.Instance.GetSaveDate(SaveSlotNumber);
            deleteButton.interactable = true;
            deleteButton.image.color = Color.white;
        }
        else
        {
            saveText.text = $"New";
            saveData.text = "No Save";
            deleteButton.interactable = false;
            deleteButton.image.color = Color.grey;
        }
    }

    public void OnLoadButtonClicked()
    {
        if (saveExists)
        {
            FileOperationsManager.Instance.LoadGame(SaveSlotNumber);
        }
        else
        {
            FileOperationsManager.Instance.SetUpNewGame(SaveSlotNumber);
        }
    }

    public void OnDeleteButtonClicked()
    {
        if (awaitingConfirmation)
        {
            FileOperationsManager.Instance.DeleteSave(SaveSlotNumber);
            awaitingConfirmation = false;
            if (confirmationCoroutine != null)
            {
                StopCoroutine(confirmationCoroutine);
            }
            UpdateButtonState();
        }
        else
        {
            awaitingConfirmation = true;
            string originalText = saveData.text;
            saveData.text = "Press again to delete";
            confirmationCoroutine = StartCoroutine(ResetConfirmationAfterDelay(originalText));
        }
    }

    private IEnumerator ResetConfirmationAfterDelay(string originalText)
    {
        yield return new WaitForSeconds(2f);
        if (awaitingConfirmation)
        {
            awaitingConfirmation = false;
            saveData.text = originalText;
        }
    }
}
