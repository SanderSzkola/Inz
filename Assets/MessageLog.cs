using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MessageLog : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private int maxMessages = 6;
    [SerializeField] private float tempMessageDuration = 2f;

    private Queue<string> messages = new Queue<string>();
    private string temporaryMessage = null;
    private float tempMessageTimer = 0f;

    void Update()
    {
        if (temporaryMessage != null)
        {
            tempMessageTimer -= Time.deltaTime;
            if (tempMessageTimer <= 0f)
            {
                RemoveTemporaryMessage();
            }
        }
    }
    public void AddMessage(string message)
    {
        messages.Enqueue(message);
        if (messages.Count > maxMessages)
        {
            messages.Dequeue();
        }
        RemoveTemporaryMessage();
        UpdateLog();
    }

    public void AddTemporaryMessage(string message)
    {
        temporaryMessage = message;
        tempMessageTimer = tempMessageDuration;

        UpdateLog();
    }

    private void RemoveTemporaryMessage()
    {
        temporaryMessage = null;
        UpdateLog();
    }

    private void UpdateLog()
    {
        string combinedLog = string.Join("\n", messages);
        if (temporaryMessage != null)
        {
            if (!string.IsNullOrEmpty(combinedLog))
            {
                temporaryMessage = "\n" + temporaryMessage;
            }
            combinedLog += temporaryMessage;
        }
        logText.text = combinedLog;
    }

    public void ClearMessages()
    {
        messages.Clear();
        tempMessageTimer = 0f;
    }
}
